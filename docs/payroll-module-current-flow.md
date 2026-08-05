# Payroll Module — Current Flow

> Status: Current reference for payroll generation, month locking, payslip creation, and the way payroll depends on attendance and calendar state.

## 1. What this module owns

Payroll is the month-oriented execution layer. It turns:

- salary structure;
- attendance records;
- leave reconciliation;
- holidays and weekly offs;
- penalty exceptions;

into a monthly payroll result and payslip output.

It does not own base salary setup; that lives in the salary module.

## 2. Core files

| Area | File |
|---|---|
| Controller | [`Codeji.CMS.API/Controllers/PayRollController.cs`](../Codeji.CMS.API/Controllers/PayRollController.cs) |
| Auto payroll controller | [`Codeji.CMS.API/Controllers/AutoPayrollController.cs`](../Codeji.CMS.API/Controllers/AutoPayrollController.cs) |
| Service | [`Codeji.CMS.Services/PayRoll/PayRollServices.cs`](../Codeji.CMS.Services/PayRoll/PayRollServices.cs) |
| Auto service | [`Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`](../Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs) |
| Rules | [`PayrollPeriodRules.cs`](../Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs), [`PayrollCalculationRules.cs`](../Codeji.CMS.Services/PayRoll/PayrollCalculationRules.cs) |
| Host | [`PayRollHostedServices.cs`](../Codeji.CMS.Services/PayRoll/PayRollHostedServices.cs) |
| DTOs | [`Codeji.CMS.DTO/PayRoll/`](../Codeji.CMS.DTO/PayRoll/) |

## 3. Permission model

The payroll module uses the `PayRoll` permission family for operational payroll tasks.

The separate payroll-settings flows use:

- `Payroll_Settings.Create`
- `Payroll_Settings.Edit`

## 4. Primary data flow

### 4.1 View employee payroll data

`POST /api/payroll/GetEmpPayRollData`

Flow:

1. Validate that the requested month is closed.
2. Resolve current company and user.
3. Check whether the caller can manage payroll.
4. Return either:
   - all employee payroll rows for authorized payroll managers, or
   - only the current user’s payroll data for non-managers.

### 4.2 Upload payroll payload

`POST /api/payroll/UploadPayloadData`

Flow:

1. Validate month closure.
2. Resolve the company.
3. Upload structured payroll data into the payroll service.

### 4.3 Create or update payroll

`POST /api/payroll/AddUpdatePayRoll`

Flow:

1. Confirm the month is closed.
2. Resolve company context.
3. Save the payroll row for the employee/month pair.

### 4.4 Process a payroll month

`POST /api/payroll/ProcessPayrollMonth`

Flow:

1. Require a closed payroll month.
2. Resolve company and current user.
3. Process selected employee payroll rows.
4. Generate the payroll records and downstream payslips.

### 4.5 Generate payslip PDF

`POST /api/payroll/GeneratePaySlip`
`POST /api/payroll/GenerateEmployeePaySlip`

Flow:

1. Validate that the month is closed.
2. Load the payroll data for the requested employee/month.
3. Render an HTML template into PDF using PuppeteerSharp.
4. Return a downloadable PDF response.

## 5. Month-lock rules

Payroll explicitly rejects processing for:

- the current month;
- any future month.

That rule protects against premature processing before attendance, leave, and exception handling are complete.

## 6. Dependency chain

Payroll depends on:

- Attendance: monthly summaries, locking, exceptions, and worked-day data
- Leave Management: approved leave entries and reconciliation into attendance
- Calendar: holidays
- Weekly Off settings: non-working days
- Salary: compensation base values
- Policy / payroll divisor settings: company-specific calculations

## 7. How monthly payroll actually closes

The expected sequence is:

1. Attendance is captured or reconciled.
2. Missing data and exceptions are resolved.
3. Attendance month is locked.
4. Salary structure is available for the period.
5. Payroll month is processed.
6. Payslip generation uses the closed payroll data.

## 8. Risks and watch-outs

- Closing payroll too early will yield incomplete or rejected processing.
- Payroll correctness is sensitive to attendance quality.
- Since the month-close flow depends on multiple modules, one bad upstream setting can block the entire payroll cycle.

