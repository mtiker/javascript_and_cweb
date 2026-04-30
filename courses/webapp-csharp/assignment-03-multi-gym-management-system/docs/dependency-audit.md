# Dependency Audit (CLEAN/ONION)

**Audited:** 2026-04-28

This audit captures the project-reference graph and surfaces every concrete
violation of the inward-only dependency rule that CLEAN/ONION mandates.

---

## 1. Reference rule

In CLEAN/ONION the dependency arrows must point **inward only**:

```
WebApp (composition root, MVC, REST)
   │     ▲
   ▼     │ (DI only)
App.DAL.EF (Infrastructure)
   │
   ▼
App.BLL (Application — services, ports, mappers)
   │
   ▼
App.DTO  ───►  App.Domain (innermost: entities, value objects)
                 ▲
                 └── App.Resources (cross-cutting strings)
```

- `App.Domain` references nothing in this solution.
- `App.DTO` references `App.Domain` only.
- `App.BLL` references `App.Domain` and `App.DTO` only — and **may not depend
  on `Microsoft.EntityFrameworkCore` or any other infrastructure package**.
- `App.DAL.EF` references `App.BLL` and `App.Domain` (it implements the
  ports declared in BLL — DAL → BLL is correct in ONION).
- `WebApp` references everything (composition root). It is allowed to use
  `App.DAL.EF` only for DI registration; runtime code paths in
  controllers/services should use BLL ports.
- Tests reference what they assert against.

---

## 2. Project references today (`src/*.csproj`)

| Project | Project references | Package references (relevant) |
|---|---|---|
| `App.Domain` | — | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| `App.DTO` | `App.Domain` | — |
| `App.BLL` | `App.Domain`, `App.DTO` | **`Microsoft.EntityFrameworkCore`**, `System.IdentityModel.Tokens.Jwt`, `FrameworkReference Microsoft.AspNetCore.App` |
| `App.DAL.EF` | `App.BLL`, `App.Domain` | EF Core (Design, Tools, Npgsql), Identity.EntityFrameworkCore, DataProtection.EntityFrameworkCore |
| `App.Resources` | — | — |
| `WebApp` | `App.BLL`, `App.DAL.EF`, `App.DTO`, `App.Domain`, `App.Resources` | ASP.NET Core, EF Core (Tools), Npgsql, Swashbuckle |
| `WebApp.Tests` | `WebApp`, `App.DAL.EF`, `App.Domain`, `App.BLL` | xunit, EF InMemory, Testcontainers.PostgreSql |

Project-reference direction itself is correct (no inward project-ref points
from inner to outer). The violations are at the **package and code level**.

---

## 3. Violations to fix incrementally

### V1. `App.Domain` references `Microsoft.AspNetCore.Identity.EntityFrameworkCore`

`App.Domain.csproj:9` pulls in EF Core (transitively, via Identity). Domain
should not know about Identity Stores. Today the leak is small (the package is
referenced for the Identity primitive types in `App.Domain/Identity/*`).

**Plan:** keep for now. Identity types live in Domain because they are core
domain concepts (`AppUser`, `AppRole`, `AppRefreshToken`) and decoupling them
from `Microsoft.AspNetCore.Identity` requires a separate identity-domain pass.
Tracked as a follow-up rather than a Phase 9 deliverable.

### V2. `App.BLL` package references `Microsoft.EntityFrameworkCore`

`App.BLL.csproj:13`. BLL pulls EF Core in for the existing `IAppDbContext`
contract that exposes `DbSet<TEntity>` and for inline LINQ in services.

**Why it is a violation:** Application code becomes coupled to EF Core types
(`DbSet<>`, `IQueryable<>`, `Include<>`). Swapping EF for another ORM would
require touching every BLL service.

**Plan:** introduce repository + UoW interfaces in BLL that do **not** expose
EF types; migrate services file-by-file in later phases. The EF package
reference stays in BLL until the last service stops using `IQueryable` and
`Include` — see `final1-clean-onion-plan.md`.

### V3. `App.BLL` framework reference `Microsoft.AspNetCore.App`

`App.BLL.csproj:9-11`. Pulls the entire ASP.NET Core shared framework into
BLL (used by `IdentityService` + `AuthorizationService` via
`UserManager<>`, `RoleManager<>`, `IAuthorizationService`).

**Plan:** acceptable for an Identity-using application. ASP.NET Core Identity
is a cross-cutting service surface; isolating it behind a port is a separate
deliverable. The architecture test asserts that **EF Core** is forbidden in
BLL but allows the AspNetCore.App framework reference for now.

### V4. BLL services using EF Core APIs directly

19 of 25 service files in `src/App.BLL/Services` `using Microsoft.EntityFrameworkCore;`.
They call `Include`, `ToListAsync`, `FirstOrDefaultAsync`, etc. directly on
the `IAppDbContext.DbSet<T>` properties.

**Plan:** these stay until each service is migrated to repositories. Phase 9
introduces only the **contracts** and **EF implementations**; the migration
itself is deferred.

### V5. MVC controllers depending on `AppDbContext` directly

Ten MVC controllers (Areas/Admin and Areas/Client) plus `HomeController` take
`AppDbContext` in their primary constructor and execute LINQ inline. See
`controller-dbcontext-audit.md` for the full table.

API controllers under `WebApp.ApiControllers.*` are clean — they only depend
on BLL services. Only the MVC side is non-compliant.

**Plan:** MVC migration is intentionally out of scope for Phase 9. The
architecture test for "no controller depends on `AppDbContext` or
`IAppDbContext`" is added but **scoped to API controllers only** for now.
A follow-up phase widens the scope after MVC controllers are migrated.

---

## 4. What Phase 9 changes

| Change | Why |
|---|---|
| Add `App.BLL.Contracts.Persistence.IRepository<TEntity, TKey>` | Establish a port that hides EF from BLL services |
| Add `App.BLL.Contracts.Persistence.IAppUnitOfWork` | Bundle named repositories + `SaveChangesAsync` + (later) transaction control |
| Add `App.DAL.EF.Repositories.EfRepository<TEntity, TKey>` | Default EF adapter |
| Add `App.DAL.EF.Repositories.EfAppUnitOfWork` | Concrete UoW that hands out cached `EfRepository` instances per entity type |
| Add DI extension `services.AddRepositories()` in `App.DAL.EF` | Composition-root wires the new ports |
| Add architecture tests in `WebApp.Tests/Architecture/` | Lock the rule set in CI: Domain has no deps; BLL has no EF Core; API controllers have no `DbContext` |

What Phase 9 explicitly does **not** change:
- No service is migrated to use the new repositories — they continue using `IAppDbContext` until later phases.
- No controller behavior changes.
- No public API contracts change.
- No modules are introduced.

---

## 5. Future-phase backlog (out of scope here)

1. Migrate every BLL service from `IAppDbContext.<DbSet>` to
   `IAppUnitOfWork.<Repository>` access. Drop `using Microsoft.EntityFrameworkCore;`
   from each service as it is converted.
2. Migrate MVC controllers to BLL services / read-models; remove `AppDbContext`
   constructor parameters.
3. Once all services are migrated, drop the `Microsoft.EntityFrameworkCore`
   package from `App.BLL.csproj` and tighten the architecture test from
   "warning" to "fatal" for that case (already fatal in the planned tests).
4. Tighten the no-DbContext rule from "API controllers" to "all controllers".
5. Consider extracting Identity entities behind an `IdentityUser` adapter so
   `App.Domain` no longer references `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
