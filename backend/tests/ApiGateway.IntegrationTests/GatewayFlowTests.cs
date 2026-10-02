using System.Net;
using ApiGateway.Application.Analytics;
using ApiGateway.Application.Billing;
using ApiGateway.Application.Consumers;
using ApiGateway.Application.Keys;
using ApiGateway.Application.Tiers;
using ApiGateway.Domain.Enums;
using ApiGateway.IntegrationTests.Support;

namespace ApiGateway.IntegrationTests;

[Collection(GatewayAppCollection.Name)]
public class GatewayFlowTests(GatewayAppFixture fixture)
{
    [Fact]
    public async Task Requests_are_forwarded_then_rate_limited_and_all_of_it_shows_up_in_usage_and_billing()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var tier = await scenario.CreateTierAsync(requestsPerMinute: 3, cost: 2m);
        var key = await scenario.IssueKeyAsync(tier);
        await Scenario.AvoidMinuteBoundaryAsync();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            using var response = await scenario.CallAsync(key.Secret, "/orders/42?expand=items");
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests],
            statuses);

        var analytics = await Scenario.EventuallyAsync(
            () => scenario.Owner.GetAsync<OwnerAnalyticsDto>($"/api/analytics?apiId={scenario.Api.Id}"),
            a => a.Report.Summary.TotalRequests == 4);
        Assert.Equal(new UsageSummaryDto(4, 3, 0, 1, 0, 0, 6m), analytics.Report.Summary);
        Assert.Equal(4, analytics.Report.Series.Sum(point => point.Usage.TotalRequests));
        var breakdown = analytics.Report.Breakdown;
        Assert.Equal([new CountDto("200", 3), new CountDto("429", 1)], breakdown.StatusCodes);
        Assert.Equal([new CountDto("GET", 4)], breakdown.Methods);
        Assert.Equal([new CountDto("/orders/{id}", 4)], breakdown.Endpoints);
        Assert.Equal([new CountDto(scenario.Api.Name, 4)], breakdown.Apis);
        Assert.Equal([new CountDto($"{scenario.Api.Name} · {tier.Name}", 4)], breakdown.Tiers);
        Assert.Equal(3, breakdown.LatencyBuckets.Sum(b => b.Requests));
        Assert.Equal(4, breakdown.Heatmap.Sum(c => c.Requests));
        Assert.True(analytics.Report.AverageLatencyMs >= 0);

        var consumerUsage = Assert.Single(analytics.Consumers);
        Assert.Equal(scenario.ConsumerEmail, consumerUsage.Email);
        Assert.Equal(3, consumerUsage.Usage.Successful);

        var myKey = Assert.Single(await scenario.Consumer.GetAsync<List<MyKeyDto>>("/api/me/keys"));
        Assert.Equal((3L, 997L), (myKey.QuotaUsed, myKey.QuotaRemaining));
        Assert.Equal(key.Key.KeyPrefix, myKey.KeyPrefix);

        var usage = await scenario.Consumer.GetAsync<UsageReportDto>("/api/me/usage");
        Assert.Equal(1, usage.Summary.RateLimited);

        var credits = await scenario.Consumer.GetAsync<CreditAccountDto>("/api/me/credits");
        Assert.Equal(CreditPolicy.WelcomeBonus - 6m, credits.Balance);
        Assert.Equal(6m, credits.SpentThisMonth);
    }

    [Fact]
    public async Task Upstream_receives_the_path_and_query_but_never_the_gateway_key()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync());
        var marker = Guid.NewGuid().ToString("N");

        using var response = await scenario.CallAsync(key.Secret, $"/orders/42?marker={marker}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var received = Assert.Single(fixture.Upstream.ApiRequests, r => r.Query.Contains(marker));
        Assert.Equal("/api/orders/42", received.Path);
        Assert.DoesNotContain("X-API-Key", received.Headers.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rate_limit_response_tells_the_client_when_to_retry()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync(requestsPerMinute: 1));
        await Scenario.AvoidMinuteBoundaryAsync();

        using var allowed = await scenario.CallAsync(key.Secret);
        using var limited = await scenario.CallAsync(key.Secret);

        Assert.Equal("0", allowed.Headers.GetValues("X-RateLimit-Remaining").Single());
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("rate_limit_exceeded", await limited.ReadErrorCodeAsync());
        Assert.Equal("1", limited.Headers.GetValues("X-RateLimit-Limit").Single());
        Assert.InRange(limited.Headers.RetryAfter!.Delta!.Value, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task Missing_unknown_and_foreign_keys_are_refused()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var other = await Scenario.StartAsync(fixture);
        var otherKey = await other.IssueKeyAsync(await other.CreateTierAsync());

        using var missing = await scenario.CallAsync(apiKey: null);
        using var unknown = await scenario.CallAsync("gw_not-a-real-key");
        using var foreign = await scenario.CallAsync(otherKey.Secret);

        Assert.Equal((HttpStatusCode.Unauthorized, "missing_api_key"), (missing.StatusCode, await missing.ReadErrorCodeAsync()));
        Assert.Equal((HttpStatusCode.Unauthorized, "invalid_api_key"), (unknown.StatusCode, await unknown.ReadErrorCodeAsync()));
        Assert.Equal((HttpStatusCode.Forbidden, "key_not_valid_for_api"), (foreign.StatusCode, await foreign.ReadErrorCodeAsync()));
    }

    [Fact]
    public async Task Revoking_a_key_blocks_it_on_the_very_next_request()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync());
        using var before = await scenario.CallAsync(key.Secret); // also warms the key cache

        var revoked = await scenario.Owner.PostAsync<ApiKeyDto>($"/api/apis/{scenario.Api.Id}/keys/{key.Key.Id}/revoke");
        using var after = await scenario.CallAsync(key.Secret);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal(ApiKeyStatus.Revoked, revoked.Status);
        Assert.Equal((HttpStatusCode.Unauthorized, "revoked_api_key"), (after.StatusCode, await after.ReadErrorCodeAsync()));
    }

    [Fact]
    public async Task Changing_a_tier_applies_to_existing_keys_immediately()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var tier = await scenario.CreateTierAsync(requestsPerMinute: 1);
        var key = await scenario.IssueKeyAsync(tier);
        await Scenario.AvoidMinuteBoundaryAsync();
        using var first = await scenario.CallAsync(key.Secret);
        using var limited = await scenario.CallAsync(key.Secret);

        await scenario.Owner.PutAsync<TierDto>(
            $"/api/apis/{scenario.Api.Id}/tiers/{tier.Id}",
            new TierRequest(tier.Name, RequestsPerMinute: 50, tier.MonthlyQuota, tier.CreditCostPerRequest));
        using var afterUpgrade = await scenario.CallAsync(key.Secret);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterUpgrade.StatusCode);
    }

    [Fact]
    public async Task Requests_the_consumer_cannot_afford_get_402_and_topping_up_unblocks_them()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync(cost: CreditPolicy.WelcomeBonus + 50m));

        using var refused = await scenario.CallAsync(key.Secret);
        var account = await scenario.Consumer.PostAsync<CreditAccountDto>("/api/me/credits/top-up", new TopUpRequest(50m));
        using var accepted = await scenario.CallAsync(key.Secret);

        Assert.Equal((HttpStatusCode.PaymentRequired, "insufficient_credits"), (refused.StatusCode, await refused.ReadErrorCodeAsync()));
        Assert.Equal(CreditPolicy.WelcomeBonus + 50m, account.Balance);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var after = await scenario.Consumer.GetAsync<CreditAccountDto>("/api/me/credits");
        Assert.Equal(0m, after.Balance);
    }

    [Fact]
    public async Task Failed_upstream_responses_are_passed_through_metered_as_failed_and_not_charged()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync(cost: 5m));

        using var response = await scenario.CallAsync(key.Secret, "/status/503");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var usage = await Scenario.EventuallyAsync(
            () => scenario.Consumer.GetAsync<UsageReportDto>("/api/me/usage"),
            u => u.Summary.TotalRequests == 1);
        Assert.Equal(new UsageSummaryDto(1, 0, 1, 0, 0, 0, 0m), usage.Summary);
        Assert.Equal(CreditPolicy.WelcomeBonus, (await scenario.Consumer.GetAsync<CreditAccountDto>("/api/me/credits")).Balance);
    }

    [Fact]
    public async Task An_upstream_that_drops_the_connection_yields_502_and_is_metered_as_failed()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync());

        using var response = await scenario.CallAsync(key.Secret, "/abort");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var usage = await Scenario.EventuallyAsync(
            () => scenario.Consumer.GetAsync<UsageReportDto>("/api/me/usage"),
            u => u.Summary.TotalRequests == 1);
        Assert.Equal(1, usage.Summary.Failed);
    }
}
