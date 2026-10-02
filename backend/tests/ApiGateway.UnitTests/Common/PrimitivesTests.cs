using ApiGateway.Application.Analytics;
using ApiGateway.Application.Apis;
using ApiGateway.Application.Common;
using ApiGateway.Application.Gateway;
using ApiGateway.Domain.Enums;
using ApiGateway.Domain.Security;

namespace ApiGateway.UnitTests.Common;

public class FixedWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 31, 23, 59, 30, TimeSpan.Zero);

    [Fact]
    public void Minute_window_is_aligned_to_the_clock_minute()
    {
        var window = FixedWindow.Minute(Now);

        Assert.Equal("202612312359", window.Id);
        Assert.Equal(new DateTimeOffset(2026, 12, 31, 23, 59, 0, TimeSpan.Zero), window.Start);
        Assert.Equal(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero), window.End);
        Assert.Equal(TimeSpan.FromSeconds(30), window.RemainingAt(Now));
    }

    [Fact]
    public void Month_window_spans_the_calendar_month_and_rolls_over_the_year()
    {
        var window = FixedWindow.Month(Now);

        Assert.Equal("202612", window.Id);
        Assert.Equal(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero), window.Start);
        Assert.Equal(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero), window.End);
    }

    [Fact]
    public void Windows_are_computed_in_utc_whatever_the_offset()
    {
        var sameInstantElsewhere = Now.ToOffset(TimeSpan.FromHours(5.5));

        Assert.Equal(FixedWindow.Minute(Now), FixedWindow.Minute(sameInstantElsewhere));
        Assert.Equal(FixedWindow.Month(Now), FixedWindow.Month(sameInstantElsewhere));
    }

    [Fact]
    public void Requests_in_the_same_minute_share_a_window_and_the_next_minute_does_not()
    {
        Assert.Equal(FixedWindow.Minute(Now), FixedWindow.Minute(Now.AddSeconds(29)));
        Assert.NotEqual(FixedWindow.Minute(Now), FixedWindow.Minute(Now.AddSeconds(30)));
    }
}

public class QuotaAlertPolicyTests
{
    [Theory]
    [InlineData(100, 80, 80)]
    [InlineData(10, 75, 8)]   // 7.5 rounds up
    [InlineData(3, 10, 1)]    // never below one request
    [InlineData(100, 100, 100)]
    public void Threshold_count_rounds_up(int limit, int percent, long expected)
    {
        Assert.Equal(expected, QuotaAlertPolicy.ThresholdCount(limit, percent));
    }

    [Fact]
    public void Nothing_is_raised_between_lines()
    {
        Assert.Empty(QuotaAlertPolicy.Evaluate(used: 79, limit: 100, [80]));
        Assert.Empty(QuotaAlertPolicy.Evaluate(used: 81, limit: 100, [80]));
        Assert.Empty(QuotaAlertPolicy.Evaluate(used: 102, limit: 100, [80]));
    }

    [Fact]
    public void Threshold_is_raised_on_the_crossing_request_once_per_distinct_percentage()
    {
        var alerts = QuotaAlertPolicy.Evaluate(used: 80, limit: 100, [80, 80, 90]).ToList();

        Assert.Equal([(WebhookEvents.QuotaThresholdReached, (int?)80)], alerts);
    }

    [Fact]
    public void Exceeded_is_raised_on_the_first_request_over_the_limit_even_without_thresholds()
    {
        var alerts = QuotaAlertPolicy.Evaluate(used: 101, limit: 100, []).ToList();

        Assert.Equal([(WebhookEvents.QuotaExceeded, (int?)null)], alerts);
    }
}

public class ApiKeySecretTests
{
    [Fact]
    public void Generated_secret_is_prefixed_long_and_unique()
    {
        var first = ApiKeySecret.Generate();
        var second = ApiKeySecret.Generate();

        Assert.StartsWith(ApiKeySecret.Scheme, first.Plaintext);
        Assert.True(first.Plaintext.Length >= 40);
        Assert.NotEqual(first.Plaintext, second.Plaintext);
        Assert.StartsWith(first.Prefix, first.Plaintext);
    }

    [Fact]
    public void Hash_is_deterministic_hex_sha256_and_does_not_contain_the_secret()
    {
        var secret = ApiKeySecret.Generate();

        Assert.Equal(secret.Hash, ApiKeySecret.ComputeHash(secret.Plaintext));
        Assert.Matches("^[0-9a-f]{64}$", secret.Hash);
        Assert.DoesNotContain(secret.Plaintext, secret.Hash);
    }
}

public class SlugsTests
{
    [Theory]
    [InlineData("Weather API", "weather-api")]
    [InlineData("  Payments  (v2)!  ", "payments-v2")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("!!!", "")]
    public void Derives_a_slug_from_free_text(string text, string expected)
    {
        Assert.Equal(expected, Slugs.From(text));
    }

    [Theory]
    [InlineData("weather-api", true)]
    [InlineData("v2", true)]
    [InlineData("Weather", false)]
    [InlineData("-weather", false)]
    [InlineData("weather--api", false)]
    [InlineData("weather/api", false)]
    public void Validates_slug_format(string slug, bool expected)
    {
        Assert.Equal(expected, Slugs.IsValid(slug));
    }
}

public class ReportRangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 30, 0, TimeSpan.Zero);

    private static int BucketMinutes(TimeSpan span, int? requested = null) =>
        (int)ReportRange.Resolve(Now - span, Now, Now, requested).Bucket.TotalMinutes;

    [Fact]
    public void Defaults_to_the_last_24_hours_in_hourly_buckets()
    {
        var range = ReportRange.Resolve(null, null, Now);

        Assert.Equal(Now.UtcDateTime.AddHours(-24), range.From);
        Assert.Equal(Now.UtcDateTime, range.To);
        Assert.Equal(TimeSpan.FromHours(1), range.Bucket);
        Assert.Equal(25, range.Buckets().Count());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(6, 5)]
    [InlineData(24, 60)]
    [InlineData(24 * 7, 1440)]
    public void Picks_a_bucket_that_suits_the_range_when_none_is_asked_for(int hours, int expectedMinutes)
    {
        Assert.Equal(expectedMinutes, BucketMinutes(TimeSpan.FromHours(hours)));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(6, 1, 1)]
    [InlineData(24, 5, 5)]
    [InlineData(24, 30, 30)]
    [InlineData(24 * 7, 30, 30)]
    [InlineData(24 * 7, 60, 60)]
    public void Honours_a_requested_bucket_the_range_can_carry(int hours, int requested, int expected)
    {
        Assert.Equal(expected, BucketMinutes(TimeSpan.FromHours(hours), requested));
    }

    [Theory]
    [InlineData(24, 1, 5)]          // 1440 one-minute points is too many
    [InlineData(24 * 7, 5, 30)]
    [InlineData(24 * 30, 1, 1440)]
    [InlineData(24, 7, 60)]         // not an offered size: fall back to automatic
    public void Coarsens_or_ignores_a_bucket_the_range_cannot_carry(int hours, int requested, int expected)
    {
        Assert.Equal(expected, BucketMinutes(TimeSpan.FromHours(hours), requested));
    }

    [Fact]
    public void Buckets_are_aligned_to_the_clock()
    {
        var at = new DateTime(2026, 3, 15, 10, 37, 42, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 3, 15, 10, 35, 0, DateTimeKind.Utc), ReportRange.Resolve(Now.AddHours(-6), Now, Now, 5).BucketOf(at));
        Assert.Equal(new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc), ReportRange.Resolve(Now.AddHours(-6), Now, Now, 30).BucketOf(at));
        Assert.Equal(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), ReportRange.Resolve(Now.AddHours(-24), Now, Now).BucketOf(at));
        Assert.Equal(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), ReportRange.Resolve(Now.AddDays(-7), Now, Now).BucketOf(at));
    }

    [Fact]
    public void Longer_ranges_use_daily_buckets()
    {
        var range = ReportRange.Resolve(Now.AddDays(-7), Now, Now);

        Assert.Equal(8, range.Buckets().Count());
    }

    [Fact]
    public void Inverted_and_oversized_ranges_are_corrected()
    {
        Assert.Equal(TimeSpan.FromHours(24), Span(ReportRange.Resolve(Now.AddDays(1), Now, Now)));
        Assert.Equal(TimeSpan.FromDays(92), Span(ReportRange.Resolve(Now.AddDays(-400), Now, Now)));

        static TimeSpan Span(ReportRange range) => range.To - range.From;
    }
}

public class OutcomeTallyTests
{
    [Fact]
    public void Sums_counts_per_outcome_and_credits()
    {
        var tally = new OutcomeTally();
        tally.Add(RequestOutcome.Success, 5, 5m);
        tally.Add(RequestOutcome.Success, 2, 2m);
        tally.Add(RequestOutcome.Failed, 1, 0m);
        tally.Add(RequestOutcome.RateLimited, 3, 0m);

        var summary = tally.ToSummary();

        Assert.Equal(new UsageSummaryDto(11, 7, 1, 3, 0, 0, 7m), summary);
    }

    [Fact]
    public void Average_latency_covers_forwarded_requests_only()
    {
        var tally = new OutcomeTally();
        tally.Add(RequestOutcome.Success, 3, 3m, latencyMs: 90);
        tally.Add(RequestOutcome.Failed, 1, 0m, latencyMs: 110);
        tally.Add(RequestOutcome.RateLimited, 6, 0m);

        Assert.Equal(50, tally.AverageLatencyMs);
        Assert.Equal(0, new OutcomeTally().AverageLatencyMs);
    }
}

public class EndpointPatternTests
{
    [Theory]
    [InlineData("/get", "/get")]
    [InlineData("/users/42", "/users/{id}")]
    [InlineData("/users/42/orders/7", "/users/{id}/orders/{id}")]
    [InlineData("/keys/01a0f8b4-e08f-7b8a-a0ca-05c1d37ff6e1/revoke", "/keys/{id}/revoke")]
    [InlineData("/v2/status", "/v2/status")]
    [InlineData("/", "/")]
    public void Collapses_identifier_segments(string path, string expected)
    {
        Assert.Equal(expected, EndpointPattern.Of(path));
    }
}
