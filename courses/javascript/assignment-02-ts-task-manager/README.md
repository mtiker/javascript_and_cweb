# JavaScript Assignment 02 - TypeScript Migration + Enhancement

TypeScript migration of Assignment 1 with strict typing and new features.

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
2. If TypeScript tooling is available, install dependencies and rebuild `dist/` with `npm install` and `npx tsc`.
3. Serve this folder with a static server (for example VS Code Live Server).
4. Open `index.html` and confirm `dist/main.js` is loaded.
5. Treat `src/` as the source of truth and `dist/` as committed browser output.

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
- Statistics:
  - total
  - completed
  - blocked
  - overdue
  - completion rate

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
