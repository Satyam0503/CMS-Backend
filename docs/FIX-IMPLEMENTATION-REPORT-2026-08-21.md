# CMS Issues Fix Implementation Report

**Date:** 2026-08-21
**Scope:** Local source changes in `CMS-Backend-Core` and `CMS-React`
**Deployment status:** Not committed, pushed, or deployed.

## Summary

| Status | Count |
|---|---:|
| Completed code fixes | 11 |
| Partially completed findings | 0 |
| Still open / needs external work | 20 |

## Completed Fixes

| Finding | Implementation | Changed files |
|---|---|---|
| Rate limiting on sensitive endpoints | Added `account-sensitive` rate limiting for login and password reset. Applied `career-sensitive` limits to job applications and resume uploads. | `Codeji.CMS.API/Program.cs`, `Controllers/AccountController.cs`, `Controllers/ApplicantsController.cs`, `Controllers/PublicCareerController.cs` |
| SignalR hardening | Restricted inbound hub payloads to 64 KB. Detailed hub errors are enabled only in Development. | `Codeji.CMS.API/Program.cs` |
| Mongo connection-pool duplication | Repository infrastructure now receives the DI-managed `IMongoClient`; it no longer creates additional clients for normal application requests. | `MongoDbCacheService.cs`, `MongoDbContext.cs`, `MongoRepository.cs` |
| Mongo-unsafe resume entity | Removed `IFormFile File` from the persisted `Resume` entity. Upload inputs remain DTO/controller concerns. | `Entities/Recruitments/Resume.cs` |
| Weekly-off document identifier | Added Mongo `[BsonId]` to `WeeklyOffSetting.Id`. | `Entities/Attendance/WeeklyOffSetting.cs` |
| Leave-settings document identifier | Added Mongo `[BsonId]` to `LeaveSettings.Id`. | `Entities/Company/LeaveSettings.cs` |
| Company creation audit field | Added `CreatedBy` to the Company entity. | `Entities/Company/Company.cs` |
| Company audit/soft-delete participation | `Company` now implements the repository auditing and soft-delete contracts. | `Entities/Company/Company.cs` |
| Duplicate startup registrations | Removed duplicate attendance repository/service registrations from API startup. | `Codeji.CMS.API/Program.cs` |
| CI quality gates | Backend test failures now fail the Azure pipeline. The frontend pipeline uses Node 20, `npm ci`, lint, typecheck, and tests. | `backend-pipeline.yml`, `CMS-React/frontend-pipeline.yml` |
| Service test project in solution | Added `Codeji.CMS.Services.Tests` to `CodejiCMSCore.sln`. | `CodejiCMSCore.sln` |

## Validation

- Isolated build: `dotnet build Codeji.CMS.API/Codeji.CMS.API.csproj --no-restore -p:BaseOutputPath=C:\\tmp\\codeji-verify\\`
- Result: **passed with 0 errors**.
- Existing compiler warnings: **503**.
- Service-test run: **140 passed, 5 failed**. The five failures are all existing `EmployeeServiceBulkImportTests` assertions and are unrelated to the Company contract or solution inclusion changes.
- A normal-output API build was blocked because the running local API process held DLL locks; the isolated build avoided that lock.
- Diff whitespace/conflict checks passed.
- Existing local Playwright run: **87 passed, 3 skipped, 0 failed**. This was executed before the new backend binaries were loaded, so it does not validate the new rate-limit or SignalR settings at runtime.

## Remaining Findings Requiring Separate Authority or Design

The following must not be treated as completed by local source edits alone:

1. Rotate MongoDB, JWT, SMTP, and reCAPTCHA secrets; then remove them from Git history and place replacement values in secure deployment configuration.
2. Design and execute data migrations for payroll, salary, employee date fields, notifications, and entity audit inheritance.
3. Implement a Mongo transaction/outbox strategy for multi-document business workflows.
4. Complete route-permission, server-state, API-contract, console-log, lint-rule, and dependency modernization work.
5. Create and configure the production Azure environment, approvals, health checks, rollback process, and coordinated deployment.

## Local Change Boundaries

- No credentials were printed, saved, rotated, or committed.
- No production, DEP, TEST, or database changes were made.
- No existing unrelated React or QA worktree changes were modified.
