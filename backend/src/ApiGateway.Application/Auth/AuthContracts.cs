using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using FluentValidation;

namespace ApiGateway.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password, UserRole Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Name, string Email, UserRole Role, decimal CreditBalance)
{
    public static UserDto From(User user) => new(user.Id, user.Name, user.Email, user.Role, user.CreditBalance);
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).DisplayName();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
