# Security Token Audit

## JWT (access token)

| Property | Value |
|---|---|
| Algorithm | HMAC-SHA256 (`HmacSha256`) |
| Signing key source | `Jwt:Key` config (required, throws at startup if missing) |
| Default lifetime | 60 min (configurable via `Jwt:AccessTokenMinutes`) |
| Clock skew | 30 s |
| `ValidateIssuer` | yes (`Jwt:Issuer`) |
| `ValidateAudience` | yes (`Jwt:Audience`) |
| `ValidateIssuerSigningKey` | yes |
| `ValidateLifetime` | yes (except inside `GetPrincipalFromExpiredToken`) |
| HTTPS metadata required | production only (`RequireHttpsMetadata = !IsDevelopment()`) |

**Custom claims in token:**

| Claim | Description |
|---|---|
| `sub` / `ClaimTypes.NameIdentifier` | `AppUser.Id` (Guid) |
| `ClaimTypes.Role` | system roles + active tenant role |
| `active_gym_id` | `AppUserGymRole.GymId` |
| `active_gym_code` | `Gym.Code` |
| `active_role` | `AppUserGymRole.RoleName` |
| `person_id` | `AppUser.PersonId` (when set) |
| Impersonation claims | only when SystemAdmin is impersonating |

## Refresh token

| Property | Value |
|---|---|
| Length | 64 bytes → Base64URL encoded |
| Generator | `RandomNumberGenerator.GetBytes` (cryptographically secure) |
| Lifetime | 30 days |
| Storage | `AppRefreshToken` table, keyed by `UserId + RefreshToken` |
| Rotation | Old token removed, new token added on every successful renewal |
| Grace record | Previous token value preserved in `PreviousRefreshToken` (not usable, kept for audit) |

## Attack surface

### Replay attack with old refresh token
The old refresh token row is deleted and replaced. Attempting to use it again returns 403
because the lookup `token.RefreshToken == request.RefreshToken` finds nothing.
Covered by `RenewRefreshToken_RotatesToken_AndRejectsReuse`.

### Expired refresh token
`token.Expiration <= DateTime.UtcNow` check returns 403.
Covered by `RenewRefreshToken_RejectsExpiredRefreshToken`.

### Invalid / tampered JWT submitted to renew endpoint
`GetPrincipalFromExpiredToken` validates signature and algorithm but not lifetime.
A JWT with wrong signature throws `SecurityTokenException`. This is caught in
`RenewRefreshTokenAsync` and re-thrown as `ValidationAppException` → HTTP 400.
Covered by `RenewRefreshToken_RejectsInvalidJwt`.

### Session hijack via refresh token
An attacker who steals both the JWT and the refresh token can renew. Mitigations:
- 30-day refresh window limits exposure.
- Logout deletes all refresh tokens for the user.
- Tokens are stored server-side; client stores them in `sessionStorage` (not `localStorage` or cookies), so they do not persist across browser sessions.

### Cross-tenant access
The active gym and role are embedded in the JWT. `GymResolutionMiddleware` validates
that the `{gymCode}` route parameter matches the active gym claim. Access to another
gym requires a `switch-gym` call, which validates the user has a link in `AppUserGymRole`.
Covered by `MembersEndpoint_RejectsActiveGymMismatch`.

## Key rotation

`Jwt:Key` must be set via environment variable or secrets manager in production.
The default dev key in `appsettings.Development.json` is explicitly labeled for
development only. Rotating the key invalidates all live JWTs immediately; refresh
tokens remain in the database but a renewal attempt will fail signature validation
(400), requiring users to re-login.
