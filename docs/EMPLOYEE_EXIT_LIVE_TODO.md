# Employee Exit and Offboarding - Live Implementation Todo

Branch: `integration/current-work`
Scope: backend, frontend, tenant security, lifecycle consistency, tests, and production validation.

Status values: `[x] Done`, `[-] In progress`, `[ ] Remaining`, `[!] Blocked`.

## Phase 1 - Completed-exit lifecycle state

- [x] Replace completed-exit availability `Working` with `NotApplicable`.
- [x] Prevent lifecycle-case restoration for former employees.
- [x] Make date-based availability return `NotApplicable` for former employees.
- [x] Add lifecycle regression assertion.
- [x] Run focused lifecycle tests: 30 passed.

## Phase 2 - Five exit types in downstream queries

- [x] Replace resignation/termination-only exit filters in settlement paths.
- [x] Replace resignation/termination-only evidence filters.
- [x] Replace resignation/termination-only handover filters.
- [x] Replace resignation/termination-only clearance persistence filters.
- [x] Exclude completed exits from WFH allocation and office schedule lookup.
- [x] Run focused lifecycle/policy tests: 35 passed.

## Phase 3 - Policy snapshot and policy controls

- [x] Store policy source scope in the exit record.
- [x] Store policy exit type in the exit record.
- [x] Store source scope and exit type in the immutable policy snapshot.
- [x] Add backend `AllowEmployeeSelfResignation` policy setting.
- [x] Enforce self-resignation policy server-side.
- [x] Connect the existing UI self-resignation feature to the backend field.
- [ ] Add explicit company-level `RequireConfiguredPolicy` behavior with a valid configuration owner. Do not infer this from a missing policy object.
- [x] Validate incompatible feature combinations on save: completion/required/waiver settings must reference enabled features.
- [x] Validate clearance-template gate values, scope, dates, duplicate keys, and tenant/version ownership on save.
- [x] Add service tests for self-resignation enabled/disabled behavior. (Tests added; execution currently blocked by a Windows file lock on Codeji.CMS.Services.dll.)

## Phase 4 - Contract lifecycle

- [x] Reject extension of an ended contract.
- [x] Require extension end date.
- [x] Prevent extension from shortening the current contract.
- [x] Reject contract end before contract start.
- [x] Reject contract end when an active exit already exists.
- [ ] Add contract extension approval policy support.
- [ ] Add explicit contract reactivation operation and permission.
- [x] Add reusable contract validation rules and focused unit tests.
- [ ] Add contract history and duplicate-exit integration tests.
- [x] Add compensating rollback when contract-end persistence succeeds but exit creation is rejected. (True database transaction remains a future Mongo transaction option.)

## Phase 5 - Shared active-employee eligibility

- [x] Exclude former employees from attendance missing-record evaluation.
- [x] Exclude former employees from automatic-present generation.
- [x] Exclude former employees from attendance review reminders.
- [x] Exclude former employees from WFH allocation/listing paths.
- [ ] Replace remaining duplicated active-employee predicates in leave management.
- [x] Block leave-absence overlap processing for former employees.
- [x] Add authenticated tenant filtering to upcoming approved-leave overlap lookup.
- [ ] Replace remaining duplicated active-employee predicates in payroll.
- [ ] Replace remaining duplicated active-employee predicates in schedules and dashboards.
- [x] Update dashboard department, gender, and celebration rosters to exclude completed exits.
- [ ] Add Mongo-translatable shared query specifications where expression helpers cannot be translated.
- [x] Add attendance reminder regression coverage for all five completed exit types.

## Phase 6 - Login, portal, documents, and rejoin

- [ ] Make login use one shared lifecycle eligibility rule at query and authorization levels.
- [x] Centralize former-employee portal eligibility.
- [ ] Ensure death blocks portal, document, and rejoin actions consistently.
- [x] Enforce shared employee-level rejoin eligibility before reactivation.
- [x] Add tenant/scoped `AllowRejoin` policy and snapshot enforcement.
- [x] Expose `AllowRejoin` in the admin UI.
- [x] Support exit-type-specific rejoin overrides through the existing policy resolver.
- [x] Add resolver regression coverage for exit-type-specific `AllowRejoin`.
- [ ] Make rejoin update employee, lifecycle, account, payroll, and exit state atomically.
- [ ] Decide whether rejoin reactivates the old record or creates a new employment record.
- [ ] Add document ownership and tenant-isolation tests.

## Phase 7 - Completion and workflow state machine

- [ ] Centralize exit status transitions.
- [ ] Remove duplicated direct policy-boolean checks.
- [ ] Return structured completion blockers from the backend.
- [ ] Validate manager/HR approval combinations.
- [ ] Add explicit resignation withdrawal rules.
- [ ] Add exit cancellation rollback rules.
- [ ] Recalculate notice, payroll, benefits, reminders, and access dates when LWD changes.

## Phase 8 - Payroll and settlement

- [ ] Verify all settlement APIs support all five exit types.
- [ ] Apply final-settlement policy consistently to manual and automated completion.
- [ ] Add death-specific settlement/nominee handling where required by product policy.
- [ ] Centralize payroll cutoff event handling.
- [ ] Add exit-month and future-month payroll tests.

## Phase 9 - Tenant security and concurrency

- [ ] Audit every exit/policy/clearance/document/contract query for authenticated tenant filtering.
- [ ] Ensure request-body company IDs cannot override authenticated tenant IDs.
- [ ] Verify permission separation for policy, initiation, approval, completion, settlement, portal, and rejoin.
- [ ] Add cross-tenant access denial tests.
- [x] Add repository-filter coverage for tenant and version scope on exit/policy updates.
- [ ] Add version-conflict tests for clearance, contract, and rejoin updates.
- [x] Add filter-level version/tenant coverage for contract and rejoin updates.
- [x] Add filter-level version/tenant coverage for clearance and evidence updates.
- [x] Apply tenant filtering to legacy entities with a `CompanyId` property, including attendance records.
- [ ] Ensure no partial lifecycle state is saved on concurrency failure.

## Phase 10 - Frontend behavior

- [ ] Display employment, offboarding, account, payroll, availability, and portal dimensions separately.
- [ ] Render actions only from backend `AllowedActions`.
- [ ] Show structured completion blockers.
- [ ] Add contract Extend/End/Reactivation UI separation.
- [ ] Show resolved policy source, exit type, and policy version.
- [ ] Add UI coverage for all five exit types and disabled self-resignation.
- [x] Align the Manage Lifecycle UI with backend leave ownership, including AdministrativeLeave and account/payroll lock messaging.
- [x] Add a tenant-scoped policy switch for whether permanent exit may begin during an active approved leave (`AllowExitDuringActiveLeave`, default false).
- [x] Apply deceased-specific alumni handling: completed death sets Alumni to Blocked and prevents manual alumni changes.

## Phase 11 - Data migration and compatibility

- [ ] Backfill missing lifecycle states.
- [ ] Normalize legacy employment-status values.
- [ ] Reconcile `EmpUser.EmploymentStatus` with `Lifecycle.EmploymentStatus`.
- [ ] Repair former employees incorrectly stored as `AvailabilityStatus = Working`.
- [ ] Run migration dry-run and produce counts before write execution.
- [ ] Add post-migration integrity report.

## Phase 12 - Validation and release gates

- [x] Run existing full backend suite previously: 224 passed, 0 failed.
- [ ] Clear Windows build-output file locks.
- [ ] Rebuild backend after latest changes.
- [ ] Run complete backend unit and integration suite.
- [ ] Run frontend typecheck.
- [ ] Run complete frontend unit suite.
- [ ] Run frontend production build.
- [ ] Run authenticated browser/UAT scenarios for all five exit types.
- [ ] Verify tenant-isolation scenarios.
- [ ] Verify attendance, WFH, leave, payroll, login, portal, and rejoin scenarios.
- [ ] Update this file with final pass/fail counts.
- [ ] Mark production readiness only after all release gates pass.

## Current blocker

The latest backend rebuild is blocked by Windows access denial while overwriting generated DLLs under `bin\Debug\net10.0`. The last clean backend result before the latest edits was 224 passed, 0 failed. Latest changes must be rebuilt and retested after the locking process is cleared.

## Rule for continuing

Implement one unchecked phase at a time. After each phase:

1. Run focused tests.
2. Run `git diff --check`.
3. Update this file with changed files and results.
4. Continue only when the phase passes or has a documented external blocker.
