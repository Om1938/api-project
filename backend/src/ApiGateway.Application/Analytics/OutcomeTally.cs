using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Analytics;

internal sealed class OutcomeTally
{
    private readonly Dictionary<RequestOutcome, long> _counts = [];
    private decimal _credits;
    private long _forwarded;
    private long _latencyMs;

    public void Add(RequestOutcome outcome, long count, decimal credits, long latencyMs = 0)
    {
        _counts[outcome] = _counts.GetValueOrDefault(outcome) + count;
        _credits += credits;

        // rejected requests never reach the upstream, so they have no latency
        if (outcome is RequestOutcome.Success or RequestOutcome.Failed)
        {
            _forwarded += count;
            _latencyMs += latencyMs;
        }
    }

    public double AverageLatencyMs => _forwarded == 0 ? 0 : Math.Round((double)_latencyMs / _forwarded, 1);

    public UsageSummaryDto ToSummary() => new(
        _counts.Values.Sum(),
        _counts.GetValueOrDefault(RequestOutcome.Success),
        _counts.GetValueOrDefault(RequestOutcome.Failed),
        _counts.GetValueOrDefault(RequestOutcome.RateLimited),
        _counts.GetValueOrDefault(RequestOutcome.QuotaExceeded),
        _counts.GetValueOrDefault(RequestOutcome.InsufficientCredits),
        _credits);
}
