# Leave management audit

## Purpose and flows

Frontend: `modules/leaves/*` and profile leave tracker.  
Backend: `LeaveManagementController` and `LeaveManagementService` cover policy maintenance, balances, request creation, review, deletion, summaries, and accrual. Accepted/rejected reviews invoke `LeaveAttendanceReconciliationService` to synchronize attendance.

## Permissions and tenant boundaries

The controller is authenticated and uses module permissions on the management operations. Services use current company context and explicitly include company conditions in key leave/attendance lookups. Leave approval changes balance, request status, attendance, summaries, and notifications across multiple writes.

## Confirmed bugs

No fresh confirmed leave defect is filed. The previously reported leave-tab and profile refresh defects are excluded because the repository’s current report says they were addressed.

## Data-consistency and security risks

- Approval/rejection, balance adjustment, attendance reconciliation, summary invalidation, and notification delivery are not one database transaction. A mid-flow failure can leave a partial state; source review alone cannot quantify likelihood.
- Review/update concurrency needs an isolated race test: two reviewers, retry after timeout, and simultaneous withdrawal/review.
- `LeaveNotification` is `async void`, so callers cannot await or observe notification failure; this is a reliability design gap.

## Tests needed

Two-tenant IDOR tests; balance-boundary/overlap tests; working-day/holiday/half-day cases; idempotent approval/reversal tests; and failure-injection tests between each persisted write.
