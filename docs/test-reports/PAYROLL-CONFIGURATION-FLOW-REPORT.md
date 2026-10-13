# Payroll Configuration Flow and HRMS Comparison Report

**System reviewed:** Codeji CMS / HRMS backend  
**Review date:** 2026-09-29  
**Scope:** Payroll configuration, assignment resolution, automatic calculation, payroll lifecycle, payment, and comparison with established HRMS payroll patterns.

## Executive conclusion

The CMS payroll configuration is necessary and is actively used by automatic payroll. It determines which employees are included, which payroll policy applies, how salary templates and pay groups are selected, how attendance locking is enforced, and which policy/template identifiers are stored in the payroll calculation snapshot.

The current design is a useful configuration foundation, but it is not yet a complete production bank-connected or enterprise payroll-control layer. It has row-level tenant safety, effective dating, approval, lifecycle guards, and calculation snapshots. It does not yet have a first-class payroll run, pay calendar/control record, bank-payment batch, provider integration, statutory jurisdiction model, arrears ledger, or reconciliation workflow.

**Current automated evidence:** backend services suite **416 passed, 0 failed**; API compile passed. Live MongoDB transaction/concurrency and bank-provider validation were not available.

## 1. Configuration objects and relationships

```text
Company / tenant
  ├── PayrollPolicyProfile
  │     ├── divisor policy
  │     ├── attendance-lock requirement
  │     ├── effective dates/version
  │     └── active flag
  │
  ├── PayrollPayGroup
  │     ├── monthly schedule
  │     ├── legal entity
  │     ├── policy profile override
  │     └── effective dates/version
  │
  ├── SalaryTemplate
  │     ├── basic, HRA, LTA, other allowance, bonus
  │     ├── optional pay-group/department/type/grade scope
  │     └── effective dates/version
  │
  ├── EmployeePayrollAssignment
  │     ├── employee
  │     ├── pay group
  │     ├── salary template
  │     ├── policy profile override
  │     ├── effective dates
  │     └── approval state/approver
  │
  └── EmpPayRoll
        ├── calculated salary/deductions
        ├── lifecycle/payment state
        └── calculation snapshot identifiers
```

Every configuration entity carries `CompanyId`. Configuration API permissions are `PayrollSettings.View` and `PayrollSettings.Edit`; the controller derives the tenant and actor from authenticated context rather than accepting them from the client.

## 2. Actual CMS flow

### A. Configuration maintenance

`PayrollConfigurationController` exposes tenant-scoped endpoints for pay groups, employee assignments, policies, salary templates, bulk assignment preview/submission, and assignment approval.

`PayrollConfigurationService` validates:

- effective date ranges;
- employee ownership by company;
- pay-group, salary-template, and policy-profile ownership by company;
- active referenced records;
- overlapping employee assignments;
- optimistic version on edited pay groups.

New assignments are created unapproved. A separate approval call sets `IsApproved`, `ApprovedBy`, and `ApprovedAtUtc` using a company, employee, effective-date, and unapproved-state filter.

### B. Assignment resolution

`PayrollPolicyResolver.ResolveAssignment` selects only assignments matching:

- company;
- employee;
- payroll effective date;
- approved state.

When multiple approved assignments are effective, the newest `EffectiveFrom`, approval time, and ID are used as deterministic tie-breakers.

### C. Policy resolution precedence

For an employee and payroll month, the policy precedence is:

1. approved employee assignment’s `PolicyProfileId`;
2. active pay group’s `PolicyProfileId`;
3. newest active company policy effective for the date.

This is a sound override model, but the fallback means a company can still calculate payroll when an employee has no explicit assignment, provided an effective company policy exists. That behavior must be intentional and documented by HR/Finance.

### D. Automatic payroll generation

`AutoPayrollServices.GeneratePayrollForMonthAsync`:

1. canonicalizes the payroll month to the first day;
2. loads non-deleted employees in the tenant and filters employment eligibility;
3. stops regeneration if processed/paid/closed payroll already exists;
4. loads divisor policy, policy profiles, pay groups, and salary templates;
5. validates attendance approval/lock and unresolved attendance exceptions before writes;
6. resolves each employee’s approved assignment and effective policy;
7. resolves effective salary records and assignment-linked salary templates;
8. calculates joiner/exit/salary-revision proration, attendance LOP, EPF, ESIC, TDS, and bonus;
9. writes `EmpPayRoll` with configuration IDs and a calculation snapshot;
10. marks approved attendance exceptions as applied;
11. compensates prior mutations on failure.

The result is employee-row-centric. There is no authoritative `PayrollRun` record holding the complete population, totals, validation result, approval history, or reconciliation state.

### E. Payroll lifecycle

The current lifecycle is effectively:

```text
Generated → Approved → Processed → Paid → Closed
```

The service uses company/month/state predicates and post-update rereads for approval, processing, payment, and closing. Payment currently records a manual `PaymentReference`; it does not submit funds to a bank or provider.

### F. Payslip impact

Payslips read persisted payroll values and calculation output. The self-service path now requires both authenticated `UserId` and server-derived `CompanyId`. Payroll snapshots preserve the resolved assignment, pay group, template, policy profile, policy version, divisor, attendance summary version, and proration dates.

## 3. How each configuration item affects payroll

| Configuration | Direct effect | If changed | Current protection |
|---|---|---|---|
| Policy profile | divisor and attendance-lock behavior; policy version | changes LOP basis/eligibility controls for effective months | effective dates, active flag, snapshot IDs |
| Pay group | schedule and policy override | changes policy grouping and future resolution | tenant scope, effective dates |
| Salary template | recurring salary components and optional scope | changes selected salary components | tenant scope, active flag, effective dates |
| Employee assignment | employee-specific group/template/policy | changes employee calculation precedence | approval and effective-date resolution |
| Divisor policy | calendar/30-day/working-day basis | changes per-day LOP and proration amounts | effective policy record and snapshot |
| Attendance lock | blocks or permits automatic generation | controls whether attendance is final enough for payroll | approved+locked summary check |
| Salary record effective dates | daily salary used in proration | changes revision/joiner/exit calculations | effective-date coverage guard |
| Payroll state | controls whether recalculation/payment/closing is allowed | prevents destructive edits after publication | state predicates and conflict responses |

## 4. What is strong in the current design

- Tenant boundaries are repeated in configuration reads and writes.
- Assignment approval is separate from configuration entry.
- Effective dating is present on policies, groups, templates, assignments, and salary records.
- Automatic payroll snapshots the resolved configuration identity and versions.
- Attendance is treated as a payroll prerequisite when policy requires it.
- Processed/paid/closed states are guarded against unsafe regeneration.
- Payroll payment transitions are tenant-scoped and atomic at the row level.
- Rounding is now explicit and consistent for the covered automatic monetary calculations.

## 5. Current gaps compared with established HRMS payroll

### Payroll control and calendar

SAP Employee Central Payroll uses payroll areas/control records to group employees, determine payroll timing, release payroll for processing, and restrict master-data changes during controlled stages. SAP also describes a complete sequence of preparation, payroll execution, payment-data creation, payslips, and financial posting. [SAP payroll control record](https://help.sap.com/docs/successfactors-platform/implementing-business-rules-in-sap-successfactors/get-payroll-area-control-record) and [SAP payroll/post-payroll process](https://help.sap.com/docs/SAP_SUCCESSFACTORS_EMPLOYEE_CENTRAL_PAYROLL/427486e0f6ee4cd9a7edcae683af2c06/b3979f007f0f4663a1473c4c9550f93a.html)

CMS has pay groups and monthly dates, but not a first-class payroll calendar/control record or run aggregate.

### Pay components and salary structures

SAP models pay components and pay groups as reusable foundation objects. [SAP pay-related foundation objects](https://help.sap.com/docs/successfactors-employee-central/implementing-employee-central-core/payroll?locale=en-US.)

CMS has salary templates and payroll-row bonus/deduction fields, but not an immutable one-time component ledger with approval, taxability, source document, reversal, and idempotency.

### Bank and payment execution

Established payroll systems separate payroll calculation from payment execution. RazorpayX Payroll describes employee bank/IFSC onboarding, missing-information checks, payroll finalization, funding, execution authorization, and direct salary transfer. [Razorpay payroll execution checklist](https://razorpay.com/docs/x/xpayroll/employers/setup/execute-payroll/) and [Razorpay Payroll](https://razorpay.com/payroll/)

CMS currently records a payment reference after an external/manual payment. It does not create payment batches, verify beneficiaries, submit bank instructions, process callbacks, track UTRs, or reconcile provider results.

### Indian bank rails and beneficiary safety

For Indian NEFT/RTGS, RBI describes beneficiary details including name, account number, account type, bank/branch, and IFSC. RBI also directed beneficiary account-name lookup using account number and IFSC, with logs for lookups and related activity. [RBI NEFT/RTGS beneficiary-name lookup](https://www.rbi.org.in/scripts/NotificationUser.aspx?Id=12759) and [RBI RTGS FAQ](https://www.rbi.org.in/scripts/FS_FAQs.aspx?Id=65)

NPCI describes NACH-Credit as a bulk transaction route used for salary credits. [NPCI NACH](https://www.npci.org.in/what-we-do/nach/faqs)

CMS lacks the bank-account versioning, IFSC verification, batch submission, and settlement-status model required to use these rails safely.

## 6. Production-readiness assessment

| Area | CMS status | Assessment |
|---|---|---|
| Configuration tenant isolation | implemented and tested | strong |
| Assignment/effective dating | implemented and tested | usable with policy documentation |
| Salary calculation | implemented with row snapshots | needs run-level totals/control |
| Attendance gate | implemented | good row-level control |
| Payroll lifecycle | implemented at row/month level | incomplete without run aggregate and reopen/reversal policy |
| Bank payment | manual reference only | not bank-integrated |
| Statutory compliance | partial EPF/ESIC/TDS | not complete for multi-state production |
| Reconciliation | absent | production finance gap |
| Live transaction validation | unavailable | cannot claim full production proof |

## 7. Recommended target architecture

```text
Configuration
  → Pay calendar / PayrollRun
  → Employee assignment and bank-account snapshot
  → Validation gate
  → Calculation snapshot per employee
  → Approval and lock
  → PaymentBatch / PaymentItems
  → Bank or provider submission
  → Provider callbacks / UTR
  → Reconciliation
  → Payslip and accounting export
```

Recommended new aggregates:

- `PayrollRun`: company, pay period, population, version, totals, validation, approval, lock, correction state;
- `EmployeeBankAccount`: encrypted/tokenized account data, IFSC, verification, approval, effective date, version;
- `PayrollPaymentBatch`: run, provider, funding status, totals, idempotency key, approval;
- `PayrollPaymentItem`: payroll row, bank snapshot, amount, provider ID, UTR, status, failure/reversal data;
- `PayrollComponentLedger`: one-time earnings/deductions, source, approval, taxability, effective period, reversal link;
- statutory policy/version records for jurisdiction and financial year.

## 8. Priority actions

**P0 — before real bank payment:** add bank-account approval/versioning, payment batch/item states, idempotency keys, beneficiary verification, provider adapter, webhook handling, UTR storage, and reconciliation. Keep current manual `RecordPayrollPayment` as a controlled fallback only.

**P0 — before declaring payroll complete:** add a tenant-scoped `PayrollRun` aggregate and prevent partial/mixed runs from being treated as complete.

**P1 — compliance:** define jurisdiction/year/versioned rules for PT, EPF, ESIC, TDS, employer contributions, caps, exemptions, filing outputs, and rounding.

**P1 — corrections:** define arrears, salary revisions, reversal, reopen, replacement-run, and final-settlement interaction.

**P1 — controls:** add live MongoDB replica-set integration tests for concurrent generation, payment, rollback, tenant collision, and stale approval/close operations.

## Final assessment

The payroll configuration is necessary and it is connected to automatic payroll calculation through approved, effective-dated employee assignments, pay groups, salary templates, and policy profiles. It materially affects salary amounts, attendance eligibility, LOP, statutory deductions, proration, and the calculation snapshot.

It is a solid configuration foundation, but it is not equivalent to the payroll-control, bank-disbursement, statutory, and reconciliation architecture found in mature HRMS products. The current implementation should be described as **configuration-driven payroll calculation with controlled manual payment recording**, not as a complete bank-integrated production payroll system.