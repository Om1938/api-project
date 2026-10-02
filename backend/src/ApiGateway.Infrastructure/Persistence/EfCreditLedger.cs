using ApiGateway.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Infrastructure.Persistence;

internal sealed class EfCreditLedger(AppDbContext db) : ICreditLedger
{
    public Task<decimal> GetBalanceAsync(Guid consumerId, CancellationToken cancellationToken) =>
        db.Users
            .Where(u => u.Id == consumerId)
            .Select(u => u.CreditBalance)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryChargeAsync(Guid consumerId, decimal amount, CancellationToken cancellationToken)
    {
        var updated = await db.Users
            .Where(u => u.Id == consumerId && u.CreditBalance >= amount)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance - amount),
                cancellationToken);

        return updated == 1;
    }

    public Task DepositAsync(Guid consumerId, decimal amount, CancellationToken cancellationToken) =>
        db.Users
            .Where(u => u.Id == consumerId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance + amount),
                cancellationToken);
}
