# GymManagement Module Contracts

**Status:** Phase 18 member contract baseline.

The GymManagement module publishes mediator request messages for tenant member
CRUD. Handlers stay internal to `Modules.GymManagement.Application.Members`.

## Public API Compatibility

The HTTP member contract remains unchanged:

| Route | Request | Response | Auth |
|---|---|---|---|
| `GET /api/v1/{gymCode}/members` | none | `IReadOnlyCollection<MemberResponse>` | JWT tenant role |
| `GET /api/v1/{gymCode}/members/me` | none | `MemberDetailResponse` | JWT tenant role |
| `GET /api/v1/{gymCode}/members/{id}` | none | `MemberDetailResponse` | JWT tenant role |
| `POST /api/v1/{gymCode}/members` | `MemberUpsertRequest` | `MemberDetailResponse` | gym owner/admin |
| `PUT /api/v1/{gymCode}/members/{id}` | `MemberUpsertRequest` | `MemberDetailResponse` | gym owner/admin |
| `DELETE /api/v1/{gymCode}/members/{id}` | none | `204 No Content` | gym owner/admin |

No React, MVC, or API client contract changes are required.

## Mediator Contract Namespace

GymManagement member messages live in:

`Modules.GymManagement.Contracts`

The WebApp host may reference this namespace as the endpoint adapter and
composition root. Other modules must not reference
`Modules.GymManagement.Application`.

## Published Messages

| Message | Response | Purpose |
|---|---|---|
| `ListMembersQuery(string GymCode)` | `IReadOnlyCollection<MemberResponse>` | List active tenant members. |
| `GetCurrentMemberQuery(string GymCode)` | `MemberDetailResponse` | Load the authenticated member's profile in the active gym. |
| `GetMemberQuery(string GymCode, Guid MemberId)` | `MemberDetailResponse` | Load one member detail with tenant and self-access checks. |
| `CreateMemberCommand(string GymCode, MemberUpsertRequest Request)` | `MemberDetailResponse` | Create a tenant member and linked person record. |
| `UpdateMemberCommand(string GymCode, Guid MemberId, MemberUpsertRequest Request)` | `MemberDetailResponse` | Update member status and person fields. |
| `DeleteMemberCommand(string GymCode, Guid MemberId)` | none | Soft-delete a member row. |

## Boundary Rules

- Controllers adapt HTTP requests into mediator messages.
- Handlers are registered by `AddGymManagementModule`.
- Handlers preserve existing BLL validation, tenant authorization, and mapper
  behavior during the transition.
- GymManagement does not reference Users internals. User and active-gym context
  still arrive through shared auth claims and existing authorization services.
- DTOs remain in `App.DTO.v1.Members` until API versioning changes.

## Package Contract Decision

`MembershipPackage` contracts are intentionally not published by
GymManagement. Package CRUD remains on the existing membership package API and
BLL services until the MembershipFinance migration, because package lifecycle
rules belong with membership sale, payment, and pricing behavior.
