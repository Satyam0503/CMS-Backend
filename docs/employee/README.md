# Employee

The employee module owns directory/profile data and exposes exit integration through `UserController` and `EmployeeService`. Primary React entry: `../CMS-React/src/app/modules/users/UserProfile.tsx`; primary contracts: `Codeji.CMS.DTO/Employee`; primary entities: `Codeji.CMS.Repository/Entities/Employees`.

Do not treat `EmpUser` employment compatibility fields as an alternative offboarding workflow source. Exit behaviour is documented in `../offboarding/README.md`. Existing checked references: `../employee-module-current-flow.md` and `../employee-profile-current-flow.md`.
## Employee lifecycle status model

Last verified against code: 2026-09-07.

Employee status is additive and dimension-specific. Do not use the legacy `EmpUser.Status` boolean as a former-employee indicator.

- Employment: `Active`, `NoticePeriod`, `Suspended`, `Resigned`, `Terminated`
- Availability: working, leave types, sabbatical, unauthorized absence, absconding review, and temporary suspension
- Account: active, locked, temporarily disabled, suspended, or closed
- Payroll: active, hold, paused, final settlement, or closed
- Offboarding, former portal, and alumni are separate relationship/workflow dimensions

Completed exits remain authoritative in `EmployeeExit`; rejoin is only valid after a completed former exit. See [../employee-lifecycle-status-model.md](../employee-lifecycle-status-model.md) and [../employee-lifecycle-production-todo.md](../employee-lifecycle-production-todo.md).
