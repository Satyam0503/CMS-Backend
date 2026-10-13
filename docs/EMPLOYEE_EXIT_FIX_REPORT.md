# Employee Exit and Offboarding Fix Report

Branch: `integration/current-work`

## Implementation log

### Phase 1 - Completed exit availability and lifecycle restoration

**Problem**

Completed exits were assigned `AvailabilityStatus = Working`. A leave or lifecycle-case reversal could also restore a former employee to `Working`.

**Old behavior**

```csharp
state.AvailabilityStatus = EmployeeLifecycleStatuses.Working;
```

**New behavior**

```csharp
state.AvailabilityStatus = EmployeeLifecycleStatuses.NotApplicable;
```

Former employees are also rejected by `TryRestoreCaseLifecycleState`, and `EmploymentPeriod.ResolveAvailabilityOn` returns `NotApplicable` for completed exits.

**Files changed**

- `Codeji.CMS.Repository/Entities/Employees/EmployeeLifecycle.cs`
- `Codeji.CMS.Services/Employees/EmployeeLifecycleTransitionService.cs`
- `Codeji.CMS.Services/Employees/EmployeeLifecycleCaseService.cs`
- `Codeji.CMS.Services/Employees/EmploymentPeriod.cs`
- `Codeji.CMS.Services.Tests/EmployeeLifecycleTransitionTests.cs`

**Validation**

Focused lifecycle tests: **30 passed, 0 failed**.

### Phase 2 - Completed-exit status filters

**Problem**

Settlement, evidence, handover, and clearance persistence paths used explicit `Resigned`/`Terminated` checks and could exclude retirement, contract-end, and death cases.

**New behavior**

Those paths now use the Mongo-translatable shared collection:

```csharp
EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)
```

The same phase also removed exited employees from WFH allocation/listing and office schedule employee lookup when lifecycle status is already final.

**Validation**

Lifecycle and policy resolver tests: **35 passed, 0 failed**.

### Phase 3 - Policy snapshot traceability

The immutable snapshot now records the resolved `SourceScope` and `ExitType` in addition to the policy ID/version stored on the exit case. Existing cases therefore retain both the policy values and the exact scope/type resolution used at initiation.

### Phase 4 - Remaining exit-type filters

Additional employee-exit evidence and handover paths were updated from explicit resignation/termination checks to `CompletedEmploymentStatuses`, so retirement, contract-end, and deceased cases are not silently excluded.

The full suite was previously validated at **224/224** before this phase. A new rebuild is currently blocked by an existing Windows file lock on `Codeji.CMS.Services.dll`; the compiler reports access denied even after the visible `dotnet` processes were stopped. This is an environment lock, not a test assertion failure, and must be cleared before final validation.

### Phase 5 - Self-resignation policy control

Service-level coverage now verifies both policy branches in `Codeji.CMS.Services.Tests/UnitTest1.cs`:

- `SubmitOwnResignation_WhenPolicyDisablesSelfResignation_IsRejectedBeforeAnyWrite` expects `SELF_RESIGNATION_DISABLED_BY_POLICY` and verifies no exit is persisted.
- `SubmitOwnResignation_WhenPolicyAllowsSelfResignation_CreatesResignationCase` verifies an enabled policy creates a `Resigned` exit case.

The focused test command reached compilation but could not finish because Windows denied access to `Codeji.CMS.Services/obj/Debug/net10.0/Codeji.CMS.Services.dll`; this is recorded as an environment/file-lock blocker, not a test assertion failure.

### Phase 6 - Contract end compensation

`UpdateEmployeeContract` now records the prior contract state and compensates the contract write if the subsequent `ContractEnded` exit cannot be created. This prevents a rejected offboarding case from leaving the employee permanently marked `Ended`. A Mongo transaction remains preferable where deployment topology supports it; the current fallback is an optimistic versioned compensation update.

### Phase 7 - Leave ownership UI alignment

`EmployeeLifecycleDialog.tsx` now treats `AdministrativeLeave` as leave-owned, matching the backend. When an employee has an active leave-owned availability status, the account and payroll selectors are disabled and the dialog explains that those controls belong to the linked lifecycle case/leave decision. This prevents the UI from inviting manual changes that can later conflict with case control reconciliation.

Permanent exit initiation now checks the tenant/scoped exit policy and an overlapping accepted leave. By default it returns `ACTIVE_LEAVE_BLOCKS_EXIT`; companies may explicitly enable `AllowExitDuringActiveLeave`. The resolved value is copied into the exit snapshot so later policy edits cannot change an active case's decision.

### Phase 8 - Deceased alumni protection

Completed `Deceased` exits now set alumni status to `Blocked`, and lifecycle transition requests cannot manually change alumni status for a deceased employee (`DECEASED_EMPLOYEE_ALUMNI_LOCKED`). The UI disables the alumni selector for deceased employees.

Added `AllowEmployeeSelfResignation` to the tenant/scoped exit policy, request DTO, and immutable exit policy snapshot. `SubmitOwnResignation` now rejects self-submission when the resolved policy disables it. Manager/HR-initiated exits remain unaffected.

Validation is pending because the build output is currently locked by another Windows process; the last clean full-suite result remains **224/224**.

The admin UI already exposes the modular `SelfResignation` feature. It now also maps that feature to the backend `allowEmployeeSelfResignation` policy field, so the setting is persisted and enforced server-side.

### Phase 6 - Contract lifecycle validation

Contract extension/end now:

- Rejects extension of an already-ended contract.
- Requires an extension end date after the current end date.
- Rejects end dates before the contract start date.
- Rejects contract end when an active employee-exit case already exists.
- Preserves the contract history action as `Extended` or `Ended`.

This prevents the previous partial state where the contract could be saved as ended and exit initiation could then fail because another active exit already existed.

### Phase 7 - Attendance reminder eligibility

Attendance reminder evaluation now uses `EmploymentExitRules.IsCurrentEmployee`, and scheduled automatic-present/review reminder paths exclude completed exits. Resigned, terminated, retired, contract-ended, and deceased employees are no longer candidates for missing-attendance reminders or automatic office-present creation.

### Phase 8 - Contract rule tests

Extracted contract date/status validation into `EmployeeContractLifecycleRules` and added focused tests for ended-contract extension, non-forward extension dates, invalid end dates, duplicate active exits, and valid operations.

Added attendance reminder regression coverage for every completed exit type. Former employees are now explicitly asserted to be absent from missing-attendance results.

Dashboard department counts, gender summaries, and upcoming celebration employee rosters now exclude inactive and completed-exit employees while retaining active, notice-period, and suspended employees.

Rejoin now performs the shared `EmploymentExitRules.CanRejoin(employee)` check after loading the employee, in addition to exit-record validation. Regression coverage was added for all rejoinable completed-exit types and deceased exclusion.

Former-employee portal sessions now use `EmploymentExitRules.CanUseFormerEmployeePortal`, which requires an eligible completed exit, enabled policy, enabled post-exit access, and verified personal email. Deceased exits are excluded by the shared rule.

Added tenant/scoped `AllowRejoin` policy support. The value is captured in the exit policy snapshot and enforced during rejoin, while the existing exit-interview rehire decision remains an additional gate.

The admin policy screen now exposes an `Allow rejoin` toggle under Post-exit decisions. It is saved through the existing policy payload and remains subject to exit-interview and deceased-employee safeguards.

Because policy resolution already prioritizes exit-type-specific scope before generic scope, `AllowRejoin` now supports distinct rejoin behavior for resignation, termination, retirement, contract end, and death. A resolver regression test covers a termination-specific policy overriding the generic policy.

Added `EmployeeExitSecurityTests` to assert that exit-policy and exit-case update filters contain tenant, entity ID, version, active-state, and soft-delete constraints.

Extended the same security coverage to contract updates and rejoin updates, asserting tenant, employee/exit identity, and optimistic version constraints.

Added equivalent tenant/version/soft-delete coverage for clearance and evidence update paths.

Fixed the generic repository filter so tenant scoping is based on the presence of a `CompanyId` property, not only `ISupportAuditing`. Legacy `AttendanceModel` records now receive automatic tenant filtering whenever a tenant context is supplied.

Removed a stray `Console.WriteLine` from exit-policy persistence so policy updates no longer write diagnostic data directly to standard output.

Policy feature validation now rejects completion blockers, required features, waivers, or waiver-reason requirements when their parent feature state is incompatible.

Clearance-template persistence already enforces supported policy gates, valid scope, department ownership, effective date ranges, duplicate scoped keys, tenant ownership, and optimistic version checks. This phase documented and counted those controls as complete.

Leave-management overlap handling now rejects former employees before checking active absence, and the upcoming approved-leave query now includes `CompanyId`, preventing cross-tenant overlap reads.

## Remaining phases

- Replace legacy `Resigned`/`Terminated` filters with the shared completed-status rule.
- Centralize active work-activity eligibility for attendance, WFH, leave, and schedules.
- Complete policy snapshot audit metadata and policy fallback validation.
- Harden contract lifecycle and duplicate-exit handling.
- Review login, payroll, portal, rejoin, and document authorization.
- Add full five-exit-type integration coverage and tenant isolation coverage.
