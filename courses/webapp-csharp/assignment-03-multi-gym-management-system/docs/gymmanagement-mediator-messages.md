# GymManagement Mediator Messages

**Status:** Implemented for Phase 18 member CRUD adapter.

This document maps each GymManagement member message to its handler, endpoint
adapter, and key invariants.

## Message Flow

```text
HTTP request
  -> WebApp MembersController
  -> IMediator.SendAsync(...)
  -> Modules.GymManagement.Application.Members handler
  -> IMemberWorkflowService
  -> Unit of Work + repositories
```

The controller keeps route attributes, API versioning, Swagger response
metadata, and `CreatedAtAction` behavior. GymManagement owns the mediator
surface behind the endpoint.

## Messages

### `ListMembersQuery`

- Endpoint: `GET /api/v1/{gymCode}/members`
- Handler: `ListMembersQueryHandler`
- Service method: `IMemberWorkflowService.GetMembersAsync`
- Output DTO: `IReadOnlyCollection<MemberResponse>`
- Invariants:
  - caller must have owner/admin tenant access
  - returned rows are scoped to the active gym
  - soft-deleted members are excluded by repository/query filters

### `GetCurrentMemberQuery`

- Endpoint: `GET /api/v1/{gymCode}/members/me`
- Handler: `GetCurrentMemberQueryHandler`
- Service method: `IMemberWorkflowService.GetCurrentMemberAsync`
- Output DTO: `MemberDetailResponse`
- Invariants:
  - caller must have tenant access as owner, admin, or member
  - current actor must have a member profile in the active gym
  - missing profile returns the existing not-found problem response

### `GetMemberQuery`

- Endpoint: `GET /api/v1/{gymCode}/members/{id}`
- Handler: `GetMemberQueryHandler`
- Service method: `IMemberWorkflowService.GetMemberAsync`
- Output DTO: `MemberDetailResponse`
- Invariants:
  - caller must have tenant access as owner, admin, or member
  - member-role callers can access only their own profile
  - foreign-gym member IDs return `404`, not cross-tenant data

### `CreateMemberCommand`

- Endpoint: `POST /api/v1/{gymCode}/members`
- Handler: `CreateMemberCommandHandler`
- Service method: `IMemberWorkflowService.CreateMemberAsync`
- Input DTO: `MemberUpsertRequest`
- Output DTO: `MemberDetailResponse`
- Invariants:
  - caller must have owner/admin tenant access
  - subscription member limit is checked before persistence
  - first name, last name, and member code are required
  - member code must be unique inside the gym
  - personal code must not belong to another person

### `UpdateMemberCommand`

- Endpoint: `PUT /api/v1/{gymCode}/members/{id}`
- Handler: `UpdateMemberCommandHandler`
- Service method: `IMemberWorkflowService.UpdateMemberAsync`
- Input DTO: `MemberUpsertRequest`
- Output DTO: `MemberDetailResponse`
- Invariants:
  - caller must have owner/admin tenant access
  - lookup is tenant-scoped
  - uniqueness rules match create behavior
  - member and linked person fields are updated together

### `DeleteMemberCommand`

- Endpoint: `DELETE /api/v1/{gymCode}/members/{id}`
- Handler: `DeleteMemberCommandHandler`
- Service method: `IMemberWorkflowService.DeleteMemberAsync`
- Output: none
- Invariants:
  - caller must have owner/admin tenant access
  - lookup is tenant-scoped
  - deletion remains soft delete through existing EF behavior
  - successful response remains `204 No Content`

## Registration

`AddGymManagementModule` registers all `IRequestHandler<>` and
`IRequestHandler<,>` implementations from the `Modules.GymManagement`
assembly. `AddAppModules` registers `AddBuildingBlocks()` before
`AddGymManagementModule()`, so `IMediator` can resolve the handlers at
request time.

## Test Evidence

- `TenantControllerTests.MembersController_ForwardsParametersAndReturnsCurrentResultShapes`
- `MemberCrudTests`
- `Final1CriticalE2ETests.MemberCrud_E2E_CreateReadUpdateDelete`
- `AuthSecurityAndErrorTests`
- `TenantIsolationAndIdorTests`
- `ModuleArchitectureTests.NonUsersModules_DoNotReferenceUsersInternals`
