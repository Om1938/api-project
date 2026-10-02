using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Analytics;
using ApiGateway.Application.Common;
using ApiGateway.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Application.Consumers;

public sealed record MyKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    ApiKeyStatus Status,
    string ApiName,
    string ApiSlug,
    string TierName,
    int RequestsPerMinute,
    int MonthlyQuota,
    decimal CreditCostPerRequest,
    long QuotaUsed,
    long QuotaRemaining,
    DateTime CreatedAt);

public sealed class ConsumerPortalService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IQuotaUsageReader quotaUsage,
    UsageAggregator aggregator,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<MyKeyDto>>> ListKeysAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from key in db.ApiKeys.AsNoTracking()
            join api in db.Apis on key.ApiId equals api.Id
            join tier in db.Tiers on key.TierId equals tier.Id
            where key.ConsumerId == currentUser.UserId
            orderby key.CreatedAt descending
            select new { Key = key, ApiName = api.Name, ApiSlug = api.Slug, Tier = tier })
            .ToListAsync(cancellationToken);

        var month = FixedWindow.Month(timeProvider.GetUtcNow());
        var usedByKey = await quotaUsage.CountByKeyAsync(rows.Select(r => r.Key.Id).ToList(), month, cancellationToken);

        return rows
            .Select(r =>
            {
                var used = usedByKey.GetValueOrDefault(r.Key.Id);
                return new MyKeyDto(
                    r.Key.Id,
                    r.Key.Name,
                    r.Key.KeyPrefix,
                    r.Key.Status,
                    r.ApiName,
                    r.ApiSlug,
                    r.Tier.Name,
                    r.Tier.RequestsPerMinute,
                    r.Tier.MonthlyQuota,
                    r.Tier.CreditCostPerRequest,
                    used,
                    Math.Max(0, r.Tier.MonthlyQuota - used),
                    r.Key.CreatedAt);
            })
            .ToList();
    }

    public async Task<Result<UsageReportDto>> GetUsageAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? bucketMinutes,
        CancellationToken cancellationToken)
    {
        var scope = db.UsageRecords.Where(r => r.ConsumerId == currentUser.UserId);
        var range = ReportRange.Resolve(from, to, timeProvider.GetUtcNow(), bucketMinutes);

        return await aggregator.BuildReportAsync(scope, range, cancellationToken);
    }
}
