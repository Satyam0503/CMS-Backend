# Leave, Attendance, and Payroll: Detailed End-to-End Working Flow

## 1. Purpose

This document describes the current implemented behavior of the HRMS Leave Management, Attendance, Payroll, and Payroll Settings modules. It is based on the ASP.NET Core backend and the connected React frontend as of 22 July 2026.

It explains:

- what each module owns;
- which data is the source of truth;
- permissions and tenant isolation;
- detailed user and backend flows;
- status transitions and locking;
- how leave becomes attendance;
- how attendance becomes payroll;
- how payroll becomes visible to an employee;
- calculations, validations, failure paths, and remaining gaps;
- a recommended monthly operating sequence and test checklist.

## 2. Complete business flow

```text
Company configuration
  |-- Leave policies and balances
  |-- Attendance status rules
  |-- Weekly offs and holidays
  |-- LHD/ED penalty policy
  |-- Salary structures
  `-- Payroll divisor policy
             |
             v
Employee requests leave
             |
             v
HR reviews request
  |-- Rejected -> balance unchanged/refunded; linked attendance reversed
  `-- Accepted -> balance deducted; leave attendance reconciled
                                      |
                                      v
Daily attendance records <------ manual attendance marking
             |
             v
Monthly attendance validation
  |-- structural errors -> correct attendance and recalculate
  |-- policy exception -> HR decision
  `-- clean month -> approve and lock summaries
                                      |
                                      v
Generate payroll for a closed month
  |-- locked attendance summary required
  |-- effective salary required
  |-- unresolved exception blocks generation
  `-- creates unprocessed payroll records
                                      |
                                      v
HR reviews or edits payroll draft
                                      |
                                      v
Process payroll month
  `-- sets IsProcessed = true for the selected company/month
                                      |
                                      v
Employee can see that month's payroll and download the payslip
```

The important distinction is:

- **Generate payroll** calculates and saves a draft payroll record.
- **Process payroll** publishes that saved record to employees.
- An employee cannot see an unprocessed payroll record.

## 3. Identity, tenant, and permission model

### 3.1 Identity fields

The system commonly uses two employee identifiers:

| Field | Meaning |
|---|---|
| `UserId` | Immutable application/user identity used for authentication and ownership |
| `EmployeeId` | Company-facing employee code used by attendance and payroll business records |
| `CompanyId` | Tenant boundary that must be included in every company-scoped operation |

Code must not assume `EmployeeId` is globally unique. A safe business lookup normally includes `CompanyId`, and user-specific access also includes `UserId`.

### 3.2 Permissions

The application uses module/action permissions rather than job titles alone.

| Module | View | Create | Edit | Delete |
|---|---|---|---|---|
| Leave Management | View policies, requests, balances | Create policies/requests/balances | Update policies, requests, decisions, balances | Withdraw/delete eligible requests |
| Attendance | View attendance and exceptions | Mark attendance | Update attendance, review exceptions, validate/lock | No complete active delete flow |
| PayRoll | View payroll | Import/create payroll | Edit and process payroll | No central payroll delete workflow described here |
| Payroll Settings | View settings | Generate/configure where permitted | Change settings/generate where permitted | Setting-specific |

For payroll reads, there is an additional ownership rule:

1. A user with PayRoll Create or Edit is treated as a payroll manager and may retrieve company payroll data.
2. A user without those management permissions is restricted to their own `UserId`.
3. Non-managers receive only records where `IsProcessed = true`.

This means menu visibility is not the security boundary. The API permission and ownership checks are the real boundary.

## 4. Leave Management

## 4.1 Main data

### LeavePolicy

A leave policy defines:

- name and code;
- active status;
- accrual period and accrual amount;
- maximum balance;
- carry-over permission and limit;
- minimum advance notice;
- whether weekends and holidays count as leave;
- the mapped `AttendanceStatusCode` used after approval.

The attendance mapping is essential. Without it, an approved request cannot reliably become a payroll-consumable attendance record.

The mapped status must:

- belong to the authenticated company;
- be active;
- be a no-time status, because a leave day does not have check-in/check-out times.

Examples are `CL`, `SL`, `EL`, `UL`, `COMP-OFF`, `CL-HALF`, and `SL-HALF`.

### EmployeeLeaveBalance

This stores a balance per employee and leave policy:

- available `Balance`;
- `UsedBalance`;
- company, user, and policy identifiers;
- accrual tracking such as `LastAccrual`.

### LeaveRequest

Important fields include:

- employee/user identity;
- leave policy;
- start and end dates;
- half-day indicator;
- calculated total days;
- reason;
- status;
- reviewer, comment, and review timestamp;
- version used by reconciliation.

## 4.2 Leave policy creation

Endpoint: `POST /api/LeaveManagement/CreateLeavePolicy`

Required permission: `LeaveManagement.Create`

Flow:

1. Validate the request model.
2. If accrual is monthly or yearly, require a positive accrual amount.
3. If carry-over is enabled, require a carry-over limit.
4. If carry-over is disabled, clear the carry-over limit.
5. Resolve `CompanyId` from the authenticated context.
6. Validate the attendance status mapping.
7. Save the policy in the company scope.
8. Create initial balance rows for eligible employees using the policy accrual amount.

## 4.3 Leave balance accrual

`LeaveAccrualHostedService` invokes the leave accrual flow in the background.

For monthly policies:

1. Load balances for the policy and company.
2. Skip a balance already accrued for the current month.
3. Add `AccrualAmount`.
4. Cap the result at `MaxBalance`, when configured.
5. Update `LastAccrual` to prevent duplicate accrual.

For yearly policies:

1. Determine the carry-over amount if carry-over is allowed.
2. Cap carry-over using `CarryOverLimit`.
3. Add the annual accrual amount.
4. Apply `MaxBalance`.
5. Update the yearly accrual marker.

The update filter includes the previous accrual timestamp, which helps prevent the same period from being applied repeatedly.

## 4.4 Employee creates a leave request

Endpoint: `POST /api/LeaveManagement/CreateLeaveRequest`

Required permission: `LeaveManagement.Create`

The controller overrides the submitted user ID with the authenticated user ID, preventing an employee from applying as another employee through the request body.

Detailed flow:

1. Reject an end date earlier than the start date.
2. Confirm the authenticated user is an active employee of the authenticated company.
3. Load the employee balance for the requested policy.
4. Confirm the policy exists, belongs to the company, and is active.
5. Reject overlap with an existing Pending or Accepted request.
6. Validate the policy's minimum notice period.
7. Calculate included leave dates through the company working calendar.
8. Respect the leave policy's weekend and holiday inclusion settings.
9. If half-day, use the configured fractional total.
10. Reject a request whose calculated total exceeds the available balance.
11. Save the request as Pending.

Balance is checked at request time but deducted at acceptance time. This is necessary because multiple pending requests can exist over time, so acceptance rechecks the balance.

## 4.5 Request states

```text
Pending
  |-- employee withdrawal --> Withdrawn
  |-- reviewer rejection ---> Rejected
  `-- reviewer acceptance --> Accepted

Accepted
  `-- later rejection ------> Rejected + balance refund + attendance reversal

Rejected
  `-- later acceptance -----> Accepted + balance deduction + reconciliation
```

The current status service rejects transitions to Pending or Withdrawn through the reviewer endpoint. Employee deletion/withdrawal is allowed only while the request is Pending.

## 4.6 HR accepts a request

Endpoint: `PATCH /api/LeaveManagement/LeaveRequest/{id}/Status`

Required permission: `LeaveManagement.Edit`

Flow:

1. Load the request using `CompanyId + LeaveRequestId`.
2. Reject invalid or duplicate status transitions.
3. Load the employee balance using company, user, and policy.
4. Recheck available balance.
5. Subtract `TotalDays` from `Balance`.
6. Add `TotalDays` to `UsedBalance`.
7. Store reviewer, comment, timestamp, status, and incremented version.
8. Save the leave request.
9. Save the updated balance.
10. Call `ReconcileAcceptedLeaveAsync`.
11. Send the leave notification if the full flow succeeds.

## 4.7 Accepted leave to attendance reconciliation

The reconciliation service converts approved leave into real attendance rows.

For every included leave date:

1. Reload the company-scoped request, employee, policy, and attendance status.
2. Calculate included dates using weekly offs, holidays, and policy inclusion flags.
3. Look for an existing attendance row using company, user, and date.
4. If there is no row, create a leave-sourced attendance row.
5. If the row was previously created by the same leave request, update it idempotently.
6. Store:
   - `SourceType = LEAVE`;
   - `SourceId = LeaveRequestId`;
   - `SourceVersion = LeaveRequest.Version`;
   - mapped attendance status;
   - employee and company identity.
7. Invalidate affected unlocked monthly summaries so they will be rebuilt.

### Conflict behavior

If a manual or differently sourced attendance row already exists, the reconciliation does not overwrite it. Instead it creates a blocking `LEAVE_ATTENDANCE_CONFLICT` exception containing:

- attendance date;
- existing attendance ID and status;
- requested leave status;
- leave request ID and version.

That conflict must be resolved before the month can lock and payroll can be generated.

## 4.8 Rejection after acceptance

When Accepted changes to Rejected:

1. Add the request total back to available balance.
2. Subtract it from used balance.
3. Increment and save the request version.
4. Delete only attendance records linked through `SourceType=LEAVE`, the request ID, and an allowed source version.
5. Do not delete unrelated manual attendance.
6. Invalidate affected unlocked summaries.

## 4.9 Leave gaps and risks

- Leave request, balance, attendance, summary, and notification updates are multi-document operations without a Mongo transaction exposed by the repository abstraction. A failure can leave partial state.
- Some older leave read/balance queries do not consistently include `CompanyId` in every repository predicate. These should be hardened even where IDs are currently obtained from company-scoped employee lists.
- Policy update effects on already accepted future leave need an explicit reconciliation/version policy.
- Employee self-service Create/Edit permissions are shared with broader module actions; more granular `ApplyOwn`, `ViewOwn`, and `ManageAll` capabilities would be clearer.

## 5. Attendance

## 5.1 Attendance sources

An attendance row may originate from:

- manual HR/admin marking;
- bulk marking initiated by the frontend;
- accepted leave reconciliation;
- imported or migration data.

`SourceType`, `SourceId`, and `SourceVersion` identify automated ownership and prevent one workflow from silently overwriting another.

## 5.2 Attendance status settings

Each company status defines:

| Property | Effect |
|---|---|
| Code | Stored daily attendance value |
| Name | Display label |
| Active | Whether the status may be used |
| RequiresTime | Whether check-in and check-out are mandatory |
| PaidDayFraction | Paid contribution, normally 0, 0.5, or 1 |
| UnpaidDayFraction | Unpaid contribution, normally 0, 0.5, or 1 |
| DisplayOrder | UI ordering |

The sum of paid and unpaid fractions cannot exceed one. Fractions cannot be negative.

Common examples:

| Code | Paid | Unpaid | Requires time |
|---|---:|---:|---:|
| P | 1 | 0 | Yes |
| A | 0 | 1 | No |
| CL / SL / EL | 1 | 0 | No |
| HD / LHD / WFH-HD | 0.5 | 0.5 | Yes |
| WFH | 1 | 0 | Yes |
| COMP-OFF | 1 | 0 | No |

## 5.3 Calendar configuration

Working-day behavior depends on:

- company weekly-off settings;
- one-time and recurring company holidays;
- employee joining date;
- employee exit date;
- leave-policy inclusion rules.

Weekly offs and holidays are not ordinary editable attendance days.

## 5.4 Attendance page load

For the selected month, the frontend loads:

1. employees accessible to the user;
2. active attendance statuses;
3. weekly-off configuration;
4. company holidays;
5. approved leave context;
6. attendance rows for the selected employees and date range;
7. monthly exception and lock information where applicable.

The grid is a display/composition layer. Stored attendance rows remain the backend source of truth for validation and payroll.

## 5.5 Manual attendance creation

Endpoint: `POST /api/admin/attendance`

Required permission: `Attendance.Create`

Flow:

1. Resolve authenticated company.
2. Confirm the employee belongs to that company and is active.
3. Normalize the attendance date.
4. Reject a locked month.
5. Reject a weekly off or holiday.
6. Load the active company status.
7. If the status requires time, validate `HH:mm` check-in and check-out.
8. Reject check-out at or before check-in.
9. Calculate total hours using the current break rule.
10. Derive late/early indicators where applicable.
11. Upsert using company, user, and date.

## 5.6 Attendance update

Endpoint: `PUT /api/admin/attendance/{userId}`

Required permission: `Attendance.Edit`

The update repeats tenant, employee, calendar, lock, status, and time validation. A locked month cannot be changed through the ordinary endpoint.

## 5.7 Bulk attendance

The current frontend bulk workflow sends multiple attendance operations. It is not a single transactional backend batch.

Consequences:

- some rows may succeed before another fails;
- the UI must report partial results clearly;
- production hardening should add validate-all and controlled bulk-write behavior.

## 5.8 Monthly validation

Endpoint: `POST /api/attendance-penalty/month/validate-lock`

Required permission: `Attendance.Edit`

Validation is intended for a completed payroll month.

Flow:

1. Normalize the selected month to its first day.
2. Identify employees eligible in that month using joining/exit dates.
3. Calculate working dates from weekly offs and holidays.
4. Load company-scoped daily attendance.
5. Evaluate each row using attendance status rules.
6. Identify structural issues:
   - missing attendance;
   - missing checkout;
   - invalid time order;
   - duplicates or leave conflicts.
7. Calculate monthly totals:
   - present and WFH;
   - paid and unpaid leave;
   - half days and absences;
   - paid/unpaid fractions;
   - LHD and ED counts.
8. Create or refresh `MonthlyAttendanceSummary` while it is unlocked.
9. Create/update exceptions for unresolved issues.
10. Resolve structural exceptions automatically after their underlying data is corrected and validation runs again.
11. Stop if blocking Pending Review exceptions remain.
12. Otherwise mark summaries Approved and Locked with actor, time, and version.

## 5.9 LHD/ED penalty flow

The penalty policy defines a combined monthly late-arrival-half-day and early-departure limit and an effective date.

Recalculation:

1. Count LHD and ED occurrences in the month.
2. Compare the combined count with the allowed count.
3. Create `LHD_ED_LIMIT_EXCEEDED` when the limit is exceeded.
4. If a previously open exception no longer applies, cancel/resolve it.

HR review choices:

| Decision | Payroll effect |
|---|---|
| WAIVE | No extra loss-of-pay days |
| WARNING_ONLY | No extra loss-of-pay days |
| HALF_DAY_LOP | Add 0.5 loss-of-pay day |
| FULL_DAY_LOP | Add 1 loss-of-pay day |
| CUSTOM | Add the approved custom fraction |

Review uses the exception version to prevent two reviewers from silently overwriting each other.

## 5.10 Lock meaning

After locking:

- normal attendance creation/update for the month is rejected;
- payroll may consume the summary;
- the summary is the approved monthly checkpoint;
- a complete audited reopen workflow is not currently implemented.

## 5.11 Attendance gaps and risks

- The unconditional one-hour break deduction can be incorrect for short or half-day shifts.
- View permission is broad; the API needs clearer View Own versus View All capability.
- Status settings GET and save authorization is not completely consistent: GET is authenticated, while save uses AdminOnly.
- There is no complete audited month reopen/correction/relock flow.
- Bulk marking is non-transactional.
- Delete permission has no clearly corresponding attendance delete workflow.

## 6. Payroll Settings

Payroll depends on configuration outside the payroll record itself.

### 6.1 Salary structure

An employee needs a salary structure effective during the selected payroll month. It supplies values such as:

- basic pay;
- HRA;
- LTA;
- other allowances;
- bonus;
- insurance, EPF, ESIC, and other configured deductions.

Missing salary structure blocks automatic payroll generation.

### 6.2 Payroll divisor policy

The divisor determines per-day salary used for loss-of-pay calculations.

Conceptually:

```text
Per-day basic salary = Monthly basic salary / resolved divisor
Loss of pay amount   = Loss-of-pay days * per-day basic salary
```

The resolved divisor may use calendar days or working days according to the effective company policy. A zero or invalid divisor blocks generation.

### 6.3 Attendance penalty policy

Although configured from attendance settings, this policy directly affects payroll through approved penalty-day deductions.

## 7. Payroll

## 7.1 Payroll record lifecycle

```text
No record
   |
   | Generate automatically / Import / Add manually
   v
Draft payroll record (IsProcessed = false)
   |
   | HR review and allowed edits
   v
Process payroll month
   |
   v
Published payroll record (IsProcessed = true)
   |
   `-- visible to employee and available for payslip generation
```

Processed payroll cannot be edited or regenerated for that company/month through normal flows.

## 7.2 Closed-period rule

Payroll generation, retrieval, upload, editing, processing, and payslip operations are restricted to completed months where applicable.

Current and future months are rejected. For example, in July 2026, June 2026 is closed, while July and later months are not.

## 7.3 Automatic payroll generation

Endpoint: `POST /api/AutoPayroll/GeneratePayRollMonthly`

Required permission: Payroll Settings Create or Edit.

### Pre-validation phase

Before the first payroll write, the service validates every eligible employee:

1. employee is active and belongs to the company;
2. joining date is valid;
3. employee was eligible for at least part of the month;
4. exit date is valid and not before joining date;
5. approved and locked monthly attendance summary exists;
6. effective salary structure exists;
7. no Pending Review attendance exception exists;
8. the month has not already been processed.

If any validation fails, generation returns a combined error and writes no payroll rows in that run.

### Employee calculation phase

For each eligible employee:

1. Determine `monthStart` and `monthEnd`.
2. Determine `eligibleFrom = max(joiningDate, monthStart)`.
3. Determine `eligibleTo = min(exitDate, monthEnd)` when an exit exists.
4. Load the approved, locked attendance summary.
5. Load the effective salary structure.
6. Load daily attendance between the eligible dates.
7. Load company status rules.
8. Resolve working days and payroll divisor.
9. Sum unpaid attendance fractions.
10. Add approved LHD/ED penalty fractions.
11. Calculate loss-of-pay days and amount.
12. Prorate earnings for joiners/leavers.
13. Calculate paid days.
14. Calculate tax and other deductions.
15. Save or update the unprocessed payroll record.
16. Save calculation snapshot and deduction-line evidence.

## 7.4 Paid Days calculation

In automatic generation:

```text
Eligible days = inclusive calendar days between eligibleFrom and eligibleTo

Configured unpaid days = sum of UnpaidDayFraction for attendance records

Loss-of-pay days = configured unpaid days + approved policy penalty days

Paid Days = max(0, Eligible days - Loss-of-pay days)
```

Examples:

### Full-month employee

```text
June calendar days:       30
Attendance unpaid days:    2
Approved penalty days:   0.5
Loss-of-pay days:         2.5
Paid Days:               27.5
```

### Employee joins on 16 June

```text
Eligible days:             15
Unpaid days in period:      1
Paid Days:                 14
Joining proration factor: 15 / 30
```

The automatic value is attendance-driven. The manual Add/Update Payroll API also accepts `PaidDays`, so an authorized payroll manager can override a draft. The UI should populate the calculated value rather than presenting a blank required field.

## 7.5 Earnings and deductions

Conceptual calculation:

```text
Proration factor = Eligible days / Days in month

Prorated basic       = Original basic * proration factor
Prorated HRA         = Original HRA * proration factor
Prorated LTA         = Original LTA * proration factor
Prorated allowances  = Original allowances * proration factor
Prorated bonus       = Original bonus * proration factor

Gross = basic + HRA + LTA + other allowance + bonus

Total deductions = income tax + insurance + EPF + ESIC + loss of pay

Net pay = Gross - Total deductions
```

Loss of pay is stored as a deduction and should be subtracted only once.

## 7.6 Manual add/update

Endpoint: `POST /api/PayRoll/AddUpdatePayRoll`

Required permission: PayRoll Create or Edit.

Flow:

1. Reject current/future months.
2. Reject negative earnings, deductions, paid days, or loss-of-pay days.
3. Resolve employee within the authenticated company.
4. Reject changes if any payroll in that company/month is already processed.
5. If payroll ID is supplied, verify payroll ID, company, user, employee, and month.
6. Otherwise reject a duplicate employee/month payroll.
7. Store earnings, deductions, paid days, pay month, and paid date.

This route is an authorized override path. It does not independently recalculate attendance-derived paid days.

## 7.7 Payroll import

Endpoint: `POST /api/PayRoll/UploadPayloadData`

Required permission: PayRoll Create or Edit.

The import flow:

1. requires a closed month;
2. validates required columns and numeric values;
3. rejects negative amounts/day values;
4. rejects import into a processed month;
5. maps employee payroll data;
6. creates or updates monthly draft rows.

Imported values are supplied values, not necessarily recalculated from attendance. Automatic generation should be used when attendance-driven payroll is required.

## 7.8 Payroll processing/publication

Endpoint: `POST /api/PayRoll/ProcessPayrollMonth`

Required permission: PayRoll Create or Edit.

Flow:

1. Confirm the month is closed.
2. Load payroll rows using `CompanyId + PayMonth`.
3. If no rows exist, return: generate payroll before processing.
4. If every row is already processed, return an idempotent success message.
5. Update every unprocessed row in that company/month:
   - `IsProcessed = true`;
   - `ProcessedAt = UtcNow`;
   - `ProcessedBy = authenticated user`.
6. Return that the month is available to employees.

Current processing is month-level, not per-employee selection.

## 7.9 Payroll listing and employee visibility

Endpoint: `POST /api/PayRoll/GetEmpPayRollData`

Required permission: `PayRoll.View`

### Payroll manager

If the caller also has PayRoll Create or Edit:

- company payroll rows for the selected month are returned;
- processed and unprocessed drafts may be shown;
- searching by employee name remains company-scoped.

### Normal employee

If the caller does not have Create/Edit:

- the query is restricted to the authenticated `UserId`;
- only `IsProcessed = true` rows are returned;
- the result is month-specific.

Therefore, processing June payroll does not expose May or July payroll. Each month has its own record and processed state.

## 7.10 Payslip generation

Own payslip endpoint: `POST /api/PayRoll/GeneratePaySlip`

Manager preview endpoint: `POST /api/PayRoll/GenerateEmployeePaySlip`

Own-payslip flow:

1. Resolve authenticated user.
2. Validate that month/year is closed.
3. Load payroll by employee, user, company, month, and year.
4. Reject if no payroll exists.
5. Reject if `IsProcessed` is false.
6. Build the salary-slip model.
7. Calculate displayed gross and total deductions.
8. Generate and return the PDF.

The process button is therefore the publication gate for both payroll visibility and payslip availability.

## 7.11 Payroll gaps and risks

- Month processing publishes every unprocessed payroll row; individual publishing is not currently part of the backend method.
- Automatic generation pre-validates all employees, but Mongo multi-document writes still lack a repository transaction/session abstraction.
- Manual edit accepts Paid Days independently from attendance, which can create a mismatch unless overrides are audited.
- The UI must populate all existing payroll values, especially Paid Days, when editing.
- A formal payroll revision/reversal workflow after processing is not implemented.
- Generate Employee Payslip should consistently map expected domain errors to safe client responses, as the own-payslip endpoint does.

## 8. Cross-module dependencies

| Upstream data | Consumer | Why it matters |
|---|---|---|
| Employee company/user IDs | All modules | Tenant and ownership scope |
| Joining/exit dates | Attendance and payroll | Defines eligible period |
| Leave policy attendance code | Attendance | Determines approved-leave status |
| Leave request status/version | Attendance reconciliation | Creates/reverses source-linked rows |
| Weekly offs and holidays | Leave and attendance | Determines included/working dates |
| Attendance status fractions | Monthly summary and payroll | Determines paid/unpaid day fractions |
| Attendance exceptions | Lock and payroll | Pending blockers prevent payroll |
| Locked monthly summary | Payroll | Approval checkpoint |
| Salary structure | Payroll | Earnings/deduction inputs |
| Payroll divisor policy | Payroll | Per-day loss-of-pay rate |
| IsProcessed | Employee payroll/payslip | Publication gate |

## 9. Recommended monthly operating procedure

1. Confirm attendance statuses and paid/unpaid fractions.
2. Confirm weekly offs and holidays.
3. Confirm leave policies have valid attendance mappings.
4. Confirm salary structures and divisor policy are effective for the month.
5. Review and decide all leave requests.
6. Confirm accepted leave reconciled into attendance.
7. Resolve any leave-attendance conflicts.
8. Complete missing attendance and checkout data.
9. Recalculate monthly exceptions.
10. Review LHD/ED policy exceptions.
11. Run Validate and Lock.
12. Confirm every eligible employee has an Approved and Locked summary.
13. Generate payroll for the closed month.
14. Review draft Paid Days, loss-of-pay days, earnings, and deductions.
15. Correct draft payroll only when an authorized override is justified.
16. Process the payroll month.
17. Verify employee month visibility.
18. Download sample payslips and reconcile net values.

## 10. End-to-end test scenarios

### Leave

- Create a full-day paid leave request and approve it.
- Confirm balance decreases once.
- Confirm source-linked attendance is created for included dates.
- Repeat reconciliation and confirm no duplicate attendance.
- Reject accepted leave and confirm balance refund and linked-row removal.
- Create manual attendance first, approve overlapping leave, and confirm a blocking conflict instead of overwrite.
- Verify company A cannot access company B policies, requests, or balances.

### Attendance

- Mark Present with valid times.
- Reject missing checkout for a time-required status.
- Reject attendance on weekly off or holiday.
- Verify half-day produces configured paid/unpaid fractions.
- Validate a month with missing records and confirm blockers.
- Correct the rows, recalculate, and confirm structural exceptions resolve.
- Review an LHD/ED exception with each decision type.
- Lock the month and confirm ordinary edits are rejected.

### Payroll

- Attempt generation for current month and confirm rejection.
- Attempt generation without a locked summary and confirm no records are written.
- Attempt generation without salary and confirm no records are written.
- Generate a clean closed month and verify draft records are unprocessed.
- Verify employee cannot see the draft or download its payslip.
- Verify Paid Days equals eligible days minus attendance and approved penalty LOP days.
- Process the month and confirm all draft rows receive processor and timestamp.
- Verify employee sees only their processed record for that selected month.
- Verify employee payslip works only after processing.
- Verify processed payroll cannot be edited or regenerated.

### Permissions

- View-only employee can view allowed own data but cannot mark attendance, manage leave, generate, edit, or process payroll.
- Attendance Create without Edit can create but cannot edit or lock.
- PayRoll View without Create/Edit receives only own processed payroll.
- PayRoll Create/Edit permits company draft management.
- Direct API calls are denied even if a hidden UI route is manually entered.

## 11. Source map

### Controllers

- `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- `Codeji.CMS.API/Controllers/AttendanceController.cs`
- `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- `Codeji.CMS.API/Controllers/AutoPayrollController.cs`
- `Codeji.CMS.API/Controllers/PayRollController.cs`
- `Codeji.CMS.API/Controllers/PayrollDivisorPolicyController.cs`

### Services

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs`
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs`
- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs`
- `Codeji.CMS.Services/Attendance/CompanyWorkingCalendarService.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- `Codeji.CMS.Services/PayRoll/PayrollCalculationRules.cs`
- `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs`

### Entities

- `Codeji.CMS.Repository/Entities/Leave/*`
- `Codeji.CMS.Repository/Entities/Attendance/*`
- `Codeji.CMS.Repository/Entities/Employees/EmpPayRoll.cs`
- salary-structure and payroll-divisor entities under Employees

## 12. Summary

The implemented payroll chain is intentionally gated:

```text
Accepted leave
  -> source-linked attendance
  -> corrected daily attendance
  -> resolved exceptions
  -> approved and locked monthly summary
  -> generated payroll draft
  -> HR payroll review
  -> processed payroll
  -> employee visibility and payslip
```

If any upstream stage is incomplete, the downstream stage should stop. This prevents an approved leave from being treated as unexplained absence, prevents unresolved attendance from silently affecting salary, and prevents an employee from viewing payroll before HR publishes it.
