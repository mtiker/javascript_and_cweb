# Data Model

## Overview

The Final2 defense model is intentionally smaller than the earlier enterprise
SaaS model. The defended product is multi-gym operations plus memberships, not
platform support, billing, coaching, employment roster, or invoice/refund
ledger management.

Tenant-owned business rows use `GymId`.

## Mermaid ERD

```mermaid
erDiagram
    GYM ||--|| GYM_SETTINGS : has
    GYM ||--o{ GYM_CONTACT : owns
    GYM ||--o{ APP_USER_GYM_ROLE : assigns
    GYM ||--o{ MEMBER : owns
    GYM ||--o{ STAFF : owns
    GYM ||--o{ TRAINING_CATEGORY : owns
    GYM ||--o{ TRAINING_SESSION : owns
    GYM ||--o{ BOOKING : owns
    GYM ||--o{ MEMBERSHIP_PACKAGE : owns
    GYM ||--o{ MEMBERSHIP : owns
    GYM ||--o{ PAYMENT : owns
    GYM ||--o{ EQUIPMENT_MODEL : owns
    GYM ||--o{ EQUIPMENT : owns
    GYM ||--o{ MAINTENANCE_TASK : owns

    APP_USER ||--o| PERSON : links_to
    APP_USER ||--o{ APP_USER_GYM_ROLE : has
    PERSON ||--o{ PERSON_CONTACT : has
    CONTACT ||--o{ PERSON_CONTACT : links
    CONTACT ||--o{ GYM_CONTACT : links

    PERSON ||--o{ MEMBER : profiles
    PERSON ||--o{ STAFF : profiles

    STAFF ||--o{ TRAINING_SESSION : trains
    TRAINING_CATEGORY ||--o{ TRAINING_SESSION : groups
    TRAINING_SESSION ||--o{ BOOKING : booked_as
    MEMBER ||--o{ BOOKING : makes

    MEMBER ||--o{ MEMBERSHIP : owns
    MEMBERSHIP_PACKAGE ||--o{ MEMBERSHIP : sold_as
    MEMBERSHIP ||--o{ PAYMENT : paid_by
    BOOKING ||--o{ PAYMENT : paid_by

    EQUIPMENT_MODEL ||--o{ EQUIPMENT : defines
    EQUIPMENT ||--o{ MAINTENANCE_TASK : receives
    STAFF ||--o{ MAINTENANCE_TASK : assigned_to
```

## Defended Entities

Platform and tenant context:
- `Gym`
- `GymSettings`
- `AppUserGymRole`

Shared identity/person entities:
- `AppUser`
- `AppRole`
- `AppRefreshToken`
- `Person`
- `Contact`
- `PersonContact`
- `GymContact`

Tenant business entities:
- `Member`
- `Staff`
- `TrainingCategory`
- `TrainingSession`
- `Booking`
- `MembershipPackage`
- `Membership`
- `Payment`
- `EquipmentModel`
- `Equipment`
- `MaintenanceTask`

## Removed From Final2 Scope

The pruning migration `PruneFinal2Scope` removes these optional contexts:
- platform subscriptions, support tickets, audit log
- coaching plans and coaching plan items
- invoices, invoice lines, and invoice payments
- job roles, employment contracts, vacations, and work shifts
- opening hours and opening-hour exceptions
- maintenance assignment history

## Special Modeling Decisions

- `AppUser` stays separate from business profiles and links to `Person`.
- `AppUserGymRole` is the tenant membership table for gym and role switching.
- A training session has optional `TrainerStaffId`; the old contract/work-shift
  trainer assignment model was removed for Final2 scope control.
- `Payment` is the only finance transaction entity kept for the defense.
- Maintenance assignment notes are stored on `MaintenanceTask.Notes`; assignment
  history is no longer a separate entity.
- `LangStr` is used where DB-backed translation is required.
- tenant business entities inherit audit/soft-delete behavior through
  `TenantBaseEntity`.
