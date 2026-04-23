# Study Guide: DAL and EF Core

## Purpose
`src/App.DAL.EF` provides persistence, EF mappings, migrations, and seed initialization for the SaaS system.

## `AppDbContext` Responsibilities
- Register all DbSets used by platform and tenant modules.
- Configure precision, indexes, relationships, and enum-backed state fields.
- Apply tenant-aware query filters for soft-deleted business records.
- Persist audit and translation-friendly value shapes.

## Tenant Isolation in Persistence
- Tenant entities are queried with `GymId` invariants from BLL.
- Soft-delete query filters hide logically deleted rows by default.
- Tests in `AppDbContextBehaviorTests` verify both audit writes and soft-delete filtering.

## Migrations
- EF code-first migrations are under `src/App.DAL.EF/Migrations`.
- Batch 3 migration introduced coaching, finance, and maintenance history tables.

## Seeding
- `Seeding/AppDataInit*.cs` initializes:
  - platform/system users
  - gym demo tenants
  - realistic tenant demo data for members, sessions, bookings, memberships, finance, coaching, and maintenance

## Defense Notes
- Data Protection keys are persisted in the same database for stable auth cookies/tokens across container restarts.
- Reverse-proxy and deployment behavior rely on seeded, migratable schema consistency.
