# Offboarding Data Model and Rules

Last verified against code: 2026-09-07.

## Policy builder behavior

The policy settings page configures the recommended journey for future cases. Preset level is only a starting point: administrators can independently enable or disable every module and independently mark enabled modules as completion blockers. The resolver applies the most-specific matching scope and snapshots the resulting policy onto the exit case. Policy changes therefore affect newly created cases only; active cases remain stable and auditable. Disabled modules are omitted from runtime journey UI, while server-side authorization and completion validation remain authoritative.

| Record | Source | Purpose |
|---|---|---|
| `EmployeeExit` | `Codeji.CMS.Repository/Entities/Employees/EmployeeExit.cs` | Tenant-owned aggregate for status, LWD, policy snapshot, operations and audit events. |
| `EmployeeExitPolicy` | `.../EmployeeExitPolicy.cs` | Company/department/role policy used to resolve a new case. |
| `EmployeeExitClearanceTemplate` | `.../EmployeeExitClearanceTemplate.cs` | Company configuration copied into a case as clearance work. |
| `EmployeeFinalSettlement` | `.../EmployeeFinalSettlement.cs` | Final settlement calculation, approval and payment information. |
| `EmployeeExitOutbox` | `.../EmployeeExitOutbox.cs` | Durable queued exit notifications/events. |
| `FormerEmployeeAccessToken` | `.../FormerEmployeeAccessToken.cs` | Expiring/revocable restricted portal access. |

`EmployeeExit` embeds handover tasks, assets, access items, finance and benefit items, evidence, reminders, post-exit access, clearances, reassignment, interview, documents and audit events. Field/status definitions are in the entity; transport contracts are in `EmployeeExitDtos.cs`.

Rules enforced by service methods include:

1. Actor and company come from authenticated context; a request does not establish a company boundary.
2. An active case is versioned. A write with an outdated `Version` must refresh before retrying.
3. Policy is resolved for a new case and snapshotted, so later configuration changes do not retroactively change that case.
4. Normal completion depends on state, LWD timing, and only the operational gates enabled by the snapshot. Exceptional completion is a separate request.
5. Evidence, portal records and operational items are case and company scoped; access also depends on the operation-specific authority and evidence visibility.

For index/migration details and the full policy-resolution and portal discussion, use `../offboarding-module-current-flow.md` and the migrations under `Codeji.CMS.Migrations/Migrations`.

## Lifecycle state synchronization

The additive lifecycle state is synchronized at the authoritative EmployeeExit boundaries:

| Exit boundary | Lifecycle result | Employee relationship |
|---|---|---|
| Resignation pending review | `EmploymentStatus=Active` | Current employee |
| Approved resignation / HR initiation | `EmploymentStatus=NoticePeriod` | Current employee through LWD |
| Completed exit | `Resigned` or `Terminated`, `AccountStatus=Closed`, `PayrollStatus=FinalSettlement`, `OffboardingStatus=Completed` | Former employee |
| Rejoin | `EmploymentStatus=Active`, `AvailabilityStatus=Working`, payroll/account active | Current employee |

Legacy `Status=false` is not sufficient evidence of a former employee during migration. The lifecycle backfill preserves employment as `Active` unless legacy employment explicitly says `Resigned` or `Terminated`, while representing legacy account inactivity as `AccountStatus=Closed`.
# Offboarding queue visibility

Active case summaries are tenant-scoped. HR/admin users with offboarding case-management access can see the company queue; a manager can see only employees assigned to them as reporting manager. The queue is a summary only; case details are authorized again when the employee profile loads.
