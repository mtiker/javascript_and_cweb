# API Overview

## Versioning

All API routes use URL-segment versioning:

- `/api/v1/account/...`
- `/api/v1/system/...`
- `/api/v1/{gymCode}/...`

Tenant routes also pass through `GymResolutionMiddleware`, which resolves `gymCode` early and rejects unknown or inactive gyms before controller/BLL execution.

Controllers return public DTOs from `App.DTO/v1`.

## Authentication

API auth flow:
- `POST /api/v1/account/register`
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- `POST /api/v1/account/switch-gym`
- `POST /api/v1/account/switch-role`
- `POST /api/v1/account/forgot-password`
- `POST /api/v1/account/reset-password`

JWT carries:
- user id
- person id when available
- active gym id
- active gym code
- active tenant role
- system roles

Auth responses also return `availableTenants`, a list of active gym memberships and role names assigned to the user. The React client uses this for the shell gym/role picker; authorization still depends on the JWT claims and backend role checks.

## Platform API

Gyms:
- `GET /api/v1/system/gyms`
- `POST /api/v1/system/gyms`
- `PUT /api/v1/system/gyms/{gymId}/activation`
- `GET /api/v1/system/gyms/{gymId}/snapshot`

Platform analytics:
- `GET /api/v1/system/platform/analytics`

Platform-role expectations:
- `SystemAdmin`: gym onboarding, activation, snapshots, and analytics

Removed for Final2 scope control:
- subscriptions and support tickets
- impersonation
- `SystemSupport` and `SystemBilling` role flows

## Tenant API

Members:
- `GET /api/v1/{gymCode}/members`
- `GET /api/v1/{gymCode}/members/me`
- `GET /api/v1/{gymCode}/members/{id}`
- `POST /api/v1/{gymCode}/members`
- `PUT /api/v1/{gymCode}/members/{id}`
- `DELETE /api/v1/{gymCode}/members/{id}`
- `GET /api/v1/{gymCode}/member-workspace/me`
- `GET /api/v1/{gymCode}/member-workspace/members/{memberId}`

Training:
- `GET|POST /api/v1/{gymCode}/training-categories`
- `PUT|DELETE /api/v1/{gymCode}/training-categories/{id}`
- `GET /api/v1/{gymCode}/training-sessions`
- `GET /api/v1/{gymCode}/training-sessions/{id}`
- `POST /api/v1/{gymCode}/training-sessions`
- `PUT /api/v1/{gymCode}/training-sessions/{id}`
- `DELETE /api/v1/{gymCode}/training-sessions/{id}`
- `GET|POST /api/v1/{gymCode}/bookings`
- `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- `DELETE /api/v1/{gymCode}/bookings/{id}`

Memberships and payments:
- `GET|POST /api/v1/{gymCode}/membership-packages`
- `PUT|DELETE /api/v1/{gymCode}/membership-packages/{id}`
  - Package create/update validation, unused soft-delete, and used-package conflict semantics are summarized in `domain-workflows.md`.
- `GET|POST /api/v1/{gymCode}/memberships`
- `PUT /api/v1/{gymCode}/memberships/{id}/status`
- `DELETE /api/v1/{gymCode}/memberships/{id}`
- `GET|POST /api/v1/{gymCode}/payments`

Invoices, refunds, and the finance workspace were removed from the Final2
defense surface. `Payment` remains the only finance transaction API.

Staff:
- `GET|POST /api/v1/{gymCode}/staff`
- `PUT|DELETE /api/v1/{gymCode}/staff/{id}`

Employment contracts, job roles, vacations, and work shifts were removed from
the defended API surface.

Facilities:
- `GET|POST /api/v1/{gymCode}/equipment-models`
- `PUT|DELETE /api/v1/{gymCode}/equipment-models/{id}`
- `GET|POST /api/v1/{gymCode}/equipment`
- `PUT|DELETE /api/v1/{gymCode}/equipment/{id}`
- `GET|POST /api/v1/{gymCode}/maintenance-tasks`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/assignment`
- `POST /api/v1/{gymCode}/maintenance-tasks/generate-due`
- `DELETE /api/v1/{gymCode}/maintenance-tasks/{id}`
- `GET /api/v1/{gymCode}/gym-settings`
- `PUT /api/v1/{gymCode}/gym-settings`
- `GET|POST /api/v1/{gymCode}/gym-users`
- `DELETE /api/v1/{gymCode}/gym-users/{appUserId}/{roleName}`

Opening hours and maintenance assignment history were removed from the Final2
defense surface.

## Separate Client Contract

The React client now consumes focused Final2 workspaces:
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- `POST /api/v1/account/switch-gym`
- `POST /api/v1/account/switch-role`
- `POST /api/v1/account/forgot-password`
- `POST /api/v1/account/reset-password`
- member CRUD including `GET /api/v1/{gymCode}/members/{id}` for edit forms
- `GET /api/v1/{gymCode}/members/me` for member self-booking
- training-category CRUD
- membership-package CRUD
- session list/detail through `GET /api/v1/{gymCode}/training-sessions`
- member/admin booking through `POST /api/v1/{gymCode}/bookings`
- trainer/admin attendance through `GET /api/v1/{gymCode}/bookings` and `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- member workspace through `GET /api/v1/{gymCode}/member-workspace/me`
- caretaker/admin maintenance workspace through maintenance status,
  assignment, and due-generation endpoints

The member detail route returns a fuller payload than the member list route so the client can edit person fields without inventing a second contract.

The deployed ASP.NET Core app serves the built React client at `/client`; the client is still a separately built Vite application and consumes these REST endpoints through JWT-bearing HTTP calls. It sends the selected UI language as `Accept-Language` so `LangStr` values are resolved consistently in API responses.

## Security Rules

Tenant routes are protected by:
- active gym code matching
- role checks per route
- self-only or assignment-only checks where required

Important enforced rules:
- members can only read their own member profile data
- trainers can only update attendance for assigned sessions
- caretakers can only update assigned maintenance tasks
- tenant users cannot call system-only routes

## Errors

Unhandled API errors return `application/problem+json`.

Public API controllers document standard `ProblemDetails` error responses for:
- `400`
- `401`
- `403`
- `404`
- `409`

Typical mappings:
- validation failures -> `400`
- missing/invalid auth -> `401`
- forbidden access -> `403`
- missing resources -> `404`
- conflicts -> `409`
- unexpected server failures -> `500`

## REST Semantics Notes

Workflow endpoints used by the React client now follow these response rules:
- create actions return `201` with payload (`Created` or `CreatedAtAction`)
- delete/cancel actions return `204 NoContent`
- update/read actions return `200` with payload

## Swagger

Swagger is enabled in local development:

- `https://localhost:7245/swagger`
