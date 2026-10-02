using System.Text.Json;
using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Application.Gateway;
using ApiGateway.Domain.Enums;
using ApiGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ApiGateway.Infrastructure.Gateway;

internal sealed class EfKeyContextResolver(AppDbContext db) : IKeyContextResolver
{
    public async Task<KeyContext?> ResolveAsync(string keyHash, CancellationToken cancellationToken)
    {
        var row = await (
            from key in db.ApiKeys.AsNoTracking()
            join api in db.Apis on key.ApiId equals api.Id
            join tier in db.Tiers on key.TierId equals tier.Id
            where key.KeyHash == keyHash
            select new { Key = key, Api = api, Tier = tier })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var thresholds = await db.WebhookSubscriptions
            .Where(w => w.ApiId == row.Api.Id && w.IsActive && w.Events.HasFlag(WebhookEvents.QuotaThresholdReached))
            .Select(w => w.ThresholdPercent)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return new KeyContext(
            row.Key.Id,
            row.Key.IsActive,
            row.Key.ConsumerId,
            row.Api.Id,
            row.Api.Slug,
            row.Api.IsActive,
            row.Api.TargetBaseUrl,
            row.Tier.RequestsPerMinute,
            row.Tier.MonthlyQuota,
            row.Tier.CreditCostPerRequest,
            thresholds);
    }
}

internal sealed class CachedKeyContextResolver(EfKeyContextResolver inner, KeyContextCache cache) : IKeyContextResolver
{
    public async Task<KeyContext?> ResolveAsync(string keyHash, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(keyHash);
        if (cached.Found)
        {
            return cached.Context;
        }

        var context = await inner.ResolveAsync(keyHash, cancellationToken);
        await cache.SetAsync(keyHash, context);
        return context;
    }
}

internal sealed class KeyContextCache(IConnectionMultiplexer redis) : IKeyContextInvalidator
{
    private const string UnknownKeyMarker = "-";
    private static readonly TimeSpan KnownKeyTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan UnknownKeyTtl = TimeSpan.FromSeconds(15);

    public async Task<(bool Found, KeyContext? Context)> GetAsync(string keyHash)
    {
        var value = await redis.GetDatabase().StringGetAsync(StoreKeys.KeyContext(keyHash));
        if (value.IsNullOrEmpty)
        {
            return (false, null);
        }

        return value == UnknownKeyMarker
            ? (true, null)
            : (true, JsonSerializer.Deserialize<KeyContext>((string)value!));
    }

    public Task SetAsync(string keyHash, KeyContext? context) =>
        context is null
            ? redis.GetDatabase().StringSetAsync(StoreKeys.KeyContext(keyHash), UnknownKeyMarker, UnknownKeyTtl)
            : redis.GetDatabase().StringSetAsync(StoreKeys.KeyContext(keyHash), JsonSerializer.Serialize(context), KnownKeyTtl);

    public Task InvalidateAsync(IEnumerable<string> keyHashes, CancellationToken cancellationToken) =>
        redis.GetDatabase().KeyDeleteAsync(keyHashes.Select(hash => (RedisKey)StoreKeys.KeyContext(hash)).ToArray());
}
