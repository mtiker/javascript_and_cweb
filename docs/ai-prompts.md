# AI Prompt Log

Record AI-assisted development evidence here.

## Entry Template

- Date:
- Subject:
- Assignment:
- Prompt:
- AI output used:
- Changes made manually:

## Entries

- Date: 2026-02-25
- Subject: javascript
- Assignment: assignment-01-task-manager
- Prompt: Build Assignment 1 task manager with CRUD, async handling, validation, and command support.
- AI output used: Project scaffolding, modular JS architecture, command deck UI, storage/validation/service layers, README update.
- Changes made manually: Requirement tuning for course flow and project naming.

- Date: 2026-02-25
- Subject: repository-rules
- Assignment: n/a
- Prompt: Add persistent AGENTS rules for subject-specific course links and stronger focus on visual quality and security.
- AI output used: AGENTS.md rule extension and source-priority policy.
- Changes made manually: Confirmed subject names and final link set.

- Date: 2026-02-25
- Subject: javascript
- Assignment: assignment-02-ts-task-manager
- Prompt: Migrate A1 to strict TypeScript and add recurring tasks, dependencies, statistics, search, sorting, and category-priority rules.
- AI output used: Full TS project scaffold, typed architecture, generic utilities, and UI command integration.
- Changes made manually: Validation rule tuning and assignment-specific wording.

- Date: 2026-03-04
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Build a multi-tenant dental clinic SaaS skeleton in .NET/C# with Identity, tenant isolation, onboarding, treatment-plan workflow, tests, and full documentation in phased workflow.
- AI output used: Solution scaffolding, domain/DAL/BLL/API code skeleton, tenant middleware and filters, auth endpoints, test suite (unit/integration), and assignment documentation set.
- Changes made manually: Fixed integration test DB provider override behavior and aligned endpoint/docs details with generated code.

- Date: 2026-03-04
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Continue after phase completion and add concrete tenant operations (patient CRUD + appointment scheduling with overlap validation), with tests and updated docs.
- AI output used: Added BLL services, DTOs, tenant controllers, 2 additional tests, and documentation updates.
- Changes made manually: Verified endpoint flow against integration test setup and adjusted documentation scope statements.

- Date: 2026-03-04
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Continue and implement SystemAdmin impersonation flow with reason + audit trail + integration test.
- AI output used: Added impersonation BLL context service, system controller endpoint, JWT impersonation claims, audit log write, and `IntegrationTestImpersonation`.
- Changes made manually: Ensured BLL/Web layer boundaries stayed clean by moving token emission and audit persistence to Web controller.

- Date: 2026-03-05
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Fix local startup so DB connection errors are handled and provide one-command local setup.
- AI output used: Added `docker-compose.yml`, local PowerShell scripts (`start-db`, `start-app`, `migrate-db`, `stop-db`), configuration flags for sensitive logging, and DB init fallback when no migrations exist.
- Changes made manually: Verified startup flow and refined README commands to match actual scripts and defaults.

- Date: 2026-03-05
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Switch to production-ready DB approach: add real EF migrations, remove EnsureCreated fallback, keep Docker/Postgres as standard local runtime.
- AI output used: Added `AppDbContextDesignTimeFactory`, generated `InitialCreate` migration + snapshot, removed `EnsureCreated` fallback, improved Docker startup script behavior.
- Changes made manually: Validated build/tests and checked startup dependency status (Docker engine readiness).

- Date: 2026-03-05
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Build a presentable web interface similar to exam project style, not only Swagger.
- AI output used: Added frontend files under `src/WebApp/wwwroot` and enabled `UseDefaultFiles()` so `/` opens the UI; implemented onboarding/login/switch-company/patient list-create-delete flows.
- Changes made manually: Verified HTTP root (`/`) serves UI and Swagger still works.

- Date: 2026-03-12
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Add broad seed/demo data across modules so functionality can be tested immediately.
- AI output used: Extended identity seed users, added demo companies and role links, and seeded sample records for patients, tooth records, treatment types, plans/items, appointments, treatments, xrays, insurance, estimates, invoices, and payment plans.
- Changes made manually: Validated build and updated README with demo login/slug information.

- Date: 2026-03-12
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Replace hash-tab navigation with role-based `/app/...` routes, keep refresh-safe routing, and document full role purpose/access matrix in README.
- AI output used: Added path-based client routing and browser history support, reserved `/app` from tenant middleware resolution, configured server fallback for `/app/*`, and expanded README role/view documentation.
- Changes made manually: Verified role landing routes and aligned role descriptions with controller authorization scope.

- Date: 2026-03-12
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Harden onboarding security by removing anonymous tenant registration and requiring system-role authorization; update tests and docs accordingly.
- AI output used: Added authorization on onboarding POST, updated integration tests to authenticate a system admin before onboarding, and aligned UI/docs text with new access rule.
- Changes made manually: Verified endpoint auth behavior and adjusted walkthrough/docs wording for system-role-first onboarding flow.

- Date: 2026-03-17
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Create a very detailed project-specific study material for `App.DAL.EF`, explaining the purpose of the DAL/EF layer, each file in the folder, where it is used, and how the important functions behave.
- AI output used: Added a dedicated `App.DAL.EF` study material document, linked it from the assignment README, and summarized tenant filtering, soft delete, audit logging, migrations, and seed data flow.
- Changes made manually: Reviewed the generated explanation against the actual project structure and kept the wording aligned with the repository documentation style.

- Date: 2026-03-17
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Create detailed study material for `App.BLL`, explaining the BLL layer generally and documenting every file in the folder, including purpose, behavior, usage locations, and how functions work.
- AI output used: Added `docs/app-bll-study-guide.md` and updated the assignment README docs map.
- Changes made manually: Reviewed the material against the current BLL, controller, middleware, and tenant-flow code so the explanations match the current project state.

- Date: 2026-03-17
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Move remaining finance and treatment-plan CRUD business logic out of controllers into the BLL service layer and align the BLL study guide with the refactor.
- AI output used: Added BLL contracts and services for treatment plans, finance workspace, cost estimates, invoices, and payment plans; refactored tenant controllers to use those services; updated the App.BLL study guide wording.
- Changes made manually: Verified solution build and test pass after the refactor and checked that API response shapes remained compatible with the existing frontend.

- Date: 2026-03-17
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Extend service-layer test coverage around finance and treatment-plan flows after the BLL refactor.
- AI output used: Added targeted unit tests for treatment plan create/update/open-items flows and for finance services covering cost estimates, invoices, payment plans, and finance workspace aggregation.
- Changes made manually: Ran the full solution test suite with a fresh build to confirm the new tests compile and pass against the current service implementations.

- Date: 2026-03-19
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Create a project-specific `App.Domain` study guide in the same style as the existing `App.BLL` and `App.DAL.EF` guides.
- AI output used: Added `docs/app-domain-guide.md` covering the domain layer overview, common abstractions, identity models, enums/helpers, and all domain entities with usage context.
- Changes made manually: Reviewed the guide against the current `App.Domain` folder contents and kept the scope limited to documentation plus AI usage logging.

- Date: 2026-03-20
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Create a project-specific `App.DTO` study guide in the same style as the existing `App.Domain`, `App.DAL.EF`, and `App.BLL` guides.
- AI output used: Added `docs/app-dto-guide.md`, linked it from the assignment README, and documented the DTO layer purpose, versioned folder structure, validation patterns, controller mapping flow, and representative DTO groups.
- Changes made manually: Cross-checked the guide against the current `App.DTO` folders, request/response classes, and controller usage so the explanation matches the real project structure.
