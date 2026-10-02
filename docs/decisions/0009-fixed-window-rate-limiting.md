# 0009. Fixed-window rate limiting in Redis

## Context

The PRD prescribes Redis with a fixed-window algorithm and HTTP `429` when the limit is exceeded.
The limit is the tier's *requests per minute*, applied per API key.

## Decision

- One counter per key per clock minute: `rl:{keyId}:{yyyyMMddHHmm}` (UTC).
- Each request runs **one Lua script** that does `INCR` and, when the result is 1, `PEXPIRE`. Running
  both in a script makes them atomic: a counter can never exist without a TTL, and concurrent requests
  always get distinct counts.
- The request is allowed while `count <= limit`. Otherwise: `429`, code `rate_limit_exceeded`,
  `Retry-After` set to the seconds left in the window.
- `X-RateLimit-Limit`, `-Remaining` and `-Reset` are returned on allowed and rejected responses.
- Counters expire 5 seconds after their window ends.
- The algorithm is expressed with two reusable pieces: `FixedWindow` (which window is "now"?) and
  `IFixedWindowCounter` (increment a named counter). The monthly quota uses the same two pieces with
  a different window ([0010](0010-monthly-quota.md)).
- The current time comes from the injected `TimeProvider` on the application server, and is taken once
  per request so every gate sees the same instant.
- **If Redis cannot be reached the request fails (500).** The gateway fails closed rather than serve
  unmetered traffic.

## Alternatives considered

- **Sliding window log / sliding window counter / token bucket.** All smooth out the boundary burst
  described below. The PRD asks for fixed window, which is also the easiest to explain and verify.
- **`INCR` then `EXPIRE` as two commands.** A crash between them leaves a counter that never expires
  and locks the key out for good.
- **ASP.NET Core's built-in rate limiter.** In-memory per instance; the limit would multiply with the
  number of backend instances.
- **Redis server time (`TIME`) for the window.** Removes app-server clock skew, at the price of a less
  testable gate. With a single backend instance there is no skew to remove.

## Consequences

- **Boundary burst:** a client can send `limit` requests at the end of one minute and `limit` more at
  the start of the next, i.e. up to twice the limit in a short span. This is inherent to fixed windows.
- Rejected requests also increment the counter, so a client hammering a limited key stays limited
  until the window ends, never longer.
- One Redis round trip per request for the rate limit.
