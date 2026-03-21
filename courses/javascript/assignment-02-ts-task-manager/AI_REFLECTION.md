# AI Usage Reflection - Assignment 02

Date: 2026-02-25
Subject: JavaScript
Assignment: 02 (TypeScript Migration + Enhancement)

## What Worked Well

- Breaking A2 into layers (`types`, `validation`, `storage`, `service`, `ui`) made migration faster and clearer.
- Defining strict domain types early reduced runtime bugs while adding new features.
- Generic utilities (`groupBy`, `sortBy`, `uniqueBy`) reduced duplicate logic in statistics and sorting.
- Service-first implementation made advanced features (dependencies, recurring tasks) easier to enforce consistently.

## What Did Not Work Well

- PowerShell execution policy can block `npm`, so Windows verification may need `npm.cmd`.
- Browser module setup requires `dist/` build output; opening without compile step can look broken.
- Strict mode with `noUncheckedIndexedAccess` and DOM template typing required extra cleanup before `tsc` could pass.

## What Was Adjusted

- Added explicit build instructions and strict `tsconfig.json`.
- Added defensive parsing for storage and stronger validation boundaries.
- Kept runtime errors visible in UI status line and technical details in console.
- Added automated regression tests and coverage scripts for recurrence, dependency-cycle protection, and date calculation.

## Next Iteration Improvements

- Add CI step to run `npm run check`, `npm test`, and coverage automatically.
- Add export/import JSON backup feature for task data portability.
- Add browser-level interaction tests once the course moves to a fuller frontend toolchain.
