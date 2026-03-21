# Agent Rules For This Repository

These rules apply to this repository in future Codex sessions.

## Repository Structure

- Root layout:
  - `courses/javascript`
  - `courses/webapp-csharp`
  - `shared`
  - `docs`
- Assignment folders use: `assignment-XX-short-name`.
- Use lowercase kebab-case for folder/file names.

## Branch Naming

- Feature: `feat/<subject>/aXX-<short-name>`
- Fix: `fix/<subject>/aXX-<short-name>`
- Docs: `docs/<subject>/aXX-<short-name>`

Subjects:
- `javascript`
- `webapp-csharp`

## Commit Naming

Use Conventional Commits with subject/assignment scope:
- `feat(javascript/a01): ...`
- `fix(webapp-csharp/a01): ...`
- `docs(javascript/a01): ...`

## Documentation Rules

- Keep root `README.md` as repository map and workflow rules.
- Keep per-assignment `README.md` inside each assignment folder.
- If an assignment is deployed and reachable from a third device/network, place its live URL at the beginning of that assignment `README.md`.
- Log meaningful AI assistance in `docs/ai-prompts.md`.
- Each AI log entry should include, where relevant:
  - date
  - task or assignment context
  - prompt summary or prompt text
  - files affected
  - what AI helped with
  - what AI got wrong or what had to be corrected
  - what was reviewed/corrected manually
  - alternative solutions considered
  - ADR/ERD notes when relevant
- With every implemented change, verify that related documentation stays in sync, including relevant `README.md` files and `docs/ai-prompts.md`.

## Documentation Standard

Write documentation at a professional level expected from a strong senior/top-level developer.

Documentation must be:
- accurate
- concise but complete
- easy for a new contributor to follow
- updated in the same change when behavior changes

When implementing or changing features, keep in sync where relevant:
- root `README.md`
- assignment-level `README.md`
- deployed assignment URL at the start of the assignment `README.md` when public third-device access exists
- setup steps
- run/test instructions
- architectural notes
- known limitations
- `docs/ai-prompts.md`

## Course Source Priority

- For `courses/javascript` tasks, prioritize:
  - `https://courses.taltech.akaver.com/javascript/`
- For `courses/webapp-csharp` tasks, prioritize:
  - `https://courses.taltech.akaver.com/web-applications-with-csharp`
  - `https://courses.taltech.akaver.com/programming-in-csharp`
- When course material conflicts with generic guidance, follow course material first.
- If a task maps to a specific assignment, defense, or course phase, read the relevant official course pages before implementing and extract the current required stack, deliverables, and defense expectations.

## Course-Driven Planning Rules

- Before writing code, identify the subject, assignment number, current course phase, and whether the work is cumulative from earlier assignments.
- Translate official course requirements into a concrete checklist before implementation:
  - architecture and stack
  - domain scope and entities
  - roles/auth requirements
  - tests
  - documentation
  - diagrams/evidence
  - deployment/CI expectations
- Do not skip prerequisite artifacts when the course requires them, such as project proposal approval, a real client application, deployment documentation, or diagrams.
- Favor course-taught tools and patterns before introducing alternatives. If deviating, document why and keep the result easy to defend verbally.
- Build around real assignment/domain rules early so later work is not blocked by missing entities, relationships, validation, authorization, or deployment setup.
- If the task looks large enough that context may become tight, plan and implement one layer at a time instead of spreading partial work across the whole stack at once.
- In context-sensitive work, prefer an order such as domain/data model -> business/application layer -> API/UI layer -> tests -> documentation, unless the task clearly requires a different order.
- Tell the user early when work will be split into layer-based batches because context compaction can lose lower-level details, temporary findings, or assumptions if too much is changed at once.
- Before any context compaction or major checkpoint, summarize completed work, current assumptions, open risks, and the next concrete layer to continue from.

## Subject-Specific Defaults

### `courses/javascript`

- Match the assignment phase rather than jumping ahead:
  - A1: pure JavaScript, browser-based CRUD, storage, async handling, and validation.
  - A2: strict TypeScript, custom types, generic utilities, search/sorting, recurring-task enhancements, relationship modeling, Zod runtime validation, and coverage reporting.
  - A3: Vite + Vue 3 + TypeScript + Pinia + Vue Router, reusable components, responsive/mobile-first UI, local persistence, and drag-drop where relevant.
  - A4: Vite + React + TypeScript, Router, state management, custom hooks, reusable components, error boundaries, memoization where appropriate, and loading states.
  - A5: Docker, GitLab CI/CD, VPS deployment, multi-stage images, local/prod compose files, health checks, and deployment documentation.
  - A6: NestJS + TypeScript + Prisma/TypeORM + migrations + Swagger + JWT + refresh tokens + integrated frontend auth + comprehensive testing.
  - A7: Angular integrated with the backend, using modern Angular patterns such as standalone components, services, interceptors, route guards, and RxJS-based HTTP flows where relevant.
- At least one framework implementation must behave like a full application, not a thin CRUD shell. Plan for:
  - aggregated dashboards or summary views
  - multi-step or workflow-style flows where appropriate
  - backend integration
  - polished UX states
  - a deployed demo
- For JS backend/full-stack work, model required domain complexity early:
  - minimum 10 domain entities when the course requires it
  - identity/auth entities
  - meaningful relationships
  - at least 3 main entities with full CRUD
- Preferred test stack where supported:
  - Vitest for unit tests
  - framework component tests for interaction-heavy UI
  - Supertest for backend integration tests
  - Playwright for E2E/auth flows

### `courses/webapp-csharp`

- Follow the architecture progression expected by the course:
  - A1: server-rendered ASP.NET Core MVC N-tier app with Identity, i18n, htmx/Alpine.js, and tests from the start
  - A2: new student project using Clean/Onion Architecture + REST API + JWT + working client app
  - A3: Docker + CI/CD + VPS deployment
  - A4: modular monolith refactor of the same A2/A3 project
  - A5: extract one module into a simple microservice
- Do not collapse A1 and A2 into one architecture. A1 is a separate foundational exercise; A2-A5 are cumulative on the same student project.
- Treat A2 as blocked until the project proposal is approved when the course requires that prerequisite.
- Preserve cumulative deliverables from earlier phases. Moving from A2 to A5 must not silently regress the client app, deployment, auth flow, or test coverage already required.
- Use the course-aligned stack by default:
  - .NET 10
  - ASP.NET Core
  - EF Core Code First migrations
  - PostgreSQL in Docker
  - Swagger
  - Docker Desktop
  - Postman/Bruno
- Treat multi-tenancy as a first-class invariant where the course assignment expects SaaS:
  - strict `CompanyId` filtering
  - no cross-tenant leaks
  - system-level and company-level roles
  - audit trail
  - soft delete for business entities
  - subscription-tier or feature-limit rules
  - multi-company membership where applicable
- Treat client/API/auth work as mandatory, not optional polish:
  - a real client app is required when the course expects it; Swagger/Postman alone is not enough
  - JWT access tokens plus refresh token rotation
  - authorization and tenant isolation tests
  - consistent `ProblemDetails` error responses
  - validation in the application layer, not only controllers
- Treat DevOps and deployment artifacts as part of the feature set when the course phase expects them:
  - CI must run the existing tests
  - deployment docs must stay current
  - production URL and health-check path must be maintained where applicable
  - docker-compose and environment setup must reflect the actual runnable system
- Keep the C# codebase aligned with course-taught design principles:
  - nullable reference types and null safety
  - DTOs at boundaries
  - base entities and audit fields where appropriate
  - mapping discipline
  - clean layer boundaries with inward dependencies
  - global exception handling and structured logging
  - architecture-boundary verification/tests where practical
- For teacher-assigned SaaS domains, derive entities, roles, and business constraints from the assignment description before coding.
- Maintain required diagrams and evidence when relevant:
  - ERD in Mermaid
  - DB/architecture/auth-flow diagrams
  - module/context maps for modular monolith work
  - trade-off notes for refactors and service extraction

## Definition of Done and Evidence

Do not present partial implementation as complete.

For course assignments, completion normally includes all relevant parts moving together:
- implementation
- tests
- documentation
- diagrams/evidence
- deployment or startup instructions
- AI usage log

For every user-facing feature or meaningful change, verify and complete where relevant:
- create/add flow
- read/list/details flow
- edit/update flow
- delete/remove flow
- filter/search flow
- loading state
- empty state
- filtered-empty state
- error state
- success feedback
- validation feedback
- disabled/in-progress state

If the assignment involves defense/demo expectations, also verify where relevant:
- app builds successfully
- app starts successfully
- core happy-path scenario is smoke-tested
- demo or seed data exists where needed
- README contains current run/test/deploy commands
- diagrams/evidence are updated

## Defense-Readiness Rules

- Never leave assignment work in a state that does not compile, start, or run its core scenario if the task is being reported as complete.
- Prefer a stable, explainable, course-aligned solution over risky last-minute refactors.
- Before and after substantial refactors, verify the current known-good build/test/start path.
- For defense-oriented work, keep the repository ready for a known-good commit/tag by maintaining runnable code, clear setup instructions, and synchronized documentation.
- If a task affects architecture, auth, tenant isolation, or deployment, verify the corresponding evidence and explanation artifacts too, not just the code.
- Close to defense or code-freeze milestones, prefer preserving a last known working commit/tag and avoid leaving the branch in a half-migrated state.

## Quality Defaults

- Put strong emphasis on visual quality in frontend outputs:
  - responsive layout
  - clear hierarchy and typography
  - intentional color/motion choices
  - consistent spacing and component states
  - accessible interaction patterns

- For frontend features, require complete UX behavior where relevant:
  - success toast for successful mutations
  - error toast for failed mutations
  - confirmation for destructive actions such as delete
  - clear empty-state messaging
  - clear filtered-empty-state messaging
  - filter reset/clear option
  - loading and disabled states during async work

- Put strong emphasis on security in all assignments:
  - strict input validation
  - safe DOM writes (`textContent`, no unsafe HTML insertion)
  - defensive error handling
  - secure-by-default browser settings where practical (for example CSP)

- Require tests for implemented changes whenever the codebase supports them, covering:
  - positive scenarios
  - negative scenarios
  - edge cases
  - regression-prone paths

## Testing Rules

When the repository supports testing, every meaningful change must add or update tests at the appropriate level.

Test expectations:
- unit tests for logic and helpers
- component/UI tests for interaction-heavy frontend behavior
- integration tests for end-to-end feature flow across modules where practical

For CRUD-style work, cover where relevant:
- create success and failure
- edit success and failure
- delete success and failure
- filtering/search behavior
- validation errors
- loading state
- empty state
- error state

When fixing a bug:
- first reproduce it with a failing or missing test when practical
- then implement the fix
- keep the regression test in place

For auth, multi-tenant, and security-sensitive work, add explicit tests for forbidden paths when practical:
- unauthorized requests
- wrong-role access
- cross-tenant data access attempts
- invalid token or refresh-token flows

## Architecture and Change Discipline

- Modify the minimum necessary files, but do not skip adjacent required updates.
- If a change affects behavior, also review and update where relevant:
  - tests
  - documentation
  - types/contracts
  - UI states
  - validation
  - error handling
  - diagrams/evidence
- Do not leave critical TODOs for required functionality.
- Start larger features by clarifying:
  - domain entities and relationships
  - authorization boundaries
  - validation rules
  - state transitions
  - error cases
  - deployment/runtime implications
- Prefer course-aligned patterns that are easy to explain in defense over unnecessary abstraction or unsupported libraries.

## Integration Rule

When both subjects start sharing functionality:
- Create/extend a dedicated integration area under `courses`.
- Move only truly reusable parts to `shared`.
- Do not duplicate shared logic across subject folders.

## Required Final Response

When finishing work, summarize:
1. what was changed
2. which files were changed
3. what tests were added or updated
4. what documentation was updated
5. any remaining limitations or follow-up items
