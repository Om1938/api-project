using System.Net;
using System.Net.Http.Json;
using ApiGateway.Application.Apis;
using ApiGateway.Application.Auth;
using ApiGateway.Application.Consumers;
using ApiGateway.Application.Keys;
using ApiGateway.Application.Tiers;
using ApiGateway.Application.Webhooks;
using ApiGateway.Domain.Enums;
using ApiGateway.IntegrationTests.Support;

namespace ApiGateway.IntegrationTests;

[Collection(GatewayAppCollection.Name)]
public class ManagementApiTests(GatewayAppFixture fixture)
{
    [Fact]
    public async Task Dashboard_endpoints_require_a_token_and_the_right_role()
    {
        var scenario = await Scenario.StartAsync(fixture);

        using var anonymous = await scenario.Anonymous.GetAsync("/api/apis");
        using var consumerOnOwnerRoute = await scenario.Consumer.GetAsync("/api/apis");
        using var ownerOnConsumerRoute = await scenario.Owner.GetAsync("/api/me/keys");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, consumerOnOwnerRoute.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, ownerOnConsumerRoute.StatusCode);
    }

    [Fact]
    public async Task Login_returns_a_working_token_and_rejects_a_wrong_password()
    {
        var scenario = await Scenario.StartAsync(fixture);

        var auth = await scenario.Anonymous.PostAsync<AuthResponse>(
            "/api/auth/login", new LoginRequest(scenario.ConsumerEmail.ToUpperInvariant(), "Password123!"));
        using var wrong = await scenario.Anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(scenario.ConsumerEmail, "not-the-password"));

        Assert.Equal(UserRole.Consumer, auth.User.Role);
        Assert.Equal(scenario.ConsumerEmail, (await scenario.Consumer.GetAsync<UserDto>("/api/auth/me")).Email);
        Assert.Equal((HttpStatusCode.Unauthorized, "invalid_credentials"), (wrong.StatusCode, await wrong.ReadErrorCodeAsync()));
    }

    [Fact]
    public async Task Registering_the_same_email_twice_conflicts()
    {
        var scenario = await Scenario.StartAsync(fixture);

        using var duplicate = await scenario.Anonymous.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Again", scenario.ConsumerEmail, "Password123!", UserRole.Consumer),
            Scenario.Json);

        Assert.Equal((HttpStatusCode.Conflict, "email_taken"), (duplicate.StatusCode, await duplicate.ReadErrorCodeAsync()));
    }

    [Fact]
    public async Task An_owner_cannot_see_or_change_another_owners_api()
    {
        var mine = await Scenario.StartAsync(fixture);
        var theirs = await Scenario.StartAsync(fixture);

        using var get = await mine.Owner.GetAsync($"/api/apis/{theirs.Api.Id}");
        using var createTier = await mine.Owner.PostAsJsonAsync(
            $"/api/apis/{theirs.Api.Id}/tiers", new TierRequest("Sneaky", 1, 1, 0m));
        using var analytics = await mine.Owner.GetAsync($"/api/analytics?apiId={theirs.Api.Id}");
        var list = await mine.Owner.GetAsync<List<ApiDto>>("/api/apis");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createTier.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, analytics.StatusCode);
        Assert.Equal([mine.Api.Id], list.Select(a => a.Id));
    }

    [Fact]
    public async Task Invalid_input_is_rejected_with_field_errors()
    {
        var scenario = await Scenario.StartAsync(fixture);

        using var response = await scenario.Owner.PostAsJsonAsync(
            "/api/apis", new CreateApiRequest("", null, null, "ftp://not-http"));

        Assert.Equal((HttpStatusCode.BadRequest, "validation_failed"), (response.StatusCode, await response.ReadErrorCodeAsync()));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TargetBaseUrl", body);
        Assert.Contains("Name", body);
    }

    [Fact]
    public async Task Slugs_are_derived_from_the_name_and_must_be_unique()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var name = $"Weather {Guid.NewGuid():N}";

        var created = await scenario.Owner.PostAsync<ApiDto>(
            "/api/apis", new CreateApiRequest(name, null, "desc", "https://example.com/v1/"));
        using var duplicate = await scenario.Owner.PostAsJsonAsync(
            "/api/apis", new CreateApiRequest(name, null, null, "https://example.com"));

        Assert.Equal(Slugs.From(name), created.Slug);
        Assert.Equal("https://example.com/v1", created.TargetBaseUrl);
        Assert.Equal((HttpStatusCode.Conflict, "slug_taken"), (duplicate.StatusCode, await duplicate.ReadErrorCodeAsync()));
    }

    [Fact]
    public async Task Keys_need_an_existing_consumer_and_a_tier_of_the_same_api()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var other = await Scenario.StartAsync(fixture);
        var tier = await scenario.CreateTierAsync();
        var foreignTier = await other.CreateTierAsync();
        var url = $"/api/apis/{scenario.Api.Id}/keys";

        using var unknownConsumer = await scenario.Owner.PostAsJsonAsync(
            url, new CreateApiKeyRequest("k", tier.Id, "nobody@test.local"));
        using var foreign = await scenario.Owner.PostAsJsonAsync(
            url, new CreateApiKeyRequest("k", foreignTier.Id, scenario.ConsumerEmail));

        Assert.Equal("unknown_consumer", await unknownConsumer.ReadErrorCodeAsync());
        Assert.Equal("unknown_tier", await foreign.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task The_key_secret_is_shown_once_and_never_listed()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var created = await scenario.IssueKeyAsync(await scenario.CreateTierAsync());

        var second = await scenario.IssueKeyAsync(await scenario.CreateTierAsync());

        using var ownerList = await scenario.Owner.GetAsync($"/api/apis/{scenario.Api.Id}/keys");
        using var consumerList = await scenario.Consumer.GetAsync("/api/me/keys");

        Assert.StartsWith("gw_", created.Secret);
        Assert.DoesNotContain(created.Secret, await ownerList.Content.ReadAsStringAsync());
        Assert.DoesNotContain(created.Secret, await consumerList.Content.ReadAsStringAsync());

        var listed = await ownerList.ReadAsync<List<ApiKeyDto>>();
        Assert.Equal([second.Key.Id, created.Key.Id], listed.Select(k => k.Id)); // newest first
        Assert.All(listed, k => Assert.Equal((ApiKeyStatus.Active, scenario.ConsumerEmail), (k.Status, k.ConsumerEmail)));

        var consumers = await scenario.Owner.GetAsync<List<ConsumerDto>>("/api/consumers");
        Assert.Equal(2, consumers.Single(c => c.Email == scenario.ConsumerEmail).ActiveKeyCount);
    }

    [Fact]
    public async Task A_tier_with_keys_cannot_be_deleted_but_the_whole_api_can()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var tier = await scenario.CreateTierAsync();
        var key = await scenario.IssueKeyAsync(tier);

        using var deleteTier = await scenario.Owner.DeleteAsync($"/api/apis/{scenario.Api.Id}/tiers/{tier.Id}");
        using var deleteApi = await scenario.Owner.DeleteAsync($"/api/apis/{scenario.Api.Id}");
        using var callAfterDelete = await scenario.CallAsync(key.Secret);

        Assert.Equal((HttpStatusCode.Conflict, "tier_in_use"), (deleteTier.StatusCode, await deleteTier.ReadErrorCodeAsync()));
        Assert.Equal(HttpStatusCode.NoContent, deleteApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, callAfterDelete.StatusCode);
        Assert.Empty(await scenario.Consumer.GetAsync<List<MyKeyDto>>("/api/me/keys"));
    }

    [Fact]
    public async Task Tiers_and_webhooks_can_be_listed_changed_and_removed()
    {
        var scenario = await Scenario.StartAsync(fixture);
        var apiUrl = $"/api/apis/{scenario.Api.Id}";
        var tier = await scenario.CreateTierAsync(requestsPerMinute: 10);
        var webhook = await scenario.Owner.PostAsync<WebhookDto>(
            $"{apiUrl}/webhooks",
            new WebhookRequest("https://example.com/hook", 80, [WebhookEventNames.QuotaExceeded], IsActive: true));

        var api = await scenario.Owner.GetAsync<ApiDto>(apiUrl);
        var tiers = await scenario.Owner.GetAsync<List<TierDto>>($"{apiUrl}/tiers");
        using var duplicateTier = await scenario.Owner.PostAsJsonAsync(
            $"{apiUrl}/tiers", new TierRequest(tier.Name, 1, 1, 0m));
        var updated = await scenario.Owner.PutAsync<WebhookDto>(
            $"{apiUrl}/webhooks/{webhook.Id}",
            new WebhookRequest("https://example.com/other", 50, [WebhookEventNames.QuotaThresholdReached], IsActive: false));
        var webhooks = await scenario.Owner.GetAsync<List<WebhookDto>>($"{apiUrl}/webhooks");
        using var deleteWebhook = await scenario.Owner.DeleteAsync($"{apiUrl}/webhooks/{webhook.Id}");
        using var deleteTier = await scenario.Owner.DeleteAsync($"{apiUrl}/tiers/{tier.Id}");

        Assert.Equal(scenario.Api.Slug, api.Slug);
        Assert.Equal(tier, Assert.Single(tiers));
        Assert.Equal("tier_name_taken", await duplicateTier.ReadErrorCodeAsync());
        Assert.Equal(webhook.Secret, updated.Secret);
        Assert.Equal(("https://example.com/other", 50, false), (updated.Url, updated.ThresholdPercent, updated.IsActive));
        Assert.Equal([WebhookEventNames.QuotaThresholdReached], Assert.Single(webhooks).Events);
        Assert.Equal(HttpStatusCode.NoContent, deleteWebhook.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteTier.StatusCode);
        Assert.Empty(await scenario.Owner.GetAsync<List<TierDto>>($"{apiUrl}/tiers"));
        Assert.Empty(await scenario.Owner.GetAsync<List<WebhookDto>>($"{apiUrl}/webhooks"));
    }
}
