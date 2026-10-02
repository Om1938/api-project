using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway.Gates;

public sealed class CreditGate(ICreditLedger ledger) : IRequestGate
{
    public async Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken)
    {
        var key = context.Key!;
        if (key.CreditCostPerRequest <= 0)
        {
            return GateResult.Allow;
        }

        var balance = await ledger.GetBalanceAsync(key.ConsumerId, cancellationToken);

        return balance >= key.CreditCostPerRequest
            ? GateResult.Allow
            : GateResult.Deny(new GateRejection(
                402,
                "insufficient_credits",
                $"This request costs {key.CreditCostPerRequest:0.####} credits but the balance is {balance:0.####}.",
                RequestOutcome.InsufficientCredits));
    }
}
