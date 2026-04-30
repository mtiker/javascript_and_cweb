# Finance Rules Audit

Date: 2026-04-30

## Scope

This audit covers internal finance behavior for invoices, invoice lines, invoice payments, refunds, and finance workspace balances. No external payment provider is implemented.

## Rules

Invoice creation:
- caller must be `GymOwner` or `GymAdmin`
- member must belong to the active gym
- at least one invoice line is required
- line description is required
- line quantity must be greater than zero
- line unit price cannot be negative
- credit lines reduce the invoice total
- total cannot go below zero
- blank currency defaults to `EUR`; supplied currency is uppercased

Payment posting:
- caller must have tenant access
- invoice must belong to the active gym
- member self-access rules apply
- payment amount must be greater than zero
- payment amount cannot exceed outstanding amount
- internal `Payment` status is `Completed`
- no external provider call is made

Refund posting:
- same access rules as payment posting
- refund amount must be greater than zero
- refund amount cannot exceed paid amount
- internal `Payment` status is `Refunded`
- refund increases outstanding amount and lowers paid amount

Outstanding balance:
- invoice outstanding amount is updated on every payment/refund
- workspace outstanding balance is the sum of mapped invoice outstanding amounts
- refund credits are reported from invoice payment ledger entries marked `IsRefund`

## Access Control

Finance workspace reads call `EnsureTenantAccessAsync` and then `EnsureMemberSelfAccessAsync` where a member-specific resource is requested. A member role can see only its own finance workspace and invoices. Admin and owner roles can inspect member workspaces inside the active gym.

## Test Evidence

`MembershipFinanceCleanSliceTests` covers invoice creation, payment posting, refund posting, outstanding balance calculation, refund credit reporting, and member workspace access rejection.
