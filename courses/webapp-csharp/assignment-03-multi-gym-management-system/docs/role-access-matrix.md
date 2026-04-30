# Role Access Matrix

Legend: ✅ allowed · ❌ forbidden · 🔒 self-only · 👥 own-assignments-only

## System Roles (cross-tenant, no gym context required)

| Endpoint group              | SystemAdmin | SystemSupport | SystemBilling |
|-----------------------------|:-----------:|:-------------:|:-------------:|
| GET /system/platform/*      | ✅          | ✅            | ✅            |
| POST /system/gyms/register  | ✅          | ❌            | ❌            |
| GET /system/subscriptions   | ✅          | ✅            | ✅            |
| PUT /system/subscriptions/* | ✅          | ❌            | ✅            |
| GET /system/support-tickets | ✅          | ✅            | ❌            |
| POST /system/impersonation  | ✅          | ❌            | ❌            |
| Any tenant endpoint         | ✅ (via SwitchGym/GymOwner escalation) | ❌ | ❌ |

SystemAdmin may call SwitchGym to obtain a GymOwner-scoped token for any active gym, granting
full tenant admin access.  Without an active gym context in their JWT, even SystemAdmin is
blocked by `TenantAccessChecker`.

---

## Tenant Roles (gym-scoped, require active gym context)

### Members

| Endpoint                                    | GymOwner | GymAdmin | Member  | Trainer | Caretaker |
|---------------------------------------------|:--------:|:--------:|:-------:|:-------:|:---------:|
| GET /members                                | ✅       | ✅       | ❌      | ❌      | ❌        |
| GET /members/me                             | ✅       | ✅       | ✅      | ❌      | ❌        |
| GET /members/{id}                           | ✅       | ✅       | 🔒 self | ❌      | ❌        |
| POST /members                               | ✅       | ✅       | ❌      | ❌      | ❌        |
| PUT /members/{id}                           | ✅       | ✅       | ❌      | ❌      | ❌        |
| DELETE /members/{id}                        | ✅       | ✅       | ❌      | ❌      | ❌        |

### Member Workspace

| Endpoint                                    | GymOwner | GymAdmin | Member  | Trainer | Caretaker |
|---------------------------------------------|:--------:|:--------:|:-------:|:-------:|:---------:|
| GET /member-workspace/me                    | ✅       | ✅       | ✅      | ❌      | ❌        |
| GET /member-workspace/members/{id}          | ✅       | ✅       | 🔒 self | ✅      | ❌        |

### Training

| Endpoint                                    | GymOwner | GymAdmin | Member  | Trainer | Caretaker |
|---------------------------------------------|:--------:|:--------:|:-------:|:-------:|:---------:|
| GET /training-categories                    | ✅       | ✅       | ✅      | ✅      | ❌        |
| POST/PUT/DELETE /training-categories        | ✅       | ✅       | ❌      | ❌      | ❌        |
| GET /training-sessions                      | ✅       | ✅       | ✅      | ✅      | ✅        |
| POST/PUT/DELETE /training-sessions          | ✅       | ✅       | ❌      | ❌      | ❌        |

### Bookings

| Endpoint                                    | GymOwner | GymAdmin | Member      | Trainer         | Caretaker |
|---------------------------------------------|:--------:|:--------:|:-----------:|:---------------:|:---------:|
| GET /bookings                               | ✅       | ✅       | 🔒 own      | 👥 assigned     | ❌        |
| POST /bookings                              | ✅       | ✅       | 🔒 self-book| ❌              | ❌        |
| PUT /bookings/{id}/attendance               | ✅       | ✅       | ❌          | 👥 assigned     | ❌        |
| DELETE /bookings/{id}                       | ✅       | ✅       | 🔒 own      | ❌              | ❌        |

For **Trainer – PUT attendance**: allowed only for sessions where a Training WorkShift exists
linking the trainer's contract to the session.

### Maintenance

| Endpoint                                    | GymOwner | GymAdmin | Member  | Trainer | Caretaker        |
|---------------------------------------------|:--------:|:--------:|:-------:|:-------:|:----------------:|
| GET /maintenance-tasks                      | ✅       | ✅       | ❌      | ❌      | ✅               |
| POST /maintenance-tasks                     | ✅       | ✅       | ❌      | ❌      | ✅               |
| PUT /maintenance-tasks/{id}/status          | ✅       | ✅       | ❌      | ❌      | 👥 assigned only |
| PUT /maintenance-tasks/{id}/assignment      | ✅       | ✅       | ❌      | ❌      | ❌               |
| GET /maintenance-tasks/{id}/assignment-history | ✅    | ✅       | ❌      | ❌      | ✅               |
| POST /maintenance-tasks/generate-due        | ✅       | ✅       | ❌      | ❌      | ❌               |
| DELETE /maintenance-tasks/{id}              | ✅       | ✅       | ❌      | ❌      | ❌               |
| GET /equipment, /equipment-models          | ✅       | ✅       | ❌      | ❌      | ✅               |
| POST/PUT/DELETE /equipment*                 | ✅       | ✅       | ❌      | ❌      | ❌               |

For **Caretaker – PUT status**: allowed only when the task's `AssignedStaffId` matches the
caretaker's own Staff record at the active gym.

### Staff, Finance, Settings

| Endpoint                                    | GymOwner | GymAdmin | Member  | Trainer | Caretaker |
|---------------------------------------------|:--------:|:--------:|:-------:|:-------:|:---------:|
| GET /staff                                  | ✅       | ✅       | ❌      | ❌      | ❌        |
| POST/PUT/DELETE /staff                      | ✅       | ✅       | ❌      | ❌      | ❌        |
| GET /work-shifts                            | ✅       | ✅       | ❌      | 👥 own  | 👥 own    |
| GET /gym-settings                           | ✅       | ✅       | ✅      | ✅      | ✅        |
| PUT /gym-settings                           | ✅       | ✅       | ❌      | ❌      | ❌        |
| GET/PUT /gym-users                          | ✅       | ✅       | ❌      | ❌      | ❌        |
| GET /finance, /invoices                     | ✅       | ✅       | 🔒 own  | ❌      | ❌        |
| GET /memberships, /membership-packages      | ✅       | ✅       | 🔒 own  | ❌      | ❌        |

---

## HTTP Status Codes

| Situation                                          | Status |
|----------------------------------------------------|--------|
| No `Authorization` header                          | 401    |
| Valid JWT but wrong gym context (URL ≠ active gym) | 403    |
| Valid JWT but insufficient role for this operation | 403    |
| Resource not found within the active tenant        | 404    |
| Resource from a different tenant (ID manipulation) | 404    |
