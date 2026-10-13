# Attendance, Leave Management, and Profile Tracker: Complete Reference

> **Snapshot:** 2026-07-31. This is the authoritative implementation reference for the Attendance module, Leave Management module, and employee Profile Tracker in `CMS-Backend-Core` and sibling `CMS-React`. The code is authoritative if it differs from this document.

## 1. Scope and design boundary

The three surfaces work together but are not the same permission domain:

```text
Employee Profile
  ├─ Attendance calendar: read-only personal monthly view
  └─ Leave Tracker: own balance, own requests, own request history

Attendance module
  ├─ Employee mode: own monthly grid, read-only, correction request
  └─ HR/Admin mode: company grid, marking, exception and monthly controls

Leave Management module (HR/Admin)
  ├─ policies and allocation
  ├─ company request review and decisions
  └─ balances, summaries and leave-to-attendance reconciliation
```

An employee can use the Profile Leave Tracker without being allowed into the Leave Management module. An employee who can view Attendance can see only their own grid; that does not grant employee-directory, company-grid, edit, policy, exception, or payroll access.

## 2. Shared identity, tenant, and persistence rules

Every authenticated request obtains `UserId` and `CompanyId` from JWT/current context. The browser must never be trusted for tenant identity or ownership.

| Concept | Record | Logical ownership/key |
|---|---|---|
| Employee identity | `EmpUser` | `CompanyId + UserId`; `EmployeeId` is business code |
| Attendance | `AttendanceModel` / Mongo collection `Attendance` | `CompanyId + UserId + Date` |
| Leave policy | `LeavePolicy` | `CompanyId + normalized name/code` |
| Leave balance | `EmployeeLeaveBalance` | `CompanyId + UserId + LeavePolicyId` |
| Leave request | `LeaveRequest` | `CompanyId + LeaveRequestId` |
| Attendance correction | `AttendanceCorrectionRequest` | `CompanyId + UserId + AttendanceDate` while pending |
| Notifications | `Notifications` + `UserNotifications` | Notification has company; recipient mapping has user ID |
| Email audit | `EmpEmailLogs` | Company and sender/recipient are recorded |

All policy, employee, balance, request, attendance, and recipient lookups must include `CompanyId`. The service derives a self-service user ID from the caller; it never accepts another employee as an authority.

## 3. Role and permission matrix

Role names alone are not authorization. The assigned module permission is evaluated by `ModulePermissionAttribute` and the relevant service still applies ownership/company filters.

| Persona | Attendance | Leave Tracker in own profile | Leave Management |
|---|---|---|---|
| Employee with `Attendance.View` | `GET api/attendance/me/grid`; read-only monthly grid; correction requests | View own assigned balances, apply, view/edit/withdraw own pending request | No policy, company list, decisions, or other employee data |
| Employee without `Attendance.View` | No self grid or correction route | Unchanged: own leave tracker remains available | No management access |
| HR/Admin with `Attendance.ViewAll` | Company employee grid, subject to further Create/Edit checks | Own tracker; selected employee tracker requires Leave Management View | Can view company leave information according to assigned actions |
| HR/Admin with Attendance Create/Edit | Mark/edit attendance, review exceptions, validate/lock as permitted | Same as above | Separate permission set; attendance rights do not imply leave-policy rights |
| HR/Admin with Leave Management View/Create/Edit | No automatic attendance edit grant | May use their own tracker | Policies, employee balances, lists and request decisions as action permits |

`AddAttendanceViewAllPermission` seeds `Attendance.ViewAll` and grants it to Admin/HR roles. `Attendance.View` alone is deliberately insufficient for `POST api/admin/attendance/GetAllAttendanceItems`.

## 4. Backend implementation map

| Area | Main backend files | Responsibility |
|---|---|---|
| Company attendance operations | `Codeji.CMS.API/Controllers/AttendanceController.cs` | Protected company grid route and attendance write/read endpoints |
| Employee attendance self-service | `Codeji.CMS.API/Controllers/MyAttendanceController.cs` | Own grid and attendance-correction request endpoints |
| Leave HTTP contract | `Codeji.CMS.API/Controllers/LeaveManagementController.cs` | HR/Admin routes plus token-derived `me/*` profile routes |
| Leave lifecycle | `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs` | Policy, balance, request, decision, notification, email and standalone-Mongo fallback |
| Attendance lifecycle | `Codeji.CMS.Services/Attendance/AttendanceService.cs` | Daily validations, normalization and persistence |
| Leave attendance ownership | `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs` | Creates/reverses source-linked attendance after decisions |
| Work calendar | `ICompanyWorkingCalendarService` | Weekly offs, holidays and leave date inclusion calculation |
| Monthly controls | `AttendancePenaltyService.cs` | exceptions, validation, summary and lock state |
| Correction persistence | `Entities/Attendance/AttendanceCorrectionRequest.cs` | Employee-to-HR audit record |

## 5. Frontend implementation map

| User surface | Main React files | Behavior |
|---|---|---|
| Attendance page | `src/app/modules/attendance/EmployeeAttendance.tsx` | Selects manager vs employee mode; renders correction form only for employee mode |
| Attendance matrix | `src/app/modules/attendance/component/AttendanceCalendar.tsx` | `readOnly` determines company grid versus own grid, editing, selection and exceptions visibility |
| Profile attendance calendar | `src/app/modules/users/components/LeaveCalendar.tsx` | Read-only monthly status/times/legend in profile |
| Leave tracker | `src/app/modules/users/leavetracker/LeaveTracker.tsx` | Own balance endpoint for self; protected employee endpoint for selected profile |
| Apply/edit form | `components/LeaveApplication.tsx` | Uses `me/requests` in self profile; management routes for authorised selected employee flow |
| Leave history | `components/MyLeaveRequest.tsx` | Uses `me/requests/search` and own withdraw route in self profile |
| Transport | `src/app/modules/leaves/leaveServices.ts` | Typed calls for management and profile self-service routes |
| Profile route/tab gate | `profileTabs.ts`, `routing/ProfileRoutes.tsx` | Self Leave Tracker always available; selected employee tracker requires Leave Management View |

## 6. Attendance: employee self-service flow

### 6.1 Read own month

1. Employee opens Attendance.
2. `EmployeeAttendance.tsx` checks whether the user has Attendance Create or Edit. If not, it creates one visual row named **My attendance** and does not load the employee list.
3. `AttendanceCalendar.tsx` receives `readOnly=true` and calls `GET api/attendance/me/grid?year={year}&month={month}`.
4. `MyAttendanceController` derives company/user, verifies the active employee in that company, queries only their date range, and returns `items`.
5. The calendar renders codes/times for persisted rows. It does not infer absent status for missing rows.

In employee mode, search, employee selector, selection checkboxes, bulk mark, day click editor, keyboard edit action, save controls, and Monthly Exceptions are hidden or disabled. A direct request to the company grid is rejected because it requires `Attendance.ViewAll`.

### 6.2 Report incorrect attendance

1. Employee selects an attendance date, issue type, and a reason (maximum 1000 characters).
2. React posts `{ attendanceDate, reason }` to `POST api/attendance/me/correction-requests`.
3. Backend validates model, authenticated company/user, active employee, and a real attendance record for that date.
4. Backend rejects a second Pending request for the same employee/date.
5. It saves `AttendanceCorrectionRequest` with `Pending` status.
6. It finds active same-company Administrator/HR role holders, creates a `Notifications` record and one `UserNotifications` entry per recipient, then attempts a SignalR push.

The request is an audit/work queue, not a mutation. It does not change attendance or override a lock, holiday/weekly-off rule, source owner, leave decision, or WFH workflow. HR/Admin must correct through the applicable authorised workflow.

### 6.3 HR/Admin attendance flow

1. The operator opens Attendance with `Attendance.ViewAll`.
2. The frontend loads eligible employees and calls `POST api/admin/attendance/GetAllAttendanceItems` for current-company employee IDs/date range.
3. Create/Edit permissions determine whether marking and editing actions are enabled.
4. `AttendanceService` resolves the employee within company, validates status and required times, uses `AttendanceEditGuard`, calculates hours, and upserts by company/user/date.
5. Monthly exception, validation and lock actions are HR/Admin-only controls.

Manual editing cannot overwrite source-owned leave/WFH attendance or a locked month. UI checks improve usability; server checks are the security boundary.

## 7. Profile attendance calendar

### Business-date invariant

`LeaveRequest.StartDate`, `LeaveRequest.EndDate`, and `Attendance.Date` for a leave-owned row represent one calendar business day and are persisted at UTC midnight. Reconciliation derives both values from `DateOnly`, never from a timezone-converted instant. `NormalizeLegacyLeaveBusinessDates` repairs the former India-local-midnight (`18:30 UTC`) representation and safely aligns single-day source rows. A target-day collision is left for review, not replaced.

The profile calendar is read-only and is not the Attendance page editor.

- In the signed-in user's profile it calls `GET api/attendance/me/calendar?year={year}&month={month}`. The API derives company/user from JWT. The older `api/admin/attendance/my-calendar` endpoint remains only as a compatibility wrapper.
- In an authorised HR/Admin selected-employee profile it uses the protected range query for that one employee.
- It loads tenant-specific status settings, colours the day by `ColorHex`, displays code/status/times in a tooltip, and renders a month legend.
- A row with `SourceType=LEAVE` displays the persisted mapped leave status. A row with `SourceType=WFH_REQUEST` displays its WFH status/times. Neither can be altered from the profile calendar.

## 8. Leave Management: HR/Admin policy and balance flow

### 8.1 Policy lifecycle

HR/Admin uses Leave Management routes, protected by Leave Management Create/View/Edit actions, to create/list/update policies. A policy contains name/code, active state, paid setting, accrual configuration, max balance, carry over, notice days, half-day permission, inclusion rules, assigned employees, and optional `AttendanceStatusCode`.

- Name/code uniqueness is tenant-scoped.
- Employee assignments are validated in the same company.
- An empty `ApplicableTo` assigns active employees in that company; `null` assigns none.
- Attendance mapping can be absent at setup, but an approval requires a valid active, leave-eligible, no-time status mapping in the same company.
- `GetEmployeeLeaveBalance` and profile balance queries filter employees, balances and policies by current `CompanyId`.

Balance is checked at application time; it is deducted only when a request is accepted. Monthly/yearly accrual runs as a background operation and must be tested only in an isolated database because it mutates balances.

### 8.2 Management request and decision routes

| Action | Route | Permission/owner |
|---|---|---|
| Create policy | `POST api/LeaveManagement/CreateLeavePolicy` | Leave Management Create |
| Update policy | `POST api/LeaveManagement/UpdateLeavePolicy` | Leave Management Edit |
| List policies | `POST api/LeaveManagement/GetAllLeavePolicies` | Leave Management View |
| Read employee balance | `GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}` | Leave Management View |
| Create for employee | `POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}` | Leave Management Edit |
| List company requests | `POST api/LeaveManagement/GetLeaveRequests` | Leave Management View |
| Approve/reject | `PATCH api/LeaveManagement/LeaveRequest/{id}/Status` | Leave Management Edit |

## 9. Profile Leave Tracker: employee self-service flow

The Profile Leave Tracker is deliberately independent of the Leave Management module permission.

| User action | Route | Server ownership enforcement |
|---|---|---|
| Read own balances/policies | `GET api/LeaveManagement/me/balances` | Current token user/company |
| Apply | `POST api/LeaveManagement/me/requests` | Replaces submitted `UserId` with token user |
| Read history | `POST api/LeaveManagement/me/requests/search` | Replaces filter employee ID with token user |
| Edit own pending request | `PATCH api/LeaveManagement/me/requests` | Current user and pending request check |
| Withdraw own pending request | `DELETE api/LeaveManagement/me/requests/{id}` | Current user/company request filter |

The tracker exposes only the employee's assigned policy/balance records, their own requests and their own decision outcome. It never returns the policy catalogue, other employees' balances, or company-wide requests. When HR/Admin opens another employee profile, that tracker remains separately gated by Leave Management View.

### Application validation

`LeaveManagementService.CreateLeaveRequest` and update logic validate: current-company active employee, active assigned policy/balance, start/end ordering, date overlap with Pending/Accepted requests, minimum notice, half-day rule, non-zero included duration, and available balance. `ICompanyWorkingCalendarService` is used for configured weekly offs, holidays, and policy inclusion flags; frontend weekend calculations are only user feedback and not authoritative.

## 10. Decision, balance, and attendance state transition

```text
Pending --employee withdraw--> Withdrawn
Pending --reviewer reject----> Rejected
Pending --reviewer accept----> Accepted
Rejected --reviewer accept---> Accepted
Accepted --reviewer reject---> Rejected
```

On Accept, the service conditionally debits balance and conditionally updates request status/version. On Accepted-to-Rejected, it conditionally restores balance. A replica-set Mongo deployment uses a transaction. A standalone Mongo deployment cannot transact; the service detects the unsupported transaction exception and uses compare-and-set with balance compensation if the request update loses its race.

After a successful decision:

1. Accept calls `LeaveAttendanceReconciliationService.ReconcileAcceptedLeaveAsync`.
2. Reject calls `ReverseLeaveAttendanceAsync`.
3. Only after successful reconciliation/reversal does the service send the status notification.

## 11. Leave-to-attendance reconciliation

For each included leave day, reconciliation creates/updates a daily record using the policy's attendance code:

```text
Attendance.SourceType    = LEAVE
Attendance.SourceId      = LeaveRequestId
Attendance.SourceVersion = LeaveRequest.Version
Attendance.Remarks       = generated-source explanation
```

The operation is idempotent for the same leave source. It never silently overwrites manual or WFH attendance. Instead, it creates a blocking `LEAVE_ATTENDANCE_CONFLICT` exception. Reversing a decision removes/reverses only records owned by the matching leave source/version. Affected unlocked monthly summaries are invalidated for recalculation.

## 12. Notifications and email

### Leave applied

When a pending request is created, recipients are active same-company users whose role is Administrator or HR, excluding the requester. The service persists a shared `Notifications` record and recipient-specific `UserNotifications` records, attempts SignalR delivery, and emails recipients with a verified non-empty email address.

### Leave approved/rejected

The requester is the recipient when their `LeaveStatusUpdate` notification preference is enabled. The same persistence, SignalR, and verified-email rules apply. The email is recorded through `IMiddlewareService.EmailSendAndSave` in `EmpEmailLogs`.

### Delivery resilience and troubleshooting

- Inbox rows are persisted before SignalR delivery; a disconnected real-time client still sees the notification after reload.
- A SignalR failure is isolated so it cannot prevent email delivery.
- Email requires SMTP configuration plus recipient `IsEmailVerified=true` and a non-empty email.
- Notification delivery failures do not roll back an already saved leave request/decision.
- If no HR/Admin recipient exists in the same company with a matching role assignment, there is no recipient to notify; correct the user RoleId/role configuration rather than widening tenant selection.

## 13. Monthly controls and payroll dependency

Daily attendance, leave-generated rows, exceptions, and configured paid/unpaid fractions feed `MonthlyAttendanceSummary`. HR/Admin recalculates/reviews exceptions, validates and locks eligible months. Payroll should use approved/locked monthly results, not raw frontend calendar presentation.

Once locked, normal edits are blocked. The application does not provide a complete audited reopen/relock and payroll-revision process; do not change locked data directly in MongoDB.

## 14. Operational verification checklist

### Employee self-service

- Log in as an employee with only Attendance View: confirm one read-only grid row, no employee list/modal/bulk controls/exceptions.
- Confirm a changed query/body cannot retrieve another employee through `api/attendance/me/*` or `api/LeaveManagement/me/*`.
- Submit one correction request, verify duplicate pending request rejection, and check HR/Admin notification inbox.
- In My Profile, confirm Leave Tracker shows own balances/history and can apply/edit/withdraw pending requests without Leave Management permission.

### HR/Admin

- Confirm policy name/code collision is rejected within the same company and accepted in a different company.
- Confirm an unmapped/time-required/inactive attendance status blocks leave approval.
- Apply as employee, confirm same-company HR/Admin inbox/email log; approve/reject, confirm employee inbox/email log.
- Confirm manual/WFH row conflict creates an exception rather than a silent overwrite.
- Confirm company grid is unavailable to a user with only Attendance View.

### Runtime/production

- Use a MongoDB replica set for atomic leave status/balance decisions; standalone fallback is concurrency-protected but cannot provide full transaction atomicity.
- Check `EmpEmailLogs` for mail outcome and SMTP failures.
- Verify role assignments and notification preference values in the authenticated company.
- Use an isolated test database for accrual, payroll, or multi-write integration tests; do not test destructive flows against production data.

## 15. Known limits and future hardening

- Leave decision, reconciliation, summary invalidation, notification persistence, and email are multiple operations; a durable outbox/saga would improve replay and observability.
- Standalone Mongo fallback compensates balance when optimistic update loses its race but is not equivalent to replica-set transactions.
- Correction requests are submitted and notified; a dedicated HR correction-review/status UI and audited resolution workflow remain the next implementation step.
- Email outcome depends on mail infrastructure and verified recipient addresses; the persisted notification inbox is the primary in-product audit trail.

## 16. Related documents

- [Attendance module current flow](attendance-module-current-flow-and-audit.md)
- [Leave management current flow](leave-management-current-flow.md)
- [Employee profile current flow](employee-profile-current-flow.md)
- [Leave, attendance, payroll end-to-end flow](leave-attendance-payroll-end-to-end-flow.md)
- [Work from home module](work-from-home-module.md)
