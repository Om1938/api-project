using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ApiGateway.Infrastructure.Security;

internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        Hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
