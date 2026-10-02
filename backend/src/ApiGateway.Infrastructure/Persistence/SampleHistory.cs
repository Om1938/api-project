using ApiGateway.Application.Abstractions;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;
using ApiGateway.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Infrastructure.Persistence;

internal static class SampleHistory
{
    private const int RandomSeed = 20260101;
    private const double RequestsPerHourAtPeak = 14;
    private const double MedianLatencyMs = 42;

    private sealed record Profile(string Name, string Email, string KeyName, bool Pro, double Weight);

    private static readonly Profile[] Consumers =
    [
        new("Northwind Traders", "northwind@demo.local", "Mobile app", Pro: true, Weight: 1.6),
        new("Contoso Retail", "contoso@demo.local", "Storefront", Pro: true, Weight: 0.8),
        new("Fabrikam Labs", "fabrikam@demo.local", "Prototype", Pro: false, Weight: 0.15),
    ];

    private static readonly (int Weight, string Method, string Path)[] Requests =
    [
        (30, "GET", "/get"),
        (14, "GET", "/anything/users/{0}"),
        (12, "GET", "/anything/orders/{0}"),
        (12, "POST", "/post"),
        (6, "PUT", "/anything/orders/{0}"),
        (4, "DELETE", "/anything/orders/{0}"),
        (6, "GET", "/uuid"),
        (4, "GET", "/headers"),
    ];

    public static void Add(
        AppDbContext db,
        IPasswordHasher hasher,
        SeedOptions seed,
        RegisteredApi api,
        Tier free,
        Tier pro,
        User demoConsumer,
        DateTime now)
    {
        var start = now.Date.AddDays(-seed.SampleHistoryDays);
        var random = new Random(RandomSeed);
        var keys = new List<(ApiKey Key, Tier Tier, Profile Profile)>
        {
            NewKey(demoConsumer, new Profile(demoConsumer.Name, demoConsumer.Email, "Reporting job", Pro: true, Weight: 1.0)),
        };

        foreach (var profile in Consumers)
        {
            var user = new User(profile.Email, profile.Name, UserRole.Consumer);
            user.SetPasswordHash(hasher.Hash(seed.Password));
            user.AddCredits(seed.ConsumerCredits);
            db.Users.Add(user);
            db.CreditTransactions.Add(new CreditTransaction(
                user.Id, seed.ConsumerCredits, CreditTransactionType.TopUp, "Demo starting balance"));
            keys.Add(NewKey(user, profile));
        }

        db.ApiKeys.AddRange(keys.Select(k => k.Key));

        // an afternoon where the upstream misbehaved, so the success-rate and latency charts have a story
        var incidentDay = start.AddDays(seed.SampleHistoryDays * 2 / 3);
        var forwardedPerMonth = new Dictionary<(Guid Key, int Month), int>();
        var spent = new Dictionary<Guid, decimal>();

        for (var hour = start; hour < now; hour = hour.AddHours(1))
        {
            var activity = Activity(hour);
            var growth = 0.7 + 0.45 * (hour - start).TotalDays / seed.SampleHistoryDays;
            var incident = hour.Date == incidentDay && hour.Hour is >= 13 and <= 15;

            foreach (var (key, tier, profile) in keys)
            {
                var count = (int)Math.Round(RequestsPerHourAtPeak * profile.Weight * activity * growth * (0.75 + random.NextDouble() * 0.5));

                for (var i = 0; i < count; i++)
                {
                    var timestamp = hour.AddSeconds(random.Next(3600));
                    if (timestamp >= now)
                    {
                        continue;
                    }

                    var month = (key.Id, timestamp.Year * 100 + timestamp.Month);
                    var quotaLeft = forwardedPerMonth.GetValueOrDefault(month) < tier.MonthlyQuota;
                    var outcome = PickOutcome(random, profile.Pro, activity, incident, quotaLeft);
                    if (outcome is RequestOutcome.Success or RequestOutcome.Failed)
                    {
                        forwardedPerMonth[month] = forwardedPerMonth.GetValueOrDefault(month) + 1;
                    }

                    var record = NewRecord(random, key, tier, timestamp, outcome, incident);
                    spent[key.ConsumerId] = spent.GetValueOrDefault(key.ConsumerId) + record.CreditsCharged;
                    db.UsageRecords.Add(record);
                }
            }
        }

        // keep the ledger consistent: what was spent must have been topped up at some point
        foreach (var (consumerId, amount) in spent.Where(pair => pair.Value > 0))
        {
            db.CreditTransactions.Add(new CreditTransaction(
                consumerId, amount, CreditTransactionType.TopUp, "Earlier top-ups (sample data)"));
        }

        foreach (var entry in db.ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.CreatedAt = start;
        }

        (ApiKey, Tier, Profile) NewKey(User user, Profile profile)
        {
            var tier = profile.Pro ? pro : free;
            return (new ApiKey(api.Id, tier.Id, user.Id, profile.KeyName, ApiKeySecret.Generate()), tier, profile);
        }
    }

    // daytime peak, quiet nights, half the traffic at weekends
    private static double Activity(DateTime hour)
    {
        var daytime = hour.Hour is >= 5 and <= 21 ? Math.Sin(Math.PI * (hour.Hour - 5) / 16.0) : 0;
        var weekend = hour.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.5 : 1;
        return (0.25 + 0.75 * daytime) * weekend;
    }

    private static RequestOutcome PickOutcome(Random random, bool pro, double activity, bool incident, bool quotaLeft)
    {
        var roll = random.NextDouble();
        var rateLimited = pro ? 0.02 + 0.05 * activity : 0.15;

        if (roll < rateLimited)
        {
            return RequestOutcome.RateLimited;
        }

        if (roll < rateLimited + 0.002)
        {
            return RequestOutcome.InsufficientCredits;
        }

        if (!quotaLeft)
        {
            return RequestOutcome.QuotaExceeded;
        }

        return random.NextDouble() < (incident ? 0.35 : 0.04) ? RequestOutcome.Failed : RequestOutcome.Success;
    }

    private static UsageRecord NewRecord(Random random, ApiKey key, Tier tier, DateTime timestamp, RequestOutcome outcome, bool incident)
    {
        var (method, path) = PickRequest(random);
        var forwarded = outcome is RequestOutcome.Success or RequestOutcome.Failed;

        return new UsageRecord
        {
            ApiKeyId = key.Id,
            ApiId = key.ApiId,
            ConsumerId = key.ConsumerId,
            Timestamp = timestamp,
            Method = method,
            Path = path,
            Outcome = outcome,
            StatusCode = outcome switch
            {
                RequestOutcome.Success => 200,
                RequestOutcome.Failed => incident || random.NextDouble() < 0.7 ? (random.NextDouble() < 0.6 ? 500 : 503) : 404,
                RequestOutcome.InsufficientCredits => 402,
                _ => 429,
            },
            LatencyMs = forwarded ? Latency(random, incident) : 0,
            CreditsCharged = outcome == RequestOutcome.Success ? tier.CreditCostPerRequest : 0,
        };
    }

    private static (string Method, string Path) PickRequest(Random random)
    {
        var roll = random.Next(Requests.Sum(r => r.Weight));
        foreach (var (weight, method, path) in Requests)
        {
            if (roll < weight)
            {
                return (method, string.Format(path, random.Next(1, 500)));
            }

            roll -= weight;
        }

        throw new InvalidOperationException("Request weights do not add up.");
    }

    private static int Latency(Random random, bool incident)
    {
        // log-normal: Box-Muller for the normal sample
        var normal = Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        var latency = MedianLatencyMs * Math.Exp(0.55 * normal);

        if (incident)
        {
            latency *= 5;
        }

        if (random.NextDouble() < 0.02)
        {
            latency *= 8;
        }

        return (int)Math.Min(latency, 5000);
    }
}
