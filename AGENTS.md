# Agent Rules For This Repository

These rules guide future Codex sessions in this repository. Use them as project-specific defaults, not as a reason to block simple work.

## Operating Principles

- Act as a principal engineer: optimize for correctness, maintainability, clarity, security, and operability.
- Understand the local architecture and conventions before changing non-trivial code.
- Prefer the smallest correct change that fits the existing project shape.
- Keep domain logic, data access, orchestration, and UI/API boundaries clear.
- Avoid unnecessary dependencies, speculative abstractions, fake implementations, and placeholder logic.
- Do not make silent breaking changes.
- Scale process to risk: small mechanical changes need light validation; architectural, auth, data, deployment, or assignment-scope changes need stronger planning and verification.

## Repository Structure

- Root layout:
  - `courses/javascript`
  - `courses/webapp-csharp`
  - `shared`
  - `docs`
- Assignment folders use `assignment-XX-short-name`.
- Use lowercase kebab-case for new folders and files unless an existing framework convention requires otherwise.

## Branch And Commit Naming

- Branches:
  - Feature: `feat/<subject>/aXX-<short-name>`
  - Fix: `fix/<subject>/aXX-<short-name>`
  - Docs: `docs/<subject>/aXX-<short-name>`
- Subjects:
  - `javascript`
  - `webapp-csharp`
- Commits use Conventional Commits with subject/assignment scope:
  - `feat(javascript/a01): ...`
  - `fix(webapp-csharp/a01): ...`
  - `docs(javascript/a01): ...`

## Documentation

- Keep documentation accurate when a change affects setup, behavior, architecture, deployment, or assignment status.
- Do not force documentation updates for purely internal, mechanical, or exploratory changes.
- Keep root `README.md` as the repository map and workflow entry point.
- Keep per-assignment `README.md` files focused on running, testing, deploying, and understanding that assignment.
- If an assignment is publicly deployed and reachable from another device/network, place the live URL near the beginning of that assignment `README.md`.
- Log meaningful AI assistance in `docs/ai-prompts.md` when it materially affects assignment work or design decisions. Routine small edits do not need a log entry.
- For `courses/webapp-csharp/assignment-03-multi-gym-management-system`, keep `docs/a3-saas-plan.md` aligned when implementation scope, architecture, entities, roles, APIs, tests, deployment expectations, or delivery status change.

## Course Source Priority

- For `courses/javascript` work, prefer the current official course material:
  - `https://courses.taltech.akaver.com/javascript/`
- For `courses/webapp-csharp` work, prefer the current official course material:
  - `https://courses.taltech.akaver.com/web-applications-with-csharp`
  - `https://courses.taltech.akaver.com/programming-in-csharp`
- If course material conflicts with generic advice, follow the course material.
- Read official course pages before implementing only when the task depends on current assignment requirements, defense expectations, or course phase. For localized bug fixes or cleanup, use existing repo context first.

## Planning Defaults

For non-trivial assignment work:

- Identify the subject, assignment number, active course phase, and whether the work is cumulative.
- Convert requirements into a short checklist covering only relevant areas:
  - architecture and stack
  - domain entities and relationships
  - roles/auth boundaries
  - tests and validation
  - docs, diagrams, deployment, or CI when affected
- Work layer-by-layer when the change is large enough to become hard to review.
- Surface assumptions, tradeoffs, and risks before editing when requirements are ambiguous or the change could affect architecture, data integrity, auth, deployment, or grading.

For small tasks:

- Do the change directly after checking the local context.
- Avoid ceremony that does not reduce risk.

## JavaScript Course Defaults

- Match the current assignment phase instead of jumping ahead:
  - A1: browser JavaScript, CRUD, browser storage, async handling, and validation.
  - A2: TypeScript migration, custom types, generic utilities where required, domain enhancements, runtime validation, and tests.
  - A3: Vite + Vue 3 + TypeScript + Pinia + Vue Router, reusable components, responsive UI, local persistence, drag-drop where relevant.
  - A4: Vite + React + TypeScript with React Router, Zustand or Context + `useReducer`, custom hooks, reusable components, protected routes, error boundaries, memoization where useful, and loading states.
  - A5: Docker, GitLab CI/CD, VPS deployment, multi-stage images, compose files, reverse-proxy/SSL concerns, health checks, and deployment notes where required.
  - A6: NestJS + TypeScript + PostgreSQL or SQLite with Prisma or TypeORM, migrations, Swagger, JWT/refresh tokens, frontend auth integration, and meaningful tests when this phase is active.
  - A7: Angular 17+ integration with standalone components, services, interceptors, guards, RxJS HTTP flows, reactive forms, and auth integration when this phase is active.
- Treat A6-A7 as extended curriculum unless the active course phase requires them.
- Prefer Vitest for unit tests, framework component tests for interaction-heavy UI, Supertest for backend integration tests, and Playwright for E2E/auth flows when the project already supports them.
- At least one framework implementation should behave like a real application when the assignment asks for a full app, not just a thin CRUD shell.

## Webapp C# Course Defaults

- Follow the course architecture progression:
  - A1: ASP.NET Core MVC N-tier app with Identity, i18n, htmx/Alpine.js where required, and tests where practical.
  - A2: new student project with Clean/Onion Architecture, REST API, JWT, and a working client app.
  - A3: Docker, CI/CD, and VPS deployment.
  - A4: modular monolith refactor of the same A2/A3 project when required.
  - A5: extract one module into a simple microservice when required.
- Do not collapse A1 and A2 into one architecture.
- Treat A2-A5 as cumulative unless the teacher reduces scope.
- Use the course-aligned stack by default where applicable:
  - .NET 10
  - ASP.NET Core
  - EF Core Code First migrations
  - PostgreSQL in Docker
  - Swagger
  - Docker Desktop
  - Postman/Bruno
- For SaaS assignments, treat tenant isolation as a first-class invariant:
  - strict tenant filtering
  - no cross-tenant data leaks
  - appropriate system/company roles
  - audit trail and soft delete where the domain needs them
  - subscription or feature limits when required
- Keep DTO boundaries, validation, null safety, structured errors, logging, and dependency direction aligned with Clean Architecture principles.
- A real client app is required when the course expects it; Swagger/Postman alone is not enough.

## Quality And Security

- Frontend work should be responsive, accessible, visually coherent, and complete enough for the assignment or feature scope.
- For user-facing mutations, include success/error feedback, validation feedback, disabled/in-progress states, and destructive-action confirmation when relevant.
- For lists and searches, handle loading, empty, filtered-empty, and error states when relevant.
- Use safe DOM APIs and avoid unsafe HTML insertion unless explicitly justified and sanitized.
- Validate inputs at appropriate boundaries. For APIs, prefer consistent error responses.
- For auth, tenant, and security-sensitive work, add explicit forbidden-path checks where practical.

## Testing And Verification

- Add or update tests for meaningful behavior changes when the project supports testing.
- Focus test depth on risk:
  - unit tests for logic and helpers
  - component/UI tests for interaction-heavy frontend behavior
  - integration or E2E tests for cross-module flows, auth, data access, and deployment-critical paths
- For bug fixes, reproduce the bug with a test when practical, then keep the regression test.
- Run the strongest relevant checks available for the changed area: tests, linting, type checks, build, or smoke startup.
- Do not claim something works unless it was actually verified. If verification is skipped or blocked, state that clearly.

## Architecture And Change Discipline

- Modify the minimum necessary files, but include adjacent updates that are required for correctness.
- If behavior changes, review related tests, types/contracts, validation, UI states, error handling, and documentation where relevant.
- Do not leave critical TODOs for required functionality.
- Prefer course-aligned patterns that are easy to explain in defense over unnecessary abstraction or unsupported libraries.
- When both subjects start sharing functionality, create or extend a dedicated integration area under `courses`; move only truly reusable parts to `shared`.

## Final Response

When finishing implementation work, summarize:

1. what changed
2. which files changed
3. what tests were added or updated
4. what documentation changed, or that none was needed
5. what remains unverified or worth following up
