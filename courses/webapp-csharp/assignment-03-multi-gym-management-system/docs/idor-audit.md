# IDOR Audit – Multi-Gym Management System

## Scope

This audit covers Insecure Direct Object Reference (IDOR) risks across all mutable tenant API
endpoints.  For each endpoint, it checks whether a resource belonging to gym A can be accessed
or mutated by an authenticated user whose active gym context is gym B.

---

## Findings

### ✅ Resolved – Cross-tenant booking cancel (CancelBookingAsync)

**Endpoint:** `DELETE /api/v1/{gymCode}/bookings/{id}`

**Root cause:** The booking was fetched by primary key only (`entity.Id == id`) after the gym
context check.  An admin whose JWT was scoped to gym B could supply the ID of a gym-A booking
and the fetch would succeed.  `EnsureBookingAccessAsync` then short-circuited for GymOwner /
GymAdmin roles, so the booking was cancelled without verifying it belonged to the active gym.

**Fix:** Scope the lookup to the active gym: `entity.Id == id && entity.GymId == gymId`.  When
the ID belongs to a different tenant the query returns null → 404.

**Test:** `GymAdmin_AtNorthStar_CannotCancel_PeakForgeBooking_ViaIdManipulation`

---

### ✅ Resolved – Cross-tenant attendance update (UpdateAttendanceAsync)

**Endpoint:** `PUT /api/v1/{gymCode}/bookings/{id}/attendance`

**Root cause:** Same pattern – booking fetched by ID only; admin access check short-circuited.

**Fix:** `entity.Id == bookingId && entity.GymId == gymId`

---

### ✅ Resolved – Cross-tenant maintenance task status update (UpdateTaskStatusAsync)

**Endpoint:** `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`

**Root cause:** Task fetched by primary key only; caretaker/admin access check relied on
`GetCurrentStaffAsync(task.GymId)` returning null for cross-tenant tasks, which was only
reliable if the caretaker had no staff record at the foreign gym.  An admin would bypass that
check entirely.

**Fix:** `entity.Id == taskId && entity.GymId == gymId`

**Test:** `GymAdmin_AtNorthStar_CannotUpdateStatus_PeakForgeMaintenanceTask_ViaIdManipulation`

---

## Still Present (low exploitability, out of scope for this phase)

### ⚠️ GetMemberAsync / UpdateMemberAsync / DeleteMemberAsync

Member records are fetched by primary key without a gym filter.  `EnsureMemberSelfAccessAsync`
short-circuits for GymOwner/GymAdmin.  A multi-gym admin with a JWT for gym A could read or
mutate a gym-B member by supplying their ID.

**Exploitability:** Low – requires knowing the target member's UUID and having a valid JWT for
any gym in the system.

**Mitigation path:** Add `entity.GymId == gymId` to all three member lookups.

---

## How the Core Tenant Guard Works

`TenantAccessChecker.EnsureTenantAccessAsync` is called at the start of every service method.
It verifies:

1. The user is authenticated with an active gym context (`ActiveGymId`, `ActiveGymCode`).
2. The gym identified by `gymCode` in the URL exists and is active.
3. The JWT's active gym matches the URL's gym (prevents URL-gymCode swapping).
4. The user holds at least one of the permitted roles in their JWT.

This prevents the simplest form of cross-tenant access (changing the URL `gymCode`).  The ID-
manipulation bugs described above were a second layer of defence that was missing from some
mutable endpoints.
