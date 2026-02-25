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

- Environment did not have `tsc` available, so compile-time verification was not executable here.
- Browser module setup requires `dist/` build output; opening without compile step can look broken.
- Strict mode with `noUncheckedIndexedAccess` adds extra friction unless null/undefined handling is done carefully.

## What Was Adjusted

- Added explicit build instructions and strict `tsconfig.json`.
- Added defensive parsing for storage and stronger validation boundaries.
- Kept runtime errors visible in UI status line and technical details in console.

## Next Iteration Improvements

- Add unit tests for dependency cycle detection and recurrence generation.
- Add CI step to run `tsc --noEmit` automatically.
- Add export/import JSON backup feature for task data portability.
