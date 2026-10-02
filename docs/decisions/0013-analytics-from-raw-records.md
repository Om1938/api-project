# 0013. Analytics computed from raw usage records

## Context

The dashboards show total, successful, failed and rate-limited requests, usage per consumer, credits
consumed, and a time series; for an owner across their APIs, for a consumer across their keys.

## Decision

- Analytics are **`GROUP BY` queries over `UsageRecords`**, run on demand. There are no rollup tables
  and no scheduled jobs.
- One query grouped by (day, hour, outcome) yields both the totals and the time series; a second one
  grouped by (consumer, outcome) yields the per-consumer table.
- `UsageAggregator` takes *any* slice of the log (`IQueryable<UsageRecord>`), so the owner view
  (records of my APIs) and the consumer view (my records) share the aggregation and differ only in the
  filter. `OutcomeTally` is the one place that turns grouped rows into a summary.
- A report also carries breakdowns, each one more `GROUP BY` over the same slice
  (`UsageBreakdownBuilder`): response status codes, HTTP methods, top endpoints, latency histogram,
  requests per API and per tier, and a day-of-week by hour-of-day grid. Average latency comes from
  the main query and covers forwarded requests only; a rejected request never reaches the upstream.
- **Endpoints** are grouped by path with identifier segments collapsed (`/users/42` becomes
  `/users/{id}`; numbers, GUIDs and long hex strings count as identifiers), so one route does not
  appear as hundreds of rows. Only the top 8 are returned.
- **Latency buckets** are fixed: under 25, 25-50, 50-100, 100-250, 250-500, 500-1000 and 1000+ ms.
- **Ranges:** default last 24 hours, at most 92 days. Up to 48 hours the series is hourly, beyond that
  daily. Buckets are UTC; the frontend formats them in the viewer's time zone.
- The series always contains every bucket in the range, including empty ones, so charts do not need
  to fill gaps.
- Three composite indexes match the three filters: `(ApiId, Timestamp)`, `(ConsumerId, Timestamp)`,
  `(ApiKeyId, Timestamp)`.

Outcome definitions are in [gateway-request-flow.md](../gateway-request-flow.md).

## Alternatives considered

- **Pre-aggregated hourly rollups.** Constant-time dashboards at any volume, but a second write path
  that must stay consistent with the raw log.
- **Counters in Redis.** Real-time and cheap, but fixed to the dimensions chosen up front.
- **A time-series database.** The right tool at scale; another service to run.

## Consequences

- Query cost grows with the number of requests in the range. Fine for a demo (index range scan); a
  busy deployment would add rollups and prune old raw rows.
- Daily buckets are UTC days, which may not match the viewer's calendar day. The day/hour grid is
  UTC as well, because a viewer's offset is not always a whole number of hours.
- A report now costs eight grouped queries instead of two, repeated on every 5-second poll. Fine at
  demo volume; rollups would be the fix at scale.
- Latency is stored in whole milliseconds, so a very fast upstream shows as 0 ms.
- Percentiles (p95, p99) are not offered: MySQL has no percentile aggregate, and approximating them
  from the histogram would overstate their accuracy.
- Because history has no foreign keys, deleting an API removes it from the owner's analytics (the
  filter is "APIs I own") while the consumer still sees their past usage.
