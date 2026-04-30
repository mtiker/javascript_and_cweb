# Assignment 3 — Compliance Audit

**Project:** Multi-Gym Management System (SaaS)
**Audited:** 2026-04-27
**Branch:** main

---

## How to read this document

Each row maps one assignment requirement to its implementation evidence.
Statuses:

| Status | Meaning |
|--------|---------|
| **Pass** | Fully implemented and verifiable |
| **Partial** | Implemented but with gaps or caveats noted |
| **Missing** | Not implemented |

---

## R1 — N-tier project structure

**Requirement:** Solution must follow the N-tier pattern: Domain → DAL.EF → BLL → DTO → WebApp.

| Field | Value |
|-------|-------|
| **Files** | `src/App.Domain/`, `src/App.DAL.EF/`, `src/App.BLL/`, `src/App.DTO/`, `src/WebApp/` |
| **Routes** | n/a |
| **Tests** | `Unit/RuntimeConfigurationTests.cs` — verifies startup succeeds |
| **Evidence** | `dotnet build multi-gym-management-system.slnx` |
| **Status** | **Pass** |
| **Risk** | None |

---

## R2 — Domain entities with multi-tenancy

**Requirement:** Tenant-owned entities carry a `GymId` foreign key. A `TenantBaseEntity` or equivalent composing `Id`, `GymId`, and audit fields is expected.

| Field | Value |
|-------|-------|
| **Files** | `src/App.Domain/Entities/` (33 entities), `src/App.DAL.EF/Tenant/IGymContext.cs` |
| **Routes** | n/a |
| **Tests** | `Unit/AppDbContextBehaviorTests.cs` — validates query filter behaviour |
| **Evidence** | Inspect any entity: `Member.GymId`, `TrainingSession.GymId`, etc. |
| **Status** | **Pass** |
| **Risk** | `TenantBaseEntity` is not a single base class — `GymId` is set per entity in EF config. Functionally equivalent but the inheritance hierarchy is flat. |

---

## R3 — EF Core with PostgreSQL, migrations, seeding

**Requirement:** PostgreSQL via EF Core. Migrations applied on startup. Demo seed data.

| Field | Value |
|-------|-------|
| **Files** | `src/App.DAL.EF/AppDbContext.cs`, `src/App.DAL.EF/Migrations/`, `src/App.DAL.EF/Seeding/AppDataInit.cs` |
| **Routes** | n/a |
| **Tests** | `Integration/PostgreSqlPersistenceTests.cs` (requires Docker, guarded by `RequiresDockerFact`) |
| **Evidence** | `docker compose up -d && dotnet run --project src/WebApp` — DB migrated and seeded on first start |
| **Status** | **Pass** |
| **Risk** | `PostgreSqlPersistenceTests` are skipped by default (need `RUN_POSTGRES_TESTS=1`). CI runs against SQLite in-memory. |

---

## R4 — JWT authentication with refresh token rotation

**Requirement:** Login returns JWT + refresh token. `POST /renew-refresh-token` rotates the token. Reused refresh tokens are rejected (403).

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/ApiControllers/Identity/AccountController.cs`, `src/WebApp/Setup/IdentitySetupExtensions.cs` |
| **Routes** | `POST /api/v1/account/login`, `POST /api/v1/account/renew-refresh-token` |
| **Tests** | `Integration/AuthSecurityAndErrorTests.cs` — `RenewRefreshToken_RotatesToken_AndRejectsReuse`, `RenewRefreshToken_RejectsExpiredRefreshToken` |
| **Evidence** | `curl -X POST https://localhost:7245/api/v1/account/login -d '{"email":"admin@peakforge.local","password":"GymStrong123!"}'` |
| **Status** | **Pass** |
| **Risk** | None |

---

## R5 — Role-based access control (system + tenant roles)

**Requirement:** Platform roles (SystemAdmin, SystemSupport, SystemBilling) are separate from tenant roles (GymOwner, GymAdmin, Member, Trainer, Caretaker). Endpoints enforce roles.

| Field | Value |
|-------|-------|
| **Files** | `src/App.Domain/RoleNames.cs`, `src/WebApp/ApiControllers/System/`, `src/WebApp/ApiControllerBase.cs` |
| **Routes** | All `/api/v1/system/*` endpoints — system roles only. All `/api/v1/{gymCode}/*` — tenant JWT required |
| **Tests** | `Integration/AuthSecurityAndErrorTests.cs` — `SystemPlatformAnalytics_RejectsTenantOnlyUser` |
| **Evidence** | Login as `member@peakforge.local`, attempt `GET /api/v1/system/platform/analytics` → 403 |
| **Status** | **Pass** |
| **Risk** | None |

---

## R6 — IDOR protection: gym context isolation

**Requirement:** A token scoped to gym A must be rejected when it accesses gym B's resources. Route `{gymCode}` must match the active gym in the JWT claims.

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/Middleware/GymResolutionMiddleware.cs`, `src/App.BLL/Services/AuthorizationService.cs` |
| **Routes** | All `GET/POST/PUT/DELETE /api/v1/{gymCode}/*` |
| **Tests** | `Integration/AuthSecurityAndErrorTests.cs` — `MembersEndpoint_RejectsActiveGymMismatch` (403 when gymCode ≠ active gym). `Unit/AuthorizationServiceTests.cs` — `EnsureTenantAccessAsync_RejectsWhenRouteGymDiffersFromActiveGym` |
| **Evidence** | Login as `admin@peakforge.local` (active gym = peak-forge), `GET /api/v1/north-star/members` → 403 |
| **Status** | **Pass** |
| **Risk** | Cross-gym IDOR is integration-tested at the members endpoint only. Other tenant resources (sessions, maintenance tasks, coaching plans) rely on the shared middleware and service layer but are not individually integration-tested for the cross-gym case. |

---

## R7 — IDOR protection: member self-access

**Requirement:** A member may only read their own record. Reading another member's record must return 403.

| Field | Value |
|-------|-------|
| **Files** | `src/App.BLL/Services/AuthorizationService.cs` — `EnsureMemberSelfAccessAsync` |
| **Routes** | `GET /api/v1/{gymCode}/members/{id}` |
| **Tests** | `Integration/AuthSecurityAndErrorTests.cs` — `Member_CannotReadAnotherMember`. `Unit/AuthorizationServiceTests.cs` — `EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers` |
| **Evidence** | Login as `member@peakforge.local`, `GET /api/v1/peak-forge/members/{otherMemberId}` → 403 |
| **Status** | **Pass** |
| **Risk** | None |

---

## R8 — Admin MVC area with real Razor views

**Requirement:** Admin area must use real MVC Razor views (not just API). Graders look for MVC controllers rendering views, not just redirects.

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/Areas/Admin/Controllers/DashboardController.cs`, `src/WebApp/Areas/Admin/Views/Dashboard/Index.cshtml` |
| **Routes** | `GET /Admin` → `Areas/Admin/Controllers/DashboardController.Index` |
| **Tests** | `Integration/SmokeTests.cs` — `AdminDashboard_UsesSharedLayoutAndSiteStyles` |
| **Evidence** | Login as `admin@peakforge.local` via MVC, navigate to `/Admin` — renders dashboard with gym/member/session counts |
| **Status** | **Partial** |
| **Risk** | Only `DashboardController` renders a real Razor view. `GymsController`, `MembershipsController`, `SessionsController`, `OperationsController` all do `Redirect()` to React client URLs and render no Razor content. A grader expecting full Admin CRUD in MVC will find only a summary dashboard. |

---

## R9 — Admin views use ViewBag or ViewData

**Requirement:** Admin Razor views should pass data via `ViewBag` or `ViewData` (course convention).

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/Areas/Admin/Controllers/DashboardController.cs`, `src/WebApp/Models/AdminDashboardViewModel.cs` |
| **Routes** | `GET /Admin` |
| **Tests** | `Integration/SmokeTests.cs` — `AdminDashboard_UsesSharedLayoutAndSiteStyles` |
| **Evidence** | Read `DashboardController.cs` — passes `AdminDashboardViewModel` as strongly-typed model |
| **Status** | **Missing** |
| **Risk** | `ViewBag`/`ViewData` are not used anywhere in the project. All MVC views receive strongly-typed `ViewModel` objects (which is architecturally better, but may not satisfy a specific course requirement for `ViewBag`/`ViewData` usage). If the rubric explicitly requires `ViewBag` or `ViewData`, this is a gap. |

---

## R10 — Client (non-admin) MVC area with Razor views

**Requirement:** A client-facing MVC area for members, trainers, and caretakers with real Razor views.

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/Areas/Client/Controllers/`, `src/WebApp/Areas/Client/Views/` |
| **Routes** | `GET /mvc-client`, `GET /mvc-client/Profile`, `GET /mvc-client/Sessions`, `GET /mvc-client/Sessions/Details/{id}`, `GET /mvc-client/Sessions/Roster/{id}`, `GET /mvc-client/Maintenance`, `GET /mvc-client/Maintenance/Details/{id}` |
| **Tests** | `Integration/SmokeTests.cs` — `SeededMvcPages_RenderWithSharedLayoutAndStyles` (covers member, trainer, caretaker views) |
| **Evidence** | Login as `member@peakforge.local`, navigate to `/mvc-client` — dashboard with upcoming sessions and bookings |
| **Status** | **Pass** |
| **Risk** | Client area posts (Book, CancelBooking, UpdateAttendance, UpdateStatus) use `TempData["StatusMessage"]`, not `ViewBag`/`ViewData`. Same gap as R9 if course requires those specifically. |

---

## R11 — React SPA client

**Requirement:** A React client application. Must be separately built and serve as a SPA.

| Field | Value |
|-------|-------|
| **Files** | `client/` — Vite + React 18 + TypeScript. `client/src/App.tsx` — routing. |
| **Routes (React)** | `/login`, `/platform`, `/console`, `/members`, `/sessions`, `/attendance`, `/maintenance`, `/member-workspace`, `/coaching-workspace`, `/finance-workspace`, `/training-categories`, `/membership-packages` |
| **Tests** | `client/src/pages/CrudPages.test.tsx`, `client/src/pages/WorkspacePages.test.tsx`, `client/src/pages/OperationsPages.test.tsx`, `client/src/App.test.tsx`, `client/src/pages/SessionsPage.test.tsx` (Vitest) |
| **Evidence** | `cd client && npm ci && npm test && npm run build` |
| **Status** | **Pass** |
| **Risk** | See R12 for hosting caveat. |

---

## R12 — React is separately hosted (not embedded in WebApp)

**Requirement:** React client must run on a different origin from the backend (i.e., separately hosted), so CORS and the SPA boundary are visible.

| Field | Value |
|-------|-------|
| **Files** | `Dockerfile` lines 37 (`COPY --from=client-build /client/dist ./wwwroot/client`), `src/WebApp/Setup/MiddlewareExtensions.cs` lines 46–47 |
| **Routes** | `GET /client/*` — served from `wwwroot/client/` by the backend |
| **Tests** | n/a |
| **Evidence** | Production: `docker compose -f docker-compose.prod.yml up` — React is served at `http://localhost:83/client`, same host as API. Development: `cd client && npm run dev` — runs separately on `http://localhost:5173` (different port = different origin) |
| **Status** | **Partial** |
| **Risk** | **In production the React SPA is served by the same ASP.NET Core process from `wwwroot/client`** — not a separate server. It is not "truly separately hosted" in the deployed artifact. CORS is configured and enforced, but in prod both API and React share the same origin (`http://localhost:83`). In development the Vite dev server is a separate origin (`localhost:5173`) and CORS does activate. If the assignment requires separate hosting in the graded (production) deployment, this is a gap. |

---

## R13 — React CRUD: Members

**Requirement:** React client must implement full CRUD (list, create, update, delete) for Members.

| Field | Value |
|-------|-------|
| **Files** | `client/src/pages/MembersPage.tsx` |
| **Routes** | React: `/members`. API: `GET/POST/PUT/DELETE /api/v1/{gymCode}/members` |
| **Tests** | `client/src/pages/CrudPages.test.tsx` — "creates a member and reloads the list", "shows a load error for members when the API fails" |
| **Evidence** | Login as `admin@peakforge.local` → navigate to `/client/members` |
| **Status** | **Pass** |
| **Risk** | None |

---

## R14 — React CRUD: Training Categories

**Requirement:** React client must implement full CRUD for Training Categories.

| Field | Value |
|-------|-------|
| **Files** | `client/src/pages/TrainingCategoriesPage.tsx` |
| **Routes** | React: `/training-categories`. API: `GET/POST/PUT/DELETE /api/v1/{gymCode}/training-categories` |
| **Tests** | `client/src/pages/CrudPages.test.tsx` — "updates a training category", "shows an API save error for training categories" |
| **Evidence** | Login as `admin@peakforge.local` → navigate to `/client/training-categories` |
| **Status** | **Pass** |
| **Risk** | None |

---

## R15 — React CRUD: Membership Packages

**Requirement:** React client must implement full CRUD for Membership Packages.

| Field | Value |
|-------|-------|
| **Files** | `client/src/pages/MembershipPackagesPage.tsx` |
| **Routes** | React: `/membership-packages`. API: `GET/POST/PUT/DELETE /api/v1/{gymCode}/membership-packages` |
| **Tests** | `client/src/pages/CrudPages.test.tsx` — "deletes a membership package", "shows an API save error for membership packages" |
| **Evidence** | Login as `admin@peakforge.local` → navigate to `/client/membership-packages` |
| **Status** | **Pass** |
| **Risk** | None |

---

## R16 — Swagger / OpenAPI documentation

**Requirement:** API is documented with Swagger UI accessible at `/swagger`.

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/Setup/WebApiExtensions.cs`, `src/WebApp/ConfigureSwaggerOptions.cs` |
| **Routes** | `GET /swagger`, `GET /swagger/v1/swagger.json` |
| **Tests** | n/a |
| **Evidence** | `https://localhost:7245/swagger` (dev) or `https://mtiker-cweb-4.proxy.itcollege.ee/swagger` (prod) |
| **Status** | **Pass** |
| **Risk** | None |

---

## R17 — Docker deployment (dev + prod)

**Requirement:** Project can be started with Docker Compose. Dev compose starts only the database. Prod compose starts both database and app.

| Field | Value |
|-------|-------|
| **Files** | `docker-compose.yml`, `docker-compose.prod.yml`, `Dockerfile` |
| **Routes** | Prod app: `http://localhost:83` (default), `http://localhost:83/swagger` |
| **Tests** | n/a |
| **Evidence** | Dev: `docker compose up -d`. Prod: `JWT__Key=<key> docker compose -f docker-compose.prod.yml up` |
| **Status** | **Pass** |
| **Risk** | `JWT__Key` is mandatory in prod compose (fails without it). No default or dev fallback in the prod file. `appsettings.Development.json` must include `Jwt.Key` for `dotnet run` without Docker. |

---

## R18 — CI/CD pipeline

**Requirement:** GitLab CI pipeline with build, test, and deploy stages.

| Field | Value |
|-------|-------|
| **Files** | `.gitlab-ci.yml` |
| **Routes** | n/a |
| **Tests** | Stage `test` runs `dotnet test` |
| **Evidence** | GitLab pipeline: stages `client → build → test → package → deploy` |
| **Status** | **Pass** |
| **Risk** | Deploy stage requires `./scripts/deploy.sh` and environment secrets (`JWT__Key`) to be configured in GitLab CI. |

---

## R19 — Localization (Estonian + English)

**Requirement:** UI supports at least Estonian and English. API returns localized strings based on `Accept-Language`.

| Field | Value |
|-------|-------|
| **Files** | `src/App.Resources/`, `src/App.Domain/Common/LangStr.cs` |
| **Routes** | All tenant API responses with `LangStr` fields |
| **Tests** | `Integration/AuthSecurityAndErrorTests.cs` — `TenantApi_UsesAcceptLanguageForLangStrResponses` (expects "Jõutreening" with `Accept-Language: et-EE`). `Unit/LangStrTests.cs` |
| **Evidence** | `curl -H 'Accept-Language: et-EE' .../api/v1/peak-forge/training-categories` → Estonian names |
| **Status** | **Pass** |
| **Risk** | None |

---

## R20 — Impersonation with audit trail

**Requirement:** SystemAdmin can impersonate a tenant user. Impersonation is audit-logged.

| Field | Value |
|-------|-------|
| **Files** | `src/WebApp/ApiControllers/System/ImpersonationController.cs`, `src/App.BLL/Services/IPlatformService.cs` |
| **Routes** | `POST /api/v1/system/impersonation` |
| **Tests** | `Integration/ImpersonationTests.cs` — `StartImpersonation_WritesAuditRefreshTokenAndClaims` |
| **Evidence** | Login as `systemadmin@gym.local`, POST `/api/v1/system/impersonation` with target user ID |
| **Status** | **Pass** |
| **Risk** | None |

---

## R21 — SaaS subscription tiers

**Requirement:** Gyms have subscription tiers (Starter, Growth, Enterprise). Tier limits are enforced (member count, session count, staff count).

| Field | Value |
|-------|-------|
| **Files** | `src/App.BLL/Services/ISubscriptionTierLimitService.cs` |
| **Routes** | Enforced within `POST /api/v1/{gymCode}/members`, `POST /api/v1/{gymCode}/training-sessions`, etc. |
| **Tests** | `Unit/SubscriptionTierLimitServiceTests.cs` — Starter/Growth/Enterprise limits |
| **Evidence** | Attempting to create more members than the Starter tier allows returns 403/422 |
| **Status** | **Pass** |
| **Risk** | None |

---

## Summary Table

| # | Requirement | Status |
|---|-------------|--------|
| R1 | N-tier structure | **Pass** |
| R2 | Domain entities with GymId | **Pass** |
| R3 | EF Core / migrations / seeding | **Pass** |
| R4 | JWT + refresh token rotation | **Pass** |
| R5 | Role-based access control | **Pass** |
| R6 | IDOR: gym context isolation | **Pass** |
| R7 | IDOR: member self-access | **Pass** |
| R8 | Admin MVC area — real Razor views | **Partial** — only Dashboard is real MVC; others redirect to React |
| R9 | Admin views use ViewBag/ViewData | **Missing** — all views use strongly-typed ViewModels |
| R10 | Client MVC area — Razor views | **Pass** |
| R11 | React SPA client | **Pass** |
| R12 | React is separately hosted | **Partial** — separate in dev (port 5173), embedded in prod (wwwroot/client) |
| R13 | React CRUD: Members | **Pass** |
| R14 | React CRUD: Training Categories | **Pass** |
| R15 | React CRUD: Membership Packages | **Pass** |
| R16 | Swagger / OpenAPI | **Pass** |
| R17 | Docker deployment | **Pass** |
| R18 | CI/CD pipeline | **Pass** |
| R19 | Localization | **Pass** |
| R20 | Impersonation + audit trail | **Pass** |
| R21 | SaaS subscription tiers | **Pass** |

---

## Top risks before Final1 / Final2

1. **R9 — No ViewBag/ViewData** — If the grader's rubric specifically scores for `ViewBag`/`ViewData`, this is a definite point loss. All controllers use strongly-typed models. Verify the rubric before submission.
2. **R8 — Admin is mostly redirects** — Only the Dashboard page is a real Razor view. The other admin pages (Gyms, Memberships, Sessions, Operations) are shells that redirect to React. If Admin CRUD in MVC is required, this is a gap.
3. **R12 — React not separately hosted in prod** — In the Docker production image, React is served by the backend from `wwwroot/client`. A grader expecting a separate server (e.g., a second Docker container or a CDN URL) will not see that. This may or may not be penalised depending on the rubric wording.
4. **R6 — Cross-gym IDOR integration test coverage is narrow** — Only `/members` is integration-tested for gym mismatch. The middleware handles all routes, but having more covered endpoints tested would increase confidence.
