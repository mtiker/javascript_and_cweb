# Architecture

## Üldpilt

Rakendus järgib layered/N-tier mustrit, sama suunda nagu näidisprojektis:

- `App.Domain`: domeeni entiteedid, enumid, rollikonstandid
- `App.DAL.EF`: EF Core `AppDbContext`, tenant query filtrid, seeding
- `App.BLL`: use-case teenused (onboarding, treatment plan decision, patient, appointment, tenant access, impersonation context)
- `App.DTO`: API sisend/väljund mudelid
- `WebApp`: API controllerid, middleware, auth setup, DI, swagger
- `WebApp.Tests`: unit + integration testid

## Kihtide piirid

- Äriloogika on BLL-is, mitte controlleris.
- Controllerid teevad request/response mapingu ja auth/route käsitluse.
- EF päringud ja persistence detailid on DAL-is.
- DTO-d ei sisalda persistence loogikat.

## Olulisemad disainiotsused (ADR-stiilis)

1. Shared schema multi-tenancy (`CompanyId`)
- Põhjus: kiire MVP, madalam operatiivkulu, selge migratsioonitee.

2. Tenant isolation DbContext query filtritega
- Põhjus: süsteemne kaitse, et vältida cross-tenant lekkeid.

3. Path-based tenant routing middlewarega
- Põhjus: vastab nõudele `/{companySlug}` ja seob tenant context'i requestiga.

4. Soft delete tenant-ärientiteetidel
- Põhjus: andmete taastatavus, auditeeritavus, vastab nõudele.

5. Audit log SaveChanges tasemel
- Põhjus: keskne muutuste logimine, sõltumata controllerist.

6. Identity + JWT
- Põhjus: rollipõhine autoriseerimine ja API-first kasutus.

7. Appointment overlap kontroll BLL-is
- Põhjus: vältida topeltbroneeringuid sama arsti või toa jaoks.

8. Impersonation ainult SystemAdmin rollile
- Põhjus: kõrgendatud turvarisk, vaja range piiranguid + auditit.

## Dependency suund

- `WebApp` -> `App.BLL`, `App.DAL.EF`, `App.DTO`, `App.Domain`
- `App.BLL` -> `App.DAL.EF`, `App.Domain`
- `App.DAL.EF` -> `App.Domain`
- `App.Domain` -> (ei sõltu rakenduse teistest kihtidest)

## Kriitilised komponendid

- Tenant resolution: `WebApp/Middleware/TenantResolutionMiddleware.cs`
- Tenant filter: `App.DAL.EF/AppDbContext.cs`
- Error handling: `WebApp/Middleware/GlobalExceptionMiddleware.cs`
- Auth setup: `WebApp/Setup/IdentitySetupExtensions.cs`
- Onboarding use-case: `App.BLL/Services/CompanyOnboardingService.cs`
- Treatment plan decision use-case: `App.BLL/Services/TreatmentPlanService.cs`
- Patient use-case: `App.BLL/Services/PatientService.cs`
- Appointment use-case: `App.BLL/Services/AppointmentService.cs`
- Impersonation context resolver: `App.BLL/Services/ImpersonationService.cs`
- Impersonation endpoint (token + audit): `WebApp/ApiControllers/System/ImpersonationController.cs`
