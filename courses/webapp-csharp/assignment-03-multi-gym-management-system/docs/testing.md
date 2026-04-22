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
  - verifies membership overlap detection and suggested next start date

Backend integration tests:
- `SmokeTests.HomePage_ReturnsSuccess`
- `SmokeTests.Register_ReturnsJwtPayload`
- `SmokeTests.Login_SeededMultiGymAdmin_CanSwitchGym`
- `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse`
- `AuthSecurityAndErrorTests.RenewRefreshToken_RejectsExpiredRefreshToken`
- `AuthSecurityAndErrorTests.MembersEndpoint_RejectsActiveGymMismatch`
- `AuthSecurityAndErrorTests.Member_CannotReadAnotherMember`
- `AuthSecurityAndErrorTests.SystemPlatformAnalytics_RejectsTenantOnlyUser`
- `AuthSecurityAndErrorTests.ApiErrors_ReturnProblemDetailsJson`
- `AuthSecurityAndErrorTests.TenantApi_UsesAcceptLanguageForLangStrResponses`
- `AuthSecurityAndErrorTests.SetCulture_StoresOnlySupportedCultureCookie`
- `AuthSecurityAndErrorTests.HtmlErrors_RenderHtmlErrorPage`
- `ProposalWorkflowTests.ReactClientFallback_ServesClientShell`
- `ProposalWorkflowTests.TrainingSessions_HandleNullableDescriptionInListAndDetail`
- `ProposalWorkflowTests.MemberBooking_RequiresPaymentReferenceWhenPaymentIsDue`
- `ProposalWorkflowTests.TrainerAttendance_UpdateIsLimitedToAssignedTrainerSessions`
- `ProposalWorkflowTests.CaretakerStatus_UpdateIsLimitedToAssignedTasks`

Frontend Vitest coverage:
- auth guard redirects anonymous users to login
- logout clears persisted session state
- system roles route to the React platform console
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

## Manual Smoke Checklist

Recommended manual verification before defense:
1. Start PostgreSQL and run migrations.
2. Start `WebApp`.
3. Open `/swagger` and `/health`.
4. Log in to the MVC admin area with `multigym.admin@gym.local` and switch between `peak-forge` and `north-star`.
5. Open the MVC client area with `member@peakforge.local`.
6. Start the React client and log in with `admin@peakforge.local`.
7. In the React client, create, edit, and delete a member.
8. In the React client, create, edit, and delete a training category.
9. In the React client, create, edit, and delete a membership package.
10. In the React client, open Sessions and create a booking for a selected member with a payment reference.
11. Log in as `trainer@peakforge.local` and update attendance from the React Attendance page.
12. Log in as `caretaker@peakforge.local` and update a maintenance task from the React Maintenance page.
13. Confirm the React client stays authenticated across an access-token refresh.
14. Log in as `systemadmin@gym.local`, open `/platform`, and verify platform analytics/gyms/subscriptions/support data loads.
15. From `/console`, run one safe GET action such as `GET /api/v1/{gymCode}/gym-settings`.
16. Switch the React language selector to `ET` and confirm translated seeded API values such as `Jõutreening` appear after refresh.
17. After a production Docker build/deploy, open `/client`, `/client/platform`, and `/client/members` on the backend host.

## Test Notes

- Backend integration tests use EF Core InMemory and seeded demo data.
- The HTML error-page test uses a production-style test host because MVC exception handling is only enabled outside development.
- Frontend tests run in `jsdom` and mock network traffic directly.
- The assignment CI pipeline now verifies the React client before the .NET build and test stages.
- `WebApp.Tests` pins `System.Security.Cryptography.Xml` to the patched 10.0.6 line so the transitive test dependency chain stays free of the April 2026 high-severity advisories.
