using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Analytics;

public static class LatencyBuckets
{
    private static readonly string[] Labels = ["< 25", "25–50", "50–100", "100–250", "250–500", "500–1000", "≥ 1000"];

    public static async Task<IReadOnlyList<CountDto>> CountAsync(IQueryable<UsageRecord> records, CancellationToken cancellationToken)
    {
        // the thresholds here must stay in step with Labels
        var counts = await records
            .GroupBy(r =>
                r.LatencyMs < 25 ? 0 :
                r.LatencyMs < 50 ? 1 :
                r.LatencyMs < 100 ? 2 :
                r.LatencyMs < 250 ? 3 :
                r.LatencyMs < 500 ? 4 :
                r.LatencyMs < 1000 ? 5 : 6)
            .Select(g => new { Bucket = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Bucket, x => x.Count, cancellationToken);

        return Labels.Select((label, index) => new CountDto(label, counts.GetValueOrDefault(index))).ToList();
    }
}
