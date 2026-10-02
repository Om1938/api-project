using ApiGateway.Domain.Enums;

namespace ApiGateway.Domain.Entities;

public sealed class User : Entity
{
    private User() { }

    public User(string email, string name, UserRole role)
    {
        Email = NormalizeEmail(email);
        Name = name.Trim();
        Role = role;
    }

    public string Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    public decimal CreditBalance { get; private set; }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void AddCredits(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CreditBalance += amount;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
