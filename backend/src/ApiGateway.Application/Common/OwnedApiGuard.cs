using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Common;

public sealed class OwnedApiGuard(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<RegisteredApi>> FindAsync(Guid apiId, CancellationToken cancellationToken)
    {
        var api = await db.Apis.FirstOrDefaultAsync(
            a => a.Id == apiId && a.OwnerId == currentUser.UserId,
            cancellationToken);

        if (api is null)
        {
            return Error.NotFound("API");
        }

        return api;
    }
}
