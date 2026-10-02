# Demo walkthrough

Shows the PRD's success criteria: an owner creates an API and a tier and issues a key; a consumer
calls the API through the gateway; the rate limit is enforced; the usage appears on the dashboard.

## Start

```bash
docker compose up --build -d
```

| What | Where |
|---|---|
| Dashboard | http://localhost:3000 |
| Swagger UI | http://localhost:8080/swagger (also http://localhost:3000/swagger) |
| Gateway | http://localhost:3000/gw/{slug}/... or http://localhost:8080/gw/{slug}/... |
| Demo upstream (httpbin) | http://localhost:8081 from your machine, `http://upstream:8080` from the backend |

Seeded accounts (password `Password123!`):

| Role | Email | Starts with |
|---|---|---|
| Owner | `owner@demo.local` | API "HTTPBin Demo" (`/gw/httpbin`) with tiers Free (5/min, 100/month, 1 credit) and Pro (60/min, 10,000/month, 0.5 credit) |
| Consumer | `consumer@demo.local` | 100 credits |

The first start also generates three weeks of **made-up usage history** for the `httpbin` API (and
three extra consumers), so the charts are populated straight away. Set `SEED_HISTORY_DAYS=0` in `.env`
before the first start to skip it. Details in [decisions/0017](decisions/0017-docker-compose-and-seed-data.md).

## Option A: the script (about 10 seconds)

Needs `curl` and `jq`.

```bash
./scripts/demo.sh
```

It signs in as the owner, registers a new API pointing at the demo upstream, creates a 5-requests-per-minute
tier, issues a key to the demo consumer, sends 8 requests through the gateway (five `200`, then `429`),
and prints the owner's analytics and the consumer's quota and credit balance.

## Generating traffic

`scripts/traffic.py` (Python 3, no dependencies) sends random requests through the gateway so the
dashboards fill up.

```bash
./scripts/traffic.py                 # 100 requests, then a summary
./scripts/traffic.py -n 500 -d 0.2   # 500 requests, at most 0.2 s apart
./scripts/traffic.py --forever       # keep going until Ctrl-C
```

On first run it creates an API (`/gw/traffic-sim`) with two tiers, registers four consumers and issues
each a key; later runs reuse them (`--fresh` starts over). The mix is mostly successful calls, plus
upstream errors, deliberately slow responses (30 ms to about a second), the odd missing or invalid
key, and occasional bursts that trip the rate limit. A
consumer that runs out of credits is topped up automatically. `--help` lists the other options.

## Option B: by hand in the dashboard

1. **Sign in as the owner** at http://localhost:3000 (`owner@demo.local`).
2. **Create an API.** *APIs → Register API.* Name it, and use `http://upstream:8080` as the target base URL.
3. **Create a tier.** On the API's page, *Tiers → New tier*: 5 requests per minute, quota 100, 1 credit per request.
4. **(Optional) Add a webhook.** *Webhooks → New webhook* with `http://upstream:8080/post` as the endpoint
   and a threshold of, say, 5 %. httpbin accepts any POST, so deliveries will show as delivered.
5. **Issue a key.** *API keys → Issue key*: pick the tier and the consumer `consumer@demo.local`.
   Copy the key (it is shown only once) or the ready-made `curl` command.
6. **Call the API as the consumer.** Run the copied command more than five times within a minute:

   ```bash
   for i in $(seq 1 8); do
     curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:3000/gw/<slug>/get" -H "X-API-Key: <key>"
   done
   ```

   The first five print `200`, the rest `429`. Add `-i` to see `Retry-After` and the `X-RateLimit-*` headers.
   Use the *Analytics* pages in the side menu for the detail, and the two selectors at the top to
   change the time range (last hour to 30 days) and the resolution (per minute up to per day). To
   watch requests arrive live, pick *Last hour* and *Per 1 minute*.
   The dashboards show more than totals: outcome share, success rate, latency over time and its
   distribution, a day-by-hour heatmap, top endpoints, status codes, methods, and usage per API, tier
   and consumer. Let `./scripts/traffic.py --forever` run for a while to fill them.
7. **See it on the owner's dashboard.** The API's *Usage* tab (and *Overview*) refresh every few
   seconds: total, successful and rate-limited requests, credits consumed, and the per-consumer table.
8. **See it as the consumer.** Sign out, sign in as `consumer@demo.local`: usage, quota used per key,
   credit balance (100 minus what was spent). *Credits → + 50 credits* tops up the mock balance.
9. **Revoke the key** as the owner (*API keys → Revoke*) and repeat the call: `401`, `revoked_api_key`.

### Other things worth showing

| To show | Do |
|---|---|
| Quota exceeded | Create a tier with a monthly quota of 3 and call four times: the fourth is `429 quota_exceeded`. With a webhook configured, *Recent deliveries* shows the threshold and exceeded events. |
| Out of credits | Create a tier costing more than the consumer's balance: `402 insufficient_credits`. Top up and call again. |
| Failed requests | Call `/gw/<slug>/status/500`: passed through as `500`, counted as *Failed*, not charged. |
| Live tier change | Raise a tier's requests per minute while rate-limited: the next call succeeds. |

## Running without Docker (development)

```bash
docker compose up -d mysql redis upstream      # dependencies only

cd backend && dotnet run --project src/ApiGateway.Api        # http://localhost:8080
cd frontend && npm install && npm run dev                    # http://localhost:5173 (proxies /api and /gw)
```

In this mode the backend reaches the demo upstream at `http://localhost:8081`, which is what the
development seed uses.

## Tests

```bash
cd backend && dotnet test                         # unit + integration (integration needs Docker)
cd frontend && npm run typecheck && npm run lint && npm test
```

## Reset

```bash
docker compose down -v      # also deletes the database volume
```
