# Testing Guide

## Automated Verification Commands

Backend build:

```powershell
dotnet build multi-gym-management-system.slnx
```

Backend tests:

```powershell
dotnet test multi-gym-management-system.slnx
```

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
- `SmokeTests.SystemAdmin_GymsRoute_RedirectsToClientPlatform`
- `SmokeTests.TenantAdmin_WorkspaceRoutes_RedirectToClientWorkspaces`
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
- `ImpersonationTests.StartImpersonation_WritesAuditRefreshTokenAndClaims`

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
- membership packages page delete success and save-error handling
- finance workspace invoice-payment action success
- coaching workspace item-decision update success

## Manual Smoke Checklist

Recommended manual verification before defense:
1. Start PostgreSQL and run migrations.
2. Start `WebApp`.
3. Open `/swagger` and `/health`.
4. Log in to the MVC admin area with `multigym.admin@gym.local` and switch between `peak-forge` and `north-star`.
   - verify Admin quick links route into `/client/*` functional workflows
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
- Backend integration tests inject test-only JWT configuration through environment variables before the host starts.
- The HTML error-page test uses a production-style test host because MVC exception handling is only enabled outside development.
- Frontend tests run in `jsdom` and mock network traffic directly.
- The assignment CI pipeline now verifies the React client before the .NET build and test stages.
- `WebApp.Tests` pins `System.Security.Cryptography.Xml` to the patched 10.0.6 line so the transitive test dependency chain stays free of the April 2026 high-severity advisories.

