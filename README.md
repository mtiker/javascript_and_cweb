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

Examples:
- `feat(javascript/a01): add task CRUD`
- `fix(webapp-csharp/a01): resolve form validation issue`
- `docs(javascript/a01): update usage instructions`
