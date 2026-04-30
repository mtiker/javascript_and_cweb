# Final1 Auth Slice Plan

## Status

Implemented in Phase 10.

The `Account/Login/Refresh/Logout` behavior has been moved behind a dedicated
application service boundary while preserving the public API contract:

- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`

No unrelated profile, registration, password-reset, tenant switching, or user
management behavior was migrated in this slice.

## Course Context

This aligns the Assignment 03 backend with the Final1 Clean/Onion requirements:

- API controllers are boundary adapters.
- Use cases live in the application/BLL layer.
- persistence details are behind BLL contracts.
- EF implementations live in infrastructure.
- DTO projection is handled by a mapper rather than controller code.

## Implemented Boundary

| Concern | Final location |
|---|---|
| Login use case | `src/App.BLL/Services/AccountAuthService.cs` |
| Refresh-token rotation | `src/App.BLL/Services/AccountAuthService.cs` |
| Refresh-token persistence contract | `src/App.BLL/Contracts/Persistence/IRefreshTokenRepository.cs` |
| Refresh-token EF implementation | `src/App.DAL.EF/Repositories/EfRefreshTokenRepository.cs` |
| Unit of Work refresh-token access | `IAppUnitOfWork.RefreshTokens` |
| Auth response mapping | `src/App.BLL/Mapping/AuthResponseMapper.cs` |
| Controller delegation | `src/WebApp/ApiControllers/Identity/AccountController.cs` |
| DI wiring | `src/WebApp/Setup/ServiceExtensions.cs`, `src/App.DAL.EF/PersistenceServiceExtensions.cs` |

## Request Flow

Login:

1. `AccountController.Login` receives `LoginRequest`.
2. Controller delegates to `IAccountAuthService.LoginAsync`.
3. `AccountAuthService` validates credentials through `UserManager<AppUser>`.
4. Active tenant role and available tenants are read through BLL infrastructure ports.
5. `ITokenService` creates JWT and refresh token.
6. refresh token is persisted through `IAppUnitOfWork.RefreshTokens`.
7. `IAuthResponseMapper` maps the public `JwtResponse`.

Refresh:

1. `AccountController.RenewRefreshToken` receives `RefreshTokenRequest`.
2. `AccountAuthService` validates the expired JWT signature and claims.
3. `IRefreshTokenRepository.GetByUserAndTokenAsync` loads the stored refresh token.
4. expired, missing, or reused refresh tokens are rejected.
5. valid token is removed and replaced through the Unit of Work.
6. the JWT is rebuilt with the same active gym/role context.
7. a new `JwtResponse` is returned with a rotated refresh token.

Logout:

1. `AccountController.Logout` delegates to `IAccountAuthService.LogoutAsync`.
2. current user id is resolved from `IUserContextService`.
3. all refresh tokens for that user are loaded through `IRefreshTokenRepository`.
4. tokens are removed and saved through `IAppUnitOfWork`.

## Tests

Coverage added or preserved:

- login returns JWT, refresh token, expiry, active tenant context, roles
- refresh-token rotation returns a new refresh token
- refresh-token reuse is rejected with 403
- logout invalidates existing refresh tokens
- invalid JWT on refresh returns 400
- expired refresh token returns 403
- `AccountController` delegates auth-session endpoints to `IAccountAuthService`
- public account route templates, request DTOs, and response DTOs remain unchanged
- architecture tests assert the dedicated service, repository, UOW, and mapper boundaries

## Out Of Scope

- registration
- forgot/reset password
- switch-gym and switch-role
- profile/user CRUD
- Identity model changes
- database schema changes
- endpoint path or DTO changes
- broader migration of all BLL services off `IAppDbContext`

## Remaining Work

The auth slice still uses `IAppDbContext` for active tenant-role and available-tenant
queries. That is an application-layer infrastructure port, not the concrete EF
context, and is intentionally left in place until the broader repository/UOW
migration covers read-model query needs across the BLL.
