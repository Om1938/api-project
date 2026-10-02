using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Analytics;

public sealed class UsageAggregator(IAppDbContext db, UsageBreakdownBuilder breakdowns)
{
    public async Task<UsageReportDto> BuildReportAsync(
        IQueryable<UsageRecord> scope,
        ReportRange range,
        CancellationToken cancellationToken)
    {
        // whole hours unless the report is finer than that; 60 folds every minute into :00
        var minuteStep = range.IsSubHourly ? (int)range.Bucket.TotalMinutes : 60;

        var rows = await InRange(scope, range)
            .GroupBy(r => new { Day = r.Timestamp.Date, r.Timestamp.Hour, Minute = r.Timestamp.Minute / minuteStep * minuteStep, r.Outcome })
            .Select(g => new
            {
                g.Key.Day,
                g.Key.Hour,
                g.Key.Minute,
                g.Key.Outcome,
                Count = g.LongCount(),
                Credits = g.Sum(r => r.CreditsCharged),
                Latency = g.Sum(r => (long)r.LatencyMs),
            })
            .ToListAsync(cancellationToken);

        var overall = new OutcomeTally();
        var buckets = range.Buckets().ToDictionary(bucket => bucket, _ => new OutcomeTally());

        var heatmap = new Dictionary<(DayOfWeek Day, int Hour), long>();

        foreach (var row in rows)
        {
            overall.Add(row.Outcome, row.Count, row.Credits, row.Latency);

            var bucket = range.BucketOf(row.Day.AddHours(row.Hour).AddMinutes(row.Minute));
            if (buckets.TryGetValue(bucket, out var tally))
            {
                tally.Add(row.Outcome, row.Count, row.Credits, row.Latency);
            }

            var cell = (row.Day.DayOfWeek, row.Hour);
            heatmap[cell] = heatmap.GetValueOrDefault(cell) + row.Count;
        }

        var series = buckets
            .OrderBy(pair => pair.Key)
            .Select(pair => new UsagePointDto(pair.Key, pair.Value.ToSummary(), pair.Value.AverageLatencyMs))
            .ToList();

        var cells = heatmap
            .Select(pair => new HeatmapCellDto(pair.Key.Day, pair.Key.Hour, pair.Value))
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Hour)
            .ToList();

        return new UsageReportDto(
            range.From,
            range.To,
            (int)range.Bucket.TotalMinutes,
            overall.ToSummary(),
            overall.AverageLatencyMs,
            series,
            await breakdowns.BuildAsync(InRange(scope, range), cells, cancellationToken));
    }

    public async Task<IReadOnlyList<ConsumerUsageDto>> BuildConsumerBreakdownAsync(
        IQueryable<UsageRecord> scope,
        ReportRange range,
        CancellationToken cancellationToken)
    {
        var rows = await InRange(scope, range)
            .GroupBy(r => new { r.ConsumerId, r.Outcome })
            .Select(g => new
            {
                g.Key.ConsumerId,
                g.Key.Outcome,
                Count = g.LongCount(),
                Credits = g.Sum(r => r.CreditsCharged),
            })
            .ToListAsync(cancellationToken);

        var tallies = new Dictionary<Guid, OutcomeTally>();
        foreach (var row in rows)
        {
            if (!tallies.TryGetValue(row.ConsumerId, out var tally))
            {
                tallies[row.ConsumerId] = tally = new OutcomeTally();
            }

            tally.Add(row.Outcome, row.Count, row.Credits);
        }

        var consumerIds = tallies.Keys.ToList();
        var consumers = await db.Users
            .AsNoTracking()
            .Where(u => consumerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToListAsync(cancellationToken);

        return consumers
            .Select(c => new ConsumerUsageDto(c.Id, c.Name, c.Email, tallies[c.Id].ToSummary()))
            .OrderByDescending(c => c.Usage.TotalRequests)
            .ToList();
    }

    private static IQueryable<UsageRecord> InRange(IQueryable<UsageRecord> scope, ReportRange range) =>
        scope.AsNoTracking().Where(r => r.Timestamp >= range.From && r.Timestamp <= range.To);
}
