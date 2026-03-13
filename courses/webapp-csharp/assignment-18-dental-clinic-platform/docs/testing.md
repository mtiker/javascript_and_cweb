# Testing

## Testide tüübid

- Unit testid
  - `UnitTestTreatmentPlanService`
  - `UnitTestTenantAccessService`
  - `UnitTestAppointmentService`
- Integration testid
  - `IntegrationTestIdentity`
  - `IntegrationTestOnboarding`
  - `IntegrationTestTenantOperations`
  - `IntegrationTestImpersonation`

## Käivitamine

Lahenduse kaustast:

```powershell
dotnet test dental-clinic-platform.slnx
```

## Viimane tulemus

- `Passed: 8`
- `Failed: 0`
- `Skipped: 0`

## Mis on kaetud

- treatment plan decision äriloogika
- tenant role check (forbidden case)
- appointment overlap valideerimine (unit)
- account register/login flow (HTTP kaudu)
- onboarding flow (HTTP kaudu) + andmete püsivus
- tenant patient CRUD flow (integration)
- tenant appointment create/list flow (integration)
- system admin impersonation flow + audit kirje (integration)

## Mis ei ole veel kaetud

- refresh/logout negatiivsed servajuhud
- kõikide dental-entiteetide CRUD vood
- keerukad insurance/payment plan stsenaariumid
- migration smoke test päris PostgreSQL vastu CI-s

## Miks

Praegune testikiht katab kriitilise vertical slice'i (auth + tenant + patient + appointment + treatment-plan decision + impersonation).
