# Final-1 CLEAN/ONION Migration Plan

**Status:** Phase 9 (foundation) — boundary contracts and architecture tests
landed. Service / controller migration deferred to later phases.

**Phase 10 update:** the auth clean slice is now implemented. Account
login/logout/refresh-token behavior uses `IAccountAuthService`,
`IRefreshTokenRepository`, `IAppUnitOfWork`, and `AuthResponseMapper`; public
account routes and DTOs are unchanged. See `docs/final1-auth-slice-plan.md`,
`docs/auth-service-boundary-audit.md`, and
`docs/refresh-token-repository-contract.md`.

**Phase 11 update:** the member clean slice is now implemented. Member CRUD
behavior uses `IMemberWorkflowService`, `IMemberRepository`, `IAppUnitOfWork`,
and `IMemberMapper`; public member routes, MVC Admin pages, and React member
pages are unchanged. See `docs/final1-member-slice-plan.md`,
`docs/member-repository-contract.md`, and `docs/member-mapper-audit.md`.

This is the master plan for getting the assignment to the **mandatory
CLEAN/ONION shape** required by the Final-1 grading criteria. It is
intentionally phased: the foundation lands now without disturbing runtime
behavior, and each subsequent migration is small, reviewable, and reversible.

For the underlying audits and contracts, see:
- `docs/dependency-audit.md`
- `docs/controller-dbcontext-audit.md`
- `docs/repository-uow-contract-plan.md`

---

## 1. Target architecture

```
┌─────────────────────────────────────────────────────────────┐
│ WebApp (Presentation, Composition Root)                     │
│   ApiControllers, MVC Areas, Views, ViewComponents          │
│   Setup/* — DI registrations                                │
└──────────────┬──────────────────────────────────────────────┘
               │  uses BLL ports only at runtime
               ▼
┌─────────────────────────────────────────────────────────────┐
│ App.DAL.EF (Infrastructure)                                 │
│   AppDbContext, Configurations, Migrations, Seeding,        │
│   Repositories, EfAppUnitOfWork                             │
└──────────────┬──────────────────────────────────────────────┘
               │  implements IRepository<,>, IAppUnitOfWork
               ▼
┌─────────────────────────────────────────────────────────────┐
│ App.BLL (Application)                                       │
│   Services (use cases) — CoachingPlanService etc.           │
│   Contracts/Persistence — IRepository<,>, IAppUnitOfWork    │
│   Contracts/Infrastructure — IAppDbContext (legacy port)    │
│   Mapping — entity ↔ DTO mappers                            │
│   Exceptions                                                │
└──────────────┬──────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│ App.DTO  ─────────────►  App.Domain  ◄──── App.Resources    │
│  request/response          entities, value objects,         │
│  shapes                    enums, identity types            │
└─────────────────────────────────────────────────────────────┘
```

Inward-only references:
- Domain has no project references.
- DTO references Domain.
- BLL references Domain + DTO.
- DAL.EF references BLL + Domain (implements BLL ports).
- WebApp references everything (composition root).

---

## 2. What Phase 9 lands

| Item | Where |
|---|---|
| Repository contract | `src/App.BLL/Contracts/Persistence/IRepository.cs` |
| Unit of Work contract | `src/App.BLL/Contracts/Persistence/IAppUnitOfWork.cs` |
| EF repository implementation | `src/App.DAL.EF/Repositories/EfRepository.cs` |
| EF UoW implementation | `src/App.DAL.EF/Repositories/EfAppUnitOfWork.cs` |
| DI extension | `src/App.DAL.EF/PersistenceServiceExtensions.cs` |
| Mapping namespace marker | `src/App.BLL/Mapping/` (folder ready; existing `MembershipWorkflowMapping` not moved yet — see §6) |
| Architecture tests | `tests/WebApp.Tests/Architecture/ArchitectureTests.cs` |

DI is wired in `WebApp/Setup/DatabaseExtensions.cs` next to the existing
`AddDbContext` call so callers can inject `IAppUnitOfWork` today even though
nothing does yet.

What does **not** change in Phase 9:

- No BLL service is rewritten.
- No controller behavior changes.
- No public API contracts change.
- No modules are introduced.
- The legacy `IAppDbContext` port stays in place.

---

## 3. Phase ordering after Phase 9

Phase 10 was redirected to the auth clean slice because account login,
refresh-token rotation, refresh-token reuse rejection, logout invalidation, and
the public auth DTO mapper are higher-risk Final1 boundary evidence than the
membership service migration. The membership/UOW migration remains the next
data-heavy service phase.

| Phase | Scope | Exit criteria |
|---|---|---|
| **9 (now)** | Foundation contracts, EF adapters, architecture tests, audits | Tests for forbidden deps green; DI resolves `IAppUnitOfWork`; no behavior change |
| 10 | Migrate `MembershipPackageService` and `MembershipService` to `IAppUnitOfWork` | Those two services drop `using Microsoft.EntityFrameworkCore;`; integration tests still green |
| 11 | Migrate workflow services (Coaching, Maintenance, Member, Training, Staff, Booking, Payment) | Same pattern; 7 services converted |
| 12 | Migrate workspace + platform services (Finance, Member workspace, Platform, Subscription tier, Resource auth, Tenant access) | All BLL services off `IAppDbContext` |
| 13 | Drop `Microsoft.EntityFrameworkCore` package from `App.BLL.csproj`; remove `IAppDbContext` | Architecture test tightened to forbid the package outright |
| 14 | Migrate MVC controllers + `WorkspaceSwitcherViewComponent` to BLL services / read-models | `controller-dbcontext-audit.md` table empty; widen architecture test to all controllers |
| 15 | Identity decoupling — extract `App.Domain.Identity` from `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package | Domain has zero infra package refs |

Each phase is sized for a single PR. Each one is reversible: revert the PR
and the previous phase's tests still hold.

---

## 4. Architecture tests in scope today

`tests/WebApp.Tests/Architecture/ArchitectureTests.cs` (added in Phase 9):

| Test | Asserts |
|---|---|
| `DomainAssembly_DoesNotReferenceForbiddenAssemblies` | `App.Domain` has no transitive ref to `App.BLL`, `App.DAL.EF`, `App.DTO`, `WebApp` |
| `DtoAssembly_DoesNotReferenceForbiddenAssemblies` | `App.DTO` has no ref to `App.BLL`, `App.DAL.EF`, `WebApp` |
| `BllAssembly_DoesNotReferenceDalOrInfrastructure` | `App.BLL` has no project ref to `App.DAL.EF` or `WebApp`, and **no** package ref to EF Core providers/relational |
| `DalEfAssembly_DoesNotReferenceWebApp` | `App.DAL.EF` has no ref to `WebApp` |
| `ApiControllers_DoNotDependOnDbContext` | every `WebApp.ApiControllers.*` `ControllerBase` constructor is free of `DbContext` / `IAppDbContext` parameters |
| `Mappers_LiveOnlyInBllMappingNamespace` | any type whose name ends in `Mapping` or `Mapper` lives under `App.BLL.Mapping` (or the legacy `App.BLL.Services` namespace until §6 moves it) |

The tests are intentionally lenient where Phase 9 cannot tighten yet (e.g. BLL
still has the EF Core package reference). Each later phase tightens one
assertion.

---

## 5. Risks and mitigations

| Risk | Mitigation |
|---|---|
| EF tracker desync between `IAppDbContext` and `IAppUnitOfWork` running in the same scope | Both bind to the same scoped `AppDbContext` instance — `EfAppUnitOfWork(AppDbContext)` ctor; `IAppDbContext` is `provider.GetRequiredService<AppDbContext>()`. One `DbContext`, one tracker. |
| Architecture tests turning into a maintenance tax | Tests are coarse (assembly-level for refs, namespace-pattern for mappers). They fail on real boundary breaks, not stylistic drift. |
| Mapper rule clashing with already-shipped `MembershipWorkflowMapping` | Test allows `App.BLL.Services` until §6 migration; tightens later. |
| Future contributors adding repositories without UoW | `IAppUnitOfWork.Repository<T>()` is the only surface registered for repositories at injection time; standalone `IRepository<,>` is registered open-generic so direct injection still works for narrow use cases. |

---

## 6. Mapper migration follow-up

Existing mappers live in `App.BLL.Services` (e.g. `MembershipWorkflowMapping`).
The Phase 9 architecture test allows this namespace **and** the target
`App.BLL.Mapping`. A small later PR moves them under `App.BLL/Mapping/` and
the test tightens to "Mapping namespace only".

This split keeps the boundary work green now and the namespace move clean.

---

## 7. Out of scope (so reviewers don't ask)

- Migrating any BLL service or MVC controller.
- Removing `IAppDbContext` or the EF Core package from BLL.
- Splitting BLL into `Application` + `BLL` projects.
- Introducing modules / vertical slices.
- Replacing the manual mapper pattern with AutoMapper / Mapster.
- Identity decoupling.
