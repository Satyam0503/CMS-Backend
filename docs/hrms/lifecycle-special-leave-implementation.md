# Lifecycle special leave implementation

## Purpose

This project is India-first. Ordinary leave remains an employee entitlement. Medical, maternity, paternity, long-term leave, and sabbatical are optional approval-driven lifecycle events. They are configured from Leave Management and executed from Lifecycle Management.

## Ownership model

| Concern | Owner | Behaviour |
|---|---|---|
| Leave name, code, paid/unpaid flag, attendance mapping | `LeavePolicy` | The normal leave contract and payroll inputs |
| Accrual, balance, carry-over | `LeavePolicy` + entitlement records | Disabled for lifecycle special leave |
| Lifecycle event, approval and evidence requirements | `LeavePolicy.Lifecycle` and compatibility `EmployeeLifecyclePolicy` | One active company rule per event |
| Approval and case creation | Leave request approval + lifecycle case service | Case is created only after approval |
| Attendance | Leave attendance reconciliation | Approved dates receive the policy attendance status |
| Payroll | Payroll period calculation | Uses approved attendance and the policy paid flag; no duplicate payroll rule |
| Account/availability | Lifecycle transition/case workflow | Applied only at the lifecycle transition defined by the event |

## Configuration flow

1. HR opens Payroll > Leave Management > Leave Policies.
2. HR creates or edits a Leave Policy and enables **Approval-driven special leave**.
3. HR selects one lifecycle event and the required reason, return date, evidence, and HR approval controls.
4. The API normalizes accrual amount, accrual period, maximum balance, and carry-over to zero/off.
5. The LeavePolicy is saved in the tenant's leave-policy collection.
6. The API synchronizes the existing EmployeeLifecyclePolicy compatibility record using the LeavePolicy id, preserving its version and tenant boundary.
7. A failed lifecycle synchronization is returned as an error; the UI must not show a false success.
8. The UI reloads from the API after success, so Linked/Not configured is derived from persisted records rather than local state.

## Employee request flow

1. Employee selects a lifecycle special leave policy.
2. The request form does not require or validate a leave balance for that policy.
3. Reason, expected return date, and evidence are enforced from the synchronized lifecycle policy.
4. The request enters the configured approval route. A pending request has no attendance, account, or payroll effect.
5. On approval, leave attendance reconciliation writes the source-owned attendance result and the lifecycle case workflow applies the event state.
6. Payroll reads the approved result for the payroll period. Paid/unpaid remains the LeavePolicy decision; payroll does not debit a leave balance.
7. Rejection, withdrawal, or cancellation does not create an entitlement debit. Any already-created operational effects are reversed through the existing reconciliation/state-transition workflow.

## Normal leave flow

Normal leave is unchanged: it can accrue credits, displays a balance, validates availability, debits only on the existing approval path, reconciles attendance, and feeds payroll according to its paid flag.

## Safety rules

- Every read/write is tenant-scoped.
- Lifecycle links may target only active, non-deleted LeavePolicy records.
- A lifecycle policy cannot be attached to WFH or another invalid policy type.
- Stale lifecycle versions are handled by the existing compatibility save contract; successful saves are re-read before rendering.
- Effective dates are date-only business values and are normalized to UTC without the `0001-01-01` display bug.
- Existing `EmployeeLifecyclePolicy` documents and endpoints remain intact for compatibility.
- The old Lifecycle Policy Controls surface is an operational compatibility view; Leave Management is the authoring source.

## Known operational distinction

Lifecycle policy configuration is not the same thing as automatically changing an employee's account. Configuration defines the approval and transition rules. Account, employment, availability, attendance, and payroll status changes occur only when an approved lifecycle case executes the corresponding transition, preserving auditability and preventing an unapproved request from changing payroll.

## Verification checklist

- Create a medical policy with lifecycle enabled: accrual and balance are zero/off; an active compatibility policy links to its id.
- Refresh Leave Management: lifecycle configuration remains present.
- Refresh Lifecycle Management: the same event remains linked.
- Apply medical leave exceeding any previous balance: no balance error; required lifecycle fields are enforced.
- Submit pending request: no attendance or payroll effect.
- Approve request: case, attendance reconciliation, and payroll input are created through existing services.
- Create/edit ordinary leave: existing balance and accrual behaviour remains unchanged.
- Save a stale lifecycle snapshot: conflict handling does not silently revert the UI.
- Fail compatibility synchronization: response is unsuccessful and no success toast is shown.
- Verify desktop, tablet, and narrow mobile layouts; no horizontal overflow is introduced.
