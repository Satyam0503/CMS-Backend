# HRMS Offboarding, Payroll, and Employee Lifecycle

This folder is the implementation-aligned reference for the Codeji CMS HRMS offboarding, payroll, and employee lifecycle modules. It describes the current backend and React implementation, including user journeys, permissions, API boundaries, persisted records, state changes, cross-module effects, operational behavior, and known limitations.

## Documents

- [Offboarding](offboarding.md) — resignation, review, notice period, handover, clearance, assets, access, finance, benefits, settlement, documents, former-employee access, reminders, withdrawal, cancellation, completion, and rejoin.
- [Payroll](payroll.md) — payroll settings, monthly calculation, attendance readiness, locking, manual/imported payroll, processing, payslips, permissions, and offboarding interaction.
- [Employee lifecycle](employee-lifecycle.md) — lifecycle dimensions, lifecycle policy configuration, approved-leave cases, evidence, return tracking, reconciliation, and offboarding boundaries.

## System ownership map

| Concern | Authoritative record/service | UI entry point |
|---|---|---|
| Employment and availability dimensions | `EmpUser.Lifecycle` and `EmployeeLifecycleTransitionService` | Employees profile and lifecycle workspace |
| Leave entitlement and paid/unpaid policy | `LeavePolicy` and `LeaveManagementService` | Leave Management → Leave Policies |
| Lifecycle-linked absence case | `EmployeeLifecycleCase` and `EmployeeLifecycleCaseService` | Employees → Lifecycle cases |
| Resignation and offboarding workflow | `EmployeeExit` and `EmployeeService` | Employee profile, Manage Offboarding, Offboarding cases |
| Attendance source rows and payroll readiness | Attendance repositories, reconciliation and readiness services | Attendance and payroll processing |
| Monthly payroll result | `EmpPayRoll`, `PayRollServices`, and `AutoPayRollServices` | Payroll and employee payslip UI |
| Final settlement | `EmployeeFinalSettlement` linked to `EmployeeExit` | Manage Offboarding |

## End-to-end flow

```text
Company settings and role permissions
              |
              v
Employee + office schedule + salary settings
              |
       +------+-----------------------+
       |                              |
       v                              v
Leave request                   Resignation / HR exit
       |                              |
       v                              v
Approval and balance            EmployeeExit workflow
       |                              +--> notice/handover/clearance/evidence
       v                              +--> final settlement/documents
Attendance reconciliation             |
       |                              v
       v                       completed exit + account closure
Approved and locked attendance        |
       +---------------+--------------+
                       v
                Payroll calculation
                       |
                       v
             Processed payroll + payslip
```

## Shared security rules

- Authentication uses the current JWT identity and company context.
- Company ownership is resolved server-side; clients must not supply authoritative tenant or audit fields.
- `ModulePermission` attributes protect controller actions, while services repeat sensitive ownership and operation checks.
- Data queries include `CompanyId` and deletion/activity filters.
- Version fields provide optimistic concurrency for lifecycle and offboarding mutations.
- Evidence is stored outside public static paths and downloaded through authorized, tenant-scoped endpoints.
- UI visibility is convenience and defense in depth; server authorization is the security boundary.

## Cross-module invariants

1. A leave policy does not directly change employment status. It defines entitlement, paid/unpaid behavior, and attendance mapping.
2. A lifecycle policy links an active company leave policy to an employment-impacting absence type.
3. Only an approved linked leave can create a lifecycle case and temporary availability change.
4. Reversing an approved leave cancels its active lifecycle case and restores availability when the case still owns that state.
5. Temporary lifecycle absence does not automatically stop payroll. Payroll eligibility, paid/unpaid attendance, and payroll status remain separate controls.
6. `EmployeeExit` is the authoritative offboarding aggregate. Compatibility fields on `EmpUser` are projections, not a replacement for the exit case.
7. Exit completion moves payroll to final settlement and closes the account; it does not erase payroll, attendance, or audit history.
8. Payroll finalization requires a completed month and reviewed/locked attendance.

## Repository locations

Backend:

- Controllers: `Codeji.CMS.API/Controllers`
- Services: `Codeji.CMS.Services/Employees`, `Codeji.CMS.Services/PayRoll`, `Codeji.CMS.Services/Attendance`
- Entities: `Codeji.CMS.Repository/Entities/Employees`
- Contracts: `Codeji.CMS.DTO/Employee`, `Codeji.CMS.DTO/PayRoll`, `Codeji.CMS.DTO/LeaveManagement`
- Migrations: `Codeji.CMS.Migrations/Migrations`
- Tests: `Codeji.CMS.Services.Tests`, `Codeji.CMS.API.Tests`

Frontend (`../CMS-React`):

- Employee/lifecycle APIs and screens: `src/app/modules/employees`
- Offboarding profile UI: `src/app/modules/users/components`
- Payroll UI: `src/app/modules/payroll`
- Payroll settings UI: `src/app/modules/payrollsettings`
- Leave policy/request UI: `src/app/modules/leaves`
- Attendance UI: `src/app/modules/attendance`

## Release verification checklist

- Run backend service and API tests and compile the API.
- Run frontend typecheck, focused tests, full tests, lint, and production build.
- Verify employee, reporting-manager, HR, payroll-operator, offboarding-operator, and administrator roles.
- Verify tenant isolation with records from at least two companies.
- Verify policy save, linked leave approval, lifecycle case creation, evidence, return, reversal, resignation, withdrawal, exit completion, settlement, payroll processing, and payslip generation.
- Confirm desktop, tablet, and mobile behavior.

## Known architectural risk

Several workflows span multiple MongoDB writes. The repository abstraction does not consistently expose a shared transaction/session boundary. Idempotency, version checks, reconciliation, and outbox processing reduce risk, but they do not prove atomic rollback across every leave, attendance, employee, exit, payroll, notification, and audit write. Production rollout should monitor partial failures and retain repair/reconciliation procedures.
