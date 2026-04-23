# Study Guide: Auth and Tenant Flow

## Flow Summary
1. User authenticates via `/api/v1/account/login` and receives JWT + refresh token.
2. JWT carries active gym and role context claims.
3. Tenant API requests include route `gymCode` and bearer token.
4. `GymResolutionMiddleware` validates route gym early (exists + active) and stores resolution in request context.
5. BLL authorization (`AuthorizationService`) validates:
   - active gym claim alignment
   - role requirements
   - self-access (member)
   - assignment access (trainer/caretaker)
6. EF query filters and `GymId` checks enforce tenant data boundaries.
7. Audit entries and soft-delete behavior preserve operational traceability.

## Role Switching
- `switch-gym` updates active tenant context for multi-gym users.
- `switch-role` updates active tenant role when allowed for current user/tenant assignment.
- `availableTenants` in auth payload supports shell-side tenant/role selectors.

## Impersonation
System roles can start impersonation with explicit reason metadata.
- actor and target are recorded
- impersonation claims are issued
- refresh token and audit row are created

## Security Controls
- strict password policy
- HTTPS metadata requirement outside development
- fail-fast production CORS validation
- forwarded-header processing before auth for reverse-proxy correctness

## Defense Notes
Middleware provides early rejection and cleaner request flow evidence.
BLL remains the final source of truth for authorization decisions.
