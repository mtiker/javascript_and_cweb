# Membership Package Validation Rules

Date: 2026-04-28

These rules apply to `MembershipPackageUpsertRequest` for both `POST /api/v1/{gymCode}/membership-packages` and `PUT /api/v1/{gymCode}/membership-packages/{id}`.

## Required Fields

| Field | Rule | Failure message |
| --- | --- | --- |
| `name` | Must be present after trimming. | `Package name is required.` |
| `durationValue` | Must be greater than zero. | `Duration value must be greater than zero.` |
| `basePrice` | Must be zero or greater. | `Base price must be zero or greater.` |
| `currencyCode` | Must be present after trimming. | `Currency code is required.` |

## Value Rules

| Field | Rule | Failure message |
| --- | --- | --- |
| `packageType` | Must be a defined `MembershipPackageType` enum value. | `Package type is invalid.` |
| `durationUnit` | Must be a defined `DurationUnit` enum value. | `Duration unit is invalid.` |
| `currencyCode` | Must be three alphabetic letters; persisted uppercase. | `Currency code must be a three-letter ISO currency code.` |
| `trainingDiscountPercent` | Optional; when supplied, must be between `0` and `100`. | `Training discount must be between 0 and 100.` |
| `description` | Optional; blank values are stored as `null`. | None. |

## Normalization

The BLL normalizes accepted requests before persistence:

- `name` is trimmed.
- `currencyCode` is trimmed and uppercased.
- blank `description` becomes `null`; non-blank `description` is trimmed.
- `trainingDiscountPercent` remains `null` when omitted.

## API Error Shape

Validation failures use `ValidationAppException`, which is converted by `ProblemDetailsMiddleware` to:

```json
{
  "title": "Validation Failed",
  "status": 400,
  "detail": "Validation failed.",
  "errors": ["Base price must be zero or greater."]
}
```

React uses the same rules locally before submit so package forms show validation feedback without making unnecessary API calls. The API remains authoritative.
