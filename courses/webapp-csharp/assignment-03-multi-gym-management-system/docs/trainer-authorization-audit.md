# Trainer Authorization Audit

## Purpose

This audit captures the authorization rules that constrain trainer-only
operations in the training slice (Phase 12). It complements
`docs/booking-rules-audit.md` and `docs/role-access-matrix.md`.

The rules are unchanged by Phase 12 — they continue to live in
`ResourceAuthorizationChecker` and are exposed to the training service
through `IAuthorizationService`. The audit documents both the trainer-side
and admin-side branches so the slice's behavior is fully traceable.

## Surface

The training slice consults `IAuthorizationService` at three trainer-relevant
boundaries:

| Boundary | Service method | Authorization call |
|---|---|---|
| List bookings (trainer) | `TrainingWorkflowService.GetBookingsAsync` | `userContextService.GetCurrent().HasRole(Trainer)` then `IBookingRepository.ListForTrainerAsync(gymId, staffId)` (no exception, just query restriction). |
| Update booking attendance | `TrainingWorkflowService.UpdateAttendanceAsync` | `IAuthorizationService.EnsureTrainingAttendanceAccessAsync(trainingSession)` |
| Cancel booking | `TrainingWorkflowService.CancelBookingAsync` | `IAuthorizationService.EnsureBookingAccessAsync(booking)` |

## Rules

### Update attendance (`EnsureTrainingAttendanceAccessAsync`)

`ResourceAuthorizationChecker.EnsureTrainingAttendanceAccessAsync(trainingSession)`:

1. If the current actor has `Owner`/`Admin` (`HasTenantAdminPrivileges`),
   allow.
2. Otherwise the actor must hold the `Trainer` role; otherwise
   `ForbiddenException("Only assigned trainers or gym admins can update attendance.")`.
3. The trainer's `Staff` profile in the active gym must exist; otherwise
   `ForbiddenException("Trainer staff profile not found for the active gym.")`.
4. There must exist a `WorkShift` such that:
   - `GymId == trainingSession.GymId`
   - `TrainingSessionId == trainingSession.Id`
   - `Contract.StaffId == currentStaff.Id`
   - `ShiftType == ShiftType.Training`
5. If no such shift exists, `ForbiddenException("Only assigned trainers can update session attendance.")`.

This means an unassigned trainer is rejected even if they otherwise have
the `Trainer` role in the active gym. The training slice surfaces this as a
`403`/`ProblemDetails` response.

### Booking access (`EnsureBookingAccessAsync`)

`ResourceAuthorizationChecker.EnsureBookingAccessAsync(booking)`:

1. `Owner`/`Admin` always allowed.
2. `Member` actor: `EnsureMemberSelfAccessAsync(booking.GymId, booking.MemberId)`
   — must own the booking.
3. `Trainer` actor: must be assigned to `booking.TrainingSessionId` through
   a training-type `WorkShift`; otherwise `ForbiddenException`.
4. Anyone else: `ForbiddenException`.

### Bookings list filter (Trainer)

`TrainingWorkflowService.GetBookingsAsync` does **not** throw for trainers
without assignments — it returns an empty list. The repository call
`IBookingRepository.ListForTrainerAsync(gymId, staffId)` filters bookings to
those whose session has a training shift for the trainer's staff record.

This mirrors the behavior in the pre-Phase-12 implementation.

## Persistence Path Summary

| Concern | Persistence call |
|---|---|
| Trainer staff lookup | `IAuthorizationService.GetCurrentStaffAsync(gymId)` |
| Trainer-assigned session ids (list bookings) | `IBookingRepository.ListForTrainerAsync(gymId, staffId)` |
| Assigned-shift check (attendance/cancel) | EF query inside `ResourceAuthorizationChecker` over `WorkShifts` (still depends on `IAppDbContext` until Phase 13). |

## Future Hardening

- `ResourceAuthorizationChecker` still depends on `IAppDbContext`. It is
  intentionally **out of scope** for Phase 12 because the rule lives outside
  the training slice and is shared with the maintenance, member, and
  membership slices. A later phase can convert it to use
  `IWorkShiftRepository` directly so that the boundary becomes uniform.
- Consider exposing a dedicated `IAssignedTrainerCheckService` to remove the
  EF query from the authorization layer entirely.

## Verification

- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_AllowsAssignedTrainer`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_RejectsTrainerForUnassignedSession`
- `AuthorizationServiceTests.*` (existing).
- `TenantIsolationAndIdorTests.*` (existing).
