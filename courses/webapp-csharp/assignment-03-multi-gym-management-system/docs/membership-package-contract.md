# Membership Package Contract

Date: 2026-04-28
Phase: 6 - membership packages CRUD vertical slice

This is the locked contract for the REST API and React client types that manage sellable membership packages. It does not introduce external payments and does not redesign finance.

## Routes

Base route: `/api/v1/{gymCode}/membership-packages`

| Method | Path | Roles | Success | Notes |
| --- | --- | --- | --- | --- |
| GET | `/` | `GymOwner`, `GymAdmin`, `Member` | `200 MembershipPackageResponse[]` | Lists active, non-deleted packages for the active gym. |
| POST | `/` | `GymOwner`, `GymAdmin` | `201 MembershipPackageResponse` | Creates a package in the active gym. |
| PUT | `/{id}` | `GymOwner`, `GymAdmin` | `200 MembershipPackageResponse` | Updates a package only when `id` belongs to the active gym. |
| DELETE | `/{id}` | `GymOwner`, `GymAdmin` | `204 No Content` | Soft-deletes an unused package only when `id` belongs to the active gym. |

All routes require Bearer JWT authentication. The URL `{gymCode}` must match the caller's active gym context; otherwise the API returns `403 application/problem+json`.

## DTOs

### `MembershipPackageResponse`

```json
{
  "id": "guid",
  "name": "string",
  "packageType": 1,
  "durationValue": 1,
  "durationUnit": 1,
  "basePrice": 79.00,
  "currencyCode": "EUR",
  "trainingDiscountPercent": null,
  "isTrainingFree": false,
  "description": "string | null"
}
```

### `MembershipPackageUpsertRequest`

```json
{
  "name": "string",
  "packageType": 1,
  "durationValue": 1,
  "durationUnit": 1,
  "basePrice": 79.00,
  "currencyCode": "EUR",
  "trainingDiscountPercent": null,
  "isTrainingFree": false,
  "description": "string | null"
}
```

Enums:

- `packageType`: `0 Single`, `1 Monthly`, `2 Yearly`, `3 Custom`
- `durationUnit`: `0 Day`, `1 Month`, `2 Year`

## Error Contract

Package validation is enforced in `MembershipPackageService`, not only in controller/model binding. Validation failures return:

- HTTP `400`
- `Content-Type: application/problem+json; charset=utf-8`
- `title: "Validation Failed"`
- `errors`: string array with one or more validation messages

Unknown package IDs return `404`. Wrong active gym or insufficient role returns `403`.

## Delete Semantics

`MembershipPackage` inherits `TenantBaseEntity`, so deletes follow the existing soft-delete design. The service calls `Remove(package)`, and `AppDbContext.ApplySoftDelete()` converts that into `IsDeleted = true` and `DeletedAtUtc`.

Used package behavior is intentionally blocked:

- `DELETE` returns `409 Conflict` when a package is already referenced by a membership.
- The conflict message tells the caller to deactivate the package instead of deleting it.
- Existing `Membership` rows keep `MembershipPackageId`.
- `Membership.PriceAtPurchase` and `Membership.CurrencyCode` preserve the sales snapshot.
- Unused package deletes still use the existing soft-delete path and disappear from normal package lists.

No external payment workflow is part of this contract.

## Tenant Isolation

Update and delete lookups include both `Id` and active `GymId`. This is required because tests disable EF global query filters to prove the service does not rely on filters alone. Cross-gym ID manipulation returns `404`.
