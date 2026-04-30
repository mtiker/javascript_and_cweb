# Member CRUD Audit

**Audited:** 2026-04-28
**Phase:** 4 — Members vertical slice
**Scope:** Member entity end-to-end across REST API, MVC Admin, React client, and tests.

---

## Why this document exists

Phase 4 hardens the **Member** vertical slice. The audit captures every place a Member is read, mutated, or rendered, and flags the gaps that the Phase 4 tests-first workstream targets. Only Member code is in scope. Memberships, sessions, training categories, etc. are out of scope.

---

## 1. Backend slice

### 1.1 Domain & EF

| Concern | File | Notes |
|---------|------|-------|
| Entity | `src/App.Domain/Entities/Member.cs` | `GymId`, `PersonId`, `MemberCode`, `Status` (`MemberStatus` enum). No `Email` on the entity — email lives on `AppUser` linked through `Person`. |
| Person link | `src/App.Domain/Entities/Person.cs` | Holds `FirstName`, `LastName`, `PersonalCode`, `DateOfBirth`. Required for any Member lookup that returns a name. |
| Multi-tenant filter | `src/App.DAL.EF/AppDbContext.cs` | Global query filter applies `GymId` and `!IsDeleted` to tenant-owned reads, including Members. |
| Soft delete | `src/App.Domain/Common/TenantBaseEntity.cs` | `Member` inherits `IsDeleted` / `DeletedAtUtc`; `AppDbContext.SaveChangesAsync` converts deletes to soft deletes. |

### 1.2 BLL workflow

| File | Surface |
|------|---------|
| `src/App.BLL/Services/IMemberWorkflowService.cs` | `GetMembersAsync`, `GetCurrentMemberAsync`, `GetMemberAsync`, `CreateMemberAsync`, `UpdateMemberAsync`, `DeleteMemberAsync` |
| `src/App.BLL/Services/MemberWorkflowService.cs` | Implements the interface. Calls `IAuthorizationService.EnsureTenantAccessAsync` (gym-context check) and `EnsureMemberSelfAccessAsync` for `GetMember`. Detail, update, and delete lookups explicitly filter by `GymId` so cross-tenant ID manipulation fails even if EF filters are disabled in tests. Calls `ISubscriptionTierLimitService.EnsureCanCreateMemberAsync` on POST. Throws `ValidationAppException` on duplicate `MemberCode` (per-gym) and duplicate `PersonalCode` (global, since persons are shared across gyms). |

### 1.3 REST API

| Method | Route | Status | Notes |
|--------|-------|--------|-------|
| GET | `/api/v1/{gymCode}/members` | 200 | List of `MemberResponse` (id, memberCode, fullName, status). |
| GET | `/api/v1/{gymCode}/members/me` | 200 | Self profile for Members. |
| GET | `/api/v1/{gymCode}/members/{id}` | 200 / 403 / 404 | Member self-access enforced; admin/owner can read any. |
| POST | `/api/v1/{gymCode}/members` | 201 + Location | Returns `MemberDetailResponse`. |
| PUT | `/api/v1/{gymCode}/members/{id}` | 200 | Returns `MemberDetailResponse`. |
| DELETE | `/api/v1/{gymCode}/members/{id}` | 204 | Soft delete via `ISoftDeleteEntity`; normal reads hide the row and subsequent `GET` returns 404. |

Source: `src/WebApp/ApiControllers/Tenant/MembersController.cs`.

### 1.4 Soft delete

Soft delete is implemented through inheritance: `Member : TenantBaseEntity`, and `TenantBaseEntity` implements `ISoftDeleteEntity`. `DeleteMemberAsync` still calls `dbContext.Members.Remove(...)`, but `AppDbContext.ApplySoftDelete()` changes that delete into `IsDeleted = true` plus `DeletedAtUtc`. The linked `Person` is retained. This is now explicit in `member-contract.md` and covered by `MemberCrudTests.DeleteMember_SoftDeletesMemberRow`.

---

## 2. MVC Admin slice

| Concern | File | Status |
|---------|------|--------|
| Admin member page | `src/WebApp/Areas/Admin/Controllers/MembersController.cs` | Added in Phase 4. Reads through `IMemberWorkflowService` and role-gates to gym owner/admin. |
| Admin member view | `src/WebApp/Areas/Admin/Views/Members/Index.cshtml` | Added in Phase 4. Read-only member directory with counts. |
| Admin member view model | `src/WebApp/Models/AdminMembersPageViewModel.cs` | Added in Phase 4. Strongly typed model; no `ViewBag` / `ViewData`. |

**Phase 4 fix:** `/Admin/Members` now renders a real Razor MVC Admin Members page that reads `IMemberWorkflowService` and renders an `AdminMembersPageViewModel`. The view uses a strongly typed model - no `ViewBag` / `ViewData` - to keep the rule already documented in `assignment-compliance.md` R9 ("all views use strongly-typed ViewModels"). The page is read-only (list + counts); CRUD edits stay in the React client and the REST API.

---

## 3. React client slice

| File | Notes |
|------|-------|
| `client/src/pages/MembersPage.tsx` | Full CRUD: list, search, edit form, create, delete (with `window.confirm`). Surfaces `pageError` for load failures and a `NoticeBanner` for save / delete results. Validates the form locally before POST/PUT. |
| `client/src/lib/apiClient.ts` | `getMembers`, `getMember`, `createMember`, `updateMember`, `deleteMember`. Sends Bearer token, retries once on 401, handles `Accept-Language`. |
| `client/src/lib/types.ts` | `MemberSummary`, `MemberDetail`, `MemberUpsertRequest`, `MemberStatus` enum. Matches the frozen API contract. |
| `client/src/pages/CrudPages.test.tsx` | Covers loading, create-and-reload, load-error 403, validation-before-submit, and delete-with-confirm for Members. |

**Phase 4 fix:** Added tests for the React loading, validation, and delete paths. The validation test asserts the API is not called when required fields are missing.

---

## 4. Test slice (current state)

| Layer | Tests today |
|-------|-------------|
| Unit (BLL) | None directly for `MemberWorkflowService`. `AuthorizationServiceTests` covers `EnsureMemberSelfAccessAsync`. |
| Unit (controller) | `tests/WebApp.Tests/Unit/TenantControllerTests.cs` → `MembersController_ForwardsParametersAndReturnsCurrentResultShapes` (verifies 200 / 201 / 204 + `CreatedAtAction` shape). |
| Integration | `tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs` — `MembersEndpoint_RejectsActiveGymMismatch`, `MembersEndpoint_RejectsUnknownGym_Early`, `MembersEndpoint_RejectsInactiveGym_Early`, `Member_CannotReadAnotherMember`, `ApiErrors_ReturnProblemDetailsJson`. |
| React | `client/src/pages/CrudPages.test.tsx` — happy create + 403 load error. |
| MVC Admin | None — there is no Admin Members page yet. |

**Phase 4 adds** an integration test class focused on Members CRUD (list / detail / 201 / 200 update / 204 soft delete / duplicate ProblemDetails / wrong gym / cross-gym ID manipulation), MVC Admin Members tests (renders from view model, no `ViewBag` / `ViewData`), and React tests (loading, validation, delete).

---

## 5. Risks identified during this audit

1. **Admin Members surface is missing.** The dashboard quick-link "Members" leaves the MVC area entirely. If a grader expects an MVC-rendered member list, today they would see nothing.
2. **No integration test asserts the 201 + Location header** for `POST /members`. The unit test asserts the action result shape, but the actual HTTP status was previously untested.
3. **Duplicate `MemberCode` / `PersonalCode`** is enforced by the BLL but no integration test confirms the response is `400 application/problem+json` with a `detail` field — the contract the React client depends on.
4. **No test exercised delete behaviour** (DELETE then GET -> 404 and the persisted soft-delete marker).
5. **Member update/delete relied too heavily on EF query filters.** Phase 4 added explicit `GymId` predicates and a cross-gym ID manipulation regression test.

The Phase 4 test plan in `member-tests-map.md` closes these risks for the member vertical slice.
