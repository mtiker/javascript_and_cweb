# Tenant Isolation Audit

## Architecture

Every tenant API endpoint follows this enforcement chain:

```
Controller action
  └─ BLL service method
        ├─ authorizationService.EnsureTenantAccessAsync(gymCode, roles)  ← gate 1
        │     └─ TenantAccessChecker
        │           1. Requires authenticated user with ActiveGymId + ActiveGymCode claims
        │           2. Resolves gym by URL gymCode; 404 if not found; 403 if inactive
        │           3. Asserts ActiveGymId == gym.Id && ActiveGymCode == gymCode  (403 if not)
        │           4. Asserts user holds at least one allowed role (403 if not)
        │
        ├─ DB query scoped to returned gymId  ← gate 2 (fixed in Phase 3)
        │
        └─ authorizationService.EnsureXxxAccessAsync(resource)  ← gate 3 (resource-level)
```

Gate 1 prevents the simple attack of changing the `gymCode` segment in the URL.  
Gate 2 prevents ID-manipulation attacks where a resource from another tenant is fetched by ID.  
Gate 3 enforces self-access (member vs member) and assignment checks (trainer, caretaker).

---

## Gate 1 – URL gymCode validation

`TenantAccessChecker.EnsureTenantAccessAsync` is called at the top of every BLL method that
touches tenant data.  It reads three JWT claims set at login / gym-switch time:

- `AppClaimTypes.GymId` (resolved gym GUID)
- `AppClaimTypes.GymCode` (e.g., `"peak-forge"`)
- `AppClaimTypes.ActiveRole` (e.g., `"Trainer"`)

If any of these are absent, or if they disagree with the URL `gymCode`, the request is rejected
with 403 before any database query runs.

**Test coverage:** All six `MembersEndpoint_*` tests in `AuthSecurityAndErrorTests`, plus the
new `Trainer_CannotAccess_DifferentGym_*` and `Caretaker_CannotAccess_DifferentGym_*` tests in
`TenantIsolationAndIdorTests`.

---

## Gate 2 – gymId-scoped entity queries

After the tenant check resolves the gym's `Guid`, every entity lookup that could be targeted by
ID manipulation must filter on `entity.GymId == gymId`.

### Phase 3 fixes applied

| Method                          | Before                                 | After                                           |
|---------------------------------|----------------------------------------|-------------------------------------------------|
| `UpdateAttendanceAsync`         | `entity.Id == bookingId`               | `entity.Id == bookingId && entity.GymId == gymId` |
| `CancelBookingAsync`            | `entity.Id == id`                      | `entity.Id == id && entity.GymId == gymId`      |
| `UpdateTaskStatusAsync`         | `entity.Id == taskId`                  | `entity.Id == taskId && entity.GymId == gymId`  |

### Remaining gap (low priority)

`GetMemberAsync`, `UpdateMemberAsync`, and `DeleteMemberAsync` look up members by primary key
without a `GymId` filter.  `EnsureMemberSelfAccessAsync` short-circuits for GymOwner/GymAdmin,
so a multi-gym admin could in theory read or mutate a member from a foreign tenant by ID.
The `gymCode` gate already prevents the ordinary case; this gap is only exploitable by an admin
who already has access to some gym in the system and knows a foreign member's UUID.

---

## Gate 3 – resource-level authorization

Implemented in `ResourceAuthorizationChecker` (`IResourceAuthorizationChecker`).

### Member self-access (`EnsureMemberSelfAccessAsync`)

- GymOwner / GymAdmin: allowed for any member in the active gym.
- Member: allowed only if `GetCurrentMemberAsync(gymId).Id == memberId`.
- Other roles: 403.

### Booking access (`EnsureBookingAccessAsync`)

- GymOwner / GymAdmin: allowed.
- Member: delegated to `EnsureMemberSelfAccessAsync` (own booking only).
- Trainer: allowed only if a `WorkShift` with `ShiftType.Training` links the trainer's contract
  to `booking.TrainingSessionId`.

### Training attendance (`EnsureTrainingAttendanceAccessAsync`)

- GymOwner / GymAdmin: allowed.
- Trainer: allowed only for sessions the trainer is assigned to (same WorkShift check as above).
- All other roles: 403.

### Maintenance task access (`EnsureMaintenanceTaskAccessAsync`)

- GymOwner / GymAdmin: allowed.
- Caretaker: allowed only if `task.AssignedStaffId == currentStaff.Id` (where `currentStaff` is
  resolved from `GetCurrentStaffAsync(task.GymId)`).
- All other roles: 403.

---

## System-role tenant isolation

Users with only system roles (SystemAdmin, SystemSupport, SystemBilling) have no
`AppUserGymRole` entries unless they call `/account/switch-gym`.  Without an active gym context
in their JWT, Gate 1 rejects any tenant-scoped request with 403.

SystemAdmin calling `/account/switch-gym` receives a GymOwner-scoped token for the requested
gym, granting full admin access to that tenant.  That behaviour is intentional and tested in
`ImpersonationTests`.

**Test coverage:** `SystemSupportUser_CannotAccess_TenantMembersEndpoint`,
`SystemBillingUser_CannotAccess_TenantTrainingSessionsEndpoint`,
`SystemUser_WithNoActiveGymContext_CannotAccessTenantEndpoint` in `TenantIsolationAndIdorTests`.

---

## Multi-tenant users

`multigym.admin@gym.local` has GymAdmin roles at both `peak-forge` and `north-star`.  Login
selects the first gym alphabetically ("North Star Fitness") as the active context.  The admin
must call `/account/switch-gym` to change context.

**Test coverage:** `GymAdmin_AtNorthStar_CannotCancel_PeakForgeBooking_ViaIdManipulation` and
`GymAdmin_AtNorthStar_CannotUpdateStatus_PeakForgeMaintenanceTask_ViaIdManipulation` prove that
even a legitimate admin cannot cross tenant boundaries by supplying a foreign resource ID.
