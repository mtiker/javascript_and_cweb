# Modules.MembershipFinance - Application

Module-internal handlers live here.

Implemented module-owned slices:
- membership package CRUD, validation, tenant authorization, used-package conflict checks, and repository/UOW persistence

Slices that still use transitional BLL workflow services:
- membership lifecycle
- payments and refunds
- finance workspace
- membership workflow extensions and mapping
