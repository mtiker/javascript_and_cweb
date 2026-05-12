# Localization Audit

Date: 2026-05-11

## Scope

This audit covers the current Assignment 03 localization model:

- UI strings through `.resx` resources in `App.Resources`
- request culture selection through ASP.NET Core request localization
- database-owned business strings through `LangStr`
- React client language state and `Accept-Language` API headers

No localization subsystem rewrite was performed.

## Current Design

Supported cultures:

- `et-EE`
- `et`
- `en`
- `en-US`

Default culture:

- `et-EE`

ASP.NET Core request localization uses the default provider chain, including `Accept-Language`, culture cookies, and query string culture values.

UI localization:

- MVC views inject `IStringLocalizer<SharedResources>`
- shared labels live in `src/App.Resources/SharedResources.resx`
- Estonian labels live in `src/App.Resources/SharedResources.et.resx`
- tested MVC login labels prove `.resx` lookup for `en` and `et-EE`
- high-visible MVC Admin labels for dashboard, gyms, members, memberships,
  membership packages, sessions, operations, and training categories use shared
  `.resx` resources

Database localization:

- business-owned translatable values use `LangStr`
- EF stores `LangStr` through the existing value converter and comparer
- training category names/descriptions are projected using current UI culture

React localization:

- language state is stored in `localStorage`
- the shell language selector updates the shared language context
- API requests set `Accept-Language` from the current language

## Verified Behavior

Verified by automated tests:

- `Accept-Language: en` returns English category names
- `Accept-Language: et` returns Estonian category names
- `Accept-Language: et-EE` resolves through neutral `et` category values
- missing requested translation falls back to a safe available value
- MVC labels render from `.resx` resources
- authenticated `/Admin/Members` renders English and Estonian labels from
  `.resx` resources
- React language selector affects the next training-category API request header
- validation errors for training-category create return `ProblemDetails`

## Boundaries

The two localization mechanisms solve different problems:

- `.resx` is for application UI labels owned by the codebase
- `LangStr` is for tenant/business data stored in the database

The React client has its own small UI translation dictionary for client-side labels. It still relies on `Accept-Language` for localized API data.

## Remaining Limitations

- The training-category API does not expose multi-culture edit payloads. It writes the submitted value to the request UI culture.
- Resource coverage is focused on the active MVC Admin and MVC Client labels,
  not every string in the application.
- Domain values such as enum display text and tenant-owned data can still appear
  in their stored/source form unless a specific view maps them through resources.
- No separate localization management UI exists for tenant admins.
