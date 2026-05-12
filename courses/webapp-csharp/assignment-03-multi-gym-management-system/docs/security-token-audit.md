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

## Client refresh-token storage tradeoff

Current behavior:
- The separate React client stores the serialized auth session in
  `sessionStorage` through `client/src/lib/storage.ts`.
- The stored session includes the access token, refresh token, active gym/role
  context, system roles, and available tenant metadata returned by the auth API.
- This is an accepted phase tradeoff, not the strongest browser-storage model.
  `sessionStorage` is JavaScript-readable, so an XSS vulnerability in the client
  origin could read the refresh token while the browser session is live.
- `sessionStorage` avoids longer-lived `localStorage` persistence and does not
  attach tokens automatically to cross-site requests, but it does not defend
  against same-origin script compromise.

Existing compensating controls:
- Refresh-token rotation: every successful renewal removes the old server-side
  token and persists a replacement.
- Refresh-token reuse rejection: the old token is no longer accepted after
  rotation because renewal requires an exact user + refresh-token database match.
- Logout invalidation: API logout removes the user's stored refresh tokens, so
  the logged-out refresh token cannot be renewed.
- Server-side token state: a valid JWT alone is not enough to renew; the matching
  refresh token must still exist in the database for the same user.
- Access-token lifetime: access tokens are time-limited and can be made
  short-lived with `Jwt:AccessTokenMinutes` / `JWT__AccessTokenMinutes` when
  configured for the target environment.

Future migration:
- Move refresh tokens from JavaScript-readable storage to `HttpOnly`, `Secure`,
  `SameSite` refresh cookies.
- That migration should be handled as a separate security hardening phase because
  it changes client/API behavior and must include cookie issuance/clearing,
  credentialed CORS review, CSRF protection decisions, regression tests, and
  updated deployment documentation.

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
- Refresh-token rotation and reuse rejection limit replay after a legitimate renewal.
- Logout deletes all refresh tokens for the user.
- Access tokens are time-limited and can be shortened per environment.
- The refresh token remains XSS-sensitive while stored in `sessionStorage`; future
  hardening should move it to an `HttpOnly`, `Secure`, `SameSite` cookie with an
  explicit CSRF strategy.

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
