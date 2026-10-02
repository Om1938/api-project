using System.Linq.Expressions;
using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Gateway;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Analytics;

public sealed class UsageBreakdownBuilder(IAppDbContext db)
{
    private const int TopEndpoints = 8;
    private const int MaxDistinctPaths = 2000;

    public async Task<UsageBreakdownDto> BuildAsync(
        IQueryable<UsageRecord> records,
        IReadOnlyList<HeatmapCellDto> heatmap,
        CancellationToken cancellationToken)
    {
        return new UsageBreakdownDto(
            await CountByAsync(records, r => r.StatusCode, cancellationToken),
            await CountByAsync(records, r => r.Method, cancellationToken),
            await EndpointsAsync(records, cancellationToken),
            await LatencyBuckets.CountAsync(records.Where(UsageRules.ConsumesQuota), cancellationToken),
            await ApisAsync(records, cancellationToken),
            await TiersAsync(records, cancellationToken),
            heatmap);
    }

    private static async Task<IReadOnlyList<CountDto>> CountByAsync<TKey>(
        IQueryable<UsageRecord> records,
        Expression<Func<UsageRecord, TKey>> key,
        CancellationToken cancellationToken)
    {
        var rows = await records
            .GroupBy(key)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new CountDto(x.Key!.ToString()!, x.Count)).ToList();
    }

    private static async Task<IReadOnlyList<CountDto>> EndpointsAsync(IQueryable<UsageRecord> records, CancellationToken cancellationToken)
    {
        var paths = await records
            .GroupBy(r => r.Path)
            .Select(g => new { Path = g.Key, Count = g.LongCount() })
            .OrderByDescending(x => x.Count)
            .Take(MaxDistinctPaths)
            .ToListAsync(cancellationToken);

        return paths
            .GroupBy(x => EndpointPattern.Of(x.Path))
            .Select(g => new CountDto(g.Key, g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Requests)
            .Take(TopEndpoints)
            .ToList();
    }

    private async Task<IReadOnlyList<CountDto>> ApisAsync(IQueryable<UsageRecord> records, CancellationToken cancellationToken)
    {
        var rows = await records
            .GroupBy(r => r.ApiId)
            .Select(g => new { ApiId = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(x => x.ApiId).ToList();
        var names = await db.Apis
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        return rows
            .Select(x => new CountDto(names.GetValueOrDefault(x.ApiId, "Deleted API"), x.Count))
            .OrderByDescending(x => x.Requests)
            .ToList();
    }

    private async Task<IReadOnlyList<CountDto>> TiersAsync(IQueryable<UsageRecord> records, CancellationToken cancellationToken)
    {
        var rows = await (
            from record in records
            join key in db.ApiKeys on record.ApiKeyId equals key.Id
            join tier in db.Tiers on key.TierId equals tier.Id
            join api in db.Apis on tier.ApiId equals api.Id
            group record by new { Api = api.Name, Tier = tier.Name } into g
            select new { g.Key.Api, g.Key.Tier, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new CountDto($"{x.Api} · {x.Tier}", x.Count))
            .OrderByDescending(x => x.Requests)
            .ToList();
    }
}
