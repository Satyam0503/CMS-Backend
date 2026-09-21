# Offboarding Notice-Period Reminder Engine

## Source of truth

The server owns reminder scheduling. An approved `EmployeeExit` in `NoticePeriod` receives durable embedded reminder records. `EmployeeExitOutboxProcessor` claims due records atomically and writes retryable `EmployeeExitOutbox` work. The existing notification, SignalR, and email services deliver the work.

## Policy

`EmployeeExitPolicy` controls whether reminders are enabled, the recurring interval, milestone offsets before the Last Working Date, eligible audiences, and email delivery. These values are copied to `EmployeeExit.PolicySnapshot` when the case is created; editing a company policy does not rewrite an active case.

## Recipients and authorization

Recipients are resolved inside the tenant from active employee records. Employee and reporting-manager delivery follows the case snapshot. HR recipients are active users who pass the existing Offboarding case-management permission check. Administrators are opt-in and never inferred from browser state or client-supplied recipient IDs.

## Reliability

Each reminder/recipient pair has a stable tenant-scoped `DedupeKey`. Due reminders are claimed with a MongoDB conditional update before outbox creation. Outbox delivery retries with exponential backoff and dead-letters after repeated failure. LWD changes cancel pending schedules and create a fresh schedule.

When five or more cases are due in one worker cycle, authorized HR case managers also receive one deduplicated `ExitReminderDigest` event targeting the offboarding queue. Employee and reporting-manager reminders remain separate.

## Lifecycle and user actions

Pending or queued reminders are cancelled when a case is withdrawn, cancelled, completed, or exceptionally completed. In-app notification records support recipient-scoped read, snooze, and dismiss actions. These actions do not alter another recipient's notification state.

## Verification

Run the backend solution build and the frontend TypeScript check. Runtime verification additionally requires the configured MongoDB instance, authenticated tenant users, SignalR, and email provider. Browser tests must verify employee, manager, and HR visibility separately; a successful compile alone does not prove delivery.
