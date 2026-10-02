using ApiGateway.Application.Gateway;
using ApiGateway.Application.Gateway.Gates;
using ApiGateway.Domain.Enums;

namespace ApiGateway.UnitTests.Gateway;

public class RateLimitGateTests
{
    private readonly InMemoryFixedWindowCounter _counter = new();

    private Task<GateResult> EvaluateAsync(KeyContext key, DateTimeOffset now)
    {
        var context = new GateContext { ApiSlug = key.ApiSlug, PresentedKey = "k", Now = now, Key = key };
        return EvaluateAsync(context);
    }

    private Task<GateResult> EvaluateAsync(GateContext context) =>
        new RateLimitGate(_counter).EvaluateAsync(context, CancellationToken.None);

    [Fact]
    public async Task Allows_exactly_the_limit_within_one_minute_then_rejects_with_429()
    {
        var key = GateTestData.Key(requestsPerMinute: 3);

        for (var i = 0; i < 3; i++)
        {
            Assert.True((await EvaluateAsync(key, GateTestData.Now)).IsAllowed);
        }

        var rejected = await EvaluateAsync(key, GateTestData.Now);

        Assert.Equal(429, rejected.Rejection!.StatusCode);
        Assert.Equal("rate_limit_exceeded", rejected.Rejection.Code);
        Assert.Equal(RequestOutcome.RateLimited, rejected.Rejection.Outcome);
    }

    [Fact]
    public async Task A_new_minute_starts_a_fresh_window()
    {
        var key = GateTestData.Key(requestsPerMinute: 1);
        await EvaluateAsync(key, GateTestData.Now);
        Assert.False((await EvaluateAsync(key, GateTestData.Now)).IsAllowed);

        var nextMinute = await EvaluateAsync(key, GateTestData.Now.AddMinutes(1));

        Assert.True(nextMinute.IsAllowed);
    }

    [Fact]
    public async Task Keys_are_limited_independently()
    {
        var first = GateTestData.Key(requestsPerMinute: 1);
        var second = GateTestData.Key(requestsPerMinute: 1);
        await EvaluateAsync(first, GateTestData.Now);

        Assert.True((await EvaluateAsync(second, GateTestData.Now)).IsAllowed);
    }

    [Fact]
    public async Task Retry_after_is_the_time_left_in_the_window()
    {
        var key = GateTestData.Key(requestsPerMinute: 1);
        await EvaluateAsync(key, GateTestData.Now); // 10:30:20
        var rejected = await EvaluateAsync(key, GateTestData.Now);

        Assert.Equal(TimeSpan.FromSeconds(40), rejected.Rejection!.RetryAfter);
    }

    [Fact]
    public async Task Publishes_rate_limit_state_for_response_headers()
    {
        var key = GateTestData.Key(requestsPerMinute: 5);
        var context = GateTestData.Context(key);

        await EvaluateAsync(context);
        await EvaluateAsync(context);

        Assert.Equal(new RateLimitState(5, 3, new DateTimeOffset(2026, 3, 15, 10, 31, 0, TimeSpan.Zero)), context.RateLimit);
    }

    [Fact]
    public async Task Counter_expires_shortly_after_the_window_ends()
    {
        var key = GateTestData.Key();

        await EvaluateAsync(key, GateTestData.Now);

        var timeToLive = Assert.Single(_counter.TimeToLive.Values);
        Assert.InRange(timeToLive, TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(60));
    }
}
