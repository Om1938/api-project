using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Gateway;
using ApiGateway.Domain.Enums;
using NSubstitute;

namespace ApiGateway.UnitTests.Gateway;

public class GatePipelineTests
{
    private sealed class RecordingGate(string name, List<string> log, GateResult result) : IRequestGate
    {
        public Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken)
        {
            log.Add(name);
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task Runs_gates_in_registration_order_and_allows_when_all_allow()
    {
        var log = new List<string>();
        var pipeline = new GatePipeline(
        [
            new RecordingGate("first", log, GateResult.Allow),
            new RecordingGate("second", log, GateResult.Allow),
        ]);

        var result = await pipeline.RunAsync(GateTestData.Context(), CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task Stops_at_the_first_rejection()
    {
        var log = new List<string>();
        var rejection = new GateRejection(429, "rate_limit_exceeded", "slow down");
        var pipeline = new GatePipeline(
        [
            new RecordingGate("first", log, GateResult.Deny(rejection)),
            new RecordingGate("second", log, GateResult.Allow),
        ]);

        var result = await pipeline.RunAsync(GateTestData.Context(), CancellationToken.None);

        Assert.Same(rejection, result.Rejection);
        Assert.Equal(["first"], log);
    }
}

public class RequestSettlementTests
{
    private readonly ICreditLedger _ledger = Substitute.For<ICreditLedger>();
    private readonly IUsageSink _sink = Substitute.For<IUsageSink>();
    private readonly List<UsageEvent> _recorded = [];
    private readonly RequestInfo _request = new("GET", "/forecast");

    public RequestSettlementTests()
    {
        _sink.Record(Arg.Do<UsageEvent>(_recorded.Add));
        _ledger.TryChargeAsync(default, default, default).ReturnsForAnyArgs(true);
    }

    private RequestSettlement Settlement => new(_ledger, _sink);

    [Fact]
    public async Task Successful_response_is_charged_the_tier_cost_and_recorded_as_success()
    {
        var key = GateTestData.Key(cost: 2.5m);

        await Settlement.SettleForwardedAsync(
            GateTestData.Context(key), _request, statusCode: 200, TimeSpan.FromMilliseconds(42), CancellationToken.None);

        await _ledger.Received(1).TryChargeAsync(key.ConsumerId, 2.5m, Arg.Any<CancellationToken>());
        var usage = Assert.Single(_recorded);
        Assert.Equal(RequestOutcome.Success, usage.Outcome);
        Assert.Equal(2.5m, usage.CreditsCharged);
        Assert.Equal(42, usage.LatencyMs);
        Assert.Equal((key.KeyId, key.ApiId, key.ConsumerId), (usage.ApiKeyId, usage.ApiId, usage.ConsumerId));
        Assert.Equal(("GET", "/forecast", 200), (usage.Method, usage.Path, usage.StatusCode));
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(502)]
    public async Task Failed_response_is_recorded_but_never_charged(int statusCode)
    {
        await Settlement.SettleForwardedAsync(
            GateTestData.Context(GateTestData.Key()), _request, statusCode, TimeSpan.Zero, CancellationToken.None);

        await _ledger.DidNotReceiveWithAnyArgs().TryChargeAsync(default, default, default);
        var usage = Assert.Single(_recorded);
        Assert.Equal(RequestOutcome.Failed, usage.Outcome);
        Assert.Equal(0m, usage.CreditsCharged);
    }

    [Fact]
    public async Task When_the_balance_ran_out_meanwhile_nothing_is_recorded_as_charged()
    {
        _ledger.TryChargeAsync(default, default, default).ReturnsForAnyArgs(false);

        await Settlement.SettleForwardedAsync(
            GateTestData.Context(GateTestData.Key()), _request, 200, TimeSpan.Zero, CancellationToken.None);

        Assert.Equal(0m, Assert.Single(_recorded).CreditsCharged);
    }

    [Fact]
    public void Metered_rejection_is_recorded_with_its_outcome()
    {
        var rejection = new GateRejection(429, "rate_limit_exceeded", "slow down", RequestOutcome.RateLimited);

        Settlement.SettleRejected(GateTestData.Context(GateTestData.Key()), rejection, _request);

        var usage = Assert.Single(_recorded);
        Assert.Equal(RequestOutcome.RateLimited, usage.Outcome);
        Assert.Equal(429, usage.StatusCode);
    }

    [Fact]
    public void Unauthenticated_rejection_is_not_recorded()
    {
        var rejection = new GateRejection(401, "invalid_api_key", "who are you");

        Settlement.SettleRejected(GateTestData.Context(key: null), rejection, _request);

        Assert.Empty(_recorded);
    }
}
