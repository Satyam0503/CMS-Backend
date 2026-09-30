# Payroll remediation report

Updated: 2026-09-29  
Project: CMS backend, local checkout `D:\CMS\CMS-Backend-Core`  
Branch: `integration/current-work`

## PAY-P0-001 — duplicate payroll processing and idempotency (P0)

**Status: ◻ IN PROGRESS — green check withheld**

### Observed flow and root cause

The React payroll page calls `POST api/PayRoll/ProcessPayrollMonth`. The controller requires authentication and `PayRoll` Create/Edit permission, derives the company and actor from the current request, and calls `PayRollServices.ProcessPayrollMonth`. The service checks month/readiness, then uses a MongoDB `UpdateMany` scoped to company, month, and `IsProcessed != true`.

A second concurrent request can read the same drafts before the first request writes. Its conditional update then changes zero rows and the repository returns a failure. The API reported a processing failure even when the first request had completed the month. Further, `MongoRepository.UpdateMany` reports success when *any* row changes (`ModifiedCount >= 1`), so a partially updated month could previously be reported as complete. The existing unique `(CompanyId, EmployeeId, PayMonth)` index controls duplicate payroll rows only if its migration has succeeded in the target database; it does not make a multi-row month publication transactional.

### Implementation in this remediation

- Moved the retry logic into `ProcessPayrollMonth`; an earlier draft had mistakenly placed it in `ClosePayrollMonth`. Close-month behavior is restored.
- An already processed company/month now returns the existing idempotent result before repeating readiness checks or writing.
- After `UpdateMany`, the service re-reads the same tenant/month. When all rows are processed, a zero-match concurrent loser receives HTTP 200 with an “already processed” message.
- A mixed or missing month returns HTTP 409 instead of a false success. A Mongo server error remains a server error when the month is still incomplete.
- No frontend code or payroll calculation rules changed.

### Files changed for PAY-P0-001

1. `Codeji.CMS.Services/PayRoll/PayRollServices.cs` — `ProcessPayrollMonth` idempotency and whole-month postcondition.
2. `Codeji.CMS.Services.Tests/PayRollProcessingIdempotencyTests.cs` — six targeted tests.
3. `docs/test-reports/PAYROLL-REMEDIATION-REPORT.md` — this living report.

The backend and frontend checkouts had extensive pre-existing changes. Those changes were preserved. No commit, push, migration execution, or deployment was performed.

### Verification results

| Gate | Result | Evidence |
| --- | --- | --- |
| Targeted idempotency/concurrency tests | **6 passed, 0 failed** | Sequential retry, two callers released together, losing concurrent request, tenant scope, mixed month, Mongo failure |
| Full services regression | **393 passed, 0 failed, 0 skipped** | `Codeji.CMS.Services.Tests` |
| Full API regression | **99 passed, 0 failed, 0 skipped** | `Codeji.CMS.API.Tests` |
| Payroll UI component tests | **6 passed, 0 failed** across 2 files | Vitest payroll module |
| Frontend typecheck | **Passed** | `npm run typecheck` |
| Backend solution build | **Passed, 0 errors, 25 warnings** | `CodejiCMSCore.sln`; warnings are in other existing API code |
| Source authorization/tenant review | **Passed for this path** | Controller requires PayRoll Create/Edit; actor/company come from current context; update and re-read include `CompanyId` |
| Real MongoDB concurrent requests | **Not run** | No local `mongod`, `mongosh`, Docker, or Mongo service was available |
| Multi-row transaction/rollback | **Not passed** | MongoDB `UpdateMany` is not an all-or-nothing month transaction; a partial write can remain after HTTP 409 |
| Deployed migration/index check | **Not run** | Target database and safe test tenant were not available |
| Authenticated browser smoke | **Not run** | No safe test account/database was provided; frontend code was unchanged |

### Remaining risk and completion gate

This fix improves retry behavior and prevents a false “month complete” response. It does **not** yet guarantee that an interrupted multi-row update cannot expose some employees as processed while others remain drafts. Payroll draft generation also uses compensating rollback across employee rows; simultaneous generation was traced but not validated against a real database.

Before marking PAY-P0-001 **✓ VERIFIED FIXED**, run two authorized HR requests simultaneously against an isolated MongoDB replica-set test tenant, verify one month publication and one audit transition per employee, inject an update failure and prove no partial payslip publication, verify cross-tenant isolation and the unique index, then rerun backend/frontend regression. If the failure injection confirms partial publication, add a transaction or durable month-level publication boundary and retest. The green check remains withheld until those gates pass.

## PAY-P0-002 — payroll lifecycle state vs `IsProcessed` (P0)

**Status: ◻ IN PROGRESS — green check withheld pending live MongoDB validation**

### Reproduced inconsistency and root cause

`EmpPayRoll` stores both the legacy `IsProcessed` flag and lifecycle fields `WorkflowStatus`, `ApprovalStatus`, and `PaymentStatus`. The entity defaults `WorkflowStatus` to `DRAFT`, including when an older MongoDB document has no workflow field. A row with `IsProcessed == true` could therefore be shown as `DRAFT`. The inverse mismatch was also unsafe: status-marked processed payroll could be omitted from reads/payslips, edited, regenerated, recalculated, or processed again when `IsProcessed` was false.

### Implementation

- Added a single effective-state projection with precedence `CLOSED` → `PAID` → `PROCESSED` → `APPROVED` → `DRAFT`. The DTO now returns consistent `IsProcessed`/`WorkflowStatus` and includes the payment/closure fields already defined in the DTO.
- Processed-only payroll reads and payslip selection now include processed/paid/closed lifecycle markers while retaining company, employee, and viewer scoping.
- Manual edit, payroll upload/validation, month processing, approval, payment, and month close now recognize lifecycle markers as processed. Conditional Mongo updates include effective-state filters; payment and close transitions also synchronize `IsProcessed = true`.
- Automatic generation now blocks lifecycle-processed months, counts lifecycle-processed prior periods for year-to-date calculations, and prefers those snapshots when selecting a duplicate record.
- Payment close rules remain strict: a paid workflow marker alone does not satisfy the requirement for a recorded payment status.
- No payroll calculation rule, permission, tenant filter, frontend source, or schema migration was changed.

### Files changed for PAY-P0-002

1. `Codeji.CMS.Services/PayRoll/PayRollServices.cs` — effective status projection and all manual/read/payment/month lifecycle guards.
2. `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs` — regeneration, YTD, and snapshot selection state guards.
3. `Codeji.CMS.Services.Tests/PayrollLifecycleReadModelTests.cs` — legacy and lifecycle-only states, DTO mapping, processed-only query, and cross-company exclusion.
4. `Codeji.CMS.Services.Tests/PayrollProcessedGuardRegressionTests.cs` — edit rejection when workflow is processed but the legacy flag is false.
5. `Codeji.CMS.Services.Tests/PayRollProcessingIdempotencyTests.cs` — no repeated processing and rendered conditional-filter state guards.
6. `Codeji.CMS.Services.Tests/PayrollPaymentTests.cs` — lifecycle-only payment eligibility and legacy-flag synchronization.
7. `Codeji.CMS.Services.Tests/AutoPayrollLifecycleStateTests.cs` — automatic regeneration blocked for status-only processed payroll.
8. `docs/test-reports/PAYROLL-REMEDIATION-REPORT.md` — this living report.

### Verification results

| Gate | Result | Evidence |
| --- | --- | --- |
| Targeted lifecycle/read/edit/payment/processing/automatic tests | **29 passed, 0 failed** | Five focused test groups |
| Full backend services regression | **401 passed, 0 failed, 0 skipped** | `Codeji.CMS.Services.Tests`; compiles changed backend services |
| API regression | **99 passed, 0 failed, 0 skipped** | Existing API test binary (`--no-build`); API source unchanged |
| API rebuild | **Blocked by running application file lock** | `Codeji.CMS.API` PID 16976 held `Codeji.CMS.Services.dll`; process was left running |
| Payroll frontend tests | **6 passed, 0 failed** | Payroll history/payment modal tests (frontend source unchanged) |
| Frontend typecheck and production build | **Passed** | `npm run typecheck`; `npm run build` |
| Tenant isolation | **Passed at service/query test level** | Same employee identifier in another company is excluded; test verifies current company row only |
| MongoDB execution / integration | **Not run** | No isolated local MongoDB/test tenant; regex and expression filters were compiled/rendered in unit tests but not executed against MongoDB |

### Remaining risk and completion gate

The code and unit-level conditional filters now reconcile the lifecycle state across payroll consumers, but no MongoDB-backed run was available to validate actual query translation, updates, or legacy-document behavior against the deployed serializer/index configuration. No migration/backfill was run. The check remains withheld until an isolated MongoDB integration test validates status-only reads, payment, month processing/closing, automatic-generation protection, and tenant separation. PAY-P0-001 remains **◻ IN PROGRESS**; its multi-row transaction/idempotency risk is unchanged.
## PAY-P0-003 — payroll ↔ F&F transaction integrity (P0)

**Status: ◻ IN PROGRESS — green check withheld pending safe migration and replica-set integration validation**

### Reproduced risk and root cause

Final settlement calculation, approval, and marking paid each changed `EmployeeFinalSettlement` and then separately changed the associated `EmployeeExit` version/audit history. If the second write failed its optimistic version check, MongoDB could retain the first write alone. Concurrent calculations also had a first-write race: both callers could observe no settlement and insert separate records for the same tenant/exit. The payment-update endpoint changed only the settlement document and did not participate in the exit audit/version transition.

The path remains tenant/permission scoped: each operation derives `CompanyId` and actor from request context, requires the HR exit permission, and reads the exit/settlement by company and exit identity. The existing business rule allowing calculation from a draft final-month payroll is preserved.

### Implementation

- Calculation, approval, and marking paid now write the settlement and exit audit/version snapshot together through MongoDB `WithTransactionAsync`, using company/exit/version/deletion conditional filters. Failed optimistic writes return HTTP 409; transaction-unavailable environments fail closed with HTTP 503 rather than allowing a partial pair.
- Settlement updates include optimistic settlement version/status checks. A stale payment update now returns conflict.
- New settlements receive a deterministic ID derived from company and exit IDs. A migration adds a unique `(CompanyId, EmployeeExitId)` index for active settlement records to enforce the one-settlement-per-exit invariant across concurrent writers.
- Draft payroll remains eligible for final settlement calculation; no payroll calculation or approval rule was changed.

### Files changed for PAY-P0-003

1. `Codeji.CMS.Services/Employees/EmployeeService.cs` — transactional calculate/approve/pay transitions; tenant/version CAS filters; payment concurrency check.
2. `Codeji.CMS.Services.Tests/UnitTest1.cs` — success and stale-exit conflict tests using transaction mocks.
3. `Codeji.CMS.Migrations/Migrations/AddEmployeeFinalSettlementIntegrityIndex.cs` — active per-tenant/per-exit unique index.
4. `docs/test-reports/PAYROLL-REMEDIATION-REPORT.md` — this living report.

### Verification results

| Gate | Result | Evidence |
| --- | --- | --- |
| Targeted final-settlement tests | **2 passed, 0 failed** | Existing draft-payroll calculation path and exit-version conflict response |
| Full backend services regression | **402 passed, 0 failed, 0 skipped** | `Codeji.CMS.Services.Tests` |
| Backend API build | **Passed, 0 errors, 25 existing warnings** | `Codeji.CMS.API.csproj` |
| Migration build | **Passed, 0 errors, 20 existing warnings** | `Codeji.CMS.Migrations.csproj`; migration not executed |
| Tenant and authorization source review | **Passed by inspection** | HR exit permission check plus tenant-scoped exit/settlement reads and transaction filters include `CompanyId` |
| Transaction/rollback against real Mongo replica set | **Not run** | No isolated Mongo replica-set fixture/test tenant available; mock tests are not database integration proof |
| Unique-index creation and existing-data compatibility | **Not run** | Migration not executed; any pre-existing active duplicates must be identified/resolved before unique-index creation |
| API regression | **99 passed, 0 failed, 0 skipped** | `Codeji.CMS.API.Tests` (`--no-build`) |
| Frontend checks | **Not rerun** | No frontend code changed for this issue |

### Remaining risk and completion gate

Do not deploy the migration until an authorized read-only duplicate scan is performed and any active duplicate `(CompanyId, EmployeeExitId)` settlement rows are reviewed and reconciled; the migration intentionally does not guess which financial record to retain. Then run the migration in a controlled environment and execute integration tests on a replica set covering insert/update transaction rollback, retry, concurrent first calculation, approval/payment transitions, duplicate enforcement, and cross-company isolation. The green check remains withheld until those gates pass. PAY-P0-001 and PAY-P0-002 remain unticked with their previously documented validation gaps.
## PAY-P0-004 — same-month exit double-payment (P0)

**Status: ◻ IN PROGRESS — green check withheld pending MongoDB transaction/payment-order integration validation**

### Reproduced risk and root cause

`CalculateEmployeeFinalSettlement` selected the last-working-month payroll row but unconditionally copied its basic pay, allowances, bonus, and deductions into F&F. When the monthly payroll had already been processed/paid, those same earnings could be paid again through settlement. The reverse ordering was also possible: F&F could include a draft payroll row, then a later company-wide `ProcessPayrollMonth` could publish that same row for regular payment.

### Implementation

- F&F now treats the last-month payroll row as an effective published snapshot when legacy `IsProcessed` is true or workflow/payment status indicates `PROCESSED`, `PAID`, or `CLOSED` (case-insensitive). In that case it excludes the payroll earnings, bonus, and deductions from F&F while preserving separately approved notice recovery.
- When F&F includes earnings from a draft payroll and HR records the full settlement as paid, the service marks that exact source payroll row `IsProcessed`, `WorkflowStatus=PAID`, and `PaymentStatus=PAID` in the same Mongo transaction as the settlement and exit/audit update. It records a payroll audit event identifying the settlement. Tenant, row identity, and unprocessed-state filters prevent another tenant or concurrently published row from being overwritten.
- The payment-recording endpoint rejects an amount that does not equal the calculated net payable. The alternate mark-paid path refuses negative recovery balances. A transaction conflict returns HTTP 409; unavailable Mongo transactions fail closed.
- Month processing now ignores stale `ApprovalStatus=PENDING` on payroll rows already effectively processed, while still blocking pending approval on draft rows. This lets the remaining employees in the month proceed after an F&F-paid row is excluded from regular payroll.
- No frontend, payroll calculation, attendance, or schema migration was changed.

### Files changed for PAY-P0-004

1. `Codeji.CMS.Services/Employees/EmployeeService.cs` — published-payroll exclusion and atomic F&F/source-payroll/exit paid transition.
2. `Codeji.CMS.Services/PayRoll/PayRollServices.cs` — approval guard applies only to payroll rows still requiring processing.
3. `Codeji.CMS.Services.Tests/UnitTest1.cs` — draft versus processed/paid calculation matrix, full-payment recording, source-payroll paid status, and mismatch rejection.
4. `Codeji.CMS.Services.Tests/PayRollProcessingIdempotencyTests.cs` — subsequent month processing with an F&F-paid row and stale approval marker.
5. `docs/test-reports/PAYROLL-REMEDIATION-REPORT.md` — this living report.

### Verification results

| Gate | Result | Evidence |
| --- | --- | --- |
| Targeted F&F/payment/month-processing tests | **6 passed, 0 failed** | Draft, legacy-processed, lifecycle-processed, paid, stale exit version, exact paid amount/source payroll state, and later monthly processing |
| Full backend services regression | **406 passed, 0 failed, 0 skipped** | `Codeji.CMS.Services.Tests` |
| API rebuild and regression | **Passed; 99 tests passed, 0 failed, 0 skipped** | `Codeji.CMS.API.Tests` compiled against updated service assembly |
| Tenant/authorization review | **Passed by source inspection** | HR exit permission; company-scoped exit/settlement and source-payroll reads; company/id/state conditions in transactional writes |
| Real Mongo replica-set rollback/concurrency | **Not run** | No isolated replica-set database/test tenant available; unit mocks do not prove Mongo transaction execution |
| Actual payroll/bank payment ordering | **Not run** | No safe payment sandbox or authorized test tenant; external transfer and database recording are not one distributed transaction |
| Frontend checks | **Not run** | No frontend source changed; payment form displays net payable, while the full amount must be entered exactly |
| Historical duplicate reconciliation | **Not run** | No production/test database access was used to identify prior double payments |

### Remaining risk and completion gate

Before marking **✓ VERIFIED FIXED**, run a replica-set integration test that races month processing against F&F payment recording and proves the source row is paid exactly once, the exit/settlement/source-payroll updates commit or roll back together, and tenant-A cannot affect tenant-B. Validate that actual bank/payroll transfer procedures prevent operators from issuing both payments before recording either transaction; the application cannot roll back an external transfer. Also reconcile any historical same-month exits where both payroll and F&F may already have been paid. The check remains withheld until these gates pass. PAY-P0-001 through PAY-P0-003 remain unticked with their existing completion gates.

## PAY-P0-005 — Tenant isolation

**Status: ◻ IN PROGRESS — green check withheld pending live cross-tenant API/database validation**

**Finding / root cause:** The payroll read model correctly scoped payroll rows and employees by `CompanyId`, but its job-title enrichment query filtered only by `JobTitleId`. If two tenants share the same job-title identifier, the response could join metadata from the other tenant.

**Implementation:** Added the current `companyId` to the job-title repository predicate in `PayRollServices.GetEmployeePayRoll`. No business rules, permissions, or frontend behavior were changed.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- `Codeji.CMS.Services.Tests/PayrollLifecycleReadModelTests.cs`
- this report

**Validation:**
- Focused payroll lifecycle/read-model tests: **5 passed, 0 failed**.
- Full backend services suite: **407 passed, 0 failed**.
- Backend compilation: **passed** as part of the test build.
- Regression specifically proving a `company-2` job title is not joined into a `company-1` payroll response: **passed**.
- Live MongoDB/API cross-tenant authorization test: **not run**; no safe isolated tenant dataset/replica-set environment is available.

**Remaining risk:** Other payroll-adjacent endpoints still require live tenant-matrix validation across all read, write, payment, payslip, audit, configuration, and background-worker paths. PAY-P0-005 is not marked verified until that runtime validation passes.
## PAY-P0-006 — Historical payroll snapshot integrity

**Status: ◻ IN PROGRESS — green check withheld pending live historical-data and regeneration validation**

**Finding / current control:** The payroll model stores calculation inputs and resolved policy/template identifiers on each payroll row. Manual and upload updates are company-scoped, normalize the pay month, and reject rows whose legacy or lifecycle status is processed, paid, or closed. Automatic generation selects an existing published row first and routes writes through the same immutable-state guard, so later salary/policy changes cannot overwrite a published historical snapshot through the payroll service.

**Implementation in this step:** No production code change was required after tracing the current flow; the existing guard and snapshot fields already cover the identified mutation path. Existing regression coverage was reviewed and retained. No unrelated business rule was changed.

**Relevant files reviewed:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services.Tests/PayrollProcessedGuardRegressionTests.cs`
- `Codeji.CMS.Services.Tests/AutoPayrollLifecycleStateTests.cs`

**Validation:**
- Historical lifecycle guard tests: **4 passed, 0 failed**.
- Automatic lifecycle guard test: **1 passed, 0 failed**.
- Full backend services suite: **407 passed, 0 failed**.
- Backend compilation: **passed** as part of the test run.
- Live historical-data audit, regeneration race, and MongoDB snapshot immutability: **not run**; no isolated database/test tenant is available.

**Remaining risk:** Existing rows created before snapshot fields or lifecycle-state normalization may require a read-only data audit. Runtime validation is still required for automatic regeneration, import/update races, and historical payslip consistency before marking this item verified.
## PAY-P1-001 — Attendance / LOP correctness

**Status: ◻ IN PROGRESS — green check withheld pending live attendance/payroll integration validation**

**Root cause:** Built-in fallback attendance statuses were compared case-sensitively. Lowercase or mixed-case `A`, `HD`, `LHD`, and `WFH-HD` records could therefore receive zero unpaid-day fraction when no custom status rule existed.

**Implementation:** Added `PayrollCalculationRules.ResolveFallbackUnpaidDayFraction` with trimmed, case-insensitive mapping and used it in automatic payroll LOP aggregation. Custom company status rules remain authoritative.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayrollCalculationRules.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services.Tests/PayrollCalculationRulesTests.cs`
- this report

**Validation:**
- Focused fallback-status tests: **6 passed, 0 failed**.
- Full backend services suite: **413 passed, 0 failed**.
- Backend compilation: **passed**.
- Live attendance/payroll integration, database status normalization, and tenant runtime validation: **not run**.

**Remaining risk:** A controlled test tenant is still needed to validate mixed attendance sources, partial-day segments, approved penalty exceptions, divisor policies, and persisted payroll deductions end-to-end.
## PAY-P1-002 — Joiner proration

**Status: ◻ IN PROGRESS — green check withheld pending live payroll calculation validation**

**Trace result:** Automatic payroll clips the eligible period to the later of the payroll month start and `DateOfJoining`, excludes joiners after month-end, calculates inclusive eligible days, weights effective-dated salary records day by day, and divides earned components by the actual calendar days in the month. The calculation snapshot stores the joining and eligible dates used.

**Implementation:** No production code change was made in this step. The current implementation preserves the configured calendar/working-day divisor policy and effective-dated salary behavior; changing that policy would be a business-rule change outside this defect scope.

**Relevant files reviewed:**
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services.Tests/AttendancePayrollRulesTests.cs`
- `Codeji.CMS.Services.Tests/PayrollCalculationRulesTests.cs`

**Validation:**
- Existing joiner/exit period clipping tests: **passed**.
- Full backend services suite after the preceding P1-001 fix: **413 passed, 0 failed**.
- Live joiner payroll calculation with an isolated tenant: **not run**.

**Remaining risk:** Confirm the company’s intended calendar-day versus working-day proration policy with real payroll cases, including February, month-end joiners, mid-month salary revisions, attendance LOP, and timezone/date-string variations before marking verified.
## PAY-P1-003 — Exit proration

**Status: ◻ IN PROGRESS — green check withheld pending live payroll/F&F boundary validation**

**Trace result:** `EmploymentPeriod.GetLastWorkingDate` prefers the structured `LastWorkingDate` and falls back to the legacy `ExitDate`. Automatic payroll includes the last working day inclusively, excludes dates after it, and does not include a completed exit in later months. The payroll snapshot records the exit date and eligible period used.

**Implementation:** No production code change was made in this step. The current boundary behavior is consistent with the existing final-working-day policy and the F&F integration already hardened under PAY-P0-004.

**Relevant files reviewed:**
- `Codeji.CMS.Services/Employees/EmploymentPeriod.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services.Tests/AttendancePayrollRulesTests.cs`

**Validation:**
- Final-month inclusion and post-exit exclusion tests: **passed**.
- Full backend services suite: **413 passed, 0 failed**.
- Live exit payroll/F&F calculation with an isolated tenant: **not run**.

**Remaining risk:** Validate date-only storage, India business-date handling, mid-month exits, notice-date changes, and the interaction between final-month payroll and F&F payment in a controlled integration environment.
## PAY-P1-004 — Rejoin / rehire payroll

**Status: ◻ IN PROGRESS — green check withheld pending live rehire-period validation**

**Trace result:** Rejoin is policy- and eligibility-gated, restores the employee lifecycle to active payroll, clears the former-exit dates, and marks the prior exit record as `Rejoined`. Payroll rows remain keyed by tenant, employee, and normalized month, preserving historical rows and preventing a second row for the same employee/month.

**Implementation:** No production code change was made in this step. The current model intentionally preserves the original `DateOfJoining`; it does not yet expose a separate rehire-effective date or employment-period identifier for payroll proration.

**Relevant files reviewed:**
- `Codeji.CMS.Services/Employees/EmployeeService.cs`
- `Codeji.CMS.Services/Employees/EmploymentPeriod.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- `Codeji.CMS.Services.Tests/EmployeeLifecycleTransitionTests.cs`

**Validation:**
- Rejoin eligibility and lifecycle transition tests: **passed**.
- Payroll lifecycle and unique-month guards: **passed** within the full backend suite (413/413).
- Live rehire payroll run with prior exit payroll and a controlled tenant: **not run**.

**Remaining risk:** A separate rehire effective date/employment-period key may be required for same-month rejoin, new-service-period proration, benefits, and statutory continuity. Confirm the intended policy before changing the domain model.
## PAY-P1-005 — Salary revision effective dating

**Status: ◻ IN PROGRESS — green check withheld pending live salary-revision calculation validation**

**Root cause:** During day-level salary selection, a missing effective salary interval previously fell back to the latest salary record. If that latest record was future-effective, earlier days could be calculated using the future salary.

**Implementation:** `AutoPayRollServices` now fails closed and rolls back the in-memory payroll mutations when an eligible payroll date has no effective salary record, instead of silently applying a future revision.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- this report

**Validation:**
- Full backend services suite: **413 passed, 0 failed**.
- Backend compilation: **passed**.
- Live mid-month revision/gap scenario against MongoDB: **not run**.

**Remaining risk:** A controlled tenant test is still required for continuous salary intervals, mid-month revisions, overlapping records, templates, payroll locking, and rollback after a discovered gap.
## PAY-P1-006 — Arrears

**Status: ◻ IN PROGRESS — capability gap; no green check**

**Finding:** The current payroll implementation supports effective-dated salary records and day-weighted calculation, but it has no explicit arrears entity, arrears ledger, approval workflow, source-period reference, or idempotency key for applying retroactive salary differences.

**Impact:** A salary revision entered after an earlier month was processed can be used for future recalculation only if that month remains editable. There is no production-safe, auditable path to calculate, approve, carry, and pay the difference for a locked historical month.

**Implementation:** No production code change was made. Adding arrears requires confirmed business rules for effective-date eligibility, locked-period handling, tax/statutory treatment, approval roles, payment timing, reversal, and duplicate prevention.

**Validation:**
- Source trace of payroll, salary, template, and assignment models: **completed**.
- Existing backend suite: **413 passed, 0 failed**.
- Arrears calculation/payment integration: **not available because the feature is not implemented**.

**Required follow-up:** Define an immutable arrears record keyed by tenant, employee, source payroll period/revision, and application run; require approval and audit; include reversal and statutory/tax treatment; then add targeted, concurrency, tenant, and payment-order tests before marking verified.
## PAY-P1-007 — Concurrency / stale approval

**Status: ◻ IN PROGRESS — green check withheld pending live concurrent approval validation**

**Root cause:** Payroll approval used a conditional `UpdateMany` but treated any modified row as a successful month approval. It did not verify that every tenant/month row remained unprocessed and reached `ApprovalStatus=APPROVED` before returning success.

**Implementation:** `ApprovePayrollMonth` now re-reads the scoped month after the conditional update and fails with HTTP 409 if any row is missing, processed, or not approved. Existing tenant and lifecycle filters remain unchanged.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- this report

**Validation:**
- Full backend services suite: **413 passed, 0 failed**.
- Backend compilation: **passed**.
- Live concurrent approval/process/payment race: **not run**; no isolated MongoDB replica-set test environment is available.

**Remaining risk:** MongoDB-backed integration testing is still required to prove the postcondition under simultaneous approval, processing, payment, and partial-write failure scenarios.
## PAY-P1-008 — Payroll locking

**Status: ◻ IN PROGRESS — green check withheld pending live lock/rollback validation**

**Root cause:** Month close used a conditional multi-row update but returned success when any paid row changed. It did not verify that every scoped row reached `PaymentStatus=CLOSED` and `WorkflowStatus=CLOSED`.

**Implementation:** `ClosePayrollMonth` now re-reads the company/month rows after the conditional update and returns HTTP 409 if any row is not closed. Existing payment and processed-state prerequisites remain unchanged.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- `Codeji.CMS.Services.Tests/PayrollPaymentTests.cs`
- this report

**Validation:**
- Focused payroll payment/locking tests: **14 passed, 0 failed**.
- Backend compilation: **passed**.
- Full backend suite before this change: **413 passed, 0 failed**.
- Live MongoDB partial-close/concurrent lock validation: **not run**.

**Remaining risk:** Run a replica-set integration test with mixed paid/closed rows, concurrent payment recording, and injected partial-write failure before marking verified.
## PAY-P1-009 — Negative / zero net handling

**Status: ◻ IN PROGRESS — green check withheld pending zero-net workflow validation**

**Root cause:** Component-level validation rejected negative inputs and LOP was capped, but total deductions could still exceed gross pay. The payment endpoint did not independently verify that the processed payroll had a positive net amount.

**Implementation:** `RecordPayrollPayment` now rejects payroll rows whose computed gross less total deductions is zero or negative, returning HTTP 409 before any payment transition or audit write.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- this report

**Validation:**
- Focused payroll payment tests: **14 passed, 0 failed**.
- Backend compilation: **passed**.
- Live zero-net settlement and recovery workflow: **not run**.

**Remaining risk:** Zero-net payroll needs an explicit business workflow for close/settlement without a bank transfer. That workflow must be designed and tested separately; the payment endpoint intentionally does not mark zero-net rows as paid.
## PAY-P1-010 — Rounding / financial precision

**Status: ◻ IN PROGRESS — policy decision required; no green check**

**Finding:** Imported payroll amounts are normalized with `MidpointRounding.AwayFromZero`, while automatic LOP, tax, EPF, prorated components, and penalty amounts use `Math.Round` without an explicit midpoint mode, which defaults to banker’s rounding (`ToEven`). Exact half-cent inputs can therefore produce different results between import/manual and automatic payroll paths.

**Implementation:** No production code change was made because the authoritative tenant/company rounding policy is not defined. Applying one mode globally without approval could change statutory and employee amounts.

**Validation:**
- Source trace completed across import, manual update, automatic calculation, payslip, and payment paths.
- Full backend suite: **413 passed, 0 failed**.
- Half-cent differential matrix and production rounding policy validation: **not run**.

**Required follow-up:** Approve one explicit money-rounding policy and apply it centrally to all monetary outputs, with tests for half-cent LOP, tax, EPF, ESIC, prorated salary, deductions, net pay, import, payslip, and payment reconciliation.
## PAY-P1-011 — Statutory calculations

**Status: ◻ IN PROGRESS — statutory coverage gap; no green check**

**Finding:** Automatic payroll currently calculates configurable EPF, ESIC, and TDS. The payroll result and policy model do not include Professional Tax/state slabs, employer statutory contributions, statutory effective-date/version snapshots, filings/reconciliation, or jurisdiction-specific rule selection.

**Impact:** Payroll can produce an internally consistent payslip while still being incomplete for a multi-state Indian payroll deployment. Hard-coded defaults such as EPF ceiling, ESIC threshold, and TDS regime require policy/year validation before production use.

**Implementation:** No production code change was made. Adding statutory rules without the tenant’s state registrations, applicability, contribution policy, financial year, and compliance ownership would be unsafe.

**Validation:**
- Source trace of EPF, ESIC, TDS, payroll policy, and payslip paths: **completed**.
- Full backend suite: **413 passed, 0 failed**.
- Statutory compliance/reconciliation integration: **not run**.

**Required follow-up:** Define jurisdiction and financial-year configuration, statutory rule versions, employee/employer contributions, exemptions, rounding, caps, effective dates, filing exports, reconciliation, and audit ownership; then add authoritative rule tests and controlled integration validation.
## Additional finding — One-time payroll components

**Status: ◻ IN PROGRESS — capability gap; no green check**

**Finding:** Payroll supports a bonus amount and free-form deduction lines on a payroll row, but there is no dedicated one-time earning/deduction entity with approval, source document, taxable/statutory classification, effective payroll period, reversal, or idempotency key. Reimbursements and employee loans are not integrated into automatic payroll calculation.

**Impact:** HR/Finance cannot reliably trace, approve, reverse, or prevent duplicate application of one-time components across payroll reruns and locked periods.

**Implementation:** No production code change was made. A safe implementation requires component types, taxability, statutory treatment, approval roles, source references, payment timing, and reversal rules.

**Validation:** Source trace completed; full backend suite remains **413 passed, 0 failed**. End-to-end one-time component calculation/payment validation is unavailable because the capability is not implemented.

**Required follow-up:** Design a tenant-scoped, immutable one-time component ledger and integrate it with calculation snapshots, approvals, payroll locking, arrears, F&F, payment reconciliation, and audit history.
## Additional finding — Payroll run-level control

**Status: ◻ IN PROGRESS — architecture gap; no green check**

**Finding:** Payroll lifecycle is represented primarily by individual `EmpPayRoll` rows. There is no first-class tenant-scoped payroll-run aggregate containing period, calculation version, included/excluded employees, validation results, approval/lock history, totals, or reconciliation state.

**Impact:** A multi-employee payroll can be partially generated, approved, paid, or reconciled without one authoritative run record proving the complete population and financial totals.

**Implementation:** No production code change was made. Introducing a run aggregate requires migration, API/UI workflow design, reporting requirements, and a deliberate relationship between run rows and existing payroll records.

**Validation:** Existing row-level lifecycle, approval, payment, and locking tests pass; full backend suite remains **413 passed, 0 failed**. Run-level integration and reconciliation validation are not available because the aggregate is not implemented.

**Required follow-up:** Define `PayrollRun` ownership, state transitions, versioning, employee membership, totals, approval roles, lock/reopen policy, payment batches, and reconciliation/audit behavior before implementation.
## Additional finding — Payroll reversal and reopen

**Status: ◻ IN PROGRESS — capability gap; no green check**

**Finding:** Payroll has forward lifecycle transitions through processed, paid, and closed states, but no controlled reversal, correction, reopen, supersession, or payment-reversal workflow.

**Impact:** A mistaken payroll or payment cannot be corrected through an auditable domain operation that preserves the original snapshot, links the replacement calculation, prevents duplicate payment, and records authorization.

**Implementation:** No production code change was made. A safe correction workflow requires finance authorization, reversal/payment provider integration, replacement-run semantics, statutory impact handling, and immutable audit requirements.

**Validation:** Forward lifecycle tests continue to pass; full backend suite remains **413 passed, 0 failed**. Reversal and reopen validation is unavailable because the capability is not implemented.

**Required follow-up:** Define correction types, approval roles, payment reversal/recovery handling, superseded snapshots, statutory adjustments, tenant isolation, and audit/reporting requirements before implementation.
## Additional finding — Payroll reconciliation and exports

**Status: ◻ IN PROGRESS — capability gap; no green check**

**Finding:** Payroll exposes employee rows and per-payroll audit events, but there is no authoritative run summary, payment batch, bank-file reconciliation, variance report, or statutory export boundary tied to an immutable payroll snapshot.

**Impact:** Finance cannot reliably prove that the approved payroll total equals the payment batch total, identify missing/duplicate transfers, or reconcile payment references and statutory outputs from one controlled record.

**Implementation:** No production code change was made. Export and reconciliation formats, payment-provider ownership, correction handling, and statutory requirements are not defined in the current project.

**Validation:** Existing row-level lifecycle and payment tests continue to pass; full backend suite remains **413 passed, 0 failed**. End-to-end bank/statutory reconciliation is unavailable.

**Required follow-up:** Define payroll summary totals, payment batches, provider references, reconciliation statuses, variance tolerances, exports, correction links, and finance audit permissions before implementation.
## Additional finding — Self-service payslip tenant defense

**Status: ◻ IN PROGRESS — defense-in-depth gap; no green check**

**Finding:** `GenerateEmpSalarySlip` resolves the employee by `UserId` only, while the HR/admin payslip path explicitly scopes by `CompanyId`. The self-service route is authenticated and relies on the repository’s ambient tenant filter, but the service contract does not carry an explicit tenant boundary.

**Impact:** A future caller using an unscoped repository path or missing tenant context could make the self-service lookup less defensible than the explicit company-scoped route.

**Implementation:** No code change was made in this step. Correct remediation should pass the server-derived `CompanyId` into the self-service service method, include it in employee/company/payroll filters, and add controller/service cross-tenant tests without changing self-service permissions.

**Validation:** Current payroll payslip tests and full backend suite remain **413 passed, 0 failed**. Live cross-tenant payslip validation is not available.

**Implementation completed:** Added the server-derived `CompanyId` to the self-service payslip service contract and employee lookup. Self-service permissions and the processed-only payroll requirement remain unchanged.

**Required follow-up:** Add controller/service cross-tenant runtime tests for missing-context, same-user cross-tenant, processed-only, and company-branding boundaries.
## Additional finding — Rollback filter tenant scope

**Status: ◻ IN PROGRESS — green check withheld pending live rollback validation**

**Finding:** Automatic payroll compensation restored an existing payroll row by ID alone and restored an attendance exception by ID alone, without repeating the company predicate used by the forward operation.

**Implementation:** Added `CompanyId` to both rollback filters so compensation cannot restore a row or exception outside the tenant being processed.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- this report

**Validation:** Full backend services suite: **413 passed, 0 failed**. Live injected-failure rollback and cross-tenant collision validation remain unavailable.

**Remaining risk:** Validate rollback behavior against a MongoDB replica set with forced mid-run failures and duplicate/colliding legacy identifiers.
## Additional finding — Divisor policy write scope

**Status: ◻ IN PROGRESS — green check withheld pending live tenant collision validation**

**Finding:** Payroll divisor policy version updates loaded tenant-scoped records but wrote using identifier-only filters.

**Implementation:** Added `CompanyId` to the same-day replacement and prior-open-version update filters in `PayrollDivisorPolicyService`.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayrollDivisorPolicyService.cs`
- this report

**Validation:** Full backend services suite: **413 passed, 0 failed**. Live MongoDB collision/tenant validation remains unavailable.
## Additional finding — Attendance exception write scope

**Status: ◻ IN PROGRESS — green check withheld pending live tenant collision validation**

**Finding:** Automatic payroll marked an attendance exception as applied using an identifier-only update filter.

**Implementation:** Added `CompanyId` to the update predicate so the state transition is bound to the payroll tenant.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- this report

**Validation:** Full backend services suite: **413 passed, 0 failed**. Live MongoDB collision validation remains unavailable.
## Additional finding — Payroll row update write scope

**Status: ◻ IN PROGRESS — green check withheld pending live tenant/concurrency validation**

**Finding:** Upload and manual payroll update paths loaded rows with tenant/month identity but then wrote using an identifier-only update predicate.

**Implementation:** Added `CompanyId`, employee identity, and normalized pay month to both update filters.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayRollServices.cs`
- this report

**Validation:** Full backend services suite: **413 passed, 0 failed**. Live MongoDB stale-row/tenant-collision validation remains unavailable.
## Additional finding — Payroll monetary rounding standardization

**Status: ◻ IN PROGRESS — green check withheld pending approved policy and live financial validation**

**Finding:** Automatic payroll monetary calculations used implicit banker’s rounding while manual/import payroll used explicit `AwayFromZero` rounding.

**Implementation:** Added `PayrollCalculationRules.RoundMoney` using `MidpointRounding.AwayFromZero` and applied it to automatic LOP, EPF, TDS, proration, and penalty monetary calculations. ESIC’s intentional ceiling rule was unchanged.

**Changed files:**
- `Codeji.CMS.Services/PayRoll/PayrollCalculationRules.cs`
- `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`
- this report

**Validation:** Focused half-cent rounding tests and full backend services suite: **416 passed, 0 failed**. A formal tenant/company rounding-policy approval and live financial differential validation remain pending.
## Release gate — statutory and legal review (2026-09-30)

**Status: BLOCKED — implementation may be tested, but payroll release approval is withheld.**

The current payroll code exposes configurable EPF, ESIC, and TDS values, but it does not yet model statutory jurisdiction, registration, financial-year rule version, employer/employee contribution split, exemptions, effective dates, filing outputs, and reconciliation evidence required for a production statutory claim. No statutory rate, wage ceiling, exemption, or state-specific rule was changed in this review.

Official references reviewed:

- [EPFO official FAQ](https://www.epfindia.gov.in/site_docs/PDFs/Updates/FAQ_Reduced_rate_of_contribution_20052020.pdf) describes the normal EPF contribution as 12% of basic wages and dearness allowance, while also documenting a time-bound notified reduction. This confirms that a hardcoded rate without an effective-date/version model is unsafe.
- [ESIC official publication](https://esic.gov.in/attachments/esistatedirectoratefile/ef84365f31f15306ddb397902d675858.pdf) states the ₹21,000 monthly coverage ceiling and identifies contribution rates; applicability and rates must still be validated against the employee, establishment, period, and current notification.

Release-gate checklist:

1. **Legal/payroll reviewer sign-off:** pending; an authorized reviewer must approve jurisdictions, financial year, statutory policy versions, rounding, caps, exemptions, and employer versus employee contribution treatment.
2. **Configuration evidence:** pending; each tenant must have an approved statutory profile and effective-dated policy snapshot before statutory payroll is published.
3. **Product implementation:** incomplete; Professional Tax/state slabs, statutory version snapshots, employer contributions, filings/exports, and reconciliation are not covered by the current implementation.
4. **Validation:** targeted unit coverage is green as recorded above; live MongoDB integration, cross-tenant validation, statutory differential testing, and authenticated E2E remain unavailable in this workspace.
5. **Release decision:** **do not represent the current payroll as legally compliant or production statutory payroll** until items 1–4 are closed and evidenced.

This section is a release-control record, not legal advice or legal approval.
## Implementation update — statutory policy metadata gate (2026-09-30)

Implemented a server-side release-safety gate for active statutory policy profiles. Active profiles with EPF or ESIC enabled now require `StatutoryJurisdiction`, `FinancialYear`, and `StatutoryRuleVersion`; the values are persisted and returned through the payroll configuration DTO. Exempt-only profiles and inactive draft profiles remain able to be saved without these fields. No statutory amount, rate, ceiling, or legal status was inferred or changed.

Changed files:

- `Codeji.CMS.Repository/Entities/Employees/PayrollConfigurationEntities.cs`
- `Codeji.CMS.DTO/PayRoll/PayrollConfigurationDtos.cs`
- `Codeji.CMS.Services/PayRoll/PayrollTemplateService.cs`
- `Codeji.CMS.Services.Tests/PayrollTemplateServiceTests.cs`

Validation after implementation:

- Focused payroll service tests: **133 passed, 0 failed**.
- Payroll API/controller contract tests: **23 passed, 0 failed**.
- Full backend services regression suite: **450 passed, 0 failed**.
- Live MongoDB integration and authenticated browser E2E: **not run**; environment credentials/database safety prerequisites remain unavailable.

The statutory release gate remains blocked until an authorized payroll/legal reviewer approves the tenant-specific metadata and rules.
## Extended validation update (2026-09-30)

The metadata gate was corrected so the new fields exist only on `PayrollPolicyProfile` and `PayrollPolicyProfileDto`; unrelated pay-group, salary-template, and assignment contracts were not expanded.

- Focused payroll service suite after correction: **134 passed, 0 failed**.
- Full backend services regression: **450 passed, 0 failed**.
- Payroll API/controller contract suite: **23 passed, 0 failed**.
- Frontend Vitest suite: **22 test files, 46 tests passed, 0 failed**.
- Frontend TypeScript typecheck: **passed**.
- An empty untracked frontend test placeholder was removed because it caused Vitest discovery failure; no product component was changed.

Still externally blocked: live MongoDB/replica-set integration, authenticated browser E2E, tenant-specific statutory configuration, and authorized legal/payroll sign-off.
## Final product-side completion update (2026-09-30)

Statutory metadata is now exposed in the payroll settings UI and included in the frontend payroll model. Active EPF/ESIC profiles require jurisdiction, financial year, and rule version in both UI and server validation.

Final local validation:

- Backend services: **450 passed, 0 failed**.
- Backend API tests: **106 passed, 0 failed**.
- Frontend Vitest: **22 files / 46 tests passed, 0 failed**.
- Frontend TypeScript typecheck: **passed**.
- Frontend production build: **passed**.

External release prerequisites still cannot be completed locally: authenticated E2E credentials, isolated live MongoDB/replica-set validation, tenant-specific statutory settings, and authorized legal/payroll approval.
## Tenant statutory approval workflow (2026-09-30)

Implemented tenant-scoped statutory policy governance:

- New active statutory policy profiles start as `DRAFT`.
- Payroll policy resolution ignores `DRAFT` and `SUBMITTED` profiles; legacy records with no approval state remain backward-compatible.
- Added company/id/version-constrained `SubmitPolicy` and `ApprovePolicy` service operations with audit actor/timestamp fields.
- Added API endpoints for submit (`PayrollSettings.Edit`) and approve (`PayrollSettings.Approve`).
- Added approval-state fields to the policy entity and DTO.
- Added unit coverage for tenant-scoped submitted-state approval.

Validation: **135 payroll service tests passed, 0 failed**; payroll API contract tests **23 passed, 0 failed**. Actual legal/payroll approval remains an authorized business action and must be performed by the tenant’s reviewer through the restricted approval endpoint; it cannot be fabricated by automated tests.