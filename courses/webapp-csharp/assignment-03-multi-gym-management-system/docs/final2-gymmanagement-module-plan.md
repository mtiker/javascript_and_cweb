# Final-2 GymManagement Module Plan

**Status:** Phase 18 member CRUD adapter implemented. Package CRUD remains in
MembershipFinance for Phase 20.

## Scope

Phase 18 moves the tenant member HTTP workflow behind GymManagement mediator
messages while preserving the public API contract:

- `GET /api/v1/{gymCode}/members`
- `GET /api/v1/{gymCode}/members/me`
- `GET /api/v1/{gymCode}/members/{id}`
- `POST /api/v1/{gymCode}/members`
- `PUT /api/v1/{gymCode}/members/{id}`
- `DELETE /api/v1/{gymCode}/members/{id}`

The route, DTO, status-code, `Location` header, and `ProblemDetails` behavior
remain unchanged.

## Implemented Shape

```text
HTTP request
  -> WebApp MembersController
  -> IMediator.SendAsync(...)
  -> Modules.GymManagement.Application.Members handler
  -> IMemberWorkflowService
  -> existing tenant authorization, repositories, mapper, Unit of Work
```

This is an adapter migration, not a database or DTO migration. The module now
owns the public mediator contract and controller dispatch path for member CRUD.
The underlying member workflow still uses the existing Clean/Onion service so
the phase stays small and preserves tested behavior.

## Tenant Authorization Boundary

Member handlers do not bypass authorization. They call the existing
`IMemberWorkflowService`, which enforces:

- gym-owner/gym-admin access for list, create, update, and delete
- gym-owner/gym-admin/member access for member detail reads
- member self-access checks for member-role profile reads
- route gym code matching the active gym context
- tenant-scoped repository lookups that return `404` for foreign-gym entity IDs

The public tenant API therefore keeps the same wrong-gym behavior while the
controller no longer depends directly on the BLL member service.

## Package Ownership Decision

`MembershipPackage` stays in MembershipFinance for now.

Reasons:

- package CRUD is coupled to membership sales
- deleting packages must check existing memberships
- packages carry price, currency, discount, and training-free rules
- memberships snapshot package price/currency at sale time
- booking pricing reads membership package benefit rules

Moving package CRUD into GymManagement would split one finance lifecycle across
two modules. The package migration should happen with the MembershipFinance
module slice in Phase 20, together with membership, payment, invoice, and
pricing mediator messages.

## Tests

Phase 18 is covered by:

- `MemberCrudTests` for public API CRUD compatibility
- `AuthSecurityAndErrorTests` and tenant isolation suites for wrong-gym access
- `MembershipPackageCrudTests` for package CRUD stability while packages remain
  finance-owned
- `TenantControllerTests.MembersController_ForwardsParametersAndReturnsCurrentResultShapes`
  for controller-to-mediator delegation
- `ModuleArchitectureTests.NonUsersModules_DoNotReferenceUsersInternals`
  for Users boundary enforcement
- React member/package Vitest coverage for client behavior

## Remaining Work

- Move the implementation behind `IMemberWorkflowService` from `App.BLL` into
  `Modules.GymManagement.Application` once adjacent tenant workflows are moved.
- Add GymManagement-owned mediator messages for gyms, staff, maintenance,
  equipment, opening hours, and settings.
- Move package, membership, payment, and invoice HTTP adapters into
  MembershipFinance in the finance phase.
