# CMS Backend Docs

Architectural reference for the **CMS-Backend-Core** .NET 8 ASP.NET Core API. Written for AI agents and humans who need to ramp up on the codebase without re-exploring it from scratch.

> **Snapshot date:** 2026-07-22. File paths and conventions reflect the codebase at that time. If a path no longer resolves, prefer reading the actual source over trusting the doc.

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

### Detailed HR module flows

- [`leave-management-current-flow.md`](./leave-management-current-flow.md)
- [`attendance-current-flow.md`](./attendance-current-flow.md)
- [`payroll-current-flow.md`](./payroll-current-flow.md)
- [`hr-hardening-implementation-report.md`](./hr-hardening-implementation-report.md) - implemented fixes, migration, deployment checks, and remaining risks

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
