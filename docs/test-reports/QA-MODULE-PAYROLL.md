# Payroll audit

## Purpose and entry points

Frontend: `modules/payroll/*` and `modules/payrollsettings/*`.  
Backend: `PayRollController`, `PayRollServices`, salary service, divisor-policy service, attendance summary/penalty services, and hosted payroll work.

## Normal flow

For a closed period, authorized users generate or update a tenant-scoped payroll row, then process a month (or selected employees) by setting `IsProcessed`, `ProcessedAt`, and `ProcessedBy`. Employees without payroll-management permission are limited by `GetEmpPayRollData` to their own processed rows. The self-payslip endpoint uses the current user ID.

## Confirmed controls

- Management endpoints declare Payroll permissions.
- Management payslip generation explicitly looks up the requested employee in the current company.
- Current/future periods are rejected for generation/processing.
- Processed month rows are blocked from update by service checks.

## Confirmed bugs

No fresh confirmed payroll defect is filed in this pass.

## Risks and tests needed

- Generation/update/process comprise separate reads and writes, so two managers can race. Test duplicate generation and process-vs-edit concurrently in an isolated database.
- Month process uses a bulk update after an in-memory read; verify reported count and outcome when a row changes between those operations.
- Validate arithmetic, rounding, negative amounts, joining/exiting month edges, attendance exception handling, and payslip access with isolated fixtures.
- Test that every salary/payslip response excludes fields not intended for employee self-service.
