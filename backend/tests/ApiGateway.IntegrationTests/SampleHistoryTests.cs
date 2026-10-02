using System.Net.Http.Headers;
using ApiGateway.Application.Analytics;
using ApiGateway.Application.Auth;
using ApiGateway.Application.Billing;
using ApiGateway.Application.Consumers;
using ApiGateway.IntegrationTests.Support;

namespace ApiGateway.IntegrationTests;

[Collection(GatewayAppCollection.Name)]
public class SampleHistoryTests(GatewayAppFixture fixture)
{
    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = fixture.CreateClient();
        var auth = await client.PostAsync<AuthResponse>("/api/auth/login", new LoginRequest(email, "Password123!"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Seeded_history_fills_every_day_of_the_owners_report_with_plausible_usage()
    {
        var owner = await SignInAsync("owner@demo.local");

        var analytics = await owner.GetAsync<OwnerAnalyticsDto>(
            $"/api/analytics?from={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-30).ToString("O"))}");
        var report = analytics.Report;

        Assert.InRange(report.Series.Count(day => day.Usage.TotalRequests > 0), GatewayAppFixture.SeededHistoryDays, GatewayAppFixture.SeededHistoryDays + 1);
        Assert.Equal(4, analytics.Consumers.Count);
        Assert.True(report.Summary.Successful > report.Summary.TotalRequests * 0.7);
        Assert.True(report.Summary.Failed > 0 && report.Summary.RateLimited > 0);
        Assert.InRange(report.AverageLatencyMs, 20, 200);
        Assert.Equal(report.Summary.TotalRequests, report.Breakdown.Heatmap.Sum(cell => cell.Requests));
        Assert.Equal(2, report.Breakdown.Tiers.Count);
        Assert.Contains(report.Breakdown.Endpoints, endpoint => endpoint.Label == "/anything/users/{id}");
    }

    [Theory]
    [InlineData("consumer@demo.local")]
    [InlineData("fabrikam@demo.local")]
    public async Task Seeded_history_never_puts_a_key_over_its_monthly_quota(string email)
    {
        var consumer = await SignInAsync(email);

        var key = Assert.Single(await consumer.GetAsync<List<MyKeyDto>>("/api/me/keys"));

        Assert.InRange(key.QuotaUsed, 1, key.MonthlyQuota);

        var credits = await consumer.GetAsync<CreditAccountDto>("/api/me/credits");
        Assert.Equal(100m, credits.Balance);
        Assert.True(credits.Transactions.Sum(t => t.Amount) > credits.SpentThisMonth);
    }
}
