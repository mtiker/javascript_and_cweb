# Auth Flow Audit

## Flow

```
POST /api/v1/account/login
  → IdentityService.LoginAsync
  → BuildJwtResponseAsync
  → TokenService.CreateJwt (HMAC-SHA256, configurable lifetime, default 60 min)
  → TokenService.CreateRefreshToken (64-byte random, 30-day expiry)
  → JwtResponse { jwt, refreshToken, expiresInSeconds, activeGymId/Code/Role, systemRoles, availableTenants }

POST /api/v1/account/renew-refresh-token  (public – no JWT required)
  → TokenService.GetPrincipalFromExpiredToken (validates signature, skips lifetime)
  → Lookup AppRefreshToken by userId + refreshToken value
  → Check Expiration > UtcNow
  → Remove old token, create replacement (new value, PreviousRefreshToken preserved for 30 days)
  → Rebuild JWT with same active gym/role context
  → Return new JwtResponse

POST /api/v1/account/logout  (requires JWT)
  → Delete all AppRefreshToken rows for the authenticated user

POST /api/v1/account/switch-gym  (requires JWT)
  → Validate user has an active AppUserGymRole for requested gymCode
  → SystemAdmin may impersonate any active gym as GymOwner
  → Reissue JWT with new active gym/role context

POST /api/v1/account/switch-role  (requires JWT, requires active gym in claims)
  → Validate user has the requested role in the active gym
  → SystemAdmin may take GymOwner or GymAdmin role in any active gym
  → Reissue JWT with new active role
```

## Client flow (`ApiClient`)

- All authenticated requests include `Authorization: Bearer {jwt}`.
- On a 401 response, `refreshSession()` is called once.
- Concurrent 401s share a single inflight refresh promise (`refreshInFlight`).
- On a successful refresh, the original request is retried once with the new JWT.
- If the refresh endpoint returns a non-2xx, the session is cleared and
  `"Session expired. Please sign in again."` is thrown.
- Session is stored in `sessionStorage` (cleared on tab close).

## Test coverage

| Requirement | Test location |
|---|---|
| Login returns JWT, refresh token, expiry, gym/role context | `AuthSecurityAndErrorTests.Login_ReturnsJwt_RefreshToken_Expiry_AndUserContext` |
| Refresh rotates token | `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse` |
| Reusing the old refresh token fails (403) | `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse` |
| Expired refresh token fails (403) | `AuthSecurityAndErrorTests.RenewRefreshToken_RejectsExpiredRefreshToken` |
| Invalid JWT fails (400) | `AuthSecurityAndErrorTests.RenewRefreshToken_RejectsInvalidJwt` |
| Logout invalidates refresh token | `AuthSecurityAndErrorTests.Logout_InvalidatesRefreshToken` |
| Client retries once after 401 | `ApiClient – retries once after refreshing the session on 401` |
| Client clears state if refresh fails | `ApiClient – clears the session when refresh fails` |
| Client deduplicates concurrent 401 refreshes | `ApiClient – deduplicates concurrent 401s into a single refresh request` |

## Fix applied

`IdentityService.RenewRefreshTokenAsync` previously let `SecurityTokenException`
(thrown by `TokenService.GetPrincipalFromExpiredToken` on bad signature or wrong algorithm)
propagate as an unhandled exception, which mapped to HTTP 500.

Wrapped in a try/catch that throws `ValidationAppException` instead → HTTP 400.

## Known constraints

- The refresh endpoint is `[AllowAnonymous]`. An attacker who possesses a valid
  JWT + matching refresh token can renew without any additional credential. This is
  standard for refresh-token schemes; the 30-day expiry and per-logout invalidation
  limit the attack window.
- Token rotation keeps the `PreviousRefreshToken` value for 30 days inside the same
  `AppRefreshToken` row. The previous token is not usable for renewal (the row was
  deleted and replaced), so replay attacks with the old value return 403.
