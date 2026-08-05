# CMS Backend Docs

Architectural reference for the **CMS-Backend-Core** .NET 10 ASP.NET Core API. Written for contributors who need to understand the current implementation without rediscovering it from scratch.

> **Snapshot date:** 2026-07-30. File paths and conventions reflect the codebase at that time. If a path no longer resolves, prefer reading the actual source over trusting the doc.

## How to use these docs

If you only have time to read **one** file, read [`modules.md`](./modules.md) — the catalog of every business domain (controllers + services + entities). Everything else exists to support adding to or modifying that catalog.

Recommended reading order for a new contributor (human or AI):

1. [`architecture.md`](./architecture.md) — solution layout, project map, tech stack, request lifecycle
2. [`modules.md`](./modules.md) — business modules: controllers, services, entities, endpoints
3. [`data-layer.md`](./data-layer.md) — generic Mongo repository, multi-tenancy, soft delete
4. [`auth-and-permissions.md`](./auth-and-permissions.md) — JWT, password hashing, `ModulePermission`, refresh-token flow
5. [`migrations.md`](./migrations.md) — runner mechanics, writing a new migration, existing seeds
6. [`cross-cutting.md`](./cross-cutting.md) — middleware, sanitizer, mail, background jobs, SignalR
7. [`conventions.md`](./conventions.md) — naming, `Result<T>` pattern, error handling, async style
8. [`add-new-feature.md`](./add-new-feature.md) — playbook for adding a new endpoint / module
9. [`calendar-module-current-flow.md`](./calendar-module-current-flow.md) — calendar/holiday/events behavior and links to attendance and payroll
10. [`jobs-module-current-flow.md`](./jobs-module-current-flow.md) — job vacancy lifecycle and public-career connection
11. [`payroll-module-current-flow.md`](./payroll-module-current-flow.md) — payroll generation, locking, and attendance dependency
12. [`profile-module-current-flow.md`](./profile-module-current-flow.md) — career-profile administration and public portal mapping
13. [`permissions-current-flow.md`](./permissions-current-flow.md) — permission model, middleware, and module-action matrix

> Attendance reference: [`attendance-module-current-flow-and-audit.md`](./attendance-module-current-flow-and-audit.md) documents daily attendance, source-linked leave records, monthly locking, payroll dependencies, and operations. For the employee-profile calendar and tenant/ownership behaviour, see [`employee-profile-current-flow.md`](./employee-profile-current-flow.md). For policies, balances, request decisions, notifications and email, see [`leave-management-current-flow.md`](./leave-management-current-flow.md).

### Detailed HR module flows

- [`attendance-leave-profile-tracker-complete-reference.md`](./attendance-leave-profile-tracker-complete-reference.md) - authoritative backend/frontend reference for employee attendance, HR/Admin attendance, correction requests, Leave Management, Profile Leave Tracker, tenant isolation, notifications, email, reconciliation, and payroll dependency
- [`roles-and-permissions-complete-reference.md`](./roles-and-permissions-complete-reference.md) - complete role, permission, module route, tenant and ownership reference
- [`authentication-and-authorization-reference.md`](./authentication-and-authorization-reference.md) - in-depth registration, login, JWT, refresh, recovery, role, permission, ownership and tenant-isolation reference
- [`COMPLETE_PROJECT_DOCUMENTATION.md`](./COMPLETE_PROJECT_DOCUMENTATION.md) - complete implementation map, cross-module lifecycle, tenancy, security, persistence, operations and test strategy
- [`work-from-home-module.md`](./work-from-home-module.md) - Leave Management WFH tab: policy eligibility, weekly quota, optional approval, attendance provenance, clocking, notifications and operational checks
- [`wfh-end-to-end-flow.md`](./wfh-end-to-end-flow.md) - employee-facing end-to-end WFH flow, request lifecycle, attendance creation, and clocking behavior
- [`recruitment-current-flow-and-working-context.md`](./recruitment-current-flow-and-working-context.md) - current authoritative recruitment and public-careers implementation, including six-digit company routing, APIs, data ownership, tokenized resume upload, frontend routes, operations, and validation 
- [`career-page-current-flow.md`](./career-page-current-flow.md) - focused public-careers reference covering six-digit company URLs, publishing rules, public APIs, application/resume security, authenticated handoff, migration, and troubleshooting
- [`leave-attendance-payroll-end-to-end-flow.md`](./leave-attendance-payroll-end-to-end-flow.md) - authoritative end-to-end flow, calculations, permissions, dependencies, failure paths, gaps, and tests
- [`attendance-module-current-flow-and-audit.md`](./attendance-module-current-flow-and-audit.md) - current attendance architecture, source-linked leave records, grid behavior, monthly locking, payroll integration, and operational runbook
- [`leave-management-current-flow.md`](./leave-management-current-flow.md)
- [`payroll-current-flow.md`](./payroll-current-flow.md)
- [`hr-hardening-implementation-report.md`](./hr-hardening-implementation-report.md) - implemented fixes, migration, deployment checks, and remaining risks
- [`calendar-module-current-flow.md`](./calendar-module-current-flow.md)
- [`jobs-module-current-flow.md`](./jobs-module-current-flow.md)
- [`payroll-module-current-flow.md`](./payroll-module-current-flow.md)
- [`profile-module-current-flow.md`](./profile-module-current-flow.md)
- [`permissions-current-flow.md`](./permissions-current-flow.md)

## What this is not

- Not a tutorial. Assumes you know C#, ASP.NET Core, MongoDB, JWT, dependency injection.
- Not generated. Update by hand when you add a controller, service, entity, or change a cross-cutting pattern.
- Not exhaustive of every endpoint. The module catalog in [`modules.md`](./modules.md) lists controllers and notable endpoints; the rest are discoverable via grep.

## When to update these docs

- **New module** (controller + service + entity) → add a section to [`modules.md`](./modules.md).
- **New cross-cutting middleware, hosted service, or SignalR hub** → update [`cross-cutting.md`](./cross-cutting.md).
- **Change to JWT / auth / permission attribute** → update [`auth-and-permissions.md`](./auth-and-permissions.md).
- **New migration** → add to the existing-migrations table in [`migrations.md`](./migrations.md).
- **Renamed, moved, or deleted a file referenced here** → grep this folder for the old path and replace.

## Companion: frontend docs

This repo only documents the **backend**. The React frontend lives in a sibling repo (`CMS-React`). When implementing a feature that needs UI, also add the corresponding service file, route, and translations there. The frontend docs at `CMS-React/docs/` mirror this structure.
