# Training Module Contracts

**Status:** Phase 20 category workflow ownership update on top of the Phase 19
mediator contract baseline for categories, sessions, bookings, and attendance.

## Public API Compatibility

The HTTP contract remains unchanged:

| Route | Request | Response | Auth |
|---|---|---|---|
| `GET /api/v1/{gymCode}/training-categories` | none | `IReadOnlyCollection<TrainingCategoryResponse>` | tenant role |
| `POST /api/v1/{gymCode}/training-categories` | `TrainingCategoryUpsertRequest` | `TrainingCategoryResponse` | owner/admin |
| `PUT /api/v1/{gymCode}/training-categories/{id}` | `TrainingCategoryUpsertRequest` | `TrainingCategoryResponse` | owner/admin |
| `DELETE /api/v1/{gymCode}/training-categories/{id}` | none | `204 No Content` | owner/admin |
| `GET /api/v1/{gymCode}/training-sessions` | none | `IReadOnlyCollection<TrainingSessionResponse>` | tenant role |
| `GET /api/v1/{gymCode}/training-sessions/{id}` | none | `TrainingSessionResponse` | tenant role |
| `POST /api/v1/{gymCode}/training-sessions` | `TrainingSessionUpsertRequest` | `TrainingSessionResponse` | owner/admin |
| `PUT /api/v1/{gymCode}/training-sessions/{id}` | `TrainingSessionUpsertRequest` | `TrainingSessionResponse` | owner/admin |
| `DELETE /api/v1/{gymCode}/training-sessions/{id}` | none | `204 No Content` | owner/admin |
| `GET /api/v1/{gymCode}/bookings` | none | `IReadOnlyCollection<BookingResponse>` | owner/admin/member/trainer |
| `POST /api/v1/{gymCode}/bookings` | `BookingCreateRequest` | `BookingResponse` | owner/admin/member |
| `PUT /api/v1/{gymCode}/bookings/{id}/attendance` | `AttendanceUpdateRequest` | `BookingResponse` | owner/admin/trainer |
| `DELETE /api/v1/{gymCode}/bookings/{id}` | none | `204 No Content` | owner/admin/member |

## Mediator Contract Namespace

Training messages live in:

`Modules.Training.Contracts`

The WebApp host references this namespace as an endpoint adapter. Other
modules must not reference `Modules.Training.Application`.

Training category CRUD handlers are implemented inside
`Modules.Training.Application` and must not delegate to
`ITrainingWorkflowService`. They still use shared BLL persistence,
authorization, mapper, and exception contracts so the route/DTO/security
contract remains unchanged while the Training module owns the workflow.

## Published Messages

| Message | Response | Purpose |
|---|---|---|
| `ListTrainingCategoriesQuery(string GymCode)` | `IReadOnlyCollection<TrainingCategoryResponse>` | List tenant training categories. |
| `CreateTrainingCategoryCommand(string GymCode, TrainingCategoryUpsertRequest Request)` | `TrainingCategoryResponse` | Create a category using active request culture for `LangStr`. |
| `UpdateTrainingCategoryCommand(string GymCode, Guid CategoryId, TrainingCategoryUpsertRequest Request)` | `TrainingCategoryResponse` | Update category localized fields. |
| `DeleteTrainingCategoryCommand(string GymCode, Guid CategoryId)` | none | Delete a tenant-scoped category. |
| `ListTrainingSessionsQuery(string GymCode)` | `IReadOnlyCollection<TrainingSessionResponse>` | List tenant sessions with trainer contract IDs. |
| `GetTrainingSessionQuery(string GymCode, Guid SessionId)` | `TrainingSessionResponse` | Load one tenant session detail. |
| `CreateTrainingSessionCommand(string GymCode, TrainingSessionUpsertRequest Request)` | `TrainingSessionResponse` | Create a session and trainer assignment shifts. |
| `UpdateTrainingSessionCommand(string GymCode, Guid SessionId, TrainingSessionUpsertRequest Request)` | `TrainingSessionResponse` | Update session fields and trainer assignments. |
| `DeleteTrainingSessionCommand(string GymCode, Guid SessionId)` | none | Delete a tenant-scoped session. |
| `ListBookingsQuery(string GymCode)` | `IReadOnlyCollection<BookingResponse>` | List bookings scoped by caller role. |
| `CreateBookingCommand(string GymCode, BookingCreateRequest Request)` | `BookingResponse` | Book a member into a published session. |
| `UpdateBookingAttendanceCommand(string GymCode, Guid BookingId, AttendanceUpdateRequest Request)` | `BookingResponse` | Mark attendance/no-show/cancelled. |
| `CancelBookingCommand(string GymCode, Guid BookingId)` | none | Cancel an existing booking. |

## Non-Goals

- No finance, package, invoice, payment, or pricing contract migration.
- No maintenance, equipment, opening-hours, or work-shift CRUD migration.
- No DTO route or version changes.
