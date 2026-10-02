# 0015. Caching key lookups and invalidating them

## Context

Authenticating a gateway request needs the key, its API (slug, target, active flag), its tier (limits,
cost) and the API's alert thresholds: a three-table join plus one query. Doing that in MySQL on every
proxied request would make the database the gateway's bottleneck.

## Decision

- The lookup result is one flat, serialisable record, **`KeyContext`**.
- `EfKeyContextResolver` builds it from MySQL. **`CachedKeyContextResolver` decorates it** with Redis:
  key `keyctx:{sha256(key)}`, JSON value, **60-second TTL**. Both implement `IKeyContextResolver`; the
  gate cannot tell which one it has.
- **Unknown keys are cached too**, as a marker with a 15-second TTL, so a flood of guessed keys costs
  one database query per guess per 15 seconds instead of one per request.
- **Changes evict immediately.** Any use case that changes what a context was built from calls
  `KeyContextRefresher`: revoking a key evicts that key; editing a tier evicts the tier's keys; editing
  or deleting an API, or changing its webhooks, evicts the API's keys. The TTL is only a safety net.

## Alternatives considered

- **No cache.** Simplest and always fresh; a database round trip on every request.
- **In-process memory cache.** Fastest, but with more than one backend instance a revocation on one
  would not reach the others until expiry.
- **TTL only, no eviction.** A revoked key would keep working for up to a minute, which is not an
  acceptable answer to "I just revoked that key".
- **Caching the credit balance as well.** Left out deliberately: money-like data is always read from
  MySQL ([0011](0011-credit-billing.md)).

## Consequences

- A warm gateway request authenticates with a single Redis read.
- Revocation and limit changes take effect on the next request (covered by integration tests).
- The cache is keyed by hash and stores no plaintext key.
- Eviction happens after the database commit. If Redis is unreachable at that moment the use case
  fails after the change was saved; the stale entry then lives at most 60 seconds.
