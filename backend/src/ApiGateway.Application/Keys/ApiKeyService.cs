using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using ApiGateway.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Keys;

public sealed class ApiKeyService(
    IAppDbContext db,
    OwnedApiGuard guard,
    KeyContextRefresher keyContexts,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<ApiKeyDto>>> ListAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        return await Project(db.ApiKeys.Where(k => k.ApiId == apiId)).ToListAsync(cancellationToken);
    }

    public async Task<Result<CreatedApiKeyDto>> CreateAsync(Guid apiId, CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        if (!await db.Tiers.AnyAsync(t => t.Id == request.TierId && t.ApiId == apiId, cancellationToken))
        {
            return Error.Validation("unknown_tier", "The tier does not belong to this API.");
        }

        var email = User.NormalizeEmail(request.ConsumerEmail);
        var consumerId = await db.Users
            .Where(u => u.Email == email && u.Role == UserRole.Consumer)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (consumerId is null)
        {
            return Error.Validation("unknown_consumer", $"No consumer account is registered with '{email}'.");
        }

        var secret = ApiKeySecret.Generate();
        var key = new ApiKey(apiId, request.TierId, consumerId.Value, request.Name, secret);
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await Project(db.ApiKeys.Where(k => k.Id == key.Id)).SingleAsync(cancellationToken);
        return new CreatedApiKeyDto(dto, secret.Plaintext);
    }

    public async Task<Result<ApiKeyDto>> RevokeAsync(Guid apiId, Guid keyId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.ApiId == apiId, cancellationToken);
        if (key is null)
        {
            return Error.NotFound("API key");
        }

        key.Revoke(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForKeyAsync(key, cancellationToken);

        return await Project(db.ApiKeys.Where(k => k.Id == keyId)).SingleAsync(cancellationToken);
    }

    private IQueryable<ApiKeyDto> Project(IQueryable<ApiKey> keys) =>
        from key in keys.AsNoTracking()
        join tier in db.Tiers on key.TierId equals tier.Id
        join consumer in db.Users on key.ConsumerId equals consumer.Id
        orderby key.CreatedAt descending
        select new ApiKeyDto(
            key.Id,
            key.ApiId,
            key.Name,
            key.KeyPrefix,
            key.Status,
            tier.Id,
            tier.Name,
            consumer.Id,
            consumer.Name,
            consumer.Email,
            key.CreatedAt,
            key.RevokedAt);
}
