using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;
using FluentValidation;

namespace ApiGateway.Application.Keys;

public sealed record CreateApiKeyRequest(string Name, Guid TierId, string ConsumerEmail);

public sealed record ApiKeyDto(
    Guid Id,
    Guid ApiId,
    string Name,
    string KeyPrefix,
    ApiKeyStatus Status,
    Guid TierId,
    string TierName,
    Guid ConsumerId,
    string ConsumerName,
    string ConsumerEmail,
    DateTime CreatedAt,
    DateTime? RevokedAt);

public sealed record CreatedApiKeyDto(ApiKeyDto Key, string Secret);

public sealed class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name).DisplayName();
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.ConsumerEmail).NotEmpty().EmailAddress();
    }
}
