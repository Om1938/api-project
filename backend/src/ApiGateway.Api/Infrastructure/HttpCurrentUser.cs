using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Enums;
using ApiGateway.Infrastructure.Security;

namespace ApiGateway.Api.Infrastructure;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId => Guid.Parse(Claim(JwtClaims.UserId));

    public UserRole Role => Enum.Parse<UserRole>(Claim(JwtClaims.Role));

    private string Claim(string type) =>
        accessor.HttpContext?.User.FindFirst(type)?.Value
        ?? throw new InvalidOperationException("No authenticated user on the current request.");
}
