# QA Bug Report 4 - high-risk module audit, fresh findings only

Date: 2026-07-28  
Scope: Read-only source audit of authentication, tenant isolation, employees, leave, attendance, payroll, recruitment, and public careers. No database connection, mutation, source edit, or configuration edit was performed.

This report deliberately excludes BUG-01 through BUG-05, BUG-2-01 through BUG-2-04, and the items described as addressed in `QA-BUG-REPORT-3.md`.

## Confirmed bugs

### BUG-4-01 - A rejected refresh-token request can leave all queued requests pending indefinitely

Severity: Medium  
Area: Authentication/session recovery  
Affected file: `CMS-React/src/server/axios.ts`

Evidence and source reproduction:

1. `refreshToken()` sets the module-level `isRefreshing` flag before calling `refreshTokenAsync()`.
2. If `/account/refresh-token` returns HTTP 401, that request goes through the same response interceptor.
3. Because `isRefreshing` is already true, the interceptor enters the `401 && isRefreshing` branch and returns a new Promise after adding the refresh request itself to `failedRequestsQueue`.
4. That Promise has no resolving or rejecting path: only a successful refresh calls the queue callbacks, but the refresh call is now awaiting its own queued Promise. Consequently `refreshToken()` cannot reach its `catch`/`finally` cleanup.

Expected: an invalid/expired refresh token rejects the refresh operation, clears the local session once, and settles all queued requests with a failure.

Actual: the refresh request can join its own queue and remain pending; requests already queued behind it also remain pending.

Impact: the UI may stay in an indefinite loading state after a session expires instead of redirecting to login.

Safe remediation direction: bypass refresh/retry logic for the refresh endpoint itself and reject queued requests when refresh fails. Add an interceptor-level test for a 401 from the refresh endpoint.

### BUG-4-02 - Any authenticated tenant user can read an internal job title without Jobs.View permission

Severity: Medium  
Area: Recruitment authorization  
Affected files:

- `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs`

Evidence and source reproduction:

1. `POST /api/JobVacancy/GetVacancyById` is inside an `[Authorize]` controller, but has no `ModulePermission(AppModule.Jobs, Permission.View)` attribute.
2. Its service call resolves the supplied `vacancyId` and returns `data.Title` when a same-tenant vacancy exists.
3. Other read endpoints in this controller and the frontend route require Jobs permission, but this endpoint checks only authentication.

Expected: a caller without `Jobs.View` receives an authorization failure and cannot resolve internal vacancy metadata.

Actual: any authenticated user in the tenant can obtain the title of a vacancy whose ID they know or guess.

Impact: module permission enforcement is bypassed for internal recruitment metadata. The current generic repository tenant filter limits this finding to the caller's tenant; it does not mitigate the missing module authorization.

Safe remediation direction: apply the same Jobs.View policy used by other vacancy reads and add an authorization test for a user who is authenticated but lacks that permission.

## Validation boundaries

- No live tenant, MongoDB, email, file-storage, or browser testing was used. Findings are source-supported, not runtime reproductions.
- The prior `cId` tenant-header finding was not repeated. Current `CurrentContext.CompanyId` limits that fallback to named public/anonymous paths; each such public flow still needs isolated runtime IDOR testing.
- Payroll, leave reconciliation, and background jobs remain multi-document workflows. Their concurrency and rollback behavior requires an isolated MongoDB test environment before it can be classified as a confirmed defect.
