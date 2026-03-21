# JavaScript Assignment 02 - TypeScript Migration + Enhancement

Public URL: `https://mtiker-js-ts.proxy.itcollege.ee`

TypeScript migration of Assignment 1 with strict typing and new features.

Deployment for the VPS/docker part of this app is documented in `../assignment-03-ci-cd-1/README.md`. The current production plan exposes this app on VPS host port `82`.

## Objective Coverage

- Full TypeScript conversion with `strict` mode
- Custom type definitions for all entities (`src/types.ts`)
- Generic utility functions (3+) in `src/generics.ts`
- New features:
  - recurring tasks
  - task dependencies
  - live statistics
  - search
  - sorting
  - category-priority relationship validation

## Stack

- Pure browser app (no frameworks)
- TypeScript + DOM APIs
- Browser storage (`localStorage`)

## Build and Run

1. Open this folder in terminal.
2. Install dependencies with `npm install`.
3. Run `npm run check` to validate the strict TypeScript source.
4. Run `npm run build` to rebuild `dist/`.
5. Run `npm test` for regression checks or `npm run coverage` for the built-in Node coverage report.
6. Serve this folder with a static server (for example VS Code Live Server).
7. Open `index.html` and confirm `dist/main.js` is loaded.
8. Treat `src/` as the source of truth and `dist/` as committed browser output.

## Command Coverage

- `add`
- `list`
- `update`
- `delete`
- `filter`
- `search`
- `sort`

## Feature Notes

- Dependency validation:
  - prevents self-dependency
  - prevents missing dependency IDs
  - prevents dependency cycles
  - prevents marking task `done` before dependencies are `done`
- Recurrence:
  - when a recurring task is completed, next task is generated automatically
  - supports frequency + interval + optional recurrence end date
  - repeated edits on the same completed task do not create duplicate next occurrences
- Statistics:
  - total
  - completed
  - blocked
  - overdue
  - completion rate

## Verification Focus

- `npm run check`
- `npm run build`
- `npm test`
- `npm run coverage`
- Manual browser smoke test for:
  - recurrence generation
  - dependency blocking
  - search/filter/sort combinations
  - statistics updates

## Security Notes

- Strict validation before every write
- Safe DOM rendering via `textContent` only
- CSP in `index.html`
- Defensive storage parsing and explicit error handling

## Project Structure

```text
assignment-02-ts-task-manager/
  index.html
  styles.css
  tsconfig.json
  src/
    constants.ts
    errors.ts
    generics.ts
    main.ts
    service.ts
    statistics.ts
    storage.ts
    types.ts
    ui.ts
    utils.ts
    validation.ts
```

## AI Reflection Deliverable

See:

- `AI_REFLECTION.md` (this folder)
