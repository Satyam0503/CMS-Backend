# QA test-environment blocker

Date: 2026-07-28  
Status: **Runtime testing not started**

## Latest execution attempt

The user selected the isolated-database option. A subsequent local capability check found no relevant process environment variables (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__mongodb`, or Mongo-specific variables), no discoverable MongoDB Windows service, and no available `mongod`, `mongosh`, or `docker` command. Therefore no isolated QA database can be created locally from this workspace.

## Safety-check result

The mandatory preconditions for an isolated human-QA run are not available in the current shell/workspace:

| Guard | Result |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT=Testing` is set | Missing |
| `ConnectionStrings__mongodb` isolated connection is set | Missing |
| Testing-specific appsettings file or launch profile exists | Missing |
| Existing launch profiles are safe for this run | No - all profiles explicitly set `ASPNETCORE_ENVIRONMENT` to `Development` |
| Isolated database host/name can be printed and validated | Not possible without an approved test connection |

The application’s existing configured connection string was deliberately not read, printed, parsed, or used. That prevents accidental use of an existing database.

## Exact missing configuration

Provide these variables only in the process used for the QA run (not in tracked `appsettings.json`):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ConnectionStrings__mongodb = "mongodb://<isolated-host>:27017/codeji-cms-human-qa-test-<unique-run-id>"
```

The URI must point to a newly created, dedicated MongoDB database. It must not reuse an existing database or a shared Mongo host unless the database name guard below passes.

## Required database-name guard

Before starting the API or creating synthetic data, the resolved URI will be parsed without disclosing credentials and the following must all be true:

- environment exactly equals `Testing`;
- database name contains at least one of `test`, `testing`, `qa`, or `automation` (case-insensitive);
- database name does **not** contain `prod`, `production`, `stage`, `staging`, `dev`, or `development`;
- the target database name is new for this run, for example `codeji-cms-human-qa-test-20260728-<guid>`;
- resolved host and database name are printed for review before any data mutation.

No migrations will run unless explicitly authorized after these checks pass. The test database will not be deleted after the run unless separately authorized.

## Blocked scenarios

All nine requested runtime scenarios remain unexecuted and are neither passed nor failed:

1. Leave approval and attendance reconciliation partial failure
2. Concurrent payroll generation/process-edit behavior
3. Employee/profile cross-company IDOR
4. Public `cId` cross-tenant behavior
5. Resume MIME/content/token/download validation
6. Applicant stage-transition and concurrency behavior
7. Locked attendance/payroll correction races
8. Refresh-token concurrency and reuse
9. Disabled user/company/permission behavior with existing tokens

## Safe unblock procedure

1. Provide the two process-only variables above with a new QA database name.
2. Confirm the MongoDB host is an isolated QA-capable host and authorize use of that database only.
3. I will print the redacted host and database name, validate every guard, and stop if any guard fails.
4. Only then will I start the application in Testing and create synthetic run-scoped YOPmail identities and data.

No source, tracked configuration, existing database record, or external mailbox was modified while determining this blocker.
