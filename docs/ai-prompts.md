# AI Prompt Log

Record AI-assisted development evidence here.

## Entry Template

- Date:
- Subject:
- Assignment:
- Prompt:
- Files affected:
- AI output used:
- What AI got wrong / needed correction:
- Changes made manually:
- Alternatives considered:

## Entries

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Fix the live Assignment 18 login issue where the deployed `sysadmin` account returned `User/Password problem`, by making seeded demo/system credentials recoverable without dropping the production database.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/src/App.DAL.EF/Seeding/AppDataInit.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/src/WebApp/Setup/AppDataInitExtensions.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/src/WebApp/appsettings.json`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/tests/WebApp.Tests/Unit/UnitTestIdentitySeed.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/testing.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/docker-deploy-study-guide.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`, `docs/ai-prompts.md`
- AI output used: Added an opt-in seed-user password resync path, enabled it by default in production compose, added a regression test, and synchronized the assignment deployment/testing documentation.
- What AI got wrong / needed correction: A simpler “just drop the database” fix would have been too destructive for a persistent production volume, so the final change preserves the data while only restoring the documented seed/demo credentials.
- Changes made manually: Traced the failure to the existing seed behavior that only created users on first startup, then documented the new override so seed-password resets stay explicit and defendable.
- Alternatives considered: Forcing `DropDatabase=true` on deploy, but resetting only the known seed/demo users is much safer and keeps tenant data intact.

- Date: 2026-03-21
- Subject: javascript
- Assignment: assignment-01-task-manager, assignment-02-ts-task-manager
- Prompt: Make the deployed JavaScript assignment READMEs comply with the repository rule by placing each app's live public URL at the beginning of the assignment README.
- Files affected: `courses/javascript/assignment-01-task-manager/README.md`, `courses/javascript/assignment-02-ts-task-manager/README.md`, `docs/ai-prompts.md`
- AI output used: Added the concrete live URLs for the deployed Assignment 01 and Assignment 02 apps to the top of their assignment READMEs.
- What AI got wrong / needed correction: The earlier deployment documentation only linked the apps to Assignment 03 and did not place the actual live URL at the start of each deployed assignment README.
- Changes made manually: Reused the already confirmed proxy URLs `mtiker-js-js.proxy.itcollege.ee` and `mtiker-js-ts.proxy.itcollege.ee` so the assignment docs stay consistent with the deployment mapping.
- Alternatives considered: Keeping the live URLs only in the deployment assignment README, but the repository rule explicitly requires them in each deployed assignment README.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Create a defense-focused study guide for Assignment 18 Docker, Docker Compose, CI/CD, and deployment files, explaining the real project setup thoroughly and line by line where helpful.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/docker-deploy-study-guide.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`
- AI output used: Added a project-specific Docker/deploy study guide covering the end-to-end deployment flow, line-by-line explanations for `Dockerfile`, Compose files, deploy script, `.dockerignore`, and the assignment CI pipeline, plus README linking.
- What AI got wrong / needed correction: The first outline risked staying too generic, so the final material was anchored tightly to the exact Assignment 18 files, environment variables, and GitLab job flow.
- Changes made manually: Reviewed the generated explanations against the current Docker, Compose, and CI files and added the live deployment URL to the assignment README for repository-rule compliance.
- Alternatives considered: Explaining Docker and deploy only inside the README, but a dedicated study guide is easier to rehearse from and keeps the assignment overview README shorter.

- Date: 2026-03-21
- Subject: javascript
- Assignment: assignment-03-ci-cd-1
- Prompt: Replace the JavaScript deployment placeholders with the real proxy hostnames and internal VPS targets for ports 81 and 82.
- Files affected: `README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/javascript/assignment-03-ci-cd-1/README.md`
- AI output used: Updated the repository and assignment deployment documentation to the concrete proxy URLs `mtiker-js-js.proxy.itcollege.ee` and `mtiker-js-ts.proxy.itcollege.ee` with VPS target `192.168.181.122` on ports `81` and `82`.
- What AI got wrong / needed correction: No major correction was needed once the real proxy mappings were provided.
- Changes made manually: Confirmed the mappings supplied by the user and applied them consistently in both the root and assignment-level deployment docs.
- Alternatives considered: Leaving placeholders in place until after a manual browser check, but the provided mappings were specific enough to document directly.

- Date: 2026-03-21
- Subject: javascript
- Assignment: assignment-03-ci-cd-1
- Prompt: Build the JavaScript CI/CD assignment so the first pure JS app and the TypeScript app deploy to the VPS as Dockerized nginx containers, with Assignment 01 on host port 81 and Assignment 02 on host port 82, and keep the monorepo documentation in sync.
- Files affected: `.gitlab-ci.yml`, `README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/javascript/assignment-01-task-manager/.dockerignore`, `courses/javascript/assignment-01-task-manager/README.md`, `courses/javascript/assignment-02-ts-task-manager/.dockerignore`, `courses/javascript/assignment-02-ts-task-manager/README.md`, `courses/javascript/assignment-03-ci-cd-1/.gitlab-ci.yml`, `courses/javascript/assignment-03-ci-cd-1/docker-compose.prod.yml`, `courses/javascript/assignment-03-ci-cd-1/dockerfiles/assignment-01.Dockerfile`, `courses/javascript/assignment-03-ci-cd-1/dockerfiles/assignment-02.Dockerfile`, `courses/javascript/assignment-03-ci-cd-1/scripts/deploy.sh`, `courses/javascript/assignment-03-ci-cd-1/README.md`
- AI output used: Added a dedicated JavaScript deployment assignment, root pipeline include, two Dockerized nginx services, runner-based CI jobs, deployment smoke checks, and synchronized deployment documentation for the repo and both source assignments.
- What AI got wrong / needed correction: The first Dockerfile draft tried to copy a shared nginx config from outside the Docker build context, which is invalid, so the nginx config was moved into the image build steps instead.
- Changes made manually: Confirmed the repository already had no reliable JavaScript proxy hostname recorded, so the docs keep explicit placeholder public URLs while documenting the verified internal port targets `81` and `82`.
- Alternatives considered: Serving both apps from one nginx container and path prefixes, but separate services on ports `81` and `82` match the planned VPS layout more directly and are easier to explain in defense.

- Date: 2026-03-21
- Subject: repository-rules
- Assignment: n/a
- Prompt: Add the repository owner's identity block to the top of the root README and extend `AGENTS.md` so deployed assignments must show their live URL at the beginning of the assignment README when reachable from a third device.
- Files affected: `README.md`, `AGENTS.md`, `docs/ai-prompts.md`
- AI output used: Updated the root README with the requested owner details and added an explicit assignment-README deploy-URL rule to the repository agent instructions.
- What AI got wrong / needed correction: The first pass normalized the surname to ASCII as `Tikerpae`, but the requested exact name `Tikerpäe` was then restored in the root README.
- Changes made manually: Verified the rule was placed in the documentation sections where future assignment README maintenance is already defined.
- Alternatives considered: Keeping the deploy URL expectation implicit in general documentation hygiene, but an explicit rule is easier to enforce consistently.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Review the Assignment 18 deployment readiness against the lecture expectations, fix concrete Docker and production configuration gaps, add a deploy smoke-check endpoint, and synchronize tests and documentation.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/.dockerignore`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/src/WebApp/Program.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/src/WebApp/Setup/MiddlewareExtensions.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/tests/WebApp.Tests/Integration/IntegrationTestDeployment.cs`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/scripts/deploy.sh`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/testing.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`
- AI output used: Identified missing production CORS wiring, missing Docker build-context filtering, and the lack of a health endpoint; added the endpoint, deployment guards, regression test, and synchronized deployment documentation.
- What AI got wrong / needed correction: The first pass could have treated the existing Docker files as “good enough,” but a deeper review showed production would still fail without `Cors:AllowedOrigins`, so the deployment docs and compose file were tightened together.
- Changes made manually: Reviewed the current CI layout, existing middleware behavior, and README claims so the fixes match the real monorepo setup and production startup path.
- Alternatives considered: Leaving health verification to ad hoc Swagger/manual browser checks, but a dedicated `/health` endpoint is easier to automate, defend, and re-check after deployment.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Set the production CORS default to the real proxy host `mtiker-cweb-a3.proxy.itcollege.ee` and align deployment documentation with that concrete URL.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/scripts/deploy.sh`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`
- AI output used: Replaced the strict required CORS env with a default proxy origin and synchronized the deploy docs to the actual production hostname.
- What AI got wrong / needed correction: The earlier hard requirement for `CORS_ALLOWED_ORIGIN` was safe but unnecessarily strict once the real proxy hostname was known, so it was relaxed to a concrete default.
- Changes made manually: Confirmed the exact proxy host provided by the user and kept the environment variable override path intact for future changes.
- Alternatives considered: Keeping the value mandatory in CI/CD variables, but a correct default reduces deployment friction while still allowing overrides.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Change the Assignment 18 GitLab pipeline to use the actual runner tag `shared` instead of the earlier planned specialized tags.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/.gitlab-ci.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`
- AI output used: Replaced all Assignment 18 job tags with `shared` and synchronized the CI/CD documentation to match the real runner setup.
- What AI got wrong / needed correction: The earlier CI design assumed separate runner tags for build, Docker, and deploy, but the actual GitLab runner available for this project uses only `shared`.
- Changes made manually: Confirmed the runner tag shown in GitLab and aligned both the assignment README and monorepo CI/CD guide with that real environment.
- Alternatives considered: Retagging the runner with multiple specialized tags, but updating the pipeline to the existing `shared` runner was the fastest and lowest-friction fix.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Update production Docker port mapping so the deployed app is exposed on host port 80, matching the likely university proxy target and avoiding proxy 502 responses.
- Files affected: `courses/webapp-csharp/assignment-18-dental-clinic-platform/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/ai-usage.md`
- AI output used: Changed the production port mapping default from host `8080` to host `80` and synchronized the deployment documentation and health-check example.
- What AI got wrong / needed correction: The previous deployment default exposed the app on host port `8080`, which was likely incompatible with the existing proxy mapping that expected port `80`.
- Changes made manually: Confirmed the container was healthy on the VPS and used that runtime evidence to narrow the issue to host/proxy port alignment rather than application startup.
- Alternatives considered: Changing the proxy target to port `8080`, but updating the host port default to `80` fit the common proxy expectation more directly.

- Date: 2026-03-21
- Subject: webapp-csharp
- Assignment: assignment-18-dental-clinic-platform
- Prompt: Implement monorepo CI/CD and Docker layout so the root GitLab file only orchestrates assignment pipelines, Assignment 18 keeps its own Docker and pipeline files, deployment expectations are documented, and runner host config stays outside the repo.
- Files affected: `.gitlab-ci.yml`, `docs/ci-cd.md`, `README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/.gitlab-ci.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/Dockerfile`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/scripts/deploy.sh`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `docs/ai-prompts.md`
- AI output used: Added a root orchestration pipeline, an Assignment 18 pipeline with `rules: changes`, a production Docker Compose file, a Dockerfile, a deploy script, and synchronized repository/assignment CI-CD documentation.
- What AI got wrong / needed correction: The plan initially did not commit to whether deployment should build on the VPS or require a registry push, so the implementation was narrowed to a simpler VPS-side compose build while keeping the Docker build validation job in CI.
- Changes made manually: Confirmed the assignment path, checked the existing local compose usage, and aligned the documentation with the current monorepo and runner-tag model.
- Alternatives considered: Keeping all GitLab CI logic in the repository root, but that would make unrelated assignments trigger the wrong pipelines and would scale poorly as more assignments get their own CI/CD setup.

- Date: 2026-03-21
- Subject: repository-rules
- Assignment: n/a
- Prompt: Extend the repository rules so that if a task seems likely to exceed comfortable context size, the work must be done one layer at a time and the user must be warned that context compaction may drop details.
- Files affected: `AGENTS.md`, `README.md`, `docs/ai-prompts.md`
- AI output used: Added explicit context-management and layer-by-layer execution rules to `AGENTS.md` and reflected the same workflow expectation in the root `README.md`.
- What AI got wrong / needed correction: The first strengthened ruleset did not yet explicitly tell the agent how to behave when context becomes tight, so that missing workflow rule was added separately.
- Changes made manually: Kept the wording aligned with the existing planning and workflow sections so the new rule reads as part of the same operating model.
- Alternatives considered: Leaving this behavior implicit under general planning rules, but that would make it too easy to forget exactly when large tasks should be split by layer.

- Date: 2026-03-21
- Subject: repository-rules
- Assignment: n/a
- Prompt: Review the official JavaScript, Web Applications with C#, and Programming in C# course materials and strengthen `AGENTS.md` so future work starts with the right architectural, testing, documentation, deployment, and defense-ready assumptions.
- Files affected: `AGENTS.md`, `README.md`, `docs/ai-prompts.md`
- AI output used: Expanded `AGENTS.md` with course-driven planning rules, subject-specific defaults, defense-readiness expectations, and stronger completion/evidence requirements; updated the root `README.md` workflow summary to reflect the same direction.
- What AI got wrong / needed correction: The initial rule expansion still needed tighter alignment with course-specific AI evidence, cumulative assignment flow, and defense-ready expectations, so those details were refined manually.
- Changes made manually: Verified the added rules against the current repository structure and kept the workflow guidance aligned with how assignments are organized in this monorepo.
- Alternatives considered: Keeping `AGENTS.md` as a shorter generic quality checklist, but that would still leave too many course-specific omissions for later debugging and rework.

- Date: 2026-02-25
- Subject: javascript
- Assignment: assignment-01-task-manager
- Prompt: Build Assignment 1 task manager with CRUD, async handling, validation, and command support.
- Files affected: `courses/javascript/assignment-01-task-manager/index.html`, `courses/javascript/assignment-01-task-manager/styles.css`, `courses/javascript/assignment-01-task-manager/src/*`, `courses/javascript/assignment-01-task-manager/README.md`
- AI output used: Project scaffolding, modular JS architecture, command deck UI, storage/validation/service layers, and README structure.
- What AI got wrong / needed correction: The initial implementation needed follow-up tuning around validation feedback and did not yet distinguish the first-run empty state from a filtered-empty result.
- Changes made manually: Requirement tuning for course flow, project naming, and the later UX correction around empty-state messaging.
- Alternatives considered: Building a simpler single-file assignment, but the modular structure was easier to defend and extend into A2.

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
- Files affected: `courses/javascript/assignment-02-ts-task-manager/index.html`, `courses/javascript/assignment-02-ts-task-manager/styles.css`, `courses/javascript/assignment-02-ts-task-manager/src/*`, `courses/javascript/assignment-02-ts-task-manager/README.md`, `courses/javascript/assignment-02-ts-task-manager/AI_REFLECTION.md`
- AI output used: Full TS project scaffold, typed architecture, generic utilities, service rules for dependencies/recurrence, statistics logic, and UI command integration.
- What AI got wrong / needed correction: The first pass still needed stricter compile-time cleanup and later hardening to prevent duplicate recurring task generation on repeated completion edits.
- Changes made manually: Validation rule tuning, assignment-specific wording, and follow-up verification work to make the strict TypeScript build pass cleanly.
- Alternatives considered: Rebuilding A2 from scratch instead of migrating A1, but incremental migration preserved the assignment progression more clearly.

- Date: 2026-03-21
- Subject: javascript
- Assignment: assignment-01-task-manager, assignment-02-ts-task-manager
- Prompt: Audit JavaScript assignments 1 and 2 against the current repo rules, fix the most important gaps, make A2 pass strict TypeScript checks, add regression tests, and synchronize the documentation.
- Files affected: `courses/javascript/assignment-01-task-manager/src/ui.js`, `courses/javascript/assignment-01-task-manager/package.json`, `courses/javascript/assignment-01-task-manager/tests/ui.test.mjs`, `courses/javascript/assignment-01-task-manager/README.md`, `courses/javascript/assignment-02-ts-task-manager/src/service.ts`, `courses/javascript/assignment-02-ts-task-manager/src/ui.ts`, `courses/javascript/assignment-02-ts-task-manager/src/utils.ts`, `courses/javascript/assignment-02-ts-task-manager/package.json`, `courses/javascript/assignment-02-ts-task-manager/tests/service.test.mjs`, `courses/javascript/assignment-02-ts-task-manager/README.md`, `courses/javascript/assignment-02-ts-task-manager/AI_REFLECTION.md`, `docs/ai-prompts.md`
- AI output used: Code review findings, strict-mode fixes, duplicate-recurring guard, Node-based regression/coverage setup, and synchronized README/reflection updates.
- What AI got wrong / needed correction: The first reviewed state treated some documentation and build gaps as isolated issues, but the final fix had to connect code, tests, and evidence together so the assignments stayed defense-ready.
- Changes made manually: Verified the PowerShell/npm execution-policy workaround, checked A2 with `npm.cmd run check`, `npm.cmd run build`, and tests, and kept the AI log aligned with the stronger repo template.
- Alternatives considered: Adding an external test framework such as Vitest, but the built-in Node test runner kept the solution dependency-light while still providing regression coverage and a coverage report.

- Date: 2026-03-21
- Subject: javascript
- Assignment: assignment-03-ci-cd-1, assignment-01-task-manager, assignment-02-ts-task-manager
- Prompt: Fix the GitLab pipeline after shell-runner jobs started failing with `node: bad option: --test-isolation=none` and later checkout permission errors caused by container-written `node_modules`.
- Files affected: `.gitlab-ci.yml`, `.gitignore`, `courses/javascript/assignment-01-task-manager/package.json`, `courses/javascript/assignment-02-ts-task-manager/package.json`, `courses/javascript/assignment-03-ci-cd-1/.gitlab-ci.yml`, `courses/javascript/assignment-03-ci-cd-1/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`
- AI output used: Removed the unsupported Node test-runner flag from both JavaScript assignments, changed the Assignment 03 CI jobs to use read-only mounts and an in-container temp workspace, added a pipeline-specific clone path, and synchronized the CI/CD documentation.
- What AI got wrong / needed correction: The first instinct was to focus on the C# deployment `502`, but the actual blocker was earlier JavaScript pipeline failures that prevented the deploy stage from running at all.
- Changes made manually: Correlated the failing GitLab logs with the exact package scripts and shell-runner checkout behavior, then kept the fix compatible with the existing `node:20-alpine` image instead of switching the project to a different Node baseline.
- Alternatives considered: Upgrading the CI image to a newer Node version that supports `--test-isolation=none`, but removing the fragile flag and preventing workspace pollution was the more stable shell-runner fix.

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
