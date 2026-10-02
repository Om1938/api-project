using ApiGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiGateway.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

internal sealed class RegisteredApiConfiguration : IEntityTypeConfiguration<RegisteredApi>
{
    public void Configure(EntityTypeBuilder<RegisteredApi> builder)
    {
        builder.ToTable("Apis");
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.TargetBaseUrl).HasMaxLength(2048).IsRequired();
        builder.HasIndex(a => a.Slug).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TierConfiguration : IEntityTypeConfiguration<Tier>
{
    public void Configure(EntityTypeBuilder<Tier> builder)
    {
        builder.ToTable("Tiers");
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => new { t.ApiId, t.Name }).IsUnique();
        builder.HasOne<RegisteredApi>().WithMany().HasForeignKey(t => t.ApiId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.Property(k => k.Name).HasMaxLength(100).IsRequired();
        builder.Property(k => k.KeyPrefix).HasMaxLength(16).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasOne<RegisteredApi>().WithMany().HasForeignKey(k => k.ApiId).OnDelete(DeleteBehavior.Cascade);

        // cascade, otherwise deleting an API fails on its own keys. TierService blocks deleting a tier in use.
        builder.HasOne<Tier>().WithMany().HasForeignKey(k => k.TierId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(k => k.ConsumerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
{
    public void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        // no foreign keys on purpose: usage history outlives the keys and APIs it points to
        builder.ToTable("UsageRecords");
        builder.Property(r => r.Method).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(UsageRecordLimits.PathMaxLength).IsRequired();
        builder.HasIndex(r => new { r.ApiId, r.Timestamp });
        builder.HasIndex(r => new { r.ConsumerId, r.Timestamp });
        builder.HasIndex(r => new { r.ApiKeyId, r.Timestamp });
    }
}

internal static class UsageRecordLimits
{
    public const int PathMaxLength = 512;
}

internal sealed class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.ToTable("CreditTransactions");
        builder.Property(t => t.Description).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => new { t.ConsumerId, t.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.ConsumerId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.Property(w => w.Url).HasMaxLength(2048).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(64).IsRequired();
        builder.HasOne<RegisteredApi>().WithMany().HasForeignKey(w => w.ApiId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.Property(d => d.EventType).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Payload).HasColumnType("text").IsRequired();
        builder.Property(d => d.Error).HasMaxLength(500);
        builder.HasIndex(d => new { d.SubscriptionId, d.CreatedAt });
        builder.HasOne<WebhookSubscription>().WithMany().HasForeignKey(d => d.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
