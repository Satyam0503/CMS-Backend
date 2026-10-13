# Leave Management and Attendance Production Audit

Date: 2026-08-13  
Scope: source review, targeted remediation, and focused unit tests. No database records, configuration, migrations, or runtime APIs were changed.

## Outcome

The modules have substantial production safeguards: authenticated company context is passed through the reviewed API paths; workflow-owned attendance is protected from manual overwrite; active leave requests cannot overlap other active leave requests; WFH requests reject overlapping WFH requests; attendance status mappings are company-specific; and leave-balance/status changes use optimistic concurrency with a transaction when MongoDB supports it.

This remediation adds a company-scoped Leave/WFH availability gate on request create, leave edit, and approval. The complementary approval path rechecks the same rule before it changes state. A simultaneous race can leave conflicting requests pending, but it cannot produce two approved workflow records that both own attendance; the second approval is rejected deterministically.

Tenant-bearing default repository reads and aggregate reads now fail closed when `CompanyId` is absent, including `AttendanceModel` and other entities which do not implement the audited base interface. `withDefaultFilter: false` remains an explicitly named, intentionally unscoped maintenance/background escape hatch and each use must carry its own company predicate or be audited as a deliberate all-company operation.

## Verification performed

Command:

```powershell
dotnet build Codeji.CMS.Repository\Codeji.CMS.Repository.csproj --no-restore -v:minimal
dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj --no-restore --filter "FullyQualifiedName~LeaveManagementServiceTests|FullyQualifiedName~WorkFromHomeDateTests|FullyQualifiedName~AttendanceStatusCalculationServiceTests|FullyQualifiedName~AttendancePayrollRulesTests|FullyQualifiedName~TenantIsolationFilterTests" -v:minimal
```

Result: repository build succeeded; focused regression suite **passed - 48/48 tests**.

The build emitted pre-existing nullable/obsolete warnings, but no test failures. These tests are deterministic unit tests; they do not exercise MongoDB transactions, unique indexes, authorization middleware, concurrent writes, or browser behavior.

## Controls confirmed from source

| Area | Evidence-backed behaviour | Result |
|---|---|---|
| Leave date overlap | Creation and pending-request edits query active requests in the authenticated company and reject intersecting ranges. | Pass for Leave vs Leave, subject to concurrency gap below. |
| WFH date overlap | Validation rejects a request that intersects a pending, approved, or returned WFH request for the authenticated employee/company. | Pass for WFH vs WFH, subject to concurrency gap below. |
| Leave/WFH policy routing | A WFH-type leave policy is rejected from the normal leave request path; WFH must use its separate workflow. | Pass. |
| Attendance source ownership | Manual attendance validation rejects rows owned by `LEAVE` and `WFH_REQUEST`. | Pass. |
| Status safety | Attendance status lookup is company-scoped; leave-mappable statuses must be active, no-time, and marked available for leave management. System statuses cannot be deleted. | Pass. |
| Leave status/balance concurrency | Approval/reversal uses versioned leave updates; balance changes and leave decision use a Mongo transaction when available. | Pass in source; replica-set runtime still required. |
| WFH approval authorization | Controller requires `ApproveTeam`; service prevents self-approval and requires either the assigned approver or a company reviewer. | Pass. |
| Request tenant scoping | Reviewed leave/WFH/attendance controller paths obtain company and user from `CurrentContext`, not request tenant fields. | Pass for reviewed paths. |

## Confirmed findings

### LA-01 - Leave/WFH collisions were not prevented before approval

Severity: High  
Status: Fixed and regression-checked

`LeaveManagementService` now checks active `WorkFromHomeRequest` records during leave creation, pending-request update, and immediately before approval. `WorkFromHomeService` checks active leave records during request validation and repeats the check immediately before a manager approval is finalized. These checks are company-scoped and include Pending, Approved/Accepted, and Returned workflow states that reserve availability.

Impact after remediation: the second conflicting request is rejected at submission when visible, or at final approval if requests were submitted concurrently. Reconciliation remains a recovery control for historic/corrupt data, not the normal overlap control.

The current product rule rejects any date intersection, including complementary half-days. If the business later permits complementary halves, replace the date-level predicate with an explicit first-half/second-half reservation model and cover it with an isolated Mongo integration test.

### LA-02 - Concurrent submissions can leave conflicting requests pending

Severity: Medium  
Status: Mitigated; isolated-runtime verification still required

Both workflows still validate overlap by reading existing documents and then inserting/updating a separate request. There is no MongoDB date-range exclusion constraint or day/segment reservation collection.

Impact: concurrent submissions can both pass the create-time read and coexist as Pending. The new final approval recheck prevents both becoming Approved and prevents conflicting attendance ownership. This is safe for attendance, but it is less polished than reserving availability at submission.

Follow-up hardening: introduce a tenant-and-employee day/segment reservation collection with a unique key such as `(CompanyId, UserId, Date, Segment)` if the product requires concurrent submissions to fail at submit time rather than at approval.

### LA-03 - Repository tenant filtering could fail open

Severity: Critical cross-cutting blocker  
Status: Fixed for default repository reads and aggregate reads

`MongoRepository` now adds an explicit `CompanyId` predicate for every tenant-bearing entity on default reads, including attendance entities. When the company context is missing it adds an impossible tenant value, returning no data rather than all-company data. Aggregate queries use the same direct BSON filter and no longer cast non-audited attendance entities to `ISupportAuditing`.

Impact after remediation: default request reads fail closed. The explicit `withDefaultFilter: false` calls used by scheduled cross-company work remain a deliberate review boundary; they are not safe as a casual soft-delete option.

Follow-up hardening: replace the legacy boolean `withDefaultFilter` escape hatch with a separately authorized maintenance repository/context so an ordinary caller cannot accidentally request an unscoped read.

## Risks requiring product decision or runtime proof

| Risk | Why it matters | Needed proof |
|---|---|---|
| Leave approver model | Leave status update protects self-withdrawal and self-approval, but the reviewed service does not use `AssignedReviewerUserId`; any principal granted generic Leave Management Edit may decide another employee’s request. This may be intended HR authority, but it is not manager-only approval. | Confirm approval policy, then add authorization tests for manager, HR, admin, and unrelated editor. |
| Mongo topology | Leave code falls back when Mongo transactions are unsupported. The fallback is protective but cannot provide the same atomicity as a replica set. | Test only against an isolated replica-set database for production sign-off. |
| Existing legacy data | Rows lacking `CompanyId`, duplicate day records, and prior workflow rows can still create reconciliation conflicts. | Read-only data audit in a non-production copy before cleanup or migration. |
| Status/policy combinations | Unit tests do not run every company-custom status, leave-policy mapping, half-day segment, payroll-lock, and office-calendar combination. | Seed synthetic Test-company data and run the matrix below. |

## Required isolated-runtime test matrix

Use a Testing environment and an isolated MongoDB database whose name contains `test`, `qa`, or `automation`; use only synthetic companies/users. Do not run migrations, cleanup, or destructive commands without approval.

1. Create Company A and Company B; prove every list/read/update/delete endpoint rejects or hides the other company’s leave, WFH policy, WFH request, attendance row, correction request, status, balance, calendar setting, and export.
2. For one employee, test Leave vs Leave: pending overlap, approved overlap, update-to-overlap, withdrawal/retry, rejected-with-reason, stale-version approval, insufficient balance, and concurrent approval attempts.
3. Test WFH vs WFH: full day, first half, second half, weekly quota, same-day cutoff, returned request, cancellation before/after office start, clocked-request cancellation, stale decision version, and concurrent submission/approval.
4. Test Leave vs WFH in both orders for the same day and half-day segments. Expected production behaviour must be an immediate deterministic rejection for a conflicting segment, not an approved request plus a blocking exception.
5. Test every active attendance status: no-time leave status, timed present/WFH status, inactive status, invalid custom code, `HD`, `LHD`, `ED`, `LHD+ED`, `WFH+WFO`, WFH half-day combinations, and protected source-owned rows.
6. Test holidays, configured weekly offs, employee joining date, exit date, payroll lock, monthly summary invalidation, correction-request review, and tenant-scoped exports.
7. Repeat critical tests with simultaneous API requests against a replica set and verify unique indexes, source versions, balances, notifications, outbox records, and no duplicate day/segment records.

## Files reviewed

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs`
- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs`
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs`
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs`
- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`
- `Codeji.CMS.Repository/Repositories/MongoRepository.cs`
- `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- `Codeji.CMS.API/Controllers/AttendanceController.cs`
- focused tests in `Codeji.CMS.Services.Tests`

## Conclusion

The code now prevents Leave/WFH conflicts from reaching two approved attendance-owning workflows and makes default tenant repository reads fail closed. The focused suite passes. Production sign-off still requires the isolated runtime matrix, particularly replica-set concurrency and authorization scenarios; source-level tests cannot prove browser/API middleware, real Mongo indexes, or existing data quality.
