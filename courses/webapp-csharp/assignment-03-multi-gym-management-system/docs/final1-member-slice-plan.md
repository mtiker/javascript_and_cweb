# Final1 Member Slice Plan

## Status

Phase 11 — Member CRUD vertical slice migrated to the mandatory Final1 shape:
repository contract + Unit of Work + application service + dedicated mapper.

The public API contract is unchanged:

- `GET /api/v1/{gymCode}/members`
- `GET /api/v1/{gymCode}/members/me`
- `GET /api/v1/{gymCode}/members/{id}`
- `POST /api/v1/{gymCode}/members`
- `PUT /api/v1/{gymCode}/members/{id}`
- `DELETE /api/v1/{gymCode}/members/{id}`

The MVC Admin members page (`/Admin/Members`) and React member pages keep their
existing routes, view models, and request/response shapes.

No training, finance, or maintenance code is migrated in this phase.

## Course Context

This aligns the Member slice of Assignment 03 with the Final1 Clean/Onion
requirements:

- API and MVC controllers are boundary adapters.
- Member use cases live in the application/BLL layer.
- Member persistence details live behind a BLL contract.
- The EF implementation lives in infrastructure (`App.DAL.EF`).
- Entity ↔ DTO/ViewModel projection lives in a mapper, not in the service.

## Implemented Boundary

| Concern | Final location |
|---|---|
| Member use cases | `src/App.BLL/Services/MemberWorkflowService.cs` |
| Member persistence contract | `src/App.BLL/Contracts/Persistence/IMemberRepository.cs` |
| Member EF implementation | `src/App.DAL.EF/Repositories/EfMemberRepository.cs` |
| Unit of Work member access | `IAppUnitOfWork.Members` |
| Member response mapping | `src/App.BLL/Mapping/MemberMapper.cs` |
| API controller delegation | `src/WebApp/ApiControllers/Tenant/MembersController.cs` |
| MVC Admin controller delegation | `src/WebApp/Areas/Admin/Controllers/MembersController.cs` |
| DI wiring | `src/WebApp/Setup/ServiceExtensions.cs`, `src/App.DAL.EF/PersistenceServiceExtensions.cs` |

## Request Flow

List members:

1. `MembersController.GetMembers` receives `(gymCode)`.
2. Controller delegates to `IMemberWorkflowService.GetMembersAsync`.
3. Service ensures `GymOwner`/`GymAdmin` access via `IAuthorizationService.EnsureTenantAccessAsync`.
4. Service calls `IMemberRepository.ListByGymAsync(gymId)` through `IAppUnitOfWork.Members`.
5. `IMemberMapper.ToSummary(...)` projects each `Member` to `MemberResponse`.

Get one member (admin or self):

1. Controller delegates to `GetMemberAsync(gymCode, id)`.
2. Service ensures tenant access (also allowing the `Member` role).
3. Service calls `IAuthorizationService.EnsureMemberSelfAccessAsync(gymId, id)` so a
   member cannot fetch another member's detail by ID.
4. Service calls `IMemberRepository.FindWithPersonAsync(gymId, id)`.
5. `NotFoundException` is thrown on miss.
6. `IMemberMapper.ToDetail(...)` projects to `MemberDetailResponse`.

Get current member (`/me`):

1. Service ensures tenant access (`Owner`/`Admin`/`Member`).
2. Service resolves the current member through `IAuthorizationService.GetCurrentMemberAsync`.
3. Service loads the full member via `IMemberRepository.FindWithPersonAsync(gymId, currentMember.Id)`.
4. Mapper projects to `MemberDetailResponse`.

Create member:

1. Service ensures `Owner`/`Admin` access.
2. Service calls `ISubscriptionTierLimitService.EnsureCanCreateMemberAsync`.
3. Request is normalized + validated (required fields).
4. Uniqueness checks via `IMemberRepository.MemberCodeExistsAsync` and
   `IMemberRepository.PersonalCodeExistsAsync`.
5. New `Member`/`Person` graph is added through `IMemberRepository.AddAsync`.
6. `IAppUnitOfWork.SaveChangesAsync` commits.
7. Mapper projects the result.

Update member:

1. Service ensures `Owner`/`Admin` access.
2. Service loads the member via `IMemberRepository.FindWithPersonAsync(gymId, id)` (404 on miss).
3. Uniqueness check excludes the current `memberId`/`personId`.
4. Properties are mutated on the tracked graph (no explicit `Update`).
5. `IAppUnitOfWork.SaveChangesAsync` commits.
6. Mapper projects the result.

Delete member:

1. Service ensures `Owner`/`Admin` access.
2. Service loads the member via `IMemberRepository.FindAsync(gymId, id)` (404 on miss).
3. `IMemberRepository.Remove(member)` stages the delete.
4. `IAppUnitOfWork.SaveChangesAsync` commits — `AppDbContext.ApplySoftDelete()`
   converts the remove to `IsDeleted = true` and sets `DeletedAtUtc`.

## Tests

Coverage added or preserved:

- `MemberWorkflowServiceTests.CreateMemberAsync_RejectsDuplicateMemberCode` — duplicate code throws `ValidationAppException`.
- `MemberWorkflowServiceTests.CreateMemberAsync_RejectsDuplicatePersonalCode` — duplicate personal code throws `ValidationAppException`.
- `MemberWorkflowServiceTests.GetMemberAsync_ScopedToGymTenant` — wrong gym ID returns `NotFoundException`.
- `MemberWorkflowServiceTests.GetMemberAsync_RejectsCrossMemberAccessForMemberRole` — a `Member` reading another member's detail bubbles up the `EnsureMemberSelfAccessAsync` rejection.
- `MemberWorkflowServiceTests.GetMembersAsync_OrdersByLastNameThenFirstName` — list ordering preserved through repository.
- `MemberCrudTests.*` — full API CRUD regression (admin list, get, create, update, delete soft, duplicate code, duplicate personal code, cross-tenant 404).
- `AdminMembersPageTests.*` — MVC Admin members flow regression.
- `client/src/pages/CrudPages.test.tsx` — React member CRUD UI regression.
- `ArchitectureTests.MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries` — service composition + namespace assertion.
- `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence` — `IMemberRepository` lives in `App.BLL.Contracts.Persistence`.

## Out Of Scope

- Training (sessions, categories) workflow services.
- Finance / payments / invoices.
- Maintenance tasks.
- Member workspace read model.
- Identity model changes.
- Member API route or DTO changes.
- Removing `IAppDbContext` from BLL.

## Notes

- The service is renamed in spirit to `MemberService`, but the existing class
  name `MemberWorkflowService` and interface `IMemberWorkflowService` are kept
  to avoid churn across MVC controllers, tests, and the React-facing API
  controller. Behavior, dependencies, and persistence boundary are the Final1
  shape; the file name is the only thing that does not change.
- `IMemberRepository.Remove` triggers the soft-delete path inherited from
  `TenantBaseEntity` because `AppDbContext.ApplySoftDelete()` rewrites
  `EntityState.Deleted` to `Modified` for tenant entities.
