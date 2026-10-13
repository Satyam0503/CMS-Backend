# Authentication module and tenant-isolation test report

**Date:** 2026-08-13  
**Scope:** company registration, email verification, login, refresh, logout, forgot-password, create-password, and signed-user bootstrap.  
**Mode:** source audit and isolated unit tests only. No production/shared-database records, email logs, tokens, passwords, or company data were created, changed, or deleted.

## Result summary

| Area | Result | Evidence |
|---|---|---|
| Account service unit suite | Passed | `dotnet test Codeji.CMS.Services.Tests/Codeji.CMS.Services.Tests.csproj --filter "FullyQualifiedName~AccountAuthTests" --no-restore`: 10/10 passed |
| Login credential and role gates | Passed by source and unit test | User must be active and verified; role must match both `RoleId` and the user's `CompanyId`; only an app-enabled, non-deleted role can issue a token. |
| JWT tenant claims | Passed by unit test | Successful login JWT has `user_id`, `company_id`, and `role_id` claims; account test asserts all three. |
| Refresh-token rotation | Passed by unit test | Refresh token is stored hashed, replacement is generated, and the prior token is revoked. |
| Logout ownership | Passed by source and unit test | Logout filters stored refresh token by both token hash and current JWT `user_id`. |
| Email verification / reset / registration live flow | Not executed | Current `appsettings.json` points to a shared MongoDB and live SMTP. Executing these workflows would mutate records and send email. |
| Browser/API authorization and two-company E2E | Not executed | Requires an isolated database named with `test`, `qa`, or `automation`, two synthetic companies, and mailbox capture. |

## Auth flow traced

1. `POST /api/account/register` calls `CompanyService.Register`.
   It creates a new company ID, company-specific default roles, a registering employee, company-specific attendance defaults, verification token/email, and default company data.
2. `GET /api/account/verify-email?token=...` hashes and consumes a one-time email-verification token, then marks the matching active user verified.
3. `POST /api/account/login` validates the active, verified employee; its active same-company role; app access; and BCrypt password. It returns an access token and a hashed-at-rest refresh token.
4. `POST /api/account/refresh-token` resolves the refresh token, validates its owner, then validates the user's same-company app-enabled role before rotating both tokens.
5. `POST /api/account/logout` requires JWT authentication and can revoke only a refresh token owned by the JWT user.
6. `POST /api/account/ResetPassword` requests a reset email; `POST /api/CreateNewPassword` consumes a one-time reset/invite token, changes the user password, and invalidates remaining reset/invite tokens for that user.

## Tenant-isolation assessment

### Verified by source/tests

| Boundary | Assessment |
|---|---|
| Login role lookup | Isolated: `AccountServices.VerifyAndGenerateToken` requires `role.RolesId == user.RoleId`, `role.CompanyId == user.CompanyId`, and `!role.IsDeleted`. A role from another company cannot authorize login. |
| Access-token claims | Isolated: the server generates `company_id` from the authenticated employee, never from client payload. |
| Refresh token | Isolated: refresh resolves the token's stored user, derives that user's company, then validates the role under that same company. The request has no client-supplied company ID. |
| Logout | Isolated: the service lookup includes the token hash and JWT-derived user ID, preventing one authenticated user from revoking another user's token. |
| Verify/create-password | Isolated by opaque token ownership: the user is resolved from the server-side token record; client input never supplies a user or company ID. |
| Registration | Allocates a new GUID company ID and passes it into role/default provisioning. It does not accept a client company ID. |

### E2E checks still required

Run these only in an isolated environment with companies A and B and synthetic accounts:

| Test | Expected result |
|---|---|
| Login A, call signed-user endpoint with A token | Only A user and A company data are returned. |
| Use A refresh token while attempting B context | Token rotates only for A's stored user; no B claims or data are issued. |
| Use A token to log out B's refresh token | Rejected; B token remains usable. |
| Verify A email token / reset A password token | Only A account changes; B account, tokens, and password remain unchanged. |
| Register two synthetic companies with distinct emails | Roles, default data, verification tokens, and employees are all company-specific. |
| Retry registration / force each downstream write or email failure | No orphaned roles, company, user, or token remains. |

## Confirmed defects

### AUTH-001 — Password reset lookup is case-sensitive

**Severity:** Medium  
**Status:** Confirmed by source inspection  
**Location:** `Codeji.CMS.Services/Account/AccountServices.cs:153`

Login uses a case-insensitive email comparison (`StringComparison.OrdinalIgnoreCase` at line 83), but `GenerateTokenAndSendEmail` uses `x.Email == email`. Therefore a valid account can log in with a casing variation but a forgot-password request with that variation is accepted without issuing the reset email. The endpoint intentionally returns an accepted response for absent users, making this appear successful to the user.

**Fix recommendation:** normalize email comparison in the reset lookup to the same case-insensitive comparison used by login, registration, and resend-verification. Add a unit test using a stored mixed-case email and a lower-case request.

### AUTH-002 — Create-password request failures are invisible in the UI

**Severity:** Medium  
**Status:** Confirmed by source inspection  
**Location:** `CMS-React/src/app/modules/auth/components/CreatePassword.tsx:76-77`

The catch block only writes to the browser console. Network, API, or server errors leave the user on the form without a visible error, so an expired-link or delivery/API failure can look like a non-responsive submit.

**Fix recommendation:** surface a translated error/toast in the catch block, preserving a generic safe message for transport failures.

### AUTH-003 — Registration is not atomic and ignores several downstream write results

**Severity:** High  
**Status:** Confirmed by source inspection; runtime failure path not executed  
**Location:** `Codeji.CMS.Services/Companies/CompanyService.cs:91-171`

Default roles are created before the company; after the company insert succeeds, employee, notification preference, attendance defaults, verification email/token, and seed writes are awaited without checking or compensating their individual results. A failure can leave a company without its administrator, or role/default records with no fully usable company. This risks broken onboarding and complicates tenant recovery.

**Fix recommendation:** use a MongoDB transaction where deployment supports it, or implement explicit compensating actions and fail the registration response if a required company/admin identity write fails. Add failure-injection tests for every post-company write.

### AUTH-004 — Signed-user bootstrap endpoint is not explicitly authenticated

**Severity:** Medium  
**Status:** Confirmed by source inspection  
**Location:** `Codeji.CMS.API/Controllers/AccountController.cs:171-187`

`POST /api/account/getSignedUserDetails` reads JWT-derived identity but has no `[Authorize]` attribute, while logout explicitly does. With no global fallback authorization policy configured, this endpoint is anonymously reachable and relies on downstream claim handling rather than an authorization boundary.

**Fix recommendation:** add `[Authorize]`, then add an API integration test proving an anonymous request returns 401 and a tenant A token cannot obtain tenant B data.

## Existing test coverage and gaps

`AccountAuthTests` currently covers valid/invalid/unverified login, app-access rejection, verification resend delivery paths, refresh rotation, logout, and selected create-password behavior. It does **not** cover company registration, successful email verification, reset-email case-insensitivity, expired/used token branches, refresh replay/concurrency, controller authorization, browser error display, or two-company E2E isolation.

## Test environment requirement before live execution

Use `ASPNETCORE_ENVIRONMENT=Testing`, a separate MongoDB database whose name contains `test`, `qa`, or `automation`, synthetic mail capture, and two dedicated synthetic companies. Do not run migrations, cleanup, registration, verification, reset, or token-replay tests against the currently configured shared database without explicit approval.
