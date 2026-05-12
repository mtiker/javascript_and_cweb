# Training Category Audit

Date: 2026-04-28

## Scope

This audit covers the Assignment 03 training-category vertical slice:

- domain entity: `TrainingCategory`
- DTOs: `TrainingCategoryResponse`, `TrainingCategoryUpsertRequest`
- module orchestration: `Modules.Training.Application` category handlers
- API controller: `/api/v1/{gymCode}/training-categories`
- React CRUD page: `TrainingCategoriesPage`
- localization behavior for category `Name` and `Description`

The work intentionally did not move the project to a new architecture or rewrite the localization system.

## Current Contract

API routes:

- `GET /api/v1/{gymCode}/training-categories`
- `POST /api/v1/{gymCode}/training-categories`
- `PUT /api/v1/{gymCode}/training-categories/{id}`
- `DELETE /api/v1/{gymCode}/training-categories/{id}`

Access rules:

- read: `GymOwner`, `GymAdmin`, `Member`, `Trainer`
- create/update/delete: `GymOwner`, `GymAdmin`
- all operations must pass active gym access validation
- update/delete now also query the category by both `Id` and active `GymId`

Response behavior:

- translated category values are projected through the existing training mapper
  using `CultureInfo.CurrentUICulture`
- create returns `201 Created`
- update returns `200 OK`
- delete returns `204 NoContent`
- validation failures return `application/problem+json`

## Findings

Resolved:

- whitespace category names were accepted and persisted
- category update/delete looked up by `Id` only after tenant authorization, leaving the query itself less explicit than the tenant invariant requires

Current implementation:

- `TrainingCategoryUpsertRequest.Name` is required and capped at 128 characters
- `TrainingCategoryUpsertRequest.Description` is capped at 512 characters
- Training-module create/update handlers reject blank names with
  `ValidationAppException`
- update/delete use `entity.Id == id && entity.GymId == gymId`

## Test Evidence

Backend integration coverage:

- full training-category CRUD
- `Accept-Language: en` returns the English `LangStr` value
- `Accept-Language: et` and `et-EE` return the Estonian `LangStr` value
- missing requested translation falls back safely
- invalid category name returns `ProblemDetails`
- MVC login labels render from `.resx` resources for `en` and `et-EE`

Frontend coverage:

- React shell language selector changes the selected language
- subsequent training-category API request sends `Accept-Language: et-EE`

## Remaining Risks

- The API currently accepts one localized value per create/update request, using the request UI culture as the write culture. Editing multiple translations in one request is not implemented.
- Existing seed data has some descriptions only in English; this is acceptable because `LangStr` fallback is now covered by tests.
- Full cross-tenant mutation tests for training categories are not part of this slice, but the touched update/delete queries now include active `GymId`.
