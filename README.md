# API Gateway

A lightweight API gateway with a web dashboard. API owners register upstream APIs, define access
tiers and issue API keys; consumers call those APIs through the gateway, which authenticates the key,
enforces a per-minute rate limit and a monthly quota, meters usage, deducts mock credits and fires
webhooks on quota events.

Built to demonstrate gateway concepts, not to run in production. See [docs/prd.md](docs/prd.md) for
the requirements and [docs/](docs/README.md) for the design and every decision behind it.

## Quick start

Requires Docker.

```bash
docker compose up --build -d
./scripts/demo.sh            # optional: runs the whole demo flow with curl (needs jq)
./scripts/traffic.py --forever   # optional: random traffic until Ctrl-C (Python 3)
```

| | |
|---|---|
| Dashboard | http://localhost:3000 |
| Swagger UI | http://localhost:8080/swagger |
| Gateway | `http://localhost:3000/gw/{api-slug}/...` with an `X-API-Key` header |

Sign in with the seeded accounts, password `Password123!`:

- owner: `owner@demo.local`
- consumer: `consumer@demo.local`

A fresh database is seeded with three weeks of generated usage so the charts are not empty
(`SEED_HISTORY_DAYS=0` turns that off).

A step-by-step tour is in [docs/demo-walkthrough.md](docs/demo-walkthrough.md).

## What is in the box

| Feature | Where to look |
|---|---|
| API, tier, key and webhook management | Dashboard (owner), `/api/apis/...` |
| API-key authentication, rate limit, quota, credits | [docs/gateway-request-flow.md](docs/gateway-request-flow.md) |
| Fixed-window rate limiting on Redis | [docs/decisions/0009](docs/decisions/0009-fixed-window-rate-limiting.md) |
| Usage analytics | Dashboard (owner: *Overview* and each API's *Usage*; consumer: *Overview*) |
| Mock credit billing | Dashboard (consumer: *Credits*), [docs/decisions/0011](docs/decisions/0011-credit-billing.md) |
| Webhook alerts | Each API's *Webhooks* tab, [docs/decisions/0014](docs/decisions/0014-webhook-delivery.md) |

## Layout

```
backend/    ASP.NET Core (.NET 10) solution: Domain, Application, Infrastructure, Api + tests
frontend/   React + TypeScript dashboard (Vite)
docs/       Architecture, request flow, data model, demo walkthrough, decision records
scripts/    demo.sh, traffic.py
docker-compose.yml
```

## Stack

C# / ASP.NET Core 10, EF Core + MySQL 8.4, Redis 7, YARP (forwarding), Swashbuckle (Swagger),
React 19 + TypeScript, TanStack Query, Tailwind CSS, Recharts, Docker Compose.

## Development

```bash
docker compose up -d mysql redis upstream

cd backend  && dotnet run --project src/ApiGateway.Api     # http://localhost:8080
cd frontend && npm install && npm run dev                  # http://localhost:5173
```

```bash
cd backend  && dotnet test                                 # integration tests need Docker
cd frontend && npm run typecheck && npm run lint && npm test
cd frontend && npm run gen:api                             # regenerate API types from a running backend
```

## Deploying the published images

Images are published to GHCR, so a host only needs Docker and two files from this repository:

```bash
cp .env.ghcr.example .env.ghcr        # set the passwords and JWT_SIGNING_KEY
docker compose -f docker-compose.ghcr.yml --env-file .env.ghcr up -d
```

- `ghcr.io/om1938/api-project-backend`
- `ghcr.io/om1938/api-project-frontend`

Only the frontend's port is published (`HTTP_PORT`, default 80); it serves the dashboard and proxies
`/api`, `/gw` and `/swagger` to the backend. Demo data is off by default here; set
`SEED_DEMO_DATA=true` to get the demo accounts and sample history.

To publish new images:

```bash
docker build -t ghcr.io/om1938/api-project-backend:latest backend
docker build -t ghcr.io/om1938/api-project-frontend:latest frontend
docker push ghcr.io/om1938/api-project-backend:latest
docker push ghcr.io/om1938/api-project-frontend:latest
```

## Not for production as-is

Demo credentials and signing key in `docker-compose.yml`, owner-supplied URLs are not restricted to
public hosts ([0019](docs/decisions/0019-outbound-url-validation.md)), usage records buffered in
memory ([0012](docs/decisions/0012-asynchronous-usage-metering.md)), and no payments, OAuth or
multi-instance support. Each decision record lists its own consequences.
