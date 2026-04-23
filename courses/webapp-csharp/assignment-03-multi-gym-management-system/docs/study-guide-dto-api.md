# Study Guide: DTO and API Layer

## Purpose
The API layer (`src/WebApp/ApiControllers`) exposes versioned REST endpoints using DTO contracts from `src/App.DTO/v1`.

## API Organization
- Identity: `/api/v1/account/...`
- Platform: `/api/v1/system/...`
- Tenant: `/api/v1/{gymCode}/...`

## DTO Organization
`App.DTO/v1` is split by resource folders (members, sessions, maintenance, coaching plans, finance, etc.) to keep contracts explicit and defense-friendly.

## Response Semantics
- `GET`/`PUT` operations return `200` with DTO payloads.
- Create operations used by the React workflows return `201` (`Created` / `CreatedAtAction`).
- Delete/cancel operations used by the React workflows return `204 NoContent`.

## Problem Details Contract
Public API controllers advertise and return `ProblemDetails` for:
- `400` validation issues
- `401` auth failures
- `403` forbidden access
- `404` missing resources
- `409` conflict paths

## Batch 4 Workflow Endpoints
- Member workspace: `/member-workspace/me`, `/member-workspace/members/{memberId}`
- Coaching workspace: `/coaching-plans`, `/coaching-plans/{id}`, status/item decision routes
- Finance workspace: `/finance-workspace/me`, `/finance-workspace/members/{memberId}`, invoice/payment/refund routes
- Maintenance workspace: assignment update/history and due-generation routes

## Defense Notes
- Public route stability was preserved while extending depth.
- API contract behavior is backed by unit and integration tests in `tests/WebApp.Tests` and Vitest coverage in `client/`.
