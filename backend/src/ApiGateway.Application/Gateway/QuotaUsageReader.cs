using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Gateway;

public sealed class QuotaUsageReader(IAppDbContext db) : IQuotaUsageReader
{
    public async Task<long> CountAsync(Guid apiKeyId, FixedWindow window, CancellationToken cancellationToken) =>
        await InWindow(window).LongCountAsync(r => r.ApiKeyId == apiKeyId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, long>> CountByKeyAsync(
        IReadOnlyCollection<Guid> apiKeyIds,
        FixedWindow window,
        CancellationToken cancellationToken)
    {
        if (apiKeyIds.Count == 0)
        {
            return new Dictionary<Guid, long>();
        }

        return await InWindow(window)
            .Where(r => apiKeyIds.Contains(r.ApiKeyId))
            .GroupBy(r => r.ApiKeyId)
            .Select(g => new { ApiKeyId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.ApiKeyId, x => x.Count, cancellationToken);
    }

    private IQueryable<UsageRecord> InWindow(FixedWindow window)
    {
        var start = window.Start.UtcDateTime;
        var end = window.End.UtcDateTime;

        return db.UsageRecords
            .AsNoTracking()
            .Where(UsageRules.ConsumesQuota)
            .Where(r => r.Timestamp >= start && r.Timestamp < end);
    }
}
