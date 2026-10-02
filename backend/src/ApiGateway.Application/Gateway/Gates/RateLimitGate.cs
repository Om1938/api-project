using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway.Gates;

public sealed class RateLimitGate(IFixedWindowCounter counter) : IRequestGate
{
    private static readonly TimeSpan ExpiryGrace = TimeSpan.FromSeconds(5);

    public async Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken)
    {
        var key = context.Key!;
        var window = FixedWindow.Minute(context.Now);
        var remainingTime = window.RemainingAt(context.Now);

        var count = await counter.IncrementAsync(
            StoreKeys.RateLimit(key.KeyId, window),
            remainingTime + ExpiryGrace,
            cancellationToken);

        var limit = key.RequestsPerMinute;
        context.RateLimit = new RateLimitState(limit, Math.Max(0, limit - count), window.End);

        return count <= limit
            ? GateResult.Allow
            : GateResult.Deny(new GateRejection(
                429,
                "rate_limit_exceeded",
                $"Rate limit of {limit} requests per minute exceeded.",
                RequestOutcome.RateLimited,
                remainingTime));
    }
}
