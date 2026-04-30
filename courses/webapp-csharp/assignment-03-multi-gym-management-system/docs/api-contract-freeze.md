# API Contract Freeze

**Version:** v1.0
**Frozen:** 2026-04-27
**Stability target:** Final1 and Final2 submissions

Any change to the contracts below is a breaking change. The React client (`client/`) and all integration tests (`tests/WebApp.Tests/Integration/`) depend on these exact shapes.

---

## Identity contracts

### POST `/api/v1/account/login`

Request:
```json
{ "email": "string", "password": "string" }
```

Response `200 JwtResponse`:
```json
{
  "jwt": "string",
  "refreshToken": "string",
  "activeGymId": "guid | null",
  "activeGymCode": "string | null",
  "activeRole": "string | null",
  "systemRoles": ["string"]
}
```

### POST `/api/v1/account/register`

Request:
```json
{ "email": "string", "password": "string", "displayName": "string" }
```

Response `200 JwtResponse`: same shape as login.

### POST `/api/v1/account/renew-refresh-token`

Request:
```json
{ "jwt": "string", "refreshToken": "string" }
```

Response `200 JwtResponse`: same shape.
`403 Forbidden` if refresh token is expired or already rotated.

### POST `/api/v1/account/logout`

Requires: `Authorization: Bearer <jwt>`
Response `200 { "message": "string" }`.

### POST `/api/v1/account/switch-gym`

Requires: `Authorization: Bearer <jwt>`
Request: `{ "gymCode": "string", "role": "string" }`
Response `200 JwtResponse`.

### POST `/api/v1/account/switch-role`

Requires: `Authorization: Bearer <jwt>`
Request: `{ "role": "string" }`
Response `200 JwtResponse`.

---

## Tenant — Members

Base: `/api/v1/{gymCode}/members`
Auth: `Authorization: Bearer <jwt>` (active gym must match `{gymCode}`)

### GET `/api/v1/{gymCode}/members`

Response `200 MemberResponse[]`:
```json
[
  {
    "id": "guid",
    "memberCode": "string",
    "fullName": "string",
    "status": 0
  }
]
```

### GET `/api/v1/{gymCode}/members/me`

Response `200 MemberDetailResponse`:
```json
{
  "id": "guid",
  "memberCode": "string",
  "firstName": "string",
  "lastName": "string",
  "fullName": "string",
  "personalCode": "string",
  "dateOfBirth": "date | null",
  "status": 0
}
```

### GET `/api/v1/{gymCode}/members/{id}`

Response `200 MemberDetailResponse` (same as `/me`).
`403` if caller is a Member and `id` is not their own record.
`404` if not found.

### POST `/api/v1/{gymCode}/members`

Request `MemberUpsertRequest`:
```json
{
  "firstName": "string",
  "lastName": "string",
  "memberCode": "string",
  "personalCode": "string | null",
  "dateOfBirth": "date | null"
}
```

Response `201 MemberDetailResponse`. `Location` header set to `GET /members/{id}`.

### PUT `/api/v1/{gymCode}/members/{id}`

Request: same as POST.
Response `200 MemberDetailResponse`.

### DELETE `/api/v1/{gymCode}/members/{id}`

Response `204 No Content`.

---

## Tenant — Training Categories

Base: `/api/v1/{gymCode}/training-categories`

### GET

Response `200 TrainingCategoryResponse[]`:
```json
[{ "id": "guid", "name": "string", "description": "string | null" }]
```

### POST

Request: `{ "name": "string", "description": "string | null" }`
Response `201 TrainingCategoryResponse`.

### PUT `/{id}`

Request: same as POST.
Response `200 TrainingCategoryResponse`.

### DELETE `/{id}`

Response `204 No Content`.

---

## Tenant — Membership Packages

Base: `/api/v1/{gymCode}/membership-packages`

### GET

Response `200 MembershipPackageResponse[]`:
```json
[
  {
    "id": "guid",
    "name": "string",
    "packageType": 0,
    "durationValue": 0,
    "durationUnit": 0,
    "basePrice": 0.0,
    "currencyCode": "string",
    "trainingDiscountPercent": 0.0,
    "isTrainingFree": false,
    "description": "string | null"
  }
]
```

### POST

Request matches response shape (minus `id`).
Response `201 MembershipPackageResponse`.

### PUT `/{id}`

Request: same as POST.
Response `200 MembershipPackageResponse`.

### DELETE `/{id}`

Response `204 No Content`.

---

## Tenant — Training Sessions

Base: `/api/v1/{gymCode}/training-sessions`

### GET

Response `200 TrainingSessionResponse[]`:
```json
[
  {
    "id": "guid",
    "categoryId": "guid",
    "name": "string",
    "description": "string | null",
    "startAtUtc": "datetime",
    "endAtUtc": "datetime",
    "capacity": 0,
    "basePrice": 0.0,
    "currencyCode": "string",
    "status": 0
  }
]
```

### GET `/{id}` — `200 TrainingSessionResponse`

### POST — `201 TrainingSessionResponse`

### PUT `/{id}` — `200 TrainingSessionResponse`

### DELETE `/{id}` — `204 No Content`

---

## Tenant — Bookings

Base: `/api/v1/{gymCode}/bookings`

### GET

Response `200 BookingResponse[]`:
```json
[
  {
    "id": "guid",
    "trainingSessionId": "guid",
    "trainingSessionName": "string",
    "memberId": "guid",
    "memberName": "string",
    "memberCode": "string",
    "status": 0,
    "chargedPrice": 0.0,
    "paymentRequired": false
  }
]
```

### POST — `201 BookingResponse`

Request: `{ "trainingSessionId": "guid", "memberId": "guid", "paymentReference": "string | null" }`

### PUT `/{id}/attendance` — `200 BookingResponse`

Request: `{ "status": 0 }` (BookingStatus enum)

### DELETE `/{id}` — `204 No Content`

---

## System — Platform

Base: `/api/v1/system`
Auth: `Authorization: Bearer <jwt>` with role `SystemAdmin`, `SystemSupport`, or `SystemBilling`

### GET `/api/v1/system/gyms` — `200 GymSummaryResponse[]`

### POST `/api/v1/system/gyms` — `201 RegisterGymResponse` (SystemAdmin only)

### PUT `/api/v1/system/gyms/{gymId}/activation` — `200 Message` (SystemAdmin only)

### GET `/api/v1/system/gyms/{gymId}/snapshot` — `200 CompanySnapshotResponse` (SystemAdmin | SystemSupport)

### GET `/api/v1/system/platform/analytics` — `200 PlatformAnalyticsResponse`

### POST `/api/v1/system/impersonation` — `200 StartImpersonationResponse` (SystemAdmin only)

### GET `/api/v1/system/subscriptions` — subscription data

### PUT `/api/v1/system/subscriptions/{gymId}` — `200 Message` (SystemBilling only)

### GET `/api/v1/system/support` — support ticket list

### POST `/api/v1/system/support/{gymId}/tickets` — `201 SupportTicket`

---

## Error response shape (ProblemDetails — RFC 7807)

All API errors return `application/problem+json`:

```json
{
  "type": "string | null",
  "title": "string",
  "status": 0,
  "detail": "string | null",
  "instance": "string | null"
}
```

This shape is contract-frozen. The React client reads `detail` to display user-facing error messages (confirmed by `CrudPages.test.tsx` — checks `detail` field text).

---

## Stability notes

- All routes are prefixed `/api/v{version:apiVersion}/`. Current version is `1.0`.
- `{gymCode}` is a slug (lowercase, hyphenated, e.g., `peak-forge`).
- `{id}` is always a UUID (`Guid`).
- All datetimes are UTC ISO-8601.
- Status/enum fields are integer-encoded in JSON.
- `LangStr` fields (e.g., training category `name`) are returned as a translated `string` in the `Accept-Language` locale, not as an object.

---

## What must NOT change before Final2

1. The `JwtResponse` shape — auth.tsx and the MVC cookie auth both parse it.
2. The `MemberResponse` / `MemberDetailResponse` split — `CrudPages.test.tsx` mocks both.
3. The `detail` field in ProblemDetails — tests and React error display depend on it.
4. Route template `/api/v1/{gymCode}/members` — integration tests hard-code gym codes `peak-forge` and `north-star`.
5. The `/api/v1/account/renew-refresh-token` endpoint and its 403-on-reuse behaviour — tested in `AuthSecurityAndErrorTests`.
6. The 201 + `Location` header pattern for all create endpoints — tests call `CreatedAtAction`.
