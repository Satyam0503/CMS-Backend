# Blocked manual QA tests

## Environment blocker

The API build cannot complete because existing processes hold the API output DLLs. The QA run did not terminate those processes because their ownership and purpose were not established. Port 5036 has no listening API instance.

## Scope blocker

Payroll generation, payroll processing, attendance month locking, and attendance reopening are blocked by the shared-database safety rule, even after the API environment becomes available.

## Blocked test inventory

- Employee/profile authorization and cross-user edits
- Leave request, approval/rejection, duplicate approval, balance, and reconciliation
- Attendance check-in/out, half-day, absence, holiday, weekly off, and duplicate handling
- Job creation, application, synthetic resume, stage change, saved job, and follow company
- Login/logout/refresh/expired-token/disabled-user behavior
- Role and permission enforcement
- Company profile and public profile behavior
- UI responsiveness, validation, loading, error, pagination, sorting, search, theme, and state synchronization

These tests are unexecuted. They have no pass/fail result.
