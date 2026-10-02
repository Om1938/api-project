# Documentation

| Document | What it answers |
|---|---|
| [prd.md](prd.md) | What was asked for. |
| [architecture.md](architecture.md) | How the system is put together, and where SOLID and DRY show up in the code. |
| [gateway-request-flow.md](gateway-request-flow.md) | What happens to one request through the gateway: checks, status codes, headers. |
| [data-model.md](data-model.md) | MySQL tables and Redis keys. |
| [demo-walkthrough.md](demo-walkthrough.md) | How to run it and show the PRD's success criteria. |
| [decisions/](decisions/) | Why each significant choice was made (one record per decision). |

## Decision records

Each record states the context, the decision, the alternatives that were considered and the consequences.

| # | Decision |
|---|---|
| [0001](decisions/0001-repository-layout.md) | One repository, one deployable backend |
| [0002](decisions/0002-layered-architecture.md) | Clean Architecture layering and the SOLID/DRY conventions |
| [0003](decisions/0003-controllers-and-swagger.md) | MVC controllers documented with Swashbuckle |
| [0004](decisions/0004-ef-core-and-mysql-provider.md) | EF Core 9 with the Pomelo MySQL provider; migrations on startup |
| [0005](decisions/0005-dashboard-authentication.md) | Email/password with JWT for the dashboard |
| [0006](decisions/0006-api-key-format-and-storage.md) | API key format, hashing and show-once |
| [0007](decisions/0007-gateway-routing-and-forwarding.md) | Routing by slug and forwarding with YARP |
| [0008](decisions/0008-gate-pipeline.md) | Admission checks as an ordered chain of gates |
| [0009](decisions/0009-fixed-window-rate-limiting.md) | Fixed-window rate limiting in Redis |
| [0010](decisions/0010-monthly-quota.md) | Monthly quota counting |
| [0011](decisions/0011-credit-billing.md) | Mock credit billing semantics |
| [0012](decisions/0012-asynchronous-usage-metering.md) | Asynchronous usage metering |
| [0013](decisions/0013-analytics-from-raw-records.md) | Analytics computed from raw usage records |
| [0014](decisions/0014-webhook-delivery.md) | Webhook delivery, signing, de-duplication and retries |
| [0015](decisions/0015-key-context-caching.md) | Caching key lookups and invalidating them |
| [0016](decisions/0016-frontend-stack.md) | Frontend stack and OpenAPI-generated types |
| [0017](decisions/0017-docker-compose-and-seed-data.md) | Docker Compose topology and seed data |
| [0018](decisions/0018-testing-strategy.md) | Testing strategy |
| [0019](decisions/0019-outbound-url-validation.md) | Validation of owner-supplied URLs (SSRF stance) |

## Adding a decision

Copy the shape of an existing record into `decisions/NNNN-short-title.md`, number it sequentially, and
add a row to the table above. Records are not edited after the fact; a changed decision gets a new
record that says which one it supersedes.
