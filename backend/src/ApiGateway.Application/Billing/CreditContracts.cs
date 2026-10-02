using ApiGateway.Domain.Enums;
using FluentValidation;

namespace ApiGateway.Application.Billing;

public sealed record TopUpRequest(decimal Amount);

public sealed record CreditTransactionDto(Guid Id, decimal Amount, CreditTransactionType Type, string Description, DateTime CreatedAt);

public sealed record CreditAccountDto(
    decimal Balance,
    decimal SpentThisMonth,
    IReadOnlyList<CreditTransactionDto> Transactions);

public sealed class TopUpRequestValidator : AbstractValidator<TopUpRequest>
{
    public TopUpRequestValidator()
    {
        RuleFor(x => x.Amount).InclusiveBetween(CreditPolicy.MinTopUp, CreditPolicy.MaxTopUp);
    }
}
