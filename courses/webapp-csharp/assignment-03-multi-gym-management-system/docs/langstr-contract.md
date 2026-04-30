# LangStr Contract

Date: 2026-04-28

## Purpose

`LangStr` represents database-owned translated business text. It is used when the value is not a static application label and must be stored with the entity, for example training category names.

Use `.resx` resources for UI labels. Use `LangStr` for tenant/business data.

## Storage Shape

`LangStr` stores translations as a case-insensitive dictionary:

```json
{
  "translations": {
    "en": "Strength Lab",
    "et": "Joud"
  }
}
```

EF Core persists it through the existing `LangStr` value converter/comparer.

## Culture Keys

Supported keys should prefer neutral language codes for business values:

- `en`
- `et`

Specific request cultures such as `et-EE` are resolved to neutral keys when no exact value exists.

## Read Resolution

`Translate(culture)` resolves in this order:

1. exact culture key, for example `et-EE`
2. neutral culture key, for example `et`
3. `LangStr.DefaultCulture`, currently `en`
4. first available translation value
5. `null` when no translations exist

This means missing translations must not crash API projection. The caller can still coalesce `null` to an empty string where a DTO requires a non-null value.

## Write Rules

For the current training-category API:

- create/update accepts a single display value
- the value is written through `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`
- blank names are invalid
- descriptions may be omitted

The API does not currently accept a multi-language dictionary payload. That is an explicit limitation of this vertical slice, not a `LangStr` limitation.

## Validation Rules

Training category payload:

- `name` is required
- `name` max length is 128
- `description` max length is 512
- blank names return `ProblemDetails`

## Test Contract

The current regression suite proves:

- English request culture returns English business text
- Estonian neutral/specific request cultures return Estonian business text
- missing requested translations fall back safely
- invalid training-category payloads return `ProblemDetails`
