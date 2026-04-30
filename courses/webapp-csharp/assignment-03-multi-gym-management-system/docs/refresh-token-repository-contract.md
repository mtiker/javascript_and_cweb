# Refresh Token Repository Contract

## Purpose

`IRefreshTokenRepository` is the persistence contract for API session refresh
tokens. It keeps refresh-token lookup, rotation, and logout invalidation out of
controllers and out of concrete EF code.

Contract location:

```text
src/App.BLL/Contracts/Persistence/IRefreshTokenRepository.cs
```

EF implementation:

```text
src/App.DAL.EF/Repositories/EfRefreshTokenRepository.cs
```

Unit of Work access:

```text
IAppUnitOfWork.RefreshTokens
```

## Methods

| Method | Responsibility |
|---|---|
| `GetByUserAndTokenAsync(Guid userId, string refreshToken, CancellationToken)` | Load one token by user id and token value; includes the user for refresh use cases |
| `ListByUserAsync(Guid userId, CancellationToken)` | Load all refresh tokens for logout invalidation |
| `AddAsync(AppRefreshToken refreshToken, CancellationToken)` | Stage a new refresh token for persistence |
| `Remove(AppRefreshToken refreshToken)` | Stage a token deletion for rotation |
| `RemoveRange(IEnumerable<AppRefreshToken> refreshTokens)` | Stage all user token deletions for logout |

`SaveChangesAsync` is intentionally not on the repository. Transaction completion
belongs to `IAppUnitOfWork`.

## Rotation Contract

Refresh-token renewal must:

1. validate the submitted JWT signature and extract user id
2. load the stored refresh token by user id and token value
3. reject missing tokens
4. reject expired tokens
5. remove the old token
6. create a replacement token with `PreviousRefreshToken` set for audit context
7. add the replacement token
8. call `IAppUnitOfWork.SaveChangesAsync`
9. return the replacement token value in `JwtResponse`

Reusing the old refresh token after rotation must fail because the old row no
longer exists.

## Logout Contract

Logout must:

1. resolve the current authenticated user id
2. load all refresh tokens for that user
3. remove the loaded token set
4. call `IAppUnitOfWork.SaveChangesAsync`

After logout, the old refresh token must not renew the session.

## Query Constraints

The repository must not:

- return tokens for another user
- expose IQueryable to callers
- save changes internally
- accept controller or DTO types
- depend on `WebApp`

The EF implementation may use includes and EF query operators because it lives
in `App.DAL.EF`.

## Verification

Covered by:

- `AuthSecurityAndErrorTests.RenewRefreshToken_RotatesToken_AndRejectsReuse`
- `AuthSecurityAndErrorTests.Logout_InvalidatesRefreshToken`
- `AuthSecurityAndErrorTests.RenewRefreshToken_RejectsExpiredRefreshToken`
- `ArchitectureTests.RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence`
- `ArchitectureTests.AccountAuthSlice_UsesDedicatedServiceRepositoryAndMapperBoundaries`
