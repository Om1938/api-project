using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Apis;

public sealed class ApiService(
    IAppDbContext db,
    ICurrentUser currentUser,
    OwnedApiGuard guard,
    KeyContextRefresher keyContexts)
{
    public async Task<Result<IReadOnlyList<ApiDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var apis = await db.Apis
            .AsNoTracking()
            .Where(a => a.OwnerId == currentUser.UserId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return apis.Select(ApiDto.From).ToList();
    }

    public async Task<Result<ApiDto>> GetAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        return ApiDto.From(api.Value);
    }

    public async Task<Result<ApiDto>> CreateAsync(CreateApiRequest request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrEmpty(request.Slug) ? Slugs.From(request.Name) : request.Slug;
        if (await db.Apis.AnyAsync(a => a.Slug == slug, cancellationToken))
        {
            return Error.Conflict("slug_taken", $"The slug '{slug}' is already in use. Choose another one.");
        }

        var api = new RegisteredApi(currentUser.UserId, request.Name, slug, request.Description, request.TargetBaseUrl);
        db.Apis.Add(api);
        await db.SaveChangesAsync(cancellationToken);

        return ApiDto.From(api);
    }

    public async Task<Result<ApiDto>> UpdateAsync(Guid apiId, UpdateApiRequest request, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        api.Value.Update(request.Name, request.Description, request.TargetBaseUrl, request.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        await keyContexts.ForApiAsync(apiId, cancellationToken);

        return ApiDto.From(api.Value);
    }

    public async Task<Result> DeleteAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await guard.FindAsync(apiId, cancellationToken);
        if (!api.IsSuccess)
        {
            return api.Error!;
        }

        // evict first: the key hashes are gone once the API is deleted
        await keyContexts.ForApiAsync(apiId, cancellationToken);
        db.Apis.Remove(api.Value);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
