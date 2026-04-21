# API Overview

## Versioning

All API routes use URL-segment versioning:

- `/api/v1/account/...`
- `/api/v1/system/...`
- `/api/v1/{gymCode}/...`

Controllers return public DTOs from `src/App.DTO/v1`.

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

## Platform API

Gyms:
- `GET /api/v1/system/gyms`
- `POST /api/v1/system/gyms`
- `PUT /api/v1/system/gyms/{gymId}/activation`
- `GET /api/v1/system/gyms/{gymId}/snapshot`

Subscriptions:
- `GET /api/v1/system/subscriptions`
- `PUT /api/v1/system/subscriptions/{gymId}`

Support:
- `GET /api/v1/system/support`
- `POST /api/v1/system/support/{gymId}/tickets`

Platform analytics:
- `GET /api/v1/system/platform/analytics`

Impersonation:
- `POST /api/v1/system/impersonation`

Platform-role expectations:
- `SystemAdmin`: full gym onboarding, activation, analytics, impersonation
- `SystemSupport`: gym list, snapshots, tickets, analytics
- `SystemBilling`: gym list, subscriptions, analytics

## Tenant API

Members:
- `GET /api/v1/{gymCode}/members`
- `GET /api/v1/{gymCode}/members/me`
- `GET /api/v1/{gymCode}/members/{id}`
- `POST /api/v1/{gymCode}/members`
- `PUT /api/v1/{gymCode}/members/{id}`
- `DELETE /api/v1/{gymCode}/members/{id}`

Training:
- `GET|POST /api/v1/{gymCode}/training-categories`
- `PUT|DELETE /api/v1/{gymCode}/training-categories/{id}`
- `GET /api/v1/{gymCode}/training-sessions`
- `GET /api/v1/{gymCode}/training-sessions/{id}`
- `POST /api/v1/{gymCode}/training-sessions`
- `PUT /api/v1/{gymCode}/training-sessions/{id}`
- `DELETE /api/v1/{gymCode}/training-sessions/{id}`
- `GET|POST /api/v1/{gymCode}/work-shifts`
- `PUT|DELETE /api/v1/{gymCode}/work-shifts/{id}`
- `GET|POST /api/v1/{gymCode}/bookings`
- `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- `DELETE /api/v1/{gymCode}/bookings/{id}`

Memberships and payments:
- `GET|POST /api/v1/{gymCode}/membership-packages`
- `PUT|DELETE /api/v1/{gymCode}/membership-packages/{id}`
- `GET|POST /api/v1/{gymCode}/memberships`
- `DELETE /api/v1/{gymCode}/memberships/{id}`
- `GET|POST /api/v1/{gymCode}/payments`

Staff:
- `GET|POST /api/v1/{gymCode}/staff`
- `PUT|DELETE /api/v1/{gymCode}/staff/{id}`
- `GET|POST /api/v1/{gymCode}/job-roles`
- `PUT|DELETE /api/v1/{gymCode}/job-roles/{id}`
- `GET|POST /api/v1/{gymCode}/contracts`
- `PUT|DELETE /api/v1/{gymCode}/contracts/{id}`
- `GET|POST /api/v1/{gymCode}/vacations`
- `PUT|DELETE /api/v1/{gymCode}/vacations/{id}`

Facilities:
- `GET|POST /api/v1/{gymCode}/opening-hours`
- `PUT|DELETE /api/v1/{gymCode}/opening-hours/{id}`
- `GET|POST /api/v1/{gymCode}/opening-hours-exceptions`
- `PUT|DELETE /api/v1/{gymCode}/opening-hours-exceptions/{id}`
- `GET|POST /api/v1/{gymCode}/equipment-models`
- `PUT|DELETE /api/v1/{gymCode}/equipment-models/{id}`
- `GET|POST /api/v1/{gymCode}/equipment`
- `PUT|DELETE /api/v1/{gymCode}/equipment/{id}`
- `GET|POST /api/v1/{gymCode}/maintenance-tasks`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`
- `POST /api/v1/{gymCode}/maintenance-tasks/generate-due`
- `DELETE /api/v1/{gymCode}/maintenance-tasks/{id}`
- `GET /api/v1/{gymCode}/gym-settings`
- `PUT /api/v1/{gymCode}/gym-settings`
- `GET|POST /api/v1/{gymCode}/gym-users`
- `DELETE /api/v1/{gymCode}/gym-users/{appUserId}/{roleName}`

## Separate Client Contract

The React client intentionally consumes only this subset:
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- member CRUD including `GET /api/v1/{gymCode}/members/{id}` for edit forms
- `GET /api/v1/{gymCode}/members/me` for member self-booking
- training-category CRUD
- membership-package CRUD
- session list/detail through `GET /api/v1/{gymCode}/training-sessions`
- member/admin booking through `POST /api/v1/{gymCode}/bookings`
- trainer/admin attendance through `GET /api/v1/{gymCode}/bookings` and `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- caretaker/admin task updates through `GET /api/v1/{gymCode}/maintenance-tasks` and `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`

The member detail route returns a fuller payload than the member list route so the client can edit person fields without inventing a second contract.

The deployed ASP.NET Core app serves the built React client at `/client`; the client is still a separately built Vite application and consumes these REST endpoints through JWT-bearing HTTP calls.

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

Typical mappings:
- validation failures -> `400`
- forbidden access -> `403`
- missing resources -> `404`
- unexpected server failures -> `500`

## Swagger

Swagger is enabled in local development:

- `https://localhost:7245/swagger`
