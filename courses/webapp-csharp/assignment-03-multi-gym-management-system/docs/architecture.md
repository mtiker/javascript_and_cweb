# Architecture Overview

## Goal

Assignment 03 is implemented as a multi-tenant SaaS platform for gym operations.

The solution combines:
- one ASP.NET Core backend host for MVC, REST API, Swagger, and health checks
- a separate React + TypeScript admin client that consumes the REST API
- shared domain, data-access, and business layers under the same solution

## Runtime Surfaces

Backend host:
- MVC admin area
- MVC client area mounted at `/mvc-client`
- versioned REST API under `/api/v1/...`
- Swagger
- `/health`
- production React client mounted at `/client`

Separate client:
- `client/`
- Vite + React + TypeScript
- JWT + refresh-token flow
- system-role platform console for analytics, onboarding, billing, support, snapshots, and impersonation
- tenant function console for staff, contracts, vacations, scheduling, bookings, memberships, payments, facilities, maintenance, settings, and gym users
- focused admin CRUD for members, training categories, and membership packages
- session list/detail and booking through the REST API
- trainer attendance and caretaker maintenance task updates through the REST API
- language selector that sends `Accept-Language` to the API

## Layered Structure

Projects:
- `App.Domain`: entities, enums, role names, claim types, shared abstractions
- `App.DAL.EF`: `AppDbContext`, mappings, migrations, tenant filters, seeding
- `App.BLL`: business services, `IAppDbContext` boundary, authorization helpers, token and SaaS workflows
- `App.DTO`: public API contracts
- `App.Resources`: `.resx` localization resources
- `WebApp`: API controllers, MVC controllers, middleware, views, startup
- `WebApp.Tests`: unit and integration tests
- `client`: separate React client and frontend tests

Code organization mirrors the Assignment 18 backend style: each domain entity has its own file, DTOs are split by API resource namespace, BLL service interfaces live beside their implementations, seed data is split through partial files, and `WebApp/Setup` separates database, identity, service registration, web API, middleware, and data initialization.

## Request Flows

MVC flow:
1. request enters `WebApp`
2. cookie authentication resolves the signed-in user
3. MVC controller uses view models and server-side rendering
4. Admin routes use the standard area route; Client routes use `/mvc-client` to avoid colliding with the React `/client` mount
5. shared localization and anti-forgery protections apply

API flow:
1. request enters `WebApp`
2. JWT authentication resolves the caller
3. tenant access is checked against route gym code and claims
4. tenant workflow controllers delegate to BLL services
5. DTOs from `App.DTO.v1` are returned

Separate client flow:
1. React client logs in through `/api/v1/account/login`
2. auth state is stored in `sessionStorage`
3. `ApiClient` adds the bearer token and selected `Accept-Language` to API requests
4. a `401` triggers one refresh attempt through `/api/v1/account/renew-refresh-token`
5. refresh failure clears auth state and sends the user back to login

Production client flow:
1. Docker builds `client/` with Node 20
2. `client/dist` is copied into `WebApp/wwwroot/client`
3. ASP.NET Core serves `/client` and `/client/{route}` from `client/index.html`
4. production API calls default to same-origin when `VITE_API_BASE_URL` is not set

## Multi-Tenancy Model

Tenant root:
- `Gym`

Isolation rules:
- every tenant-owned entity carries `GymId`
- tenant API routes use `/api/v1/{gymCode}/...`
- the route gym code must match the active gym in claims
- system APIs live under `/api/v1/system/...`

Implementation details:
- `IGymContext` exposes active gym data
- `AppDbContext` applies query filters for `ITenantEntity`
- business entities use soft delete through `TenantBaseEntity`
- authorization helpers block cross-gym access and self-only violations

## Authentication and Authorization

Auth modes:
- ASP.NET Core Identity for user management
- JWT access tokens for the REST API
- refresh-token rotation for API sessions
- MVC cookie auth for the server-rendered UX

Runtime configuration:
- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` are required; the app fails startup if any are missing
- local development should provide these through user secrets
- production should provide them through environment variables such as `JWT__Key`
- ASP.NET Core Data Protection keys are persisted in the database through `AppDbContext`

Platform roles:
- `SystemAdmin`
- `SystemSupport`
- `SystemBilling`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

Important rules:
- members can access only their own member data
- trainers can update attendance only for assigned sessions
- caretakers can update only assigned maintenance tasks
- gym admins and owners can manage tenant-wide business data
- the React client admits `SystemAdmin`, `SystemSupport`, `SystemBilling`, `GymAdmin`, `GymOwner`, `Member`, `Trainer`, and `Caretaker` sessions

## Localization

UI localization:
- `.resx` files in `App.Resources`
- request localization for `et-EE` and `en`
- shared MVC culture switcher
- React language selector persisted in `localStorage`

DB localization:
- `LangStr` value object
- stored through EF value conversion
- used for multilingual business-owned text such as category and package names

## Error Handling

API/JSON requests:
- `ProblemDetailsMiddleware` maps unhandled exceptions to `application/problem+json`

HTML/MVC requests:
- production uses `UseExceptionHandler("/Home/Error")`
- `/Home/Error` renders a localized MVC error view

This keeps API failures machine-readable while still giving browser users a normal error page.

## Separate Client Design

Current frontend scope:
- single active gym per login session
- shell-level gym and role picker for assigned multi-gym users, plus SystemAdmin tenant picking
- switch-gym and switch-role actions remain available in the React console for explicit API testing
- system-role platform and tenant-owner/admin function console coverage
- CRUD coverage for:
  - members
  - training categories
  - membership packages
- session list/detail coverage
- member booking with payment reference support
- trainer attendance updates
- caretaker maintenance task status updates

Frontend structure:
- `src/lib/apiClient.ts`: HTTP client, refresh retry logic, DTO mapping
- `src/lib/auth.tsx`: auth context, route protection, session persistence
- `src/lib/language.tsx`: language state and API localization header source
- `src/pages/*`: CRUD pages and login
- `src/components/*`: shell and notice components

Boundary note:
- tenant members, staff, contracts, vacations, training, membership/payment, and facilities workflows now run through BLL services backed by `IAppDbContext`
- the remaining direct `AppDbContext` injection is intentionally limited to broad MVC/admin read composition and framework infrastructure such as Identity/EF setup

## CORS

The backend exposes a named `ClientApp` policy.

Default allowed origins:
- `http://localhost:5173`
- `https://localhost:5173`
- `http://127.0.0.1:5173`

Production Compose sets the first allowed origin to `https://mtiker-cweb-4.proxy.itcollege.ee` by default.
The list is configurable through `Cors:AllowedOrigins` or `Cors__AllowedOrigins__0`.

## Design Choices

Why keep the backend as one ASP.NET Core host:
- it preserves the course-aligned monolith for MVC, API, Swagger, and business logic
- it keeps deployment complexity lower while the public deploy remains pending

Why add a separate client instead of replacing MVC:
- the assignment requires both working MVC UX and a real API client
- keeping MVC plus React makes the domain easier to demo from multiple angles

Why use a console for the broad SaaS surface:
- the dental clinic reference exposes platform and tenant operations from a client UI, so Assignment 03 now does the same without replacing the existing focused pages
- the console keeps every REST function reachable while the higher-use workflows still have dedicated pages
