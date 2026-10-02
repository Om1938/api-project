using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiGateway.Application.Analytics;
using ApiGateway.Application.Webhooks;
using ApiGateway.IntegrationTests.Support;

namespace ApiGateway.IntegrationTests;

[Collection(GatewayAppCollection.Name)]
public class QuotaAndWebhookTests(GatewayAppFixture fixture)
{
    [Fact]
    public async Task Monthly_quota_is_enforced_and_each_quota_event_is_delivered_once_signed()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var hookName = Guid.NewGuid().ToString("N");
        var webhook = await scenario.Owner.PostAsync<WebhookDto>(
            $"/api/apis/{scenario.Api.Id}/webhooks",
            new WebhookRequest(
                $"{fixture.Upstream.BaseUrl}/hooks/{hookName}",
                ThresholdPercent: 50,
                [WebhookEventNames.QuotaThresholdReached, WebhookEventNames.QuotaExceeded],
                IsActive: true));
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync(monthlyQuota: 4));

        var statuses = new List<HttpStatusCode>();
        string? lastErrorCode = null;
        for (var i = 0; i < 6; i++)
        {
            using var response = await scenario.CallAsync(key.Secret);
            statuses.Add(response.StatusCode);
            lastErrorCode = response.IsSuccessStatusCode ? null : await response.ReadErrorCodeAsync();
        }

        Assert.Equal(4, statuses.Count(s => s == HttpStatusCode.OK));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
        Assert.Equal("quota_exceeded", lastErrorCode);

        var usage = await Scenario.EventuallyAsync(
            () => scenario.Owner.GetAsync<OwnerAnalyticsDto>($"/api/analytics?apiId={scenario.Api.Id}"),
            a => a.Report.Summary.TotalRequests == 6);
        Assert.Equal(2, usage.Report.Summary.QuotaExceeded);

        var received = await Scenario.EventuallyAsync(
            () => Task.FromResult(fixture.Upstream.WebhookRequests.Where(r => r.Path.EndsWith(hookName)).ToList()),
            requests => requests.Count >= 2);
        Assert.Equal(
            [WebhookEventNames.QuotaExceeded, WebhookEventNames.QuotaThresholdReached],
            received.Select(r => r.Headers["X-Gateway-Event"]).Order());

        foreach (var request in received)
        {
            var expectedSignature = "sha256=" + Convert.ToHexStringLower(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhook.Secret), Encoding.UTF8.GetBytes(request.Body)));
            Assert.Equal(expectedSignature, request.Headers["X-Gateway-Signature"]);
        }

        var threshold = received.Single(r => r.Headers["X-Gateway-Event"] == WebhookEventNames.QuotaThresholdReached);
        var data = JsonDocument.Parse(threshold.Body).RootElement.GetProperty("data");
        Assert.Equal(2, data.GetProperty("used").GetInt64());
        Assert.Equal(4, data.GetProperty("limit").GetInt32());
        Assert.Equal(50, data.GetProperty("thresholdPercent").GetInt32());
        Assert.Equal(key.Key.Id, data.GetProperty("apiKeyId").GetGuid());

        var deliveries = await Scenario.EventuallyAsync(
            () => scenario.Owner.GetAsync<List<WebhookDeliveryDto>>($"/api/apis/{scenario.Api.Id}/webhooks/deliveries"),
            d => d.Count == 2);
        Assert.Equal(2, deliveries.Count);
        Assert.All(deliveries, d => Assert.True(d is { Success: true, Attempts: 1, ResponseStatus: 200 }));
    }

    [Fact]
    public async Task A_failing_webhook_is_retried_and_logged_as_failed()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var hookName = Guid.NewGuid().ToString("N");
        await scenario.Owner.PostAsync<WebhookDto>(
            $"/api/apis/{scenario.Api.Id}/webhooks",
            new WebhookRequest(
                $"{fixture.Upstream.BaseUrl}/failing-hooks/{hookName}", 100, [WebhookEventNames.QuotaExceeded], IsActive: true));
        var key = await scenario.IssueKeyAsync(await scenario.CreateTierAsync(monthlyQuota: 1));

        using var allowed = await scenario.CallAsync(key.Secret);
        using var overQuota = await scenario.CallAsync(key.Secret);

        var deliveries = await Scenario.EventuallyAsync(
            () => scenario.Owner.GetAsync<List<WebhookDeliveryDto>>($"/api/apis/{scenario.Api.Id}/webhooks/deliveries"),
            d => d.Count == 1);
        var delivery = Assert.Single(deliveries);
        Assert.False(delivery.Success);
        Assert.Equal(3, delivery.Attempts);
        Assert.Equal((500, "HTTP 500"), (delivery.ResponseStatus, delivery.Error));
        Assert.Equal(3, fixture.Upstream.WebhookRequests.Count(r => r.Path.EndsWith(hookName)));
    }
}
