using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiGateway.Application.Apis;
using ApiGateway.Application.Auth;
using ApiGateway.Application.Keys;
using ApiGateway.Application.Tiers;
using ApiGateway.Domain.Enums;

namespace ApiGateway.IntegrationTests.Support;

public sealed class Scenario
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string Password = "Password123!";
    private readonly GatewayAppFixture _fixture;

    private Scenario(GatewayAppFixture fixture) => _fixture = fixture;

    public HttpClient Owner { get; private set; } = null!;
    public HttpClient Consumer { get; private set; } = null!;
    public HttpClient Anonymous { get; private set; } = null!;
    public string ConsumerEmail { get; private set; } = null!;
    public ApiDto Api { get; private set; } = null!;

    public static async Task<Scenario> StartAsync(GatewayAppFixture fixture)
    {
        var scenario = new Scenario(fixture);
        var id = Guid.NewGuid().ToString("N")[..12];
        scenario.ConsumerEmail = $"consumer-{id}@test.local";

        scenario.Anonymous = fixture.CreateClient();
        scenario.Owner = await scenario.RegisterAsync($"owner-{id}@test.local", UserRole.Owner);
        scenario.Consumer = await scenario.RegisterAsync(scenario.ConsumerEmail, UserRole.Consumer);
        scenario.Api = await scenario.Owner.PostAsync<ApiDto>(
            "/api/apis",
            new CreateApiRequest($"Test API {id}", $"api-{id}", null, $"{fixture.Upstream.BaseUrl}/api"));

        return scenario;
    }

    public async Task<HttpClient> RegisterAsync(string email, UserRole role)
    {
        var auth = await Anonymous.PostAsync<AuthResponse>(
            "/api/auth/register", new RegisterRequest("Test User", email, Password, role));

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public Task<TierDto> CreateTierAsync(int requestsPerMinute = 100, int monthlyQuota = 1000, decimal cost = 1m) =>
        Owner.PostAsync<TierDto>(
            $"/api/apis/{Api.Id}/tiers",
            new TierRequest($"tier-{Guid.NewGuid():N}", requestsPerMinute, monthlyQuota, cost));

    public Task<CreatedApiKeyDto> IssueKeyAsync(TierDto tier) =>
        Owner.PostAsync<CreatedApiKeyDto>(
            $"/api/apis/{Api.Id}/keys",
            new CreateApiKeyRequest("test key", tier.Id, ConsumerEmail));

    public Task<HttpResponseMessage> CallAsync(string? apiKey, string path = "/ping", string? apiSlug = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/gw/{apiSlug ?? Api.Slug}{path}");
        if (apiKey is not null)
        {
            request.Headers.Add("X-API-Key", apiKey);
        }

        return Anonymous.SendAsync(request);
    }

    public static async Task AvoidMinuteBoundaryAsync()
    {
        while (DateTime.UtcNow.Second > 50)
        {
            await Task.Delay(250);
        }
    }

    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> read, Func<T, bool> isExpected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var value = await read();
            if (isExpected(value) || DateTime.UtcNow > deadline)
            {
                return value;
            }

            await Task.Delay(100);
        }
    }
}

public static class HttpClientJsonExtensions
{
    public static async Task<T> GetAsync<T>(this HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        return await response.ReadAsync<T>();
    }

    public static async Task<T> PostAsync<T>(this HttpClient client, string url, object? body = null)
    {
        using var response = await client.PostAsJsonAsync(url, body, Scenario.Json);
        return await response.ReadAsync<T>();
    }

    public static async Task<T> PutAsync<T>(this HttpClient client, string url, object body)
    {
        using var response = await client.PutAsJsonAsync(url, body, Scenario.Json);
        return await response.ReadAsync<T>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode} from {response.RequestMessage?.RequestUri}: {content}");
        return JsonSerializer.Deserialize<T>(content, Scenario.Json)!;
    }

    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
