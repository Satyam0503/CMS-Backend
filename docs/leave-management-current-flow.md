# Leave Management: Policies, Balances, Requests, Decisions, Notifications, and Attendance

## Scope and source of truth

Leave Management owns company-specific leave policies, employee balances and leave-request lifecycle. It does not directly make payroll calculations or permit the profile screen to alter attendance. Its approved-request reconciliation creates source-owned attendance records; attendance validation and payroll then consume those records.

From an implementation standpoint, leave management is intentionally split into two layers:

- a policy/request lifecycle layer that handles balances, approvals, notifications, and history; and
- an attendance reconciliation layer that converts an accepted leave decision into a daily attendance row owned by the leave workflow.

That split is important because it keeps leave policy decisions auditable while still ensuring payroll and attendance use a consistent, persisted daily record.

| Data | Ownership | Tenant key |
|---|---|---|
| `LeavePolicy` | HR policy configuration | `CompanyId` |
| `EmployeeLeaveBalance` | Per-user policy balance | `CompanyId + UserId + LeavePolicyId` |
| `LeaveRequest` | Employee request and review history | `CompanyId + LeaveRequestId` |
| `AttendanceModel` created from leave | Attendance module, source `LEAVE` | `CompanyId + UserId + Date` |
| `Notifications` / `EmpEmailLogs` | Delivery/audit records | Company captured at creation |

The `HardenHrTenantIndexes` migration provides company-aware unique indexes for policy normalized name/code and employee/policy balance. Policies with the same name or code are allowed in different companies; they are not allowed twice in the same company.

## API, permissions, and profile self-service

All routes are under `api/LeaveManagement` and require authentication. The **Leave Management module** is the HR/Admin workspace for policies, company-wide requests, balances, and decisions. The **Profile Leave Tracker** is a separate self-service workspace for the signed-in employee.

| Capability | Route | Required permission |
|---|---|---|
| Create policy | `POST CreateLeavePolicy` | Create |
| List policies | `POST GetAllLeavePolicies` | View |
| Update policy | `POST UpdateLeavePolicy` | Edit |
| Apply for an employee | `POST CreateEmployeeLeaveRequest/{employeeId}` | Edit |
| Review accept/reject | `PATCH LeaveRequest/{leaveRequestId}/Status` | Edit |
| Read balances/requests/summaries | Get/List routes | View |

### Profile Leave Tracker routes

These profile routes do not require Leave Management module permission. Every route derives the employee ID from the access token, so a browser cannot substitute another user's ID.

| Employee action | Route | Ownership rule |
|---|---|---|
| View assigned policies and own balances | `GET me/balances` | Current user and current company only |
| Apply for own leave | `POST me/requests` | Ignores client `UserId` |
| View own history | `POST me/requests/search` | Current user only |
| Edit own pending request | `PATCH me/requests` | Current user; pending request only |
| Withdraw own pending request | `DELETE me/requests/{leaveRequestId}` | Current user and current company only |

The profile tab is always available in **My Profile**. It shows only the policies/balances assigned to that employee and their own request history. It does not grant policy configuration, balance allocation, decision, company request listing, or another employee's data. HR/Admin continue to do those actions through Leave Management with module permissions.

The older generic self endpoints remain module-permission protected for compatibility with management screens; the Profile Leave Tracker calls the `me/*` endpoints.

## Policy lifecycle

A policy defines name, code, status, paid/unpaid setting, accrual period/amount, maximum balance, carry-over, minimum notice, half-day support, weekend/holiday inclusion, optional applicable employees and an optional `AttendanceStatusCode`.

### Implementation detail: policy mapping and validation

The policy is not only a balance rule. It also determines how approved leave will appear in attendance. The service validates the configured `AttendanceStatusCode` before it allows the policy to participate in the acceptance/reconciliation flow.

That validation enforces three important constraints:

1. the status must exist in the same company;
2. it must be active; and
3. it must be eligible for leave management and not require clock-in/out times.

If that mapping is missing or invalid, the leave request can still exist, but the system will not treat it as a safe attendance source. In practice, HR must complete the attendance-status configuration before relying on approved leave to produce payroll-visible attendance.

Creation normalizes name/code and checks duplicate name/code only within the current company. The policy document receives `CompanyId` from the server. If `ApplicableTo` is an empty array, active employees in that company receive initial balances; if it is `null`, no employee is assigned. Submitted employee IDs are accepted only after a same-company active-employee check.

Attendance mapping is optional during policy setup. This lets HR create a policy before Attendance Settings is complete. A mapped code is normalized, but approval is blocked until it resolves to an active, no-time, leave-eligible status in the same company. This avoids creating time-less leave attendance with a clock-in/out status.

## Request creation and edit

### Business-date contract (2026-07-31)

Leave is a calendar-day domain, not an instant-in-time domain. The API accepts a `YYYY-MM-DD` date, validates it as `DateOnly`, and persists `LeaveRequest.StartDate` and `EndDate` as **UTC midnight**. Reconciliation uses that same UTC-midnight value for `Attendance.Date`; it must never derive a leave day through local/UTC timestamp conversion.

`NormalizeLegacyLeaveBusinessDates` repairs the historical India-local-midnight encoding (`18:30 UTC` on the prior date) and realigns safe single-day `SourceType=LEAVE` rows. It skips an occupied target day for authorized conflict review.

```text
authenticated employee / delegated HR request
  -> company + employee membership check
  -> policy and balance lookup in same company
  -> pending/accepted overlap check
  -> notice, date and half-day validation
  -> CompanyWorkingCalendar date calculation
  -> pending LeaveRequest persistence
  -> HR/Admin in-app notification + verified-email delivery for an employee-submitted request
```

The service rejects end-before-start, inactive/missing employee or policy, no assigned balance, insufficient balance, overlap with another pending/accepted request, invalid half day, zero included days, and unmet minimum notice. `CompanyWorkingCalendarService` calculates included dates using the company weekly-off configuration, recurring/one-time holidays, and policy inclusion flags.

Balance is checked at submission but is deducted only on acceptance. Editing is limited to the employee's own pending request and recalculates duration and overlap while excluding that request.

### Profile leave self-service flow

```text
Employee opens My Profile > Leave Tracker
  -> GET me/balances returns only that employee's assigned current-company balances
  -> employee applies through POST me/requests
  -> service derives company/user, validates policy, balance, dates, overlap and notice
  -> a Pending request is stored
  -> same-company HR/Admin receive in-app notification and verified-email delivery is attempted
  -> HR/Admin reviews only through Leave Management
  -> the employee receives the decision in profile and email when preferences permit
```

## Decision state machine

```text
Pending --employee withdraw--> Withdrawn
Pending --reviewer reject----> Rejected
Pending --reviewer accept----> Accepted
Rejected --reviewer accept---> Accepted
Accepted --reviewer reject---> Rejected
```

Reviewer transitions to Pending/Withdrawn and no-op transitions are rejected. Acceptance/debit and accepted-to-rejected credit operations use conditional writes and an optimistic `Version` check. When MongoDB transactions are available, leave status and balance updates use a transaction; standalone MongoDB uses compare-and-set plus compensation if the status update loses its race.

### Operational meaning of the state machine

A leave request is not considered fully completed just because the reviewer accepted it. The service must also complete the attendance reconciliation step and update the employee balance safely. That is why the implementation keeps a clear distinction between the leave status and the downstream reconciliation outcome.

In practical terms:

- acceptance moves the request into an approved state and debits the balance;
- reconciliation then creates or updates attendance rows for the approved dates;
- if reconciliation fails, the request remains approved but the attendance side is marked as conflicted or retry-scheduled rather than silently treated as complete.

This is important for payroll and audit because a leave decision without successful attendance reconciliation is not yet a fully usable payroll input.

Before acceptance, the service confirms an active mapped attendance status. It then invokes leave-attendance reconciliation. Rejection reverses matching source-owned attendance. A reconciliation failure returns a failure result rather than falsely reporting a fully completed decision.

## Attendance and payroll connection

Approved leave is not merely visual:

1. `LeaveAttendanceReconciliationService` reloads request, employee, policy and status inside the company.
2. It calculates dates through the same working calendar and policy inclusion flags.
3. It creates/updates attendance records with `SourceType=LEAVE`, `SourceId=LeaveRequestId`, `SourceVersion=LeaveRequest.Version`, mapped status and a source remark.
   The stored `Attendance.Date` is the exact approved UTC business date.
4. It never overwrites a manual/WFH row. A conflict creates a blocking `LEAVE_ATTENDANCE_CONFLICT` exception.
5. It invalidates affected unlocked monthly summaries so Attendance/Payroll rebuild from the persisted daily source.

Manual attendance and profile-calendar UI must not be used to alter a leave-owned row. Corrections originate from the leave decision or an audited exception-resolution workflow.

### Why this matters for payroll

Payroll depends on the daily attendance record, not on the leave request UI itself. If a leave request is approved but attendance reconciliation fails or conflicts, the payroll month can remain incomplete or blocked. That is why the service marks reconciliation status and invalidates monthly summaries as part of the same approval path.

In short:

- leave policy decides whether leave exists;
- the approval decision decides whether balance changes;
- reconciliation decides whether attendance becomes payroll-visible;
- monthly summaries and payroll depend on that daily attendance state.

## Notifications and email

On an employee-submitted pending request, active HR/Administrator users belonging to the same company are recipients (excluding the requesting employee). When HR/Admin creates a pending request for another employee, no email is sent for that administrative entry; the persisted in-app notification/audit still applies. On acceptance/rejection, the requesting employee is the recipient when `LeaveStatusUpdate` preference is enabled.

For each delivery event the service creates a `Notifications` document with title, body, request target, type, actor and company; persists a `UserNotifications` row for each recipient; sends a SignalR `notification` event; and sends email to verified addresses through `IMiddlewareService.EmailSendAndSave`. Every email is wrapped by `Emailer.BuildProfessionalHtmlBody`, which supplies the shared branded header, content card and footer without changing the event-specific body. Email status/error is recorded in `EmpEmailLogs`. Delivery errors are contained so they do not roll back a leave request that was already persisted.

## Operations and verification

### Recommended manual test scenarios

1. Create a policy with a valid attendance mapping and approve a leave request for a single day.
2. Verify that the attendance row is created with `SourceType=LEAVE` and the mapped status.
3. Try a conflict case where a manual attendance row already exists for that day and confirm that the service records a conflict rather than overwriting it.
4. Reject an already approved leave request and confirm the source-owned attendance row is removed or reversed only for the matching leave source.
5. Lock the month and confirm that manual attendance changes are rejected while leave-owned attendance remains protected.
6. Verify that the employee receives notification history and that HR/Admin see the request and decision in the management workspace.

### Approval and reconciliation safety update (2026-08-01)

Approval state and attendance synchronization now have separate persisted state on
`LeaveRequest`. A successful leave decision begins with `ReconciliationStatus =
Pending`; reconciliation records `Completed`, `Conflict`, `RetryScheduled`, or
`Reversed` independently. Therefore an accepted leave must not be read as proof
that attendance creation succeeded.

The decision service rejects a reviewer whose authenticated user ID is the
requester (`LEAVE_SELF_APPROVAL_NOT_ALLOWED`). It also accepts an optional
`ExpectedVersion` from current clients and rejects stale versions before the
existing Mongo compare-and-set update. Employee leave balances now carry a
version; decision debit/credit filters include that version and increment it,
which prevents a second concurrent decision from changing the same balance.

When immediate reconciliation fails after a committed approval, the request stays
approved, is marked `RetryScheduled`, retains a safe error code/message and a
five-minute next-attempt timestamp. The current release does not yet include the
durable leave outbox worker that consumes that timestamp; HR must treat this as an
operational follow-up rather than a completed production retry implementation.

Reconciliation still refuses to overwrite manual or WFH attendance. It records
`Conflict` when such a row is encountered, and reversals delete only matching
`LEAVE` source rows before invalidating unlocked summaries.

- Test policy duplicate names/codes inside one company and the same values in a second company.
- Confirm the created policy and each balance carry the authenticated company ID.
- Confirm `ApplicableTo=[]` creates balances only for same-company active users; confirm `null` assigns none.
- Test a valid mapped approval, invalid/missing mapping rejection, manual-attendance conflict, and accepted-to-rejected reversal.
- Verify pending notification reaches only same-company HR/Admin; verify status notification reaches the employee in profile and email logs record success/failure.
- Run monthly accrual only against an isolated test database when performing runtime QA; it changes balances.

## Related references

- [Attendance module](attendance-module-current-flow-and-audit.md)
- [Employee profile and attendance calendar](employee-profile-current-flow.md)
- [Calendar module](calendar-module-current-flow.md)
- [End-to-end Leave/Attendance/Payroll flow](leave-attendance-payroll-end-to-end-flow.md)
