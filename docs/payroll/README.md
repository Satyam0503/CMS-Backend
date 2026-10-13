# Payroll and Salary

`PayRollController`, `AutoPayrollController`, `PayrollDivisorPolicyController`, and `SalaryController` own payroll runs, automatic processing, divisor policy, and salary history/updates. Services are in `Codeji.CMS.Services/PayRoll`. Existing verified references: `../payroll-module-current-flow.md` and `../leave-attendance-payroll-end-to-end-flow.md`.

Lifecycle integration verified 2026-09-07: payroll selection consults the additive payroll/employment lifecycle rules. Temporary leave and sabbatical do not close payroll automatically; completed exits remain eligible for the exit month when applicable and move through final settlement.

Offboarding final settlement consumes processed payroll through the employee-exit service; it is documented as a dependency, not a second payroll implementation.
