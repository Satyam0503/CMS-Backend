# Work From Home (WFH) module — detailed implementation guide

> Current implementation reference for `WorkFromHomeController`, `WorkFromHomeService`, WFH DTOs/entities, attendance integration and the React Leave Management **Work from home** tab at `/leavemanagement/work-from-home`. Updated 29 July 2026.

## Purpose and boundaries

WFH is a governed alternative work location. It is not a user-editable attendance status. A request is validated against the tenant policy, employee eligibility, calendar and attendance state; when approved, it owns the attendance row for its date. The employee records time through the WFH screen and HR/Admin can review the resulting attendance.

The module supports two company policies:

| Policy setting | Result |
|---|---|
| `ManagerApprovalRequired = true` | Employee creates `Pending`; the assigned manager or authorized HR/Admin must decide it. |
| `ManagerApprovalRequired = false` | Eligible request becomes `Approved` immediately; attendance is created and HR/Admin are notified for review. |

The default weekly limit is one day per Monday–Sunday week. It is a configurable policy field but the intended company policy is **one WFH day per employee per week**.

## Code map

| Responsibility | Location |
|---|---|
| HTTP API | `Codeji.CMS.API/Controllers/WorkFromHomeController.cs` |
| Business rules | `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs` |
| Contracts | `Codeji.CMS.DTO/Attendance/WorkFromHomeDto.cs` |
| Documents | `Codeji.CMS.Repository/Entities/Attendance/WorkFromHomeRequest.cs` |
| Attendance protection | `Codeji.CMS.Services/Attendance/AttendanceService.cs` |
| React page | `../CMS-React/src/app/modules/attendance/WorkFromHome.tsx` |
| Migrations | `Codeji.CMS.Migrations/Migrations/AddWorkFromHomeWorkflow.cs`, related WFH seed/permission migrations |

## Persistence model

### `WorkFromHomePolicy`

One tenant policy controls enabled state, weekly/monthly/consecutive limits, advance notice, half/mixed day support, approval requirement, calendar allowances, reason requirement, eligible population, clock-in window, minimum hours and attendance status codes.

The current additive policy fields also support explicit employee exclusions, eligible employment types, an effective date range, company time-zone ID, office display hours and independent check-in/check-out windows. Empty new fields preserve the pre-existing company-wide/default behavior.

Eligibility is calculated as follows:

```text
Explicit employee exclusion → deny
Explicit employee inclusion → grant
Eligible department or employment type → grant
ApplyToAllEmployees → grant
Otherwise → deny
```

An employee never receives the policy-management UI. The employee view only presents their remaining allowance and request/timing history. HR/Admin owns policy configuration.

### `WorkFromHomeRequest`

Stores the employee/user identity, date range, duration, reason, status, approver/reviewer information, cancellation/revocation metadata and `Version`. Statuses used by the workflow are `Draft`, `Pending`, `Approved`, `Rejected`, `Returned` and `Cancelled` where applicable.

`Version` is required for mutable actions. Approval/cancellation updates include the expected version, preventing two users from making conflicting decisions silently.

### `WorkFromHomeRequestLog`

Append-only audit events retain previous/new status, action, actor, time, review remarks and version. The log explains why a request state changed even if the request later changes again.

### Attendance row ownership

An approved date becomes an `AttendanceModel` row with:

```text
SourceType    = WFH_REQUEST
SourceId      = WorkFromHomeRequest.RequestId
SourceVersion = request.Version
RemarkCode    = WFH_APPROVED
```

This provenance is important: ordinary attendance administration cannot overwrite `WFH_REQUEST` rows. Corrections must go through an appropriate WFH reversal/correction workflow so payroll and audit history remain defensible.

## API contract

All routes require authentication. Controller-level policy/team/all and decision endpoints carry WFH module permissions; employee self-service routes are protected by authenticated ownership checks in the service.

| Method | Route | Audience | Function |
|---|---|---|---|
| GET | `/api/wfh/policy` | authenticated | Read effective policy/employee eligibility context. |
| GET | `/api/wfh/context` | employee | Server-calculated feature/eligibility/quota/next-date and safe effective schedule information. |
| PUT or POST | `/api/wfh/policy` | policy editor | Save policy. POST is retained for the React form helper. |
| POST | `/api/wfh/requests` | employee | Create own request. |
| GET | `/api/wfh/requests/my` | employee | Own history only. |
| GET | `/api/wfh/requests/team` | manager | Team requests. |
| GET | `/api/wfh/requests/all` | HR/Admin | Company requests. |
| POST | `/api/wfh/requests/{id}/approve|reject|return` | manager/HR/Admin | Versioned decision. |
| POST | `/api/wfh/requests/{id}/cancel` | owner | Cancel eligible request. |
| POST | `/api/wfh/requests/{id}/check-in` | owner | Server-time check-in. |
| POST | `/api/wfh/requests/{id}/check-out` | owner | Server-time check-out. |
| GET | `/api/wfh/requests/{id}/timing` | owner | Today’s approved timing and required hours. |

The controller also exposes attendance remark-option routes; those remain protected by Attendance permissions and are not employee WFH policy controls.

## End-to-end logic

### 1. Employee request creation

```text
Employee submits date(s), duration and reason
  → resolve current CompanyId/UserId and active employee
  → load enabled tenant policy
  → check employee eligibility
  → validate range, duration, reason and advance notice
  → reject backdating, overlap, weekly off, holiday or locked month as policy requires
  → count existing pending/approved dates in each Monday–Sunday week
  → reject if quota would exceed MaxDaysPerWeek
  → Pending + assigned approver, or Approved immediately
  → if Approved: reconcile source-owned attendance
  → write audit log and queue notifications
```

Important validation responses include `WFH_INVALID_DATE_RANGE`, `WFH_ADVANCE_NOTICE_REQUIRED`, `WFH_OVERLAPPING_REQUEST`, `WFH_WEEKLY_QUOTA_EXCEEDED`, `WFH_WEEKLY_OFF_NOT_ALLOWED`, `WFH_HOLIDAY_NOT_ALLOWED` and `WFH_DATE_LOCKED`.

### 2. Approval path

For approval-enabled companies, only the assigned approver or a company reviewer with WFH view-all authority can decide. A requester cannot approve their own request. Approval rechecks the active policy, active employee, quota and attendance reconciliation before persisting the new status.

Rejected and returned requests do not create WFH attendance. An approved employee receives a durable notification, realtime SignalR push and, when the employee has a verified email, an email.

### 3. Direct-WFH path

For a no-approval company, a valid request is approved immediately. HR/Admin does not need to approve it, but is notified so the day can be reviewed. This gives employees self-service without permitting uncontrolled attendance edits.

### 4. Attendance reconciliation

For every approved covered day the service:

1. resolves the correct configured WFH status (`WFH`, `WFH-HD` or `WFH+WFO`);
2. confirms the status is active;
3. rechecks weekly quota;
4. rejects a locked payroll/attendance month;
5. detects an existing non-WFH attendance row;
6. creates a blocking payroll exception instead of overwriting leave/manual attendance;
7. creates or updates the WFH-owned attendance row only when safe.

Pending, approved and returned requests reserve the weekly allowance. Rejected and cancelled requests do not. A returned request must be cancelled or resubmitted before the employee can schedule another date in that week.

### 5. Clock-in and clock-out

Only the request owner with an approved request covering today can clock. Server UTC time is used, not a client-supplied timestamp. Check-in validates the configured allowed time window. Check-out requires a prior check-in and calculates `TotalHours`.

The calendar work date and clock windows are evaluated using the policy company time-zone ID (falling back to UTC if its configured ID is invalid). Check-out may also be limited by its own configured start/end window.

If total hours are below the duration minimum (`FullDayMinimumHours` or `HalfDayMinimumHours`), the service creates a pending blocking `WFH_INSUFFICIENT_HOURS` payroll exception. This records a review requirement rather than silently changing a completed attendance day.

The UI intentionally presents one action: **Clock in** until a check-in exists, then **Clock out**. It also displays office hours, required hours and a live current-working-hours timer. After check-out, the record is not available for ordinary attendance overwrite.

## Notifications and email

Notifications are queued after request creation/approval so the request transaction is not blocked by a mail provider. The queued delegate captures company and actor values before execution because there is no HTTP context in a background worker.

| Event | In-app notification | Email |
|---|---|---|
| Pending request | HR/Admin recipients; persisted + SignalR | verified HR/Admin recipients, employee/ID/department/role/dates/reason category |
| Direct approved WFH | HR/Admin recipients; persisted + SignalR | verified HR/Admin recipients, same details |
| Manager/HR approval | employee; persisted + SignalR | verified employee |

In-app notifications are stored in `Notifications` and `UserNotifications` before SignalR is invoked; users who are offline still see notification history after reconnecting. Broad WFH notifications and emails use the reason category, not the employee's free-text explanation. Email delivery itself depends on valid provider settings and recipient verification; `EmpEmailLogs` is the audit point for queued/sent mail.

## UI and visibility rules

WFH is presented as a Leave Management workflow, not as a standalone sidebar module. The sidebar has no `Work from home` item and the former `/work-from-home` route is intentionally removed. Navigate to **Leave Management → Work from home** instead. This changes only the frontend location; the secured `api/wfh` contracts, WFH-specific policy permissions and source-owned attendance workflow remain in place.

Employee view:

- shows only their own request and approved history;
- calls `/api/wfh/context` for the authoritative one-per-week availability, next available Monday and effective clock-window summary; the UI disables unavailable submission but the backend recalculates the same rules;
- shows the request form with date/duration/reason validation;
- never displays policy administration;
- displays clock/timer information only for an approved request covering today.

HR/Admin view:

- can configure the policy, including optional approval and eligible departments/employee IDs;
- sees all company requests (manager view is limited to team);
- can approve/reject/return where approval is required;
- receives notifications even in direct-WFH mode and can review attendance/exceptions.

## Security and data-integrity invariants

1. Request ownership is always enforced by current user and tenant.
2. Policy changes require `WorkFromHome:PolicyEdit` permission.
3. Team/all data routes require their corresponding WFH permissions.
4. Request decisions are optimistic-version protected.
5. Attendance conflict is blocking; never overwrite an existing leave/manual source row.
6. Attendance month lock prevents WFH changes for locked dates.
7. Manual attendance edit rejects `WFH_REQUEST` source rows.
8. Notification recipient lookup is tenant-scoped and targeted to active HR/Admin roles.

## Test and operational checklist

1. Restart the API after deploying WFH code/migrations.
2. Configure an enabled company WFH policy with one weekly day.
3. Test eligible and ineligible employees; test department and explicit employee targeting.
4. Test direct WFH and approval-required WFH separately.
5. Test duplicate/overlapping request, advance notice, holiday, weekly off, locked month and quota failures.
6. Verify notification bell history, SignalR live update and `EmpEmailLogs`.
7. Verify check-in, check-out, live hours and insufficient-hours exception.
8. Verify HR review visibility and that an admin attendance edit cannot overwrite the WFH row.

## Known operational dependency

The API process must have the notification hub, priority worker and mail configuration enabled for live delivery. The durable notification records and email logs can be inspected independently when an external SMTP/provider delivery issue is suspected.
