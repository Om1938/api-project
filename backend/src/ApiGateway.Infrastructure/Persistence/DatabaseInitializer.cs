using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGateway.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string Section = "Seed";

    public bool Enabled { get; set; }
    public string OwnerEmail { get; set; } = "owner@demo.local";
    public string ConsumerEmail { get; set; } = "consumer@demo.local";
    public string Password { get; set; } = "Password123!";
    public decimal ConsumerCredits { get; set; } = 100m;
    public string UpstreamUrl { get; set; } = "http://upstream:8080";

    public int SampleHistoryDays { get; set; } // days of generated usage; 0 = none
}

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await db.Database.MigrateAsync(cancellationToken);

        var seed = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (!seed.Enabled || await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = new User(seed.OwnerEmail, "Demo Owner", UserRole.Owner);
        owner.SetPasswordHash(hasher.Hash(seed.Password));

        var consumer = new User(seed.ConsumerEmail, "Demo Consumer", UserRole.Consumer);
        consumer.SetPasswordHash(hasher.Hash(seed.Password));
        consumer.AddCredits(seed.ConsumerCredits);

        var api = new RegisteredApi(
            owner.Id, "HTTPBin Demo", "httpbin", "Echo service used to demonstrate the gateway.", seed.UpstreamUrl);

        db.Users.AddRange(owner, consumer);
        db.CreditTransactions.Add(new CreditTransaction(
            consumer.Id, seed.ConsumerCredits, CreditTransactionType.TopUp, "Demo starting balance"));
        db.Apis.Add(api);
        var free = new Tier(api.Id, "Free", requestsPerMinute: 5, monthlyQuota: 100, creditCostPerRequest: 1m);
        var pro = new Tier(api.Id, "Pro", requestsPerMinute: 60, monthlyQuota: 10_000, creditCostPerRequest: 0.5m);
        db.Tiers.AddRange(free, pro);

        if (seed.SampleHistoryDays > 0)
        {
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
            SampleHistory.Add(db, hasher, seed, api, free, pro, consumer, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded demo data: {Owner} / {Consumer}, {Days} days of sample usage",
            seed.OwnerEmail, seed.ConsumerEmail, seed.SampleHistoryDays);
    }
}
