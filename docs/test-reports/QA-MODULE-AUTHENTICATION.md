# Authentication and account module

## Purpose

The account module registers companies, authenticates users, issues and rotates JWT/refresh tokens, logs out sessions, verifies email, and completes password-reset/invite onboarding.

## Frontend and backend entry points

- Frontend: `modules/auth/*`, `AuthHelpers.ts`, `Auth.tsx`, and `src/server/axios.ts`.
- Backend: `AccountController`, `AccountServices`, JWT setup in `Program.cs`, `CurrentContext`, and antiforgery middleware.

## Authentication and authorization lifecycle

Login requires an active employee, verified email, a role, app access, and a valid password. The JWT includes user, company, role, and role-name claims. The server validates issuer, audience, signature, and lifetime. Module-level authorization is then applied by controller attributes and the role handler. Refresh tokens are persisted as hashes and normal refresh rotates/revokes them.

## Tenant boundaries

For authenticated requests, company identity comes from the JWT claim. Unauthenticated account endpoints are intentionally among the named paths that may use a public tenant context, so their request models and service queries require isolated IDOR tests.

## Current tests

`AccountAuthTests.cs` covers login claims, invalid credentials, unverified email, normal refresh rotation, logout, and password-token consumption. These are pure/mock tests and do not verify the HTTP pipeline or browser behavior.

## Confirmed bug

`BUG-4-01` in `QA-BUG-REPORT-4.md`: the frontend can deadlock when its own refresh request receives 401.

## Potential risks and missing tests

- Tokens are held in local storage, increasing impact if a same-origin XSS occurs.
- Test stale permissions/role changes, disabled company/user, duplicated or missing claims, multiple sessions, cross-tab logout, and antiforgery failures.
- Verify the account endpoints that rely on an absent `[Authorize]` attribute cannot return authenticated data when a caller is anonymous.

## Recommended priority

Add a failing-refresh interceptor test first, then exercise the HTTP authentication/authorization pipeline in an isolated test host with no production configuration or database.
