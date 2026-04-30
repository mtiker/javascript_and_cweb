# Platform Role Audit

## Scope

This audit covers system-level role access for platform routes and the
separation from tenant-level gym roles.

## Platform Roles

| Role | Platform route access | Tenant route access |
|---|---|---|
| `SystemAdmin` | Allowed for platform analytics, gyms, billing/support/admin operations according to controller attributes | Can intentionally select an active tenant context for support/demo flows |
| `SystemSupport` | Allowed for read/support-oriented platform endpoints | Not allowed to use tenant endpoints without an active tenant role |
| `SystemBilling` | Allowed for billing/platform summary endpoints | Not allowed to use tenant endpoints without an active tenant role |
| Tenant-only roles | Rejected from system platform endpoints | Allowed only within active gym and allowed role list |

## Verified Routes

| Route | Expected access | Verification |
|---|---|---|
| `GET /api/v1/system/platform/analytics` | `SystemAdmin`, `SystemSupport`, `SystemBilling` | `SystemPlatformAnalytics_AllowsPlatformRoles` |
| `GET /api/v1/system/platform/analytics` | reject tenant-only member | `SystemPlatformAnalytics_RejectsTenantOnlyUser` |
| tenant member/training endpoints | reject system-only support/billing users without tenant role | existing tenant isolation tests |
| `/Admin/Gyms` | render for system admin MVC session | existing smoke/compliance tests |

## Boundary Findings

- API platform controllers use JWT bearer authentication and explicit role
  attributes.
- Tenant API controllers still require active gym context and allowed tenant
  roles through `AuthorizationService`.
- System support and billing identities are intentionally not treated as tenant
  admins unless a tenant role/context exists.

## Remaining Risk

- Platform service internals still use `IAppDbContext`. This slice preserved the
  existing service boundary and focused the Final1 repository migration on
  maintenance/facilities as requested.

