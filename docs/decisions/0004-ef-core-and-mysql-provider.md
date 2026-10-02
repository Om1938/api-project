# 0004. EF Core 9 with the Pomelo MySQL provider; migrations on startup

## Context

The database is MySQL. The backend targets .NET 10. At the time of writing, the Pomelo provider's
latest release (9.0.0) targets EF Core 9; there is no Pomelo release for EF Core 10.

## Decision

- **EF Core is pinned to 9.x** with `Pomelo.EntityFrameworkCore.MySql` 9.0.0, running on `net10.0`.
  Versions are managed centrally in `backend/Directory.Packages.props`.
- The MySQL server version is **pinned in code** (8.4) instead of auto-detected, so building the model
  and generating migrations never needs a live database.
- **Migrations are applied on startup** (`DatabaseInitializer`), followed by optional demo seed data.
- `DateTime` columns are read back as UTC and decimals default to `decimal(18,4)` through model-wide
  conventions in `AppDbContext`, not per property.
- `CreatedAt` is stamped by `AppDbContext` when an entity is first saved, from the injected
  `TimeProvider`, truncated to microseconds. MySQL `DATETIME(6)` stores microseconds, so without the
  truncation the value returned right after an insert would differ from the one read back later.
- MySQL `DATETIME` carries no time zone, so values come back with `Kind=Unspecified` and would be
  serialised without the `Z` suffix. A model-wide converter marks them as UTC, which is what is stored.
- Transient-failure retries are enabled on the provider.

## Alternatives considered

- **Oracle's `MySql.EntityFrameworkCore` 10.x.** Tracks EF Core 10, but Pomelo has the better track
  record for LINQ translation (date parts, flags), which the analytics queries rely on.
- **Dapper / raw SQL.** Full control, but hand-written mapping and SQL for a CRUD-heavy app works
  against DRY.
- **Applying migrations as a separate deploy step.** The correct choice for production with several
  instances; unnecessary friction for `docker compose up`.

## Consequences

- When Pomelo ships an EF Core 10 provider, the upgrade is a version bump in one file.
- Startup migration is not safe with multiple backend instances starting at once.
- `dotnet-ef` is a local tool (`backend/dotnet-tools.json`). To add a migration:
  `dotnet ef migrations add <Name> --project src/ApiGateway.Infrastructure --startup-project src/ApiGateway.Api --output-dir Persistence/Migrations`
