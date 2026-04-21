# Data Model

## Overview

The schema is split into:
- SaaS/platform entities
- shared person/contact entities
- gym tenant business entities

Key invariant:
- tenant-owned business rows use `GymId`

## Mermaid ERD

```mermaid
erDiagram
    GYM ||--|| GYM_SETTINGS : has
    GYM ||--o{ SUBSCRIPTION : owns
    GYM ||--o{ SUPPORT_TICKET : owns
    GYM ||--o{ GYM_CONTACT : owns
    GYM ||--o{ APP_USER_GYM_ROLE : assigns
    GYM ||--o{ MEMBER : owns
    GYM ||--o{ STAFF : owns
    GYM ||--o{ JOB_ROLE : owns
    GYM ||--o{ TRAINING_CATEGORY : owns
    GYM ||--o{ TRAINING_SESSION : owns
    GYM ||--o{ WORK_SHIFT : owns
    GYM ||--o{ BOOKING : owns
    GYM ||--o{ MEMBERSHIP_PACKAGE : owns
    GYM ||--o{ MEMBERSHIP : owns
    GYM ||--o{ PAYMENT : owns
    GYM ||--o{ OPENING_HOURS : owns
    GYM ||--o{ OPENING_HOURS_EXCEPTION : owns
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

    STAFF ||--o{ EMPLOYMENT_CONTRACT : has
    JOB_ROLE ||--o{ EMPLOYMENT_CONTRACT : primary_role
    EMPLOYMENT_CONTRACT ||--o{ VACATION : has
    EMPLOYMENT_CONTRACT ||--o{ WORK_SHIFT : has

    TRAINING_CATEGORY ||--o{ TRAINING_SESSION : groups
    TRAINING_SESSION ||--o{ WORK_SHIFT : staffed_by
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

## Notes

Platform entities:
- `Gym`
- `GymSettings`
- `Subscription`
- `SupportTicket`
- `AuditLog`
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
- `JobRole`
- `EmploymentContract`
- `Vacation`
- `TrainingCategory`
- `TrainingSession`
- `WorkShift`
- `Booking`
- `MembershipPackage`
- `Membership`
- `Payment`
- `OpeningHours`
- `OpeningHoursException`
- `EquipmentModel`
- `Equipment`
- `MaintenanceTask`

## Special Modeling Decisions

- `AppUser` stays separate from business profiles and links to `Person`
- `AppUserGymRole` is the tenant membership table for SaaS context switching
- `WorkShift` models both training delivery and assisting floor work
- multiple trainers per session are represented by multiple training shifts linked to the same session
- `LangStr` is used where DB-backed translation is required
- business entities inherit audit/soft-delete behavior through `TenantBaseEntity`
