using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Tiers;

public sealed class TierService(IAppDbContext db, OwnedApiGuard guard, KeyContextRefresher keyContexts)
{
    public async Task<Result<IReadOnlyList<TierDto>>> ListAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var tiers = await db.Tiers
            .AsNoTracking()
            .Where(t => t.ApiId == apiId)
            .OrderBy(t => t.RequestsPerMinute)
            .ToListAsync(cancellationToken);

        return tiers.Select(TierDto.From).ToList();
    }

    public async Task<Result<TierDto>> CreateAsync(Guid apiId, TierRequest request, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        if (await NameIsTakenAsync(apiId, request.Name, exceptTierId: null, cancellationToken))
        {
            return NameTaken(request.Name);
        }

        var tier = new Tier(apiId, request.Name, request.RequestsPerMinute, request.MonthlyQuota, request.CreditCostPerRequest);
        db.Tiers.Add(tier);
        await db.SaveChangesAsync(cancellationToken);

        return TierDto.From(tier);
    }

    public async Task<Result<TierDto>> UpdateAsync(Guid apiId, Guid tierId, TierRequest request, CancellationToken cancellationToken)
    {
        var tier = await FindAsync(apiId, tierId, cancellationToken);
        if (!tier.IsSuccess)
        {
            return tier.Error!;
        }

        if (await NameIsTakenAsync(apiId, request.Name, tierId, cancellationToken))
        {
            return NameTaken(request.Name);
        }

        tier.Value.Update(request.Name, request.RequestsPerMinute, request.MonthlyQuota, request.CreditCostPerRequest);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForTierAsync(tierId, cancellationToken);

        return TierDto.From(tier.Value);
    }

    public async Task<Result> DeleteAsync(Guid apiId, Guid tierId, CancellationToken cancellationToken)
    {
        var tier = await FindAsync(apiId, tierId, cancellationToken);
        if (!tier.IsSuccess)
        {
            return tier.Error!;
        }

        if (await db.ApiKeys.AnyAsync(k => k.TierId == tierId, cancellationToken))
        {
            return Error.Conflict("tier_in_use", "This tier still has API keys assigned to it.");
        }

        db.Tiers.Remove(tier.Value);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result<Tier>> FindAsync(Guid apiId, Guid tierId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        var tier = await db.Tiers.FirstOrDefaultAsync(t => t.Id == tierId && t.ApiId == apiId, cancellationToken);
        if (tier is null)
        {
            return Error.NotFound("Tier");
        }

        return tier;
    }

    private Task<bool> NameIsTakenAsync(Guid apiId, string name, Guid? exceptTierId, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        return db.Tiers.AnyAsync(t => t.ApiId == apiId && t.Name == trimmed && t.Id != exceptTierId, cancellationToken);
    }

    private static Error NameTaken(string name) =>
        Error.Conflict("tier_name_taken", $"This API already has a tier named '{name.Trim()}'.");
}
