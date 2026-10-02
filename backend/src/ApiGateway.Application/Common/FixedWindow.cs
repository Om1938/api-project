namespace ApiGateway.Application.Common;

public readonly record struct FixedWindow(string Id, DateTimeOffset Start, DateTimeOffset End)
{
    public static FixedWindow Minute(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        var start = new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero);
        return new FixedWindow(start.ToString("yyyyMMddHHmm"), start, start.AddMinutes(1));
    }

    public static FixedWindow Month(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        var start = new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return new FixedWindow(start.ToString("yyyyMM"), start, start.AddMonths(1));
    }

    public TimeSpan RemainingAt(DateTimeOffset now) => End > now ? End - now : TimeSpan.Zero;
}
