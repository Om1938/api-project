using System.Security.Cryptography;
using System.Text;

namespace ApiGateway.Domain.Security;

public sealed record ApiKeySecret(string Plaintext, string Prefix, string Hash)
{
    public const string Scheme = "gw_";
    private const int RandomBytes = 32;
    private const int PrefixLength = 11; // "gw_" + 8 chars

    public static ApiKeySecret Generate()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RandomBytes))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var plaintext = Scheme + random;

        return new ApiKeySecret(plaintext, plaintext[..PrefixLength], ComputeHash(plaintext));
    }

    public static string ComputeHash(string plaintext) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
}
