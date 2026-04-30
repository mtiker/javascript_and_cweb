# Member Repository Contract

## Purpose

`IMemberRepository` is the persistence contract for tenant-scoped member CRUD.
It keeps member lookup, uniqueness checks, and lifecycle operations out of the
controllers and out of concrete EF code.

Contract location:

```text
src/App.BLL/Contracts/Persistence/IMemberRepository.cs
```

EF implementation:

```text
src/App.DAL.EF/Repositories/EfMemberRepository.cs
```

Unit of Work access:

```text
IAppUnitOfWork.Members
```

## Methods

| Method | Responsibility |
|---|---|
| `ListByGymAsync(Guid gymId, CancellationToken)` | Tenant-scoped, ordered list of members with the related `Person` populated for display projections. |
| `FindAsync(Guid gymId, Guid memberId, CancellationToken)` | Load one member by tenant + id without `Person` includes (used by Delete). |
| `FindWithPersonAsync(Guid gymId, Guid memberId, CancellationToken)` | Load one member by tenant + id with `Person` populated (used by Get / Update). |
| `MemberCodeExistsAsync(Guid gymId, string memberCode, Guid? excludeMemberId, CancellationToken)` | Tenant-scoped uniqueness check for `MemberCode`. |
| `PersonalCodeExistsAsync(string personalCode, Guid? excludePersonId, CancellationToken)` | Cross-tenant uniqueness check for `Person.PersonalCode` (personal codes are unique across the platform). |
| `AddAsync(Member member, CancellationToken)` | Stage a new member (and its owned `Person`) for persistence. |
| `Remove(Member member)` | Stage a member deletion. The DbContext converts this into a soft delete. |

`SaveChangesAsync` is intentionally **not** on the repository. Transaction
completion belongs to `IAppUnitOfWork`.

## Tenant Isolation

All single-row reads (`FindAsync`, `FindWithPersonAsync`) MUST filter by both
`Id` and the tenant `GymId` parameter. Cross-tenant ID manipulation MUST return
`null`. This invariant is required because integration tests disable EF global
query filters to prove the boundary does not rely on the filter alone.

`MemberCodeExistsAsync` MUST scope by `GymId`. Member codes are unique within a
gym, not globally.

`PersonalCodeExistsAsync` MUST NOT scope by gym — personal codes are
person-level (a real person belongs to at most one `Person` row, and that
`Person` may be reused across gyms in future workflows).

## Soft Delete Semantics

`Member` inherits `TenantBaseEntity`. `Remove(member)` is rewritten by
`AppDbContext.ApplySoftDelete()` into:

- `IsDeleted = true`
- `DeletedAtUtc = DateTime.UtcNow`

`ListByGymAsync`, `FindAsync`, and `FindWithPersonAsync` MUST NOT return
soft-deleted members. The default EF query filter handles this; the repository
relies on it and does not call `IgnoreQueryFilters`.

## Query Constraints

The repository must not:

- return members for another `gymId`
- expose `IQueryable` to callers
- save changes internally
- accept controller or DTO types
- depend on `WebApp`

The EF implementation may use `Include`, `OrderBy`, and other EF query
operators because it lives in `App.DAL.EF`.

## Verification

Covered by:

- `MemberCrudTests.GetMembers_ReturnsListForActiveGym`
- `MemberCrudTests.GetMember_ReturnsDetail_ForGymAdmin`
- `MemberCrudTests.CreateMember_Returns201_AndLocationHeader`
- `MemberCrudTests.UpdateMember_Returns200_AndUpdatedDetail`
- `MemberCrudTests.DeleteMember_Returns204_AndSubsequentGetIs404`
- `MemberCrudTests.DeleteMember_SoftDeletesMemberRow`
- `MemberCrudTests.CreateMember_DuplicateMemberCode_ReturnsProblemDetails`
- `MemberCrudTests.CreateMember_DuplicatePersonalCode_ReturnsProblemDetails`
- `MemberCrudTests.UpdateMember_ForeignGymMemberId_Returns404`
- `MemberWorkflowServiceTests.CreateMemberAsync_RejectsDuplicateMemberCode`
- `MemberWorkflowServiceTests.CreateMemberAsync_RejectsDuplicatePersonalCode`
- `MemberWorkflowServiceTests.GetMemberAsync_ScopedToGymTenant`
- `MemberWorkflowServiceTests.GetMemberAsync_RejectsCrossMemberAccessForMemberRole`
- `MemberWorkflowServiceTests.GetMembersAsync_OrdersByLastNameThenFirstName`
- `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence`
- `ArchitectureTests.MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries`
