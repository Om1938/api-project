using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway;

public sealed class RequestSettlement(ICreditLedger ledger, IUsageSink usageSink)
{
    private const int FirstErrorStatus = 400;

    public void SettleRejected(GateContext context, GateRejection rejection, RequestInfo request)
    {
        if (context.Key is null || rejection.Outcome is null)
        {
            return;
        }

        usageSink.Record(ToEvent(context, request, rejection.StatusCode, rejection.Outcome.Value, TimeSpan.Zero, creditsCharged: 0));
    }

    public async Task SettleForwardedAsync(
        GateContext context,
        RequestInfo request,
        int statusCode,
        TimeSpan latency,
        CancellationToken cancellationToken)
    {
        var key = context.Key!;
        var succeeded = statusCode < FirstErrorStatus;

        var charged = succeeded
            && key.CreditCostPerRequest > 0
            && await ledger.TryChargeAsync(key.ConsumerId, key.CreditCostPerRequest, cancellationToken)
                ? key.CreditCostPerRequest
                : 0m;

        var outcome = succeeded ? RequestOutcome.Success : RequestOutcome.Failed;
        usageSink.Record(ToEvent(context, request, statusCode, outcome, latency, charged));
    }

    private static UsageEvent ToEvent(
        GateContext context,
        RequestInfo request,
        int statusCode,
        RequestOutcome outcome,
        TimeSpan latency,
        decimal creditsCharged)
    {
        var key = context.Key!;
        return new UsageEvent(
            key.KeyId,
            key.ApiId,
            key.ConsumerId,
            context.Now,
            request.Method,
            request.Path,
            statusCode,
            outcome,
            (int)latency.TotalMilliseconds,
            creditsCharged);
    }
}
