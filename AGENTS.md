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

## Integration Rule

When both subjects start sharing functionality:
- Create/extend a dedicated integration area under `courses`.
- Move only truly reusable parts to `shared`.
- Do not duplicate shared logic across subject folders.
