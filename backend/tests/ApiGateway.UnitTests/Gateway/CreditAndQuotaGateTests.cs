using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Application.Gateway;
using ApiGateway.Application.Gateway.Gates;
using ApiGateway.Domain.Enums;
using NSubstitute;

namespace ApiGateway.UnitTests.Gateway;

public class CreditGateTests
{
    private readonly ICreditLedger _ledger = Substitute.For<ICreditLedger>();

    private Task<GateResult> EvaluateAsync(KeyContext key, decimal balance)
    {
        _ledger.GetBalanceAsync(key.ConsumerId, Arg.Any<CancellationToken>()).Returns(balance);
        return new CreditGate(_ledger).EvaluateAsync(GateTestData.Context(key), CancellationToken.None);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 50.0)]
    public async Task Allows_when_the_balance_covers_the_cost(decimal cost, decimal balance)
    {
        Assert.True((await EvaluateAsync(GateTestData.Key(cost: cost), balance)).IsAllowed);
    }

    [Fact]
    public async Task Rejects_with_402_when_the_balance_is_too_low()
    {
        var result = await EvaluateAsync(GateTestData.Key(cost: 2m), balance: 1.5m);

        Assert.Equal(402, result.Rejection!.StatusCode);
        Assert.Equal("insufficient_credits", result.Rejection.Code);
        Assert.Equal(RequestOutcome.InsufficientCredits, result.Rejection.Outcome);
    }

    [Fact]
    public async Task Free_tiers_never_read_the_balance()
    {
        var result = await EvaluateAsync(GateTestData.Key(cost: 0m), balance: 0m);

        Assert.True(result.IsAllowed);
        await _ledger.DidNotReceiveWithAnyArgs().GetBalanceAsync(default, default);
    }
}

public class QuotaGateTests
{
    private readonly InMemoryFixedWindowCounter _counter = new();
    private readonly IQuotaUsageReader _usageReader = Substitute.For<IQuotaUsageReader>();
    private readonly IQuotaAlertSink _alerts = Substitute.For<IQuotaAlertSink>();

    private Task<GateResult> EvaluateAsync(KeyContext key, DateTimeOffset? now = null) =>
        new QuotaGate(_counter, _usageReader, _alerts).EvaluateAsync(
            new GateContext { ApiSlug = key.ApiSlug, PresentedKey = "k", Now = now ?? GateTestData.Now, Key = key },
            CancellationToken.None);

    [Fact]
    public async Task Allows_up_to_the_monthly_quota_then_rejects_with_429()
    {
        var key = GateTestData.Key(monthlyQuota: 2);

        Assert.True((await EvaluateAsync(key)).IsAllowed);
        Assert.True((await EvaluateAsync(key)).IsAllowed);
        var rejected = await EvaluateAsync(key);

        Assert.Equal(429, rejected.Rejection!.StatusCode);
        Assert.Equal("quota_exceeded", rejected.Rejection.Code);
        Assert.Equal(RequestOutcome.QuotaExceeded, rejected.Rejection.Outcome);
    }

    [Fact]
    public async Task Quota_resets_in_the_next_calendar_month()
    {
        var key = GateTestData.Key(monthlyQuota: 1);
        await EvaluateAsync(key);
        Assert.False((await EvaluateAsync(key)).IsAllowed);

        Assert.True((await EvaluateAsync(key, GateTestData.Now.AddMonths(1))).IsAllowed);
    }

    [Fact]
    public async Task A_missing_counter_is_rebuilt_from_recorded_usage()
    {
        var key = GateTestData.Key(monthlyQuota: 10);
        _usageReader.CountAsync(key.KeyId, FixedWindow.Month(GateTestData.Now), Arg.Any<CancellationToken>()).Returns(10);

        var result = await EvaluateAsync(key);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task Raises_threshold_and_exceeded_alerts_exactly_when_the_lines_are_crossed()
    {
        var key = GateTestData.Key(monthlyQuota: 4, alertThresholds: 50);
        var raised = new List<QuotaAlert>();
        await _alerts.PublishAsync(Arg.Do<QuotaAlert>(raised.Add), Arg.Any<CancellationToken>());

        for (var i = 0; i < 6; i++)
        {
            await EvaluateAsync(key);
        }

        Assert.Collection(
            raised,
            threshold =>
            {
                Assert.Equal(WebhookEvents.QuotaThresholdReached, threshold.Event);
                Assert.Equal(50, threshold.ThresholdPercent);
                Assert.Equal(2, threshold.Used);
                Assert.Equal("202603", threshold.Period);
            },
            exceeded =>
            {
                Assert.Equal(WebhookEvents.QuotaExceeded, exceeded.Event);
                Assert.Equal(5, exceeded.Used);
                Assert.Equal(key.KeyId, exceeded.ApiKeyId);
            });
    }
}
