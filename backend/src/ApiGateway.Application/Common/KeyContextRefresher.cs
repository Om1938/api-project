using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Common;

public sealed class KeyContextRefresher(IAppDbContext db, IKeyContextInvalidator invalidator)
{
    public Task ForApiAsync(Guid apiId, CancellationToken cancellationToken) =>
        EvictAsync(db.ApiKeys.Where(k => k.ApiId == apiId), cancellationToken);

    public Task ForTierAsync(Guid tierId, CancellationToken cancellationToken) =>
        EvictAsync(db.ApiKeys.Where(k => k.TierId == tierId), cancellationToken);

    public Task ForKeyAsync(ApiKey key, CancellationToken cancellationToken) =>
        invalidator.InvalidateAsync([key.KeyHash], cancellationToken);

    private async Task EvictAsync(IQueryable<ApiKey> keys, CancellationToken cancellationToken)
    {
        var hashes = await keys.Select(k => k.KeyHash).ToListAsync(cancellationToken);
        if (hashes.Count > 0)
        {
            await invalidator.InvalidateAsync(hashes, cancellationToken);
        }
    }
}
