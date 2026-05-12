# Final2 Users Module Plan

**Status:** Phase 17 implemented. Auth/session behavior for the public
account API now runs through `Modules.Users` mediator handlers. Member CRUD is
not migrated in this phase.

## Scope

This phase moves the following account-session use cases into the Users module:

- login through `POST /api/v1/account/login`
- refresh-token rotation through `POST /api/v1/account/renew-refresh-token`
- logout refresh-token invalidation through `POST /api/v1/account/logout`
- active gym switching through `POST /api/v1/account/switch-gym`
- active role switching through `POST /api/v1/account/switch-role`

The route surface, request DTOs, response DTOs, JWT claims, refresh-token
rotation semantics, and `ProblemDetails` error behavior remain unchanged.

## Non-goals

- Do not migrate member CRUD.
- Do not move registration, forgot-password, or reset-password yet.
- Do not introduce a second `DbContext`, database schema split, or migration.
- Do not add direct module-to-module project references.

## Design

`AccountController` remains in `WebApp` as a thin endpoint adapter because the
host owns routing, versioning, authentication attributes, and Swagger metadata.
For the migrated session operations it creates Users-module mediator messages
and dispatches them through `IMediator`.

Handlers live in `Modules.Users.Application.Auth` and call the module-internal
`IUsersSessionService`. That service owns the moved auth/session behavior:
password login, default active tenant selection, refresh-token lookup and
rotation, logout invalidation, system-admin tenant context switching, and auth
response construction.

The module still reuses existing stable application ports while the modular
monolith migration is in progress:

- `IAppDbContext`
- `IAppUnitOfWork`
- `ITokenService`
- `IUserContextService`
- `IAuthResponseMapper`
- ASP.NET Core Identity `UserManager<AppUser>`

This keeps the phase small and route-compatible while moving the runtime entry
point and session orchestration into `Modules.Users`.

## Files

| Concern | File |
|---|---|
| Public mediator messages | `src/Modules.Users/Contracts/AuthSessionMessages.cs` |
| Users session behavior | `src/Modules.Users/Application/Auth/UsersSessionService.cs` |
| Mediator handlers | `src/Modules.Users/Application/Auth/AuthSessionHandlers.cs` |
| Module DI | `src/Modules.Users/UsersModuleServiceCollectionExtensions.cs` |
| Endpoint adapter | `src/WebApp/ApiControllers/Identity/AccountController.cs` |
| App service DI cleanup | `src/WebApp/Setup/ServiceExtensions.cs` |

## Tests

Coverage added or updated:

- public API login, refresh, logout route compatibility
- public API switch-role behavior after gym selection
- controller adapter sends `LoginCommand`, `RefreshSessionCommand`,
  `LogoutCommand`, `SwitchGymCommand`, and `SwitchRoleCommand`
- architecture test verifies account auth is mediated through Users contracts
- architecture test verifies non-Users modules do not reference Users internals

Existing smoke coverage already verifies login and switch-gym behavior for a
multi-gym tenant admin and system admin.

## Risks and Tradeoffs

The old `AccountAuthService` remains in `App.BLL` as legacy code because this
phase is scoped to moving runtime behavior, not deleting all historical Final1
types. It is no longer registered in the WebApp service collection and is not
used by `AccountController`.

The Users module still references `App.BLL` transition ports. That is accepted
for Final2 migration phases and should shrink after identity provisioning,
context, and mapping contracts are moved to module-owned ports.

## Definition of Done

- [x] Public account routes are unchanged.
- [x] Login is mediated through `Modules.Users`.
- [x] Refresh-token rotation is mediated through `Modules.Users`.
- [x] Logout invalidation is mediated through `Modules.Users`.
- [x] Switch gym and switch role are mediated through `Modules.Users`.
- [x] Member CRUD remains outside this phase.
- [x] Focused backend tests pass.
