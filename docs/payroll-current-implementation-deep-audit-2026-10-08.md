# Codeji HRMS Payroll — Current Implementation Deep Audit

**Prepared:** 8 October 2026  
**Scope:** Employee salary onboarding/import, payroll configuration, calculation, approvals, monthly runs, payments, payslips, and associated API/UI flows in `CMS-Backend-Core` and `CMS-React`.  
**Review method:** Read-only source and documentation review; existing tests were inspected but not executed. No product code, migration, or configuration was changed.  
**Regulatory caveat:** Engineering assessment only—not legal, tax, or payroll advice. Obtain authorized payroll/legal approval for the actual jurisdiction and tax year before live use.

> Worktree note: both repositories contain extensive pre-existing uncommitted payroll changes. This report is additive and does not replace or modify those changes or earlier reports.

## Executive summary

The current implementation is a substantial MVP foundation: it has company-level policy, salary components, pay groups, templates, employee salary structures, dated versions, PDF/Excel import and review, salary approval, payroll runs with frozen input snapshots, attendance/LOP integration, payroll approval, payment batches, audits, and PDF payslips.

It is **not yet proven production-ready for live payroll**. The core risk is a mismatch between the flexible employee component model and the older fixed-field payroll/payslip model. Source inspection found confirmed inconsistencies in bonus recurrence, aggregate deduction import, Health Insurance classification, salary approval atomicity, tax-year/employee tax inputs, and approval-role enforcement. Some additional areas are design gaps or require integration/runtime proof; they are labeled accordingly below.

```text
Accepted offer / approved salary revision (dated, versioned components)
    + employee eligibility and payroll assignment
    + effective company/pay-group policy and tax-year rules
    + approved attendance and variable payroll inputs
        -> deterministic period calculation
        -> frozen, itemized result and audit evidence
        -> independent review and approval
        -> payment, payslip, accounting and compliance outputs
```

The key acceptance principle is amount traceability: an employee’s approved salary, component definitions, policy version, tax-year rules, attendance, one-time inputs, and rounding must yield the same itemized amounts in preview, calculation, payroll run, payslip, and payment.

## 1. Scope and evidence

Reviewed current paths in both repositories, including:

- Backend: `EmployeeSalaryStructureService`, `EmployeeSalaryPayrollMapper`, `EmployeeSalaryPdfImportParser`, `EmployeeSalaryImportService`, `AutoPayRollServices`, `PayrollCalculationRules`, `PayrollComponentCalculationEngine`, `PayrollRunService`, `PayrollRunCalculationService`, `PayrollRunWorkflowService`, `PayrollPaymentBatchService`, `PayRollServices`, `PayrollConfigurationController`, `SalarySlipTemplateModel`.
- Frontend: `UpdateEmployeeSalaryData.tsx`, `salaryImportMapping.ts`, `EmployeeSalaryImportPanel.tsx`, `PayrollSettings.tsx`, onboarding salary step.
- Existing tests: mapper, PDF parser, employee salary structure, payroll-run calculation, source resolver, import, controller permissions/contracts.

Direct evidence includes code behavior and existing test assertions. Test files were not run, so no current pass/fail claim is made. “Needs runtime proof” means an integration, concurrency, or browser test is needed before relying on the behavior.

## 2. Existing design and actual flow

### Company setup

The payroll settings area separates statutory/calculation policy, component catalog, pay groups, salary templates, and employee assignments. Policy profiles are dated and have draft/submit/approve stages. Policy approval is protected by `AdminOnly` at its controller endpoint. Related setup uses payroll-settings module permissions.

The concepts should remain distinct:

- A **pay group** sets a processing cohort/schedule and defaults such as policy context.
- A **salary template** is a reusable starting package.
- An **employee salary structure** is the employee’s actual compensation agreement.
- A **statutory profile** defines applicable calculation rules for a jurisdiction and effective period.

### Onboarding and salary changes

Employee onboarding supports a company component catalog, template selection, individual amounts, effective date, a calculation preview, and PDF/Excel import. The backend validates tenant employee ownership, component definitions, duplicate codes, template/pay-group compatibility and dates, salary overlaps, formula validity, and monthly cadence. Salary saves as a draft, then can be submitted, independently approved, and retrieved as history.

Templates can scale onboarding, but individual employees remain free to have different packages. Salary changes should create approved versions; they should not mutate the offer or already paid payroll history.

### Payroll execution

The newer payroll run captures employee salary revisions, policy, assignment, pay group, and template in a period snapshot. Calculation validates those frozen references against a generated legacy `EmpPayRoll` row and computes run totals from that row’s fixed fields. Existing tests explicitly codify snapshot mismatch blocking and using the actual payroll row for totals.

The legacy monthly generator resolves salary by effective date and combines salary with employment dates, attendance, LOP, statutory deductions, and annualized TDS. Payroll workflow and payment-batch services support review/approval, processing/closing, payment submission, and reconciliation.

### Payslip

The PDF payslip is built from a processed payroll row. Its DTO contains fixed fields such as Basic, HRA, LTA, Other Allowance, Bonus, Health Insurance, EPF, ESIC, TDS, and a single Other Deductions total. That fixed DTO cannot faithfully display arbitrary tenant-defined component names.

## 3. Confirmed findings

Severity is an engineering estimate: **P0** release stop; **P1** material incorrect-pay/compliance/authorization risk; **P2** significant workflow/data fidelity gap; **P3** improvement. This is not a legal classification.

### P1 — Recurring and one-time Bonus behavior conflicts

The onboarding component builder sets standard `BONUS` to `isRecurring: false`. The component calculation engine includes recurring earnings in gross. However, `EmployeeSalaryPayrollMapper` explicitly includes a `BONUS` component in recurring earnings even when `IsRecurring` is false. The current mapper test passes a non-recurring bonus and asserts that it remains in `GrossSalary`. Payroll run’s primary component list, meanwhile, filters on recurring status.

**Impact:** The form’s monthly gross can exclude the bonus while the legacy payroll mapper includes it; the snapshot can omit an item included in the paid row. This makes gross, tax/statutory bases, and employee expectations inconsistent.

**Recommended correction:** Do not infer recurrence from the code `BONUS`. Recurring bonus is a recurring salary component. A one-time bonus belongs in an approved payroll-period input with pay month, source, tax treatment, and audit trail. Require parity across preview, saved structure, generated row, snapshot, and payslip.

### P1 — PDF import treats aggregate Total Deductions as another deduction

The PDF parser requires Health Insurance and Total Deductions and emits both as recurring deduction components. Frontend mapping also carries a fixed `TOTAL_DEDUCTIONS` code. The mapper separately sums recurring deduction components and the Health Insurance value.

**Impact:** If “Total Deductions” already includes Health Insurance or other itemized amounts, payroll can deduct those twice. A company component catalog that does not contain `TOTAL_DEDUCTIONS` can also reject the imported structure.

**Recommended correction:** Import itemized deduction lines only; calculate their sum and compare it with the document’s total as a validation checksum. An aggregate-only document amount should be review-only and must not enter payroll as a component until HR maps it to actual items.

### P1 — Health Insurance `Kind` is ignored in the payroll mapper

The mapper always excludes `HEALTH_INSURANCE` from earnings and adds its amount as a deduction irrespective of the canonical component’s configured `Kind`.

**Impact:** If a tenant uses that code for an earning/benefit, gross omits it and net is reduced.

**Recommended correction:** Preserve component kind and employee/employer/statutory flags for all codes. If the legacy payroll shape cannot represent a valid kind, reject it explicitly. Add tests for each allowed kind.

### P1 — Salary approval is not atomic across prior and new salary rows

The approval service closes the previous open approved revision first, then approves the submitted revision. On second-write failure it tries to reopen the old row, but does not check whether that compensating write succeeded. The close operation does not include the prior record’s expected version/status in its filter.

**Impact:** A persistence failure or concurrent approval can leave an effective-date gap or stale salary timeline.

**Recommended correction:** Use a Mongo transaction/session where supported, or a recoverable, idempotent transition protocol. Apply optimistic concurrency to both records and test races plus failures at each write boundary.

### P1 — TDS is not a complete employee-specific, tax-year-versioned engine

`PayrollCalculationRules` hard-codes regime slabs, standard deductions, rebate threshold, and cess. Its inputs are annual gross and regime mode; it does not receive a tax-year version, employee investment declarations/proofs, exemptions, prior-employer income/TDS, perquisites, or a complete reviewed tax worksheet. Its code comment still refers to Section 115BAC. The Income Tax Department’s transition FAQ says salary for Tax Year 2026–27 is under the Income-tax Act, 2025, Section 392(1), and should reset from 1 April 2026. That establishes a need for tax-year versioning; it does not prove every numeric constant currently in code is wrong.

**Impact:** Tax-year rules can change without changing the selected rule version. Employee-level regime/deduction inputs cannot be fully represented, risking under/over-withholding.

**Recommended correction:** Version tax rules by jurisdiction and tax year. Capture employee regime choice, declarations/proofs, prior employer data, perquisites, eligible deductions/exemptions and YTD tax. Generate an employee-readable worksheet, keep external/manual calculation explicit, and require authorized payroll/legal approval before activating a rule pack.

### P1 — Admin-only approval is inconsistent across payroll entities

The payroll policy approval endpoint has `AdminOnly`. Employee salary approval uses `Permission.Approve` without `AdminOnly`; the service blocks creator/submitter self-approval but does not require the approver to be an admin. Payroll-run approval similarly uses an approval permission.

**Impact:** If the intended rule is “only company admin approves,” a non-admin role granted `Approve` can approve a salary or run.

**Recommended correction:** Define approvers separately for policies, salaries, assignments, and runs. Enforce each policy server-side, not just by hiding UI actions. Test admin, payroll admin, HR, submitter, employee, and cross-tenant identities.

### P2 — Custom component names are lost in legacy payroll/payslips

Custom recurring earnings are folded into `OtherAllowances`. Payslip DTO is fixed-field; custom deductions are aggregated into one Other Deductions value.

**Impact:** Totals may be right while the employee cannot see named items such as Meal, Travel, or Loan Repayment. Reviewers cannot reconcile an item to its source definition or tax treatment.

**Recommended correction:** Keep named, versioned earning/deduction/employer contribution/reimbursement lines from salary structure through payroll result, run review, export, and payslip. Include stable component code, label, amount, treatment, source, and formula basis.

### P2 — Negative net is silently clamped to zero

Mapper, component engine, and payroll-run calculation use `Math.Max(0, gross - deductions)`.

**Impact:** Excess deductions or a recovery balance disappear; zero payout can look like a resolved calculation.

**Recommended correction:** Preserve signed net before payment and apply an explicit policy: block for review, cap/carry a disclosed recovery balance, or allow negative net only with documented approval. Show gross, deductions, balance and actual payment separately.

### P2 — Annual CTC and monthly gross are not reconciled

Salary draft recalculates `MonthlyGross` while retaining submitted `AnnualCtc` independently. The PDF parser sets annual CTC to gross × 12 rather than extracting a stated offer CTC. The legacy mapper has a similar fallback. Employer contributions, benefits, and variable compensation may form part of CTC but not gross.

**Impact:** Offer CTC, structure CTC, monthly gross, employer cost, and take-home can disagree or be conflated.

**Recommended correction:** Define and separately show annual CTC, fixed annual gross, variable target, monthly gross, employer cost, estimated net, and actual net. Reconcile CTC under a named convention and flag mismatches.

### P2 — Effective date does not specify arrears payout month

Salary versions are effective-dated, but the employee salary revision flow does not model payout month as a separate arrears decision.

**Impact:** A late-approved backdated change cannot clearly choose whether to prorate an open period, pay arrears later, or apply prospectively.

**Recommended correction:** Effective-from means when entitlement starts; payout month means when arrears are disbursed. Calculate arrears by prior period/component through a new approved adjustment/run; do not edit paid history.

### P2 — Payroll run validates a separate legacy calculation result

Payroll-run calculation validates frozen inputs against precomputed `EmpPayRoll` snapshots, then derives totals from the legacy fixed fields. Tests intentionally assert this behavior.

**Impact:** This is a useful guard, but two calculation pathways can drift. Snapshot checks must include all amount-affecting input values/versions, not only record IDs.

**Recommended correction:** Prefer calculation directly from the frozen run snapshot. If the legacy row remains authoritative, compute a canonical input/output hash and compare itemized lines and totals; block every mismatch. Document the sequence so runs cannot use stale rows.

### P2 — PDF extraction is fixed-label and text-based

The parser supports a fixed set of labels and text-readable PDFs, not arbitrary/scanned offer letter layouts.

**Impact:** Alternate labels, columns, annual/monthly ambiguity, CTC, employer PF, gratuity, variable bonus, and currency formats can be missed or misassigned.

**Recommended correction:** Limit the parser to a published supported template, or show source evidence and confidence for each extracted value and require HR review/reconciliation. Use Excel/CSV with deterministic column mapping for bulk import.

### P2 — Only monthly cadence is supported

Backend salary and pay group validations accept monthly cadence only. This is a valid MVP boundary if disclosed, but not a general multi-frequency payroll engine.

**Recommended correction:** Keep unsupported frequencies rejected server-side; treat weekly/biweekly/semimonthly as separate calendar, proration, tax/YTD, attendance and idempotency work.

### P3 — Invalid identifiers are silently substituted

The mapper creates a random `SalaryId` for a non-GUID source ID and uses `Guid.Empty` for invalid `UserId`.

**Impact/correction:** Source traceability weakens and bad identity can flow into payroll. Validate or retain canonical string IDs; do not use random/empty fallbacks.

## 4. Important capabilities to close

### Compensation and employee lifecycle

- Component types: earning, employee deduction, employer contribution, reimbursement, taxable benefit/perquisite, informational item.
- Component semantics: recurring/one-time, fixed/variable/formula, CTC/gross/net/tax/statutory bases, employer/employee liability, rounding, caps/floors, proration, effective dates, payslip visibility, override controls, accounting mapping.
- Eligibility by work location/state, establishment, employment type, start/exit dates, and statutory membership/evidence.
- Onboarding should create a salary draft from an accepted offer; a template pre-fills but does not dictate the individual’s package. Keep offer version, document hash, parser version, extracted evidence, HR corrections, and approval provenance.
- Bulk workflow for 1,000+ employees: sample schema, column mapping, dry-run, row-level warnings/errors, idempotent submit, retry/partial failure report, bulk revisions, cohorts, exception queue.
- Future raises, grade/location/type changes, backdated corrections, cancellation/supersession, rehire, exit/final settlement, and overlap prevention.

### Payroll operations

- Readiness gate for approved policy, locked attendance/leave, unresolved exceptions, missing bank/tax/statutory data, salary coverage, supported cadence, and anomalous net.
- Distinct draft/calculated/validated/submitted/approved/processed/paid/reconciled/closed states and audited reopen/reversal.
- Separate normal, off-cycle, one-time, arrears, bonus, final settlement, correction/reversal, and payment retry. Idempotency by company/period/type/population; concurrency protection.
- Variance view for headcount and month-over-month totals; zero/negative net, manual changes, statutory totals, held employees, funding amount.
- Bank file/advice, transfer references, partial/failing payments, safe retries, employee-level reconciliation.
- Itemized payslips, payroll register, statutory reports, tax worksheets/certificates, journals/cost centers, audit exports; prior YTD payroll import for mid-year go-live.

### Platform controls

- Tenant scope every read/write/index/approval/import/payment/report/payslip download; test two tenants with overlapping employee IDs.
- Optimistic concurrency, atomic/recoverable approval, idempotent import/run/payment; immutable approved/paid results, adjustment/reversal instead of silent history edits.
- Least privilege/encryption for PAN, bank, salary/tax and offer documents; retention, access audit, backup, recovery, migration rehearsal, monitoring and runbook.

## 5. Recommended end-to-end behavior

1. **Configure company:** jurisdiction/establishment, tax year, cadence/cutoff/pay date, divisor, currency; version components/statutory rules; create groups/templates; approve dated policy.
2. **Onboard:** accepted offer creates a salary draft. Apply template defaults, permit employee-specific values, review PDF/Excel import, reconcile CTC/gross/employer cost/net estimate, submit for independent approval, then create immutable salary revision.
3. **Run payroll:** freeze dated salary/component/policy/assignment versions; include approved attendance and one-time inputs; calculate itemized lines; validate, review, approve, then use the same frozen result for payment and payslip.
4. **Change salary:** effective-from is the entitlement start; effective-to is the last covered date (blank until superseded). A new approved revision/version signals a change. If prior payroll is paid, pay arrears through a later approved adjustment, not by editing history.

## 6. Benchmark and statutory research

Product documentation shows patterns, not legal requirements:

| Pattern | Reference | Implication for Codeji |
|---|---|---|
| Fixed/variable/scheduled one-time earnings | Zoho India [Salary Components](https://www.zoho.com/in/payroll/help/employer/settings/set-salary-components.html) | Recurrence and payout month must be explicit, not inferred from code. |
| Salary revision approval/arrears | Zoho [Salary Revisions](https://www.zoho.com/in/payroll/help/employer/approvals/salary-revision.html), [Arrears](https://www.zoho.com/in/payroll/kb/employer/approvals/arrear-component.html) | Keep entitlement date separate from payout month. |
| Bulk employee salary import | Zoho [Data Import](https://www.zoho.com/in/payroll/help/employer/import.html) | Use field mapping, validation, preview and row-level results. |
| Run review and approval | Zoho [Pay Runs](https://www.zoho.com/in/payroll/help/employer/pay-runs/) | Separate calculation, review, approval and payment. |
| Employee transparency | Zoho [Employee Portal](https://www.zoho.com/in/payroll/help/employee/basics/) | Expose salary structure, payslips, annual earnings, contributions and tax worksheet. |
| Flexible approval routes | Zoho [Approval Workflows](https://www.zoho.com/in/payroll/help/employer/settings/configure-approvals.html) | Enforce clear admin/delegate rules server-side. |

Official Indian sources to re-check for the employer and effective period:

- [EPFO contribution reference](https://www.epfindia.gov.in/site_docs/PDFs/MiscPDFs/ContributionRate.pdf) discusses contribution rates and higher-wage cases.
- [ESIC FAQ](https://www.esic.gov.in/attachments/files/faq.pdf) and [ESIC contribution publication](https://esic.gov.in/attachments/esistatedirectoratefile/ef84365f31f15306ddb397902d675858.pdf) provide coverage/rate context; rules must be effective-dated and applicability confirmed.
- Income Tax Department [TDS transition FAQ](https://www.incometax.gov.in/iec/foportal/help/all-topics/e-filing-services/%20tds%20compliance-faq) says Tax Year 2026–27 salary uses the Income-tax Act, 2025, Section 392(1), with a reset from 1 April 2026.

Check current notifications, state/professional-tax rules, establishment-specific applicability, and obtain authorized payroll/legal sign-off. These links do not replace that approval.

## 7. Prioritized implementation plan

### Sprint 0 — Establish baseline

- Record active branch/commit; preserve dirty worktree.
- Run backend service/API tests and frontend tests/typecheck/build; report actual totals and failures.
- Do not run production migrations. Use disposable local replica-set Mongo for migration/transaction tests.

### Sprint 1 — Correct pay amounts

1. Align Bonus recurrence in UI, formulas, mapper, generator, snapshots, tests; one-time bonus becomes period input.
2. Remove `TOTAL_DEDUCTIONS` as a component; reconcile imported total to itemized lines.
3. Preserve Health Insurance component kind.
4. Make negative net an explicit hold/recovery exception.
5. Reconcile CTC/gross/employer cost and validate IDs.

**Exit:** golden fixtures prove onboarding preview = saved salary = payroll lines = run totals = payslip for canonical and legacy sources.

### Sprint 2 — Approval, concurrency and tenant controls

1. Make salary revision approval atomic/recoverable and concurrency-safe.
2. Decide admin-only vs delegated approvals and enforce on server.
3. Test idempotency/retries for imports, assignments, approvals, canceled runs, payments; verify audit and two-tenant isolation.

### Sprint 3 — One calculation path and itemized results

1. Calculate from frozen run input or implement full input/output hashes and parity checks.
2. Carry named lines through payroll, run UI, exports, payslips and payment register.
3. Add one-time/variable pay, reimbursements, loans, arrears/corrections, joiner/leaver and final settlement cases.

### Sprint 4 — Tax/statutory controls

1. Version tax by year; store declarations, prior-employer income and YTD.
2. Model jurisdiction/location/eligibility/wage bases/limits/rates/rounding/effective dates.
3. Require authorized rule sign-off and test vectors around thresholds.
4. Add compliance outputs and year-end reconciliation.

### Sprint 5 — Operations and UX

1. Readiness dashboard, variance review, exception queue, run timeline, approver inbox.
2. Reviewable PDF/Excel imports and bulk operations.
3. Responsive/accessibility review of onboarding, imports, run review, payments and payslips.
4. Staging/UAT, two-tenant E2E, migration/backup/restore, failed payment and recovery rehearsal.

## 8. Required verification matrix

- **Unit:** formulas/cycles/missing bases/rounding; recurrence and kinds; negative net; CTC reconciliation; tax-year/regime/YTD/thresholds; effective-date boundaries; import format/currency/aggregate deductions/retry.
- **Integration/regression:** onboarding draft→approval→payroll; stale/concurrent approval and injected partial failure; preview-to-payslip parity; attendance/LOP exactly once; salary revision and arrears; duplicate runs; canceled-run replacement; partial-payment retry/reconcile; tenant isolation.
- **Manual UAT:** valid/invalid/duplicate Excel rows; PDF reconciliation; mid-month salary change and arrears after paid period; attendance exception; run approval→payslip→payment; role/tenant checks.
- Use synthetic employees and local/staging data. Record expected/actual line items, run IDs, policy/salary versions, and test counts. None of these checks were executed for this report.

## 9. Readiness decision

**Assessment: substantial MVP foundation; not proven production-ready.** Do not use for live payroll until the P1 inconsistencies, approval integrity, tax-year rule versioning, and itemized amount parity are corrected and validated. Obtain authorized payroll/legal approval; a UI “ready” state is not a substitute.

---

## 10. October 2026 Addendum: Industry Benchmark & The 12 Logical Gaps

### 10.1 Industry Benchmark: Codeji HRMS vs. Keka, Zoho Payroll & GreytHR

| Capability Dimension | Keka HR | Zoho Payroll | GreytHR | Codeji HRMS (Current) | Codeji HRMS (Target MVP) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Startup 1-Click Payroll** | Supported (Quick Run mode) | Supported (Express Run) | Supported | ❌ Blocked (Hard attendance lock & missing row checks) |  1-Click Agile Mode (Optional attendance lock, defaults to 100% pay) |
| **Attendance Enforcement** | Configurable per Pay Group | Configurable per Pay Group | Configurable per Employee | ⚠️ Global Policy Gate (Aborts company run if 1 employee is unlocked) |  3-Tier Cascading Toggle (Company -> Pay Group -> Employee Override) |
| **Employee Tax Regime Choice** | Employee Portal / HR Admin | Employee Portal / HR Admin | Employee Portal / HR Admin | ❌ Policy Profile Level Only (Forces profile duplication) |  Stored on Employee Structure (New vs Old vs Manual per employee) |
| **Statutory EPF Flexibility** | Capped / Actual / Exempt per employee | Capped / Actual / Exempt per employee | Capped / Actual / Exempt per employee | ⚠️ Policy Profile Level Only (Cannot exempt single employee on record) |  Employee Structure Override (EpfModeOverride: "EXEMPT") |
| **Non-Taxable Earnings (TDS)** | Excluded from tax gross | Excluded from tax gross | Excluded from tax gross | ❌ Hard Blocker (Aborts payroll if non-taxable earning exists) |  Deduct from Tax Base (Never block calculation) |
| **Payslip Itemization** | 100% Dynamic custom lines | 100% Dynamic custom lines | Dynamic lines | ⚠️ Fixed DTO (Collapses custom lines into "Other Allowances") |  Itemized Dynamic Lines (Displays exact custom component names) |
| **Bank Payout Files** | 50+ Bank CSV/TXT formats | 40+ Bank Formats | Standard Bank Formats | ❌ Missing (Only stores text payment references) |  Standard Bank Transfer CSV Export (Beneficiary, IFSC, Account, Amount) |
| **Full & Final (F&F) Settlement** | Integrated with last pay | Integrated with last pay | Integrated with last pay | ⚠️ Double-Counting Risk (Does not check monthly PaymentStatus) |  Reconciled F&F Pipeline (Prevents duplicate payout) |

### 10.2 Catalog of 12 Confirmed Logical Issues, Bugs & Gaps

1. **Circular Run Calculation Deadlock (`PayrollRunCalculationService.cs#L80-L87`)**: `CalculateRunAsync` searches for pre-existing `EmpPayRoll` records and fails with `PAYROLL_ROW_MISSING`. Must compute directly from approved salary structures and attendance.
2. **Dual-Stacked UI Conflict (`PayRollManagement.tsx#L1-L120`)**: Legacy `PayRollTable` and modern `PayrollRunsPanel` are rendered vertically stacked on the same page, presenting conflicting action buttons and workflow states.
3. **Rigid Attendance Lock Blocker (`AutoPayRollServices.cs#L194-L198`)**: Throws validation errors and rolls back the company payroll run if an attendance summary is missing or unlocked, blocking startups without biometric devices.
4. **Non-Taxable Component TDS Blocker (`PayrollCalculationRules.cs#L14-L24`)**: `FindUnsupportedAutomaticTdsTreatment` rejects any employee who has non-taxable earnings instead of simply excluding them from taxable gross.
5. **Missing Employee-Level Overrides (`EmployeeSalaryStructure.cs`)**: Tax Regime, EPF Mode, and Attendance Tracking Mode are stored only at policy profile level, not per employee.
6. **Bonus Recurrence Inconsistency (`EmployeeSalaryPayrollMapper.cs#L40`)**: Forces `BONUS` into monthly recurring gross even when `IsRecurring = false`.
7. **Silent Negative Net Clamping (`AutoPayRollServices.cs` & `PayrollComponentCalculationEngine.cs`)**: Deductions exceeding gross clamp net pay to 0 without carrying forward a recovery balance.
8. **F&F Settlement Double-Payout Vulnerability (`EmployeeService.cs#L3623-L3660`)**: Settlement pulls pending monthly salary without verifying whether monthly payroll has already been disbursed.
9. **Fixed-Field Payslip DTO (`SalarySlipTemplateModel.cs`)**: Custom earnings and deductions are lumped into generic `OtherAllowance` rows on PDF payslips.
10. **Hardcoded Currency Formatting (`PayRollServices.cs#L124`)**: Hardcoded `IndianCurrencyWords.ToWords(...)` prints "Rupees" even if the company pays in USD, EUR, or AED.
11. **Missing Bank NEFT/RTGS CSV Export (`PayrollPaymentBatchService.cs`)**: Payment batches cannot export standard banking CSV files for disbursement.
12. **Legacy Approval Authorization Leak (`PayRollController.cs#L175`)**: `ApprovePayrollMonth` has `ModulePermission` but lacks `Authorize(Policy = "AdminOnly")`.

