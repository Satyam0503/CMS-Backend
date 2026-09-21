# Payroll and Salary Data and Rules

Payroll services consume attendance inputs and salary records and persist payroll results through repository entities/services. Do not infer a status transition or calculation formula from a UI label: follow the relevant service method and `AttendancePayrollRules`. Supporting current reference: `../payroll-module-current-flow.md`.

> ⚠ Not conclusively determined from current code in this pass: a single universal payroll transaction boundary. Inspect the invoked service/repository method and Mongo deployment capability before asserting atomicity.
## Lifecycle-aware payroll

Last verified against code: 2026-09-07.

Payroll uses lifecycle employment and payroll dimensions in addition to existing pay rules. Temporary leave and sabbatical do not close payroll automatically; paid/unpaid treatment remains governed by leave and payroll policy. Employees with a completed exit remain included for the exit month when the last working date falls in that month, while future months are excluded after payroll is closed. Final settlement remains part of the authoritative EmployeeExit workflow.
