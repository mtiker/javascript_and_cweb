# Booking Rules Audit

## Purpose

This audit documents the business rules enforced when a booking is created,
its attendance is updated, or it is cancelled. The rules are owned by
`TrainingWorkflowService` (Phase 12 Final1 shape), with cross-cutting
authorization delegated to `IAuthorizationService` (and ultimately
`ResourceAuthorizationChecker`).

The rules are unchanged by Phase 12 - only their persistence path changed.
Verified on 2026-04-30 against the repository-backed
`TrainingWorkflowService` and the focused Phase 12 service/API contract tests.

## Inventory

| # | Rule | Location | Verification |
|---|---|---|---|
| 1 | Caller must have `Owner`/`Admin`/`Member` access to the tenant. | `TrainingWorkflowService.CreateBookingAsync` calls `IAuthorizationService.EnsureTenantAccessAsync`. | `TenantIsolationAndIdorTests.*`, slice service tests. |
| 2 | The training session must exist within the active gym. | `ITrainingSessionRepository.FindAsync(gymId, request.TrainingSessionId)` — `null` ⇒ `NotFoundException`. | `TrainingWorkflowServiceTests.CreateBookingAsync_*`. |
| 3 | The member must exist within the active gym. | `IMemberRepository.FindWithPersonAsync(gymId, request.MemberId)` — `null` ⇒ `NotFoundException`. | Service tests, manual API. |
| 4 | A `Member` actor may only book sessions for themselves. | `IAuthorizationService.EnsureMemberSelfAccessAsync(gymId, member.Id)`. | Existing `TenantIsolationAndIdorTests`, service tests. |
| 5 | A member cannot have two active bookings for the same session. | `IBookingRepository.ExistsForMemberSessionAsync` ⇒ `ValidationAppException`. | `TrainingWorkflowServiceTests.CreateBookingAsync_RejectsDuplicateBookingForMember`. |
| 6 | Bookings can only be created for `Published` sessions. | Status check in `CreateBookingAsync` ⇒ `ValidationAppException`. | Service tests. |
| 7 | A booking cannot exceed the session's `Capacity`. | `IBookingRepository.CountActiveForSessionAsync` ≥ capacity ⇒ `ValidationAppException`. | Service tests, manual API. |
| 8 | If `GymSettings.AllowNonMemberBookings == false`, the booking must apply a member discount (charged price < base price). | Branch in `CreateBookingAsync`. | Existing membership integration tests. |
| 9 | When `chargedPrice > 0` a `PaymentReference` is required. | Inline check ⇒ `ValidationAppException`. | Existing membership integration tests. |
| 10 | A booking is recorded with `BookedAtUtc = UtcNow` and `Status = Booked`. | `Booking { Status = BookingStatus.Booked, ... }` and the entity default. | Visible in DTO regression tests. |
| 11 | If payment is required, a `Payment` row with `Completed` status is created against the booking. | `IRepository<Payment, Guid>.AddAsync`. | Existing membership integration tests, service tests. |
| 12 | Update attendance requires `Owner`/`Admin`/`Trainer` tenant access **and** the trainer must be assigned to the session (see `trainer-authorization-audit.md`). | `EnsureTenantAccessAsync` + `EnsureTrainingAttendanceAccessAsync`. | `TrainingWorkflowServiceTests.UpdateAttendanceAsync_*`. |
| 13 | Cancelling a booking requires `Owner`/`Admin`/`Member` tenant access and `EnsureBookingAccessAsync` permission. | `IAuthorizationService.EnsureBookingAccessAsync`. | `TrainingWorkflowServiceTests.CancelBookingAsync_StampsCancelledTimestamp`. |
| 14 | Cancellation stamps `CancelledAtUtc = UtcNow` and sets `Status = Cancelled`. | Inline mutation in `CancelBookingAsync` and `UpdateAttendanceAsync`. | `TrainingWorkflowServiceTests.CancelBookingAsync_StampsCancelledTimestamp`. |

## Persistence Path Summary

| Concern | Persistence call |
|---|---|
| Find session | `unitOfWork.TrainingSessions.FindAsync(gymId, sessionId)` |
| Find member | `unitOfWork.Members.FindWithPersonAsync(gymId, memberId)` |
| Duplicate booking guard | `unitOfWork.Bookings.ExistsForMemberSessionAsync(gymId, memberId, sessionId)` |
| Capacity guard | `unitOfWork.Bookings.CountActiveForSessionAsync(sessionId)` |
| Gym settings | `unitOfWork.Repository<GymSettings>().ListAsync(predicate)` then first |
| Add booking | `unitOfWork.Bookings.AddAsync(booking)` |
| Add payment | `unitOfWork.Repository<Payment>().AddAsync(payment)` |
| Find booking for attendance | `unitOfWork.Bookings.FindWithTrainingSessionAndMemberAsync(gymId, bookingId)` |
| Find booking for cancellation | `unitOfWork.Bookings.FindAsync(gymId, bookingId)` |
| Save | `unitOfWork.SaveChangesAsync()` |

## Out Of Scope

- Booking notifications and reminder workflows.
- Refund handling on cancellation (Phase 13+).
- Group bookings / bulk creation.
- Client-side validation; this audit is server-only.
