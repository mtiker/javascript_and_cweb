# Testing

## Testide tüübid

### Integration testid

- `IntegrationTestIdentity`
- `IntegrationTestOnboarding`
- `IntegrationTestTenantOperations`
- `IntegrationTestImpersonation`
- `IntegrationTestDeployment`

### Unit testid

- `UnitTestTenantAccessService`
- `UnitTestPatientService`
- `UnitTestAppointmentService`
- `UnitTestTreatmentPlanService`
- `UnitTestFinanceServices`
- `UnitTestIdentitySeed`
- `UnitTestTenantApiServiceControllers`
- `UnitTestTenantApiDbControllers`

## Testide maht

Praeguses repos on 61 testijuhtu (`[Fact]`), mis katavad nii teenusekihi kui ka HTTP/controllerite taseme.

## Käivitamine

Lahenduse kaustast:

```powershell
dotnet test dental-clinic-platform.slnx
```

## Mida testid katavad

- account register/login/forgot-password HTTP vood
- `/health` deployment smoke endpoint
- seeditud identity kasutajate rolli- ja parooli-sünkroniseerimine
- onboarding flow ja loodud tenant-andmed
- tenant patient CRUD integration flow
- tenant appointment create/list flow
- impersonation flow ja audit log kirje
- tenant role check ja forbidden juht
- patsiendi loomise, uuendamise, profiili ja limitite reeglid
- appointment overlap ja clinical-record valideerimine
- treatment plan CRUD/workflow reeglid
- finance arvutus- ja workflow loogika
- tenant controllerite mapping, auth guardid ja vastused

## Mida ei kata täielikult

- kõikide endpointide täis integration suite
- production PostgreSQL smoke test CI-s
- UI/browser automation
- väliste integratsioonide laadne käitumine, sest neid selles assignmentis sisuliselt ei ole

## Testimisstrateegia

- keerukamad ärireeglid on valdavalt unit-testitud teenusekihis
- kriitilised otsast-lõpuni vood on integration-testitud `WebApplicationFactory` abil
- osa lihtsamatest DbContext/controller voogudest on kaetud controlleri taseme unit-testidega
