# Member Tests Map (Phase 4)

**Updated:** 2026-04-28
**Test runners:** `dotnet test multi-gym-management-system.slnx` (xUnit, .NET 10) and `cd client && npm test` (Vitest 2).

This map ties the nine Phase 4 scenarios to the exact test method names that prove them. If a row's "Phase 4 status" says "added", the test was written in this phase; "existing" means the slice already had coverage and Phase 4 only references it.

---

## 1. Backend integration scenarios — `tests/WebApp.Tests/Integration/MemberCrudTests.cs`

All tests use `CustomWebApplicationFactory` (in-memory SQLite, seeded). Auth is exercised by logging in as `admin@peakforge.local` (gym admin in `peak-forge`) and, where needed, `member@peakforge.local`.

| # | Phase 4 scenario | Test method | Phase 4 status |
|---|------------------|-------------|----------------|
| 1 | GET members list for active gym | `MemberCrudTests.GetMembers_ReturnsListForActiveGym` | added |
| 2 | GET member detail | `MemberCrudTests.GetMember_ReturnsDetail_ForGymAdmin` | added |
| 3 | POST creates member and returns 201 | `MemberCrudTests.CreateMember_Returns201_AndLocationHeader` | added |
| 4 | PUT updates member and returns 200 | `MemberCrudTests.UpdateMember_Returns200_AndUpdatedDetail` | added |
| 5.a | DELETE returns 204 and normal follow-up GET returns 404 | `MemberCrudTests.DeleteMember_Returns204_AndSubsequentGetIs404` | added |
| 5.b | DELETE performs documented soft delete | `MemberCrudTests.DeleteMember_SoftDeletesMemberRow` | added |
| 6.a | Duplicate `memberCode` returns ProblemDetails 400 | `MemberCrudTests.CreateMember_DuplicateMemberCode_ReturnsProblemDetails` | added |
| 6.b | Duplicate `personalCode` returns ProblemDetails 400 | `MemberCrudTests.CreateMember_DuplicatePersonalCode_ReturnsProblemDetails` | added |
| 7 | Wrong gym (active gym ≠ route) is blocked | `AuthSecurityAndErrorTests.MembersEndpoint_RejectsActiveGymMismatch` | existing — referenced from Phase 4 |
| 7.b | Cross-gym member ID manipulation is blocked | `MemberCrudTests.UpdateMember_ForeignGymMemberId_Returns404` | added |

The duplicate-error tests assert all three required pieces: HTTP `400`, `Content-Type: application/problem+json; charset=utf-8`, and a non-empty `detail` substring matching the contract in `docs/member-contract.md` §4.
Email duplicate handling is not listed here because member CRUD has no email field; AppUser email uniqueness belongs to the identity/account slice.

---

## 2. React scenarios — `client/src/pages/CrudPages.test.tsx`

| # | Phase 4 scenario | Test name | Phase 4 status |
|---|------------------|-----------|----------------|
| 8.a | Loading state | `"shows the loading state while members are fetched"` | added |
| 8.b | Success — create + reload | `"creates a member and reloads the list"` | existing — referenced |
| 8.c | Validation error before submit | `"surfaces validation errors when required member fields are empty"` | added |
| 8.d | Delete flow with confirm | `"deletes a member after confirmation"` | added |

Every Phase 4 React test uses the existing `renderWithAuth` + `jsonResponse` helpers from `client/src/test/testUtils.tsx`. The validation test must assert that **no** `fetch` call is made when client-side validation fails — that is the contract the page promises (form errors are surfaced before the API is called).

---

## 3. MVC Admin scenario — `tests/WebApp.Tests/Integration/AdminMembersPageTests.cs`

| # | Phase 4 scenario | Test method | Phase 4 status |
|---|------------------|-------------|----------------|
| 9.a | `/Admin/Members` renders a Razor view (not a redirect) | `AdminMembersPageTests.AdminMembersPage_RendersRazorView` | added |
| 9.b | The view is bound to a strongly-typed view model | `AdminMembersPageTests.AdminMembersPage_RendersFromViewModel` | added |
| 9.c | The controller and view do **not** use `ViewBag` or `ViewData` | `AdminMembersPageTests.AdminMembersPage_DoesNotUseViewBagOrViewData` | added |

`AdminMembersPage_DoesNotUseViewBagOrViewData` is a static-source test: it reads `Areas/Admin/Controllers/MembersController.cs` and `Areas/Admin/Views/Members/Index.cshtml` from disk and asserts neither file contains the tokens `ViewBag` or `ViewData`. This guards against a future drift where someone "just adds one more field" via `ViewBag`.

---

## 4. Coverage summary

| Layer | Member-related tests after Phase 4 |
|-------|------------------------------------|
| BLL unit | `AuthorizationServiceTests.EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers` (existing) |
| Controller unit | `TenantControllerTests.MembersController_ForwardsParametersAndReturnsCurrentResultShapes` (existing) |
| API integration | 9 cases in `MemberCrudTests` + 4 existing cases in `AuthSecurityAndErrorTests` (`MembersEndpoint_RejectsActiveGymMismatch`, `MembersEndpoint_RejectsUnknownGym_Early`, `MembersEndpoint_RejectsInactiveGym_Early`, `Member_CannotReadAnotherMember`) |
| MVC Admin integration | 3 cases in `AdminMembersPageTests` |
| React component | 4 cases in `CrudPages.test.tsx` |

---

## 5. How to run only the member-related tests

```bash
# Backend — Member integration tests
dotnet test multi-gym-management-system.slnx --filter "FullyQualifiedName~MemberCrudTests|FullyQualifiedName~AdminMembersPageTests|FullyQualifiedName~MembersController_Forwards|FullyQualifiedName~Member_CannotReadAnotherMember|FullyQualifiedName~MembersEndpoint_"

# React — only Members page
cd client && npm test -- CrudPages
```

The CI pipeline already runs `dotnet test` and `npm test` in the `test` stage, so all of these are exercised on every push.
