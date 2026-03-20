# Architecture

## Üldpilt

Rakendus järgib layered/N-tier lähenemist, kuid mitte täiesti "puhtal" kujul. Keerukamad use-case'id on BLL teenustes, lihtsamad CRUD controllerid kasutavad kohati `AppDbContext`-i otse.

- `App.Domain`: domeeni entiteedid, enumid, rollikonstandid
- `App.DAL.EF`: EF Core `AppDbContext`, tenant query filtrid, migratsioonid, seeding
- `App.BLL`: keerukamad use-case teenused
- `App.DTO`: API sisend/väljund mudelid
- `WebApp`: API controllerid, middleware, auth setup, DI, swagger ja staatiline UI `wwwroot` all
- `WebApp.Tests`: unit + integration testid

## Kihtide piirid

- keerukam äriloogika elab BLL-is
- osa lihtsamaid CRUD vooge on controller + `AppDbContext` põhised
- controllerid vastutavad authi, route'i, request/response mappingu ja kerge valideerimise eest
- tenant isolatsioon ei ole ainult controllerites, vaid peamiselt `AppDbContext` query filtrites
- DTO-d ei sisalda persistence loogikat

## Olulisemad disainiotsused

1. Shared schema multi-tenancy (`CompanyId`)
- Põhjus: assignmenti mahu jaoks lihtne hallata ja laiendada.

2. Tenant isolation DbContext query filtritega
- Põhjus: vähendab cross-tenant lekete riski ka siis, kui controlleris või teenuses midagi ununeb.

3. Path-based tenant routing middlewarega
- Põhjus: tenant kontekst seotakse URL-ist loetava `companySlug`-iga.

4. Soft delete tenant-ärientiteetidel
- Põhjus: taastatavus, audit ja ohutum kustutamine.

5. Audit log `SaveChangesAsync` tasemel
- Põhjus: keskne jälg nii teenuse- kui ka controlleripõhiste muudatuste jaoks.

6. Identity + JWT
- Põhjus: rollipõhine autoriseerimine ja mitme tenant-membershipi tugi.

7. Appointment scheduling ja clinical-record workflow BLL-is
- Põhjus: konfliktikontroll, hambakaardi uuendus ja treatmentite loomine on seotud ärireeglid.

8. Treatment plan ja finance workflow teenustesse
- Põhjus: plaanid, hinnangud, arved, maksed ja payment planid vajavad koondatud reegleid.

9. Impersonation ainult `SystemAdmin` rollile
- Põhjus: kõrgendatud turvarisk, vaja põhjendust, claim'e ja auditit.

10. Staatiline UI `wwwroot` all
- Põhjus: assignmenti demo ja käsitsi testimine on kohe brauseris kasutatav.

## Dependency suund

- `WebApp` -> `App.BLL`, `App.DAL.EF`, `App.DTO`, `App.Domain`
- `App.BLL` -> `App.DAL.EF`, `App.Domain`
- `App.DAL.EF` -> `App.Domain`
- `App.Domain` -> (ei sõltu rakenduse teistest kihtidest)

## Requesti voog

1. Request jõuab `TenantResolutionMiddleware`-i.
2. Middleware leiab `companySlug` põhjal aktiivse tenanti ja täidab `ITenantProvider` konteksti.
3. Controller kontrollib authi ja route'i.
4. Keerukamates voogudes kutsub controller BLL teenust; lihtsamates voogudes teeb otse `AppDbContext` päringu.
5. `AppDbContext` rakendab tenant filtreid, auditit ja soft delete'i.
6. Vea korral vormistab `GlobalExceptionMiddleware` vastuse `ProblemDetails`-ina.

## Kriitilised komponendid

- UI shell + route fallback: `WebApp/wwwroot/index.html`, `WebApp/wwwroot/js/*.js`
- tenant resolution: `WebApp/Middleware/TenantResolutionMiddleware.cs`
- tenant filter ja audit: `App.DAL.EF/AppDbContext.cs`
- error handling: `WebApp/Middleware/GlobalExceptionMiddleware.cs`
- auth setup: `WebApp/Setup/IdentitySetupExtensions.cs`
- onboarding: `App.BLL/Services/CompanyOnboardingService.cs`
- patients: `App.BLL/Services/PatientService.cs`
- appointments: `App.BLL/Services/AppointmentService.cs`
- treatment plans: `App.BLL/Services/TreatmentPlanService.cs`
- finance: `App.BLL/Services/CostEstimateService.cs`, `InvoiceService.cs`, `PaymentPlanService.cs`, `FinanceWorkspaceService.cs`
- impersonation: `App.BLL/Services/ImpersonationService.cs`

## Arhitektuuri aus hinnang

See projekt ei suru kogu loogikat vägisi BLL-i. Praegune seis on praktiline hybrid:

- kõrgema riskiga ja mitut entiteeti siduvad vood on BLL teenustes
- lihtsam CRUD on kohati controllerites

Assignmenti jaoks on see mõistlik kompromiss, sest tenant isolatsioon ja turvakriitilised reeglid on siiski tsentraalselt kaitstud.
