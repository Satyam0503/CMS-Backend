# Bulk Attendance Marking and Import

## Scope

This document describes the implemented HR/Admin bulk attendance routes. It does not describe employee self-service, leave approval, WFH approval, or payroll processing.

## Manual bulk marking

`POST /api/admin/attendance/bulk` accepts `AttendanceBulkMutationDto` with up to 500 `AttendanceBulkRowDto` values and `SkipExisting`.

Manual clients send `UserId`. The additive `EmployeeCode` property is also supported for import clients; at least one identifier is required. `AttendanceBulkMutationService` normalizes the business date, rejects duplicate identity/date rows, and calls `AttendanceMutationValidator` for every row before it writes a prepared attendance record.

`SkipExisting` maps to the existing create-missing behavior. Otherwise editable manual records can be updated. Leave (`LEAVE`) and approved WFH (`WFH_REQUEST`) source-owned rows are always protected and are counted as `protectedSkipped`.

## Office-start defaults and HR review

`AttendanceReminderHostedService` runs every minute in India Standard Time. For every active employee still missing today's attendance, it resolves the effective schedule in this order: employee assignment, department assignment, then company default. Once that employee's scheduled `StartTime` is reached, the shared validator creates a `P` row with `SourceType = SYSTEM_OFFICE_START_PRESENT` and the effective schedule's clock times. It uses create-missing-only behavior, so it never replaces a manual row, Leave row, WFH row, or another system-created row.

At **10:52 AM IST**, `AttendanceReminderProcessor` creates one deduplicated in-app review notification for each company that has office-start default rows. It is delivered to active Administrator, HR, and HR Executive users. The notification tells the reviewer to overwrite attendance for exceptions such as Absent, Sick Leave, or Earned Leave. There is no 6 PM auto-present operation or 6 PM review notification.

The React **Bulk Attendance Marking** page begins with all active employees selected and makes ordinary rows easy to overwrite. Leave the “Keep existing records” option unchecked to apply the selected exception status. For statuses that need times, the API uses each selected employee's effective office schedule rather than fixed browser-provided `09:00`/`18:00` values.

## Excel import

The server-side validation route is `POST /api/admin/attendance/bulk-import/validate` with multipart form field `file`. It accepts only `.xlsx`, at most 5 MB, and checks the ZIP signature before loading the workbook in memory with ClosedXML. It does not store the upload.

The workbook must contain a worksheet named `Attendance Import`. Its first row must contain unique headers:

| Required | Optional |
|---|---|
| Employee Code | Check In Time |
| Attendance Date | Check Out Time |
| Status Code | Remarks |

Formula cells are rejected. An empty worksheet is rejected. The server processes at most 500 data rows. The current browser sample uses the same six column names; there is no server template-download endpoint yet.

`Employee Code` is `EmpUser.EmployeeId`. `AttendanceBulkEmployeeResolver` searches only the authenticated request's `CompanyId`, rejects missing, inactive, and ambiguous matches, and returns the internal `UserId` only to server code. The existing authorization model is the Attendance module permission check plus company scope; no separate team/department employee-scope service is implemented.

## Validate-only preview

Validation calls `AttendanceMutationValidator` with the resolved user and does not call `AttendanceBulkMutationService`. Therefore it creates no attendance record, `AttendanceAuditEvent`, or `AttendanceNotificationOutbox` record.

Each `AttendanceBulkImportRowPreviewDto` contains source row, employee code/name, date, requested status, outcome, reason/message, and `CanInclude`. Implemented outcomes are `READY_CREATE`, `READY_UPDATE`, `UNCHANGED`, `PROTECTED`, and `INVALID`.

Duplicates are detected by case-insensitive `Employee Code + Attendance Date`; every matching row is returned as `INVALID` with `ATTENDANCE_IMPORT_DUPLICATE_ROW`, rather than failing the whole preview.

The summary provides `total`, `valid`, `willCreate`, `willUpdate`, `unchanged`, `protected`, and `invalid`.

## Commit

`POST /api/admin/attendance/bulk-import/commit` accepts `AttendanceBulkImportCommitRequestDto`: `IdempotencyKey`, `SkipExisting`, and rows. It requires the same `Attendance.CreateForEmployee` and `Attendance.Override` permissions as manual bulk marking. It delegates to `AttendanceBulkMutationService`, which re-resolves supplied employee codes and re-runs the shared validator immediately before writing.

The response preserves `created`, `updated`, `skipped`, and `protectedSkipped`. `AttendanceImportOperation` stores a completed result per company, actor, and idempotency key; a repeated completed key returns that stored result. There is currently no migration-created unique index for this record, so concurrent duplicate submissions are not yet race-safe.

## Date, timing, and protection

Attendance is normalized to UTC midnight by the existing bulk/validator path. The validator rejects future, pre-joining, post-exit, holiday, weekly-off, and locked-period updates. It resolves active `AttendanceStatusSetting` entries and effective schedules. Required-time statuses use automatic schedule timing when no times are supplied; supplied times are parsed by the validator. The repository continues using company/user day-range lookups.

## Persistence and duplicate safety

Committed writes replace/upsert the prepared attendance row by the logical `CompanyId + UserId + Date` identity. The database must retain its unique index for that identity to provide the race-level duplicate safeguard. The current bulk mutation service does not create a separate audit/outbox row; do not claim an audit/outbox transaction until that behavior is implemented and verified.

## Error codes

Common import errors include `ATTENDANCE_IMPORT_FILE_REQUIRED`, `ATTENDANCE_IMPORT_XLSX_REQUIRED`, `ATTENDANCE_IMPORT_INVALID_FILE_SIGNATURE`, `ATTENDANCE_IMPORT_INVALID_WORKBOOK`, `ATTENDANCE_IMPORT_REQUIRED_SHEET_MISSING`, `ATTENDANCE_IMPORT_REQUIRED_HEADERS_INVALID`, `ATTENDANCE_IMPORT_EMPTY_WORKBOOK`, `ATTENDANCE_IMPORT_FORMULA_NOT_ALLOWED`, `ATTENDANCE_IMPORT_INVALID_DATE`, `ATTENDANCE_IMPORT_DUPLICATE_ROW`, `ATTENDANCE_IMPORT_EMPLOYEE_CODE_REQUIRED`, `ATTENDANCE_IMPORT_EMPLOYEE_NOT_FOUND`, `ATTENDANCE_IMPORT_EMPLOYEE_INACTIVE`, `ATTENDANCE_IMPORT_EMPLOYEE_CODE_AMBIGUOUS`, and `ATTENDANCE_IMPORT_IDEMPOTENCY_KEY_REQUIRED`.

## Testing and debugging

The existing database-free service test project currently passes 83 tests. Focused import validation/commit integration tests have not yet been added; therefore no runtime MongoDB transaction/idempotency claim should be inferred from that suite.

When diagnosing an import, verify the route permission, `.xlsx` signature/size, sheet name and exact headers, employee code uniqueness within the company, date format, active status configuration, schedule, locked month, and leave/WFH ownership. For a commit failure, verify replica-set transaction support and inspect `AttendanceAuditEvent` and `AttendanceNotificationOutbox` only after a successful result.

## File references

- `Codeji.CMS.API/Controllers/AttendanceController.cs`
- `Codeji.CMS.DTO/Attendance/AttendanceBulkMutationDto.cs`
- `Codeji.CMS.DTO/Attendance/AttendanceBulkImportValidationDto.cs`
- `Codeji.CMS.Services/Attendance/AttendanceBulkEmployeeResolver.cs`
- `Codeji.CMS.Services/Attendance/AttendanceBulkImportValidationService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceBulkImportCommitService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceBulkMutationService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs`
- `Codeji.CMS.Repository/Entities/Attendance/AttendanceImportOperation.cs`
- `CMS-React/src/app/modules/attendance/components/AttendanceImportModal.tsx`
