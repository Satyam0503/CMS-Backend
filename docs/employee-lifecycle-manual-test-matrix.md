# Employee lifecycle manual/UAT matrix

Run these checks with a test employee in a non-production tenant. Record the employee id, company id, actor, timestamps, lifecycle history, attendance result, leave result, WFH result, payroll inclusion result, and login/request result for every case.

## Baseline

1. Confirm the employee is `Employment: Active`, `Availability: Working`, `Account: Active`, and `Payroll: Active`.
2. Confirm a normal attendance mark, leave request, WFH request, and payroll preview succeed according to the configured company policies.
3. Open Manage lifecycle and record the lifecycle version before each transition.

## Availability states

| State | Save expectation | Attendance | New leave/WFH | Return behavior |
|---|---|---|---|---|
| Working | Saves successfully | Normal marking allowed | Allowed | N/A |
| Annual/Sick/Medical/Sick-Medical/Maternity/Paternity/Long-term/Administrative leave | Saves with policy guidance | No normal attendance | Rejected | Resume Work or configured return date restores Working |
| Sabbatical | Return date required and must be future/effective | No normal attendance | Rejected | On/after return date, effective availability resolves to Working; verify history remains auditable |
| Unauthorized absence | HR reason required by policy | No normal attendance | Rejected | Manual return to Working requires resolution reason |
| Absconding under review | HR approval/reason policy applies | No normal attendance | Rejected | Return to Working requires resolution reason; completed exit follows offboarding |
| Suspended | HR approval/reason policy applies | No normal attendance | Rejected | Manual return or completed exit; verify history |

For each row, reload the employee list and reopen the modal. Verify the effective status, highlighted lifecycle action, policy guidance, and lifecycle history match the saved state.

## Account access

For each of `Active`, `Locked`, `Limited`, `TemporarilyDisabled`, `Suspended`, and `Closed`:

1. Save the account status without changing availability or payroll.
2. Verify the employee row displays the account status without overwriting availability/payroll labels.
3. Verify login, refresh-token, and authenticated API behavior matches the account policy. `Active` permits normal access; restricted/closed states must not be treated as active.
4. Restore `Active` and verify access recovery where the policy permits it.

## Payroll

For each of `Active`, `Hold`, `Paused`, `FinalSettlement`, and `Closed`:

1. Save the payroll status independently from availability and account status.
2. Verify the payroll settings list reflects the saved status after reload.
3. Verify `Hold`, `Paused`, and `Closed` current employees are excluded from normal payroll processing; `FinalSettlement` is used only for exit settlement behavior.
4. Verify an exited employee is included only in the configured exit month and is not included in later normal payroll runs.

## Atomic save and concurrency

1. Change availability, account, and payroll together and save once.
2. Confirm all requested values and all audit entries persist after reload, or none persist when validation fails.
3. Open the same employee in two sessions. Save from session A, then save the stale version from session B. Session B must receive a version-conflict notification and must not overwrite session A.
4. Submit a missing required reason/return date. Confirm no lifecycle dimension changes.

## Leave, attendance, and WFH integration

1. While Working, submit an approved leave and verify leave-owned attendance is created according to the leave policy.
2. While on leave or another non-Working availability state, attempt a new leave and WFH request. Both must be rejected with a short, user-readable reason.
3. Attempt normal attendance marking during the same state. It must not create a normal present/working mark.
4. Set a future return date, verify the state remains unavailable before that date, then run the date-aware reconciliation/processing path on and after the date and verify effective Working behavior.
5. Verify source-owned leave/WFH attendance cannot be edited through generic attendance controls.

## Completed exit

1. Complete a Resigned exit and a Terminated exit through the configured offboarding gates.
2. Verify employment status, exit date, account `Closed`, payroll `FinalSettlement`, offboarding `Completed`, former-portal eligibility, alumni state, and audit history.
3. Verify leave, WFH, attendance, and normal payroll requests are rejected after exit.
4. Verify rejoin follows the configured rejoin workflow and does not silently reopen a closed account or payroll state.

## Release evidence

Attach screenshots or API responses for each transition, the lifecycle history, payroll inclusion decision, attendance/leave/WFH decision, account-access response, and the final database record. Do not mark the live gate complete until these checks run against a connected staging database and API.
