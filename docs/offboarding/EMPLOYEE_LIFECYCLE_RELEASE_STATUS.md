# Employee lifecycle release status

Updated: 2026-09-08

## Implemented behavior

Employee lifecycle is additive to the legacy employee and exit flows. Employment, availability, account, payroll, offboarding, former-portal, and alumni relationships are stored and resolved separately.

- Temporary availability such as annual, sick, medical, maternity, paternity, sabbatical, long-term, administrative, and unauthorized absence does not create an exit or former-employee classification.
- Notice-period employees remain current employees until the effective last working date. Leave may overlap notice period.
- Absconding starts as unauthorized absence or absconding under review. HR decision is required before a real `EmployeeExit` is completed.
- Confirmed absconding remains backward compatible through a completed exit with termination/absconding reason.
- Sabbatical and other temporary states return through `Resume Work`; completed exits use rejoin.
- Account access and payroll state are independent of employment status.
- Attendance eligibility is date-aware and considers joining date, last working date, approved leave, holidays, weekoffs, schedules, WFH, and lifecycle availability.
- Former portal access is controlled by completed exit eligibility and remains separate from alumni membership.

## Database migration status

All migrations completed successfully against the configured database, including:

- `2026-08-25-01-BackfillEmployeeEmploymentStatus`
- `2026-08-25-02-BackfillEmployeeExitIndexes`
- `2026-08-27-AddFormerEmployeePortalIndexes`
- `2026-08-31-HardenOffboardingPolicyTemplateIndexes`
- `2026-09-07-01-BackfillEmployeeLifecycleState`
- `2026-09-07-02-AddEmployeeLifecycleIndexes`

The former-portal token migration removes unusable empty legacy hashes and manages unique indexes for valid string hashes. It is safe to retry after a partial index migration.

## Validation status

| Check | Result |
|---|---|
| Backend services tests | 192 passed, 0 failed, 0 skipped |
| Frontend typecheck | Passed |
| Frontend unit tests | 16 files passed, 34 tests passed |
| Frontend production build | Passed |
| Authenticated Playwright E2E | 90 tests: 38 passed, 49 failed, 3 skipped |

The latest E2E run reached the local API and authenticated tests ran. The remaining failures include an attendance selector mismatch (`Select to update attendance` not found), employee-list login/session failures in some browser projects, and production security checks that are not valid against the local HTTP environment. The supplied credentials were passed only as temporary process environment variables and were not saved. The earlier run was blocked by an unavailable API; this run is no longer classified as connection-refused.

## Release gate

The code and database migration gates pass, but the feature is not release-approved until the backend and frontend are started together on the intended local/staging URLs and the authenticated E2E suite is rerun successfully. Prior to production deployment, verify tenant isolation, HR/admin permissions, leave/attendance/WFH/payroll interactions, offboarding completion, former portal access, and rejoin/resume-work behavior on staging data.
