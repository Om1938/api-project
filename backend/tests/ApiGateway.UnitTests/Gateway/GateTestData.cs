using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Gateway;

namespace ApiGateway.UnitTests.Gateway;

internal static class GateTestData
{
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 30, 20, TimeSpan.Zero);

    public static KeyContext Key(
        bool keyIsActive = true,
        bool apiIsActive = true,
        string apiSlug = "weather",
        int requestsPerMinute = 5,
        int monthlyQuota = 100,
        decimal cost = 1m,
        params int[] alertThresholds) =>
        new(
            KeyId: Guid.NewGuid(),
            KeyIsActive: keyIsActive,
            ConsumerId: Guid.NewGuid(),
            ApiId: Guid.NewGuid(),
            ApiSlug: apiSlug,
            ApiIsActive: apiIsActive,
            TargetBaseUrl: "http://upstream.test",
            RequestsPerMinute: requestsPerMinute,
            MonthlyQuota: monthlyQuota,
            CreditCostPerRequest: cost,
            AlertThresholdPercents: alertThresholds);

    public static GateContext Context(KeyContext? key = null, string apiSlug = "weather", string? presentedKey = "gw_secret") =>
        new() { ApiSlug = apiSlug, PresentedKey = presentedKey, Now = Now, Key = key };
}

internal sealed class InMemoryFixedWindowCounter : IFixedWindowCounter
{
    private readonly Dictionary<string, long> _counts = [];

    public Dictionary<string, TimeSpan> TimeToLive { get; } = [];

    public Task<long> IncrementAsync(string key, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        TimeToLive.TryAdd(key, timeToLive);
        return Task.FromResult(_counts[key] = _counts.GetValueOrDefault(key) + 1);
    }

    public async Task<long> IncrementAsync(
        string key,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<long>> seed,
        CancellationToken cancellationToken)
    {
        if (!_counts.ContainsKey(key))
        {
            _counts[key] = await seed(cancellationToken);
        }

        return await IncrementAsync(key, timeToLive, cancellationToken);
    }
}
