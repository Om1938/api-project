using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Common;

public static class StoreKeys
{
    public static string RateLimit(Guid apiKeyId, FixedWindow window) => $"rl:{apiKeyId:N}:{window.Id}";

    public static string Quota(Guid apiKeyId, FixedWindow window) => $"quota:{apiKeyId:N}:{window.Id}";

    public static string KeyContext(string keyHash) => $"keyctx:{keyHash}";

    public static string QuotaAlertSent(Guid apiKeyId, string period, WebhookEvents @event, int? thresholdPercent) =>
        $"alert:{apiKeyId:N}:{period}:{(int)@event}:{thresholdPercent ?? 0}";
}
