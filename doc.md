# Human-style high-risk QA execution status

Date: 2026-07-28  
Scope requested: nine high-risk manual/integration scenarios supplied for leave, payroll, tenant isolation, uploads, recruitment workflow, attendance locking, refresh tokens, and disabled accounts.

## Safety decision

No test account, company, employee, leave record, payroll record, applicant, resume, or email was created. YOPmail was not used.

The instruction to avoid changing any data conflicts with the supplied plan's mandatory setup of two companies and several employees in an isolated MongoDB database. The workspace has only development launch profiles and does not include a test-only application configuration, Docker compose file, or approved isolated MongoDB connection. Starting the API would resolve its configured MongoDB connection, which has not been demonstrated safe for this test run.

Accordingly, the nine scenarios were **not executed**. This is a safety blocker, not a passed test and not a product failure.

## Required safety checks not satisfied

| Required check | Status | Evidence |
| --- | --- | --- |
| Resolved MongoDB host printed and approved | Not performed | No approved test connection was provided. |
| Database name matches the required test/qa pattern | Not satisfied | No test database configuration exists in the workspace. |
| Environment is Testing | Not satisfied | `Codeji.CMS.API/Properties/launchSettings.json` contains Development profiles only. |
| Original databases excluded | Cannot verify safely | The application connection string was not used or exposed. |
| Synthetic data authorization | Not available | User instructed that no data be changed. |

## Execution order once unblocked

1. Leave approval/reconciliation partial failure
2. Concurrent payroll generation and process/edit race
3. Cross-company employee/profile IDOR
4. Public `cId` cross-tenant matrix
5. Resume content/MIME/token/download checks
6. Applicant transition matrix and concurrency
7. Locked attendance/payroll correction races
8. Refresh-token concurrency/reuse
9. Disabled user/company/permission revocation with existing tokens

## Existing, separate source-audit findings

No new runtime bug is reported in this document. The previously documented fresh source findings remain in `QA-BUG-REPORT-4.md` and were not duplicated here:

- a 401 from the refresh endpoint can deadlock the frontend retry queue;
- authenticated users lacking Jobs.View can call the internal vacancy-title endpoint.

Neither finding was exercised against a running application in this pass.

## Minimum unblocker

Provide explicit authorization to create and mutate **only** an isolated MongoDB database whose resolved name includes `test`, `testing`, `qa`, or `automation`, together with a Testing-only connection mechanism. Before any run, the host, database name, and `Testing` environment will be printed and checked. The database will not be deleted unless separately authorized after the same name guard passes.
