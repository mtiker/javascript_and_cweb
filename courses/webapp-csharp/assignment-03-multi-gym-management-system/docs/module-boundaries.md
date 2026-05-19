# Module Boundaries

This document is the Final2 source of truth for module ownership and
modular-monolith posture.

## Boundary Rules

- `WebApp` is the composition root and may reference all modules.
- `BuildingBlocks` owns shared module abstractions and the in-process mediator.
- Module projects may reference `BuildingBlocks`, `App.Domain`, `App.DTO`, and
  transitional `App.BLL` contracts/services.
- Module projects must not reference each other directly.
- Public HTTP routes and DTOs stay in place during module migration.
- Cross-module or WebApp-to-module calls go through `IMediator` request types.
- A single PostgreSQL database and a single `AppDbContext` remain in use for
  this assignment phase.

## Projects

| Project | Purpose |
|---|---|
| `BuildingBlocks` | `IModule`, `IMediator`, request/handler interfaces, handler registration |
| `Modules.Users` | auth session, account context, role and gym switching |
| `Modules.GymManagement` | tenant root, members, staff, equipment, maintenance, settings |
| `Modules.Training` | training categories, sessions, bookings, attendance |
| `Modules.MembershipFinance` | packages, memberships, payments |

## Entity Ownership

| Module | Owns |
|---|---|
| Users | `AppUser`, `AppRole`, `AppUserGymRole`, `AppRefreshToken`, `Person`, `Contact`, `PersonContact` |
| GymManagement | `Gym`, `GymContact`, `GymSettings`, `EquipmentModel`, `Equipment`, `MaintenanceTask`, `Staff` |
| Training | `TrainingCategory`, `TrainingSession`, `Booking` |
| MembershipFinance | `MembershipPackage`, `Membership`, `Payment` |
| Shared tenant identity | `Member` is a gym business profile used by several workflows; current APIs expose member CRUD through GymManagement while training and finance read it through application contracts. |

## Current Implementation Status

| Module | Current implementation | Boundary quality |
|---|---|---|
| Users | Account session commands route through mediator handlers and `UsersSessionService`. | Partial. The service still depends on `IAppDbContext` for gym-role and gym lookups. |
| GymManagement | Member, staff, equipment, maintenance, settings, and gym-user messages exist. Several handlers delegate to shared BLL workflow services. | Partial. Good mediator surface; weak internal ownership. |
| Training | Category CRUD is owned directly by module handlers using UOW/repositories/mappers. Session, booking, and attendance handlers still delegate to shared BLL services. | Strongest for category CRUD, partial for the rest. |
| MembershipFinance | Package CRUD is owned directly by module handlers using UOW/repositories/mappers. Membership and payment handlers are mediated but still transitional. | Strongest for package CRUD, partial for the rest. |

## Mediator Contract Files

- Users: `src/Modules.Users/Contracts/AuthSessionMessages.cs`
- GymManagement members: `src/Modules.GymManagement/Contracts/MemberMessages.cs`
- GymManagement maintenance/facilities: `src/Modules.GymManagement/Contracts/MaintenanceMessages.cs`
- Training: `src/Modules.Training/Contracts/TrainingMessages.cs`
- MembershipFinance: `src/Modules.MembershipFinance/Contracts/FinanceMessages.cs`

Request/handler conventions:
- query records return DTO response types
- command records return DTO response types or `Message`
- delete/cancel commands implement `IRequest`
- handlers are internal to their module application namespace
- WebApp controllers should depend on `IMediator` or a narrow adapter that
  dispatches through `IMediator`

## Architecture Tests

The module architecture tests should keep proving:
- every module exposes one module marker and one DI extension
- module assemblies do not reference one another
- non-Users modules do not reference Users internals
- Training does not reference Users or GymManagement internals
- `BuildingBlocks` does not reference WebApp or modules
- mediator abstractions live in `BuildingBlocks`
- migrated module handlers are resolvable from the composition root
- module-owned handlers for Training category and MembershipFinance package
  workflows use `IAppUnitOfWork` directly rather than shared workflow services

As migrations continue, add tests that fail if a migrated handler starts
injecting `IAppDbContext` or a shared `App.BLL.Services.*WorkflowService`.

## Next Migration Targets

1. Users session boundary:
   - add repository contracts for user gym roles and gym lookup
   - remove `IAppDbContext` from `UsersSessionService`
   - add mediator tests for login, refresh, logout, switch-gym, and switch-role

2. GymManagement member ownership:
   - move member list/detail/create/update/delete orchestration from
     `MemberWorkflowService` into module handlers
   - keep public API routes and DTOs unchanged
   - keep tenant isolation and cross-tenant tests green

3. Training workflow ownership:
   - move sessions, bookings, and attendance into
     module handlers
   - add repository contracts only where the existing UOW surface is missing
   - preserve booking duplicate, payment-reference, and trainer-assignment
     rules

4. MembershipFinance workflow ownership:
   - move membership sale/status/delete and payments into module handlers
   - preserve package validation, used-package delete conflict behavior, and
     payment behavior

5. GymManagement operations:
   - move equipment, maintenance, gym settings, gym users, and staff into
     module-owned handlers
   - keep caretaker authorization behavior unchanged

## Defense Wording

Use this wording:

"The project is a modular monolith in progress. Module assemblies, mediator
contracts, DI registration, and no-direct-module-reference rules are in place.
Training category CRUD and membership package CRUD demonstrate module-owned
workflow orchestration. Other module surfaces are mediated but still delegate to
shared BLL services while the migration continues. The assignment does not
claim separate schemas, per-module DbContexts, full internal isolation, or
microservice extraction."
