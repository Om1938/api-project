# 0018. Testing strategy

## Context

The behaviour that matters most (limits, quotas, billing, cache invalidation) spans Redis, MySQL and
HTTP. Tests need to prove that behaviour without being slow or brittle.

## Decision

**Backend unit tests** (`tests/ApiGateway.UnitTests`, xUnit + NSubstitute) cover the logic that
depends only on ports:

- each gate (`ApiKeyGate`, `RateLimitGate`, `CreditGate`, `QuotaGate`) against fakes, including window
  rollover, by passing the request time in rather than waiting;
- `GatePipeline` ordering and short-circuiting; `RequestSettlement` charging rules;
- pure helpers: `FixedWindow`, `QuotaAlertPolicy`, `ApiKeySecret`, `Slugs`, `ReportRange`, `OutcomeTally`.

**Backend integration tests** (`tests/ApiGateway.IntegrationTests`) run the real application in-process
(`WebApplicationFactory`) against **real MySQL and Redis started by Testcontainers**, and a small real
HTTP server (`UpstreamStub`) that plays the upstream API and the webhook receiver. They drive the
public HTTP API only and cover the PRD's flows end to end: forward and rate-limit, quota and webhooks
(including signature verification and retries), credits and top-up, revocation, live tier changes,
role and ownership checks, validation, and upstream failures.

Each test creates its own owner, consumer and API (`Scenario`), so tests share one set of containers
without sharing state.

**Frontend tests** (Vitest) cover what has logic: the API client (token, error mapping, session end),
chart data shaping, range presets, formatting, and the shared table and meter components.
Type checking and linting are part of the frontend's verification.

**`scripts/demo.sh`** exercises the running Docker stack through its public ports.

## Alternatives considered

- **EF Core in-memory provider / SQLite for integration tests.** Fast, but they do not behave like
  MySQL for exactly the things used here: atomic `UPDATE ... WHERE`, date-part grouping, flag columns.
  A LINQ translation error in a list query (ordering after projecting to a DTO) only shows up
  against the real provider.
- **Mocking Redis.** The Lua scripts and `SET NX` semantics *are* the behaviour under test.
- **Browser end-to-end tests (Playwright) in the repository.** Valuable, but they need browser
  binaries; left out to keep `npm test` self-contained.

## Consequences

- `dotnet test` needs a running Docker daemon for the integration project; the unit project does not.
- Tests that count requests inside one rate-limit window first wait if the clock is within the last
  seconds of a minute (`Scenario.AvoidMinuteBoundaryAsync`), so they cannot straddle a window.
- Asynchronous effects (usage records, webhook deliveries) are asserted by polling with a timeout
  (`Scenario.EventuallyAsync`) instead of fixed sleeps.
