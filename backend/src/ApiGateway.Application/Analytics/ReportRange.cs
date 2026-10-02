namespace ApiGateway.Application.Analytics;

public sealed record ReportRange(DateTime From, DateTime To, TimeSpan Bucket)
{
    private static readonly TimeSpan DefaultSpan = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaxSpan = TimeSpan.FromDays(92);
    private static readonly TimeSpan HourlyUpTo = TimeSpan.FromHours(48);

    public static ReportRange Resolve(DateTimeOffset? from, DateTimeOffset? to, DateTimeOffset now)
    {
        var end = (to ?? now).UtcDateTime;
        var start = (from ?? end - DefaultSpan).UtcDateTime;

        if (start >= end)
        {
            start = end - DefaultSpan;
        }

        if (end - start > MaxSpan)
        {
            start = end - MaxSpan;
        }

        var bucket = end - start <= HourlyUpTo ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1);
        return new ReportRange(start, end, bucket);
    }

    public bool IsHourly => Bucket == TimeSpan.FromHours(1);

    public DateTime BucketOf(DateTime utc) =>
        IsHourly
            ? new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc)
            : new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);

    public IEnumerable<DateTime> Buckets()
    {
        for (var bucket = BucketOf(From); bucket <= To; bucket += Bucket)
        {
            yield return bucket;
        }
    }
}
