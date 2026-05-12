# Users Module Contracts

**Status:** Phase 17 contract baseline.

The Users module publishes only mediator request messages for the auth/session
slice. Handlers and session services are internal to `Modules.Users`.

## Public API Compatibility

The HTTP contract remains the existing account API:

| Route | Request | Response | Auth |
|---|---|---|---|
| `POST /api/v1/account/login` | `LoginRequest` | `JwtResponse` | anonymous |
| `POST /api/v1/account/renew-refresh-token` | `RefreshTokenRequest` | `JwtResponse` | anonymous |
| `POST /api/v1/account/logout` | none | `Message` | JWT |
| `POST /api/v1/account/switch-gym` | `SwitchGymRequest` | `JwtResponse` | JWT |
| `POST /api/v1/account/switch-role` | `SwitchRoleRequest` | `JwtResponse` | JWT |

No client contract changes are required.

## Mediator Contract Namespace

Users mediator messages live in:

`Modules.Users.Contracts`

The WebApp host may reference this namespace because it is the composition
root and endpoint adapter. Other modules must not reference
`Modules.Users.Application`, `Modules.Users.Infrastructure`, or
`Modules.Users.Domain`.

## Published Messages

| Message | Response | Purpose |
|---|---|---|
| `LoginCommand(LoginRequest Request)` | `JwtResponse` | Authenticate credentials, select default active gym/role, create JWT and refresh token. |
| `RefreshSessionCommand(RefreshTokenRequest Request)` | `JwtResponse` | Validate expired JWT identity, rotate refresh token, preserve active gym/role claims when still valid. |
| `LogoutCommand()` | `Message` | Remove all refresh tokens for the current authenticated user. |
| `SwitchGymCommand(SwitchGymRequest Request)` | `JwtResponse` | Issue a new JWT for another gym the user can access. |
| `SwitchRoleCommand(SwitchRoleRequest Request)` | `JwtResponse` | Issue a new JWT for another role inside the active gym. |

## Boundary Rules

- Controllers adapt HTTP requests into mediator messages.
- Handlers stay internal to `Modules.Users.Application.Auth`.
- Other modules communicate with Users only through published mediator
  messages, not through Users application services.
- Auth/session persistence remains behind existing ports for this phase:
  `IAppDbContext`, `IAppUnitOfWork`, and `IRefreshTokenRepository`.
- `JwtResponse`, `Message`, and account request DTOs remain in `App.DTO`
  until the public API versioning strategy changes.

## Validation and Errors

The module preserves existing error semantics:

- invalid login credentials: `ValidationAppException`
- malformed refresh requests: `ValidationAppException`
- invalid or expired refresh token: `ForbiddenException`
- missing authenticated user for switch operations: `ForbiddenException`
- missing user during refresh or switch: `NotFoundException`

The WebApp `ProblemDetails` pipeline continues to translate these exceptions
into stable API error responses.
