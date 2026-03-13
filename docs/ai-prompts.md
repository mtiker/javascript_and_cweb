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
