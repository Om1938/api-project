# 0010. Monthly quota counting

## Context

Tiers have a monthly request quota. Consumers must be able to see how much is left, and webhooks
fire as the quota is approached and exceeded.

## Decision

- **What counts:** a request consumes quota when it is **forwarded upstream**, whatever the upstream
  answers. Requests the gateway rejects (rate limit, credits, quota itself) do not. The rule is defined
  once, as `UsageRules.ConsumesQuota`.
- **The month** is the calendar month in UTC. The quota is per API key.
- **Enforcement** uses the same fixed-window counter as the rate limit, with key
  `quota:{keyId}:{yyyyMM}`. Beyond the quota: `429`, code `quota_exceeded`, `Retry-After` until the
  month ends.
- **Durability:** Redis is the fast path, MySQL is the truth. When the counter does not exist (first
  request of the month, or Redis lost it) it is **seeded from the usage log** before incrementing.
  `SET NX` makes concurrent seeding safe.
- **What consumers see** (`quotaUsed`, `quotaRemaining`) is counted from the usage log with the same
  rule, not read from Redis.
- Crossing a configured threshold or the quota itself raises an alert ([0014](0014-webhook-delivery.md)).

## Alternatives considered

- **Counting in MySQL on every request.** Always exact, but puts a write and a count on the hot path.
- **Redis only.** Simplest, but a Redis flush would silently reset everyone's month.
- **Counting only successful requests.** Arguably fairer to consumers, but a quota protects the
  upstream from load, and a failed call is still load. Billing, by contrast, charges only successes
  ([0011](0011-credit-billing.md)).
- **Rolling 30-day window.** Fairer at month boundaries; much harder to explain and to show as
  "remaining this month".

## Consequences

- Usage is written asynchronously ([0012](0012-asynchronous-usage-metering.md)), so a counter re-seeded
  right after a Redis loss can be a few requests low. The consumer-facing number can likewise lag the
  enforced number by about a second.
- Over-quota requests keep incrementing the Redis counter. That is harmless: the decision is
  `count > quota`, and re-seeding uses the log, which records them as rejected.
- Lowering a tier's quota below current usage blocks its keys until the next month.
