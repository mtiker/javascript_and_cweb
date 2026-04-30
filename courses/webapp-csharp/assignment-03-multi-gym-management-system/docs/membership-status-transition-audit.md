# Membership Status Transition Audit

Date: 2026-04-30

## Current States

`MembershipStatus` values are:

- `Pending`
- `Active`
- `Paused`
- `Expired`
- `Cancelled`
- `Refunded`
- `Renewed`

## Allowed Transitions

| Current | Allowed next states |
| --- | --- |
| `Pending` | `Active`, `Cancelled` |
| `Active` | `Paused`, `Expired`, `Cancelled`, `Refunded`, `Renewed` |
| `Paused` | `Active`, `Cancelled`, `Expired` |
| `Expired` | `Renewed`, `Cancelled` |
| `Cancelled` | `Renewed` |
| `Refunded` | `Renewed`, `Cancelled` |
| `Renewed` | `Active`, `Paused`, `Expired`, `Cancelled` |

No-op transitions are accepted.

## Enforcement

`MembershipService.UpdateMembershipStatusAsync`:
1. authorizes tenant access
2. loads membership by `(gymId, membershipId)`
3. applies member self-access rules
4. validates the transition in BLL
5. updates status and shortens `EndDate` when early-expiring an active future membership
6. commits through Unit of Work

Invalid transitions throw `ValidationAppException` and return API `400 ProblemDetails`.

## Test Evidence

`MembershipFinanceCleanSliceTests.MembershipStatusTransitions_AllowValidAndRejectInvalidTransitions` verifies `Active -> Paused` succeeds and `Paused -> Refunded` fails. Existing membership workflow tests also cover overlap detection and invalid `Pending -> Refunded`.
