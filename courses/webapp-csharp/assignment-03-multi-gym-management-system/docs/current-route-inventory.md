# Current Route Inventory

**Audited:** 2026-04-28

---

## 1. MVC Routes (Razor — cookie auth)

### Home Controller — `src/WebApp/Controllers/HomeController.cs`

| Method | Path | Action | Notes |
|--------|------|--------|-------|
| GET | `/` | `Index` | Login page (redirects to `/Admin` or `/mvc-client` if authenticated) |
| POST | `/login` | `Login` | MVC cookie sign-in |
| POST | `/logout` | `Logout` | Cookie sign-out |
| GET | `/workspace` | `RedirectToWorkspace` | Redirects based on active role |
| POST | `/switch-gym` | `SwitchGym` | Changes active gym context (MVC session) |
| POST | `/switch-role` | `SwitchRole` | Changes active role (MVC session) |
| GET | `/access-denied` | `AccessDenied` | Access denied page |
| POST | `/set-culture` | `SetCulture` | Sets culture cookie (returns 302) |
| GET | `/Home/Error` | `Error` | Error page (HTML) |

### Admin Area — `src/WebApp/Areas/Admin/Controllers/`

Route pattern: `{area:exists}/{controller=Dashboard}/{action=Index}`

| Method | Path | Controller | Action | Returns | ViewBag/ViewData |
|--------|------|------------|--------|---------|-----------------|
| GET | `/Admin` | `DashboardController` | `Index` | Razor view (`AdminDashboardViewModel`) | None — strongly-typed model |
| GET | `/Admin/Gyms` | `GymsController` | `Index` | Razor view (`AdminGymsPageViewModel`) | None - strongly-typed model |
| GET | `/Admin/Members` | `MembersController` | `Index` | Razor view (`AdminMembersPageViewModel`) | None - strongly-typed model |
| GET | `/Admin/Memberships` | `MembershipsController` | `Index` | Razor view (`AdminMembershipsPageViewModel`) | None - strongly-typed model |
| GET | `/Admin/Sessions` | `SessionsController` | `Index` | Razor view (`AdminSessionsPageViewModel`) | None - strongly-typed model |
| GET | `/Admin/Operations` | `OperationsController` | `Index` | Razor view (`AdminOperationsPageViewModel`) | None - strongly-typed model |

**Note:** Admin resource controllers now render read-only MVC evidence pages. Mutation-heavy workflows remain in the React client and REST API.

**Razor views under `Areas/Admin/Views/`:**
- `Dashboard/Index.cshtml` — metrics + quick-link strip (renders via `AdminDashboardViewModel`)
- `Gyms/Index.cshtml` - platform gym list
- `Members/Index.cshtml` - tenant member directory
- `Memberships/Index.cshtml` - packages and active memberships
- `Sessions/Index.cshtml` - session schedule summary
- `Operations/Index.cshtml` - opening hours, equipment, and maintenance summary

### Client Area — `src/WebApp/Areas/Client/Controllers/`

Route pattern: `mvc-client/{controller=Dashboard}/{action=Index}/{id?}`

| Method | Path | Controller | Action | Returns | Notes |
|--------|------|------------|--------|---------|-------|
| GET | `/mvc-client` | `DashboardController` | `Index` | Razor view (`ClientDashboardViewModel`) | Upcoming sessions, recent bookings, assigned tasks |
| GET | `/mvc-client/Profile` | `ProfileController` | `Index` | Razor view (`ClientProfilePageViewModel`) | Member profile |
| GET | `/mvc-client/Sessions` | `SessionsController` | `Index` | Razor view (`SessionsPageViewModel`) | Session list |
| GET | `/mvc-client/Sessions/Details/{id}` | `SessionsController` | `Details` | Razor view (`SessionDetailPageViewModel`) | Session + booking form |
| POST | `/mvc-client/Sessions/Details/{id}` (Book) | `SessionsController` | `Book` | Redirect to `Details` | `TempData["StatusMessage"]` |
| POST | `/mvc-client/Sessions/CancelBooking/{id}` | `SessionsController` | `CancelBooking` | Redirect to `Details` | `TempData["StatusMessage"]` |
| GET | `/mvc-client/Sessions/Roster/{id}` | `SessionsController` | `Roster` | Razor view (`TrainerRosterPageViewModel`) | Trainer attendance |
| POST | `/mvc-client/Sessions/UpdateAttendance` | `SessionsController` | `UpdateAttendance` | Redirect to `Roster` | `TempData["StatusMessage"]` |
| GET | `/mvc-client/Maintenance` | `MaintenanceController` | `Index` | Razor view (`MaintenancePageViewModel`) | Assigned tasks list |
| GET | `/mvc-client/Maintenance/Details/{id}` | `MaintenanceController` | `Details` | Razor view (`MaintenanceTaskDetailPageViewModel`) | Task detail + status form |
| POST | `/mvc-client/Maintenance/UpdateStatus/{id}` | `MaintenanceController` | `UpdateStatus` | Redirect to `Details` | `TempData["StatusMessage"]` |

**Razor views under `Areas/Client/Views/`:**
- `Dashboard/Index.cshtml`
- `Profile/Index.cshtml`
- `Sessions/Index.cshtml`, `Details.cshtml`, `Roster.cshtml`
- `Maintenance/Index.cshtml`, `Details.cshtml`

**Shared views:**
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_CultureSwitcher.cshtml`
- `Views/Shared/Components/WorkspaceSwitcher/Default.cshtml`
- `Views/Home/Index.cshtml`, `AccessDenied.cshtml`, `Error.cshtml`

---

## 2. React SPA Routes (`client/src/App.tsx`)

Served at: `/client/*` (prod) or `http://localhost:5173/*` (dev)

| React Path | Component | Accessible To | Auth Required |
|-----------|-----------|--------------|--------------|
| `/login` | `LoginPage` | All | No |
| `/` | `RoleLandingRedirect` | All (redirects) | Yes |
| `/platform` | `SaasConsolePage` | SystemAdmin, SystemSupport, SystemBilling | Yes |
| `/console` | `SaasConsolePage` | GymOwner, GymAdmin | Yes |
| `/members` | `MembersPage` | GymAdmin, GymOwner | Yes |
| `/sessions` | `SessionsPage` | GymAdmin, GymOwner, Trainer | Yes |
| `/attendance` | `AttendancePage` | Trainer | Yes |
| `/maintenance` | `MaintenanceTasksPage` | GymAdmin, GymOwner, Caretaker | Yes |
| `/member-workspace` | `MemberWorkspacePage` | Member | Yes |
| `/coaching-workspace` | `TrainerCoachingWorkspacePage` | Trainer | Yes |
| `/finance-workspace` | `FinanceWorkspacePage` | GymAdmin, GymOwner | Yes |
| `/training-categories` | `TrainingCategoriesPage` | GymAdmin, GymOwner | Yes |
| `/membership-packages` | `MembershipPackagesPage` | GymAdmin, GymOwner | Yes |
| `*` | `Navigate to /` | — | — |

Role landing defaults: SystemAdmin/Support/Billing → `/platform`, Trainer → `/coaching-workspace`, Caretaker → `/maintenance`, Member → `/member-workspace`, GymAdmin/Owner → `/members`.

---

## 3. REST API Routes (JWT Bearer auth)

### Identity — `src/WebApp/ApiControllers/Identity/AccountController.cs`

Base: `/api/v1/account`

| Method | Path | Auth | Roles |
|--------|------|------|-------|
| POST | `/register` | None | — |
| POST | `/login` | None | — |
| POST | `/logout` | JWT | Any |
| POST | `/renew-refresh-token` | None | — |
| POST | `/switch-gym` | JWT | Any |
| POST | `/switch-role` | JWT | Any |
| POST | `/forgot-password` | None | — |
| POST | `/reset-password` | None | — |

### System — `src/WebApp/ApiControllers/System/`

Base: `/api/v1/system`

| Method | Path | Roles |
|--------|------|-------|
| GET | `/gyms` | SystemAdmin, SystemSupport, SystemBilling |
| POST | `/gyms` | SystemAdmin |
| PUT | `/gyms/{gymId}/activation` | SystemAdmin |
| GET | `/gyms/{gymId}/snapshot` | SystemAdmin, SystemSupport |
| GET | `/subscriptions` | SystemAdmin, SystemSupport, SystemBilling |
| PUT | `/subscriptions/{gymId}` | SystemBilling |
| GET | `/support` | SystemAdmin, SystemSupport, SystemBilling |
| POST | `/support/{gymId}/tickets` | SystemAdmin, SystemSupport, SystemBilling |
| GET | `/platform/analytics` | SystemAdmin, SystemSupport, SystemBilling |
| POST | `/impersonation` | SystemAdmin |

### Tenant — `src/WebApp/ApiControllers/Tenant/`

Base: `/api/v1/{gymCode}/...` — all require JWT with active gym = `{gymCode}`

| Resource | GET list | GET one | POST | PUT | DELETE |
|----------|----------|---------|------|-----|--------|
| `members` | ✓ | ✓ `/{id}`, `/me` | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `training-categories` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `membership-packages` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `training-sessions` | ✓ | ✓ `/{id}` | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `bookings` | ✓ | — | ✓ | ✓ `/{id}/attendance` | ✓ `/{id}` |
| `coaching-plans` | ✓ | ✓ `/{id}` | ✓ | ✓ `/{id}`, `/{id}/status`, `/{id}/items/{itemId}/decision` | ✓ `/{id}` |
| `maintenance-tasks` | ✓ | — | ✓ | ✓ `/{id}/status`, `/{id}/assignment` | ✓ `/{id}` |
| `maintenance-tasks/generate-due` | — | — | ✓ | — | — |
| `maintenance-tasks/{id}/assignment-history` | ✓ | — | — | — | — |
| `member-workspace/me` | ✓ | — | — | — | — |
| `member-workspace/members/{memberId}` | ✓ | — | — | — | — |
| `finance-workspace/me` | ✓ | — | — | — | — |
| `finance-workspace/members/{memberId}` | ✓ | — | — | — | — |
| `invoices` | ✓ | ✓ `/{id}` | ✓ | — | — |
| `invoices/{id}/payments` | — | — | ✓ | — | — |
| `invoices/{id}/refunds` | — | — | ✓ | — | — |
| `memberships` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `staff` | ✓ | ✓ `/{id}` | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `employment-contracts` | ✓ | ✓ `/{id}` | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `vacations` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `work-shifts` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `job-roles` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `equipment` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `equipment-models` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `gym-settings` | ✓ | — | — | ✓ | — |
| `gym-users` | ✓ | — | ✓ | ✓ `/{userId}` | ✓ `/{userId}` |
| `opening-hours` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `opening-hours-exceptions` | ✓ | — | ✓ | ✓ `/{id}` | ✓ `/{id}` |
| `payments` | ✓ | — | — | — | — |

### Infrastructure endpoints

| Method | Path | Notes |
|--------|------|-------|
| GET | `/health` | Health check (no auth) |
| GET | `/swagger` | Swagger UI |
| GET | `/swagger/v1/swagger.json` | OpenAPI spec |
| GET | `/client` | React SPA index.html |
| GET | `/client/{*path}` | React SPA fallback |

---

## 4. Middleware pipeline order

```
ForwardedHeaders
↓ ExceptionHandler (prod) / DeveloperException (dev)
↓ HSTS (prod)
↓ ProblemDetailsMiddleware (API error normalisation)
↓ HttpsRedirection
↓ StaticFiles
↓ Routing
↓ CORS ("ClientApp" policy)
↓ RequestLocalization
↓ Authentication
↓ GymResolutionMiddleware (resolves + validates {gymCode} from route)
↓ Authorization
↓ Controller/endpoint handlers
```
