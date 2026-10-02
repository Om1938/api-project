# 0012. Asynchronous usage metering

## Context

Every gateway request that is attributable to a key must be recorded for analytics, quotas and
billing reports. Writing to MySQL inside the request would add latency to every call and couple the
gateway's availability to insert speed.

## Decision

- The request path only **enqueues** a `UsageEvent` on an in-memory bounded channel (`IUsageSink`).
  Enqueueing never blocks and never throws.
- A hosted service, `UsageWriterService`, drains the channel and **inserts in batches** of up to 500
  records per transaction. There is no artificial delay: it writes whatever has accumulated.
- On graceful shutdown the service flushes what is still buffered.
- The channel holds up to 50,000 events. If it is full, new events are dropped and a warning is logged.
- What is recorded: key, API, consumer, timestamp, method, path (truncated to 512 characters), status,
  outcome, latency, credits charged. Query strings and bodies are not recorded.
- Requests that fail authentication are not recorded at all: there is no consumer to attribute them to.

## Alternatives considered

- **Synchronous insert per request.** Durable and simple; slowest, and a database hiccup becomes a
  gateway outage.
- **A durable queue (Redis Streams, Kafka, RabbitMQ).** Survives a crash and works across instances.
  That is the production answer; it adds infrastructure the PRD does not ask for.
- **Aggregating in Redis and never storing raw rows.** Small and fast, but loses the per-request detail
  that makes the analytics flexible ([0013](0013-analytics-from-raw-records.md)).

## Consequences

- **Records in the buffer are lost if the process crashes.** Normally that is well under a second of
  traffic. Credits are charged synchronously ([0011](0011-credit-billing.md)), so a crash can lose the
  *record* of a charge but never the charge itself.
- Dashboards lag reality by a moment; they poll every 5 seconds anyway.
- If a batch insert fails, the batch is logged and discarded rather than retried forever.
