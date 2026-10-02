# 0001. One repository, one deployable backend

## Context

The PRD asks for a lightweight demo of gateway concepts and rules out microservices and Kubernetes.
The work must live in one top-level folder with `backend/`, `frontend/` and `docs/`.

## Decision

- A single repository: `backend/` (.NET solution), `frontend/` (Vite app), `docs/`, `scripts/`, and
  `docker-compose.yml` at the root.
- The backend is **one process** that serves both the management API (`/api`) and the gateway (`/gw`).
- Inside that process the code is split into four projects by layer ([0002](0002-layered-architecture.md)).

## Alternatives considered

- **Separate gateway and management services.** Closer to a production topology (the data plane scales
  and fails independently of the control plane), but it doubles the deployment and needs a shared
  contract for key lookups. Out of scope by the PRD.
- **A single .NET project.** Less ceremony, but nothing would stop a controller from talking to Redis
  directly; the layering would exist only by convention.

## Consequences

- One image to build, one thing to run, one log to read.
- Heavy gateway traffic and the dashboard share a process and a database connection pool.
- The gateway code depends only on Application ports, so moving it into its own host later means
  writing a new composition root, not rewriting the gates.
