# Employee lifecycle deployment runbook

## Before deployment

1. Back up the tenant MongoDB database and verify the backup can be restored.
2. Confirm the migration runner points to the intended database and environment.
3. Confirm `EmployeeExit` has no unresolved duplicate active records.
4. Confirm the application and migration runner use the same Mongo collection names.
5. Review the lifecycle transition matrix and permission seed changes.

## Migration order

The migration runner discovers lifecycle migrations automatically. The safe order is:

1. Backfill legacy `EmploymentStatus` and `LastWorkingDate` compatibility fields.
2. Backfill `EmpUser.Lifecycle` for records that do not yet have a lifecycle document.
3. Add tenant-scoped lifecycle indexes.
4. Run a read-only verification query for employees, lifecycle versions, active exits, and former-portal records.

## Verification queries

- Every non-deleted employee has `Lifecycle.EmploymentStatus`, `AvailabilityStatus`, `AccountStatus`, and `PayrollStatus`.
- `Lifecycle.EmploymentStatus` is `Resigned` or `Terminated` only when the employee is represented by a completed `EmployeeExit`, except for explicitly documented legacy rows under remediation.
- Employees with `Sabbatical`, leave, suspension, or absconding review are not returned by the former-employee query.
- Exit-month payroll remains eligible through the stored last working date.
- Future payroll is excluded for lifecycle-closed employees.
- Lifecycle indexes exist under the names created by `AddEmployeeLifecycleIndexes`.
- `GET api/User/EmployeeLifecycle/History/{employeeUserId}` returns history only for an authenticated HR/admin actor with lifecycle-management permission; ordinary directory viewers must receive `LIFECYCLE_FORBIDDEN`.

## Rollback and incident response

Do not delete lifecycle fields during rollback. Disable the lifecycle UI/API route, preserve the audit history, and continue using legacy read compatibility while investigating. Restore from backup only after confirming the migration or application write caused data corruption. Never alter completed `EmployeeExit` records to repair a lifecycle read-model issue.

## Required release evidence

- Migration output and database verification results.
- Backend build and focused/full test counts.
- Frontend typecheck, lint, build, and browser test counts.
- Authenticated checks for HR/admin, manager, employee, and former-employee portal roles.
- Desktop and mobile screenshots or equivalent runtime verification for the lifecycle dialog.

## Current validation evidence

- Isolated backend solution build: 0 errors.
- Focused lifecycle/attendance/payroll tests: 15 passed.
- Services test suite: 182 passed.
- Frontend typecheck and production build: passed.
- Authenticated API and browser E2E execution remains a deployment-gate requirement; the local API harness currently requires a Windows EventLog permission fix before its results can be treated as product evidence.
