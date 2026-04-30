# Repository / Unit of Work Contracts

**Status:** in-place for Phase 9 — services not yet migrated.

This document defines the persistence ports declared in
`App.BLL.Contracts.Persistence` and how `App.DAL.EF.Repositories` implements
them. It also nails down where mappers live.

---

## 1. Goal

Give the application layer a way to load, query, and persist domain entities
that does **not** leak EF Core types (`DbSet<>`, `IQueryable<>`, `Include<>`,
`AsNoTracking()`). Once every BLL service routes through these ports, EF Core
becomes a swap-out infrastructure detail.

Phase 9 introduces the contracts and a default EF implementation. Service
migration is intentionally deferred so this PR stays small and risk-free.

---

## 2. `IRepository<TEntity, TKey>`

```csharp
namespace App.BLL.Contracts.Persistence;

public interface IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : struct
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
```

### What's deliberately not here

- **No `SaveChangesAsync`** — that belongs on the unit of work.
- **No `IQueryable`** — leaks EF; instead callers either pass a predicate, or
  ask the UoW for a typed read-model service in a later phase.
- **No `Include` / eager-load API** — too EF-shaped. When an aggregate boundary
  needs eager loads, expose a typed read method on a focused repository
  interface that derives from `IRepository<,>` (e.g.
  `IMembershipPackageRepository : IRepository<MembershipPackage, Guid>` adds
  `GetWithMembershipsAsync`).
- **No tracking control** — `IRepository` is "load by id, list by predicate".
  EF tracks. If a BLL caller wants a no-track read, the convention is to
  expose a dedicated read-model method on a derived repository interface
  rather than parameterizing every call.

### Why `TKey : struct`

All current entities key on `Guid`. Restricting `TKey` to value types prevents
an accidental `string`-keyed entity slipping through unnoticed. If a future
identity primitive changes (e.g. the audit log keying on a long), relax the
constraint.

---

## 3. `IAppUnitOfWork`

```csharp
namespace App.BLL.Contracts.Persistence;

public interface IAppUnitOfWork
{
    IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Resolution semantics:

- `Repository<TEntity>()` returns the **same instance** for the same entity
  type within the lifetime of the UoW (one cached `EfRepository<>` per
  entity). This keeps tracker state aligned with the surrounding `DbContext`.
- `SaveChangesAsync` simply forwards to the underlying `DbContext.SaveChangesAsync`
  — including the existing audit-log + soft-delete hooks in
  `AppDbContext.SaveChangesAsync`.
- The UoW's lifetime is **scoped** (matches the EF `DbContext` scope).

### Future extensions (out of scope for Phase 9)

- `Task<IAppTransaction> BeginTransactionAsync(CancellationToken)` once a
  service genuinely needs explicit transactions. Today every workflow service
  fits in the single `SaveChangesAsync` envelope.
- Specialized repository getters (`Members`, `MembershipPackages`, …) for
  call sites that need eager-loading. We add them on demand, not preemptively.

---

## 4. EF implementations (`App.DAL.EF.Repositories`)

### `EfRepository<TEntity, TKey>`

- Wraps `DbContext.Set<TEntity>()`.
- `FindAsync` uses `DbSet.FindAsync` (uses tracker if loaded).
- `ListAsync(null)` materializes the whole set; with a predicate, applies
  `Where`. No tracking is left as the EF default (tracked) — callers that
  want untracked reads must use a future read-model API.
- `ExistsAsync` uses `AnyAsync`.
- `AddAsync`, `Update`, `Remove` thin wrappers over `DbSet` equivalents.

### `EfAppUnitOfWork`

- Constructor takes `AppDbContext`.
- Caches repositories in `ConcurrentDictionary<Type, object>` keyed by entity
  type.
- `SaveChangesAsync` calls through to `AppDbContext.SaveChangesAsync` so the
  existing audit-log + soft-delete pipeline runs unchanged.

### Why repositories live in `App.DAL.EF.Repositories` and not in `App.BLL`

Repository **interfaces** are a port (Application layer). Repository
**implementations** are an adapter (Infrastructure layer). Putting EF code
inside `App.BLL` would bring back the dependency we're trying to remove.

---

## 5. DI registration

A new extension method on `App.DAL.EF`:

```csharp
namespace App.DAL.EF;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddAppPersistence(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IAppUnitOfWork, EfAppUnitOfWork>();
        return services;
    }
}
```

Called from `WebApp.Setup.DatabaseExtensions.AddAppDatabase` next to the
existing `AddDbContext<AppDbContext>` registration — this is the
composition-root layer talking to infrastructure, not BLL.

`IAppDbContext` registration stays in place. Both APIs coexist until every
service migrates.

---

## 6. Mapper rule

**Rule:** entity ↔ DTO mappers live in `App.BLL/Mapping`.

- They sit in BLL because they translate between **Domain** entities and
  **DTOs**, both of which BLL already references — `App.DAL.EF` does not
  reference `App.DTO`, so it cannot host mappers.
- They are `internal static class …Mapping` with extension or static methods
  named `To<Target>(this <Source>)`. `MembershipWorkflowMapping` already
  follows this shape and stays where it is.
- A mapper **never** touches `DbSet` or `IQueryable` and **never** issues IO
  itself. It is a pure projection.
- A mapper that only translates between Domain and Domain (rare) lives in
  `App.Domain` itself. None exist today.
- DTO ↔ View-model translations stay in `WebApp` because View Models live in
  the presentation layer. They are not "mappers" in the BLL sense.

This rule is locked by the architecture test
`Mappers_LiveOnlyInBllMappingNamespace`.

---

## 7. What this enables in later phases

1. Migrate BLL services one at a time: each service drops its
   `using Microsoft.EntityFrameworkCore;` and switches from
   `dbContext.SomeSet` → `unitOfWork.Repository<T>()`.
2. Once the last service is converted, drop the `Microsoft.EntityFrameworkCore`
   package reference from `App.BLL.csproj` and tighten the architecture test
   from "BLL must not reference EF Core packages" → "BLL must not reference
   EF Core types at all" (already covered).
3. Migrate MVC controllers and view components to BLL services / read-models
   the same way.
4. Then revisit the Identity-on-Domain leak (V1 in `dependency-audit.md`).
