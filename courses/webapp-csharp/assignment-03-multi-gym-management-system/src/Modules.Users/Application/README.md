# Modules.Users - Application

Module-internal services and mediator handlers for Users-owned behavior live
here.

Moved in Phase 17:
- account auth/session behavior for login, refresh, logout, switch gym, and
  switch role
- mediator handlers in `Application/Auth`
- session orchestration in `UsersSessionService`

Slices that still need a later Users-module migration:
- identity provisioning (`IIdentityService`, `IdentityService`)
- token issuance (`ITokenService`, `TokenService`)
- user context (`IUserContextService`, `UserContextService`)
- refresh-token repository contract (the EF implementation stays in `App.DAL.EF`)
- auth response mapping (`IAuthResponseMapper`, `AuthResponseMapper`)
