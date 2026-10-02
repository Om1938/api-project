using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway;

public static class QuotaAlertPolicy
{
    public static IEnumerable<(WebhookEvents Event, int? ThresholdPercent)> Evaluate(
        long used,
        int limit,
        IEnumerable<int> thresholdPercents)
    {
        foreach (var percent in thresholdPercents.Distinct())
        {
            if (used == ThresholdCount(limit, percent))
            {
                yield return (WebhookEvents.QuotaThresholdReached, percent);
            }
        }

        if (used == (long)limit + 1)
        {
            yield return (WebhookEvents.QuotaExceeded, null);
        }
    }

    public static long ThresholdCount(int limit, int percent) =>
        Math.Max(1, (long)Math.Ceiling(limit * (percent / 100m)));
}
