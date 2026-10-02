using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    UserRole Role { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);

public interface ITokenIssuer
{
    AccessToken Issue(User user);
}
