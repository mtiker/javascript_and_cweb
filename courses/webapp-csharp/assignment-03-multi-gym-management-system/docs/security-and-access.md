# Security And Access

This document consolidates the previous auth, token, role, IDOR, tenant, and
CORS notes.

## Authentication Modes

The application uses:
- ASP.NET Core Identity for user management
- JWT access tokens for REST API calls
- refresh-token rotation for API sessions
- MVC cookie auth for server-rendered Admin and Client areas

Required JWT configuration:
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

Local development should set these through user secrets from `src/WebApp`.
Production should set them through environment variables such as `JWT__Key`.
The app must not rely on committed production secrets.

## Token Handling

REST login returns:
- access token
- refresh token
- expiration metadata
- active gym context
- tenant role context
- system roles
- available tenant/role assignments

Refresh-token rules:
- refresh rotates the token
- old refresh token reuse is rejected
- logout invalidates the refresh token
- expired or malformed refresh tokens are rejected
- refresh-token persistence is behind `IRefreshTokenRepository`

Known limitation:
- the React client currently stores the refresh token in `sessionStorage`
- this is acceptable for the current assignment phase only because rotation,
  reuse rejection, logout invalidation, and server-side lookup are in place
- a future hardening pass should move refresh tokens to `HttpOnly`, `Secure`,
  `SameSite` cookies and add matching CSRF/CORS protections

## Roles

Platform roles:
- `SystemAdmin`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

High-level access matrix:

| Area | SystemAdmin | GymOwner | GymAdmin | Member | Trainer | Caretaker |
|---|---|---|---|---|---|---|
| Platform analytics | yes | no | no | no | no | no |
| Gym onboarding/activation | yes | no | no | no | no | no |
| Tenant admin data | via selected gym context | yes | yes | no | limited | limited |
| Member self workspace | no | no | no | own data | no | no |
| Trainer attendance | no | yes | yes | no | assigned sessions | no |
| Caretaker maintenance | no | yes | yes | no | no | assigned tasks |

System users are not allowed to bypass tenant checks silently. When a system
user works with tenant routes, the active gym context must still be selected.

## Tenant Isolation

Tenant root:
- `Gym`

Tenant-owned rows carry `GymId`. Tenant routes use:
- `/api/v1/{gymCode}/...`

Isolation gates:
1. `GymResolutionMiddleware` resolves route `gymCode` early and rejects unknown
   or inactive gyms.
2. JWT claims carry active gym id and active gym code.
3. Tenant access checks require route gym code to match active gym context.
4. Role checks enforce tenant permissions.
5. Resource checks enforce self-only or assignment-only rules.
6. EF query filters apply tenant scoping for tenant-owned entities.

Important rules:
- tenant users cannot access another gym by changing `{gymCode}`
- members can only read their own member profile/workspace
- trainers can update attendance only for sessions they are assigned to
- caretakers can update maintenance tasks only when assigned
- cross-tenant IDs return `404` or `403` depending on whether the failure is
  resource-not-found or forbidden access

## MVC Security

MVC Admin:
- protected by role checks
- uses strongly typed view models
- POST actions use anti-forgery protection
- tenant admin mutations must verify active gym access

MVC Client:
- mounted under `/mvc-client`
- member/trainer/caretaker routes use cookie auth
- client actions must preserve the same self-only and assignment-only rules as
  API workflows

Known cleanup target:
- some MVC Client and Home/workspace switching code still uses direct
  `AppDbContext`; migrate those paths behind page/query services before
  claiming a fully clean presentation boundary.

## API Error Behavior

Public API controllers advertise `ProblemDetails` metadata for:
- `400`
- `401`
- `403`
- `404`
- `409`

Typical mappings:
- validation failures -> `400`
- missing/invalid auth -> `401`
- forbidden access -> `403`
- missing resources -> `404`
- conflicts -> `409`
- unexpected server failures -> `500`

## CORS

The backend exposes a named `ClientApp` CORS policy.

Development allowed origins:
- `http://localhost:5173`
- `https://localhost:5173`
- `http://127.0.0.1:5173`

Production requirements:
- configure explicit public origins only
- no wildcard origins
- no localhost or loopback origins in production
- set the standalone client origin when Mode B deployment is used

Production variables:
- `CORS_ALLOWED_ORIGIN` for the primary backend/client origin
- `CORS_ALLOWED_ORIGIN_CLIENT` for standalone client hosting

Mode A, embedded client:
- React is served from `/client` on the backend origin
- browser CORS is not exercised for normal same-origin API calls

Mode B, standalone client:
- React runs behind a separate origin
- backend CORS must allow the client origin
- smoke verification must include an OPTIONS preflight against the deployed API

## Security Validation

Relevant automated tests:
- `AuthSecurityAndErrorTests`
- `AuthorizationServiceTests`
- `TenantIsolationAndIdorTests`
- `AdminMembersCrudTests`
- `AdminTrainingCategoriesCrudTests`
- `AdminMembershipPackagesCrudTests`
- `ApiContractMetadataTests`
- `RuntimeConfigurationTests`
- `ImpersonationTests`

Manual defense checks:
1. Login as a tenant admin and access the active gym.
2. Switch to another assigned gym and verify tenant data changes.
3. Attempt a tenant route with the wrong `gymCode`; expect rejection.
4. Login as a member and attempt another member's profile; expect rejection.
5. Login as trainer/caretaker and try an unassigned workflow; expect rejection.
6. In production Mode B, verify CORS preflight from the standalone client host.
