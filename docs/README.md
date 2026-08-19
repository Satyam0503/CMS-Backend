# CMS Backend Documentation

This is the authoritative documentation index for the Codeji CMS backend. Each business module has one primary, in-depth reference covering its flow, responsibilities, dependencies, tenant boundary, and operational concerns. Dated material in `test-reports/` is retained as test evidence, not as a second source of implementation truth.

## Start here

[Project layout](./project-layout.md) provides a backend/frontend folder map, dependency direction, and the main entry points for common changes.

[Project design and UI guide](./project-design-and-ui.md) defines the application shell, shared UI system, responsive rules, and attendance-grid conventions.

1. [Architecture](./architecture.md) — solution map, request lifecycle, and runtime composition.
2. [Business modules](./modules.md) — controller/service/entity catalog.
3. [Data layer](./data-layer.md) — Mongo repository behaviour, tenancy, and soft deletes.
4. [Cross-cutting infrastructure](./cross-cutting.md) — middleware, mail, jobs, and SignalR.
5. [Conventions](./conventions.md) and [adding a feature](./add-new-feature.md) — implementation rules.
6. [Migrations](./migrations.md) — migration runner and seed conventions.

## Authoritative module references

| Module | Primary reference |
|---|---|
| Authentication and authorization | [Authentication and authorization reference](./authentication-and-authorization-reference.md) |
| Roles and permissions | [Roles and permissions reference](./roles-and-permissions-complete-reference.md) |
| Company and master data | [Company module current flow](./company-module-current-flow.md) |
| Employee management | [Employee module current flow](./employee-module-current-flow.md) |
| Employee profile | [Employee profile current flow](./employee-profile-current-flow.md) |
| Attendance | [Attendance module current flow and audit](./attendance-module-current-flow-and-audit.md) |
| Leave management | [Leave management current flow](./leave-management-current-flow.md) |
| Work from home | [Work from home module](./work-from-home-module.md) |
| Office schedules and shift assignment | [Office schedule and shift management](./office-schedule-shifts.md) |
| Calendar and holidays | [Calendar module current flow](./calendar-module-current-flow.md) |
| Payroll | [Payroll module current flow](./payroll-module-current-flow.md) |
| Recruitment | [Recruitment current flow and working context](./recruitment-current-flow-and-working-context.md) |
| Public careers | [Career page current flow](./career-page-current-flow.md) |
| Jobs | [Jobs module current flow](./jobs-module-current-flow.md) |
| Career profile | [Profile module current flow](./profile-module-current-flow.md) |
| Policies | [Policy workflow](./policy-workflow.md) |

## Supporting design references

These are focused specifications or impact analyses, not duplicate module overviews: attendance bulk marking and status settings, employee-specific leave allocation, half-day/WFH segments, payroll LHD/ED impact, notification and company registration, dashboard design, and public career API behaviour. For user-facing styling and responsive layout, use the [project design and UI guide](./project-design-and-ui.md); for the manager/delivery overview, use [Project complete documentation](./Project%20complete%20ducumentation.md).

## Test evidence

Use [test reports](./test-reports/) for dated audits, bug reports, validation evidence, and environment blockers. These reports do not replace the module references above.

## Documentation rules

- Update the module's primary reference whenever its flow, dependency, endpoint ownership, persistence, or tenant boundary changes.
- Keep focused design notes only when they cover a distinct feature or decision; do not create a second module overview.
- Record verification and unresolved runtime evidence in `test-reports/` with a date.
- Prefer source over documentation whenever they differ.
