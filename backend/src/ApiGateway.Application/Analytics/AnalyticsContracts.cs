namespace ApiGateway.Application.Analytics;

public sealed record UsageSummaryDto(
    long TotalRequests,
    long Successful,
    long Failed,
    long RateLimited,
    long QuotaExceeded,
    long InsufficientCredits,
    decimal CreditsConsumed);

public sealed record UsagePointDto(DateTime BucketStart, UsageSummaryDto Usage, double AverageLatencyMs);

public sealed record CountDto(string Label, long Requests);

public sealed record HeatmapCellDto(DayOfWeek DayOfWeek, int Hour, long Requests);

public sealed record UsageBreakdownDto(
    IReadOnlyList<CountDto> StatusCodes,
    IReadOnlyList<CountDto> Methods,
    IReadOnlyList<CountDto> Endpoints,
    IReadOnlyList<CountDto> LatencyBuckets,
    IReadOnlyList<CountDto> Apis,
    IReadOnlyList<CountDto> Tiers,
    IReadOnlyList<HeatmapCellDto> Heatmap);

public sealed record UsageReportDto(
    DateTime From,
    DateTime To,
    int BucketMinutes,
    UsageSummaryDto Summary,
    double AverageLatencyMs,
    IReadOnlyList<UsagePointDto> Series,
    UsageBreakdownDto Breakdown);

public sealed record ConsumerUsageDto(Guid ConsumerId, string Name, string Email, UsageSummaryDto Usage);

public sealed record OwnerAnalyticsDto(UsageReportDto Report, IReadOnlyList<ConsumerUsageDto> Consumers);
