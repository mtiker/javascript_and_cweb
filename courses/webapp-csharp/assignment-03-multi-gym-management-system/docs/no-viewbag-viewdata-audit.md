# No ViewBag/ViewData Audit

**Audited:** 2026-04-28

## Rule

MVC Admin pages must pass data through strongly typed view models, not through `ViewBag` or `ViewData`.

## Current Result

All Razor files under `src/WebApp/Areas/Admin/Views` avoid:
- `ViewBag`
- `ViewData`

All Admin controllers under `src/WebApp/Areas/Admin/Controllers` avoid:
- `ViewBag`
- `ViewData`

## Anti-Forgery Result

The Admin area currently has no POST actions. The regression test still scans Admin controller source and will fail if a future `[HttpPost]` action is added without `[ValidateAntiForgeryToken]`.

MVC Client POST actions do use anti-forgery tokens and remain separate from this Admin audit.

## Tests

Covered by `MvcComplianceTests`:
- `AdminViews_DoNotUse_ViewBagOrViewData`
- `AdminPostActions_UseAntiForgery`
- `AdminControllers_ReturnStronglyTypedViewModels`

Covered by `AdminMembersPageTests`:
- `AdminMembersPage_DoesNotUseViewBagOrViewData`

## Defense Note

The course examples may show `ViewBag` or `ViewData` for simple MVC demos, but this project deliberately uses view models because they are safer, typed, easier to test, and more maintainable. This choice is consistent with the repository architecture rules and keeps data flow explicit for defense.
