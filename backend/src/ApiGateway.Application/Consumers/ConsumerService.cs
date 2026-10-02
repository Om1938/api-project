using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Consumers;

public sealed record ConsumerDto(Guid Id, string Name, string Email, int ActiveKeyCount, DateTime CreatedAt);

public sealed class ConsumerService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<IReadOnlyList<ConsumerDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var ownedApiIds = db.Apis.Where(a => a.OwnerId == currentUser.UserId).Select(a => a.Id);

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Consumer)
            .OrderBy(u => u.Name)
            .Select(u => new ConsumerDto(
                u.Id,
                u.Name,
                u.Email,
                db.ApiKeys.Count(k => k.ConsumerId == u.Id
                    && k.Status == ApiKeyStatus.Active
                    && ownedApiIds.Contains(k.ApiId)),
                u.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
