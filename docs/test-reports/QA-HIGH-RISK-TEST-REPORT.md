# High-risk audit test report

Date: 2026-07-28

## Read-only execution

Command:

```powershell
dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj --no-restore --nologo
```

Result: 70 passed, 0 failed, 0 skipped.

The test project completed without a database connection or migration. The execution emitted dependency vulnerability warnings for AngleSharp 0.17.1, SharpCompress 0.30.1, and Snappier 1.0.0, plus existing nullable warnings in test sources. These are not classified as the fresh code defects in `QA-BUG-REPORT-4.md`.

## Coverage interpretation

The suite includes pure/mock checks for account token handling, career-portal token/sanitization/publication helpers, attendance/payroll rules, role permissions, tenant filter behavior, and career-profile validation. Passing it does not prove controller authorization, Axios interceptor behavior, two-tenant isolation, Mongo concurrency, uploads, email delivery, or browser flows.

## Unexecuted by design

- No API host or MongoDB integration test was started.
- No migration, background job, email, SignalR, upload, or database write was run.
- No frontend test was run because this pass identified an interceptor failure path that lacks a focused test; running the existing suite would not reproduce it without adding a test, which is outside the read-only scope.
