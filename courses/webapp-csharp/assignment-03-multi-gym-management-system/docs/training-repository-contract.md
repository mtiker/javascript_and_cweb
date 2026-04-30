# Training Repository Contracts

## Purpose

The training slice exposes four persistence contracts on the BLL boundary.
They keep training, work-shift, and booking lookups, uniqueness/capacity
checks, and lifecycle operations out of the controllers and out of concrete
EF code.

| Contract | EF implementation | Unit of Work property |
|---|---|---|
| `ITrainingCategoryRepository` | `EfTrainingCategoryRepository` | `IAppUnitOfWork.TrainingCategories` |
| `ITrainingSessionRepository` | `EfTrainingSessionRepository` | `IAppUnitOfWork.TrainingSessions` |
| `IBookingRepository` | `EfBookingRepository` | `IAppUnitOfWork.Bookings` |
| `IWorkShiftRepository` | `EfWorkShiftRepository` | `IAppUnitOfWork.WorkShifts` |

Contract location: `src/App.BLL/Contracts/Persistence/`.

EF implementation location: `src/App.DAL.EF/Repositories/`.

`SaveChangesAsync` is intentionally **not** on any of these repositories.
Transaction completion belongs to `IAppUnitOfWork`.

## ITrainingCategoryRepository

| Method | Responsibility |
|---|---|
| `ListByGymAsync(Guid gymId, CancellationToken)` | Tenant-scoped list ordered by `ValidFrom` (ascending). |
| `FindAsync(Guid gymId, Guid categoryId, CancellationToken)` | Load one category by tenant + id (used by Update / Delete). |
| `AddAsync(TrainingCategory, CancellationToken)` | Stage a new category. |
| `Remove(TrainingCategory)` | Stage a delete (rewritten to soft-delete by `AppDbContext.ApplySoftDelete`). |

## ITrainingSessionRepository

| Method | Responsibility |
|---|---|
| `ListByGymAsync(Guid gymId, CancellationToken)` | Tenant-scoped list ordered by `StartAtUtc`. |
| `FindAsync(Guid gymId, Guid sessionId, CancellationToken)` | Load one session by tenant + id. |
| `AddAsync(TrainingSession, CancellationToken)` | Stage a new session. |
| `Remove(TrainingSession)` | Stage a delete. |

## IBookingRepository

| Method | Responsibility |
|---|---|
| `ListByGymAsync(Guid gymId, CancellationToken)` | Tenant-scoped list with `TrainingSession`, `Member`, and `Member.Person` included; ordered by `BookedAtUtc` descending. |
| `ListForMemberAsync(Guid gymId, Guid memberId, CancellationToken)` | Same shape, restricted to a single member. |
| `ListForTrainerAsync(Guid gymId, Guid staffId, CancellationToken)` | Bookings whose `TrainingSession` has a training-type `WorkShift` for the given staff member. |
| `FindAsync(Guid gymId, Guid bookingId, CancellationToken)` | Load one booking by tenant + id (no includes). |
| `FindWithTrainingSessionAndMemberAsync(Guid gymId, Guid bookingId, CancellationToken)` | Load one booking with `TrainingSession`, `Member`, and `Member.Person` included (used by attendance update). |
| `ExistsForMemberSessionAsync(Guid gymId, Guid memberId, Guid sessionId, CancellationToken)` | Duplicate booking guard. |
| `CountActiveForSessionAsync(Guid sessionId, CancellationToken)` | Capacity check - counts bookings with `Status == Booked`. |
| `AddAsync(Booking, CancellationToken)` | Stage a new booking. |

## IWorkShiftRepository

| Method | Responsibility |
|---|---|
| `ListByGymAsync(Guid gymId, CancellationToken)` | Tenant-scoped list ordered by `StartAtUtc`. |
| `ListForStaffAsync(Guid gymId, Guid staffId, CancellationToken)` | Filter the tenant-scoped list to the contracts that belong to the given staff. |
| `ListTrainingShiftsForSessionAsync(Guid gymId, Guid sessionId, CancellationToken)` | Trainer (`Training`-type) shifts attached to a session - returned as full entities so the service can clear and re-create them on session upsert. |
| `ListTrainerContractIdsForSessionAsync(Guid gymId, Guid sessionId, CancellationToken)` | Convenience that returns only the contract IDs assigned to a session - used to build `TrainerContractIds` on response. |
| `FindAsync(Guid gymId, Guid shiftId, CancellationToken)` | Load one shift by tenant + id. |
| `AddAsync(WorkShift, CancellationToken)` | Stage a new shift. |
| `RemoveRange(IEnumerable<WorkShift>)` | Stage multiple shift deletes (used when re-syncing a session's trainer assignments). |
| `Remove(WorkShift)` | Stage a single shift delete. |

## Tenant Isolation

Every single-row read MUST filter by the `gymId` parameter as well as by the
entity id. Cross-tenant ID manipulation MUST return `null`. This invariant is
required because integration tests disable EF global query filters when they
audit boundary behavior.

The training slice intentionally has no id-only repository read. Session,
category, booking, and work-shift lookup methods all require `gymId` to keep
tenant isolation explicit even when EF global query filters are bypassed in
tests.

## Soft Delete Semantics

All training-domain entities inherit `TenantBaseEntity`. `Remove(...)` on
each repository is rewritten by `AppDbContext.ApplySoftDelete()` into:

- `IsDeleted = true`
- `DeletedAtUtc = DateTime.UtcNow`

`ListByGymAsync`, `FindAsync`, and friends MUST NOT return soft-deleted
rows. The default EF query filter handles this; the repository relies on it
and does not call `IgnoreQueryFilters`.

## Query Constraints

The repositories must not:

- return rows for another `gymId`
- expose `IQueryable` to callers
- save changes internally
- accept controller or DTO types
- depend on `WebApp`

The EF implementations may use `Include`, `OrderBy`, and other EF query
operators because they live in `App.DAL.EF`.

## Verification

Covered by:

- `TrainingWorkflowServiceTests.GetCategoriesAsync_ReturnsCategoriesOrderedByValidFrom`
- `TrainingWorkflowServiceTests.GetCategoriesAsync_TranslatesLangStrToActiveCulture`
- `TrainingWorkflowServiceTests.CreateCategoryAsync_PersistsThroughUnitOfWork`
- `TrainingWorkflowServiceTests.UpdateCategoryAsync_TenantScopedNotFound`
- `TrainingWorkflowServiceTests.DeleteCategoryAsync_RemovesCategory`
- `TrainingWorkflowServiceTests.CreateBookingAsync_PersistsBookingForPublishedSession`
- `TrainingWorkflowServiceTests.CreateBookingAsync_RejectsDuplicateBookingForMember`
- `TrainingWorkflowServiceTests.CancelBookingAsync_StampsCancelledTimestamp`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_AllowsAssignedTrainer`
- `TrainingWorkflowServiceTests.UpdateAttendanceAsync_RejectsTrainerForUnassignedSession`
- `TrainingCategoryLocalizationTests.*` (existing API regression).
- `ArchitectureTests.TrainingSlice_UsesDedicatedRepositoryAndMapperBoundaries`
- `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence`
