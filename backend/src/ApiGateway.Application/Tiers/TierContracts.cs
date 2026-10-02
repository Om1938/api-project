using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using FluentValidation;

namespace ApiGateway.Application.Tiers;

public sealed record TierRequest(string Name, int RequestsPerMinute, int MonthlyQuota, decimal CreditCostPerRequest);

public sealed record TierDto(
    Guid Id,
    Guid ApiId,
    string Name,
    int RequestsPerMinute,
    int MonthlyQuota,
    decimal CreditCostPerRequest,
    DateTime CreatedAt)
{
    public static TierDto From(Tier tier) => new(
        tier.Id, tier.ApiId, tier.Name, tier.RequestsPerMinute, tier.MonthlyQuota, tier.CreditCostPerRequest, tier.CreatedAt);
}

public sealed class TierRequestValidator : AbstractValidator<TierRequest>
{
    public TierRequestValidator()
    {
        RuleFor(x => x.Name).DisplayName();
        RuleFor(x => x.RequestsPerMinute).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.MonthlyQuota).InclusiveBetween(1, 1_000_000_000);
        RuleFor(x => x.CreditCostPerRequest).InclusiveBetween(0m, 1_000_000m);
    }
}
