# Member Mapper Audit

## Purpose

Document the entity ↔ DTO projection contract for the member slice and the
locations of every member-shaped projection so future changes do not regress
back into anonymous projections inside controllers or services.

## Mapper Location

```text
src/App.BLL/Mapping/MemberMapper.cs
src/App.BLL/Mapping/IMemberMapper.cs
```

The mapper lives under `App.BLL.Mapping`. The Phase 9 architecture test
`Mappers_LiveOnlyInBllMappingOrServicesNamespace` allows this namespace.

## Public Surface

| Method | Source | Target |
|---|---|---|
| `ToSummary(Member member)` | `Member` (with `Person`) | `MemberResponse` |
| `ToDetail(Member member)` | `Member` (with `Person`) | `MemberDetailResponse` |
| `ToSummaryList(IEnumerable<Member>)` | many `Member` | `IReadOnlyCollection<MemberResponse>` |

The mapper is pure: it does not query the DbContext, it does not hit any
service, and it does not throw. Missing `Person` data is treated as empty
strings on the DTO so the public contract never returns `null` for `FullName`,
`FirstName`, or `LastName`.

## DTO Field Map

`MemberResponse` (used by list endpoint and React `MemberSummary`):

| DTO field | Source |
|---|---|
| `Id` | `member.Id` |
| `MemberCode` | `member.MemberCode` |
| `FullName` | `"{Person.FirstName} {Person.LastName}".Trim()` |
| `Status` | `member.Status` |

`MemberDetailResponse` (used by detail endpoint, `me`, create/update results):

| DTO field | Source |
|---|---|
| `Id` | `member.Id` |
| `MemberCode` | `member.MemberCode` |
| `FirstName` | `member.Person?.FirstName ?? ""` |
| `LastName` | `member.Person?.LastName ?? ""` |
| `FullName` | `"{FirstName} {LastName}".Trim()` |
| `PersonalCode` | `member.Person?.PersonalCode` |
| `DateOfBirth` | `member.Person?.DateOfBirth` |
| `Status` | `member.Status` |

## Pre-Migration Inventory

Before Phase 11 the projection logic lived in two places:

| File | Symbol | Responsibility |
|---|---|---|
| `src/App.BLL/Services/MemberWorkflowService.cs` | `ToMemberDetailResponse(Member)` private static | Detail projection |
| `src/App.BLL/Services/MemberWorkflowService.cs` | inline `Select` in `GetMembersAsync` | Summary projection |

Phase 11 removes both. The service composes `IMemberMapper` and forwards the
loaded entities.

The MVC Admin page and React app keep their own view-model / type definitions:

| Site | Mapping |
|---|---|
| `src/WebApp/Areas/Admin/Controllers/MembersController.cs` | maps `MemberResponse` → `AdminMemberSummaryViewModel` (kept in controller — view-model concern, not entity ↔ DTO) |
| `client/src/lib/types.ts` | `MemberSummary`, `MemberDetail`, `MemberUpsertRequest` mirror the JSON contract |

These are deliberately **not** moved into `IMemberMapper`. The mapper rule
covers entity ↔ DTO. View-model construction in MVC and TypeScript types in
the React client are presentation concerns and stay where they are.

## Boundary Rules

The mapper MUST:

- live under `App.BLL.Mapping`
- not depend on `App.DAL.EF` or `WebApp`
- not call `DbContext`, `IAppDbContext`, `IAppUnitOfWork`, or any repository
- treat `Person == null` as empty/null per field rather than throwing

The mapper MUST NOT:

- reference HTTP types, MVC types, or routing helpers
- emit different shapes for different callers (one method, one shape)
- inject services

## Verification

- `ArchitectureTests.Mappers_LiveOnlyInBllMappingOrServicesNamespace`
- `ArchitectureTests.MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries`
- `MemberCrudTests` shapes (`MemberResponse[]`, `MemberDetailResponse`)
- `client/src/pages/CrudPages.test.tsx` member CRUD path
