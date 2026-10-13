# Payroll Module

## Purpose

Payroll converts employee salary settings and approved, locked attendance for a completed month into tenant-scoped monthly payroll records and downloadable payslips. It supports automatic calculation, manual/imported rows, controlled finalization, employee self-service, HR payslip access, and offboarding final-settlement evidence.

## Preconditions

- The employee belongs to the current company and has applicable salary settings.
- The payroll month is completed; current/future months are rejected server-side.
- Attendance is reconciled, approved, and locked.
- Blocking attendance correction requests are resolved.
- The caller has Payroll or Payroll Settings permission for management actions.
- Processed payroll is not overwritten by normal upload/edit operations.

## End-to-end flow

```text
Salary settings + schedule/calendar + attendance status rules
                         |
Approved leave/WFH --> attendance reconciliation
                         |
Exception review --> validate and lock completed month
                         |
Payroll divisor + earnings + deductions + unpaid/penalty days
                         |
Generate monthly payroll rows
                         |
HR review and allowed corrections
                         |
Process payroll (IsProcessed = true)
                         |
Employee/HR payslip PDF and offboarding settlement evidence
```

## Attendance and calculation ownership

Payroll does not reinterpret leave requests. Leave and WFH write or reconcile their own source-owned attendance information. Attendance status settings provide paid/unpaid fractions. `AttendancePayrollReadinessService` blocks selected employees when unresolved correction requests exist.

Automatic generation calls `IAttendancePenaltyService.ValidateAndLock` for the exact month. `AutoPayRollServices` reads approved and locked summaries, attendance rows, status rules, employment dates, salary settings, and divisor policy.

Calculation includes earnings, deductions, employment-period proration, configured unpaid fractions, and approved attendance penalties. `PayrollCalculationRules.TotalLossOfPayDays` consolidates unpaid and penalty days. Loss of pay is represented once in deductions so it is not subtracted twice.

## Processing paths

### Automatic monthly generation

`POST api/AutoPayroll/GeneratePayRollMonthly`

- Payroll Settings Create/Edit required.
- Completed month required.
- Validates and locks attendance before calculation.
- Generates company-scoped rows and returns correlation IDs for unexpected failures.

### Upload payroll data

`POST api/PayRoll/UploadPayloadData`

- Payroll Create/Edit required.
- Accepts mapped rows from the Excel upload UI.
- Completed month required.
- Existing processed rows cannot be overwritten.

### Add or update one row

`POST api/PayRoll/AddUpdatePayRoll`

- Payroll Create/Edit required.
- Company comes from authenticated context.
- Only completed-month, unprocessed data can be maintained normally.

### Finalize payroll

`POST api/PayRoll/ProcessPayrollMonth`

- Payroll Create/Edit required.
- Accepts a month and optional employee IDs.
- Runs attendance readiness for the selected population.
- Marks pending rows processed with a filtered server-side update.
- Already-processed rows are not reopened.

## Retrieval and payslips

`POST api/PayRoll/GetEmpPayRollData` requires Payroll View. Users with Create/Edit can manage broader company data; restricted viewers receive processed data and are scoped to their own identity where applicable.

`POST api/PayRoll/GeneratePaySlip` derives the employee from the authenticated token and returns only that employee's processed closed-month payslip.

`POST api/PayRoll/GenerateEmployeePaySlip` requires Payroll View and supports authorized HR/admin download for a same-company employee.

PDF output uses `Codeji.CMS.API/Templates/SalarySlipTemplate.html` and `SalarySlipTemplateModel`, exposes `Content-Disposition`, and returns conflict when the processed record is unavailable or unsuitable.

## Lifecycle and offboarding effects

- Temporary Medical, Maternity, Paternity, Long-Term Leave, or Sabbatical does not automatically stop payroll.
- Paid/unpaid treatment comes from the linked Leave Policy and resulting attendance status.
- Payroll Hold/Paused is a separate lifecycle dimension.
- Exit-month eligibility follows employment-period rules; future months after effective exit are excluded.
- Completed exit sets lifecycle payroll status to FinalSettlement.
- `EmployeeFinalSettlement` can reference source payroll ID/month but does not replace or rewrite the monthly payroll aggregate.

## UI flow

The React Payroll Management screen provides a closed-month picker, server data load, employee payroll table, processed state, upload, add/edit, process confirmation, and employee payslip action. Controls are permission-aware and show loading/error/disabled outcomes.

The upload modal reads the provided template, maps rows, validates input, previews errors, and sends accepted rows. The process modal confirms the month/population and reports server readiness or completion. The employee profile provides self-service month selection and payslip download.

Payroll Settings is separate from monthly Payroll. It maintains salary structures and employee salary data. The month-effective divisor policy is read/written through `api/payroll/divisor-policy` with Payroll Settings permissions.

## API inventory

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `api/PayRoll/GetEmpPayRollData` | Retrieve scoped payroll |
| POST | `api/PayRoll/UploadPayloadData` | Import unprocessed rows |
| POST | `api/PayRoll/AddUpdatePayRoll` | Maintain one unprocessed row |
| POST | `api/PayRoll/ProcessPayrollMonth` | Finalize month/population |
| POST | `api/PayRoll/GeneratePaySlip` | Employee self-service PDF |
| POST | `api/PayRoll/GenerateEmployeePaySlip` | HR/admin employee PDF |
| POST | `api/AutoPayroll/GeneratePayRollMonthly` | Lock attendance and calculate |
| GET | `api/payroll/divisor-policy?month=...` | Effective divisor settings |
| POST | `api/payroll/divisor-policy` | Save divisor settings |

## Data and integrity

`EmpPayRoll` is the monthly result and stores company, employee/user, month, earnings, deductions, and `IsProcessed`. Salary settings and divisor policies are source configuration; attendance summaries/rows are source evidence; final settlement is offboarding-owned.

Security invariants:

- Company is always derived server-side.
- Period closure is checked server-side.
- Self-service identity is token-derived.
- HR payslip requires Payroll View and tenant ownership.
- Processed rows are protected from ordinary mutation.
- Attendance readiness prevents processing over unresolved corrections.
- Unexpected failures return a correlation ID without exposing internal exceptions.

## Operator troubleshooting

| Symptom | Cause/action |
|---|---|
| Current/future month rejected | Select a completed month |
| Processing blocked | Resolve attendance corrections and approve/lock attendance |
| Row cannot be edited | It is processed; use an authorized revision/correction process |
| Payslip returns conflict | Verify employee/month and processed payroll presence |
| Unexpected 500 | Trace server logs with the returned correlation ID |

## Implementation map

Backend: `PayRollController.cs`, `AutoPayrollController.cs`, `PayrollDivisorPolicyController.cs`, `PayRollServices.cs`, `AutoPayRollServices.cs`, `PayrollCalculationRules.cs`, `PayrollPeriodRules.cs`, `AttendancePayrollReadinessService.cs`, `AttendancePayrollRules.cs`, and `EmpPayRoll.cs`.

Frontend: `PayRollManagement.tsx`, `PayRollTable.tsx`, `ProcessPayrollModal.tsx`, `UploadPayRollData.tsx`, `AddUpdateEmployeePayRoll.tsx`, payroll `services.ts`, `PayrollSettings.tsx`, and `ProfileCard.tsx`.

## Validation and limitations

Backend tests cover processing windows, payroll calculation, payroll-period employment rules, and attendance/payroll rules. Frontend E2E coverage exists in `e2e/payroll`. Release acceptance should test automatic/manual/import parity, partial selection, repeat processing, paid/unpaid examples, exit month, tenant isolation, payslip authorization/content, and large-company performance.

A complete audited reopen/revision workflow is not demonstrated by normal endpoints. Attendance locking, payroll generation, and downstream writes do not universally share one MongoDB transaction.
