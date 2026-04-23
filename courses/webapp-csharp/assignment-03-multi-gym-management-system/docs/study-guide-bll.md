# Study Guide: BLL Layer

## Purpose
`src/App.BLL` contains business rules, authorization gates, workflow orchestration, and DTO mapping.

## Architectural Boundaries
- Controllers depend on service interfaces, not EF context directly.
- BLL depends on `IAppDbContext` and infrastructure contracts.
- Authorization is always enforced in BLL even when middleware performs early validation.

## Core Services
- Identity and session: `IdentityService`, `TokenService`
- Tenant authorization: `AuthorizationService`
- Platform workflows: `PlatformService`
- Tenant workflows:
  - `MemberWorkflowService`
  - `TrainingWorkflowService`
  - `StaffWorkflowService`
  - `MembershipWorkflowService`
  - `MaintenanceWorkflowService`
  - `MemberWorkspaceService`
  - `CoachingPlanService`
  - `FinanceWorkspaceService`

## Subscription Enforcement
`SubscriptionTierLimitService` applies plan limits for member/staff/sessions/equipment creation with explicit unit coverage.

## Error Discipline
BLL throws structured application exceptions:
- `ValidationAppException`
- `ForbiddenException`
- `NotFoundException`

These are converted to `ProblemDetails` HTTP responses by API middleware.

## Defense Notes
- Tenant role and self-access checks remain in BLL as final authority.
- Route gym validation middleware is intentionally additive, not a replacement for service-level authorization.
