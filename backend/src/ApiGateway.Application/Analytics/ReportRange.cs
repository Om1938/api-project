namespace ApiGateway.Application.Analytics;

public sealed record ReportRange(DateTime From, DateTime To, TimeSpan Bucket)
{
    public const int MaxBuckets = 400;

    private static readonly int[] BucketMinutes = [1, 5, 30, 60, 1440];
    private static readonly TimeSpan DefaultSpan = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaxSpan = TimeSpan.FromDays(92);

    public static ReportRange Resolve(DateTimeOffset? from, DateTimeOffset? to, DateTimeOffset now, int? bucketMinutes = null)
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

        return new ReportRange(start, end, PickBucket(end - start, bucketMinutes));
    }

    public bool IsSubHourly => Bucket < TimeSpan.FromHours(1);

    public DateTime BucketOf(DateTime utc) => new(utc.Ticks - utc.Ticks % Bucket.Ticks, DateTimeKind.Utc);

    public IEnumerable<DateTime> Buckets()
    {
        for (var bucket = BucketOf(From); bucket <= To; bucket += Bucket)
        {
            yield return bucket;
        }
    }

    private static TimeSpan PickBucket(TimeSpan span, int? requested)
    {
        var wanted = requested is { } minutes && BucketMinutes.Contains(minutes) ? minutes : Automatic(span);

        // a finer bucket than the range can carry is coarsened rather than refused
        var fitting = BucketMinutes.Where(m => m >= wanted).FirstOrDefault(m => span.TotalMinutes / m <= MaxBuckets, 1440);
        return TimeSpan.FromMinutes(fitting);
    }

    // thresholds leave slack: "the last hour" as sent by a client is always a little over an hour
    private static int Automatic(TimeSpan span) => span.TotalHours switch
    {
        <= 1.5 => 1,
        <= 8 => 5,
        <= 48 => 60,
        _ => 1440,
    };
}
