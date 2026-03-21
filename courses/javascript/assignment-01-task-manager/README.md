# JavaScript Assignment 01 - Task Manager

Browser-based task management utility built with pure JavaScript.

Deployment for the VPS/docker part of this app is documented in `../assignment-03-ci-cd-1/README.md`. The current production plan exposes this app on VPS host port `81`.

## Assignment Coverage

- No framework, pure HTML/CSS/JS
- CRUD operations with browser storage (`localStorage`)
- Task model:
  - `id`
  - `title`
  - `description`
  - `status`
  - `priority`
  - `dueDate`
  - `tags[]`
- Required commands implemented in UI:
  - `add`
  - `list`
  - `update`
  - `delete`
  - `filter`
  - `search`
- Async flows for storage and command handling
- Input validation and user-facing error messages

## Run

1. Prefer running via a static server (for example VS Code Live Server), because the app now uses ES module entry points.
2. Open `index.html` and confirm it loads `src/main.js`.

## Verification

1. Run `npm test` from this folder for the local regression checks.
2. Run `npm run coverage` to print the built-in Node coverage report for the current tests.
3. Open the app in a browser and verify both empty states:
   - no tasks in storage
   - filters/search hide existing tasks

## Usage

1. Fill the form and click `Add Task` (or command `add`).
2. Click `Edit` in table to load a task into form.
3. Save again to run `update`.
4. Use `Delete Selected` or row `Delete`.
5. Use command deck inputs and `filter` / `search`.
6. Use `list` to clear filters and show all tasks.
7. When filters or search hide all records, the table shows a filtered-empty message instead of the first-run empty state.

## Validation Rules

- `title`: required, max 100 chars
- `description`: max 500 chars
- `status`: `todo | in_progress | done`
- `priority`: `low | medium | high`
- `dueDate`: `YYYY-MM-DD` or empty
- `tags[]`: deduplicated, lowercase, max 10 tags, max 20 chars per tag

## Security Notes

- Strict DOM updates through `textContent` (no unsafe HTML rendering)
- CSP meta policy in `index.html`
- Defensive storage parsing with error handling
- All command handlers use controlled async error boundaries

## Project Structure

```text
assignment-01-task-manager/
  index.html
  styles.css
  src/
    constants.js
    errors.js
    main.js
    storage.js
    task-service.js
    ui.js
    utils.js
    validation.js
```

## AI Assistance Evidence

Prompts/log are documented in:

- `docs/ai-prompts.md` (repo root)
