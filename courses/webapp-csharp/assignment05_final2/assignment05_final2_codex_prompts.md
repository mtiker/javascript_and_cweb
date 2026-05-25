# assignment05_final2_codex_prompts.txt

Project context:
- Existing Final1 folder/repo is: assignment-03-multi-gym-management-system
- New Final2 folder must be named: assignment05_final2
- assignment05_final2 must be created at the same directory level as assignment-03-multi-gym-management-system and assignment-04...
- Do NOT modify Final1 directly.
- All work must happen inside assignment05_final2 after the initial copy.
- Final2 goal: evolve the existing Multi-Gym Management System into a modular monolith for the course Final2 assignment.
- Preserve existing API routes and React client compatibility unless a breaking change is explicitly required and handled in the same phase.
- Run tests after every phase.

Recommended target modules:
- Modules.Users
- Modules.Gyms
- Modules.Memberships
- Modules.Training
- Modules.Maintenance

Core rules:
- WebApp may reference modules.
- Modules may reference Shared.Contracts, SharedKernel, and App.Resources.
- Modules must NOT reference each other.
- Cross-module communication must use mediator, shared contracts, module API abstractions, or events.
- Do not use another module's DbContext, repository, entity, or internal service.
- Keep public DTOs separate from domain entities.
- Keep controllers thin.
- Keep Admin MVC strongly typed with ViewModels only.
- No ViewBag.
- No ViewData.
- Preserve JWT and refresh-token authentication.
- Preserve .resx UI translations.
- Preserve DB LangStr translations.
- Preserve IDOR and tenant isolation checks.

============================================================
PHASE 0 PROMPT: COPY FINAL1 TO assignment05_final2
============================================================

Create a Final2 copy from the existing Final1 Multi-Gym Management System.

Source folder/repo:
assignment-03-multi-gym-management-system

Target folder/repo:
assignment05_final2

Important constraints:
- assignment05_final2 must be created at the same directory level as assignment-03-multi-gym-management-system and assignment-04.
- Do not modify assignment-03-multi-gym-management-system.
- Do not change runtime behavior.
- Do not refactor architecture yet.
- Do not add modules yet.
- Only update repository/readme naming if needed.
- Keep existing project names initially unless a name change is trivial and safe.

Implementation tasks:
1. Copy the complete Final1 project into assignment05_final2.
2. Confirm that Final1 remains untouched.
3. Update README title/description to say this is Final2.
4. Keep all existing backend, frontend, test, Docker, and CI/CD files.
5. Run backend build and tests.
6. Run client tests and build.
7. Stop after the baseline copy is green.

Validation commands:
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
cd client
npm test
npm run build

Done when:
- assignment05_final2 exists.
- Final1 is unchanged.
- Backend builds.
- Backend tests pass.
- Client tests pass.
- Client build passes.

============================================================
PHASE 1 PROMPT: BASELINE INVENTORY AND ARCHITECTURE SAFETY NET
============================================================

Add a Final2 architecture inventory without moving code.

Context:
The current Final1 code is a layered/Clean-Onion style app with projects such as App.Domain, App.BLL, App.BLL.Contracts, App.DAL.EF, App.DAL.Contracts, App.DTO, App.Resources, WebApp, WebApp.Tests, and client.
Final2 must become a modular monolith.

Tasks:
1. Create docs/final2-module-map.md.
2. Map existing entities, BLL services, DAL repositories, API controllers, MVC Admin pages, MVC Client pages, and React routes to target modules:
   - Users
   - Gyms
   - Memberships
   - Training
   - Maintenance
3. Add an Architecture.Tests project if one does not already exist.
4. Prepare architecture tests to enforce that future Modules.* projects cannot reference each other.
5. Initially allow legacy App.* references because code has not yet moved.
6. Do not change runtime behavior.

Expected entity ownership:
Users:
- AppUser
- AppRole
- AppRefreshToken

Gyms:
- Gym
- GymSettings
- GymContact
- Contact
- AppUserGymRole
- tenant/access resolution
- platform/system gym management

Memberships:
- Person
- PersonContact
- Member
- Membership
- MembershipPackage
- Payment
- member workspace

Training:
- Staff
- TrainingCategory
- TrainingSession
- Booking
- attendance/session workflow

Maintenance:
- EquipmentModel
- Equipment
- MaintenanceTask
- recurring due task generation

Validation commands:
dotnet test multi-gym-management-system.slnx

Done when:
- docs/final2-module-map.md exists.
- Architecture.Tests exists and runs.
- Existing behavior is unchanged.

============================================================
PHASE 2 PROMPT: CREATE MODULE SHELLS
============================================================

Create empty module shell projects for the modular monolith.

Create these projects/folders inside assignment05_final2:
- Modules.Users
- Modules.Gyms
- Modules.Memberships
- Modules.Training
- Modules.Maintenance
- Shared.Contracts
- SharedKernel

Each module should have this internal structure:
- Api
- Application
- Domain
- Infrastructure

Tasks:
1. Add module projects to the solution.
2. Add Shared.Contracts and SharedKernel projects to the solution.
3. Add module registration extension methods:
   - AddUsersModule
   - AddGymsModule
   - AddMembershipsModule
   - AddTrainingModule
   - AddMaintenanceModule
4. Call these extension methods from WebApp startup/setup.
5. Do not move domain logic yet.
6. Do not change API routes.
7. Add or update architecture tests:
   - WebApp may reference all modules.
   - Modules may reference Shared.Contracts, SharedKernel, and App.Resources.
   - Modules must not reference each other.

Validation commands:
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- Module shell projects exist.
- WebApp composes modules.
- Architecture tests pass.

============================================================
PHASE 3 PROMPT: ADD MEDIATOR FOUNDATION
============================================================

Add mediator support for module communication.

Tasks:
1. Add MediatR/Mediator support according to the project's package style.
2. Add a marker class per module for assembly scanning.
3. Register handlers from each module through module registration methods.
4. Use IRequest<T> for commands and queries.
5. Use INotification for cross-module domain/application events.
6. Add one small sample event and handler to prove mediator registration works.
7. Do not rewrite all existing BLL services yet.
8. Do not introduce direct module references.
9. Do not send another module's internal request type directly.
10. Cross-module request/response must go through Shared.Contracts abstractions.

Validation commands:
dotnet test multi-gym-management-system.slnx

Done when:
- Mediator is registered.
- A handler from a module can be resolved.
- Sample event dispatch works.
- Architecture tests still pass.

============================================================
PHASE 4 PROMPT: EXTRACT USERS MODULE
============================================================

Extract the Users module from the copied Final2 code.

Move ownership of these concepts into Modules.Users:
- AppUser
- AppRole
- AppRefreshToken
- account authentication
- JWT generation/validation support
- refresh token management
- current user identity lookup

Constraints:
- Keep existing /api/v1/account routes stable.
- Keep public DTO contracts stable unless absolutely necessary.
- Do not reference Gyms, Memberships, Training, or Maintenance modules from Users.
- Expose only Shared.Contracts and IUsersModuleApi outside the module.
- Do not return EF/domain entities from API endpoints.

Tasks:
1. Move or wrap identity domain classes in Modules.Users/Domain.
2. Move/wrap authentication logic in Modules.Users/Application.
3. Move refresh-token persistence into Modules.Users/Infrastructure.
4. Move or delegate account controller implementation into Modules.Users/Api.
5. Add Shared.Contracts/ModuleApis/IUsersModuleApi.cs.
6. Account controller should delegate to mediator or Users module application service.
7. Keep register/login/logout/renew-refresh-token behavior working.
8. Keep Swagger JWT auth working.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Account
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- Login works.
- Logout works.
- Refresh token works.
- Users module owns identity/auth.
- Architecture tests pass.

============================================================
PHASE 5 PROMPT: EXTRACT GYMS AND TENANCY MODULE
============================================================

Extract the Gyms/Tenancy module.

Move ownership of these concepts into Modules.Gyms:
- Gym
- GymSettings
- GymContact
- Contact
- AppUserGymRole
- tenant route resolution
- gym access checks
- switch-gym
- switch-role
- system/platform gym APIs
- platform analytics

Constraints:
- Keep public API routes unchanged.
- Do not reference Users, Memberships, Training, or Maintenance modules from Gyms.
- Users are referenced by UserId, not by direct AppUser navigation where possible.
- Other modules must not use Gyms DbContext or repositories directly.
- Expose gym access checks through IGymsModuleApi in Shared.Contracts.

Tasks:
1. Move gym-related entities/configurations into Modules.Gyms.
2. Move tenant resolution logic into Modules.Gyms/Application or Infrastructure as appropriate.
3. Move system gym API implementation into Modules.Gyms/Api.
4. Add Shared.Contracts/ModuleApis/IGymsModuleApi.cs.
5. Implement ResolveAccessAsync or equivalent access resolution method.
6. Update legacy services/controllers to use mediator/IGymsModuleApi instead of direct gym repository access where possible.
7. Preserve switch-gym and switch-role behavior.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Gym
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- Gym access works.
- Switch gym works.
- Switch role works.
- System gym APIs work.
- Tenant isolation tests pass.
- Architecture tests pass.

============================================================
PHASE 6 PROMPT: EXTRACT MEMBERSHIPS MODULE
============================================================

Extract the Memberships module as a vertical slice.

Move ownership of these concepts into Modules.Memberships:
- Person
- PersonContact
- Member
- Membership
- MembershipPackage
- Payment
- member workspace
- member CRUD
- membership package CRUD
- membership status changes
- payment-related membership logic

Constraints:
- Keep existing API routes stable.
- Use IGymsModuleApi/mediator for gym access checks.
- Do not reference Gyms, Users, Training, or Maintenance module projects directly.
- Do not use another module's DbContext or repository.
- Do not expose EF/domain entities through API.

Tasks:
1. Move membership-related entities/configurations into Modules.Memberships/Domain and Infrastructure.
2. Move related repositories/UOW pieces into Modules.Memberships/Infrastructure.
3. Move BLL/application services into Modules.Memberships/Application.
4. Move mappers into Modules.Memberships/Application or Api as appropriate.
5. Move API implementation into Modules.Memberships/Api while preserving existing routes.
6. Update Admin MVC integration to call mediator/module services.
7. Keep React client contract stable.
8. Add/repair tests for member CRUD, membership packages, status changes, member workspace, and tenant isolation.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Member
dotnet test multi-gym-management-system.slnx --filter Membership
cd client
npm test

Done when:
- Members API works.
- Membership package API works.
- Member workspace works.
- Admin member/membership pages work.
- React client tests pass.
- Architecture tests pass.

============================================================
PHASE 7 PROMPT: EXTRACT TRAINING MODULE
============================================================

Extract the Training module.

Move ownership of these concepts into Modules.Training:
- Staff
- TrainingCategory
- TrainingSession
- Booking
- booking/session services
- attendance update logic
- trainer/member session workflows

Constraints:
- Keep existing API routes unchanged.
- Use shared contracts/mediator for gym and member checks.
- Do not reference Gyms, Users, Memberships, or Maintenance module projects directly.
- Do not use another module's DbContext or repository.

Tasks:
1. Move training entities/configurations into Modules.Training.
2. Move training repositories/UOW pieces.
3. Move training BLL/application services and mappers.
4. Move API implementation while preserving routes.
5. Add Shared.Contracts module API abstraction if Training needs to validate member/staff/user data.
6. Update MVC/Admin and React integration only where necessary.
7. Add/repair tests for training categories, sessions, bookings, attendance, IDOR, and tenant isolation.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Training
dotnet test multi-gym-management-system.slnx --filter Booking
cd client
npm test

Done when:
- Training category workflow works.
- Session workflow works.
- Booking workflow works.
- Attendance workflow works.
- Client tests pass.
- Architecture tests pass.

============================================================
PHASE 8 PROMPT: EXTRACT MAINTENANCE MODULE
============================================================

Extract the Maintenance module.

Move ownership of these concepts into Modules.Maintenance:
- EquipmentModel
- Equipment
- MaintenanceTask
- maintenance workflow service
- task status updates
- task assignment updates
- generate due tasks
- caretaker workflow

Constraints:
- Keep existing API routes stable.
- Use shared contracts/mediator for gym access and staff/caretaker assignment checks.
- Do not reference Gyms, Users, Memberships, or Training module projects directly.
- Do not use another module's DbContext or repository.

Tasks:
1. Move maintenance entities/configurations into Modules.Maintenance.
2. Move maintenance repositories/UOW pieces.
3. Move maintenance workflow services and mappers.
4. Move API implementation while preserving routes.
5. Add/repair tests for task listing, status update, assignment update, due generation, tenant isolation, and caretaker access.
6. Keep React maintenance workflow working.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Maintenance
cd client
npm test

Done when:
- Maintenance API works.
- Due generation works.
- Caretaker/admin workflows work.
- Client tests pass.
- Architecture tests pass.

============================================================
PHASE 9 PROMPT: SPLIT PERSISTENCE OWNERSHIP
============================================================

Split persistence ownership by module.

Preferred target:
- UsersDbContext owns users schema.
- GymsDbContext owns gyms schema.
- MembershipsDbContext owns memberships schema.
- TrainingDbContext owns training schema.
- MaintenanceDbContext owns maintenance schema.

Constraints:
- Same physical database is fine.
- Prefer schema-per-module if migrations remain manageable.
- Do not let one module query another module's DbContext.
- Do not create cross-module EF joins.
- Use module contracts/mediator instead.
- Keep database migrations working.

Tasks:
1. Move EF configurations and DbSets into owning module infrastructure.
2. Create module DbContexts.
3. Configure schema-per-module where practical.
4. Move migrations or add new migrations safely.
5. Remove old all-domain AppDbContext usage where possible.
6. Add tests proving module DbContexts can read/write own data.
7. Add architecture tests preventing cross-module DbContext usage.

Validation commands:
dotnet test multi-gym-management-system.slnx
RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx --filter PostgreSql

Done when:
- Persistence ownership is modular.
- Migrations apply.
- Tests pass.
- Cross-module DbContext access is prevented.

============================================================
PHASE 10 PROMPT: REMOVE LEGACY APP.* DEPENDENCIES
============================================================

Remove legacy App.* dependencies after all domain slices have moved into modules.

Goal:
Final2 must not keep App.BLL/App.DAL/App.Domain as the real dependency center. If App.BLL remains the real core, the project is still a layered monolith, not a modular monolith.

Tasks:
1. Find all remaining references from Modules.* to:
   - App.Domain
   - App.BLL
   - App.BLL.Contracts
   - App.DAL.EF
   - App.DAL.Contracts
2. Move remaining reusable base abstractions to SharedKernel.
3. Move remaining public DTOs/contracts to Shared.Contracts.
4. Keep App.Resources if it is still needed for localization.
5. Delete or shrink old App.* projects that no longer own behavior.
6. Update the solution file.
7. Tighten architecture tests:
   - Modules.* must not reference App.BLL.
   - Modules.* must not reference App.DAL.
   - Modules.* must not reference App.Domain.
   - WebApp must not call legacy App.BLL for moved domains.
8. Do not change public API behavior.

Validation commands:
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx

Done when:
- Modules own the domain/application/infrastructure behavior.
- App.BLL is no longer the center of the application.
- Architecture tests enforce the new dependency direction.
- Full test suite passes.

============================================================
PHASE 11 PROMPT: ADMIN UX REGRESSION AND NO VIEWBAG/VIEWDATA
============================================================

Run a full MVC Admin regression pass after modularization.

Tasks:
1. Fix all Admin controllers/views to use module contracts or mediator.
2. Do not use legacy BLL services for moved domains.
3. Do not use DbContext directly in Admin controllers.
4. Ensure all Admin views use strongly typed ViewModels.
5. Remove all ViewBag usage.
6. Remove all ViewData usage.
7. Keep anti-forgery protection on forms.
8. Ensure admin routes are protected by Admin/SystemAdmin role policy as appropriate.
9. Add/repair integration tests for protected admin CRUD flows.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Admin
grep -R "ViewBag\|ViewData" WebApp/Areas/Admin || true

Done when:
- Admin UX works.
- Anonymous users cannot access Admin.
- Normal users cannot access Admin.
- Admin CRUD flows pass.
- No ViewBag or ViewData remains in Admin area.

============================================================
PHASE 12 PROMPT: REACT CLIENT REGRESSION AND CORS/DEPLOY SPLIT
============================================================

Run a React client regression pass against the modularized backend.

Constraints:
- Do not redesign the client.
- Do not rewrite the UI.
- Fix only API integration, DTO shape mismatches, auth refresh behavior, localization headers, and CORS/deploy configuration.
- Client must remain separately deployable.
- Backend and client must be deployable from separate domains/ports.

Tasks:
1. Keep API base URL contract stable.
2. Run client tests.
3. Fix DTO mismatch caused by backend modularization.
4. Ensure JWT is attached to API calls.
5. Ensure refresh-token flow still works.
6. Ensure Accept-Language is sent correctly.
7. Ensure 401 redirects/logs out appropriately.
8. Ensure 403 is shown as forbidden/access denied.
9. Configure backend CORS:
   - development origin for local client
   - production origin for deployed client
   - no AllowAnyOrigin in production
10. Validate separate client build and deployment config.

Validation commands:
cd client
npm test
npm run build
cd ..
dotnet test multi-gym-management-system.slnx

Done when:
- React client works against Final2 backend.
- Client is separately deployable.
- CORS is production-safe.
- Client tests pass.

============================================================
PHASE 13 PROMPT: FINAL COVERAGE AND TRACEABILITY HARDENING
============================================================

Add final Final2 coverage and traceability.

Tasks:
1. Create or update docs/final2-architecture.md.
2. Create or update docs/final2-traceability.md.
3. Document how each Final2 assignment requirement is satisfied.
4. Add or repair automated tests for:
   - modular boundaries
   - no direct module references
   - mediator communication
   - IDOR/tenant isolation
   - admin authorization
   - API versioning
   - Swagger
   - JWT login
   - refresh token
   - .resx UI localization
   - LangStr DB localization
   - CORS config
   - React client critical flows
5. Do not add new features.

Validation commands:
dotnet format multi-gym-management-system.slnx --verify-no-changes
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
cd client
npm test
npm run build

Done when:
- All assignment requirements map to code and tests.
- Backend tests pass.
- Client tests pass.
- Build passes.
- Docs explain the architecture clearly.

============================================================
PHASE 14 PROMPT: FINAL2 DEPLOYMENT SMOKE TEST
============================================================

Prepare Final2 deployment.

Tasks:
1. Update Docker, compose, GitLab CI/CD, deploy scripts, and smoke scripts for assignment05_final2.
2. Backend, database, and separate client must deploy successfully.
3. Configure production CORS for the deployed client URL only.
4. Ensure database migrations run during deploy or are documented clearly.
5. Add/update smoke test script for:
   - health endpoint
   - Swagger endpoint
   - login
   - refresh token if practical
   - one authenticated tenant API read
   - client hosted separately
6. Do not change business behavior.

Validation commands:
docker compose -f docker-compose.prod.yml config
docker compose build
bash scripts/smoke-deploy.sh

Done when:
- Backend deploys.
- DB deploys/persists.
- Client deploys separately.
- Smoke test passes.

============================================================
RISK-FIX PROMPT 1: PREVENT FAKE MODULARIZATION
============================================================

Problem to fix:
Only adding folders named Modules will not satisfy the modular monolith requirement.

Prompt:
Audit the Final2 codebase for fake modularization.
The project must not merely contain folders named Modules while still behaving like a layered monolith.

Check and fix:
1. Each module must own its domain classes, application services/handlers, infrastructure, and API integration for its own feature area.
2. Modules must not share a central App.BLL service layer as their real implementation.
3. Modules must not share one all-domain repository layer as their real implementation.
4. Modules must not directly reference other modules.
5. WebApp may compose modules, but WebApp controllers should not bypass module application services.
6. Add architecture tests proving module boundaries.
7. Add docs explaining each module's responsibility and owned entities.

Do not add new features.
Focus only on making the modular monolith real and defensible.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Architecture
dotnet test multi-gym-management-system.slnx

Done when:
- Modules are real ownership boundaries.
- Architecture tests catch fake modularization.
- App still works.

============================================================
RISK-FIX PROMPT 2: REMOVE APP.BLL AS REAL DEPENDENCY CENTER
============================================================

Problem to fix:
Keeping App.BLL as the real dependency center means Final2 is still a layered monolith.

Prompt:
Audit and remove App.BLL as the central application dependency.

Tasks:
1. Find all WebApp controllers and Admin controllers that still inject App.BLL.Contracts services for domains already moved into modules.
2. Replace those injections with mediator/module application contracts.
3. Move remaining business logic from App.BLL into the owning module's Application folder.
4. Move remaining mappers into owning modules or Shared.Contracts if they are pure DTO mappers.
5. Move reusable base abstractions into SharedKernel.
6. Add architecture tests:
   - Modules.* must not reference App.BLL.
   - WebApp must not call App.BLL for moved modules.
   - App.BLL must not be required for module use cases.
7. Keep public API behavior unchanged.

Validation commands:
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- App.BLL is no longer the core of Final2.
- Modules own their application logic.
- Tests pass.

============================================================
RISK-FIX PROMPT 3: ADD HARD ARCHITECTURE TESTS AGAINST DIRECT MODULE REFERENCES
============================================================

Problem to fix:
Not adding architecture tests allows Codex to reintroduce direct module references.

Prompt:
Add hard architecture tests that fail if module boundaries are violated.

Rules to enforce:
1. Modules.Users must not reference Modules.Gyms, Modules.Memberships, Modules.Training, or Modules.Maintenance.
2. Modules.Gyms must not reference Modules.Users, Modules.Memberships, Modules.Training, or Modules.Maintenance.
3. Modules.Memberships must not reference Modules.Users, Modules.Gyms, Modules.Training, or Modules.Maintenance.
4. Modules.Training must not reference Modules.Users, Modules.Gyms, Modules.Memberships, or Modules.Maintenance.
5. Modules.Maintenance must not reference Modules.Users, Modules.Gyms, Modules.Memberships, or Modules.Training.
6. Modules.* may reference Shared.Contracts, SharedKernel, and App.Resources.
7. WebApp may reference modules and shared projects.
8. Modules.* must not reference App.BLL, App.DAL, App.DAL.EF, or App.Domain after migration is complete.
9. Modules must not use another module's DbContext, repository, entity, or internal service type.

Implementation guidance:
- Use NetArchTest, ArchUnitNET, or a simple reflection/project-reference based test.
- Include both assembly reference checks and namespace/type usage checks where feasible.
- Make tests easy to run with dotnet test.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- A direct module reference causes tests to fail.
- A legacy App.BLL/App.DAL reference from modules causes tests to fail.
- Full architecture test suite passes.

============================================================
RISK-FIX PROMPT 4: CLIENT REGRESSION AFTER BACKEND REFACTOR
============================================================

Problem to fix:
Not running client tests after backend refactor can silently break the final demo.

Prompt:
Run and harden the React client regression suite after backend modularization.

Tasks:
1. Run npm test and npm run build in the client folder.
2. Check all generated/request DTO assumptions against backend API responses.
3. Verify login, refresh token, logout, switch gym, switch role, members, membership packages, sessions/bookings, and maintenance flows.
4. Ensure 401 handling still refreshes/logs out correctly.
5. Ensure 403/tenant access errors are visible in UI.
6. Ensure Accept-Language is still sent.
7. Do not change backend routes unless matching client updates are done in the same phase.
8. Add missing client tests for the most critical broken flows.

Validation commands:
cd client
npm test
npm run build

Done when:
- Client tests pass.
- Client build passes.
- Critical API flows still work.

============================================================
RISK-FIX PROMPT 5: APPUSERGYMROLE BOUNDARY FIX
============================================================

Problem to fix:
AppUserGymRole is boundary-sensitive. Gyms should own it and reference users by UserId, not by direct AppUser navigation across modules.

Prompt:
Refactor AppUserGymRole ownership so it belongs to the Gyms module.

Tasks:
1. Move AppUserGymRole into Modules.Gyms/Domain.
2. Ensure AppUserGymRole stores UserId as a scalar identifier.
3. Remove direct AppUser navigation from AppUserGymRole if it creates cross-module EF/entity dependency.
4. If user data is needed, query it through IUsersModuleApi from Shared.Contracts.
5. Ensure Users module does not need to know Gym internals.
6. Ensure Gyms module does not reference Users module directly.
7. Update EF configuration to avoid cross-module navigation.
8. Update switch-gym, switch-role, and access resolution logic.
9. Add tests for user-gym-role lookup and tenant access.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Gym
dotnet test multi-gym-management-system.slnx --filter Account
dotnet test multi-gym-management-system.slnx --filter Architecture

Done when:
- Gyms owns AppUserGymRole.
- No cross-module AppUser navigation remains.
- Access resolution works.
- Architecture tests pass.

============================================================
RISK-FIX PROMPT 6: REMOVE CROSS-MODULE EF NAVIGATIONS BETWEEN STAFF, MEMBER, USER
============================================================

Problem to fix:
Staff, Member, and User relationships may need contract-based lookup instead of EF navigation across modules.

Prompt:
Audit and remove unsafe cross-module EF navigations involving Staff, Member, and User.

Target ownership:
- Users owns AppUser.
- Memberships owns Member and Person.
- Training owns Staff, TrainingSession, Booking.
- Gyms owns gym access and AppUserGymRole.

Tasks:
1. Search for EF navigation properties crossing module boundaries:
   - Staff -> AppUser
   - Member -> AppUser
   - Booking -> Member if Booking belongs to Training and Member belongs to Memberships
   - Any Training entity -> Memberships entity
   - Any Memberships entity -> Users entity
2. Replace unsafe navigation with scalar foreign key identifiers where appropriate:
   - UserId
   - MemberId
   - StaffId
   - GymId
3. Use module API contracts for data lookup:
   - IUsersModuleApi
   - IMembershipsModuleApi
   - IGymsModuleApi
4. Avoid cross-module Include calls.
5. Avoid cross-module EF joins.
6. Update DTO mappers to call application/module APIs when additional display data is required.
7. Add architecture tests to prevent references to another module's entity namespace.
8. Add integration tests for affected workflows.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Architecture
dotnet test multi-gym-management-system.slnx --filter Booking
dotnet test multi-gym-management-system.slnx --filter Member
dotnet test multi-gym-management-system.slnx --filter Training

Done when:
- Cross-module EF navigations are removed or justified.
- Workflows still work.
- No module references another module's entity types.

============================================================
RISK-FIX PROMPT 7: LANGSTR SAFE UPDATE FIX
============================================================

Problem to fix:
LangStr updates must not overwrite other languages.

Prompt:
Audit and fix all LangStr update logic.

Rules:
1. Updating one language must not delete other existing translations.
2. If request contains only et, existing en must remain.
3. If request contains only en, existing et must remain.
4. Empty string should be handled intentionally: either reject or treat as explicit clear, but do not accidentally erase all languages.
5. Fallback culture must work when requested language is missing.
6. API responses should return the localized string for current/requested culture.
7. Admin edit forms must show and save multilingual fields safely.

Tasks:
1. Search all code that assigns LangStr fields.
2. Replace full-object overwrite with merge/update logic where needed.
3. Add LangStr helper methods in SharedKernel if useful:
   - Get(culture, fallbackCulture)
   - MergeFrom(input)
   - Set(culture, value)
4. Add unit tests for merge behavior.
5. Add integration tests for create/update/read localized fields.
6. Add regression tests proving updates do not erase other languages.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Localization
dotnet test multi-gym-management-system.slnx --filter LangStr

Done when:
- Updating one language preserves other languages.
- Fallback behavior works.
- Admin and API localization tests pass.

============================================================
RISK-FIX PROMPT 8: PRODUCTION CORS AND VPS DOMAIN FIX
============================================================

Problem to fix:
Production CORS can pass locally but fail on VPS if Final2 domain/client origin changes.

Prompt:
Audit and fix production CORS and deployment origin configuration for assignment05_final2.

Tasks:
1. Identify backend public URL for Final2.
2. Identify separate client public URL for Final2.
3. Configure backend CORS from environment variables, not hardcoded localhost only.
4. Development may allow localhost client origins.
5. Production must allow only the deployed client origin.
6. Production must not use AllowAnyOrigin.
7. Ensure credentials/header/method configuration matches JWT Bearer API usage.
8. Ensure preflight OPTIONS succeeds for API endpoints used by client.
9. Update docker-compose.prod.yml or deployment env files.
10. Update GitLab CI/CD variables documentation.
11. Add smoke test for CORS preflight from the deployed client origin.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Cors
docker compose -f docker-compose.prod.yml config
bash scripts/smoke-deploy.sh

Done when:
- Local CORS works.
- Production CORS allows only the correct client origin.
- Deployed client can call deployed backend.
- Smoke test passes.

============================================================
RISK-FIX PROMPT 9: IDOR AND TENANT ISOLATION HARDENING
============================================================

Problem to fix:
After moving code into modules, ownership/tenant checks can accidentally move out of the execution path.

Prompt:
Perform an IDOR and tenant isolation hardening pass across all REST endpoints.

Tasks:
1. List every endpoint containing a resource id or gymCode.
2. Ensure current user id is passed into application handlers/services.
3. Ensure gymCode is resolved through Gyms module access contract.
4. Ensure resource belongs to the resolved gym before returning or mutating it.
5. Ensure user role permits the operation.
6. Add two-user/two-gym integration tests for:
   - Members
   - Memberships
   - Training sessions
   - Bookings
   - Maintenance tasks
   - Admin/system endpoints
7. Decide and document whether forbidden resources return 403 or 404.
8. Do not rely only on UI hiding buttons.

Validation commands:
dotnet test multi-gym-management-system.slnx --filter Security
dotnet test multi-gym-management-system.slnx --filter Tenant
dotnet test multi-gym-management-system.slnx

Done when:
- User A cannot access User B's data.
- Gym A user cannot access Gym B data.
- Tests prove IDOR/tenant isolation.

============================================================
RISK-FIX PROMPT 10: FINAL DEFENCE READINESS AUDIT
============================================================

Prompt:
Perform a final Final2 defence-readiness audit.

Checklist:
1. assignment05_final2 exists at the same level as assignment-03 and assignment-04.
2. Final1 was not modified.
3. There are at least 3 modules: Users plus at least two domain modules.
4. Recommended modules exist: Users, Gyms, Memberships, Training, Maintenance.
5. Modules do not reference each other.
6. Mediator is used for communication/events.
7. API routes work and are versioned.
8. Swagger opens.
9. JWT login works.
10. Refresh token works.
11. Admin UX is protected and functional.
12. Admin uses ViewModels only.
13. No ViewBag/ViewData in Admin.
14. React client works from separate origin.
15. CORS is correct.
16. .resx UI translations work.
17. LangStr DB translations work.
18. IDOR/tenant isolation tests pass.
19. Docker/CI/CD deploy works.
20. README explains how to run, test, and deploy.
21. docs/final2-architecture.md explains module ownership and boundaries.
22. docs/final2-traceability.md maps assignment requirements to code/tests.

Validation commands:
dotnet format multi-gym-management-system.slnx --verify-no-changes
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
cd client
npm test
npm run build
cd ..
docker compose build

Done when:
- All checks pass.
- The project is ready for final defence.

