# Testing Guide

## Automated Verification Commands

Format check:

```powershell
dotnet format multi-gym-management-system.slnx --verify-no-changes
```

Backend build:

```powershell
dotnet build multi-gym-management-system.slnx
```

Backend tests:

```powershell
dotnet test multi-gym-management-system.slnx
```

PostgreSQL/Testcontainers persistence tests:

```powershell
$env:RUN_POSTGRES_TESTS = "1"
dotnet test multi-gym-management-system.slnx --filter PostgreSql
Remove-Item Env:\RUN_POSTGRES_TESTS
```

Bash equivalent:

```bash
RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx --filter PostgreSql
```

These tests are intentionally skipped during the normal `dotnet test` run.
They start a real `postgres:16-alpine` container through Testcontainers and
therefore require a Docker engine that the test process can reach. Keeping
them opt-in preserves normal local and CI behavior on machines or runners that
do not expose Docker.

Separate client tests:

```powershell
cd client
npm test
```

Separate client production build:

```powershell
cd client
npm run build
```

## Latest Validation Snapshot

Validated locally on 2026-05-11:

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass, no files changed |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx` | Pass, 250 passed, 3 skipped PostgreSQL/Testcontainers tests |
| `cd client && npm test` | Pass, 7 files / 34 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass |
| `docker compose config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` | Pass |

The PostgreSQL provider tests remained skipped because `RUN_POSTGRES_TESTS=1`
was not set. No browser or public VPS smoke test was run in this pass.

## Current Test Scope

Backend unit tests:
- `LangStrTests`
  - verifies translation fallback behavior
- `MembershipWorkflowServiceTests`
  - verifies membership overlap detection, suggested next start date, and membership status transition validation
- `RuntimeConfigurationTests`
  - verifies required JWT configuration, strict password policy, JWT HTTPS metadata behavior, production CORS fail-fast validation, and Data Protection key mapping
- `AppDbContextBehaviorTests`
  - verifies EF tenant audit-log writes and soft-delete query filtering behavior
- `AuthorizationServiceTests`
  - verifies active-gym checks, role checks, member self access, trainer assignment checks, and caretaker assignment checks
- `ApiContractMetadataTests`
  - verifies all public API controllers expose `ProblemDetails` metadata for `400`, `401`, `403`, `404`, and `409`
- `TenantControllerTests` and `AdditionalControllerTests`
  - verify tenant/system/identity controller parameter forwarding and current response-shape contracts across members, member workspace, finance workspace, bookings, memberships, training sessions, coaching plans, maintenance, staff, platform, subscriptions, support, and impersonation
- `SubscriptionTierLimitServiceTests`
  - verifies starter-plan creation limits and enterprise-plan unlimited behavior

Backend integration tests:
- `SmokeTests.HomePage_ReturnsSuccess`
- `SmokeTests.Register_ReturnsJwtPayload`
- `SmokeTests.Login_SeededMultiGymAdmin_CanSwitchGym`
- `SmokeTests.SplitTenantApiControllers_KeepExistingReadRoutes`
- `SmokeTests.AdminDashboard_QuickLinks_ExposeFunctionalSaasRoutes`
- `SmokeTests.SystemAdmin_GymsRoute_RendersMvcPage`
- `SmokeTests.TenantAdmin_WorkspaceRoutes_RenderMvcPages`
- `MvcComplianceTests.AnonymousUser_CannotAccess_Admin`
- `MvcComplianceTests.WrongRole_CannotAccess_Admin`
- `MvcComplianceTests.GymAdminOrGymOwner_CanAccess_TenantAdminPages`
- `MvcComplianceTests.MvcClientRoute_Works_ForTenantRoles`
- `MvcComplianceTests.AdminViews_DoNotUse_ViewBagOrViewData`
- `MvcComplianceTests.AdminPostActions_UseAntiForgery`
- `MvcComplianceTests.AdminControllers_ReturnStronglyTypedViewModels`
- `AdminMembersCrudTests.*`
  - MVC Admin members index/create/edit/delete, validation, authorization, and
    cross-tenant id denial
- `AdminTrainingCategoriesCrudTests.*`
  - MVC Admin training category index/create/edit/delete, validation,
    localized `LangStr` rendering, authorization, and cross-tenant id denial
- `AdminMembershipPackagesCrudTests.*`
  - MVC Admin membership package index/create/edit/delete, validation,
    active-gym persistence, authorization, and cross-tenant id denial
- `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse`
- `AuthSecurityAndErrorTests.RenewRefreshToken_RejectsExpiredRefreshToken`
- `AuthSecurityAndErrorTests.MembersEndpoint_RejectsActiveGymMismatch`
- `AuthSecurityAndErrorTests.MembersEndpoint_RejectsUnknownGym_Early`
- `AuthSecurityAndErrorTests.MembersEndpoint_RejectsInactiveGym_Early`
- `AuthSecurityAndErrorTests.Member_CannotReadAnotherMember`
- `AuthSecurityAndErrorTests.SystemPlatformAnalytics_RejectsTenantOnlyUser`
- `AuthSecurityAndErrorTests.ApiErrors_ReturnProblemDetailsJson`
- `AuthSecurityAndErrorTests.TenantApi_UsesAcceptLanguageForLangStrResponses`
- `AuthSecurityAndErrorTests.SetCulture_StoresOnlySupportedCultureCookie`
- `AuthSecurityAndErrorTests.HtmlErrors_RenderHtmlErrorPage`
- `Final1CriticalE2ETests.Login_E2E_ReturnsTenantSession`
- `Final1CriticalE2ETests.MemberCrud_E2E_CreateReadUpdateDelete`
- `Final1CriticalE2ETests.TrainingCategoryCrud_E2E_CreateReadUpdateDelete`
- `Final1CriticalE2ETests.MembershipPackageCrud_E2E_CreateReadUpdateDelete`
- `Final1CriticalE2ETests.IdorNegative_E2E_CrossTenantMemberUpdateReturns404`
- `ProposalWorkflowTests.ReactClientFallback_ServesClientShell`
- `ProposalWorkflowTests.TrainingSessions_HandleNullableDescriptionInListAndDetail`
- `ProposalWorkflowTests.MemberBooking_RequiresPaymentReferenceWhenPaymentIsDue`
- `ProposalWorkflowTests.MemberCreate_ReturnsValidationProblemForDuplicateMemberCode`
- `ProposalWorkflowTests.BookingCreate_ReturnsValidationProblemForDuplicateSessionBooking`
- `ProposalWorkflowTests.TrainerAttendance_UpdateIsLimitedToAssignedTrainerSessions`
- `ProposalWorkflowTests.CaretakerStatus_UpdateIsLimitedToAssignedTasks`
- `StaffWorkflowTests.StaffRelatedTenantEndpoints_SupportCrudThroughBllServices`
- `StaffWorkflowTests.StaffRelatedTenantEndpoints_ReturnProblemDetailsForMissingResources`
- `StaffWorkflowTests.StaffRelatedTenantEndpoints_RejectWrongActiveGym`
- `StaffWorkflowTests.ContractCreate_RejectsStaffFromAnotherGym`
- `MembershipPackageCrudTests.GetMembershipPackages_ReturnsListForActiveGym`
- `MembershipPackageCrudTests.CreateMembershipPackage_Returns201`
- `MembershipPackageCrudTests.CreateMembershipPackage_InvalidPrice_ReturnsProblemDetails`
- `MembershipPackageCrudTests.CreateMembershipPackage_InvalidDuration_ReturnsProblemDetails`
- `MembershipPackageCrudTests.CreateMembershipPackage_MissingCurrency_ReturnsProblemDetails`
- `MembershipPackageCrudTests.UpdateMembershipPackage_ReturnsUpdatedPackage`
- `MembershipPackageCrudTests.DeleteUnusedMembershipPackage_SoftDeletesPackage`
- `MembershipPackageCrudTests.DeleteUsedMembershipPackage_ReturnsConflictAndKeepsMembershipSnapshot`
- `MembershipPackageCrudTests.UpdateMembershipPackage_ForeignGymPackageId_Returns404`
- `ImpersonationTests.StartImpersonation_WritesAuditRefreshTokenAndClaims`
- `PostgreSqlPersistenceTests.*`
  - optional Docker-backed slice for tenant query filtering, PostgreSQL unique-index enforcement, and `LangStr` JSONB round-trip behavior
  - skipped unless `RUN_POSTGRES_TESTS=1` is set

Frontend Vitest coverage:
- auth guard redirects anonymous users to login
- logout clears persisted session state
- system roles route to the React platform console
- assigned multi-gym users can switch active tenant and role from the shell
- role-aware landing routes send members/trainers to workspace pages
- API client retries once through refresh on `401`
- refresh failure clears session state and surfaces an auth error
- API requests include the selected `Accept-Language`
- production API base defaults to same-origin while development defaults to `https://localhost:7245`
- members page create success and request-error handling
- training categories page update success and save-error handling
- sessions page detail loading and booking success
- trainer attendance update success
- caretaker maintenance task status update success
- membership packages page loading, create, update, delete, local validation, and save-error handling
- finance workspace invoice-payment action success
- coaching workspace item-decision update success

## Manual Smoke Checklist

Recommended manual verification before defense:
1. Start PostgreSQL and run migrations.
2. Start `WebApp`.
3. Open `/swagger` and `/health`.
4. Log in to the MVC admin area with `multigym.admin@gym.local` and switch between `peak-forge` and `north-star`.
   - verify Admin Dashboard, Members, Memberships, Sessions, and Operations render Razor MVC pages
5. Open the MVC client area with `member@peakforge.local`.
6. Start the React client and log in with `admin@peakforge.local`.
   - seeded/demo password: `GymStrong123!`
7. In the React client, create, edit, and delete a member.
8. In the React client, create, edit, and delete a training category.
9. In the React client, create, edit, and delete a membership package.
10. In the React client, open Sessions and create a booking for a selected member with a payment reference.
11. Open `/member-workspace` as `member@peakforge.local` and verify profile, memberships, bookings, payments, and outstanding actions render.
12. Log in as `trainer@peakforge.local`, open `/coaching-workspace`, and validate coaching-plan create/status/item-decision flows.
13. As owner/admin, open `/finance-workspace`, create an invoice, post a payment, then post a refund and verify outstanding balance updates.
14. Log in as `caretaker@peakforge.local`, open `/maintenance`, and update task status; as admin verify assignment update/history and due-generation actions.
15. Confirm the React client stays authenticated across an access-token refresh.
16. Log in as `systemadmin@gym.local`, open `/platform`, and verify platform analytics/gyms/subscriptions/support data loads.
17. From `/console`, run one safe GET action such as `GET /api/v1/{gymCode}/gym-settings`.
18. Switch the React language selector to `ET` and confirm translated seeded API values such as `Joutreening` appear after refresh.
19. After a production Docker build/deploy, open `/client`, `/client/platform`, `/client/member-workspace`, and `/client/finance-workspace` on the backend host.

## Test Notes

- Backend integration tests use EF Core InMemory and seeded demo data.
- Final1 critical E2E coverage is API-level integration coverage through ASP.NET Core `WebApplicationFactory`; the repository does not currently include Playwright browser automation.
- Backend integration tests inject test-only JWT configuration through environment variables before the host starts.
- The HTML error-page test uses a production-style test host because MVC exception handling is only enabled outside development.
- Frontend tests run in `jsdom` and mock network traffic directly.
- The assignment CI pipeline now verifies the React client before the .NET build and test stages.
- The normal CI `assignment03_test` job preserves the default skip behavior for PostgreSQL/Testcontainers tests.
- The optional GitLab `assignment03_postgresql_tests` manual job sets `RUN_POSTGRES_TESTS=1` and runs `dotnet test multi-gym-management-system.slnx --configuration Release --no-build --filter PostgreSql`; start it only on a Docker-capable runner.
- `WebApp.Tests` pins security-sensitive transitive test dependencies to the patched 10.0.7 line so the dependency chain stays free of the April 2026 high-severity advisories.
