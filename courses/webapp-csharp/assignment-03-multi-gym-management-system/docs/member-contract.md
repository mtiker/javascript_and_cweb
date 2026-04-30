# Member Contract (Phase 4 lock)

**Frozen:** 2026-04-28
**Stability target:** Final1 / Final2

This document is the canonical contract for the **Member** REST API and the consumer types in the React client. It is a focused, member-only refinement of `docs/api-contract-freeze.md`. If this contract changes, the docs, tests, and React types must change together in a single phase.

---

## 1. Routes

Base URL: `/api/v{version:apiVersion}/{gymCode}/members` with public examples using `version = 1` and `gymCode` a slug (lowercase, hyphenated).

| Method | Path | Auth | Success | Notes |
|--------|------|------|---------|-------|
| GET | `/` | Bearer JWT — active gym must match `{gymCode}`; role in {GymOwner, GymAdmin} | `200` `MemberResponse[]` | List for the active gym only. |
| GET | `/me` | Bearer JWT — active gym matches; role in {GymOwner, GymAdmin, Member} | `200` `MemberDetailResponse` | Member's own profile in the active gym. `404` if the caller has no member record in that gym. |
| GET | `/{id:guid}` | Bearer JWT — active gym matches; role in {GymOwner, GymAdmin, Member} | `200` `MemberDetailResponse` | Member self-access enforced — a Member reading another member's `{id}` returns `403`. |
| POST | `/` | Bearer JWT — active gym matches; role in {GymOwner, GymAdmin} | `201` `MemberDetailResponse` + `Location` header -> `GET /api/v1/{gymCode}/members/{id}` | Creates Person + Member. |
| PUT | `/{id:guid}` | Bearer JWT — active gym matches; role in {GymOwner, GymAdmin} | `200` `MemberDetailResponse` | Updates the linked Person fields and the Member fields. |
| DELETE | `/{id:guid}` | Bearer JWT — active gym matches; role in {GymOwner, GymAdmin} | `204 No Content` | Soft delete (see section 5). |

> **Why `200` on PUT, not `204`?** The React client (`MembersPage.tsx → handleSubmit`) reads the response body to update its form state. `204` would force a follow-up GET. Returning the updated `MemberDetailResponse` matches the freeze in `docs/api-contract-freeze.md`.

---

## 2. DTO shapes

### 2.1 `MemberResponse` (list item)

```json
{
  "id": "guid",
  "memberCode": "string",
  "fullName": "string",
  "status": 0
}
```

`status` is the integer `MemberStatus` enum: `0 Active`, `1 Suspended`, `2 Left`.

### 2.2 `MemberDetailResponse` (single member)

```json
{
  "id": "guid",
  "memberCode": "string",
  "firstName": "string",
  "lastName": "string",
  "fullName": "string",
  "personalCode": "string | null",
  "dateOfBirth": "date | null",
  "status": 0
}
```

`dateOfBirth` is ISO-8601 `YYYY-MM-DD` (no time component).

### 2.3 `MemberUpsertRequest` (POST and PUT body)

```json
{
  "firstName": "string",
  "lastName": "string",
  "memberCode": "string",
  "personalCode": "string | null",
  "dateOfBirth": "date | null",
  "status": 0
}
```

Server normalisation (`MemberWorkflowService.NormalizeRequest`):

- `firstName`, `lastName`, `memberCode` are `Trim()`'d. Empty → `400` ProblemDetails (`"First name is required."` etc.).
- `personalCode` is `null` if blank, otherwise `Trim()`'d.
- `dateOfBirth` passes through as `DateOnly?`.
- `status` defaults to `MemberStatus.Active` (`0`).

---

## 3. Error shape (RFC 7807 ProblemDetails)

All non-2xx responses use `Content-Type: application/problem+json; charset=utf-8`:

```json
{
  "type": "string | null",
  "title": "string",
  "status": 0,
  "detail": "string | null",
  "instance": "string | null",
  "errors": ["string"]
}
```

Status mapping (from `WebApp/Middleware/ProblemDetailsMiddleware.cs`):

| Server exception | HTTP status | Title |
|------------------|-------------|-------|
| `NotFoundException` | `404` | `"Not Found"` |
| `ForbiddenException` | `403` | `"Forbidden"` |
| `ValidationAppException` | `400` | `"Validation Failed"` |
| anything else | `500` | `"Server Error"` |

The React client reads `detail` (and falls back to `errors[]`). Tests must assert `detail` is populated for the cases below.

---

## 4. Member-specific error cases (locked)

| Trigger | Status | Title | `detail` (substring) |
|---------|--------|-------|----------------------|
| Caller's active gym ≠ `{gymCode}` | `403` | `Forbidden` | `"active gym"` (case-insensitive) |
| Member reads another member's `{id}` | `403` | `Forbidden` | `"member"` |
| `GET /{id}` for an unknown id | `404` | `Not Found` | `"Member was not found."` |
| POST/PUT with blank `firstName` | `400` | `Validation Failed` | `"First name is required."` |
| POST/PUT with blank `lastName` | `400` | `Validation Failed` | `"Last name is required."` |
| POST/PUT with blank `memberCode` | `400` | `Validation Failed` | `"Member code is required."` |
| POST/PUT with `memberCode` already used in this gym | `400` | `Validation Failed` | `"Member code already exists in this gym."` |
| POST/PUT with `personalCode` already used by another person | `400` | `Validation Failed` | `"Personal code already belongs to another person."` |

Anything tightening or weakening these messages must update `docs/member-tests-map.md` in the same change so the integration tests stay aligned.

---

## 5. Delete semantics - soft delete

`DELETE /api/v1/{gymCode}/members/{id}` is a **soft delete**. `Member` inherits `TenantBaseEntity`, which implements `ISoftDeleteEntity`, so `AppDbContext.SaveChangesAsync` converts `Remove(member)` into `IsDeleted = true` and sets `DeletedAtUtc`.

The linked `Person` row is retained. Normal member reads use the tenant soft-delete query filter and therefore hide the deleted member; a subsequent `GET /{id}` returns `404 application/problem+json`. The soft-delete marker is verified with `IgnoreQueryFilters()` in `MemberCrudTests.DeleteMember_SoftDeletesMemberRow`.

---

## 6. Multi-tenant enforcement

Every member endpoint goes through `IAuthorizationService.EnsureTenantAccessAsync`, which:

1. Extracts `ActiveGymId` from the caller's JWT.
2. Resolves the route `{gymCode}` slug via `GymResolutionMiddleware`.
3. Returns `403` if the route gym ≠ active gym.
4. Returns `403` if the caller's role is not in the per-endpoint allow-list above.
5. Returns `404` if the slug does not match any gym.
6. Returns `403` if the gym exists but is `IsActive = false`.

The query filter on `AppDbContext.Members` additionally enforces `GymId == ActiveGymId` for normal application reads. Member detail, update, and delete lookups also include explicit `GymId == gymId` predicates in `MemberWorkflowService`, because integration tests intentionally disable global filters to prove the service does not rely on EF filters alone. This is asserted by `MemberCrudTests.UpdateMember_ForeignGymMemberId_Returns404`.

---

## 7. What is **not** part of the member contract

- `MembershipPackage`, `Membership`, `MembershipSale` — those are the membership *package* slice, audited in `docs/api.md` and not touched by Phase 4.
- `MemberWorkspace` (`/api/v1/{gymCode}/member-workspace/...`) — separate read-model, separate route prefix.
- AppUser email creation or email uniqueness. `MemberUpsertRequest` does not include an email field; email uniqueness belongs to the identity/account contract. The member duplicate-person field in this CRUD contract is `personalCode`.
- The Admin MVC Members page returns a Razor view, not JSON — covered by `member-tests-map.md` §3 but not part of this REST contract.
