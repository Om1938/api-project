using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Billing;

public sealed class CreditService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICreditLedger ledger,
    TimeProvider timeProvider)
{
    private const int MaxTransactionsListed = 20;

    public async Task<Result<CreditAccountDto>> GetAccountAsync(CancellationToken cancellationToken)
    {
        var consumerId = currentUser.UserId;
        var month = FixedWindow.Month(timeProvider.GetUtcNow());
        var monthStart = month.Start.UtcDateTime;

        var balance = await ledger.GetBalanceAsync(consumerId, cancellationToken);

        var spent = await db.UsageRecords
            .Where(r => r.ConsumerId == consumerId && r.Timestamp >= monthStart)
            .SumAsync(r => r.CreditsCharged, cancellationToken);

        var transactions = await db.CreditTransactions
            .AsNoTracking()
            .Where(t => t.ConsumerId == consumerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(MaxTransactionsListed)
            .Select(t => new CreditTransactionDto(t.Id, t.Amount, t.Type, t.Description, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new CreditAccountDto(balance, spent, transactions);
    }

    public async Task<Result<CreditAccountDto>> TopUpAsync(TopUpRequest request, CancellationToken cancellationToken)
    {
        var consumerId = currentUser.UserId;

        await ledger.DepositAsync(consumerId, request.Amount, cancellationToken);
        db.CreditTransactions.Add(new CreditTransaction(
            consumerId, request.Amount, CreditTransactionType.TopUp, "Credit top-up (mock payment)"));
        await db.SaveChangesAsync(cancellationToken);

        return await GetAccountAsync(cancellationToken);
    }
}
