# Attendance Module: Architecture, Workflows, Dependencies, and Operations

## 1. Purpose and scope

Attendance is the company-scoped daily record used to establish an employee's paid or unpaid time for a payroll month. It is not an isolated screen: it depends on employee identity, company work-calendar rules, configured attendance statuses, approved leave, monthly exception review, payroll locking, and workflow-owned WFH rows.

This guide documents the current backend and React implementation, including source-linked approved-leave rows, HR/Admin bulk and import behavior, employee self-service calendar, WFH attendance, and payroll-dependent locking.

### Implementation summary

At a code level, attendance works as a controlled write model:

1. The API receives a request with authenticated company context and a user target.
2. Services resolve the employee inside the current company and validate the request against tenant rules.
3. The write path uses shared validation so the same rules apply to manual edits, bulk operations, reminder-based default rows, and workflow-generated rows.
4. Once a row is written, it becomes part of the monthly summary, exception workflow, and payroll dependency chain.

This makes Attendance a downstream dependency of employee identity, work calendar, leave approval, WFH approval, and monthly exception review.

## 1.1 Current hardening and design principles

The current implementation emphasizes:

- tenant isolation through `CompanyId` in every attendance query and persistence path
- source provenance via `SourceType`, `SourceId`, and `SourceVersion`
- explicit protection of leave/WFH-owned rows rather than silent overwrite
- shared validation through `IAttendanceMutationValidator`
- monthly locking as the payroll stability boundary
- UI rendering of persisted attendance codes rather than inferred absence status

### Production hardening highlights

- `IAttendanceMutationValidator` is the shared validation authority for `POST api/admin/attendance` and `POST api/admin/attendance/bulk`.
- effective office schedules resolve in this order: employee assignment, department assignment, company default.
- bulk requests are fully prevalidated before any database write begins.
- leave and WFH source-owned rows are reported as protected skips and are not overwritten by bulk or manual updates.
- employee self-service uses the token-derived user and is not governed by arbitrary employee IDs.
- the UI and backend both reflect source ownership so HR/Admin and employees see the same persisted status.

## 2. System map

```text
Authentication and tenant claims
       |
       v
Employee master --------> Attendance status settings
       |                         |
       +--> Company / weekly offs +--> daily attendance record
       |                         |          |
Calendar holidays --------+          +------> monthly validation and exceptions
       |                                     |
Approved leave --> leave reconciliation --+  +--> approved and locked monthly summary
                    (source-linked rows)                  |
                                                        Payroll
```

The tenant boundary is `CompanyId`. Daily attendance is identified by `CompanyId + UserId + Date`; `EmployeeId` is retained as the HR/payroll business identifier.

## 3. Main implementation areas

| Layer | Key files / route | Responsibility |
|---|---|---|
| HR/Admin attendance API | `Codeji.CMS.API/Controllers/AttendanceController.cs`, route `api/admin/attendance` | Company-grid reads, manual attendance writes, correction review, and bulk marking |
| Employee self-service API | `Codeji.CMS.API/Controllers/MyAttendanceController.cs`, route `api/attendance/me` | Token-derived own monthly grid/calendar and correction requests |
| Attendance service | `Codeji.CMS.Services/Attendance/AttendanceService.cs` | validation, normalization, and persistence |
| Bulk mutation service | `Codeji.CMS.Services/Attendance/AttendanceBulkMutationService.cs`, `AttendanceMutationValidator.cs` | prepare and persist bulk rows and validate ownership |
| Edit guard | `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs` | protects locked months, holidays, weekly offs, and source-owned rows |
| Repository | `Codeji.CMS.Repository/Repositories/AttendanceRepository.cs` | company/user/date upsert and range queries |
| Status config | `AttendanceStatusSettingsController.cs`, `AttendanceStatusService.cs` | tenant-specific attendance code definitions |
| Leave reconciliation | `LeaveAttendanceReconciliationService.cs` | convert accepted leave to source-linked attendance rows |
| WFH workflow | `WorkFromHomeService.cs`, `Codeji.CMS.API/Controllers/WorkFromHomeController.cs` | WFH request approval, check-in/out, and source-owned attendance |
| Monthly controls | `AttendancePenaltyController.cs`, `AttendancePenaltyService.cs` | rule validation, exceptions, and month locking |
| Frontend | `CMS-React/src/app/modules/attendance/*` | attendance UI, company grid, employee self-service, bulk/import, status settings |

## 4. Core data model and ownership

### 4.1 `AttendanceModel`

`Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs` is the daily source of truth.

| Field | Meaning | Owner/update rule |
|---|---|---|
| `AttendanceId` | Mongo identifier | Created by repository |
| `CompanyId` | Tenant boundary | required in every filter |
| `UserId` | application identity | resolved from the target employee |
| `EmployeeId` | business/payroll code | resolved from the target employee; not trusted from client |
| `Date` | date-only business date | normalized before persistence |
| `Status` | attendance code | validated against tenant status settings |
| `CheckInTime`, `CheckOutTime` | optional times | normalized by service |
| `TotalHours` | derived working duration | calculated by service |
| `LateCount`, `EarlyExitCount` | penalty inputs | derived during attendance processing |
| `Remarks` | operator or system note | manual or generated |
| `SourceType`, `SourceId`, `SourceVersion` | provenance metadata | used to protect source-owned rows |

The repository upserts by `CompanyId + UserId + Date`. A production deployment should enforce the Mongo unique index over that key.

### 4.2 Response contract

`AttendanceResponseDto` includes the standard attendance fields plus `SourceType` and `SourceId`. The frontend uses this metadata rather than inferring leave from a separate overlay.

Example:

```json
{
  "userId": "...",
  "employeeId": "CD-0001",
  "date": "2026-07-10T00:00:00",
  "status": "CL",
  "sourceType": "LEAVE",
  "sourceId": "leave-request-id",
  "remarks": "Generated from approved leave leave-request-id"
}
```

### 4.3 Status definitions

`AttendanceStatusSetting` is tenant-scoped and controls:
- `Code`
- display `Name`
- `ColorHex`
- active state
- `RequiresTime`
- paid/unpaid fractions
- `IsAvailableForLeaveManagement`
- display order

Typical codes include:
- `P`, `A`, `UL`, `CL`, `SL`, `EL`, `COMP-OFF`, `CL-HALF`, `SL-HALF`, `HD`, `LHD`, `WFH`, `WFH+WFO`, `WFH-HD`, `ED`

Status settings that require time include `P`, `WFH`, `HD`, `ED`, `LHD`, `WFH+WFO`, `WFH-HD`.

A status that requires clock times cannot be enabled for leave policy mapping. The leave service validates this rule when saving and approving policies.

## 5. Access model and permissions

### 5.1 Authorization model

Attendance APIs require authenticated tenant context and module permissions. Key permissions include:
- `Attendance.ViewAll` — company attendance grid access
- `Attendance.CreateForEmployee` — manual attendance create/update for employees
- `Attendance.Override` — overwrite protected rows when permitted
- `Attendance.View` / `Attendance.ViewOwn` — employee self-service access

### 5.2 Self-service boundary

Employee self-service routes derive company and user from JWT. They do not trust client-supplied employee IDs.

| Capability | Route | Boundary |
|---|---|---|
| Employee own calendar | `GET api/attendance/me/grid`, `GET api/attendance/me/calendar` | active employee only |
| Correction request | `POST api/attendance/me/correction-requests` | active employee only |
| HR/Admin company grid | `POST api/admin/attendance/GetAllAttendanceItems` | `Attendance.ViewAll` required |
| Manual create/update | `POST api/admin/attendance` | `Attendance.CreateForEmployee` / `Attendance.Override` |

A user with only `Attendance.View` cannot access the company grid or modify arbitrary employees.

### 5.3 React UI gating

`EmployeeAttendance.tsx` controls display logic:
- `canViewAllAttendance` is true for Administrators or users with `Attendance.ViewAll`
- `canManageAttendance` is true for Administrators or users with `Attendance.CreateForEmployee` / `Attendance.Override`
- employee-only mode renders a single row labeled `My attendance`
- bulk actions, search, and edit controls are hidden or disabled for self-service mode

## 6. Frontend attendance surfaces

### 6.1 Main attendance page

File: `CMS-React/src/app/modules/attendance/EmployeeAttendance.tsx`

Responsibilities:
- determine company vs employee mode
- load employee list for company grid
- compute and apply selected date range
- display summary stats cards
- show correction request UI for employees
- pass `readOnly` into the attendance grid

### 6.2 Attendance calendar grid

File: `CMS-React/src/app/modules/attendance/component/AttendanceCalendar.tsx`

This component renders the monthly grid, including:
- status selection
- optional check-in/out inputs
- source ownership markers
- leave/WFH tooltip enrichment
- pagination and load state handling

It loads tenant-specific data:
- active attendance status settings
- weekly offs
- holidays/events
- company attendance rows or own attendance rows

`readOnly` mode disables writes and hides bulk/batch editing.

### 6.3 Status settings UI

File: `CMS-React/src/app/modules/attendance/AttendanceStatusSettings.tsx`

This screen allows configuration of attendance codes, display colors, required time flags, and leave-management eligibility.

### 6.4 Exceptions review UI

File: `CMS-React/src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx`

It supports:
- exception list loading
- recalculation of monthly exceptions
- reviewing individual exceptions
- resolving attendance issues via status/time corrections
- bulk decisions for LHD/ED exceptions

### 6.5 Profile attendance calendar

File: `CMS-React/src/app/modules/users/components/LeaveCalendar.tsx`

This is a read-only calendar used in employee profiles. It shows status colors and tooltips for each day, but it does not allow edits.

## 7. Employee self-service flow

### 7.1 Load own attendance

Flow:
1. Employee opens Attendance page.
2. the page enters employee mode.
3. it creates a single row for the current user.
4. the grid calls `GET api/attendance/me/grid`.
5. backend derives company and user from JWT.
6. only persisted attendance rows are shown; missing dates remain empty.

### 7.2 Correction request

Flow:
1. employee selects a date and enters a reason.
2. `POST api/attendance/me/correction-requests` is sent.
3. backend validates active employee, existing attendance row, and duplicate pending request.
4. it stores `AttendanceCorrectionRequest` with `Pending` status.
5. HR/Admin recipients receive a notification.

A correction request is audit/workflow-only. It does not change attendance directly or bypass locks/source ownership.

## 8. HR/Admin attendance flow

### 8.1 Grid load and editing

Flow:
1. HR/Admin opens Attendance page.
2. the page loads eligible employees.
3. the grid loads statuses, weekly offs, holidays, and attendance rows.
4. each cell shows persisted status and source metadata.
5. HR/Admin can click a cell to open an edit modal.

### 8.2 Manual create/update

The backend applies:
1. resolve employee within the authenticated company
2. verify employee code when supplied
3. run `AttendanceEditGuard`
4. validate status and times
5. load any existing row
6. reject leave/WFH-owned rows
7. calculate derived fields
8. upsert by `CompanyId + UserId + Date`

### 8.3 Bulk marking

Route: `POST /api/admin/attendance/bulk`

Key behavior:
- accepts up to 500 rows
- requires `Attendance.CreateForEmployee` and `Attendance.Override`
- supports `skipExisting`
- rejects duplicate rows in the request
- protects leave/WFH-owned rows and returns them as `protectedSkipped`
- resolves effective schedule times where needed

### 8.4 Import preview and commit

Routes:
- `POST /api/admin/attendance/bulk-import/validate`
- `POST /api/admin/attendance/bulk-import/commit`

Import validation:
- accepts `.xlsx`, `.xls`, `.csv`
- validates sheet headers and row data
- rejects formula cells and invalid rows
- resolves employee codes in the authenticated company
- returns per-row outcomes: `READY_CREATE`, `READY_UPDATE`, `UNCHANGED`, `PROTECTED`, `INVALID`

Commit:
- requires an idempotency key
- re-resolves employee codes
- re-runs shared validation
- returns created/updated/skipped/protected counts

### 8.5 Default present row automation

Behavior:
- `AttendanceReminderHostedService` runs every minute IST
- it creates a missing `P` row after an employee's effective schedule start
- it uses `SYSTEM_DEFAULT_PRESENT` source and `CreateMissingOnly`
- it never overwrites manual, leave, WFH, or existing system rows

Review notification:
- at or after 10:52 AM IST, `AttendanceReminderProcessor` sends one deduplicated in-app notification per company
- recipients are active Administrator, HR, and HR Executive users
- the notification encourages HR/Admin review and correction

## 9. Leave reconciliation flow

### 9.1 Business-date contract

Attendance and leave share a UTC-midnight business date. Approved leave and generated attendance use the same normalized date value.

Legacy local-midnight rows are corrected by `NormalizeLegacyLeaveBusinessDates` only when safe.

### 9.2 Leave policy mapping

Leave policies map to attendance via `AttendanceStatusCode`. The mapped code must be active, tenant-scoped, and no-time.

### 9.3 Approved leave reconciliation

When leave is accepted, `LeaveAttendanceReconciliationService`:
1. reloads the current company, employee, policy, and status
2. calculates included dates using holidays, weekly offs, and policy inclusion flags
3. writes or updates source-owned attendance rows with the mapped status
4. sets `SourceType = LEAVE`, `SourceId = LeaveRequestId`, `SourceVersion = LeaveRequest.Version`
5. writes a generated remark
6. invalidates affected unlocked monthly summaries

Reconciliation is idempotent for the same source identity.

### 9.4 Conflict handling

If an existing row is manual or has a non-matching source, reconciliation does not overwrite it. It creates a blocking `LEAVE_ATTENDANCE_CONFLICT` exception instead.

If approved leave is reversed, the service removes only matching source-owned rows. It does not delete manual attendance.

## 10. WFH attendance flow

WFH is source-owned attendance:
- WFH policy is tenant-scoped
- employee submits `POST /api/wfh/requests`
- requests validate eligibility, quota, holidays, weekly offs, and locked dates
- approval creates `WFH_REQUEST` source-owned attendance rows
- employee check-in/out uses server UTC time
- insufficient hours create a reviewable payroll exception

Manual attendance editing rejects WFH-owned rows.

## 11. Work calendar dependency

Attendance uses shared calendar rules:
- tenant weekly offs
- company holidays
- employee join/exit dates
- leave policy inclusion flags

The edit guard rejects manual changes on weekly offs and holidays. Leave reconciliation may still include them if the policy permits.

## 12. Monthly validation and payroll lock

### 12.1 Endpoints

| Endpoint | Permission | Purpose |
|---|---|---|
| `GET api/attendance/penalty/policy` | View | read penalty rules |
| `POST api/attendance/penalty/policy` | Edit | save policy |
| `POST api/attendance/penalty/exceptions/recalculate` | Edit | rebuild month exceptions |
| `POST api/attendance/penalty/exceptions/search` | View | list exceptions |
| `PATCH api/attendance/penalty/exceptions/{id}/review` | Edit | review exception |
| `GET api/attendance/penalty/month/lock-status` | View | read month lock state |
| `POST api/attendance/penalty/month/validate-lock` | Edit | validate and lock month |

### 12.2 Summary lifecycle

`MonthlyAttendanceSummary` is the payroll-facing employee/month view. It includes expected days, totals, penalties, and lock metadata.

```text
Daily attendance + source-linked leave
  -> exception generation
  -> review and correction
  -> approved summary
  -> locked summary
  -> payroll eligibility
```

Locked months block normal daily edits. The current system does not provide a full audited reopen/relock workflow.

## 13. Payroll dependency

Payroll requires attendance as a stable input:
1. the month is validated and eligible
2. the employee has an active salary structure
3. the monthly attendance summary is approved and locked
4. blocking exceptions are resolved
5. paid/unpaid status fractions and penalties are applied
6. the locked summary is used to compute pay

Changing attendance after payroll invalidates the result.

## 14. Notifications and reporting

Attendance is a read model for payroll, reports, and dashboards. Leave approval notifications, HR/Admin alerts, and employee correction alerts are persisted independently.

Reports should use persisted attendance records and locked summaries, not browser-inferred calendar status.

## 15. Safeguards and limitations

### Safeguards
- company-scoped writes and queries
- shared validation for all attendance writes
- leave/WFH source provenance and protection
- holiday/weekly-off/monthly-lock edit guard
- bulk validation and protected-skip behavior
- source-coded attendance rendering with lock/tooltips
- JWT-derived self-service ownership enforcement

### Limitations
1. leave workflow spans multiple documents and needs end-to-end failure/retry validation
2. bulk writes require replica-set transaction support in production
3. no audited reopen/relock workflow is fully implemented
4. role/permission migration must be verified per tenant
5. the unique `CompanyId + UserId + Date` index must be monitored
6. payroll revision/control workflow remains a future requirement

## 16. Operational runbook

### New company checklist
1. configure weekly offs
2. add company holidays
3. review attendance status definitions
4. configure leave policies with valid attendance mappings
5. grant Attendance permissions to HR roles
6. validate manual, leave, and half-day flows in non-production

### Month-end checklist
1. verify attendance completeness
2. resolve exceptions and leave conflicts
3. validate the month
4. confirm paid/unpaid totals
5. lock the month
6. process payroll

### Incident triage
| Symptom | Check |
|---|---|
| leave code missing in grid | confirm source-linked attendance row and leave policy mapping |
| cell not editable | inspect `SourceType`, month lock, weekly off, or holiday |
| leave did not reconcile | verify leave policy mapping and conflict exceptions |
| payroll blocked | check locked summary and exceptions |
| tenant mismatch | inspect authenticated company claims and repository filters |
| WFH row protected | confirm `SourceType = WFH_REQUEST` |
| profile calendar empty | confirm `GET api/attendance/me/calendar` path and user/token ownership |
| bulk conflict | validate duplicate rows, expectedVersion, and calendar rules |
| protected skip | check for leave/WFH source ownership or `skipExisting=true` |

## 17. Verification checklist

### API checks
- company grid requires `Attendance.ViewAll`
- employee self-service uses token-derived user only
- manual create/update rejects `SourceType = LEAVE` / `WFH_REQUEST`
- approved leave persists configured status code
- approved half-day leave persists configured half-day code
- reconciliation is idempotent for the same source
- manual attendance creates `LEAVE_ATTENDANCE_CONFLICT` instead of overwrite
- locked months reject daily edits
- WFH insufficient checkout creates a review exception
- duplicate bulk rows fail before writes
- protected skip preserves leave/WFH rows
- non-replica-set Mongo returns transaction-required error for bulk writes

### UI checks
- grid shows exact persisted codes and lock indicators
- tooltips and `aria-label`s communicate source and date
- source-owned rows do not open normal editor
- normal rows are editable only when permitted
- HR bulk writes appear in employee own calendar after refresh

## 18. Related documentation

- [Leave, Attendance, and Payroll End-to-End Flow](leave-attendance-payroll-end-to-end-flow.md)
- [Payroll Module Current Flow](payroll-module-current-flow.md)
- [Calendar Module Current Flow](calendar-module-current-flow.md)
- [Business Modules](modules.md)
- [Work From Home module](work-from-home-module.md)
