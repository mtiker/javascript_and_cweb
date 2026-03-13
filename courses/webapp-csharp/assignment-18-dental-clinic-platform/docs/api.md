# API

Base path: `/api/v1`

## Auth / Identity

### POST `/api/v1/account/register`

Request:

```json
{
  "email": "user@example.com",
  "password": "Strong.Pass.123!"
}
```

Response `200`:

```json
{
  "jwt": "...",
  "refreshToken": "...",
  "expiresInSeconds": 1200,
  "activeCompanyId": null,
  "activeCompanySlug": null,
  "activeCompanyRole": null
}
```

### POST `/api/v1/account/login`

Sama request kuju nagu register.
Tagastab JWT + refresh tokeni ja kui olemas, aktiivse company konteksti.

### POST `/api/v1/account/renewrefreshtoken`

Request:

```json
{
  "jwt": "expired-or-near-expired-jwt",
  "refreshToken": "refresh-token"
}
```

### POST `/api/v1/account/logout`

Auth: Bearer

Request:

```json
{
  "jwt": "current-jwt",
  "refreshToken": "refresh-token"
}
```

### POST `/api/v1/account/switchcompany`

Auth: Bearer

Request:

```json
{
  "companySlug": "acme"
}
```

Tagastab uue JWT, kus company claimid viitavad valitud tenantile.

## System

### POST `/api/v1/system/onboarding/registercompany`

Auth: `SystemAdmin` või `SystemSupport`

Security note: endpoint on praegu role-protected. Kui tenant provisioning peab olema ainult tsentraalne backoffice tegevus, ahenda lubatud rollid `SystemAdmin`-iks või liiguta flow invite/approval workflow taha.

Request:

```json
{
  "companyName": "Acme Dental",
  "companySlug": "acme",
  "ownerEmail": "owner@acme.test",
  "ownerPassword": "Strong.Pass.123!",
  "countryCode": "DE"
}
```

Response `200`:

```json
{
  "companyId": "...",
  "ownerUserId": "...",
  "companySlug": "acme",
  "subscriptionTier": "Free"
}
```

### GET `/api/v1/system/onboarding/companies`

Auth: `SystemAdmin` või `SystemSupport`

Tagastab ettevõtete loendi.

### POST `/api/v1/system/impersonation/start`

Auth: `SystemAdmin`

Request:

```json
{
  "targetUserEmail": "user@tenant.test",
  "companySlug": "acme",
  "reason": "Support session for appointment troubleshooting"
}
```

Response `200`:

```json
{
  "jwt": "...",
  "refreshToken": "...",
  "expiresInSeconds": 1200,
  "activeCompanyId": "...",
  "activeCompanySlug": "acme",
  "activeCompanyRole": "CompanyEmployee",
  "impersonatedByUserId": "...",
  "impersonationReason": "Support session for appointment troubleshooting",
  "targetUserId": "...",
  "targetUserEmail": "user@tenant.test"
}
```

JWT sisaldab lisaclaime:

- `isImpersonated=true`
- `impersonatedByUserId`
- `impersonationReason`

## Tenant

### POST `/api/v1/{companySlug}/treatmentplans/recorditemdecision`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager`

Request:

```json
{
  "planId": "...",
  "planItemId": "...",
  "decision": "Accepted",
  "notes": "urgent accepted"
}
```

`decision` väärtused: `Pending`, `Accepted`, `Deferred`, `Rejected` (case-insensitive).

Response `200`:

```json
{
  "planId": "...",
  "planItemId": "...",
  "planStatus": "PartiallyAccepted",
  "itemDecision": "Accepted"
}
```

### GET `/api/v1/{companySlug}/patients`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Tagastab aktiivsete (mitte soft-deleted) patsientide loendi.

### GET `/api/v1/{companySlug}/patients/{patientId}`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Tagastab ühe patsiendi detaili.

### POST `/api/v1/{companySlug}/patients`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Request:

```json
{
  "firstName": "Marta",
  "lastName": "Kask",
  "dateOfBirth": "1992-02-12",
  "personalCode": "4900212XXXX",
  "email": "marta@example.com",
  "phone": "+3725551234"
}
```

Response `201`:

```json
{
  "id": "...",
  "firstName": "Marta",
  "lastName": "Kask",
  "dateOfBirth": "1992-02-12",
  "personalCode": "4900212XXXX",
  "email": "marta@example.com",
  "phone": "+3725551234"
}
```

### PUT `/api/v1/{companySlug}/patients/{patientId}`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Sama payload-kuju nagu create.

### DELETE `/api/v1/{companySlug}/patients/{patientId}`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Teostab soft delete.

### GET `/api/v1/{companySlug}/appointments`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Tagastab appointmentide loendi.

### POST `/api/v1/{companySlug}/appointments`

Auth: `CompanyOwner|CompanyAdmin|CompanyManager|CompanyEmployee`

Request:

```json
{
  "patientId": "...",
  "dentistId": "...",
  "treatmentRoomId": "...",
  "startAtUtc": "2026-03-05T10:00:00Z",
  "endAtUtc": "2026-03-05T10:30:00Z",
  "notes": "Initial check"
}
```

Valideerimine:

- `startAtUtc < endAtUtc`
- sama arsti ajavahemik ei tohi kattuda
- sama ruumi ajavahemik ei tohi kattuda

## Error responses

Vigade korral tagastatakse `ProblemDetails` (`application/problem+json`), sh:

- `status`
- `title`
- `detail`
- `traceId`
