# Final-2 Modular Monolith Plan

**Status:** Phase 21 final hardening - Users auth/session, member CRUD,
Training, MembershipFinance, and maintenance/facility endpoint adapters are
mediated through module contracts. Public API routes are locked by regression
tests and the Final2 defense evidence pack is current.

This document is the master plan for the Final-2 grading shape: a **modular
monolith** built on top of the Final-1 Clean/Onion baseline. It is intentionally
phased so the foundation lands first (skeleton, contracts, DI extension points,
architecture tests), and feature migration happens later in small, reviewable,
reversible PRs.

For supporting context see:
- `docs/final2-defense.md` - final submission defense checklist and local validation snapshot.
- `docs/final2-module-boundary-report.md` - module dependency and ownership evidence.
- `docs/final2-test-traceability.md` - requirement-to-test mapping.
- `docs/final2-risk-report.md` - residual risks and hardening notes.
- `docs/final2-membershipfinance-module-plan.md` - Phase 20 MembershipFinance adapter plan.
- `docs/finance-mediator-messages.md` - MembershipFinance message map.
- `docs/final2-maintenance-module-plan.md` - Phase 20 maintenance adapter plan.
- `docs/maintenance-mediator-messages.md` - Maintenance message map.
- `docs/final1-clean-onion-plan.md` — current Clean/Onion shape.
- `docs/module-boundary-audit.md` — current Final1 services mapped to modules.
- `docs/module-data-ownership.md` — entity → module mapping.
- `docs/mediator-design.md` — cross-module dispatch pattern.
- `docs/final2-users-module-plan.md` - Phase 17 Users auth/session migration.
- `docs/users-module-contracts.md` - Users public mediator and HTTP contracts.
- `docs/users-mediator-messages.md` - Users auth/session message map.
- `docs/final2-gymmanagement-module-plan.md` - Phase 18 GymManagement member adapter plan.
- `docs/gymmanagement-module-contracts.md` - GymManagement public mediator and HTTP member contracts.
- `docs/gymmanagement-mediator-messages.md` - GymManagement member message map.
- `docs/architecture.md` — runtime surfaces and request flows.

---

## 1. Goals and non-goals

### Goals

- Introduce a per-module boundary inside the existing solution while keeping
  the single ASP.NET Core host and the same `AppDbContext`.
- Each module owns its **application services**, **mappers**,
  **module-internal contracts**, and **module DI registration**.
- Cross-module collaboration uses **published contracts** in
  `BuildingBlocks.Contracts` (DTOs, mediator request types, integration events).
- Cross-module **calls** go through the in-process mediator only — never
  through direct service-to-service references.
- Architecture tests fail fast on direct module-to-module type leaks.

### Non-goals (Phase 16)

- No business logic is moved into the module assemblies yet.
- No public API routes change.
- No DTO namespaces change.
- No additional database schemas, migrations, or DbContexts are introduced.
- The Final1 Clean/Onion test surface (`tests/WebApp.Tests/...`) is preserved.

---

## 2. Target shape

```
┌────────────────────────────────────────────────────────────────────┐
│ WebApp (Composition Root)                                          │
│   Program.cs calls AddUsersModule, AddGymManagementModule,         │
│   AddTrainingModule, AddMembershipFinanceModule, AddBuildingBlocks │
│   ApiControllers and MVC areas remain here (Final1 routes frozen). │
└──────────────┬─────────────────────────────────────────────────────┘
               │ uses module public surfaces (mediator + contracts)
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Modules (each its own assembly)                                     │
│                                                                     │
│  Modules.Users           Modules.GymManagement                      │
│  Modules.Training        Modules.MembershipFinance                  │
│                                                                     │
│   each module:                                                      │
│     • Public/  — module-public mediator request/response types      │
│     • Application/ — services, handlers, mappers (internal)         │
│     • ModuleServiceCollectionExtensions.cs — DI registration        │
│                                                                     │
│   modules ONLY reference:                                           │
│     • App.Domain                                                    │
│     • App.DTO                                                       │
│     • App.BLL (during transition; will shrink as logic moves out)   │
│     • BuildingBlocks                                                │
│   modules NEVER reference each other directly.                      │
└──────────────┬─────────────────────────────────────────────────────┘
               │ implements / depends on
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│ BuildingBlocks (shared abstractions)                                │
│   • IModule — module DI marker                                      │
│   • Mediator: IRequest, IRequest<T>, IRequestHandler<T>, IMediator  │
│   • Module-public contracts (cross-module DTO surface)              │
│   • Common policies: tenant context port, clock, paging             │
└─────────────────────────────────────────────────────────────────────┘
```

Existing layers stay in place during the migration:
- `App.Domain`, `App.DTO`, `App.BLL`, `App.DAL.EF`, `App.Resources` are
  unchanged in Phase 16.
- Module assemblies sit **alongside** `App.BLL` and progressively absorb
  vertical slices in subsequent phases.

---

## 3. Module catalogue

The four modules below match the slice work that already landed in Final1
(`final1-member-slice-plan.md`, `final1-training-slice-plan.md`,
`final1-membership-finance-slice-plan.md`,
`final1-maintenance-admin-slice-plan.md`, `final1-auth-slice-plan.md`).

| Module | Bounded context | Owns |
|---|---|---|
| **Users** | identity, roles, gym membership, account auth | `AppUser`, `AppRole`, `AppUserGymRole`, `RefreshToken`, `Person`, `Contact`, `PersonContact`; account login/logout/refresh, identity provisioning, role + gym assignment |
| **GymManagement** | tenant root, staff, equipment, maintenance, opening hours, gym settings, support tickets | `Gym`, `GymContact`, `GymSettings`, `OpeningHours`, `OpeningHoursException`, `Equipment`, `EquipmentModel`, `MaintenanceTask`, `MaintenanceTaskAssignmentHistory`, `Staff`, `JobRole`, `EmploymentContract`, `Vacation`, `WorkShift`, `SupportTicket`, `AuditLog` |
| **Training** | members in their training context, training categories, training sessions, bookings, coaching plans | `Member`, `TrainingCategory`, `TrainingSession`, `Booking`, `CoachingPlan`, `CoachingPlanItem` |
| **MembershipFinance** *(optional fourth module — landed in Phase 16)* | membership packages, memberships, payments, invoices, refunds, finance workspace | `MembershipPackage`, `Membership`, `Payment`, `Invoice`, `InvoiceLine`, `InvoicePayment`, `Subscription` |

> **Note on `Member`.** The `Member` entity is conceptually shared between
> `Users` (account-level identity) and `Training` (the in-gym member who books
> sessions). Final1 already settled the seam: `Member` is owned by **Training**
> (the booking/coaching surface). `Users` references the underlying `AppUser`
> identity via `AppUser.Id` and exposes a published `UserContextDto` that
> `Training` consumes through the mediator. See `module-data-ownership.md`.

### BuildingBlocks

- `BuildingBlocks/Modules/IModule.cs` — marker interface for module DI extensions.
- `BuildingBlocks/Mediator/{IRequest,IRequest<T>,IRequestHandler,IMediator,Mediator,MediatorRegistration}.cs`
  — minimal in-process mediator, no third-party dependency required for Final-2.
- `BuildingBlocks/Contracts/` — module-public mediator request/response DTOs.
  Anything posted across module boundaries lives here.

### Why an in-process mediator and not direct service references

- Direct references would force module-to-module project references and break
  the architecture tests.
- The mediator dispatches a request type → its registered handler. Handlers
  are private to the owning module; only request and response types are
  public. This keeps the seam explicit and easy to extract later (Final-3
  microservice extraction).
- We intentionally build a tiny mediator instead of pulling in MediatR. The
  course wants a defendable monolith, not a framework migration; one file
  with `IMediator` + scoped resolution is enough for Final-2.

---

## 4. Phase ordering

| Phase | Scope | Exit criteria |
|---|---|---|
| **16** | Module skeleton: `BuildingBlocks` + four module projects, mediator, DI extension, architecture tests | Solution builds; Final1 tests still green; modules contain no business logic; architecture tests forbid direct module-to-module references |
| 17 | Move auth/session behavior into `Modules.Users` for login, refresh, logout, switch gym, and switch role | `AccountController` dispatches Users mediator messages; public account routes stay stable |
| **18 (now)** | Move member CRUD endpoint adapters into `Modules.GymManagement` mediator messages | `MembersController` dispatches GymManagement mediator messages; member API and tenant isolation stay stable; packages remain finance-owned |
| 19 | Move training slice (categories, sessions, bookings, work shifts) into `Modules.Training` | Training controllers resolved from module DI; cross-module reads go through mediator |
| 20 | Move membership/finance into `Modules.MembershipFinance` | Package, membership, payment, invoice, and finance workspace controllers served by module |
| 21 | Move remaining gym/staff/maintenance/equipment workflows into `Modules.GymManagement` | All admin/maintenance controllers served by module |
| 21 | Shrink `App.BLL` to legacy/cross-cutting only; tighten architecture tests; document defense story | `App.BLL` no longer hosts module-owned services |

Each phase is sized for a single PR. Each is reversible.

---

## 5. What Phase 16 lands

| Item | Where |
|---|---|
| BuildingBlocks project | `src/BuildingBlocks/BuildingBlocks.csproj` |
| Mediator abstractions | `src/BuildingBlocks/Mediator/*.cs` |
| Module marker | `src/BuildingBlocks/Modules/IModule.cs` |
| Cross-module contracts namespace | `src/BuildingBlocks/Contracts/` (empty placeholder) |
| Users module | `src/Modules.Users/...` |
| GymManagement module | `src/Modules.GymManagement/...` |
| Training module | `src/Modules.Training/...` |
| MembershipFinance module | `src/Modules.MembershipFinance/...` |
| DI extension per module | `Modules.<Name>/<Name>ModuleServiceCollectionExtensions.cs` |
| Modules wired in startup | `src/WebApp/Setup/ModuleExtensions.cs`, called from `Program.cs` |
| Architecture tests | `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs` |

What does **not** change in Phase 16:

- `Program.cs` still calls every existing `AddAppXxx` extension.
- Every existing controller still resolves the same services it does today.
- No DTO is moved.
- No EF mapping is moved.
- No public route is moved or renamed.

---

## 6. Module DI registration pattern

Every module exposes a single static extension method:

```csharp
namespace Modules.Users;

public static class UsersModuleServiceCollectionExtensions
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // 1. mediator handlers in this module
        services.AddModuleMediatorHandlersFromAssembly(typeof(UsersModuleServiceCollectionExtensions).Assembly);

        // 2. module-internal application services (added in later phases)

        return services;
    }
}
```

`Program.cs` (or a thin `WebApp/Setup/ModuleExtensions.cs`) calls each
`AddXxxModule` exactly once. The composition root remains the only place that
knows about the full module set.

---

## 7. Mediator contract summary

```csharp
public interface IRequest { }
public interface IRequest<TResponse> { }

public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
```

Implementation lives in `BuildingBlocks/Mediator/Mediator.cs` and is registered
once via `services.AddBuildingBlocksMediator()`. See `mediator-design.md` for
naming, registration, and per-module-handler scanning rules.

---

## 8. Architecture tests added in Phase 16

In `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`:

1. **Each module assembly does NOT reference any other module assembly.**
2. **Each module assembly references BuildingBlocks.**
3. **WebApp may reference modules and BuildingBlocks** (composition root).
4. **BuildingBlocks does NOT reference modules.**
5. **Module DI extension methods exist** — `AddUsersModule`,
   `AddGymManagementModule`, `AddTrainingModule`, `AddMembershipFinanceModule`.
6. **Mediator interfaces live in BuildingBlocks.**

These rules are enforced via `Assembly.GetReferencedAssemblies()` and reflection
checks; no third-party arch-test package is required for Final-2.

---

## 9. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Refactor pressure tempts mid-phase logic moves | Phase 16 explicitly lands no logic moves. Logic moves are scoped per follow-up phase. |
| Cross-module reads (e.g. Training needs gym name) become awkward | Use mediator queries; if a query is read-heavy and crosses many modules, consider an explicit read-model in BuildingBlocks. |
| Tests break because of assembly-load timing | Module assemblies are referenced from `WebApp.csproj` so xUnit loads them; mediator handler scan uses already-loaded assemblies. |
| Defense narrative gets noisy | This document plus `module-boundary-audit.md` and `module-data-ownership.md` form the defense bundle. Diagrams included. |

---

## 10. Definition of done for Phase 16

- [x] `docs/final2-module-plan.md` (this file)
- [x] `docs/module-boundary-audit.md`
- [x] `docs/module-data-ownership.md`
- [x] `docs/mediator-design.md`
- [x] Four module skeletons + BuildingBlocks under `src/`
- [x] Mediator wired in startup
- [x] DI extension method per module
- [x] Architecture tests forbidding direct module-to-module references
- [x] `dotnet build` succeeds
- [x] Existing `WebApp.Tests` suite passes
- [x] No public API route changes
