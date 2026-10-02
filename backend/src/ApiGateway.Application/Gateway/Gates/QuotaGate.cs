using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway.Gates;

public sealed class QuotaGate(
    IFixedWindowCounter counter,
    IQuotaUsageReader usageReader,
    IQuotaAlertSink alertSink) : IRequestGate
{
    private static readonly TimeSpan ExpiryGrace = TimeSpan.FromHours(1);

    public async Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken)
    {
        var key = context.Key!;
        var window = FixedWindow.Month(context.Now);

        var used = await counter.IncrementAsync(
            StoreKeys.Quota(key.KeyId, window),
            window.RemainingAt(context.Now) + ExpiryGrace,
            ct => usageReader.CountAsync(key.KeyId, window, ct),
            cancellationToken);

        foreach (var (alertEvent, thresholdPercent) in QuotaAlertPolicy.Evaluate(used, key.MonthlyQuota, key.AlertThresholdPercents))
        {
            await alertSink.PublishAsync(
                new QuotaAlert(key.ApiId, key.KeyId, key.ConsumerId, alertEvent, thresholdPercent, used, key.MonthlyQuota, window.Id, context.Now),
                cancellationToken);
        }

        return used <= key.MonthlyQuota
            ? GateResult.Allow
            : GateResult.Deny(new GateRejection(
                429,
                "quota_exceeded",
                $"Monthly quota of {key.MonthlyQuota} requests exceeded.",
                RequestOutcome.QuotaExceeded,
                window.RemainingAt(context.Now)));
    }
}
