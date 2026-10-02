using System.Security.Claims;
using System.Text;
using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "api-gateway";
    public string Audience { get; set; } = "api-gateway-dashboard";

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;

    public SymmetricSecurityKey CreateSecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

public static class JwtClaims
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Role = "role";
}

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken Issue(User user)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(settings.ExpiryMinutes);

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtClaims.UserId, user.Id.ToString()),
                new Claim(JwtClaims.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            SigningCredentials = new SigningCredentials(settings.CreateSecurityKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
