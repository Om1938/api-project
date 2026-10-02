# 0016. Frontend stack and OpenAPI-generated types

## Context

The PRD asks for React + TypeScript. The dashboard is forms, tables and one chart for two roles.

## Decision

| Concern | Choice |
|---|---|
| Build | Vite |
| Routing | React Router (one route tree per role, guarded by `RoleArea`) |
| Server state | TanStack Query; no client-side store |
| Forms | react-hook-form + zod |
| Styling | Tailwind CSS v4 with design tokens as CSS variables (light and dark) |
| Charts | Recharts |
| Tests | Vitest + Testing Library |
| Lint | oxlint (from the Vite template) |

**Types come from the backend.** `npm run gen:api` downloads the Swagger document and runs
`openapi-typescript`, producing `src/api/schema.d.ts`. `src/api/types.ts` only aliases those types.
No DTO is written by hand, so a backend change that breaks the frontend fails `tsc`.

**Shared building blocks instead of repeated patterns:**

- `api/client.ts`: the only `fetch` call. Adds the bearer token, parses problem-details errors into
  `ApiError`, ends the session on a rejected token.
- `QueryView`: loading / error / data for any query.
- `FormDialog`: every create/edit form (validation, pending state, server error, close on success).
- `DataTable`, `StatCard`, `Meter`, `Badge`, `Field`.
- `UsageReport`: the same tiles and chart for owners and consumers.
- `useApiMutation`: every mutation refetches all cached queries on success. Resources are interlinked
  (a new key changes key lists, consumer counts and quotas), and at this size "refresh what is on
  screen" is simpler and safer than hand-maintained invalidation lists.

**Session outside React.** The signed-in session lives in a small external store
(`features/auth/sessionStore.ts`, persisted to `localStorage`). The API client reads the token from it
and clears it when the server rejects the token; React subscribes with `useSyncExternalStore`. This
keeps `api/client.ts` free of component state. A `401` on a request that carried no token is just a
failed login and does not touch the session. When the signed-in user changes, the query cache is
cleared so one user's data is never shown to the next.

**Live dashboards by polling.** Usage, quota and credit queries refetch every 5 seconds and keep the
previous data on screen while loading. Analytics queries are cached by range *preset* (`24h`, `7d`,
`30d`), not by timestamps; the start of the range is recomputed on every refetch, so a polling
dashboard keeps sliding forward without creating a new cache entry each time.

**The chart** is a stacked bar per time bucket with four outcome series (successful, failed, rate
limited, quota-or-credits). Colours are a fixed, colour-blind-checked order defined once as tokens;
identity never relies on colour alone (legend, tooltip listing every series, and a table view of the
same numbers). Text uses ink tokens, never series colours. Segments of a bar are separated by a
2px gap of bare surface rather than an outline, and only the top segment is rounded. The series
order in `index.css` is what makes neighbouring colours distinguishable, so it must not be reordered.

**What is charted, and why that form.** One usage report drives every chart, so the range filter
scopes all of them at once.

| Chart | Form | Why |
|---|---|---|
| Requests over time by outcome | stacked columns | part-to-whole per period |
| How requests ended | donut | four-way share at a glance; exact numbers sit beside it |
| Success rate, average latency | line | a rate or a mean over time; not filled, because they do not add up |
| Credits over time | filled line | a quantity that accumulates |
| Latency distribution | histogram | shape of a distribution |
| When traffic arrives | heatmap (day x hour) | two dimensions plus magnitude |
| Top endpoints, status codes, methods, by API, by tier | ranked horizontal bars | compare magnitudes of named things |
| Busiest consumers | stacked horizontal bars | ranking plus the outcome split |

Only request outcomes get distinct colours, and the same outcome has the same colour in every chart.
Everything else is a single blue, because bar length already carries the value. The heatmap fades
that blue into the surface on a square-root scale, so one unusually busy hour does not flatten the
rest. Charts with axes offer a table view; ranked bars print their values, so they are their own table.

**Same origin.** In development Vite proxies `/api`, `/gw` and `/swagger` to the backend; in Docker
nginx does. The app uses relative URLs only, and no CORS configuration is needed in the default setup.

## Alternatives considered

- **Next.js.** Server rendering is of no use to an authenticated dashboard.
- **Redux or another store.** All state here is server state or local form state.
- **A component library (MUI, shadcn/ui).** Faster to start, heavier to own; the dozen primitives
  needed are small.
- **Hand-written DTO interfaces.** Would drift from the backend.
- **WebSockets / SSE for live updates.** Polling every few seconds is indistinguishable in a demo.

## Consequences

- `openapi-typescript` is run through `npx` rather than installed, because its current release
  declares a TypeScript 5 peer dependency and the project uses TypeScript 6. It only reads JSON, so
  this has no effect on its output.
- Regenerating types needs a running backend.
- The production bundle is a single ~800 kB chunk (Recharts is most of it); code-splitting the
  analytics routes would cut the initial load.
