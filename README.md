# School Monorepo

## Structure

- `courses/javascript`: JavaScript subject assignments
- `courses/webapp-csharp`: Web App in C# subject assignments
- `shared`: reusable code/assets moved here when both subjects need them
- `docs`: prompts, checklists, and submission notes

## Naming Rules

- Use lowercase kebab-case for folders/files.
- Assignment folders must follow `assignment-XX-short-name`.
- Keep each assignment self-contained with its own `README.md`.

## Workflow Rules

- `main` stays stable.
- Use feature branches per subject/assignment.
- Use conventional commits with subject scope.
- Check official course materials before assignment-specific architectural or tooling decisions.
- Keep assignment work defense-ready: tests, documentation, AI log, and required evidence should stay in sync with code.
- If a task is large, prefer finishing one layer at a time and leave a short checkpoint summary before context gets too compressed.

## CI/CD Layout

- Root GitLab CI entrypoint lives in `.gitlab-ci.yml`.
- Assignment-specific CI files live inside the assignment they belong to.
- Docker-related files should stay beside the deployable assignment, not in the monorepo root unless they are truly shared.
- Runner host configuration stays outside version control; document runner tags and deployment expectations in `docs/ci-cd.md`.

Examples:
- `feat(javascript/a01): add task CRUD`
- `fix(webapp-csharp/a01): resolve form validation issue`
- `docs(javascript/a01): update usage instructions`
