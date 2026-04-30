# Auth Service Boundary Audit

## Scope

Audited endpoints:

- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`

Excluded endpoints:

- register
- forgot/reset password
- switch-gym
- switch-role
- MVC cookie login/logout

## Boundary Before Phase 10

`AccountController` delegated to `IIdentityService`, but `IdentityService`
contained a broad mix of responsibilities:

- user registration
- login credential validation
- logout refresh-token invalidation
- refresh-token lookup, expiry checks, deletion, and replacement
- tenant-switching
- role-switching
- password reset
- JWT response assembly

The controller was thin, but the application service boundary was too broad for
Clean/Onion defense because account-session use cases were coupled to unrelated
identity-management use cases.

## Boundary After Phase 10

`AccountController` now has two explicit BLL dependencies:

| Dependency | Owns |
|---|---|
| `IIdentityService` | register, switch-gym, switch-role, forgot-password, reset-password |
| `IAccountAuthService` | login, logout, refresh-token renewal |

`IAccountAuthService` owns session use cases only:

- credential login
- JWT/refresh-token response construction
- refresh-token rotation
- refresh-token reuse rejection
- logout token invalidation

Persistence is split:

- refresh-token reads/writes go through `IAppUnitOfWork.RefreshTokens`
- EF-specific queries live in `EfRefreshTokenRepository`
- active tenant-role reads still use `IAppDbContext` as a BLL infrastructure port

DTO projection is split:

- `AuthResponseMapper` maps JWT, refresh token, active tenant context, system roles,
  and available tenants into `JwtResponse`
- `AccountController` does not build public auth DTOs

## Public Contract Check

The following remained unchanged:

| Endpoint | Request DTO | Response DTO |
|---|---|---|
| `POST /api/v1/account/login` | `LoginRequest` | `JwtResponse` |
| `POST /api/v1/account/logout` | none | `Message` |
| `POST /api/v1/account/renew-refresh-token` | `RefreshTokenRequest` | `JwtResponse` |

The route prefix remains:

```text
api/v{version:apiVersion}/account
```

## Tests Guarding The Boundary

| Test | Purpose |
|---|---|
| `AccountController_ForwardsParametersAndReturnsCurrentResultShapes` | controller delegates session endpoints to `IAccountAuthService` and keeps current result shapes |
| `AccountAuthPublicRoutesAndDtos_RemainStable` | locks public route templates, request DTOs, and response DTOs |
| `AccountAuthSlice_UsesDedicatedServiceRepositoryAndMapperBoundaries` | locks service, repository, UOW, and mapper contracts |
| `AuthSecurityAndErrorTests` auth tests | verify login, rotation, reuse rejection, invalid JWT rejection, expired token rejection, and logout invalidation |

## Residual Risks

- `AccountAuthService` still needs active-tenant read queries that are easier to
  express through `IAppDbContext` than through the current generic repository.
  This is acceptable for Phase 10 because the dependency is a BLL-owned port,
  not concrete infrastructure.
- `IdentityService` still creates refresh tokens for register and tenant/role
  switch responses. That behavior is outside this auth-clean slice and should be
  handled when the remaining identity/session response paths are migrated.
- `UserManager<AppUser>` remains in the BLL service. This is currently consistent
  with the project identity setup; fully abstracting ASP.NET Core Identity is a
  larger identity-decoupling phase.
