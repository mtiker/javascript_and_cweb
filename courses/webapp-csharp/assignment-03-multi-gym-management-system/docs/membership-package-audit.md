# Membership Package CRUD Audit

Date: 2026-04-28
Phase: 6 - membership packages CRUD vertical slice

## Scope

This audit covers the package-related vertical slice only:

- domain entity: `MembershipPackage`
- DTOs: `MembershipPackageResponse`, `MembershipPackageUpsertRequest`
- BLL: `MembershipPackageService` through `MembershipWorkflowService`
- API controller: `/api/v1/{gymCode}/membership-packages`
- React CRUD page: `MembershipPackagesPage`
- tests for package CRUD, validation, soft delete, and tenant isolation

External payments and finance redesign are intentionally out of scope.

## Current Implementation

### Domain and Persistence

`MembershipPackage` is a tenant entity with soft-delete fields inherited from `TenantBaseEntity`. It contains localized `Name` and `Description` fields via `LangStr`, package type, duration, base price, currency, training discount, and free-training flag.

`AppDbContext` applies the tenant soft-delete query filter for normal package reads. `SaveChangesAsync` converts `Remove(package)` to a soft delete.

### BLL

`MembershipPackageService` now:

- validates create/update requests in the application layer
- normalizes names, currency codes, and descriptions before persistence
- filters list/update/delete by active `GymId`
- returns `NotFoundException` for cross-gym package IDs
- soft-deletes unused packages
- returns `409 Conflict` for used packages so package history and lifecycle state do not become ambiguous

### API and Client

`MembershipPackagesController` exposes list/create/update/delete routes with workflow-compatible responses:

- create returns `201`
- update returns `200`
- delete returns `204`

`MembershipPackagesPage` supports loading, empty and filtered-empty states, create, update, delete confirmation, success notices, API error notices, and local validation errors.

## Findings Resolved

1. Package create/update accepted invalid values such as negative prices, zero duration, and missing currency.
2. Package update/delete looked up entities by `Id` only after tenant authorization. With EF filters disabled in the test host, cross-tenant ID manipulation could mutate the wrong gym's package.
3. The React package form relied on native number input validation for some invalid inputs, which prevented the existing custom validation banner from rendering.
4. Used package delete behavior was not explicitly documented. The implemented behavior is now a conflict with historical membership snapshots retained.

## Test Evidence

Backend integration tests in `MembershipPackageCrudTests` cover:

- list membership packages
- create package returns `201`
- invalid price returns `ProblemDetails`
- invalid duration returns `ProblemDetails`
- missing currency returns `ProblemDetails`
- update package
- delete unused package as soft delete
- delete used package returns conflict and keeps the package/membership snapshot
- wrong-gym package ID update returns `404`

Frontend tests in `CrudPages.test.tsx` cover package page loading, create and reload, update and reload, delete and reload, local validation errors, and API validation errors.

## Remaining Risks

- Package localization still follows the existing single-value write model: the request UI culture controls which `LangStr` value is written.
- There is no separate package detail endpoint because the React CRUD page edits from list items.
- Used package delete is intentionally blocked; package deactivation is documented as the safe future workflow but no separate deactivate endpoint exists yet.
