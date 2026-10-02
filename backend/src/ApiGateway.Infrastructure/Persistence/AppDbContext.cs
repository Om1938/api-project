using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApiGateway.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RegisteredApi> Apis => Set<RegisteredApi>();
    public DbSet<Tier> Tiers => Set<Tier>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampCreatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampCreatedAt();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // MySQL DATETIME has no kind; everything we store is UTC
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    private void StampCreatedAt()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond)); // DATETIME(6) precision
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
