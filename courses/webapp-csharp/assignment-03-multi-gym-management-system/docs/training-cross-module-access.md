# Training Cross-Module Access

**Status:** Phase 19 adapter boundary documented.

## Current Access Pattern

Training module handlers do not reference Users or GymManagement internals.
They delegate to `ITrainingWorkflowService`, which currently uses shared BLL
services for tenant authorization, current actor resolution, member/staff
lookup, subscription limits, and booking pricing.

This keeps Phase 19 small and preserves verified behavior while moving the
public HTTP adapter into the Training module.

## Required Boundaries

- `Modules.Training` may reference `BuildingBlocks`, `App.DTO`, `App.Domain`,
  and transitional BLL interfaces.
- `Modules.Training` must not reference:
  - `Modules.Users.Application`
  - `Modules.Users.Infrastructure`
  - `Modules.Users.Domain`
  - `Modules.GymManagement.Application`
  - `Modules.GymManagement.Infrastructure`
  - `Modules.GymManagement.Domain`
- WebApp may reference `Modules.Training.Contracts` because it is the endpoint
  adapter and composition root.
- Other modules must call Training through public mediator messages, not
  internal handlers or application classes.

## Cross-Module Data Used By Training

| Need | Current source | Future mediator contract candidate |
|---|---|---|
| Current tenant and role access | `IAuthorizationService` / claims | Users auth-context query |
| Current member profile for member-scoped bookings | `IAuthorizationService.GetCurrentMemberAsync` | GymManagement current-member query |
| Current trainer staff profile for roster/attendance | `IAuthorizationService.GetCurrentStaffAsync` | GymManagement current-staff query |
| Trainer assignment validation | `EnsureTrainingAttendanceAccessAsync` + work shifts | GymManagement/Training assignment query |
| Booking price | `IMembershipWorkflowService.CalculateBookingPriceAsync` | MembershipFinance booking-price query |
| Gym booking settings | Unit of Work repository for `GymSettings` | GymManagement gym-settings query |

## Design Decision

The deeper domain implementation still lives in `App.BLL` during Phase 19.
Moving it all at once would require simultaneous Users, GymManagement, and
MembershipFinance contract migrations and would risk changing booking and
authorization behavior. The chosen adapter migration gives a working module
surface first, then leaves implementation relocation for later phases where
each cross-module contract can be introduced deliberately.

## Test Evidence

- `ModuleArchitectureTests.EveryModule_DoesNotReferenceAnyOtherModule`
- `ModuleArchitectureTests.TrainingModule_DoesNotReferenceUsersOrGymManagementInternals`
- `TrainingModuleMediatorTests`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_RejectsTrainerForUnassignedSession`
- `TrainingWorkflowServiceTests.CreateBookingAsync_RejectsDuplicateBookingForMember`

## Non-Goals

- No finance or membership-price migration in this phase.
- No maintenance or equipment access migration in this phase.
- No public DTO or route changes.
