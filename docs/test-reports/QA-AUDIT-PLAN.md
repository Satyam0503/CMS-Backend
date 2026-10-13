# QA Audit Plan — Step 1

Date: 2026-07-27  
Scope: Read-only inspection only. No source code changes, no database changes, no configuration changes, and no package installs.

## 1) Solution overview

This backend is a multi-project ASP.NET Core / .NET solution centered on the `Codeji.CMS.API` web app and a shared service/repository architecture.

### Top-level modules / applications

- `Codeji.CMS.API`
  - Main ASP.NET Core Web API
  - Hosts controllers, middleware, SignalR hubs, static templates, and app startup
- `Codeji.CMS.Services`
  - Business logic layer
  - Background tasks, calculators, domain services, registration helpers
- `Codeji.CMS.Repository`
  - MongoDB access layer
  - Entities, repository abstractions, filters, cache/context helpers
- `Codeji.CMS.DTO`
  - Request/response models shared by API and services
- `Codeji.CMS.Utility`
  - Cross-cutting helpers: sanitization, auth helpers, config access, templates, enums
- `Codeji.CMS.Migrations`
  - Migration runner and data seed / repair migrations
- `Codeji.CMS.Services.Tests`
  - Existing xUnit test project

## 2) Existing test coverage

### Test project found

- `Codeji.CMS.Services.Tests`

### Test-related packages

- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`
- `Moq`

### Existing test files

- `AccountAuthTests.cs`
- `AttendancePayrollRulesTests.cs`
- `CareerPortalSecurityTests.cs`
- `PayrollCalculationRulesTests.cs`
- `PayrollPeriodRulesTests.cs`
- `RequestContextAndTemplatingTests.cs`
- `RolePermissionTests.cs`
- `TenantIsolationFilterTests.cs`
- `UnitTest1.cs`

### Initial coverage impression

The current tests appear focused on:

- authentication and token handling
- tenant/company isolation filters
- role/permission checks
- request context and templating
- payroll rule logic
- public career portal security

That is a good start, but coverage still looks concentrated in service-level behavior. I did not yet see evidence of broad API/controller integration tests, repository integration tests, or end-to-end workflows.

## 3) Build commands

The solution targets `.NET 10.0` according to `global.json`.

Likely build commands:

- Restore all projects:
  - `dotnet restore CodejiCMSCore.sln`
- Build solution:
  - `dotnet build CodejiCMSCore.sln`
- Build a single project:
  - `dotnet build Codeji.CMS.API\Codeji.CMS.API.csproj`
- Run tests later, when approved:
  - `dotnet test CodejiCMSCore.sln`
  - or `dotnet test Codeji.CMS.Services.Tests\Codeji.CMS.Services.Tests.csproj`

I have not run the full test suite yet, per instruction.

## 4) Risky areas to inspect first

These are the areas most likely to hide defects or regressions:

### Authentication / authorization

- JWT creation, refresh-token exchange, password reset, and antiforgery token flow
- Module permission enforcement through custom middleware / attributes
- Hardcoded values in auth flow, especially anything that should be config-driven

### Tenant isolation / data scoping

- MongoDB filtering by company / tenant
- Any code path that bypasses default filters
- Direct collection access instead of repository helpers

### Database and migration behavior

- Migration runner, seeders, and repair migrations
- Any code touching MongoDB indexes, collection names, or cross-tenant updates
- Data repair scripts that may be safe in one tenant but harmful in another

### Payroll / attendance / leave

- Payroll calculation rules, period boundaries, accrual logic, and attendance aggregation
- Any rule involving month boundaries, time zones, or partial-day logic
- These areas are usually high-risk because they combine business logic with persisted state

### Public career / recruitment

- Public application routes and tokenized resume upload paths
- File handling, validation, and authorization handoff
- Security-sensitive because public endpoints may bypass normal authentication

### Cross-cutting infrastructure

- Sanitization logic for HTML input
- Mail template rendering
- Middleware order
- SignalR hub access and request context propagation

## 5) Likely bug-prone implementation notes from inspection

These are not confirmed defects yet, but they are strong audit candidates:

1. Hardcoded antiforgery app key is present in docs and code references.
2. Some infrastructure uses synchronous blocking patterns around async work.
3. Sanitization code has a silent failure pattern.
4. Security header setup may be sensitive to duplicate header insertion.
5. Several service paths are documented as N+1 query patterns and may degrade under load.
6. Background services and migration scripts can affect persisted state, so they need special care.

## 6) Planned QA test cases for the next phase

I would validate the following before calling the backend stable:

### Auth

- login with valid / invalid credentials
- refresh token rotation and expiry handling
- forgot-password and reset-password flow
- antiforgery token request and state-changing requests
- permission denial for unauthorized module access

### Tenant isolation

- company A cannot read or modify company B records
- queries with and without default filters behave as expected
- soft-deleted records are hidden where intended

### Payroll / attendance

- payroll for month boundaries
- attendance in/out calculation
- leave accrual and leave balance updates
- salary slip generation for edge cases

### Public career

- public vacancy visibility rules
- application submission with file upload
- tokenized resume access / invalid token rejection

### Cross-cutting

- HTML sanitization removes unsafe content
- mail template rendering substitutes placeholders correctly
- SignalR notification paths do not leak cross-tenant data

## 7) What I will do next, once allowed

1. Inspect the source paths behind the risky areas above.
2. Review current tests for coverage gaps.
3. Run a targeted set of non-destructive tests only.
4. Document confirmed defects in a separate bug report with:
   - title
   - severity
   - affected module
   - reproduction steps
   - expected vs actual behavior
   - evidence / file references

## 8) Current status

Read-only audit plan created. No tests were executed in this step.

