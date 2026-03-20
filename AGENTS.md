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
- Log AI assistance in `docs/ai-prompts.md`.
- With every implemented change, verify that related documentation stays in sync, including relevant `README.md` files and `docs/ai-prompts.md`.

## Course Source Priority

- For `courses/javascript` tasks, prioritize:
  - `https://courses.taltech.akaver.com/javascript/`
- For `courses/webapp-csharp` tasks, prioritize:
  - `https://courses.taltech.akaver.com/web-applications-with-csharp`
  - `https://courses.taltech.akaver.com/programming-in-csharp`
- When course material conflicts with generic guidance, follow course material first.

## Quality Defaults

- Put strong emphasis on visual quality in frontend outputs:
  - responsive layout
  - clear hierarchy and typography
  - intentional color/motion choices
- Put strong emphasis on security in all assignments:
  - strict input validation
  - safe DOM writes (`textContent`, no unsafe HTML insertion)
  - defensive error handling
  - secure-by-default browser settings where practical (for example CSP)
- Require tests for implemented changes whenever the codebase supports them, covering both positive and negative scenarios.

## Integration Rule

When both subjects start sharing functionality:
- Create/extend a dedicated integration area under `courses`.
- Move only truly reusable parts to `shared`.
- Do not duplicate shared logic across subject folders.
