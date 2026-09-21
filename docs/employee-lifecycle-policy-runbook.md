# Employee lifecycle policy and verification runbook

## Ownership model

Lifecycle availability describes whether the employee is working or temporarily absent. LeavePolicy decides entitlement, approval, and paid/unpaid treatment. AttendanceStatusSetting decides the paid/unpaid fraction for attendance rows. WorkFromHomePolicy decides WFH eligibility, quota, approval, and attendance mapping. Lifecycle policy supplies the cross-module defaults and request restrictions for a company.

## Expected behavior matrix

| Availability | Normal attendance | New leave request | New WFH request | Typical payroll | Return date |
|---|---|---|---|---|---|
| Working | Expected | Allowed by LeavePolicy | Allowed by WorkFromHomePolicy | Payroll status decides | Optional |
| Annual/Sick/Medical/Maternity/Paternity leave | Not expected | Blocked while already absent | Blocked | Payroll status remains independently configurable; LeavePolicy controls pay | Optional/manual resume |
| Sabbatical | Not expected | Blocked | Blocked | Usually Paused/Hold by policy | Required; resolves to Working on/after date |
| Long-term/Administrative leave | Not expected | Blocked | Blocked | Payroll status remains independently configurable | Optional/manual resume |
| Unauthorized absence | Not expected | Blocked | Blocked | Usually Hold by policy | Optional/manual resume |
| Absconding under review | Not expected | Blocked | Blocked | Usually Hold by policy | HR resolution/manual resume |
| Suspended | Not expected | Blocked | Blocked | Usually Hold by policy | HR resolution/manual resume |

Account access is independent but enforced at request time. Active permits login; Locked, Limited, TemporarilyDisabled, Suspended, and Closed reject authenticated requests. Completed exits set employment to the selected exit status, account to Closed, payroll to FinalSettlement, and offboarding to Completed.

## Manual verification

1. Select a test employee and record the lifecycle version.
2. Set each availability state from the matrix, save, reload, and confirm the row highlight and status summary.
3. For Sabbatical, enter a future return date. Confirm attendance and WFH are unavailable before the date and the employee resolves to Working on/after it. Confirm Resume Work clears the date.
4. For each account status, use a fresh login and an existing authenticated request. Active succeeds; every restricted state returns unauthorized.
5. For each payroll status, run payroll for a test month. Active/FinalSettlement can be processed; Hold/Paused/Closed current employees are excluded without blocking unrelated employees.
6. While the employee is non-working, try leave and WFH submission. Confirm the short lifecycle-specific message and no request is created.
7. Approve a normal leave and WFH request for a Working employee. Confirm source-owned attendance is created/reconciled and duplicate ownership is rejected.
8. Complete a resignation and termination exit. Confirm final settlement, account closure, offboarding completion, former-portal restrictions, directory status, and lifecycle history.
9. Run the same checks with a second company and confirm no policy, employee, leave, attendance, payroll, or exit data crosses tenants.

## Release gates

- Backend API and migration projects build successfully.
- Backend services tests pass with zero failures.
- Frontend typecheck, unit tests, and production build pass.
- Execute migration runner against a backup/staging MongoDB before production rollout.
- Complete the authenticated API and browser steps above against the deployed environment.
