# Attendance Module: Architecture, Workflows, Dependencies, and Operations

## 1. Purpose and scope

Attendance is the company-scoped daily record used to establish an employee's paid or unpaid time for a payroll month. It is not an isolated screen: it depends on employee identity, company work-calendar rules, configured attendance statuses, approved leave, monthly exception review, payroll locking, and workflow-owned WFH rows.

This guide documents the current backend and React implementation, including the source-linked approved-leave behavior added to the attendance grid. It also explains how attendance is created by leave and WFH workflows, how manual edits are blocked for source-owned rows, and how payroll and month validation depend on the persisted daily source of truth.

### Implementation summary

At a code level, attendance works as a controlled write model:

1. The API receives a request with authenticated company context and an employee target.
2. Services resolve the employee inside the current company and validate the request against tenant settings.
3. The write path uses a shared validator so the same business rules apply to manual edits, bulk operations, reminder-based auto-present rows, and workflow-generated rows.
4. Once a row is written, it becomes part of the monthly summary and payroll dependency chain.

This makes attendance a downstream dependency of employee identity, work calendar, leave approval, WFH approval, and monthly exception review.

## 1.1 Production hardening additions (2026-08)

The attendance write path now has one validation authority: `IAttendanceMutationValidator`. Both the single-record API and `POST api/admin/attendance/bulk` use it. The validator scopes the target employee to `CompanyId`, enforces joining/exit dates, edit guards, configured status semantics, source ownership, and optional optimistic versions. It resolves effective office schedules in this order: employee assignment, department assignment, then company default; each assignment is date-effective and tenant scoped.

Bulk requests are prevalidated in full before any attendance row is written. The current bulk service persists the prepared rows by the company/user/day identity; the MongoDB unique index remains the final duplicate safeguard. Leave- and WFH-owned records are reported as protected skips rather than overwritten.

On each working day, `AttendanceReminderHostedService` evaluates active employees once per minute in India Standard Time. It creates a missing record only after that employee's effective office schedule has started, using the company-configured active `Present` status and `SYSTEM_DEFAULT_PRESENT` source. Legacy `SYSTEM_OFFICE_START_PRESENT` rows remain replaceable defaults. At or after **4:00 PM IST**, the service sends one deduplicated in-app attendance-review notification to active Administrator, HR, and HR Executive users. HR/Admin can overwrite a default row through the normal authorized attendance APIs; approved Leave and WFH replace a default row but remain protected from ordinary edits.

The HR/Admin bulk screen also accepts `.xlsx`, `.xls`, and `.csv` imports through `AttendanceImportModal.tsx`. It offers matching Excel and CSV templates. Before submission it requires `User Id`, `Date`, and `Status`; validates optional `Check In Time`, `Check Out Time`, and `Remarks`; rejects duplicate employee/date rows and files over 500 rows; then shows API validation messages exactly. The server remains authoritative for tenant membership, working days, join/exit dates, active status configuration, source-owned leave/WFH, payroll locks, and transactions. See `attendance-bulk-marking-flow.md` for its precise contract.

Attendance roles are least-privilege: the role migration grants employees `Attendance.ViewOwn` and HR/Admin `Attendance.ViewAll`, `Attendance.CreateForEmployee`, and `Attendance.Override`. The self-service route does not depend on the `ViewOwn` row at runtime; authentication plus token-derived ownership is its enforcement boundary. `HardenAttendanceRolePermissions` grants/revokes the role records idempotently. The employee route is `api/attendance/me`; employee grid cells are read-only and correction requests are separate records.

Correction requests have a review target (five business days), reviewer, outcome and resolution note. HR/Admin use `GET/PUT api/admin/attendance/correction-requests`; employees use `POST/GET api/attendance/me/correction-requests`. Pending requests for a payroll month block `ProcessPayrollMonth` until reviewed.

`AttendanceOutboxWorker` claims and retries live in-app delivery. It records attempts, errors, exponential backoff and a `DeadLetter` terminal state after ten failed attempts. It does not itself prove SMTP/email delivery; email delivery remains the existing mail pipeline responsibility and needs an end-to-end SMTP test before operational sign-off.

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
| HR/Admin attendance API | `Codeji.CMS.API/Controllers/AttendanceController.cs`, route `api/admin/attendance` | Company-grid reads, manual management, correction review, and transactional bulk marking |
| Employee self-service API | `Codeji.CMS.API/Controllers/MyAttendanceController.cs`, route `api/attendance/me` | Token-derived own-month grid/calendar and correction requests; it never accepts a target employee ID |
| Daily service | `Codeji.CMS.Services/Attendance/AttendanceService.cs` | Employee validation, status/time normalization, source protection, persistence |
| Bulk mutation service | `Codeji.CMS.Services/Attendance/AttendanceBulkMutationService.cs`, `AttendanceMutationValidator.cs` | Prevalidates a command, protects workflow-owned rows, then writes attendance by company/user/day identity |
| Office-start automation | `AttendanceReminderHostedService.cs`, `AttendanceReminderProcessor.cs` | Marks missing eligible employees Present at their effective office start and delivers the 10:52 AM IST HR/Admin review notification |
| Edit guard | `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs` | Blocks edits to locked months, weekly offs, and company holidays |
| Repository | `Codeji.CMS.Repository/Repositories/AttendanceRepository.cs` | Company/user/date upsert and date-range queries |
| Status configuration | `AttendanceStatusSettingsController.cs`, `AttendanceStatusService.cs` | Company-specific status code rules |
| Work calendar | `WeeklyOffService.cs`, `CompanyWorkingCalendarService.cs`, Calendar module | Weekly offs and recurring/one-time holidays |
| Monthly controls | `AttendancePenaltyController.cs`, `AttendancePenaltyService.cs` | Policy, exceptions, review, validation, and locking |
| Leave reconciliation | `LeaveAttendanceReconciliationService.cs` | Converts approved leave to source-linked attendance or records a conflict |
| Frontend grid and import | `CMS-React/src/app/modules/attendance/component/AttendanceCalendar.tsx`, `BulkAttendance.tsx`, `components/AttendanceImportModal.tsx` | Monthly employee matrix, bulk marking, Excel/CSV template download and import, and lock/source display |

## 4. Data model and ownership

### 4.1 `AttendanceModel`

`Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs` is the daily source of truth.

| Field | Meaning | Owner / update rule |
|---|---|---|
| `AttendanceId` | Mongo identifier | Created by repository |
| `CompanyId` | Tenant boundary | Required in every repository filter |
| `UserId` | Immutable application identity | Resolved from the selected employee |
| `EmployeeId` | Business/payroll code | Resolved from the selected employee; not trusted from client alone |
| `Date` | Date-only work date | Normalized before persistence |
| `Status` | Company attendance code, e.g. `P`, `CL`, `SL-HALF` | Validated against active status settings |
| `CheckInTime`, `CheckOutTime` | Times for statuses that require them | Normalized by the service |
| `TotalHours` | Working duration after current break rule | Calculated by service |
| `LateCount`, `EarlyExitCount` | Inputs to monthly penalty review | Derived during attendance/month processing |
| `Remarks` | Operator note or system explanation | Manual or system-supplied |
| `SourceType`, `SourceId`, `SourceVersion` | Provenance and reconciliation reference | `LEAVE` rows are owned by leave reconciliation |

The repository performs a replace-upsert using `CompanyId + UserId + Date`. A production deployment should also enforce a Mongo unique index over that logical key; an application-side upsert alone cannot prove database-level uniqueness under every failure mode.

### 4.2 Attendance response contract

`AttendanceResponseDto` returns the normal daily fields plus `SourceType` and `SourceId`. The frontend uses this provenance rather than inferring leave from a separate visual overlay.

For a leave-generated record, a typical response conceptually contains:

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

`AttendanceStatusSetting` is company-owned. A setting controls code, display name, active state, whether times are required, paid-day fraction, unpaid-day fraction, and display order.

Default examples:

| Code | Typical meaning | Time required | Typical fraction |
|---|---|---:|---:|
| `P` | Present | Yes | paid 1 |
| `A` / `UL` | Absent or unpaid leave | No | unpaid 1 |
| `CL`, `SL`, `EL`, `COMP-OFF` | Approved full-day leave | No | paid 1 when policy/configuration permits |
| `CL-HALF`, `SL-HALF` | Approved half-day leave | No | paid 0.5 |
| `HD`, `LHD`, `WFH-HD` | Time-based half-day status | Yes | paid/unpaid 0.5 as configured |
| `WFH`, `WFH+WFO`, `ED` | Work pattern/status | Usually yes | defined by company configuration |

### Status presentation and leave-policy availability

Each tenant attendance status now stores a six-digit display color and `IsAvailableForLeaveManagement`. Attendance Settings exposes both controls. A status that requires clock-in/out cannot be enabled for leave policies. The Leave Policy form loads only active, no-time statuses with this toggle enabled; the leave service enforces the same rule before saving or approving a mapped policy. Existing records are backfilled by `2026-08-01-BackfillAttendanceStatusPresentationAndLeaveEligibility` without overwriting an existing configured color or toggle.

## Employee profile attendance calendar

Every employee profile contains a month navigator that reads attendance for the displayed month. A recorded day uses the tenant-configured attendance-status colour. Hovering or keyboard-focusing a day displays its status code, status name, date and recorded clock-in/out values. The legend below the calendar shows the codes that occur in that month.

- The signed-in employee reads only their own month through `GET /api/attendance/me/calendar?year={year}&month={month}` (the grid uses `GET /api/attendance/me/grid`). The API derives the user and company from the authenticated request; the browser does not supply a user ID and cannot use this route to read another employee.
- An authorized HR/Admin viewer opening another employee profile uses the company-grid attendance query restricted to that employee ID. That route requires `Attendance.ViewAll`.
- The calendar is read-only. Attendance marking, workflow-owned WFH/leave protection and month-lock rules remain enforced in their existing workflows.
- Status names and colours come from `GET /api/attendance-status-settings?activeOnly=true`, so an admin change in Attendance Settings is reflected in every profile month.

## Work from home workflow

WFH is additive to manual attendance. A request is company-scoped and resolves the employee from the authenticated user; callers cannot select a company or another employee. The configured policy currently enforces a maximum of **one approved WFH day per employee per Monday–Sunday week**.

1. HR/Admin configures `GET/PUT /api/wfh/policy`, selects active attendance status codes, and assigns the policy company-wide or to matching department names and/or employee IDs.
2. The employee submits `POST /api/wfh/requests`. The service validates company membership, eligibility, date range, weekly quota, policy dates, weekly offs, holidays, locked summaries, and overlapping WFH requests.
3. When `ManagerApprovalRequired` is enabled, the assigned reporting manager reviews the pending request through `/api/wfh/requests/team`; when it is disabled, the valid request is approved directly and HR/Admin is notified for review. Every created, decision, attendance-generation, conflict, check-in, and check-out action is written to `WorkFromHomeRequestLog`.
4. Approval (including direct approval) creates only source-owned `Attendance` rows: `SourceType=WFH_REQUEST`, `SourceId=RequestId`, and the approval source version. Existing manual or LEAVE rows are never overwritten; a blocking `AttendancePayrollException` is created instead.
5. The employee checks in/out through the WFH endpoints. Server UTC time is stored; checkout calculates actual hours and creates a blocking insufficient-hours exception when needed.

Normal attendance editing rejects WFH-owned rows. This retains the existing leave ownership, weekly-off/holiday, locking, summary invalidation, and payroll safety rules.

### Safe rollout and validation

Before enabling the policy in a payroll-active company, confirm weekly offs, holidays, attendance status codes, monthly locks, HR/Admin recipients and mail configuration. Use an authorized test employee/date, then remove or complete only the records created for that validation according to company retention rules.

The configured setting—not the display label—must be used by payroll calculation. Do not use a hard-coded list as the authoritative rule set.

## 5. Superseded authorization note

All attendance API routes require authenticated company context and module permissions.

| Capability | API permission | Notes |
|---|---|---|
| Read grid, statuses, exceptions, lock state | `Attendance.View` | Current implementation is company-level; it does not yet split own vs all-employee access |
| Create manual attendance | `Attendance.Create` | Also subject to edit guard and status/time validation |
| Update daily attendance, review exceptions, validate/lock month, save penalty policy | `Attendance.Edit` | “Edit” does not override an approved-leave source record or a locked month |

The current `AdminAttendanceController` name and `api/admin/attendance` route do not mean only the Administrator role may use it. Access is controlled by the assigned module permission. Role names such as HR Manager, HR Executive, and HR Generalist must therefore receive explicit, appropriately scoped permissions.

> This section describes the prior company-grid model. The following current model supersedes its statement that `Attendance.View` is company-wide.

## 5A. Current authorization, self-service, and access boundaries

All attendance APIs require authenticated company context. Employee review is intentionally separate from HR/Admin attendance operations. The own-attendance route requires authentication and an active `EmpUser`, not a tenant `ViewOwn` permission record: this prevents an incomplete role-permission migration from hiding a valid employee's own records. It remains safe because the route obtains both tenant and user identity exclusively from JWT claims.

| Capability | API / permission | Boundary |
|---|---|---|
| Employee reads own monthly grid/calendar | `GET api/attendance/me/grid`, `GET api/attendance/me/calendar` | Authenticated active employee only; server derives company and user; one employee only |
| Employee submits/reads correction requests | `api/attendance/me/correction-requests` | Authenticated active employee only; own persisted record only; one pending request per date |
| HR/Admin reads company grid | `POST api/admin/attendance/GetAllAttendanceItems`, `Attendance.ViewAll` | Same-company employees only |
| HR/Admin manual create | Attendance Create | Edit guard and status/time validation apply |
| HR/Admin edit, exception review, validate/lock, policy | Attendance Edit | Cannot overwrite source-owned or locked attendance |

`AddAttendanceViewAllPermission` grants `Attendance.ViewAll` to Admin/HR roles. A user with only `Attendance.View` cannot load the company grid or submit arbitrary employee IDs.

### Employee attendance review and correction

The employee page contains exactly one **My attendance** row and loads `api/attendance/me/grid`. Search, employee selection, bulk marking, day-cell edit modal, write actions, and Monthly Exceptions are hidden or disabled. The employee can change month and inspect their records; an empty day means no persisted record, not an automatically inferred absence.

```text
Employee identifies an incorrect persisted day
  -> enters date, issue type and reason in Report incorrect attendance
  -> POST api/attendance/me/correction-requests
  -> API verifies token company/user, actual attendance row, valid reason and no duplicate pending request
  -> AttendanceCorrectionRequest is stored as Pending
  -> active same-company HR/Admin receive persistent in-app notifications
  -> HR/Admin investigate and correct through the authorized owning workflow
```

A correction request never changes attendance by itself. It cannot bypass a monthly lock, leave/WFH source ownership, or normal attendance authorization. Current correction-request delivery is in-app HR notification; leave request email delivery is a separate flow.

| Persona | Attendance grid | Monthly exceptions | Correction responsibility |
|---|---|---|---|
| Employee with `Attendance.ViewOwn` (when role migration is applied) | Own month, read-only | Not displayed | Submit own correction request; cannot edit |
| HR/Admin with ViewAll and Create/Edit as appropriate | Company employee grid and authorized edits | Authorized operators only | Receive/review request and apply authorized correction |
| Authenticated active employee | Own month, read-only | Not displayed | Submit own correction request; cannot edit |

## 6. Daily attendance workflow

### 6.1 Grid load

For an HR/Admin company-grid session, the React calendar:

1. loads active attendance status settings;
2. loads configured weekly offs;
3. reads company holidays for the month;
4. requests approved leave information to enrich source-linked tooltips;
5. calls `POST api/admin/attendance/GetAllAttendanceItems` with the month range and displayed user IDs;
6. keys returned records by `UserId + YYYY-MM-DD` and renders the persisted status.

The employee self-service session follows the separate `GET api/attendance/me/grid` path described in section 5A. It does not load the company employee list or the leave-request overlay, because those calls would either broaden data access or depend on Leave Management permissions that are unrelated to employee attendance review.

Approved leave is **not** converted into a frontend-only `L`/`H` badge. A source-linked daily record is displayed with its persisted code—such as `CL`, `SL`, `EL`, `CL-HALF`, `SL-HALF`, or `COMP-OFF`—and a lock indicator.

### 6.2 Manual create/update

`AdminAttendanceService` applies the following sequence:

1. resolve the employee within the authenticated company;
2. verify a submitted employee code matches that employee when supplied;
3. call `AttendanceEditGuard`;
4. load and validate the selected company status;
5. parse and validate times if the status requires times;
6. load any existing company/user/date record;
7. reject edits when `SourceType == LEAVE`;
8. calculate hours and persist by company/user/date.

The guard rejects a locked monthly summary, configured weekly offs, and holidays. The leave-source check rejects normal manual overwrite even if the record is otherwise on an editable working day.

### 6.3 Frontend presentation and accessibility

Leave-generated cells:

- show the exact persisted status code, centered in the cell;
- show a lock icon indicating that the source is approved leave;
- expose an `aria-label` containing the code, status meaning, leave type where available, duration, approval/source state, and date;
- provide the same details in a tooltip;
- open an explanatory conflict-resolution message instead of the normal attendance editor;
- are excluded from bulk marking before the client makes write calls.

The service remains authoritative: direct API requests to create/update a record that already has `SourceType = LEAVE` return a conflict response. Client-side prevention is usability protection, not the security boundary.

### 6.4 Bulk marking

Bulk marking is a single, server-side command:

`POST /api/admin/attendance/bulk`

The caller must hold both `Attendance.CreateForEmployee` and `Attendance.Override`. An employee's own calendar never invokes this route.

#### Request contract and UI modes

`AttendanceBulkMutationDto` contains 1 to 500 `rows` and the `skipExisting` option. Each row supplies the target `userId`, work `date`, status code, optional clock times, operator remarks, and optional `expectedVersion`.

```json
{
  "rows": [
    {
      "userId": "employee-user-id",
      "date": "2026-07-30",
      "status": "P",
      "checkInTime": "09:00",
      "checkOutTime": "18:00",
      "remarks": "HR bulk correction",
      "expectedVersion": 3
    }
  ],
  "skipExisting": false
}
```

`AttendanceCalendar.tsx` exposes two operator actions for one chosen date/status:

- **Bulk Apply** builds a row for every employee currently loaded in the grid.
- **Selected Employees** builds a row only for checked employees.

The current UI sends `skipExisting: false` by default. Therefore a writable manual or `SYSTEM_OFFICE_START_PRESENT` record on the same employee/date is updated; it is not inserted again. Setting `skipExisting: true` preserves existing rows and creates only missing rows. The browser does not impose `09:00`/`18:00`; when a selected status needs times, the server resolves the effective schedule for each employee.

#### Prevalidation and ownership rules

Before any database transaction starts, the service rejects duplicate logical keys inside the request (`UserId + calendar date`). It then prepares every row through `IAttendanceMutationValidator`:

1. Tenant and actor come from the authenticated request; they are never supplied by the browser.
2. The target employee must be active, not deleted, and belong to that tenant.
3. The date cannot be future-dated, before joining, or after exit.
4. The attendance edit guard checks the month lock, weekly off, and company holiday.
5. The status must be active in the tenant. Time-required statuses receive either validated custom times or the effective schedule; non-time statuses reject submitted clock times.
6. A supplied `expectedVersion` must match the persisted row version, otherwise the command fails with `ATTENDANCE_VERSION_CONFLICT` instead of silently overwriting a concurrent update.
7. A row owned by approved leave (`SourceType=LEAVE`) or WFH (`SourceType=WFH_REQUEST`) is never overwritten. It is counted as `protectedSkipped`, while other valid rows can continue.

Any other invalid row causes the command to fail before the write transaction. This avoids a partly validated import. The protected Leave/WFH exception is deliberate: those rows belong to their source workflow and are reported rather than treated as an operator error.

#### Persisted result, overwrite behavior, and duplicate safety

The logical attendance identity is `CompanyId + UserId + Date`. The write uses a `ReplaceOne` upsert filtered by that exact key:

- no matching row: creates one and returns it in `created`;
- matching ordinary/manual row with `skipExisting: false`: replaces that same row and returns it in `updated`;
- matching row with `skipExisting: true`: makes no write and returns it in `skipped`;
- matching Leave/WFH row: makes no write and returns it in `protectedSkipped`.

The application must retain the MongoDB unique index for `CompanyId + UserId + Date`; it is the final invariant against duplicate records during races or recovery. Do not retry a failed historical import blindly: first reconcile actual records by tenant, user, and date range, then retry only the missing or intended rows.

A successful command returns:

```json
{ "created": 12, "updated": 3, "skipped": 1, "protectedSkipped": 2 }
```

The React grid reloads after success, and the employee's token-scoped calendar subsequently reads the same persisted rows through `api/attendance/me/calendar` or `api/attendance/me/grid`.

### 6.5 Office-start Present and 10:52 review

```text
Every minute (IST)
  -> find active employees without today's attendance
  -> resolve each employee's effective schedule
  -> only after that schedule's StartTime, validate and create missing P rows
  -> tag rows SYSTEM_OFFICE_START_PRESENT; never replace an existing row
At/after 10:52 AM IST
  -> find today's SYSTEM_OFFICE_START_PRESENT rows by company
  -> create one persisted, deduplicated notification per company/day
  -> deliver it to active Administrator, HR, and HR Executive recipients
  -> HR/Admin review and bulk-overwrite ordinary rows as needed
```

The schedule is safe to retry after a service restart: automatic creation uses `CreateMissingOnly`, and the review notification target is `attendance-review-YYYY-MM-DD`. It does not create records before an employee's scheduled start, on a weekly off/holiday/locked date, before joining, after exit, or without an active valid `P` status and effective schedule, because the shared mutation validator rejects those cases. It intentionally does not send the prior 6 PM notification or create rows at 6 PM.

## 7. Leave-to-attendance dependency

### 7.0 Canonical business date

Attendance and leave share a UTC-midnight business-date contract. A leave request's approved `DateOnly` value is stored as UTC midnight and reconciliation stores generated `Attendance.Date` at that identical value. This prevents a local-midnight MongoDB conversion from putting an Aug 5 leave into an earlier attendance column. Historical `18:30 UTC` rows are handled by `NormalizeLegacyLeaveBusinessDates`; it changes only safe source-owned single-day rows and never overwrites an occupied target day.

### 7.1 Policy mapping

Leave policy configuration contains `AttendanceStatusCode`. It must reference an active, no-time attendance status in the same company. Examples include `CL`, `SL`, `EL`, `UL`, `COMP-OFF`, `CL-HALF`, and `SL-HALF`.

Policies without a mapping remain readable for backward compatibility but cannot produce correct attendance on approval. HR must supply a valid mapping before relying on the leave-to-attendance flow.

### 7.2 Approval reconciliation

When a leave request is accepted, `LeaveAttendanceReconciliationService`:

1. reloads the request, employee, policy, and status in the current company;
2. calculates included dates using company weekly offs and holidays plus the policy’s inclusion flags;
3. creates or updates the attendance record with the mapped status;
   its date is the exact approved UTC business date;
4. sets `SourceType = LEAVE`, `SourceId = LeaveRequestId`, and `SourceVersion = LeaveRequest.Version`;
5. writes a system remark identifying the source;
6. invalidates affected unlocked monthly summaries so the month is rebuilt from daily data.

The update is idempotent for the same source: repeating reconciliation updates the same source-linked record rather than creating a duplicate.

### 7.3 Manual-attendance conflict

If an existing row is manual or has another source, reconciliation does not overwrite it. It creates a blocking `LEAVE_ATTENDANCE_CONFLICT` exception with the existing and requested statuses. The conflict must be reviewed through the authorized exception/correction process before the month can be safely locked.

When a previously approved request is reversed, reconciliation removes only rows whose source matches that leave request/version. It must never delete a manual attendance row.

### 7.4 Correction responsibility

Changing source-linked attendance directly would make leave balances, approval history, and payroll disagree. A correction must originate from the leave workflow or an explicitly implemented, audited leave-attendance conflict-resolution workflow. The current user-facing message accurately states this requirement; it is not a general manual-edit path.

## 8. Work calendar dependency

Attendance uses the same company work-calendar concepts as Leave and Payroll:

- **Weekly offs**: stored company configuration, defaulting to Saturday/Sunday when absent;
- **Holidays**: company calendar items, including recurring annual holidays;
- **Employee employment period**: joining/exit dates define eligibility for monthly summary and payroll;
- **Policy inclusion flags**: a leave policy can separately decide whether holidays/weekends inside an approved leave range consume leave and generate attendance.

The attendance edit guard rejects manual changes on a weekly off or holiday. Leave reconciliation can still process dates when the leave policy explicitly includes them. This distinction is intentional and should not be replaced with frontend-only date checks.

## 9. Monthly validation, exceptions, and locking

### 9.1 Endpoints

| Endpoint | Permission | Purpose |
|---|---|---|
| `GET api/attendance/penalty/policy` | View | Read effective penalty rules |
| `POST api/attendance/penalty/policy` | Edit | Save policy |
| `POST api/attendance/penalty/exceptions/recalculate` | Edit | Rebuild month exceptions |
| `POST api/attendance/penalty/exceptions/search` | View | List/filter exceptions |
| `PATCH api/attendance/penalty/exceptions/{id}/review` | Edit | Review a specific exception |
| `GET api/attendance/penalty/month/lock-status` | View | Read employee/month lock state |
| `POST api/attendance/penalty/month/validate-lock` | Edit | Validate eligible records and lock the month when valid |

### 9.2 Summary lifecycle

`MonthlyAttendanceSummary` is the payroll-facing representation for one employee/month. It contains expected and eligible working days, status totals, paid/unpaid days, missing data, penalty counts, blocking exception state, approval/lock metadata, and version information.

Practical lifecycle:

```text
Daily attendance + source-linked leave
        -> validation / exception generation
        -> exception review
        -> approved monthly summary
        -> locked summary
        -> payroll eligibility
```

Once locked, `AttendanceEditGuard` blocks normal daily attendance changes. The current application does not yet provide a complete audited reopen/relock correction workflow. Do not instruct users to bypass the lock by database modification.

## 10. Payroll dependency

Payroll consumes attendance as a controlled prerequisite:

1. the payroll month must be eligible for processing;
2. the employee must have an effective salary structure;
3. a monthly attendance summary must exist, be approved, and be locked;
4. blocking attendance exceptions must be resolved;
5. attendance status paid/unpaid fractions and approved penalties determine loss-of-pay days;
6. company work-calendar and employment eligibility determine expected days and proration;
7. payroll creates the payroll record/payslip from that locked state.

```text
Daily statuses + approved leave mappings
   -> paid/unpaid day fractions
   + approved attendance penalties
   -> loss-of-pay days and amount
   -> prorated earnings, deductions, net pay
```

Changing daily attendance after payroll would invalidate the payroll result. That is why locking and a future payroll revision workflow are required controls.

## 11. Notifications and reporting dependency

Leave approval may also send notifications and update employee leave balance. Attendance reconciliation runs after the core leave status/balance change. The attendance grid is therefore a read model of persisted attendance, while the leave module remains the source for leave policy, approval, balance, and reason.

Reports and dashboard metrics should use persisted attendance records and locked monthly summaries, not derive payroll time solely from the frontend calendar display.

## 12. Current safeguards and remaining limitations

### Implemented safeguards

- Company-scoped attendance lookup and write filters.
- Employee identity resolution before writes.
- Date normalization and company status validation.
- Time validation and derived hour calculation.
- Weekly-off, holiday, and locked-month edit guard.
- Leave reconciliation using source-linked, idempotent attendance records.
- Blocking exception instead of silent manual-attendance overwrite.
- UI and API protection against manual or bulk overwrite of leave-generated records.
- Exact persisted leave status, source indicator, tooltip, and accessible text in the attendance grid.

### Limitations to address before describing the entire process as fully production-hardened

1. **Multi-document leave workflow:** leave status, balance, attendance reconciliation, monthly-summary invalidation, and notifications span multiple MongoDB documents. The leave service has transaction handling for the core leave/balance transition, but attendance reconciliation and downstream effects require an end-to-end failure/retry strategy and production replica-set validation.
2. **Bulk attendance infrastructure:** bulk writes are transactional, but require MongoDB replica-set support. A standalone deployment fails safely and must not be treated as a successful import.
3. **Correction lifecycle:** no complete audited reopen/relock and leave-attendance conflict-resolution UI is implemented yet.
4. **Permission rollout:** verify the granular role-permission migrations in every tenant. Self-service attendance remains available to active authenticated employees during a migration gap, but HR/company-grid capability still requires its explicit `ViewAll` permission.
5. **Database invariant:** confirm and monitor the unique index for `CompanyId + UserId + Date` in every deployed tenant database.
6. **Payroll revisions:** an explicit audited payroll recalculation/revision process is still needed after a permitted correction.

## 13. Operational runbook

### Configure a new company

1. Configure weekly offs.
2. Create company holidays, including recurring holidays where applicable.
3. Review active attendance status definitions and paid/unpaid fractions.
4. Configure each leave policy with a compatible no-time attendance status code.
5. Grant Attendance permissions to the intended HR roles.
6. Verify an employee, a manual day, an approved full-day leave, and an approved half-day leave in a non-production tenant before payroll use.

### Month-end procedure

1. Confirm daily attendance is complete.
2. Resolve or review attendance exceptions, including `LEAVE_ATTENDANCE_CONFLICT`.
3. Validate the month.
4. Confirm calculated paid/unpaid days and penalties.
5. Lock the month only after approval.
6. Process payroll from the locked summary.

### Incident triage

| Symptom | First check |
|---|---|
| Leave code is missing in grid | Confirm attendance response contains the source-linked row and policy has `AttendanceStatusCode` |
| Cell says it cannot be edited | Check for `SourceType = LEAVE`, locked summary, weekly off, or holiday |
| Approved leave did not reconcile | Check leave policy mapping, work-calendar inclusion, and any `LEAVE_ATTENDANCE_CONFLICT` exception |
| Payroll cannot run | Check approved/locked summary, blocking exceptions, salary structure, and payroll month rules |
| Wrong tenant data appears | Stop processing and inspect authenticated company claims and repository filters; do not use client-supplied tenant selection |
| WFH day cannot be edited | Check for `SourceType = WFH_REQUEST`; use the WFH workflow, not manual attendance editing |
| WFH clock action is unavailable | Confirm the request is approved, covers today, policy is enabled and the user owns the request |
| Employee profile says “No attendance this month” while HR can see rows | Confirm the running backend includes `MyAttendanceController` self-service route, the employee is active in `EmpUser`, then inspect `GET api/attendance/me/calendar`; the route must derive the token user and must not call the HR company-grid endpoint |
| Bulk Apply reports a conflict | Check duplicate request dates, status/timing, join/exit range, calendar/lock rule, and `expectedVersion`; do not retry until the returned error and existing records are reconciled |
| Bulk Apply skips a row | Check `skipExisting` for a manual row, or `SourceType` for `LEAVE`/`WFH_REQUEST`; use the owning Leave/WFH workflow rather than replacing source-owned attendance |

### WFH source-owned attendance

Approved WFH requests create attendance rows with `SourceType = WFH_REQUEST`, request ID and source version. The employee clocks in/out through the WFH module; server time produces check-in, check-out and total hours. Insufficient duration creates a blocking payroll exception for review. Manual attendance administration must reject WFH-owned rows, just as it rejects leave-owned rows. WFH also respects weekly offs, holidays and the monthly attendance lock before a row is generated.

See [Work From Home module](work-from-home-module.md) for policy, request, notification and clocking behavior.

## 14. Verification checklist

### Automated and API checks

- A company cannot query or change another company’s attendance.
- A manual create/update returns a conflict for an existing `SourceType = LEAVE` record.
- A manual create/update returns a conflict for an existing `SourceType = WFH_REQUEST` record.
- An approved full-day leave persists its configured code (for example `CL`), not `L`.
- An approved half-day leave persists its configured half-day code (for example `CL-HALF`), not `H`.
- Reconciliation rerun does not create another row for the same company/user/date/source.
- Existing manual attendance produces `LEAVE_ATTENDANCE_CONFLICT` instead of replacement.
- Locked months reject create and update.
- WFH check-out below configured minimum hours creates a reviewable exception rather than changing the source row manually.
- A bulk command with the same employee/date twice returns `ATTENDANCE_BULK_DUPLICATE_ROW` before it writes anything.
- Bulk Apply creates a missing manual row, updates one existing manual row when `skipExisting=false`, and preserves it when `skipExisting=true`.
- Bulk Apply preserves Leave/WFH-owned rows and reports them as `protectedSkipped`.
- A non-replica-set MongoDB returns `ATTENDANCE_BULK_TRANSACTION_REQUIRED` and does not commit bulk rows.

### Browser checks

- The grid shows exact leave codes plus a lock icon.
- Tooltip and `aria-label` communicate status, source, date, duration, and approval state.
- Light and dark themes retain readable status contrast.
- Clicking or bulk-selecting a leave-generated record shows a correction-workflow message rather than an edit dialog.
- Normal manual attendance remains editable only when the date is an eligible, unlocked working day and the user has permission.
- After HR marks a date in bulk, the affected employee's own `/profile/about` calendar shows that same persisted status after refresh; another employee's rows never appear there.

## 15. Related documentation

- [Leave, Attendance, and Payroll End-to-End Flow](leave-attendance-payroll-end-to-end-flow.md)
- [Payroll Module Current Flow](payroll-module-current-flow.md)
- [Calendar Module Current Flow](calendar-module-current-flow.md)
- [Business Modules](modules.md)
- [Work From Home module](work-from-home-module.md)
