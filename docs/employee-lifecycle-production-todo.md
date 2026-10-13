# Employee lifecycle production plan

## Business rule

Medical, maternity, paternity, long-term leave, and sabbatical are lifecycle events, not recurring leave entitlements. A linked policy may be requested for any approved duration, but it must be approved by the existing leave approver (manager, HR, or administrator according to the tenant's configured workflow). It must not require, create, or accrue an employee leave balance.

Approval remains the control point. Once approved, the existing reconciliation flow creates source-owned attendance records and the existing lifecycle case flow opens the case. Payroll continues to consume the resulting approved attendance/status data and should not infer entitlement from a balance that does not exist.

## Completed in this pass

- [x] Trace policy creation/update, leave request, approval/reversal, lifecycle case, attendance reconciliation, and accrual paths.
- [x] Bypass balance existence, sufficiency, debit, and reversal checks for an active lifecycle-linked policy.
- [x] Prevent no-accrual policies from allocating balances during policy synchronization.
- [x] Stop the scheduled accrual job from crediting an active lifecycle-linked policy.
- [x] Remove legacy unused entitlements when a policy is linked to a lifecycle event, retaining consumed history with zero remaining entitlement.
- [x] Reject the invalid `None` accrual plus non-zero credit combination at policy validation.
- [x] Keep lifecycle approval, attendance reconciliation, permissions, tenant filtering, and API payloads unchanged.
- [x] Provide a dedicated Payroll > Lifecycle Management workspace with responsive policy, reconciliation, and case views.

## Verification checklist

- [ ] Create a medical policy with accrual period `None` and credited leaves `0`.
- [ ] Link it under Lifecycle Management > Policy controls.
- [ ] Submit a multi-day medical request without a balance.
- [ ] Verify manager/HR/admin approval is required and self-approval remains blocked.
- [ ] Verify approval creates attendance reconciliation and a lifecycle case.
- [ ] Verify payroll reads the approved attendance result and does not deduct an entitlement.
- [ ] Verify rejection/withdrawal reverses attendance and cancels the lifecycle case.
- [ ] Verify a normal monthly/yearly leave policy still allocates, debits, restores, and accrues normally.
- [ ] Verify offboarding resignation, withdrawal, approval, clearance, final settlement, and permissions remain unchanged.
- [ ] Run backend tests, frontend tests, typecheck, lint, and production builds.
- [ ] Verify desktop, tablet, and mobile modal/page layouts, keyboard navigation, Escape-to-close, loading, error, empty, and populated states.

## Operational safeguards

Do not encode jurisdiction-specific medical-leave entitlement in the product. Configure approval roles, evidence, expected return date, attendance mapping, paid/unpaid treatment, and payroll treatment per company policy. Any future migration that changes existing paid/credited policies must be explicit, audited, and reversible.
