# Database Migration Audit

Audited: 2026-04-27

## Status summary

Two migrations present, both applied cleanly. Auto-migration on startup is enabled by default. No pending migrations detected.

---

## Migration inventory

| Migration | File date | Description |
|---|---|---|
| `20260409145651_InitialCreate` | 2026-04-09 | Full initial schema — all domain entities, identity tables, data-protection keys |
| `20260422204122_Batch3WorkspacesAndFinance` | 2026-04-22 | Workspace and finance tables added |

Migration files live in `src/App.DAL.EF/Migrations/`.

---

## How migrations are applied

### Automatically at startup (default)

`AppDataInitExtensions.SetupAppDataAsync` calls `dbContext.Database.MigrateAsync()` when `DataInitialization:MigrateDatabase` is `true` (the default). This runs on every app start and is idempotent — it applies only migrations that have not yet been applied.

### Manually via script

```powershell
.\scripts\migrate-db.ps1
```

Starts the database container if needed, then runs:

```
dotnet ef database update --project src/App.DAL.EF --startup-project src/WebApp
```

### Manually via dotnet-ef directly

```
dotnet ef database update --project src/App.DAL.EF --startup-project src/WebApp
dotnet ef database update <MigrationName> --project src/App.DAL.EF --startup-project src/WebApp
```

`dotnet-ef` must be installed globally:

```
dotnet tool install -g dotnet-ef
```

---

## Adding a new migration

```
dotnet ef migrations add <MigrationName> --project src/App.DAL.EF --startup-project src/WebApp
```

---

## Rolling back

```
dotnet ef database update <PreviousMigrationName> --project src/App.DAL.EF --startup-project src/WebApp
dotnet ef migrations remove --project src/App.DAL.EF --startup-project src/WebApp
```

To fully wipe and re-seed (destructive):

```powershell
.\scripts\stop-db.ps1 -RemoveVolume
.\scripts\migrate-db.ps1
```

---

## Data seeding

`AppDataInit.SeedAsync` runs after migration when `DataInitialization:SeedData` is `true` (default). It is idempotent — seeds demo gyms, roles, and users only if they do not already exist.

Demo user passwords are reset on every startup regardless of seed state (see commit `8030eac`).

---

## Blocking issues found

None.
