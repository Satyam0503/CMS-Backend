# Attendance hardening validation report

## Completed checks

- `dotnet build Codeji.CMS.API/Codeji.CMS.API.csproj --no-restore --verbosity minimal`: passed.
- `dotnet build Codeji.CMS.Migrations/Codeji.CMS.Migrations.csproj --no-restore --verbosity minimal`: passed.
- `dotnet test Codeji.CMS.Services.Tests/Codeji.CMS.Services.Tests.csproj --no-restore --verbosity minimal`: passed, 73 tests.

## Covered source controls

- Single and bulk mutations use `IAttendanceMutationValidator`.
- Bulk writes require a transaction and fail closed on standalone MongoDB.
- Audit and outbox rows are written with attendance mutations; the outbox worker retries in-app delivery and dead-letters after ten attempts.
- Employee self-view uses `Attendance.ViewOwn`; HR/Admin management uses explicit all-view/create-for-employee/override grants.
- Pending correction requests block payroll processing for the affected payroll month.

## Required deployment validation

The following are not established by the database-free suite and must run only in an isolated testing environment using synthetic data and a MongoDB replica set:

1. Run `HardenAttendanceRolePermissions` and verify employee role revocation/grants for more than one tenant.
2. Submit a multi-row bulk mutation, inject a write failure, and verify attendance/audit/outbox collections remain unchanged.
3. Verify schedule precedence: employee assignment, department assignment, company default.
4. Verify correction submission/review and payroll block/release through authenticated UI/API calls.
5. Verify SignalR and SMTP delivery plus retry/dead-letter observability.
6. Run React typecheck/build after resolving the local Windows `EPERM` access failure on `C:\Users\User 2`.
