# Users Mediator Messages

**Status:** Implemented for Phase 17 auth/session slice.

This document maps each Users-module auth/session message to its handler,
endpoint adapter, and key invariants.

## Message Flow

```text
HTTP request
  -> WebApp AccountController
  -> IMediator.SendAsync(...)
  -> Modules.Users.Application.Auth handler
  -> IUsersSessionService
  -> Identity, token service, refresh-token repository, AppDbContext
```

The controller keeps route attributes, API versioning, Swagger response
metadata, and authorization attributes. The Users module owns the session
behavior behind the mediator.

## Messages

### `LoginCommand`

- Endpoint: `POST /api/v1/account/login`
- Handler: `LoginCommandHandler`
- Service method: `IUsersSessionService.LoginAsync`
- Input DTO: `LoginRequest`
- Output DTO: `JwtResponse`
- Invariants:
  - email and password must match an Identity user
  - default active gym/role is selected from active `AppUserGymRole` links
  - a JWT and new refresh token are returned

### `RefreshSessionCommand`

- Endpoint: `POST /api/v1/account/renew-refresh-token`
- Handler: `RefreshSessionCommandHandler`
- Service method: `IUsersSessionService.RefreshAsync`
- Input DTO: `RefreshTokenRequest`
- Output DTO: `JwtResponse`
- Invariants:
  - JWT is validated without lifetime validation so expired access tokens can
    be refreshed
  - refresh token must belong to the JWT user and be unexpired
  - old refresh token is removed
  - replacement refresh token stores previous-token metadata
  - active gym/role claims are preserved only if still valid

### `LogoutCommand`

- Endpoint: `POST /api/v1/account/logout`
- Handler: `LogoutCommandHandler`
- Service method: `IUsersSessionService.LogoutAsync`
- Output DTO: `Message`
- Invariants:
  - authenticated user's refresh tokens are removed
  - missing user context is treated as idempotent logout
  - response remains `Logged out.`

### `SwitchGymCommand`

- Endpoint: `POST /api/v1/account/switch-gym`
- Handler: `SwitchGymCommandHandler`
- Service method: `IUsersSessionService.SwitchGymAsync`
- Input DTO: `SwitchGymRequest`
- Output DTO: `JwtResponse`
- Invariants:
  - caller must be authenticated
  - target gym must be active and assigned to the user
  - system admin may select any active gym as `GymOwner`
  - response includes a newly issued JWT and refresh token

### `SwitchRoleCommand`

- Endpoint: `POST /api/v1/account/switch-role`
- Handler: `SwitchRoleCommandHandler`
- Service method: `IUsersSessionService.SwitchRoleAsync`
- Input DTO: `SwitchRoleRequest`
- Output DTO: `JwtResponse`
- Invariants:
  - caller must be authenticated and have an active gym
  - requested role must be assigned in the active gym
  - system admin may select `GymOwner` or `GymAdmin`
  - response includes a newly issued JWT and refresh token

## Registration

`AddUsersModule` registers:

- `IUsersSessionService -> UsersSessionService`
- all `IRequestHandler<,>` implementations from the `Modules.Users` assembly

`AddAppModules` registers `AddBuildingBlocks()` first, then
`AddUsersModule()`, so `IMediator` and its handlers are available to
`AccountController`.

## Test Evidence

- `SmokeTests.AccountPublicApi_LoginRefreshAndLogout_StillWorkThroughStableRoutes`
- `SmokeTests.AccountPublicApi_SystemAdmin_CanSwitchRoleInsideSelectedGym`
- `SmokeTests.Login_SeededMultiGymAdmin_CanSwitchGym`
- `AdditionalControllerTests.AccountController_ForwardsParametersAndReturnsCurrentResultShapes`
- `ArchitectureTests.AccountAuthSlice_IsMediatedThroughUsersModule`
- `ModuleArchitectureTests.NonUsersModules_DoNotReferenceUsersInternals`
