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

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system and assignment-18-dental-clinic-platform
- Prompt: Compare the controllers of Assignment 03 and Assignment 18 so Assignment 03 can later be made as functional and well-made as Assignment 18.
- Files affected: `courses/webapp-csharp/backend-differences-assignment-03-vs-18.md`, `README.md`, `docs/ai-prompts.md`
- AI output used: Reworked the cross-assignment comparison into a controller-focused report covering controller surfaces, routes, authorization, DTO-to-BLL mapping, response semantics, cancellation support, direct DbContext usage, controller tests, and a phased modernization plan for Assignment 03.
- What AI got wrong / needed correction: Nothing material; the comparison separates Assignment 18 patterns worth adopting from direct-DbContext controller patterns that Assignment 03 should not copy.
- Changes made manually: Inspected both projects' API controller files, representative service-backed and direct-DbContext controllers, Assignment 18 controller unit tests, Assignment 03 route smoke coverage, the root README map, and official course controller/BLL expectations before editing.
- Alternatives considered: Implementing controller changes immediately, but the request is better served first by a precise gap map so later changes can be done in safe workflow slices without breaking routes or clients.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Continue implementing the plan to align Assignment 03 backend structure with Assignment 18 without changing routes, DTO JSON shapes, EF model, migrations, seed data, auth, tenant isolation, tests, or deployment behavior.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.Domain/Entities/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DTO/v1/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Contracts/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Exceptions/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DAL.EF/Seeding/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/ApiControllers/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Program.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Middleware/ProblemDetailsMiddleware.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/SmokeTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/*.md`, `docs/ai-prompts.md`
- AI output used: Split grouped domain entity files into one public entity per file, split DTOs into Assignment 18-style resource folders and namespaces, moved BLL service interfaces beside implementations, moved infrastructure contracts under `App.BLL.Contracts.Infrastructure`, split infrastructure services and WebApp setup extensions, standardized BLL exception names, split broad tenant API controllers while preserving route templates, split seed initialization into partial files, and added route smoke coverage for the newly separated tenant controllers.
- What AI got wrong / needed correction: Mechanical namespace updates initially left duplicate `using` directives and temporarily touched EF migration files through encoding/formatting churn; duplicate usings were cleaned and migration files were restored so no migration diff remains.
- Changes made manually: Re-ran backend build/tests and EF pending-model verification, checked that split controllers kept existing route templates, and updated assignment architecture/testing documentation to describe the new organization.
- Alternatives considered: Keeping grouped files and only adding comments was rejected because it would not match the Assignment 18 structure; introducing per-entity EF configuration was deferred because it was not needed to preserve behavior and would increase model-drift risk.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: cweb assignment 3: profile is not translated to Estonian, active gym/role and admin/client workspace text is inconsistent, SystemAdmin cannot change active tenant, seed a typical gym with full data, make booking/client/maintenance functions available like a real gym system, and ensure the separate client app also has translation.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/components/AppShell.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/lib/language.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/LoginPage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/SaasConsolePage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/IdentityService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DAL.EF/Seeding/AppDataInit.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.Resources/SharedResources*.resx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Controllers/HomeController.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/Admin/Views/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/Client/Views/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/SmokeTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `docs/ai-prompts.md`
- AI output used: Added missing MVC Estonian resource keys and wired the affected Admin/Client Razor views to them, allowed SystemAdmin to enter any active gym as a transient `GymOwner` context through MVC and JWT switch flows, added a React SystemAdmin tenant picker, expanded the React translation dictionary for login/shell labels, enriched seed data for a fuller gym demo, and added an integration test for SystemAdmin tenant switching.
- What AI got wrong / needed correction: Terminal output can display UTF-8 Estonian text as mojibake even when the file content is correct, so string verification must rely on tests/browser rendering rather than PowerShell display alone.
- Changes made manually: Reviewed the official Web Applications with C# assignment framing before coding, kept the existing demo account credentials stable, and documented that detailed React workflow form copy can still be translated incrementally after the shell/login pass.
- Alternatives considered: Persisting tenant role rows for SystemAdmin in every gym, but transient switch claims better express platform-level access and scale to newly onboarded gyms.

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: For the gym-management assignment, make sure language changes are correct everywhere, make the SaaS work like the dental clinic with every function available, and add a logo to the URL image.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/index.html`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/public/gym-logo.svg`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/App.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/components/AppShell.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/lib/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/LoginPage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/SaasConsolePage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/styles.css`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DAL.EF/Seeding/AppDataInit.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.Resources/SharedResources*.resx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Controllers/HomeController.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/ServiceCollectionExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Views/Shared/_Layout.cshtml`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/wwwroot/assets/gym-logo.svg`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/*.md`, `docs/ai-prompts.md`
- AI output used: Added React language state plus API `Accept-Language`, allowed system-role sessions, added a React SaaS console for platform/support/billing/onboarding/account/tenant operations, validated MVC culture cookies, corrected seeded Estonian translations, added SVG favicon assets, and updated tests/docs.
- What AI got wrong / needed correction: No major correction; the main care point was not touching unrelated JavaScript Assignment 04 work already present in the worktree.
- Changes made manually: Checked the current official Web Applications with C# SaaS assignment requirements and compared the gym implementation to the dental clinic reference before choosing a React-console expansion.
- Alternatives considered: Porting the dental static UI directly, but expanding the existing React client preserved the Assignment 03 architecture and production `/client` route.

- Date: 2026-04-21
- Subject: javascript
- Assignment: assignment-04-vue-secure-todo
- Prompt: For the Vue app, make sure `ä`, `ö`, and `ü` display correctly, add a logo to the URL, and add seeded data for full manual testing.
- Files affected: `courses/javascript/assignment-04-vue-secure-todo/index.html`, `courses/javascript/assignment-04-vue-secure-todo/public/favicon.svg`, `courses/javascript/assignment-04-vue-secure-todo/nginx/default.conf`, `courses/javascript/assignment-04-vue-secure-todo/src/lib/demo-seed.ts`, `courses/javascript/assignment-04-vue-secure-todo/src/stores/catalogs.ts`, `courses/javascript/assignment-04-vue-secure-todo/src/stores/todo.ts`, `courses/javascript/assignment-04-vue-secure-todo/src/views/CatalogsView.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/TasksView.vue`, `courses/javascript/assignment-04-vue-secure-todo/tests/catalogs-view.spec.ts`, `courses/javascript/assignment-04-vue-secure-todo/tests/todo-mappers.spec.ts`, `courses/javascript/assignment-04-vue-secure-todo/README.md`, `docs/ai-prompts.md`
- AI output used: Added a favicon/logo reference, configured nginx UTF-8 charset delivery, added idempotent demo seed data with Estonian category/task names, wired the Catalogs view to seed catalogs and tasks through the real API, replaced typographic quote bindings with ASCII-safe computed descriptions, and added regression tests for seed creation and UTF-8 payload preservation.
- What AI got wrong / needed correction: PowerShell `Get-Content` displayed UTF-8 strings as mojibake in the terminal, so the actual source was verified with `rg` and tests before documenting the change.
- Changes made manually: Checked the current official JavaScript Assignment 04 page, reviewed the Vue stores/views/API mappers, and kept the seed flow idempotent so manual retesting does not duplicate records.
- Alternatives considered: Hard-coding local-only sample data, but seeding through the authenticated TalTech API gives a more realistic defense/manual-test path.

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Use the `cweb-a4` proxy URL for C# Assignment 3 deployment after confirming the proxy table, and use previous assignments as the deployment source of truth.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docker-compose.prod.yml`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/deployment.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/architecture.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `courses/javascript/assignment-04-vue-secure-todo/README.md`, `courses/javascript/assignment-04-vue-secure-todo/docker-compose.prod.yml`, `courses/javascript/assignment-04-vue-secure-todo/scripts/deploy.sh`, `README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`
- AI output used: Aligned C# Assignment 3 with `https://mtiker-cweb-4.proxy.itcollege.ee` on VPS port `83`, added the production CORS origin default, documented the public route and smoke checks, and moved JavaScript Assignment 04 defaults/docs to port `84` to match the `js-vue` proxy route.
- What AI got wrong / needed correction: The first deployment guidance assumed the older `cweb-a3` URL; the user corrected this to `cweb-a4`.
- Changes made manually: Reviewed the proxy-host screenshot values and kept Assignment 18 documented on the existing `cweb-a3` route.
- Alternatives considered: Replacing Assignment 18 on `cweb-a3`, but the proxy table provides a dedicated `cweb-a4` route for the new C# assignment.

- Date: 2026-04-21
- Subject: javascript
- Assignment: assignment-04-vue-secure-todo
- Prompt: Perform a browser visual audit of the Vue assignment, checking real desktop/mobile screenshots for readability, color, spacing, responsive layout, and random or overly technical UI text; fix issues found.
- Files affected: `courses/javascript/assignment-04-vue-secure-todo/src/styles.css`, `courses/javascript/assignment-04-vue-secure-todo/src/components/AuthCard.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/components/AppShell.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/components/TaskFormModal.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/components/CatalogFormModal.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/LoginView.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/RegisterView.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/DashboardView.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/TasksView.vue`, `courses/javascript/assignment-04-vue-secure-todo/src/views/CatalogsView.vue`, `courses/javascript/assignment-04-vue-secure-todo/.gitignore`, `docs/ai-prompts.md`
- AI output used: Drove Chrome against a local Vite app and mock API, captured desktop/mobile screenshots, then tightened the palette, border radii, mobile shell density, task action button sizing, user-facing copy, and validation-message timing.
- What AI got wrong / needed correction: The first screenshot driver timed out because Vite was started through `npm.cmd`; switching to Vite's direct Node entrypoint made the browser audit reliable.
- Changes made manually: Reviewed generated screenshots for login, register, dashboard, task list, catalogs, task modal, and delete confirmation; re-ran `npm test` and `npm run build` after the polish changes.
- Alternatives considered: Reporting visual issues only, but direct fixes were safer because the screenshots showed concrete mobile and form-state problems.

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Implement the A3 readiness gap plan: package the React client into production at `/client`, move proposal-critical workflows behind BLL services, add member/trainer/caretaker/opening-hours happy paths, fix nullable session descriptions, add regression tests, and synchronize documentation.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/Dockerfile`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DAL.EF/AppDbContext.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/ApiControllers/Tenant/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/Client/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/ProposalWorkflowTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/*.md`, `docs/ai-prompts.md`
- AI output used: Added the production client build/copy stage and `/client` fallback, introduced `IAppDbContext` and BLL workflow services, refactored tenant API controllers to services, expanded MVC role happy paths, added the React sessions/booking workflow, fixed null-safe session description projections, and added backend/frontend regression tests.
- What AI got wrong / needed correction: The first `/client` fallback did not catch the bare `/client` path, and putting Vitest setup in `vite.config.ts` caused production build type conflicts.
- Changes made manually: Re-ran backend build/tests, frontend tests/build, and adjusted docs to document the pragmatic remaining `AppDbContext` exceptions.
- Alternatives considered: Serving React from a second production container or rewriting data access around repositories/unit-of-work, but same-host `/client` deployment plus `IAppDbContext` gives a safer defense-ready fix.

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Fix the remaining limitations that React did not include trainer/caretaker screens and build/test output still showed `System.Security.Cryptography.Xml` NU1903 vulnerability warnings.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/App.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/components/AppShell.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/lib/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/AttendancePage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/MaintenanceTasksPage.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/OperationsPages.test.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/styles.css`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/WebApp.Tests.csproj`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/*.md`, `docs/ai-prompts.md`
- AI output used: Added React Attendance and Maintenance pages, wired trainer/caretaker roles into auth/navigation/API methods, added Vitest coverage for role updates, and pinned `System.Security.Cryptography.Xml` 10.0.6 to remove vulnerable transitive resolution in tests.
- What AI got wrong / needed correction: No suppression was used; the fix needed a real package resolution change verified by `dotnet list package --vulnerable --include-transitive`.
- Changes made manually: Checked NuGet/GitHub advisories for the patched package line, then reran frontend tests/build, .NET build/test, and package audit.
- Alternatives considered: Suppressing NU1903 or leaving trainer/caretaker flows in MVC only, but package pinning and React role screens make the assignment easier to defend.

- Date: 2026-04-21
- Subject: javascript
- Assignment: assignment-04-vue-secure-todo
- Prompt: Fix the Vue assignment readiness review findings excluding deployment: redirect after refresh-token failure during protected-route setup, show retryable startup load-error states, and clean coverage reporting.
- Files affected: `courses/javascript/assignment-04-vue-secure-todo/src/router/index.ts`, `courses/javascript/assignment-04-vue-secure-todo/src/views/*`, `courses/javascript/assignment-04-vue-secure-todo/tests/*`, `courses/javascript/assignment-04-vue-secure-todo/vite.config.ts`, `courses/javascript/assignment-04-vue-secure-todo/README.md`, `docs/ai-prompts.md`
- AI output used: Added the auth-failure router redirect, retryable load-error panels for dashboard/tasks/catalogs, regression tests for the route and view states, and coverage excludes for generated/config/test-helper files.
- What AI got wrong / needed correction: No major correction during this pass; the coverage cleanup needed one extra exclude for top-level test helpers after the first coverage rerun.
- Changes made manually: Re-ran `npm test`, `npm run build`, and `npm run coverage` to verify the fixes and checked that assignment documentation stayed aligned.
- Alternatives considered: Letting failed catalog preload pass through to the protected view, but redirecting when the session has been cleared gives a cleaner auth boundary for defense.

- Date: 2026-04-16
- Subject: javascript
- Assignment: assignment-04-vue-secure-todo
- Prompt: Implement Assignment 04 as a Vue 3 + TypeScript secure Todo frontend against the shared TalTech backend, with Vue Router, Pinia, JWT + refresh-token handling, first-run catalog onboarding, tests, Docker deployment, GitLab child pipeline, and synchronized documentation.
- Files affected: `README.md`, `.gitlab-ci.yml`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/javascript/assignment-04-vue-secure-todo/*`
- AI output used: Built the new Vue assignment app structure, API client/auth-refresh flow, Pinia stores, router guards, responsive views/components, Vitest suite, Docker/nginx/deploy files, assignment-local CI pipeline, and repo-level documentation updates.
- What AI got wrong / needed correction: The first dependency set pulled Zod 4, which conflicted with `@vee-validate/zod`; the initial giant patch also hit Windows path/tooling limits; and the first auth-view tests were too brittle and had to be refocused onto the auth store for reliable verification.
- Changes made manually: Rechecked the live TalTech backend behavior after the April 5, 2026 overhaul, validated real endpoint shapes with throwaway API calls, pinned the compatible validation dependency set, refined the form bindings to a clearer `useForm + setFieldValue` approach, and verified the assignment with `npm run check`, `npm test`, and `npm run build`.
- Alternatives considered: Keeping the app inside the older JavaScript Assignment 03 deployment folder, storing tokens in `localStorage`, or relying on server seed data for first-run setup, but a self-contained Assignment 04 app with session-scoped auth and explicit catalog onboarding is cleaner, safer, and closer to the course brief.

- Date: 2026-04-16
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Implement the Assignment 03 correction pass excluding deployment: add a separate React + TypeScript client that uses the existing REST API with JWT and refresh tokens for 3 CRUD entities, fix production HTML vs API error handling, add security-focused tests, wire client verification into CI, and synchronize the assignment documentation.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/.gitlab-ci.yml`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DTO/v1/Tenant/TenantDtos.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/ApiControllers/Tenant/MembersController.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Controllers/HomeController.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Middleware/ProblemDetailsMiddleware.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/ServiceCollectionExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/ApplicationBuilderExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Views/Home/Error.cshtml`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/appsettings.json`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.Resources/SharedResources*.resx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/architecture.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/api.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/testing.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `docs/ai-prompts.md`
- AI output used: Built the separate React client, added session persistence and refresh-token retry logic, implemented members/training-categories/membership-packages CRUD pages and tests, added backend CORS and member-detail DTO support, fixed production HTML vs API error handling, added integration tests for refresh rotation and authorization-denial cases, and synchronized the assignment CI/docs to the new delivery shape.
- What AI got wrong / needed correction: The first backend error-response fix used a JSON helper overload that was not available in this target/runtime, the HTML error test initially ran under `Development` where MVC exception handling is disabled, and the `/Home/Error` route needed to accept re-executed failed `POST` requests instead of only `GET`.
- Changes made manually: Split Vite and Vitest config to keep the frontend build clean, switched the production error test to a production-style test host, adjusted content-type handling for `ProblemDetails`, and checked the new docs against the actual controller routes and client scope.
- Alternatives considered: Treating the MVC shell as the only client, expanding the React client to the whole tenant surface immediately, or mixing deployment changes into the same pass, but the final change stayed focused on the missing A3 deliverables without reopening deployment work.

- Date: 2026-04-09
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Implement Assignment 03 as a new full SaaS multi-gym management system based on the approved gym proposal, keep Assignment 18 untouched, and finish the work through backend, MVC UX, tests, migrations, CI/deploy files, and documentation.
- Files affected: `README.md`, `.gitignore`, `.gitlab-ci.yml`, `docs/ai-prompts.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/*`
- AI output used: Built the new A3 solution structure, platform and tenant domain model, EF Core mappings and migration, BLL services, versioned API controllers, Swagger/auth setup, MVC admin/client areas, workspace switching, tests, Docker/CI files, and assignment documentation.
- What AI got wrong / needed correction: Several early files were still copied from the dental-clinic assignment, the first `LangStr` model design broke EF/InMemory tests, and the initial MVC layer was too shell-focused for the agreed SaaS scope.
- Changes made manually: Reworked `LangStr` into a clearer value object, corrected copied project names and paths, added a seeded second gym plus a multi-gym admin for switch-gym coverage, added a migration, and extended the MVC layer with additional operational pages.
- Alternatives considered: Reusing Assignment 18 directly, keeping a single-tenant gym app, or delaying migrations/docs until later, but those options would not satisfy the agreed A3 separation and SaaS scope cleanly.

- Date: 2026-04-09
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Clean up the remaining Assignment 03 deploy/CI naming leftovers and make the README match the actual multi-gym configuration.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `docs/ai-prompts.md`
- AI output used: Re-checked the assignment-local CI, compose, Docker, deploy, and project files; confirmed they already used multi-gym naming; then corrected the README connection-string example and removed the outdated warning that still claimed dental-era names were present in the active config.
- What AI got wrong / needed correction: The previous README cleanup over-reported a legacy-config problem that no longer existed in the current assignment-local deploy and CI files.
- Changes made manually: Verified `.gitlab-ci.yml`, `docker-compose.yml`, `docker-compose.prod.yml`, `scripts/deploy.sh`, `Dockerfile`, and `WebApp.csproj` before narrowing the update to stale documentation and log synchronization.
- Alternatives considered: Editing already-correct config files just to create visible diff, but leaving working infrastructure files untouched is cleaner and safer.

- Date: 2026-04-09
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Add the full Assignment 03 SaaS plan into the project itself and extend `AGENTS.md` so future changes also update that plan when the implementation evolves.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `AGENTS.md`, `docs/ai-prompts.md`
- AI output used: Added an assignment-local A3 SaaS plan document covering the intended scope, domain model, roles, APIs, security rules, phased implementation order, tests, and delivery artifacts; linked it from the assignment README; and added a repository rule that explicit assignment plans must be kept in sync with implementation changes.
- What AI got wrong / needed correction: Keeping the plan only in transient chat context would not reliably guide later sessions, and putting the whole plan only into the README would make the README harder to maintain as a day-to-day project guide.
- Changes made manually: Chose a dedicated `docs/a3-saas-plan.md` location under the assignment so the plan remains local to the project and easy to update alongside future architecture or scope changes.
- Alternatives considered: Expanding only the README or only `AGENTS.md`, but the combination of a concrete plan file plus an explicit sync rule gives better long-term maintenance.

- Date: 2026-04-09
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Fix the Assignment 03 README because the document still described a dental-clinic SaaS instead of the actual multi-gym management system.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/ai-usage.md`, `docs/ai-prompts.md`
- AI output used: Replaced the copied dental-clinic README with a gym-domain README that matches the real solution name, domain entities, seed users, route groups, project structure, and local startup flow; also added an assignment-local AI usage note.
- What AI got wrong / needed correction: A shallow terminology swap would still leave incorrect solution names, broken doc references, and misleading instructions copied from the old dental-clinic assignment.
- Changes made manually: Checked the assignment folder structure, seeded accounts, controller routes, scripts, launch settings, and current local database configuration before rewriting the documentation.
- Alternatives considered: Only fixing the title and first paragraph, but that would still leave the rest of the README inaccurate for defense or onboarding.

- Date: 2026-04-09
- Subject: repository-rules
- Assignment: n/a
- Prompt: Re-check the live JavaScript and Web Applications with C# course materials and update the `AGENTS.md` subject-specific defaults where the current syllabus or technical requirements differ.
- Files affected: `AGENTS.md`, `docs/ai-prompts.md`
- AI output used: Tightened the JavaScript defaults around the current A1-A7 expectations, added the extended-curriculum caveat for JS, clarified the React/NestJS/Angular requirements, and added matching reduced-curriculum/testing guidance for the C# subject defaults.
- What AI got wrong / needed correction: The earlier ruleset was close, but it did not clearly capture that JavaScript A6-A7 may be dropped based on class progress and it left some current React/NestJS details too generic.
- Changes made manually: Verified the wording against the current official syllabus and technical-requirements pages before narrowing the update to the subject-specific defaults instead of rewriting broader repository rules.
- Alternatives considered: Leaving the existing defaults mostly unchanged, but that would keep future sessions slightly out of sync with the live course pages and could push planning too far ahead.

- Date: 2026-03-27
- Subject: javascript
- Assignment: assignment-03-ci-cd-1
- Prompt: Fix the JavaScript Assignment 03 child pipeline again after the Assignment 02 test container started failing with `npm error path /.npm` because npm tried to use an unwritable cache directory under the mapped shell-runner UID.
- Files affected: `courses/javascript/assignment-03-ci-cd-1/.gitlab-ci.yml`, `courses/javascript/assignment-03-ci-cd-1/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`
- AI output used: Updated the Assignment 02 CI test job to set `npm_config_cache=/tmp/npm-cache` and create that cache directory alongside `/tmp/app`, then synchronized the CI docs to explain the shell-runner npm-cache permission issue.
- What AI got wrong / needed correction: The previous fix restored a writable work directory for copied source files, but npm still defaulted to `/.npm`, which is not writable for the mapped non-root UID inside the container.
- Changes made manually: Traced the new GitLab log to npm’s cache behavior, kept the existing non-root container approach, and narrowed the follow-up fix to a writable temp cache path inside the same container.
- Alternatives considered: Running npm as root or persisting a shared writable cache on the runner host, but an in-container `/tmp/npm-cache` keeps the job isolated and avoids new host-side state.

- Date: 2026-03-27
- Subject: javascript
- Assignment: assignment-03-ci-cd-1
- Prompt: Fix the JavaScript Assignment 03 child pipeline after the Assignment 02 test container started failing with `cp: can't create ... Permission denied` while copying the app into `/tmp/app` on the shell runner.
- Files affected: `courses/javascript/assignment-03-ci-cd-1/.gitlab-ci.yml`, `courses/javascript/assignment-03-ci-cd-1/README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`
- AI output used: Changed the Assignment 02 CI test job to start in `/tmp`, create `/tmp/app` as the mapped non-root user, then copy the workspace there before running `npm ci`, `npm run check`, and `npm test`; updated the CI docs to explain the shell-runner permission detail.
- What AI got wrong / needed correction: The earlier hardening correctly avoided polluting the runner checkout, but using `docker run -w /tmp/app` let Docker pre-create that directory with ownership that the mapped UID could not write to on the runner.
- Changes made manually: Matched the GitLab log to the existing container command, narrowed the fix to the JavaScript Assignment 02 test job, and documented why the temp workdir now starts at `/tmp` instead of `/tmp/app`.
- Alternatives considered: Running the container as root or writing directly into the runner checkout, but keeping the mapped host UID and an isolated temp workspace is safer for later Git checkout steps.

- Date: 2026-03-27
- Subject: repository-ci
- Assignment: monorepo child pipelines
- Prompt: Fix the isolated assignment child pipelines after GitLab rejected `assignment18_docker_build` and `javascript_assignment03_docker_build` because the `package` stage was not declared inside the child pipeline configs.
- Files affected: `README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/javascript/assignment-03-ci-cd-1/.gitlab-ci.yml`, `courses/javascript/assignment-03-ci-cd-1/README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/.gitlab-ci.yml`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/docs/docker-deploy-study-guide.md`
- AI output used: Added explicit `stages:` declarations to both assignment-local child pipeline configs and updated the repository and assignment CI documentation to explain that child pipelines do not inherit root pipeline stages.
- What AI got wrong / needed correction: The earlier child-pipeline isolation change assumed the assignment configs would keep working without an explicit stage list, but GitLab validates each child pipeline independently and falls back to default stages when none are declared.
- Changes made manually: Correlated the GitLab error with both assignment-local `.gitlab-ci.yml` files, confirmed only `package` was outside the default stage set, and documented the child-pipeline stage boundary where the repo already explains CI orchestration.
- Alternatives considered: Renaming both Docker build jobs back to the default `build` stage, but keeping a dedicated `package` stage better reflects the real pipeline flow and only requires making the child configs self-contained.

- Date: 2026-03-27
- Subject: repository-ci
- Assignment: monorepo root pipeline
- Prompt: Restructure the GitLab monorepo pipeline so each assignment runs as its own child pipeline and one assignment failure does not stop unrelated assignments from running.
- Files affected: `.gitlab-ci.yml`, `README.md`, `docs/ci-cd.md`, `docs/ai-prompts.md`, `courses/javascript/assignment-03-ci-cd-1/README.md`, `courses/webapp-csharp/assignment-18-dental-clinic-platform/README.md`
- AI output used: Replaced root-level `include` orchestration with assignment-specific child-pipeline trigger jobs, kept assignment-local CI files as the source of each assignment's internal job chain, and synchronized the repository CI documentation.
- What AI got wrong / needed correction: A single shared root stage model looks simpler at first, but it still lets one assignment's earlier failure block unrelated later-stage jobs, so the final solution moved separation to the pipeline boundary instead of only tuning stages.
- Changes made manually: Verified the existing assignment CI files were already self-contained enough to be used as child pipeline configs, then updated the docs that still described the old include-based orchestration model.
- Alternatives considered: Keeping one combined pipeline and flattening all jobs into a shared DAG, but child pipelines make assignment isolation clearer, scale better as more assignments get CI, and are easier to explain in defense.

- Date: 2026-03-27
- Subject: repository-ci
- Assignment: monorepo root pipeline
- Prompt: Fix the GitLab pipeline startup failure `setting GIT_CLONE_PATH is not allowed, enable custom_build_dir feature` on the shell runner and keep the CI documentation in sync.
- Files affected: `.gitlab-ci.yml`, `docs/ci-cd.md`, `docs/ai-prompts.md`
- AI output used: Removed the unsupported root-level `GIT_CLONE_PATH` variable and updated the CI/CD guide to document that this variable only works when the runner host enables `custom_build_dir`.
- What AI got wrong / needed correction: The earlier CI hardening assumed a runner configuration capability that is not enabled on the actual GitLab shell runner, so the pipeline failed before any job script could start.
- Changes made manually: Confirmed the error occurs during GitLab checkout initialization rather than inside assignment scripts, then narrowed the fix to the root orchestration pipeline instead of changing assignment job logic.
- Alternatives considered: Enabling `custom_build_dir` on the runner host, but that is a host-side admin change outside the repository and removing the unsupported variable is the fastest repository-level fix.

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

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Diagnose why the deployed `/admin` page rendered as a bare unstyled Razor page and fix the MVC area styling/layout regression.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/_ViewStart.cshtml`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/SmokeTests.cs`, `docs/ai-prompts.md`
- AI output used: Identified that MVC area views were missing an area-level `_ViewStart.cshtml`, added the shared layout hook, and added an integration regression test for the admin dashboard layout/CSS.
- What AI got wrong / needed correction: Nothing material; the issue was initially checked against deployment/static asset possibilities before narrowing to Razor layout discovery.
- Changes made manually: Reviewed the rendered Razor view structure and existing `site.css` before applying the convention-based fix.
- Alternatives considered: Adding explicit `Layout = "_Layout"` to each area view, but a shared area `_ViewStart.cshtml` keeps Admin and Client views consistent with less duplication.

- Date: 2026-04-21
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Make sure the other MVC views are also bugless after fixing the unstyled `/admin` page.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/MaintenanceWorkflowService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/TrainingWorkflowService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/ApplicationBuilderExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/Client/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Models/SessionsPageViewModel.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/SmokeTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/README.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/architecture.md`, `docs/ai-prompts.md`
- AI output used: Audited MVC Admin/Client routes, found the React `/client` mount colliding with the MVC Client area, added a `/mvc-client` route prefix, tightened roster authorization, widened safe schedule/opening-hours reads for tenant roles, and expanded integration smoke coverage.
- What AI got wrong / needed correction: The first broader smoke test exposed the route collision because `/Client` returned the React shell instead of the MVC page.
- Changes made manually: Reviewed the Razor views, controller role checks, and seeded role data before choosing the route and authorization fixes.
- Alternatives considered: Moving the React bundle away from `/client`, but keeping `/client` stable for deployment and moving MVC client views to `/mvc-client` preserves the documented REST client endpoint.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Fix member and booking save failures, make scheduled categories usable, show names instead of IDs in attendance, and add maintenance scheduling.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/MemberWorkflowService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/TrainingWorkflowService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/MaintenanceWorkflowService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DTO/v1/Tenant/TenantDtos.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Areas/Client/Controllers/DashboardController.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/lib/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/pages/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/ProposalWorkflowTests.cs`, assignment README and SaaS plan docs.
- AI output used: Added BLL validation before EF unique indexes for members/bookings, enriched booking and maintenance DTOs with display names, exposed training-session and maintenance-task create calls in the React API client, added scheduling forms, updated attendance rendering, and covered the flows with backend/frontend regression tests.
- What AI got wrong / needed correction: The first React scheduling test showed that loading the newly created session cleared the success notice; the detail loader was adjusted to preserve mutation feedback.
- Changes made manually: Ran backend/frontend tests and builds, reviewed response DTO projections for MVC and React consumers, and kept the documentation aligned with the new workflow scope.
- Alternatives considered: Handling duplicate member/booking errors only in the frontend, but service-layer validation gives consistent API `ProblemDetails` and avoids leaking database exceptions to any client.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system and assignment-18-dental-clinic-platform
- Prompt: Make a full report of differences in the backends of Assignment 03 and Assignment 18 in CWEB.
- Files affected: `courses/webapp-csharp/backend-differences-assignment-03-vs-18.md`, `README.md`, `docs/ai-prompts.md`
- AI output used: Inspected both local backend projects and produced a comparative Markdown report covering architecture, domain model, tenancy, auth, API surface, services, persistence, tests, deployment/runtime differences, documentation, risks, and improvement opportunities.
- What AI got wrong / needed correction: Nothing material; the report explicitly notes that it is based on the current working tree, including uncommitted Assignment 03 changes.
- Changes made manually: Reviewed existing README/docs structure and linked the new cross-assignment report from the root repository map.
- Alternatives considered: Returning only an inline chat report, but a tracked Markdown report is easier to review, update, and use for course defense preparation.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Implement the Assignment 03 backend hardening and cleanup plan, store the provided JWT key in the correct non-tracked place, and preserve routes, DTOs, seed users, deployment URL, and frontend behavior.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/IdentitySetupExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/Setup/DatabaseExtensions.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/*StaffWorkflowService.cs`, staff-related tenant API controllers, `tests/WebApp.Tests`, assignment README/deployment/architecture/testing docs, `docs/ai-prompts.md`
- AI output used: Removed hardcoded JWT fallback behavior, required JWT runtime configuration, persisted Data Protection keys, added EF diagnostics, moved staff/job-role/contract/vacation API logic into a BLL workflow service, added regression tests, and set local WebApp user secrets for the provided JWT values.
- What AI got wrong / needed correction: The first test-host configuration attempt injected JWT settings too late for minimal-host startup; the test factory was corrected to set test JWT environment variables before host creation. One staff regression test initially assumed North Star staff seed data and was changed to create its own cross-gym fixture.
- Changes made manually: Verified the full Assignment 03 backend test suite passes after the refactor and kept the provided JWT key out of tracked source and documentation.
- Alternatives considered: Adding a new gym-resolution middleware was deferred because the existing active-gym claim and authorization service flow is stable and lower risk for this pass.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system and assignment-18-dental-clinic-platform
- Prompt: Make a full report on the structural and safety differences of the BLL in Assignment 18 and Assignment 03.
- Files affected: `courses/webapp-csharp/backend-differences-assignment-03-vs-18.md`, `README.md`, `docs/ai-prompts.md`
- AI output used: Reworked the existing cross-assignment comparison into a BLL-focused report covering project references, dependency direction, service granularity, contracts, tenant access, authorization, validation, transactions, controller leakage, tests, risks, and recommendations.
- What AI got wrong / needed correction: Nothing material; the report was grounded in the current local BLL, WebApp DI, controller, DbContext, test, and architecture documentation files.
- Changes made manually: Checked the official course BLL lecture framing and verified the report against inspected repository files.
- Alternatives considered: Keeping the broader backend report unchanged, but the requested scope was specifically BLL structure and safety.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: Implement the safe first pass from the Assignment 03 controller hardening plan and list what remains for later implementation.
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/WebApp/ApiControllers/**`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/**`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Helpers/ControllerTestHelpers.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Unit/TenantControllerTests.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Unit/MembershipWorkflowServiceTests.cs`, `courses/webapp-csharp/backend-differences-assignment-03-vs-18.md`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/a3-saas-plan.md`, `docs/ai-prompts.md`
- AI output used: Removed direct `AppDbContext` access from `ApiControllerBase`, added cancellation-token plumbing through API controllers and BLL services, added success `[ProducesResponseType]` metadata for API actions, threaded tokens into EF async calls and tenant authorization, added controller unit-test helpers and tests for members/bookings/memberships, and documented the completed safe pass plus the remaining implementation list.
- What AI got wrong / needed correction: The first mechanical OpenAPI metadata rewrite produced invalid `typeof()` attributes, which were repaired before validation. A later cancellation-token cleanup exposed a private staff helper signature mismatch, which was corrected before the final test run.
- Changes made manually: Reviewed created-response metadata, preserved existing route/DTO/status/body behavior, checked for remaining EF async calls without cancellation tokens, and ran the full backend test suite.
- Alternatives considered: Converting all create/delete responses to stricter REST semantics immediately was deferred to avoid breaking current React/MVC clients before broader controller tests and client compatibility checks are in place.

- Date: 2026-04-22
- Subject: webapp-csharp
- Assignment: assignment-03-multi-gym-management-system
- Prompt: implement a3-saas-plan.md
- Files affected: `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.DTO/v1/Identity/JwtResponse.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/src/App.BLL/Services/IdentityService.cs`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/components/AppShell.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/lib/*`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/client/src/App.test.tsx`, `courses/webapp-csharp/assignment-03-multi-gym-management-system/tests/WebApp.Tests/Integration/SmokeTests.cs`, assignment README and docs.
- AI output used: Completed the remaining local SaaS-plan gap by adding assigned tenant/role metadata to JWT responses and wiring the React shell to switch active tenant and role for non-system multi-gym users as well as SystemAdmin.
- What AI got wrong / needed correction: No material correction; the implementation was kept additive so existing auth clients can ignore the new response field.
- Changes made manually: Verified the official course assignment/deploy requirements, ran backend build/tests and frontend tests/build, and synchronized the plan, architecture, API, testing, README, and AI logs.
- Alternatives considered: Keeping switch actions only in the function console, but a shell picker is more appropriate for a real SaaS workspace and closes the plan's remaining local UX gap.
