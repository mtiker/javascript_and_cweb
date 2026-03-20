# API

Base path: `/api/v1`

## Üldreeglid

- identity route'id kasutavad mustrit `/api/v1/account/{action}`
- system route'id kasutavad mustrit `/api/v1/system/...`
- tenant route'id kasutavad mustrit `/api/v1/{companySlug}/...`
- tenant endpointidel peab route'is olev `companySlug` klappima aktiivse tenant-kontekstiga
- vead tagastatakse kas `ProblemDetails` või lihtsa `Message` payloadina sõltuvalt controllerist

## Auth / Identity

### Avalikud endpointid

- `POST /api/v1/account/register`
- `POST /api/v1/account/login`
- `POST /api/v1/account/forgotpassword`
- `POST /api/v1/account/resetpassword`
- `POST /api/v1/account/renewrefreshtoken`

### JWT-ga kaitstud endpointid

- `POST /api/v1/account/switchcompany`
- `POST /api/v1/account/switchrole`
- `POST /api/v1/account/logout`

## System API

### Onboarding

Auth: `SystemAdmin`, `SystemSupport`

- `POST /api/v1/system/onboarding/registercompany`
- `GET /api/v1/system/onboarding/companies`

### Impersonation

Auth: `SystemAdmin`

- `POST /api/v1/system/impersonation/start`

### Platform

Auth: `SystemAdmin`

- `GET /api/v1/system/platform/analytics`
- `GET /api/v1/system/platform/featureflags`
- `PUT /api/v1/system/platform/featureflags`
- `PUT /api/v1/system/platform/companies/{companyId}/activation`

### Support

Auth: `SystemAdmin`, `SystemSupport`

- `GET /api/v1/system/support/companies`
- `GET /api/v1/system/support/companies/{companySlug}`
- `GET /api/v1/system/support/tickets`
- `POST /api/v1/system/support/tickets`

### Billing

Auth: `SystemAdmin`, `SystemBilling`

- `GET /api/v1/system/billing/subscriptions`
- `PUT /api/v1/system/billing/subscriptions/{subscriptionId}`
- `GET /api/v1/system/billing/invoices`
- `PUT /api/v1/system/billing/invoices/{invoiceId}/status`

## Tenant API

### Patients

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

- `GET /api/v1/{companySlug}/patients`
- `GET /api/v1/{companySlug}/patients/{patientId}`
- `GET /api/v1/{companySlug}/patients/{patientId}/profile`
- `POST /api/v1/{companySlug}/patients`
- `PUT /api/v1/{companySlug}/patients/{patientId}`
- `DELETE /api/v1/{companySlug}/patients/{patientId}`

### Appointments

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

- `GET /api/v1/{companySlug}/appointments`
- `POST /api/v1/{companySlug}/appointments`
- `POST /api/v1/{companySlug}/appointments/{appointmentId}/clinical-record`

`POST /appointments` valideerib vähemalt:

- `startAtUtc < endAtUtc`
- sama arsti aja kattuvus
- sama ruumi aja kattuvus

### Treatment plans

Read auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Write auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/treatmentplans`
- `GET /api/v1/{companySlug}/treatmentplans/{planId}`
- `POST /api/v1/{companySlug}/treatmentplans`
- `PUT /api/v1/{companySlug}/treatmentplans/{planId}`
- `POST /api/v1/{companySlug}/treatmentplans/{planId}/submit`
- `DELETE /api/v1/{companySlug}/treatmentplans/{planId}`
- `GET /api/v1/{companySlug}/treatmentplans/openitems`
- `POST /api/v1/{companySlug}/treatmentplans/recorditemdecision`

### Finance workspace

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

- `GET /api/v1/{companySlug}/finance/workspace/{patientId}`

### Cost estimates

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/costestimates`
- `POST /api/v1/{companySlug}/costestimates`
- `GET /api/v1/{companySlug}/costestimates/{costEstimateId}/legal`

### Invoices

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/invoices`
- `GET /api/v1/{companySlug}/invoices/{invoiceId}`
- `POST /api/v1/{companySlug}/invoices`
- `POST /api/v1/{companySlug}/invoices/generate-from-procedures`
- `POST /api/v1/{companySlug}/invoices/{invoiceId}/payments`
- `PUT /api/v1/{companySlug}/invoices/{invoiceId}`
- `DELETE /api/v1/{companySlug}/invoices/{invoiceId}`

### Payment plans

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/paymentplans`
- `GET /api/v1/{companySlug}/paymentplans/{paymentPlanId}`
- `POST /api/v1/{companySlug}/paymentplans`
- `PUT /api/v1/{companySlug}/paymentplans/{paymentPlanId}`
- `DELETE /api/v1/{companySlug}/paymentplans/{paymentPlanId}`

### Dentists

Read auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Write auth: `CompanyOwner`, `CompanyAdmin`

- `GET /api/v1/{companySlug}/dentists`
- `POST /api/v1/{companySlug}/dentists`
- `PUT /api/v1/{companySlug}/dentists/{dentistId}`
- `DELETE /api/v1/{companySlug}/dentists/{dentistId}`

### Treatment rooms

Read auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Write auth: `CompanyOwner`, `CompanyAdmin`

- `GET /api/v1/{companySlug}/treatmentrooms`
- `POST /api/v1/{companySlug}/treatmentrooms`
- `PUT /api/v1/{companySlug}/treatmentrooms/{roomId}`
- `DELETE /api/v1/{companySlug}/treatmentrooms/{roomId}`

### Treatment types

Read auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Write auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/treatmenttypes`
- `POST /api/v1/{companySlug}/treatmenttypes`
- `PUT /api/v1/{companySlug}/treatmenttypes/{treatmentTypeId}`
- `DELETE /api/v1/{companySlug}/treatmenttypes/{treatmentTypeId}`

### Tooth records

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

- `GET /api/v1/{companySlug}/toothrecords`
- `POST /api/v1/{companySlug}/toothrecords`
- `DELETE /api/v1/{companySlug}/toothrecords/{toothRecordId}`

### X-rays

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

- `GET /api/v1/{companySlug}/xrays`
- `POST /api/v1/{companySlug}/xrays`
- `DELETE /api/v1/{companySlug}/xrays/{xrayId}`

### Insurance plans

Read auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Write auth: `CompanyOwner`, `CompanyAdmin`

- `GET /api/v1/{companySlug}/insuranceplans`
- `POST /api/v1/{companySlug}/insuranceplans`
- `PUT /api/v1/{companySlug}/insuranceplans/{insurancePlanId}`
- `DELETE /api/v1/{companySlug}/insuranceplans/{insurancePlanId}`

### Patient insurance policies

Auth: `CompanyOwner`, `CompanyAdmin`, `CompanyManager`

- `GET /api/v1/{companySlug}/patientinsurancepolicies`
- `GET /api/v1/{companySlug}/patientinsurancepolicies/{policyId}`
- `POST /api/v1/{companySlug}/patientinsurancepolicies`
- `PUT /api/v1/{companySlug}/patientinsurancepolicies/{policyId}`
- `DELETE /api/v1/{companySlug}/patientinsurancepolicies/{policyId}`

### Company users

Auth: `CompanyOwner`, `CompanyAdmin`

- `GET /api/v1/{companySlug}/companyusers`
- `POST /api/v1/{companySlug}/companyusers`

### Company settings

Auth: `CompanyOwner`

- `GET /api/v1/{companySlug}/companysettings`
- `PUT /api/v1/{companySlug}/companysettings`

### Subscription

Read auth: `CompanyOwner`, `CompanyAdmin`

Write auth: `CompanyOwner`

- `GET /api/v1/{companySlug}/subscription`
- `PUT /api/v1/{companySlug}/subscription`

## Error responses

Kõrgema taseme teenuse- ja middleware vead tulevad üldjuhul `ProblemDetails` kujul:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "Company slug is already in use.",
  "traceId": "00-..."
}
```

Osad controllerid tagastavad lihtsustatud vastuse:

```json
{
  "message": "Company not found."
}
```
