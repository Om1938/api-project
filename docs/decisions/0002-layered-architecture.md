# 0002. Clean Architecture layering and the SOLID/DRY conventions

## Context

SOLID and DRY are explicit requirements. They need to be visible in the structure, not just claimed.

## Decision

Four projects with dependencies pointing inwards: `Api → Infrastructure → Application → Domain`.

- **Domain** has no dependencies. Entities protect their own invariants (private setters, methods such
  as `ApiKey.Revoke`, `Tier.Update`).
- **Application** holds one service per feature, the gateway gates and the ports they need
  (`Application/Abstractions`). Use cases return `Result` / `Result<T>` for expected failures instead of
  throwing; exceptions are reserved for bugs and outages.
- **Infrastructure** implements the ports (EF Core, Redis, JWT, HTTP, background workers).
- **Api** is the composition root and the HTTP adapter. Controllers contain no logic: each action is
  one call to a service and one mapping of its `Result`.

Two pragmatic choices within that:

1. **The persistence port is `IAppDbContext`, which exposes EF Core `DbSet`s**, rather than one
   repository interface per entity. Application therefore references `Microsoft.EntityFrameworkCore`
   (the abstraction, not a provider).
2. **Services are concrete classes**, not interfaces. Interfaces are introduced where there is a real
   second implementation or a boundary to fake in tests: the ports and `IRequestGate`.

The entity for a registered API is named `RegisteredApi`, because `Api` would collide with the
`ApiGateway.Api` namespace.

## Alternatives considered

- **Repository per aggregate.** Keeps EF out of Application entirely, at the cost of a pass-through
  class and interface for every entity and awkward homes for the aggregate queries analytics needs.
  For a demo this is duplication without benefit; `DbSet` already is a repository abstraction.
- **MediatR / CQRS handlers.** One class per use case is tidy but adds a dependency and indirection that
  nine small services do not need.
- **An interface for every service.** Would add nine interfaces with exactly one implementation each.

## Consequences

- Use cases can compose LINQ directly, which keeps analytics and list queries short.
- Application services are tested through the integration suite (real MySQL) rather than with a faked
  `DbSet`; the gates, which use only ports, are unit tested. See [0018](0018-testing-strategy.md).
- Swapping MySQL for another relational database is a provider change; swapping EF Core itself would
  touch Application.

How the principles map to concrete classes is tabulated in [architecture.md](../architecture.md).
