# Backend Differences: Assignment 03 vs Assignment 18

Date: 2026-04-22

This report compares the current working-tree backend implementations of:

- `assignment-03-multi-gym-management-system`
- `assignment-18-dental-clinic-platform`

The comparison is based on the repository state at the time of writing, including uncommitted local changes. Frontend-only code is mentioned only where it affects backend hosting or API shape.

## Executive Summary

Both assignments are multi-tenant ASP.NET Core SaaS backends on the same core stack: .NET 10, ASP.NET Core Identity, JWT authentication, EF Core 10, PostgreSQL, Swagger, API versioning, Docker, and xUnit integration tests.

The main difference is product focus and backend maturity:

- Assignment 03 is a gym SaaS backend with broader operational coverage: gym onboarding, members, staff, contracts, schedules, bookings, memberships, payments, facilities, equipment, maintenance, MVC admin/client areas, a React client mount, localization, and a newer service-boundary cleanup inspired by Assignment 18.
- Assignment 18 is a dental clinic SaaS backend with deeper vertical workflows: patients, tooth records, appointments, treatment plans, clinical records, insurance, cost estimates, invoices, payment plans, finance workspace, company administration, feature flags, support, billing, and system impersonation.
- Assignment 03 currently has a cleaner BLL abstraction over persistence for most new backend workflows through `IAppDbContext`, while Assignment 18 has more domain-specific services but lets BLL depend directly on `AppDbContext`.
- Assignment 18 has more advanced clinical/finance business workflows and more backend service tests. Assignment 03 has more runtime surfaces because it serves REST, MVC admin, MVC client, and the production React client from one host.

## High-Level Backend Profile

| Area | Assignment 03: Multi-Gym | Assignment 18: Dental Clinic |
| --- | --- | --- |
| Tenant root | `Gym` | `Company` |
| Tenant route | `/api/v1/{gymCode}/...` | `/api/v1/{companySlug}/...` |
| System route | `/api/v1/system/...` | `/api/v1/system/...` |
| Identity route | `/api/v1/account/...` with kebab-case actions | `/api/v1/account/{action}` mostly action-name based |
| Backend projects | `App.Domain`, `App.DAL.EF`, `App.BLL`, `App.DTO`, `App.Resources`, `WebApp` | `App.Domain`, `App.DAL.EF`, `App.BLL`, `App.DTO`, `WebApp` |
| Localization | Backend `.resx`, request localization, and `LangStr` DB localization | No separate resources project; domain text is mostly direct scalar data |
| Hosted UI surfaces | API, Swagger, MVC Admin, MVC Client under `/mvc-client`, React client under `/client` | API, Swagger, static demo UI under `/app/*` |
| API controllers | 25 controllers, 92 HTTP actions | 23 controllers, 89 HTTP actions |
| EF migrations | 1 migration: initial create | 3 migrations: initial create plus treatment-plan/finance refinements |
| Test focus | Smoke, auth/security, proposal workflows, LangStr, membership workflow | Identity, onboarding, impersonation, tenant operations, patients, appointments, treatment plans, finance services, tenant access |

## Architecture and Layering

### Shared baseline

Both backends use the same N-tier shape:

- `App.Domain` contains entities, common base types, enums, role constants, and Identity models.
- `App.DAL.EF` owns `AppDbContext`, EF mapping, migrations, tenant filtering, audit logging, soft delete, and seeding.
- `App.BLL` contains higher-risk business workflows and validation.
- `App.DTO` contains versioned public API request/response contracts.
- `WebApp` contains startup, DI, middleware, authentication, controllers, Swagger, and hosted UI assets.
- `tests/WebApp.Tests` contains xUnit tests using `Microsoft.AspNetCore.Mvc.Testing`.

### Dependency direction

Assignment 03 has moved toward a more defensive BLL boundary:

- `App.BLL` depends on `App.Domain` and `App.DTO`.
- BLL services use `IAppDbContext` from `App.BLL.Contracts.Infrastructure`.
- `App.DAL.EF` implements the BLL persistence contract by exposing `AppDbContext` as `IAppDbContext`.
- Most tenant workflow controllers delegate to BLL services.

Assignment 18 uses a practical hybrid:

- `App.BLL` depends directly on `App.DAL.EF`.
- BLL services inject `AppDbContext` directly.
- Complex workflows are in services, but simpler CRUD controllers still use `AppDbContext` directly.
- This is less strict architecturally, but very direct and easy to follow for the assignment scope.

### Startup and middleware

Assignment 03 startup is organized around:

- `AddAppDatabase`
- `AddAppIdentity`
- `AddAppServices`
- `AddAppLocalization`
- `AddAppControllers`
- `AddAppCors`
- `AddAppApiVersioning`
- `AddAppSwagger`
- `UseAppPipeline`
- `MapAppEndpoints`

Assignment 18 startup is similar but includes additional production/deployment behavior:

- forwarded headers support
- stricter connection string and JWT configuration requirements
- persisted Data Protection keys in the database
- migrations endpoint in development
- tenant resolution middleware in the main pipeline
- static demo UI fallback under `/app/{*path:nonfile}`

## Domain Model Differences

### Assignment 03 domain

Assignment 03 models gym operations and platform SaaS administration. Main persisted sets include:

- platform: `Gym`, `GymSettings`, `Subscription`, `SupportTicket`, `AuditLog`, `AppUserGymRole`
- people/contact model: `Person`, `Contact`, `PersonContact`, `GymContact`
- member/staff model: `Member`, `Staff`, `JobRole`, `EmploymentContract`, `Vacation`
- training/scheduling: `TrainingCategory`, `TrainingSession`, `WorkShift`, `Booking`
- commercial model: `MembershipPackage`, `Membership`, `Payment`
- facilities/equipment: `OpeningHours`, `OpeningHoursException`, `EquipmentModel`, `Equipment`, `MaintenanceTask`
- identity/support: `AppUser`, `AppRole`, `AppRefreshToken`, `DataProtectionKey`

The gym backend has stronger operational breadth. It tracks member and employee structure, recurring operating constraints, bookings, attendance, equipment lifecycle, and maintenance.

### Assignment 18 domain

Assignment 18 models dental clinic operations and finance. Main persisted sets include:

- platform: `Company`, `CompanySettings`, `Subscription`, `AppUserRole`, `AuditLog`
- patient/clinical model: `Patient`, `ToothRecord`, `TreatmentType`, `Treatment`, `Appointment`, `TreatmentPlan`, `PlanItem`, `Xray`
- insurance/finance model: `InsurancePlan`, `PatientInsurancePolicy`, `CostEstimate`, `Invoice`, `InvoiceLine`, `Payment`, `PaymentPlan`, `PaymentPlanInstallment`
- resources: `Dentist`, `TreatmentRoom`
- identity/support: `AppUser`, `AppRole`, `AppRefreshToken`, `DataProtectionKey`

The dental backend has deeper transactional workflows. Treatment plans, plan item decisions, performed treatments, invoices, payment plans, and patient finance workspace form a more integrated business process than any single Assignment 03 slice.

## Multi-Tenancy and Security

### Tenant resolution

Assignment 03:

- Uses `GymId` on tenant-owned entities.
- Uses route `gymCode`.
- Uses active gym claims such as `active_gym_id`, `active_gym_code`, `active_role`, and `person_id`.
- Uses `IGymContext` and `HttpGymContext`.
- BLL authorization checks ensure the route gym matches the active gym context.

Assignment 18:

- Uses `CompanyId` on tenant-owned entities.
- Uses route `companySlug`.
- Resolves tenant context centrally through `TenantResolutionMiddleware`.
- Uses `ITenantProvider` and `RequestTenantProvider`.
- JWT uses claims such as `companyId`, `companySlug`, and `companyRole`.

Assignment 18 has tenant resolution more visibly centralized in middleware. Assignment 03 currently pushes more route/claim matching into authorization and user-context services.

### EF isolation

Both backends enforce tenant isolation at `AppDbContext` level:

- query filters for `ITenantEntity`
- combined query filters for `ITenantEntity` plus `ISoftDeleteEntity`
- automatic tenant id assignment on added tenant entities
- audit logging in `SaveChangesAsync`
- soft delete conversion for deleted business entities

Assignment 03 filters on `GymId`. Assignment 18 filters on `CompanyId`.

### Roles

Assignment 03 roles:

- system: `SystemAdmin`, `SystemSupport`, `SystemBilling`
- tenant: `GymOwner`, `GymAdmin`, `Member`, `Trainer`, `Caretaker`

Assignment 18 roles:

- system: `SystemAdmin`, `SystemSupport`, `SystemBilling`
- tenant: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Assignment 03 uses more domain-specific tenant roles that map to gym behavior. Assignment 18 uses organization roles that map to clinic administration levels.

### Authentication differences

Assignment 03:

- Allows fallback JWT issuer/audience/key defaults in code.
- Password policy is lighter: required length 6, digit required, non-alphanumeric and uppercase not required.
- Uses refresh-token rotation through BLL services.
- Supports MVC cookie auth plus JWT API auth.

Assignment 18:

- Fails startup if `JWT:Key`, `JWT:Issuer`, or `JWT:Audience` is missing.
- Uses stricter password rules: length 8, digit, uppercase, lowercase, and non-alphanumeric required.
- Sets JWT clock skew to 30 seconds.
- Uses Identity UI registration in setup, although the main app is API/static UI oriented.

Assignment 18 is stricter by default for production security configuration. Assignment 03 is easier to start locally but should avoid fallback secrets in production.

## API Surface Differences

### Assignment 03 API

Assignment 03 has 25 API controllers:

- identity: `AccountController`
- system: `GymsController`, `ImpersonationController`, `PlatformController`, `SubscriptionsController`, `SupportController`
- tenant: `BookingsController`, `EmploymentContractsController`, `EquipmentController`, `EquipmentModelsController`, `GymSettingsController`, `GymUsersController`, `JobRolesController`, `MaintenanceTasksController`, `MembersController`, `MembershipPackagesController`, `MembershipsController`, `OpeningHoursController`, `OpeningHoursExceptionsController`, `PaymentsController`, `StaffController`, `TrainingCategoriesController`, `TrainingSessionsController`, `VacationsController`, `WorkShiftsController`

The API is broad and operational. Most tenant endpoints are explicit kebab-case resource routes, for example:

- `/api/v1/{gymCode}/members`
- `/api/v1/{gymCode}/training-sessions`
- `/api/v1/{gymCode}/membership-packages`
- `/api/v1/{gymCode}/maintenance-tasks/generate-due`

### Assignment 18 API

Assignment 18 has 23 API controllers:

- identity: `AccountController`
- system: `BillingController`, `ImpersonationController`, `OnboardingController`, `PlatformController`, `SupportController`
- tenant: `AppointmentsController`, `CompanySettingsController`, `CompanyUsersController`, `CostEstimatesController`, `DentistsController`, `FinanceController`, `InsurancePlansController`, `InvoicesController`, `PatientInsurancePoliciesController`, `PatientsController`, `PaymentPlansController`, `SubscriptionController`, `ToothRecordsController`, `TreatmentPlansController`, `TreatmentRoomsController`, `TreatmentTypesController`, `XraysController`

The API is deeper around patient care and finance. Several routes are controller-name based without kebab-case normalization, for example:

- `/api/v1/{companySlug}/treatmentplans`
- `/api/v1/{companySlug}/patientinsurancepolicies`
- `/api/v1/{companySlug}/finance/workspace/{patientId}`
- `/api/v1/system/onboarding/registercompany`

Assignment 03 has more readable public route names. Assignment 18 has more workflow-rich endpoints.

## Business Workflow Differences

### Assignment 03 workflow strengths

Assignment 03 focuses on gym SaaS operations:

- gym onboarding and activation
- platform analytics, support tickets, billing/subscription administration, and impersonation
- member CRUD with self-profile access
- staff, job roles, employment contracts, vacations, and shifts
- training categories and session scheduling
- session booking, duplicate prevention, capacity checks, attendance updates
- membership packages, memberships, and payments
- opening hours, opening-hour exceptions, gym settings, equipment models, equipment, generated maintenance tasks, and task status updates
- role-specific protections for members, trainers, and caretakers

The backend is broad enough to demonstrate a full multi-gym SaaS product. The main limitation is that some staff/contract/vacation APIs still compose direct `AppDbContext` logic in controllers, while other workflows have already moved to BLL services.

### Assignment 18 workflow strengths

Assignment 18 focuses on dental clinic SaaS operations:

- company onboarding with owner membership
- company activation, feature flags, support snapshots, support tickets, billing operations
- SystemAdmin impersonation with audit trail
- patients and patient profile aggregation
- tooth record management
- dentist and treatment-room resources
- appointment scheduling with dentist/room conflict checks
- appointment clinical record workflow
- treatment plans with plan items, submission, open items, and patient decisions
- treatment type catalog
- insurance plans and patient insurance policies
- cost estimates with legal preview
- invoices, invoice generation from procedures, payments, payment plans
- finance workspace by patient
- company users, settings, and tenant subscription management

The backend is stronger for complex business transactions, especially where clinical and finance data interact.

## Persistence and Data Model Differences

Assignment 03 persistence characteristics:

- one large initial migration generated on 2026-04-09
- `GymId` tenant key
- one public entity per file in the current working tree
- `LangStr` conversion for multilingual domain-owned text
- `App.Resources` for UI localization
- many unique indexes around gym-scoped business identifiers, such as member code, staff code, booking uniqueness, equipment asset tag, and opening-hour exceptions
- audit logs store gym-related changes

Assignment 18 persistence characteristics:

- initial migration plus two subsequent migrations for treatment-plan and finance changes
- `CompanyId` tenant key
- company settings and subscription root platform data
- query splitting configured for Npgsql queries
- warnings configured to throw on multiple collection include issues
- database-persisted Data Protection keys
- audit logs store company-related changes

Assignment 03 has a larger operational schema. Assignment 18 has more evolution history and stronger EF diagnostics/configuration.

## Service Layer Differences

Assignment 03 registered services:

- `UserContextService`
- `AuthorizationService`
- `TokenService`
- `IdentityService`
- `PlatformService`
- `MemberWorkflowService`
- `MembershipWorkflowService`
- `TrainingWorkflowService`
- `MaintenanceWorkflowService`

These are coarse-grained workflow services. They group related domain actions by business area. This keeps the service list small, but individual services can become large.

Assignment 18 registered services:

- `CompanyOnboardingService`
- `CompanySettingsService`
- `CompanyUserService`
- `SubscriptionPolicyService`
- `TenantAccessService`
- `TreatmentPlanService`
- `FinanceWorkspaceService`
- `CostEstimateService`
- `InvoiceService`
- `PaymentPlanService`
- `ImpersonationService`
- `PatientService`
- `AppointmentService`
- `JwtTokenService`
- `FeatureFlagStore`

These are narrower and more use-case oriented. This gives better test targeting for complex workflows but creates more service classes and direct persistence dependencies.

## Controller Boundary Differences

Assignment 03:

- Most new tenant API controllers delegate to BLL workflow services.
- `EmploymentContractsController`, `JobRolesController`, `StaffController`, and `VacationsController` still use direct `AppDbContext`.
- MVC area controllers exist for Admin and Client pages.
- `ApiControllerBase` can expose direct DbContext access for the remaining direct-controller slices.

Assignment 18:

- Complex controllers use services: patients, appointments, treatment plans, finance workspace, cost estimates, invoices, payment plans, company settings/users, onboarding, impersonation.
- Several simpler CRUD controllers use direct `AppDbContext`: dentists, insurance plans, patient insurance policies, tooth records, treatment rooms, treatment types, subscription, and xrays.
- There are no MVC area controllers; the browser UI is static assets under `wwwroot`.

Assignment 03 is moving toward service-first controllers. Assignment 18 intentionally keeps a hybrid controller/service style.

## Error Handling and API Consistency

Assignment 03:

- Uses `ProblemDetailsMiddleware`.
- Handles API/JSON requests as `application/problem+json`.
- Leaves MVC/HTML requests to the normal error handler in production.
- `ValidationAppException` can carry multiple errors.
- Uses localized MVC error views and request localization.

Assignment 18:

- Uses `GlobalExceptionMiddleware`.
- Adds `TenantResolutionMiddleware`.
- Higher-level service and middleware errors generally return `ProblemDetails`.
- Some controller paths still return simpler `Message` payloads.

Assignment 03 has a clearer split between API and MVC error behavior. Assignment 18 has more consistent central tenant middleware but still has mixed error payload shapes in some controllers.

## Testing Differences

Assignment 03 backend test files:

- `AuthSecurityAndErrorTests`
- `ProposalWorkflowTests`
- `SmokeTests`
- `LangStrTests`
- `MembershipWorkflowServiceTests`
- `CustomWebApplicationFactory`

Main coverage themes:

- app smoke checks
- auth/security/error behavior
- gym proposal/onboarding workflows
- multilingual string behavior
- membership workflow behavior
- MVC route/layout smoke coverage in recent changes

Assignment 18 backend test files:

- integration: deployment, identity, impersonation, onboarding, tenant operations
- unit: appointment service, finance services, identity seed, patient service, tenant access service, tenant API DB controllers, tenant API service controllers, treatment plan service
- test helpers for DB context, tenant provider, and controller setup

Main coverage themes:

- identity and seed correctness
- tenant isolation and access
- onboarding and impersonation
- patient, appointment, treatment-plan, and finance service rules
- controller behavior for direct-DB and service-backed APIs

Assignment 18 has a broader backend test matrix for business services. Assignment 03 has useful tests but should add more unit tests around training, maintenance, authorization, and remaining direct-controller workflows.

## Deployment and Runtime Differences

Assignment 03:

- Public URL in README: `https://mtiker-cweb-4.proxy.itcollege.ee`
- Backend serves MVC and React production assets.
- React app is built separately and copied under `WebApp/wwwroot/client`.
- Production React route is `/client`.
- MVC client area is moved to `/mvc-client` to avoid route collision with React.
- CORS policy is named `ClientApp`.

Assignment 18:

- Public URL in README: `https://mtiker-cweb-a3.proxy.itcollege.ee`
- Static demo UI is served directly from `WebApp/wwwroot`.
- Browser routes live under `/app/*`.
- Uses forwarded headers and stricter production CORS behavior.
- CORS policy is named `CorsAllowAll`, but outside development it requires configured origins.

Assignment 03 has more hosted surfaces and therefore more route-collision risk. Assignment 18 has a simpler backend hosting story.

## Documentation Differences

Assignment 03 docs:

- assignment README
- architecture
- API
- data model
- deployment
- testing
- SaaS plan
- AI usage

Assignment 18 docs:

- assignment README
- architecture
- API
- data model
- testing
- AI usage
- detailed study guides for Domain, DAL/EF, BLL, DTO, and Docker deployment

Assignment 18 has stronger study/explanation documentation. Assignment 03 has stronger active assignment-planning documentation through `a3-saas-plan.md`.

## Main Risks and Improvement Opportunities

### Assignment 03

- Remove production fallback secrets for JWT configuration and require deployment secrets.
- Persist Data Protection keys if cookie/MVC sessions must survive container restarts across deployments.
- Finish moving staff, contracts, job roles, and vacations from direct controller persistence into BLL services.
- Add service tests for training, maintenance, authorization, and tenant self-only rules.
- Add more explicit tests for cross-gym access attempts and role-specific forbidden paths.
- Keep route documentation aligned because the backend hosts MVC and React surfaces together.

### Assignment 18

- Introduce a persistence abstraction if the course defense expects stricter clean architecture boundaries.
- Normalize route naming if public API consistency matters; current action/controller naming yields routes like `forgotpassword` and `treatmentplans`.
- Continue reducing direct `AppDbContext` usage in simple CRUD controllers if consistency becomes more important than speed.
- Standardize error payloads so all API failures consistently return `ProblemDetails`.
- Add localization only if required by the assignment or demo expectations.

## Bottom Line

Assignment 03 is now shaped as a broad, defense-ready gym SaaS backend with multiple runtime surfaces and an increasingly clean service boundary. Its strongest backend areas are tenant-aware operational breadth, MVC/API/React coexistence, gym-specific role behavior, and documentation aligned to current assignment delivery.

Assignment 18 is a more mature reference for complex vertical business workflows. Its strongest backend areas are clinical and finance workflow depth, tenant middleware, production-oriented configuration, EF diagnostics, and targeted service tests.

For future Assignment 03 backend work, the most valuable lessons to carry over from Assignment 18 are stricter production configuration, more granular workflow tests, stronger EF diagnostics, and continued movement of controller logic into services. The most valuable Assignment 03 improvement over Assignment 18 is the `IAppDbContext` BLL boundary, which makes the architecture easier to defend as layered and course-aligned.
