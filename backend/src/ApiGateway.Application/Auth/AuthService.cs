using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Billing;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Auth;

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    ICurrentUser currentUser)
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Error.Conflict("email_taken", "An account with this email already exists.");
        }

        var user = new User(email, request.Name, request.Role);
        user.SetPasswordHash(passwordHasher.Hash(request.Password));

        if (user.Role == UserRole.Consumer)
        {
            user.AddCredits(CreditPolicy.WelcomeBonus);
            db.CreditTransactions.Add(new CreditTransaction(
                user.Id, CreditPolicy.WelcomeBonus, CreditTransactionType.TopUp, "Welcome bonus"));
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Error.Unauthorized("invalid_credentials", "Email or password is incorrect.");
        }

        return ToResponse(user);
    }

    public async Task<Result<UserDto>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized("unknown_user", "The account no longer exists.");
        }

        return UserDto.From(user);
    }

    private AuthResponse ToResponse(User user)
    {
        var token = tokenIssuer.Issue(user);
        return new AuthResponse(token.Value, token.ExpiresAt, UserDto.From(user));
    }
}
