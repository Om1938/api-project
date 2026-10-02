# 0017. Docker Compose topology and seed data

## Context

The PRD names Docker Compose as the deployment, and the demo must be runnable with one command.

## Decision

Five services in `docker-compose.yml`:

| Service | Image | Host port | Role |
|---|---|---|---|
| `mysql` | `mysql:8.4` | 3306 | Configuration and usage log; data in the `mysql-data` volume |
| `redis` | `redis:7-alpine` | 6379 | Counters and key cache; no persistence configured |
| `upstream` | `mccutchen/go-httpbin` | 8081 | A ready-made API to proxy to |
| `backend` | built from `backend/Dockerfile` | 8080 | Management API + gateway + Swagger |
| `frontend` | built from `frontend/Dockerfile` | 3000 | nginx serving the dashboard and proxying `/api`, `/gw`, `/swagger` |

- The backend starts only after MySQL and Redis report healthy.
- All settings have working defaults; `.env.example` lists what can be overridden (ports, database
  credentials, JWT signing key, whether to seed).
- Both Dockerfiles are multi-stage and restore dependencies in a separate layer before copying sources.
- nginx gives the dashboard and the gateway **one origin**, so consumers get one base URL and the
  browser needs no CORS.

**Seed data** (when `Seed:Enabled` is true and the database has no users): a demo owner, a demo
consumer with 100 credits, the API `httpbin` pointing at the `upstream` service, and tiers *Free*
(5/min, 100/month, 1 credit) and *Pro* (60/min, 10,000/month, 0.5 credit). No key is seeded, because
issuing one is part of the demo.

**Sample history.** A fresh database would show empty charts, so the seed can also generate
synthetic usage for the demo API (`Seed:SampleHistoryDays`, `SEED_HISTORY_DAYS` in compose, default
21, `0` to disable). It is made up, and exists only so the dashboards have something to show:

- three more consumers (Northwind Traders, Contoso Retail, Fabrikam Labs) plus the demo consumer,
  each with one key; three on *Pro*, one on *Free*. The key secrets are discarded, so these keys
  cannot be used to call the gateway;
- roughly 11,000 usage records ending at the moment of seeding, with a daytime peak, quieter
  weekends and slow growth over the period;
- mostly successful requests, some upstream failures and rate-limited calls, log-normal latency
  around 40 ms, and one afternoon two thirds of the way through where a third of requests fail and
  latency is five times higher;
- never more forwarded requests in a calendar month than the key's tier allows; a key that reaches
  its quota gets `quota_exceeded` records from then on;
- matching top-up transactions, so each consumer's ledger adds up to their balance;
- seeded entities are back-dated to the start of the period.

Generation uses a fixed random seed, so two fresh databases seeded at the same time get the same
shape of data. Real traffic simply adds to it.

## Alternatives considered

- **Serving the frontend from the backend's `wwwroot`.** One container fewer, but couples the two
  builds and hides the fact that they are separate applications.
- **A hand-written mock upstream.** httpbin already echoes requests and can return any status
  (`/status/500`), which is what a demo needs.
- **Redis persistence (AOF).** Unnecessary: everything in Redis can be rebuilt
  ([data-model.md](../data-model.md)).

## Consequences

- The credentials and signing key in the compose file are demo values. They must be overridden for
  anything that is not a local demo.
- MySQL and Redis are published on the host for convenience during development; a real deployment
  would not expose them.
- Seeded accounts use a known password and are for local use only.
- With sample history on, analytics in a fresh demo are not real measurements. Set
  `SEED_HISTORY_DAYS=0` for a database that contains only what actually happened.
- Seeding runs only on an empty database; changing the setting later needs `docker compose down -v`.
- From inside the backend container the demo upstream is `http://upstream:8080`, not `localhost:8081`.
