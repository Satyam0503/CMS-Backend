# Attendance Module: Current Flow, Dependencies, Bugs, and Logical Audit

## 1. Purpose and scope

This document describes the Attendance module as it currently exists in the ASP.NET Core backend and React frontend. It covers:

- attendance entry and calendar display;
- status configuration, weekly offs, holidays, and time validation;
- role and permission behavior;
- monthly validation, exceptions, review, and locking;
- dependencies on Employee, Leave, Calendar, Salary, and Payroll;
- current bugs, logical gaps, operational risks, and recommended fixes;
- test scenarios required before production use.

This is a current-state document. Statements marked as gaps describe behavior that is not fully implemented even if the UI suggests otherwise.

## 2. High-level architecture

```text
Employee master
   | identity, company, joining/exit dates
   v
Attendance calendar entry <--- Status settings
   |                         <--- Weekly-off settings
   |                         <--- Company holidays
   v
Attendance records (daily source of truth) 
   |
   +--> Monthly validation and exception generation
   |        |
   |        +--> HR/Admin exception review
   |        +--> Monthly attendance summary
   |                    |
   |                    v
   |              Approved + locked month
   |                    |
   v                    v
Leave reconciliation  Payroll processing
(accepted leave writes (blocked without locked summary,
source-linked records) salary structure, or resolved exceptions)
```

## Implementation update — 22 July 2026

The approved-leave gap described in the original audit is now addressed in the backend flow:

1. A leave policy can store `AttendanceStatusCode`; create and update APIs expose it.
2. A supplied mapping is validated inside the authenticated company and must reference an active status that does not require check-in/check-out times.
3. After a request is accepted, `LeaveAttendanceReconciliationService` reloads the company-scoped request, employee, policy, and status.
4. Included dates are calculated from the company's weekly-off and holiday data while respecting the policy's independent weekend/holiday inclusion flags.
5. Each date is written as a real attendance record using `CompanyId + UserId + Date`, with `SourceType=LEAVE`, the request ID, and request version. Repeating the operation updates the same source-linked row instead of duplicating it.
6. A manual or differently sourced row is never overwritten. A blocking `LEAVE_ATTENDANCE_CONFLICT` exception records both statuses and source identifiers.
7. Rejecting a previously accepted request removes only attendance records linked to that request. It does not delete manual attendance.
8. Unlocked monthly summaries affected by reconciliation are invalidated so validation rebuilds them from daily records.
9. Attendance validation and LHD/ED recalculation now scope daily reads by company, immutable user ID, employee code, and date range.
10. Default attendance statuses are persisted on first use (including `UL`) instead of existing only in memory.

### Operational requirement for existing leave policies

Existing policies with no mapping remain readable for backward compatibility, but accepting them cannot generate attendance. HR must edit each policy and set a valid no-time attendance code such as `CL`, `SL`, `EL`, `UL`, `COMP-OFF`, `CL-HALF`, or `SL-HALF`. The Leave Policy form now exposes this field.

### Remaining production-hardening work

The current Mongo repository abstraction does not provide a transaction/session API. Leave request, balance, attendance, summary, and notification writes therefore remain a multi-document workflow rather than a true transaction. Production must confirm replica-set transaction support before claiming atomic approval. A durable saga/outbox, conditional balance/version update, company-month run entity, audited reopen/relock lifecycle, granular attendance capabilities, and payroll revision workflow remain required. These are not represented as completed by this update.

## 3. Main components

### Backend API

- `AttendanceController`: create, read, update, and date-range attendance operations.
- `AttendanceStatusSettingsController`: retrieve and save company attendance status definitions.
- `WeeklyOffSettingsController`: retrieve and save company weekly-off days.
- `AttendancePenaltyController`: penalty policy, exception recalculation/review, and month validation/locking.
- `CalendarController`: supplies company holidays used by the working calendar.
- `LeaveManagementController`: supplies approved leave requests displayed by the attendance UI.
- `AutoPayrollController`: processes payroll after attendance has been validated and locked.

### Backend services

- `AttendanceService`: validates employees, dates, statuses, times, and saves daily attendance.
- `AttendanceEditGuard`: prevents edits on locked months, weekly offs, and holidays.
- `AttendanceStatusService`: supplies default or stored status rules.
- `WeeklyOffService`: manages weekly-off configuration.
- `CompanyWorkingCalendarService`: calculates working dates using weekly offs and holidays.
- `AttendancePenaltyService`: builds summaries, creates exceptions, reviews penalties, and locks months.
- `AutoPayrollServices`: consumes locked attendance summaries and daily records to calculate payroll.

### Frontend

- `EmployeeAttendance.tsx`: attendance page, employee loading, filters, and summary cards.
- `AttendanceCalendar.tsx`: monthly grid, leave/holiday overlay, entry modal, bulk marking, and API calls.
- `AttendanceStatusSettings.tsx`: status, weekly-off, and payroll penalty settings.
- `MonthlyAttendanceExceptions.tsx`: exception review and navigation to affected attendance records.

## 4. Data model

### 4.1 Attendance

One logical record should exist per company, employee, and calendar date.

Important fields:

| Field | Purpose |
|---|---|
| `AttendanceId` | MongoDB record identifier |
| `CompanyId` | Tenant boundary; required for secure queries |
| `UserId` | Application user identity |
| `EmployeeId` | Payroll/business employee identity |
| `Date` | Attendance date, normalized to date-only semantics |
| `Status` | Status code such as `P`, `A`, `SL`, or `WFH` |
| `CheckInTime`, `CheckOutTime` | Required when the selected status requires time |
| `TotalHours` | Calculated duration after the current one-hour break rule |
| `LateCount`, `EarlyExitCount` | Derived indicators used by penalty logic |
| `SourceType`, `SourceId`, `SourceVersion` | Intended source/audit linkage; currently not used by leave synchronization |
| `Remarks` | Manual explanation |

The repository uses an upsert filter on `CompanyId + UserId + Date`. A hardened unique index is also expected for the same logical key.

### 4.2 AttendanceStatusSetting

Each company can define status behavior:

- code and display name;
- active/inactive state;
- whether check-in/check-out is required;
- paid-day fraction;
- unpaid-day fraction;
- display order.

Default codes include:

| Code | Meaning | Requires time | Paid | Unpaid |
|---|---|---:|---:|---:|
| `P` | Present | Yes | 1 | 0 |
| `A` | Absent | No | 0 | 1 |
| `SL` | Sick Leave | No | 1 | 0 |
| `CL` | Casual Leave | No | 1 | 0 |
| `EL` | Earned Leave | No | 1 | 0 |
| `WFH` | Work From Home | Yes | 1 | 0 |
| `HD` | Half Day | Yes | 0.5 | 0.5 |
| `ED` | Early Departure | Yes | 1 | 0 |
| `LHD` | Late Arrival-Half Day | Yes | 0.5 | 0.5 |
| `WFH+WFO` | Half WFH and half office | Yes | 1 | 0 |
| `COMP-OFF` | Compensatory Off | No | 1 | 0 |
| `CL-HALF` | Casual Leave half day | No | 0.5 | 0 |
| `SL-HALF` | Sick Leave half day | No | 0.5 | 0 |
| `WFH-HD` | WFH with half day | Yes | 0.5 | 0.5 |

Validation prevents negative fractions, fractions above one, or a paid-plus-unpaid total above one.

### 4.3 WeeklyOffSetting and holidays

- Weekly offs are integers from `0` through `6`, following `DayOfWeek` (`0 = Sunday`).
- The default is Saturday and Sunday.
- At least one weekly-off day must be selected.
- Holidays come from the company calendar and may be one-time or recurring annually.
- Both are used to determine editable and expected working days.

### 4.4 MonthlyAttendanceSummary

This is the payroll-facing monthly snapshot. It stores:

- eligible employment period;
- expected and eligible working days;
- present, WFH, paid leave, unpaid leave, half day, and absent totals;
- missing attendance and missing checkout counts;
- paid and unpaid days;
- LHD/ED counts;
- blocking exception count;
- approval and lock state;
- approver/locker and timestamps;
- version number.

Payroll requires a summary for the employee and month where both `IsApproved` and `IsLocked` are true.

### 4.5 AttendancePayrollException

Exceptions represent issues that must be corrected or reviewed before payroll. Current types include or imply:

- `MISSING_ATTENDANCE`;
- `MISSING_CHECKOUT`;
- `INVALID_TIME_ORDER`;
- `LHD_ED_LIMIT_EXCEEDED`;
- duplicate/structural attendance problems.

The LHD/ED exception supports decisions such as waive, warning only, half-day loss of pay, full-day loss of pay, or a custom day fraction. Optimistic version checking prevents two reviewers from silently overwriting each other.

## 5. Permissions and access flow

Attendance now follows the role-permission matrix rather than a hard-coded role type:

| Permission | Current effect |
|---|---|
| `Attendance.View` | Show Attendance navigation/route and allow attendance/status/exception reads where decorated |
| `Attendance.Create` | Create or bulk-mark attendance |
| `Attendance.Edit` | Edit attendance, settings exposed through permission-decorated APIs, validate/lock, and review exceptions |
| `Attendance.Delete` | No active delete endpoint is present in the current controller |

Important distinction: a role name such as Employee or HR does not itself decide module access. Assigned module/action permissions do. `AdminOnly` is still used for saving attendance status definitions.

## 6. Current daily attendance flow

### 6.1 Page load

1. The frontend loads employees permitted by the Employees API.
2. It loads active attendance statuses.
3. It loads company weekly offs.
4. For the selected month it loads:
   - approved leave requests;
   - company holidays;
   - attendance records for the displayed employee IDs and date range.
5. Attendance records are keyed as `UserId + YYYY-MM-DD` and mapped into the grid.
6. Holidays, weekly offs, and approved leave are rendered as calendar context.

### 6.2 Create attendance

1. User selects an employee/date and status.
2. `Attendance.Create` is checked by the API authorization filter.
3. The service confirms the employee belongs to the authenticated company and is active.
4. The edit guard rejects locked months, weekly offs, and company holidays.
5. The service loads the company’s active status configuration.
6. If time is required, check-in and check-out must parse as `HH:mm`, remain within one day, and check-out must be after check-in.
7. Total hours are calculated. The current rule subtracts one hour whenever both times exist.
8. The repository upserts the record using company, user, and date.

### 6.3 Update attendance

The update flow repeats employee, company, date, lock, weekly-off, holiday, status, and time validations. A locked monthly summary prevents later edits.

### 6.4 Bulk marking

The frontend sends one create request per selected employee/date. This is not a single transactional bulk backend operation. A mid-batch failure can therefore produce a partially completed bulk action.

## 7. Monthly validation and lock flow

1. HR/Admin chooses a completed payroll month.
2. The system identifies active employees eligible during that month using joining and exit dates.
3. Working dates are calculated from weekly offs and holidays.
4. Daily attendance is evaluated using company status rules.
5. Missing dates, missing checkouts, invalid times, and policy issues become exceptions.
6. A monthly summary is created or refreshed while it is unlocked.
7. Blocking exceptions must be corrected or reviewed.
8. When no unresolved blocker remains, each summary is marked approved and locked.
9. Once locked, normal attendance create/update is rejected by `AttendanceEditGuard`.

There is currently no complete reopen/correction workflow even though the error message refers to one.

## 8. Leave dependency

### Current UI relationship

The attendance calendar fetches approved leave requests for the month and displays them alongside attendance data. This helps a reviewer see why an employee may not have ordinary attendance.

### Current backend relationship

Leave acceptance now calls `LeaveAttendanceReconciliationService`. It creates or updates idempotent, source-linked attendance rows using the policy's mapped attendance status and populates `SourceType`, `SourceId`, and `SourceVersion`. A conflicting manual row is preserved and produces a blocking exception instead of being overwritten.

### Consequence

Payroll and monthly validation consume persisted attendance records rather than relying on the visual leave overlay. Existing leave policies must therefore have a valid no-time attendance status mapping. A missing mapping or unresolved conflict can still block the month, but accepted and successfully reconciled leave is no longer treated as missing attendance.

### Recommended leave synchronization

On leave acceptance, create/upsert attendance records for every working date in the approved range:

- `CompanyId` from the employee;
- `UserId` and `EmployeeId` from the employee;
- status derived from the leave policy code;
- half-day status/fraction where appropriate;
- `SourceType = "LEAVE"`;
- `SourceId = LeaveRequestId`;
- `SourceVersion = LeaveRequest.Version`.

On approved-leave rejection, withdrawal, date change, or policy change, reconcile only attendance records whose source matches that leave request. Manual attendance must not be silently overwritten.

## 9. Payroll dependency

Payroll processing depends on all of the following:

1. The payroll month must be completed; current/future months are rejected.
2. Employee joining/exit dates must define an eligible period.
3. A monthly attendance summary must exist, be approved, and be locked.
4. A salary structure effective for the payroll month must exist.
5. No unresolved blocking attendance exception may remain.
6. Attendance status settings determine paid and unpaid fractions.
7. Weekly offs and holidays determine expected working days and the payroll divisor where configured.
8. Approved LHD/ED deductions are included in loss-of-pay calculation.

Simplified calculation flow:

```text
Attendance records
   -> status paid/unpaid fractions
   -> configured unpaid days
   + approved penalty days
   -> loss-of-pay days
   -> loss-of-pay amount using payroll divisor
   -> prorated earnings for joining/exit period
   -> gross pay - deductions
   -> payroll record and payslip
```

After an approved attendance penalty is used, its status becomes `APPLIED_TO_PAYROLL`.

## 10. Confirmed bugs and logical gaps

### Critical/high priority

1. **Leave reconciliation is not transactionally atomic.** Request, balance, attendance, summary, and notification changes span multiple Mongo documents without a repository transaction/session abstraction. Partial failure recovery still needs a durable workflow.
2. **Tenant filter omission in penalty recalculation.** The LHD/ED record query filters by `EmployeeId` and date but does not include `CompanyId`. Duplicate employee IDs across companies can contaminate exception counts.
3. **Some leave summary/balance queries lack complete company filters.** Attendance UI calls leave APIs, so cross-tenant defects in leave data can affect attendance display and summaries.
4. **Permission scope is broad.** `Attendance.View` currently allows company attendance views; there is no separate “view own attendance” versus “view all attendance” permission.
5. **No locked-month reopen workflow.** Records become immutable, but an authorized, audited correction flow is not implemented.

### Medium priority

6. **Status GET authorization is inconsistent.** Any authenticated user can call the status settings GET endpoint because it lacks `Attendance.View`.
7. **Status save is Admin-only while other settings use permissions.** This conflicts with the permission-driven design and may block HR users explicitly given Attendance Edit.
8. **Controller naming is misleading.** `AdminAttendanceController` and `/api/admin/attendance` are permission-driven and not Administrator-only.
9. **Bulk attendance is non-transactional.** Multiple independent requests can partially succeed.
10. **Delete permission has no matching active attendance delete flow.** The matrix exposes a permission that currently has no clear effect.
11. **One-hour break deduction is unconditional.** Short shifts and some half-day records can receive an incorrect break deduction unless separately handled.
12. **Default statuses can be returned without being persisted.** Until initialization/save occurs, other services requiring stored status rows may behave differently from the UI fallback.
13. **Error handling in the calendar is mostly console-based.** A failed leave/holiday request can prevent the attendance request in the same combined `try` block and leave a blank grid without a clear user message.
14. **Selected employee pagination affects loading.** The page requests a finite employee set; records outside that set are not shown even if attendance exists.

### Historical bug repaired

15. **June 2026 records were invisible because `CompanyId` was absent.** Company-scoped attendance queries correctly excluded them. A migration backfilled the field, and future June data insertion now includes it.

## 11. Recommended target design

### Permission split

Introduce distinct capabilities:

- `Attendance.ViewOwn`;
- `Attendance.MarkOwn`;
- `Attendance.ViewAll`;
- `Attendance.CreateAll`;
- `Attendance.EditAll`;
- `Attendance.ManageSettings`;
- `Attendance.ReviewExceptions`;
- `Attendance.LockMonth`;
- `Attendance.ReopenMonth`.

### Leave-to-attendance synchronization

Use source-linked idempotent upserts and a reconciliation handler triggered by leave status/version changes.

### Lock lifecycle

Use explicit states:

```text
OPEN -> VALIDATING -> EXCEPTIONS_PENDING -> READY_TO_LOCK -> LOCKED
                                                   |
                                                   v
                                         REOPENED_FOR_CORRECTION
```

Every transition should store actor, timestamp, reason, and version.

### Bulk operation safety

Add a backend bulk endpoint that validates all rows first, writes in a controlled operation, and returns per-row results. Do not simulate an atomic operation only in the UI.

## 12. Required test matrix

### Tenant security

- A company cannot read or update another company’s attendance.
- Duplicate `EmployeeId` values across companies do not affect penalty counts.
- Missing/incorrect `CompanyId` is rejected or safely repaired.

### Permissions

- View-only role can view but cannot create/update/lock.
- Create role can mark attendance but cannot edit existing records unless Edit is granted.
- Edit role can update and run permitted workflows.
- Direct API calls are denied consistently with the UI.
- Disabled app access overrides module permissions.

### Dates and calendar

- Weekly offs and one-time/recurring holidays cannot be marked.
- Joining and exit dates limit eligible attendance.
- Month/year and timezone boundaries do not shift dates.
- Leap year February works correctly.

### Status and time

- Active known status succeeds.
- Unknown/inactive status fails.
- Required check-in/check-out validation works.
- Check-out before/equal to check-in fails.
- Half-day hours and break deduction are correct.
- Paid/unpaid fractions produce expected summaries.

### Leave integration

- Accepted full-day leave creates the correct attendance status.
- Half-day leave creates a correct fractional status.
- Withdrawal/rejection removes or reconciles only source-linked attendance.
- Manual attendance conflict requires an explicit decision.
- Approved leave never creates a missing-attendance payroll blocker.

### Locking and payroll

- Missing attendance/checkouts create blocking exceptions.
- Reviewed penalties affect payroll exactly once.
- Locked attendance cannot be edited normally.
- Payroll cannot process without an approved locked summary.
- Payroll cannot process with unresolved exceptions.
- Already processed payroll is not duplicated.

## 13. Operational checklist

Before processing payroll for a month:

1. Confirm weekly-off and holiday configuration.
2. Confirm status fractions and time requirements.
3. Reconcile all approved leave into attendance.
4. Run attendance exception recalculation.
5. Correct structural exceptions.
6. Review LHD/ED policy exceptions.
7. Validate and lock the month.
8. Confirm every eligible employee has an effective salary structure.
9. Process selected or all unprocessed payroll records.
10. Verify payslips and applied deductions.

## 14. Current source references

- `Codeji.CMS.API/Controllers/AttendanceController.cs`
- `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- `Codeji.CMS.API/Controllers/WeeklyOffSettingsController.cs`
- `Codeji.CMS.Services/Attendance/AttendanceService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs`
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`
- `Codeji.CMS.Services/Attendance/CompanyWorkingCalendarService.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs`
- `Codeji.CMS.Repository/Repositories/AttendanceRepository.cs`
- `Codeji.CMS.Repository/Entities/Attendance/*`
- `CMS-React/src/app/modules/attendance/EmployeeAttendance.tsx`
- `CMS-React/src/app/modules/attendance/component/AttendanceCalendar.tsx`
