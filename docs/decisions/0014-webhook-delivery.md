# 0014. Webhook delivery, signing, de-duplication and retries

## Context

Owners can configure a webhook URL to be told when a key reaches a share of its monthly quota or
exceeds it.

## Decision

**Configuration.** A webhook belongs to an API. It has a URL, the events it wants, a threshold
percentage (1-100) and an active flag. An API can have several webhooks with different thresholds.

**Events.**

| Name | Raised when |
|---|---|
| `quota.threshold_reached` | a key's usage this month reaches the webhook's threshold share of its quota |
| `quota.exceeded` | the first request beyond the quota arrives (the first one to be rejected) |

**Detection** happens in `QuotaGate`, using the count it has just incremented. `QuotaAlertPolicy`
raises an alert only on the *exact* request that crosses a line, so no per-key state is needed.
The threshold count is the percentage of the quota rounded up, and never less than one request
(75 % of 10 is the 8th request).

**At most once.** Before queueing, a Redis `SET NX` marker per key, month, event and percentage
guards against duplicates (several instances, or a counter that was re-seeded).

**Delivery** is off the request path. `WebhookDispatcherService` takes alerts from an in-memory queue,
finds the active webhooks of that API that want the event, and POSTs JSON:

```json
{
  "id": "0199…",
  "event": "quota.threshold_reached",
  "occurredAt": "2026-10-01T18:48:02.11+00:00",
  "data": {
    "apiId": "…", "apiKeyId": "…", "consumerId": "…",
    "period": "202610", "used": 80, "limit": 100, "thresholdPercent": 80
  }
}
```

Headers: `X-Gateway-Event`, `X-Gateway-Delivery` (same as `id`) and
`X-Gateway-Signature: sha256=<hex>`, an **HMAC-SHA256 of the raw body** with the webhook's secret
(`whsec_…`, generated on creation, shown in the dashboard). Receivers verify the signature and can
de-duplicate on `id`.

**Retries.** Any non-2xx response or network error is retried: 3 attempts in total, waiting 2 s then
4 s, 10 s timeout per attempt (all configurable under `Webhooks`).

**Audit.** Every delivery is stored with its payload, final status, attempt count and error, and listed
in the dashboard.

## Alternatives considered

- **Sending from the request path.** Would let a slow receiver slow down the gateway.
- **A persistent outbox table with a polling sender.** Survives crashes; the production answer. More
  moving parts than the demo warrants.
- **One threshold per tier or per key.** Finer control; the PRD only asks for a configurable webhook URL.
- **Evaluating thresholds periodically from the usage log.** Decoupled, but alerts would arrive late.

## Consequences

- Alerts queued in memory are lost if the process crashes before sending; the Redis marker then
  prevents them from being raised again that month.
- Deliveries are sent one at a time; an unresponsive receiver delays the alerts behind it by up to
  about 36 seconds.
- A threshold alert is tied to the exact count, so adding a webhook after usage has already passed
  its threshold does not fire retroactively.
- Webhook URLs are owner-supplied; see [0019](0019-outbound-url-validation.md).
