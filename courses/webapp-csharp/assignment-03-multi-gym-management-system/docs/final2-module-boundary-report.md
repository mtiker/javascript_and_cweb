# Final2 Module Boundary Report

## Summary

The Final2 modular monolith is implemented as one ASP.NET Core host with four
domain modules and one shared building-blocks assembly:

- `Modules.Users`
- `Modules.GymManagement`
- `Modules.Training`
- `Modules.MembershipFinance`
- `BuildingBlocks`

This exceeds the course requirement of Users plus two business modules.

This report describes logical module ownership and code-level boundaries. It
does not claim full module isolation, separate schemas, separate DbContexts, or
microservice extraction.

## Project References

Allowed dependency direction:

```text
WebApp
  -> BuildingBlocks
  -> Modules.Users
  -> Modules.GymManagement
  -> Modules.Training
  -> Modules.MembershipFinance

Modules.*
  -> BuildingBlocks
  -> App.Domain
  -> App.DTO
  -> App.BLL (transitional compatibility during modularization)

BuildingBlocks
  -> no module projects
```

Forbidden:

- `Modules.Users -> Modules.Training`
- `Modules.Training -> Modules.Users`
- `Modules.Training -> Modules.GymManagement`
- `Modules.MembershipFinance -> Modules.Training`
- any other direct module-to-module project reference

Enforced by:

- `ModuleArchitectureTests.EveryModule_DoesNotReferenceAnyOtherModule`
- `ModuleArchitectureTests.BuildingBlocks_DoesNotReferenceAnyModuleOrWebApp`
- `ModuleArchitectureTests.WebApp_ReferencesEveryModule`

## Data Ownership

| Module | Owns |
|---|---|
| Users | identity, user sessions, refresh tokens, people/contact records used by login-capable actors |
| GymManagement | tenant root, staff, contracts, work shifts, equipment, maintenance, opening hours, settings, support, audit |
| Training | gym members, training catalog, sessions, bookings, coaching plans |
| MembershipFinance | sellable packages, memberships, payments, invoices, refunds, tenant subscription records |

The complete entity map is in `docs/module-data-ownership.md`.

## Mediator Communication

Cross-module and WebApp-to-module calls use `BuildingBlocks.Mediator`:

- request interfaces: `IRequest`, `IRequest<TResponse>`
- handler interfaces: `IRequestHandler<TRequest>`,
  `IRequestHandler<TRequest,TResponse>`
- dispatcher: `IMediator`

Module handlers are registered by each module DI extension via
`AddModuleMediatorHandlersFromAssembly`. `WebApp` only needs `IMediator` and the
published request/response contracts.

Evidence:

- `src/BuildingBlocks/Mediator/*`
- `src/WebApp/Setup/ModuleExtensions.cs`
- `src/Modules.Users/Contracts/AuthSessionMessages.cs`
- `src/Modules.GymManagement/Contracts/*Messages.cs`
- `src/Modules.Training/Contracts/TrainingMessages.cs`
- `src/Modules.MembershipFinance/Contracts/FinanceMessages.cs`
- `tests/WebApp.Tests/Unit/*ModuleMediatorTests.cs`

## Current Transitional Boundary

Some module handlers still delegate to existing `App.BLL` workflow services.
That is intentional for this phase:

- public behavior stays stable
- route and DTO contracts do not move
- module projects own HTTP adapter/message boundaries first
- business logic can be moved from `App.BLL` into modules later without API churn

Training category CRUD is the first Training workflow moved beyond adapter
ownership: its handlers own authorization, tenant-scoped repository access,
validation, localization write-culture handling, persistence, and mapping
inside `Modules.Training.Application`.

Membership package CRUD is now the equivalent MembershipFinance-owned workflow:
its handlers own authorization, tenant-scoped package repository access,
validation/normalization, used-package conflict checks, persistence, and mapping
inside `Modules.MembershipFinance.Application`.

GymManagement member CRUD is mediated through the GymManagement module, but its
current handlers intentionally reuse the shared member workflow service. The
same transitional rule applies to maintenance/facility messages and to the
remaining Training and MembershipFinance workflows that are not called out
above as module-owned internally.

The improvement is therefore:
- module projects and public contracts exist for the bounded contexts
- WebApp controllers use mediator dispatch for migrated slices
- no module project directly references another module project
- selected workflows now own their orchestration inside module handlers

The remaining limitation is:
- many handlers still depend on shared BLL services and the shared Unit of Work
- the single `AppDbContext` remains the persistence boundary
- module data ownership is documented and guarded by architecture tests, but
  not yet enforced by physical database/schema separation

The risk is tracked in `docs/final2-risk-report.md`.

## CI Enforcement

The assignment child pipeline runs:

```bash
dotnet test multi-gym-management-system.slnx --configuration Release --no-build
```

Because `ModuleArchitectureTests` and `ArchitectureTests` are part of
`tests/WebApp.Tests`, boundary regressions fail CI.

Latest local verification on 2026-05-11:
- `dotnet test multi-gym-management-system.slnx` passed with 250 passed and
  3 skipped tests.
- The skipped tests were the opt-in PostgreSQL/Testcontainers slice, not the
  module architecture tests.
