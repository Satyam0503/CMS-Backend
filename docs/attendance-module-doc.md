# Attendance Module — End-to-End Design, UI, and Business Flow

## 1. Overview

The Attendance module is the daily time-tracking surface for the Codeji CMS system. It is the source of truth for employee presence, absence, leave-mapped attendance, WFH status, and the downstream payroll and monthly exception workflows.

This document covers:
- Backend architecture and ownership
- UI surfaces in `CMS-React`
- Every major user flow
- Connectivity with employee identity, holidays, leave, WFH, and payroll locks
- Data model, status configuration, and source ownership
- File and route references for implementation and debugging


## 2. Architecture and boundaries

### 2.1 Tenant and identity boundary

Attendance is tenant-scoped by `CompanyId`. The persisted record key is:
- `CompanyId + UserId + Date`

The backend never trusts the browser for company or employee ownership. Every request derives `CompanyId` and `UserId` from JWT claims or module permission context.

### 2.2 Source of truth

`AttendanceModel` in `Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs` is the daily source of truth. It is the authoritative input for:
- employee monthly attendance grid
- leave reconciliation
- WFH attendance creation
- payroll month validation and exception resolution

The record includes source provenance:
- `SourceType`
- `SourceId`
- `SourceVersion`

This allows the system to distinguish manual edits from generated leave or WFH attendance.

### 2.3 Main layers

| Layer | Key files | Responsibility |
|---|---|---|
| API | `Codeji.CMS.API/Controllers/AttendanceController.cs`, `MyAttendanceController.cs` | attendance routes for HR/Admin and employee self-service |
| Service | `Codeji.CMS.Services/Attendance/AttendanceService.cs` | validation, normalization, persistence |
| Bulk | `Codeji.CMS.Services/Attendance/AttendanceBulkMutationService.cs`, `AttendanceMutationValidator.cs` | prepare and persist bulk attendance rows |
| Status config | `AttendanceStatusService.cs`, `AttendanceStatusSettingsController.cs` | tenant-specific code definitions |
| Work calendar | `WeeklyOffService.cs`, `CompanyWorkingCalendarService.cs`, `CalendarController.cs` | weekly offs and holidays |
| Leave reconciliation | `LeaveAttendanceReconciliationService.cs` | map accepted leave to attendance rows |
| WFH workflow | `WorkFromHomeService.cs`, `WorkFromHomeController.cs` | WFH request approval and source-owned attendance |
| Monthly exceptions | `AttendancePenaltyService.cs`, `MonthlyAttendanceExceptions.tsx` | exception detection, review, and resolution |
| Frontend | `CMS-React/src/app/modules/attendance/*` | attendance UI, company grid, employee mode |


## 3. Core data model

### 3.1 `AttendanceModel`

Key fields:
- `CompanyId` — tenant boundary
- `UserId` — employee identity in the tenant
- `EmployeeId` — payroll/business employee code
- `Date` — normalized business date (UTC midnight)
- `Status` — attendance code
- `CheckInTime`, `CheckOutTime` — optional clock times
- `TotalHours` — derived working duration
- `LateCount`, `EarlyExitCount` — inputs for exception/penalty review
- `Remarks` — user or system note
- `SourceType` / `SourceId` / `SourceVersion` — provenance

The repository performs an upsert keyed by `CompanyId + UserId + Date`. A production deployment must hold a Mongo unique index on that logical key.

### 3.2 Status definitions

Tenant-specific attendance statuses are defined in `AttendanceStatusSetting`. Each code stores:
- canonical `Code`
- display `Name`
- `ColorHex`
- active state
- `RequiresTime`
- paid/unpaid day fraction
- `IsAvailableForLeaveManagement`

The React UI uses these definitions to render:
- status dropdowns
- legends
- timing requirements

Statuses that require time capture are currently:
- `P`
- `WFH`
- `HD`
- `ED`
- `LHD`
- `WFH+WFO`
- `WFH-HD`

No-time statuses are used for mapped leave and some exceptions.


## 4. Permissions and access

### 4.1 Backend authorization

The attendance APIs are protected by module permissions. The main permission surface includes:
- `Attendance.ViewAll` — company attendance grid read access
- `Attendance.CreateForEmployee` — create manual attendance for employees
- `Attendance.Override` — override protected rows when permitted
- `Attendance.View` / `Attendance.ViewOwn` — self-service and read-only access

The employee self-service route derives the employee from JWT and does not rely on a supplied employee ID.

### 4.2 React permission handling

In `CMS-React/src/app/modules/attendance/EmployeeAttendance.tsx`:
- `canViewAllAttendance` is granted to Administrators or users with `Attendance.ViewAll`
- `canManageAttendance` is granted to Administrators or users with `Attendance.CreateForEmployee` / `Attendance.Override`
- employees without view-all see a single row labeled `My attendance`

The UI hides bulk controls, employee search, and edit actions for non-admin users.


## 5. Frontend surfaces

### 5.1 EmployeeAttendance page

File: `CMS-React/src/app/modules/attendance/EmployeeAttendance.tsx`

This page is the main attendance screen. It builds context and passes props into the attendance grid.

Responsibilities:
- determine whether the current user is employee or HR/Admin mode
- fetch employee options for the company grid
- initialize search and date-range selection
- load attendance stats counts
- show correction request UI for employee mode
- preserve read-only restrictions for self-service

Key features:
- search input with debounce
- period selector using `attendancePeriods`
- summary stats cards for Present, Leave, Absent, WFH, Half Day, Exceptions
- correction request form (employee mode)

When `canViewAllAttendance` is false, the page resolves to one employee row:
- `userId` — current user
- `employeeId` — blank for the row label
- `fullName` — `My attendance`


### 5.2 AttendanceCalendar grid

File: `CMS-React/src/app/modules/attendance/component/AttendanceCalendar.tsx`

This component renders the company or own attendance matrix. It is the central attendance UI.

It loads:
- active attendance status settings via `api/attendance-status-settings?activeOnly=true`
- weekly offs from `api/attendance/weekly-offs`
- approved leave requests when in company view
- holidays from `api/holidays` or `api/attendance/weekly-offs` depending on mode

It presents:
- date columns for the selected range
- per-employee rows
- editable cells when `readOnly` is false
- status selection and optional time inputs in a modal
- `SourceType` provenance markers for leave/WFH-owned rows

The component also supports:
- page navigation and pagination
- employee-specific row scrolling when an exception edit event is raised
- calculation of visible dates using dayjs
- statistical summary callback via `onStatsChange`

### 5.3 MonthlyAttendanceExceptions

File: `CMS-React/src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx`

This component provides exception triage for locked or problematic attendance months.

Features:
- load exceptions for a selected payroll month
- recalculate exceptions via `api/attendance/penalty/exceptions/recalculate`
- review individual exceptions through `PATCH api/attendance/penalty/exceptions/{id}/review`
- resolve individual attendance by posting `api/attendance/penalty/exceptions/{id}/resolve-attendance`
- bulk review of LHD/ED exceptions
- show affected dates and exception descriptions

It is used when attendance records require HR/Admin review, not by employees in self-service mode.

### 5.4 Profile attendance calendar

File: `CMS-React/src/app/modules/users/components/LeaveCalendar.tsx`

In employee profile pages, the attendance calendar is purely read-only. It loads:
- attendance rows for the displayed month via `getAllAttendance` or `api/attendance/me/calendar`
- status settings for color and label

It is a visualization surface only and does not permit edits or corrections.

### 5.5 AttendanceStatusSettings UI

File: `CMS-React/src/app/modules/attendance/AttendanceStatusSettings.tsx`

This screen allows HR/Admin to configure tenant attendance statuses. It controls:
- active attendance codes
- display names and colors
- whether a code requires time
- whether the code is eligible for leave management mapping

The definitions here update both the attendance grid and the leave policy mapping UI.


## 6. API and service flows

### 6.1 Employee self-service attendance flow

Flow:
1. Employee opens Attendance page.
2. `EmployeeAttendance.tsx` resolves `canViewAllAttendance` false.
3. It sets a single employee row for the current user.
4. `AttendanceCalendarGrid` is rendered with `readOnly=true`.
5. The UI calls `GET api/attendance/me/grid` or `GET api/attendance/me/calendar`.
6. Backend derives `CompanyId` and `UserId` from JWT.
7. Service filters attendance only for that employee and date range.
8. The calendar displays persisted days. Missing dates are empty, not inferred absent.

Employee correction request flow:
1. Employee selects a date and enters a descriptive reason.
2. `POST api/attendance/me/correction-requests` is called.
3. Backend validates:
   - authenticated employee belongs to the company
   - attendance row exists for the selected date
   - no pending correction already exists for that employee/date
4. A new `AttendanceCorrectionRequest` is stored with `Pending` status.
5. HR/Admin recipients receive a notification.
6. Correction requests do not mutate attendance directly.

UI logic in `EmployeeAttendance.tsx` handles error statuses:
- `409` → duplicate pending request
- `404` → missing attendance record
- fallback → generic retry message


### 6.2 HR/Admin company attendance flow

Flow:
1. HR/Admin opens the Attendance page.
2. `EmployeeAttendance.tsx` loads employee list via `getAllEmployees`.
3. `AttendanceCalendarGrid` loads active status settings, holidays, weekly offs, and the selected date range.
4. The grid shows one row per employee with attendance cells for each day.
5. HR/Admin may click a cell to open an edit modal.

Edit modal behavior:
- status dropdown is built from active status settings
- times are required only for status definitions with `RequiresTime` true
- remark field is optional
- the modal identifies if the underlying row is source-owned

Submit flow:
1. `addEditAttendance` posts `AdminAttendanceCreateDto` to `api/admin/attendance`.
2. Backend resolves `UserId` and `EmployeeId` from the request.
3. `AttendanceService` calls `IAttendanceMutationValidator`.
4. Validator checks:
   - company and user belong together
   - date is within join/exit range
   - date is not holiday, weekly off, or locked if not permitted
   - requested status code is active and configured
   - required times are present when needed
   - source ownership is respected (leave/WFH source-owned rows are protected)
5. Service calculates `TotalHours`, `LateCount`, `EarlyExitCount` if applicable.
6. Repository upserts the row.
7. Response returns the updated attendance row for the grid.

The UI disables mutation actions when `readOnly=true` or permission is missing.


### 6.3 Bulk attendance marking and import

File: `CMS-Backend-Core/docs/attendance-bulk-marking-flow.md`

Key backend routes:
- `POST /api/admin/attendance/bulk`
- `POST /api/admin/attendance/bulk-import/validate`
- `POST /api/admin/attendance/bulk-import/commit`

Bulk marking flow:
1. UI collects rows and optional `SkipExisting`.
2. Client sends `AttendanceBulkMutationDto`.
3. `AttendanceBulkMutationService` normalizes business dates, rejects duplicates, and validates every row.
4. Rows are persisted by `CompanyId + UserId + Date`.
5. Leave and WFH source-owned rows are protected and counted as `protectedSkipped`.

Excel import flow:
1. Admin uploads `.xlsx` file through `AttendanceImportModal.tsx`.
2. Client posts the file to `bulk-import/validate`.
3. Server verifies:
   - `.xlsx` extension and ZIP signature
   - workbook is not empty
   - worksheet name is `Attendance Import`
   - required unique headers exist
   - no formula cells
   - at most 500 data rows
4. The server resolves `Employee Code` to an internal `UserId` scoped to the company.
5. Each row is validated individually and returned with outcome statuses:
   - `READY_CREATE`
   - `READY_UPDATE`
   - `UNCHANGED`
   - `PROTECTED`
   - `INVALID`
6. The browser shows validation messages and row-level results.
7. For commit, `AttendanceBulkImportCommitRequestDto` includes `IdempotencyKey`, `SkipExisting`, and resolved rows.
8. The server re-resolves employee codes and re-runs the shared validator before writing.

Important contract rules:
- Employee code is `EmpUser.EmployeeId`
- duplicate rows in the upload are rejected as `ATTENDANCE_IMPORT_DUPLICATE_ROW`
- the server is authoritative for company membership, join/exit dates, status config, and source ownership
- commit preserves counts for created, updated, skipped, and protected rows

The frontend import modal is implemented in `CMS-React/src/app/modules/attendance/components/AttendanceImportModal.tsx`.


### 6.4 Attendance reminder and default present rows

Backend process:
- `AttendanceReminderHostedService` runs periodically in India Standard Time.
- For active employees missing attendance, it resolves effective schedule from:
  1. employee assignment
  2. department assignment
  3. company default schedule
- After the employee's scheduled start time, it creates a `Present` row with `SourceType = SYSTEM_OFFICE_START_PRESENT` if no row exists.
- It never replaces manual rows, leave rows, WFH rows, or another system-created row.

Review notification flow:
- `AttendanceReminderProcessor` sends a single deduplicated in-app notification at 10:52 AM IST to active Administrator, HR, and HR Executive users when default present rows exist.
- The notification asks the reviewer to overwrite any exception such as Absent, Sick Leave, or Earned Leave.

The UI for review is the standard attendance grid. The notification is delivered via the existing notification system.


### 6.5 Leave reconciliation flow

The attendance module is downstream from Leave Management.

When leave is accepted:
1. `LeaveAttendanceReconciliationService.ReconcileAcceptedLeaveAsync` runs.
2. It maps included leave days to attendance rows using the policy's configured attendance code.
3. It writes source-owned rows:
   - `SourceType = LEAVE`
   - `SourceId = LeaveRequestId`
   - `SourceVersion = LeaveRequest.Version`
4. Existing manual or WFH attendance is not silently overwritten.
5. If a conflict exists, a blocking attendance exception is created instead.

When leave is rejected or reversed:
- the service removes/reverses only records owned by that leave source/version
- unlocked monthly summaries are invalidated for recalculation

This allows the attendance grid to display approved leave as attendance status without separate overlays.


### 6.6 WFH workflow flow

WFH is implemented under the Leave Management/Attendance convergence.

UI file: `CMS-React/src/app/modules/attendance/WorkFromHome.tsx`

Flow:
1. HR/Admin configures WFH policy via `GET/PUT /api/wfh/policy`.
2. The employee submits `POST /api/wfh/requests`.
3. The service validates:
   - company membership and active employee
   - policy applicability
   - date range and weekly quota
   - weekly offs, holidays, locked summaries, and overlapping requests
4. If manager approval is required, the manager reviews pending requests.
5. Approval creates source-owned attendance rows:
   - `SourceType = WFH_REQUEST`
   - `SourceId = WorkFromHomeRequestId`
6. Existing manual or leave-owned rows are never overwritten without a blocking exception.
7. Employee checks in/out through WFH endpoints; times are stored in UTC.
8. Checkout calculates actual hours and may create an insufficient-hours exception if required.

Normal HR/Admin manual editing rejects WFH-owned rows to preserve source ownership.


## 7. Monthly exception and payroll lock connectivity

Attendance is tightly connected with monthly validation and payroll readiness.

### 7.1 Monthly exceptions

`AttendancePenaltyService` identifies attendance issues such as:
- too many late arrivals or early departures
- missing required hours
- leave/attendance conflicts

`MonthlyAttendanceExceptions.tsx` is the frontend remediation screen.

HR/Admin can:
- recalculate exceptions
- resolve a specific attendance issue by posting a corrected attendance row
- review and apply bulk decisions for recurring exceptions

### 7.2 Payroll locks

Attendance edits are blocked when the payroll month is locked. The edit guard in `AttendanceEditGuard.cs` enforces:
- no mutation after summary validation/locking
- no change on locked days via manual or bulk flows

WFH approval, leave reconciliation, and attendance exception resolution all respect payroll lock state.


## 8. UI connectivity and behavior

### 8.1 Status options and time requirements

`AttendanceCalendar.tsx` loads active statuses from `api/attendance-status-settings?activeOnly=true`. It maps each code into:
- a dropdown option label
- `requiresTime` metadata

The grid uses these values to enforce client-side validation and to render time inputs only when required.

### 8.2 Weekly offs and holidays

The grid requests weekly off configuration from `api/attendance/weekly-offs`.

Holiday data is loaded from the calendar service and used to:
- mark non-working days visually
- help the backend reject invalid manual attendance on holidays

### 8.3 Search and employee selection

`EmployeeAttendance.tsx` loads employees when the user has company view.

The page supports:
- search input with 500ms debounce
- filtering by name
- employee row selection and pagination

For employee self-service mode, there is no search and only one row.

### 8.4 Modal editing experience

When a user clicks an attendance cell, the modal shows:
- current attendance status
- optional check-in and check-out fields
- remarks
- source ownership warnings

The confirmation modal is used for protective feedback when an employee is not available or when editing a joined-before date.

### 8.5 Summary cards and notifications

The page displays cards for:
- Present
- Leave
- Absent
- WFH
- Half Day
- Exceptions

Clicking a summary card can open a small modal showing employee names.

Notifications are delivered through the shared notification system and include:
- attendance review reminders for default present records
- correction request alerts to HR/Admin
- leave/WFH request updates


## 9. Error behavior and edge cases

### 9.1 Protected source-owned attendance

The shared validator protects rows owned by:
- leave reconciliation (`SourceType = LEAVE`)
- WFH approvals (`SourceType = WFH_REQUEST`)
- system default present rows (`SYSTEM_OFFICE_START_PRESENT`)

HR/Admin cannot overwrite these rows through ordinary manual or bulk edits. The UI may still show the row, but the server rejects unauthorized mutations.

### 9.2 Duplicate prevention

Bulk import and manual writes deduplicate by `CompanyId + UserId + Date`. The server rejects duplicate upload rows at validation time and uses the Mongo unique index as the final safeguard.

### 9.3 Locked date handling

Attendance mutation APIs consult the edit guard and payroll lock state. Locked months and locked dates are rejected with explicit errors. The frontend should not assume a row can be updated without a successful backend validation.

### 9.4 Incorrect attendance reporting

Employee correction requests are audit-only. They create `AttendanceCorrectionRequest` records and notify HR/Admin. They do not directly change attendance or bypass lock/source rules.

### 9.5 Source provenance display

The grid includes `SourceType` and `SourceId` metadata in the backend response. The UI uses this to indicate leave-led or WFH-led attendance and to block edits at the client layer for better usability.


## 10. Connectivity to related modules

### 10.1 Employees

Attendance depends on the employee master for:
- `UserId` resolution
- `EmployeeId` display
- join/exit date validation
- active/inactive status

If the employee is missing or company-scoped incorrectly, attendance write is rejected.

### 10.2 Leave Management

Accepted leave can generate attendance rows. The attendance module uses:
- leave policy attendance mapping
- leave request decision results
- leave summary state for conflict detection

Leave and attendance must stay synchronized via source-owned reconciliation.

### 10.3 Work-from-home

WFH is a special attendance source. Approved WFH requests create daily attendance rows with source identity and check-in/out behavior.

### 10.4 Calendar and holidays

The attendance module uses the shared calendar module for:
- tenant weekly off configuration
- public/optional holidays
- business-day calculation for correction SLAs and payroll month boundaries

### 10.5 Payroll and monthly validation

Attendance is an input to payroll readiness. Monthly validation identifies exceptions before payroll can be finalized.

Locking a payroll month prevents further attendance edits and ensures payroll stability.


## 11. File and route references

### Backend
- `Codeji.CMS.API/Controllers/AttendanceController.cs` — HR/Admin attendance routes
- `Codeji.CMS.API/Controllers/MyAttendanceController.cs` — employee self-service attendance routes
- `Codeji.CMS.Services/Attendance/AttendanceService.cs` — attendance write and persist logic
- `Codeji.CMS.Services/Attendance/AttendanceBulkMutationService.cs` — bulk attendance write
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs` — shared validation authority
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs` — leave reconciliation
- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs` — locked date and source protection checks
- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs` — WFH workflow integration
- `Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs` — attendance persistence entity
- `Codeji.CMS.Repository/Repositories/AttendanceRepository.cs` — upsert and range queries
- `Codeji.CMS.API/Controllers/WorkFromHomeController.cs` — WFH endpoints
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs` — monthly exception / penalty rules
- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs` — status definitions
- `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs` — status settings management

### React frontend
- `CMS-React/src/app/modules/attendance/EmployeeAttendance.tsx` — main attendance page
- `CMS-React/src/app/modules/attendance/component/AttendanceCalendar.tsx` — attendance matrix grid
- `CMS-React/src/app/modules/attendance/AttendanceServices.ts` — attendance API transport
- `CMS-React/src/app/modules/attendance/AttendanceModels.ts` — attendance types
- `CMS-React/src/app/modules/attendance/components/AttendanceImportModal.tsx` — import UI
- `CMS-React/src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` — exception review UI
- `CMS-React/src/app/modules/users/components/LeaveCalendar.tsx` — profile attendance calendar
- `CMS-React/src/app/modules/attendance/AttendanceStatusSettings.tsx` — status configuration UI
- `CMS-React/src/app/modules/attendance/WorkFromHome.tsx` — WFH policy and request UI

### API routes
- `POST api/admin/attendance` — manual attendance create/update
- `POST api/admin/attendance/GetAllAttendanceItems` — company attendance grid query
- `POST api/admin/attendance/bulk` — bulk marking
- `POST api/admin/attendance/bulk-import/validate` — import preview validation
- `POST api/admin/attendance/bulk-import/commit` — import commit
- `GET api/attendance/me/grid` — employee own monthly grid
- `GET api/attendance/me/calendar` — employee own calendar
- `POST api/attendance/me/correction-requests` — employee correction request
- `GET api/attendance-status-settings?activeOnly=true` — status definitions
- `GET api/attendance/weekly-offs` — weekly off configuration
- `POST api/attendance/penalty/exceptions/search` — load monthly exceptions
- `PATCH api/attendance/penalty/exceptions/{id}/review` — review exception
- `POST api/attendance/penalty/exceptions/{id}/resolve-attendance` — resolve exception
- `GET/PUT api/wfh/policy` — WFH policy management
- `POST api/wfh/requests` — create WFH request


## 12. Operational checklist

Before making attendance-related changes, verify:
- the tenant has correct `AttendanceStatusSetting` definitions
- weekly off and holiday configuration is accurate
- payroll month lock state is understood
- WFH policy and leave policy mapping are consistent with attendance codes
- the import file uses the exact header names and valid employee codes
- the UI is loading status settings from `activeOnly=true`
- `CompanyId` and `UserId` are never supplied by the browser for ownership decisions


## 13. Recommended change points

If you extend the attendance module, consider:
- centralizing source ownership rules across manual edits, bulk imports, leave reconciliation, and WFH updates
- introducing an explicit attendance row provenance UI field or tooltip in the grid
- adding test coverage for import idempotency and transaction safety
- making the day cell editor display the currently active company attendance rules and required time hints
- surfacing payroll lock state and exception reasons inline in the attendance grid


## 14. Summary

The Attendance module is a connected domain: core attendance rows are created by manual actions, leave approval, WFH approval, and system reminders; they are protected by tenant scope, monthly locks, and source ownership; and they feed the monthly validation and payroll process.

The `CMS-React` UI provides two primary surfaces:
- the company attendance grid and bulk/import helpers for HR/Admin
- the read-only self-attendance view and correction request flow for employees

This document is the reference for implementation, debugging, and cross-module connectivity for the Attendance module.
