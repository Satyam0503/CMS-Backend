# High-risk flow audit

## Authentication

Login validates credentials, verified email, and app-enabled role; creates an access JWT and hashed refresh token. Refresh creates a replacement then revokes the old token. Logout revokes the supplied token for the authenticated user. The backend flow is covered by unit tests, but browser refresh failure is not; see `BUG-4-01`.

## Tenant resolution

Authenticated routes resolve tenant from a single JWT claim. Selected anonymous paths can resolve `cId`, so public-resource predicates must independently validate publication/company ownership. Isolated two-tenant tests are required before considering this layer fully verified.

## Employee to leave to attendance to payroll

Employee and leave-policy records drive leave balances. A leave review updates request/balance then reconciles attendance. Attendance and exception data feed payroll generation; processed payroll rows block later updates. This is a multi-document chain with no demonstrated transaction/session boundary, so failure and concurrency paths remain risks, not confirmed defects.

## Recruitment to public careers

Internal jobs are managed through Jobs permissions. Public jobs are separately filtered for visibility and company eligibility. Internal applications and public applications feed applicant records; stage changes can create logs and queue email. The internal title lookup misses Jobs.View enforcement; see `BUG-4-02`.

## Test priority order

1. Browser interceptor test for expired refresh token.
2. API authorization test for internal vacancy lookup without Jobs.View.
3. Two-company IDOR suite across employee, leave, attendance, payroll, applicant, and public IDs.
4. Isolated Mongo concurrency/failure-injection tests for leave-attendance-payroll writes.
