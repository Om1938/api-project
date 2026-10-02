using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway;

public sealed record KeyContext(
    Guid KeyId,
    bool KeyIsActive,
    Guid ConsumerId,
    Guid ApiId,
    string ApiSlug,
    bool ApiIsActive,
    string TargetBaseUrl,
    int RequestsPerMinute,
    int MonthlyQuota,
    decimal CreditCostPerRequest,
    int[] AlertThresholdPercents);

public sealed record RateLimitState(int Limit, long Remaining, DateTimeOffset ResetsAt);

public sealed class GateContext
{
    public required string ApiSlug { get; init; }
    public required string? PresentedKey { get; init; }
    public required DateTimeOffset Now { get; init; }

    public KeyContext? Key { get; set; }

    public RateLimitState? RateLimit { get; set; }
}

public sealed record GateRejection(
    int StatusCode,
    string Code,
    string Message,
    RequestOutcome? Outcome = null,
    TimeSpan? RetryAfter = null);

public readonly record struct GateResult(GateRejection? Rejection)
{
    public bool IsAllowed => Rejection is null;

    public static GateResult Allow { get; } = new(null);

    public static GateResult Deny(GateRejection rejection) => new(rejection);
}

public sealed record RequestInfo(string Method, string Path);

public sealed record UsageEvent(
    Guid ApiKeyId,
    Guid ApiId,
    Guid ConsumerId,
    DateTimeOffset Timestamp,
    string Method,
    string Path,
    int StatusCode,
    RequestOutcome Outcome,
    int LatencyMs,
    decimal CreditsCharged);

public sealed record QuotaAlert(
    Guid ApiId,
    Guid ApiKeyId,
    Guid ConsumerId,
    WebhookEvents Event,
    int? ThresholdPercent,
    long Used,
    int Limit,
    string Period,
    DateTimeOffset OccurredAt);
