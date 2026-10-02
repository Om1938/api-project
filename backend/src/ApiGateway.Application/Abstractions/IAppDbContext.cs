using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RegisteredApi> Apis { get; }
    DbSet<Tier> Tiers { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<UsageRecord> UsageRecords { get; }
    DbSet<CreditTransaction> CreditTransactions { get; }
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<WebhookDelivery> WebhookDeliveries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
