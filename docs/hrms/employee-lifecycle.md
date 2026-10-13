# Employee Lifecycle Module

## Purpose

Employee lifecycle separates employment, temporary availability, account access, payroll handling, offboarding progress, former-employee access, and alumni eligibility. This avoids overloading one employee status with unrelated business meanings.

## Core dimensions

`EmpUser.Lifecycle` is the embedded lifecycle state. `EmployeeLifecycleTransitionService` is the central transition helper.

| Dimension | Meaning | Typical effect |
|---|---|---|
| Employment status | Current or ended employment relationship | Active/current or Resigned, Terminated, Retired, ContractEnded, Deceased |
| Availability status | Whether a current employee is normally available | Working, MedicalLeave, MaternityLeave, PaternityLeave, LongTermLeave, Sabbatical |
| Account status | Login/access disposition | Active or AccountClosed |
| Payroll status | Payroll handling | Active, Hold/Paused, FinalSettlement |
| Offboarding status | Exit workflow progress | None through Completed |
| Former portal status | Post-exit portal eligibility/access | NotEligible and configured access states |
| Alumni status | Rehire/alumni disposition | Eligible or business-specific state |
| Version | Optimistic concurrency token | Incremented on accepted transitions |

Employment and availability are deliberately separate. Medical or maternity leave changes availability while the employee remains employed. Completing an exit changes employment, account, payroll, offboarding, former-portal, and potentially alumni dimensions together.

## Lifecycle policy configuration

A lifecycle policy links an employment-impacting absence type to an existing active Leave Policy. Leave Management remains authoritative for entitlement, balance, paid/unpaid handling, request approval, and attendance status mapping.

Supported configurable types:

- Medical Leave
- Maternity Leave
- Paternity Leave
- Long-Term Leave
- Sabbatical

### Setup order

1. HR creates and activates a Leave Policy in Leave Management.
2. The Leave Policy defines code, credit frequency, amount, maximum balance, paid/unpaid behavior, notice rules, half-day behavior, eligibility, and attendance mapping.
3. HR opens Payroll → Leave Management → Leave Policies.
4. HR enables Lifecycle special leave on the Leave Policy, selects the lifecycle event, and configures reason, expected return date, evidence, and approval requirements.
5. The lifecycle workspace is used to review operational state, reconciliation, and cases. Its legacy Policy Controls view is read-only and must not be used to author a second configuration.
5. Save persists the tenant-owned lifecycle policy.

### Save rules

`PUT api/User/EmployeeLifecycle/Policies` accepts a dedicated request DTO. The server supplies company, audit, identity, deletion, and activity fields.

The service requires an allowlisted event type and a linked Leave Policy belonging to the same company that is active and not deleted. Existing updates use the previous version in the concurrency filter. Today is a valid effective date; a date before today is rejected on update.

Policy reads return active, non-deleted records whose effective date has started. The self-service projection lets employees see request implications without HR edit access.

## Approved-leave lifecycle flow

```text
Employee selects Leave Policy
        |
Lifecycle policy lookup by LeavePolicyId
        +-- no link --> ordinary leave workflow
        |
Employee sees reason/return/evidence requirements
        |
Leave submitted and approved
        |
Balance handling + attendance reconciliation
        |
Idempotent EmployeeLifecycleCase creation
        |
Temporary availability change
        |
Evidence/review/extension/return tracking
        |
Completed/cancelled case restores Working when appropriate
```

Case creation confirms the leave belongs to the company and is accepted, resolves the linked policy, prevents duplicate active cases for one request, snapshots leave days and reconciliation status, creates the case, and updates employee availability with version protection.

## Case states and guards

States are `Open`, `UnderReview`, `Active`, `Extended`, `Completed`, and `Cancelled`.

- Required-document cases cannot become Active until evidence is verified.
- Completed requires an actual return date.
- Extension requires a later valid expected return date.
- Completion/cancellation restores Working only if the case still owns the employee's current availability state.
- Each accepted update records an event and uses optimistic concurrency.
- Rejected or withdrawn approved leave cancels the corresponding active lifecycle case and restores availability where applicable.
- Overdue return is computed for active cases whose expected return is before today.

## Evidence

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `api/EmployeeLifecycleCases/{caseId}/evidence` | Upload private evidence, maximum request 10 MB |
| PATCH | `api/EmployeeLifecycleCases/evidence/verification` | Verify or reject evidence |
| GET | `api/EmployeeLifecycleCases/{caseId}/evidence/{evidenceId}` | Authorized download |

Files are kept in a private lifecycle evidence area. Metadata includes name, MIME type, size, SHA-256 hash, uploader, verification status, verifier, and timestamps. Internal storage keys must not be serialized.

## UI flow

The Employees page shows the lifecycle workspace only to capable HR/admin users.

- **Policy controls:** read-only compatibility view showing the linked Leave Policy, effective date, requirements, and linked/not-configured status. Authoring is in Leave Management.
- **Reconciliation:** read-only linked-case count, leave-day snapshot, reconciliation states, loading/error/empty states; it does not mutate source Leave or Attendance data.
- **Lifecycle cases:** active/pending-document/overdue/upcoming-return metrics, status/overdue filters, case detail, owner, evidence, extension, actual return, and timeline.
- **Direct lifecycle dialog:** reserved for authorized direct dimensions; leave-owned absence must use the leave/case workflow.

Dialogs use MUI focus management, Escape close, responsive sizing, internal scrolling, sticky headers, accessible labels, and permission-aware controls.

## Permissions

- Policies list: Employees View.
- Policy save: Employees Edit.
- Case management: Offboarding CaseManage/Edit checked server-side.
- Direct lifecycle changes: protected Employees/Offboarding operations.
- Employee policy implications: authenticated read-only self-service route.

## API inventory

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `api/User/EmployeeLifecycle/Policies` | HR effective policies |
| GET | `api/User/EmployeeLifecycle/SelfServicePolicies` | Employee read-only implications |
| PUT | `api/User/EmployeeLifecycle/Policies` | Create/update link |
| POST | `api/User/EmployeeLifecycle` | Protected lifecycle change |
| POST | `api/User/EmployeeLifecycle/Availability` | Availability transition |
| POST | `api/User/EmployeeLifecycle/Dimension` | Protected dimension transition |
| GET | `api/User/EmployeeLifecycle/History/{employeeUserId}` | History |
| GET | `api/EmployeeLifecycleCases` | Filtered cases |
| GET | `api/EmployeeLifecycleCases/summary` | Metrics |
| POST | `api/EmployeeLifecycleCases` | Create from approved leave |
| PATCH | `api/EmployeeLifecycleCases` | Update case/owner/return |

## Important failure codes

- `LIFECYCLE_POLICY_INVALID`
- `LIFECYCLE_POLICY_EVENT_TYPE_INVALID`
- `LIFECYCLE_LEAVE_POLICY_NOT_FOUND`
- `LIFECYCLE_POLICY_EFFECTIVE_DATE_INVALID`
- `LIFECYCLE_POLICY_VERSION_CONFLICT`
- `LIFECYCLE_RETURN_DATE_REQUIRED`
- `LIFECYCLE_RETURN_DATE_INVALID`
- `LIFECYCLE_REASON_REQUIRED`
- `LIFECYCLE_CASE_FORBIDDEN`
- `LIFECYCLE_CASE_STATUS_INVALID`
- `LIFECYCLE_EMPLOYEE_VERSION_CONFLICT`

## Implementation map

Backend: `EmployeeLifecycle.cs`, `EmployeeLifecyclePolicy.cs`, `EmployeeLifecycleCase.cs`, `EmployeeLifecycleTransitionService.cs`, `EmployeeLifecyclePolicyService.cs`, `EmployeeLifecycleCaseService.cs`, `UserController.cs`, and `EmployeeLifecycleCaseController.cs`.

Frontend: `Employees.tsx`, `employeeServices.ts`, `EmployeeLifecyclePolicyConfig.tsx`, `EmployeeLifecycleReconciliationPanel.tsx`, `EmployeeLifecycleCaseQueue.tsx`, and `EmployeeLifecycleDialog.tsx`.

## Validation and limitations

Relevant suites cover lifecycle transitions, policy service, leave integration, API authorization/security, and focused React policy/reconciliation behavior. Release acceptance must additionally prove live save/reload, linked leave approval, idempotent case creation, evidence authorization, extension, return, reversal, tenant isolation, and responsive UI.

Policy/case operations can update multiple MongoDB documents, so versioning and idempotency reduce but do not eliminate partial-write risk. Reconciliation is read-only evidence, not a general repair console. Lifecycle-sensitive export and notification expansion require explicit authorization, retention, and recipient decisions.
