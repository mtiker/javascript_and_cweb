# Final1 Test Traceability

**Date:** 2026-04-30

## Mandatory Requirement Matrix

| Requirement | Test evidence | Notes |
|-------------|---------------|-------|
| Clean/Onion dependency direction | `tests/WebApp.Tests/Architecture/ArchitectureTests.cs` | Asserts forbidden assembly references and controller dependency boundaries. |
| Repository/UOW/service/BLL/mapper usage | `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence`; slice boundary tests | Covers auth, member, training, membership/finance, and maintenance slices. |
| Full Admin UX | `SmokeTests.AdminDashboard_UsesSharedLayoutAndSiteStyles`; `SmokeTests.SystemAdmin_GymsRoute_RendersMvcPage`; `SmokeTests.TenantAdmin_WorkspaceRoutes_RenderMvcPages`; `AdminMembersPageTests`; `MvcComplianceTests` | Covers route rendering, role access, typed models, and no dynamic view data. |
| Public DTO/API stability | `ApiContractMetadataTests.cs`; controller unit tests in `TenantControllerTests.cs` and `AdditionalControllerTests.cs` | Locks route/version/problem response metadata and response shape delegation. |
| Auth login | `AuthSecurityAndErrorTests.Login_ReturnsJwt_RefreshToken_Expiry_AndUserContext`; `Final1CriticalE2ETests.Login_E2E_ReturnsTenantSession` | Covers real login payload and tenant context. |
| Refresh token rotation | `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse` | Verifies new refresh token and rejection of the old token. |
| Refresh token expiry/invalid/logout | `RenewRefreshToken_RejectsExpiredRefreshToken`; `RenewRefreshToken_RejectsInvalidJwt`; `Logout_InvalidatesRefreshToken` | Negative auth paths. |
| IDOR: active gym mismatch | `MembersEndpoint_RejectsActiveGymMismatch`; `TenantIsolationAndIdorTests.Trainer_CannotAccess_DifferentGym_TrainingSessions`; `Caretaker_CannotAccess_DifferentGym_MaintenanceTasks` | Route gym code cannot differ from active gym. |
| IDOR: member self-only | `AuthorizationServiceTests.EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers`; `AuthSecurityAndErrorTests.Member_CannotReadAnotherMember`; `TenantIsolationAndIdorTests.Member_CannotAccess_AnotherMembersWorkspace` | Member cannot read another member's data/workspace. |
| IDOR: trainer/caretaker assignment | `AuthorizationServiceTests`; `TenantIsolationAndIdorTests.Trainer_CannotUpdateAttendance_ForUnassignedSession`; `Caretaker_CannotUpdateStatus_ForUnassignedTask` | Role-specific resource ownership. |
| IDOR: cross-tenant ID manipulation | `MemberCrudTests.UpdateMember_ForeignGymMemberId_Returns404`; `MembershipPackageCrudTests.UpdateMembershipPackage_ForeignGymPackageId_Returns404`; `TenantIsolationAndIdorTests.GymAdmin_AtNorthStar_CannotCancel_PeakForgeBooking_ViaIdManipulation`; `Final1CriticalE2ETests.IdorNegative_E2E_CrossTenantMemberUpdateReturns404` | Resource IDs are scoped by gym. |
| UI i18n | `TrainingCategoryLocalizationTests.MvcLoginLabels_UseResxResourcesForRequestedCulture`; React language tests in `CrudPages.test.tsx` and `apiClient.test.ts` | MVC `.resx`, React labels, and `Accept-Language` header. |
| DB i18n | `LangStrTests.cs`; `TrainingCategoryLocalizationTests.TrainingCategories_AcceptLanguageEn_ReturnsEnglishLangStrValue`; `TrainingCategories_AcceptLanguageEt_ReturnsEstonianLangStrValue`; `TrainingCategories_MissingTranslation_FallsBackSafely`; `PostgreSqlPersistenceTests` when enabled | `LangStr` translation, fallback, and provider persistence. |
| React member CRUD | `CrudPages.test.tsx`: create member, validation errors, delete member, load error | Covers member page UX and API error display. |
| React training category CRUD | `CrudPages.test.tsx`: update category, save error, localized create request | Covers category form and i18n header behavior. |
| React membership package CRUD | `CrudPages.test.tsx`: loading, create, update, validation, delete, save error | Covers package page states. |
| Critical E2E login | `Final1CriticalE2ETests.Login_E2E_ReturnsTenantSession` | HTTP-level end-to-end through test host. |
| Critical E2E member CRUD | `Final1CriticalE2ETests.MemberCrud_E2E_CreateReadUpdateDelete` | Create/read/update/delete through REST API. |
| Critical E2E category CRUD | `Final1CriticalE2ETests.TrainingCategoryCrud_E2E_CreateReadUpdateDelete` | Create/read/update/delete through REST API. |
| Critical E2E package CRUD | `Final1CriticalE2ETests.MembershipPackageCrud_E2E_CreateReadUpdateDelete` | Create/read/update/delete through REST API. |
| Critical E2E IDOR negative | `Final1CriticalE2ETests.IdorNegative_E2E_CrossTenantMemberUpdateReturns404` | Cross-tenant update is blocked. |
| CI commands | `.gitlab-ci.yml`; `docs/final1-coverage-audit.md` | Child pipeline commands listed explicitly. |

## Traceability Notes

- The E2E tests are API-level integration tests, not browser automation. They execute against the full ASP.NET Core host with middleware, auth, routing, BLL, and EF-backed test persistence.
- React coverage is handled separately with Vitest and mocked network responses, because the repository does not currently include Playwright.
- PostgreSQL provider realism is covered by gated Testcontainers tests and should be run manually when Docker is available.
