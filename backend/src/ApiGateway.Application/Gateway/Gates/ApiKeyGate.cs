using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Security;

namespace ApiGateway.Application.Gateway.Gates;

public sealed class ApiKeyGate(IKeyContextResolver resolver) : IRequestGate
{
    public async Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.PresentedKey))
        {
            return Deny(401, "missing_api_key", "Send your API key in the X-API-Key header.");
        }

        var key = await resolver.ResolveAsync(ApiKeySecret.ComputeHash(context.PresentedKey.Trim()), cancellationToken);
        if (key is null)
        {
            return Deny(401, "invalid_api_key", "The API key is not recognised.");
        }

        if (!key.KeyIsActive)
        {
            return Deny(401, "revoked_api_key", "The API key has been revoked.");
        }

        if (!string.Equals(key.ApiSlug, context.ApiSlug, StringComparison.OrdinalIgnoreCase))
        {
            return Deny(403, "key_not_valid_for_api", "The API key does not grant access to this API.");
        }

        if (!key.ApiIsActive)
        {
            return Deny(503, "api_inactive", "This API is currently disabled by its owner.");
        }

        context.Key = key;
        return GateResult.Allow;
    }

    private static GateResult Deny(int status, string code, string message) =>
        GateResult.Deny(new GateRejection(status, code, message));
}
