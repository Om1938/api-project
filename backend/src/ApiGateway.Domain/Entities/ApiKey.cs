using ApiGateway.Domain.Enums;
using ApiGateway.Domain.Security;

namespace ApiGateway.Domain.Entities;

public sealed class ApiKey : Entity
{
    private ApiKey() { }

    public ApiKey(Guid apiId, Guid tierId, Guid consumerId, string name, ApiKeySecret secret)
    {
        ApiId = apiId;
        TierId = tierId;
        ConsumerId = consumerId;
        Name = name.Trim();
        KeyPrefix = secret.Prefix;
        KeyHash = secret.Hash;
        Status = ApiKeyStatus.Active;
    }

    public Guid ApiId { get; private set; }
    public Guid TierId { get; private set; }
    public Guid ConsumerId { get; private set; }
    public string Name { get; private set; } = null!;
    public string KeyPrefix { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public ApiKeyStatus Status { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => Status == ApiKeyStatus.Active;

    public void Revoke(DateTime utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        Status = ApiKeyStatus.Revoked;
        RevokedAt = utcNow;
    }
}
