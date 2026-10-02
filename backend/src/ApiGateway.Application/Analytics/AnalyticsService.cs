using ApiGateway.Application.Abstractions;
using ApiGateway.Application.Common;

namespace ApiGateway.Application.Analytics;

public sealed class AnalyticsService(
    IAppDbContext db,
    ICurrentUser currentUser,
    OwnedApiGuard guard,
    UsageAggregator aggregator,
    TimeProvider timeProvider)
{
    public async Task<Result<OwnerAnalyticsDto>> GetAsync(
        Guid? apiId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? bucketMinutes,
        CancellationToken cancellationToken)
    {
        if (apiId is not null)
        {
            var api = await guard.FindAsync(apiId.Value, cancellationToken);
            if (!api.IsSuccess)
            {
                return api.Error!;
            }
        }

        var ownedApiIds = db.Apis.Where(a => a.OwnerId == currentUser.UserId).Select(a => a.Id);
        var scope = db.UsageRecords.Where(r => ownedApiIds.Contains(r.ApiId));
        if (apiId is not null)
        {
            scope = scope.Where(r => r.ApiId == apiId);
        }

        var range = ReportRange.Resolve(from, to, timeProvider.GetUtcNow(), bucketMinutes);
        var report = await aggregator.BuildReportAsync(scope, range, cancellationToken);
        var consumers = await aggregator.BuildConsumerBreakdownAsync(scope, range, cancellationToken);

        return new OwnerAnalyticsDto(report, consumers);
    }
}
