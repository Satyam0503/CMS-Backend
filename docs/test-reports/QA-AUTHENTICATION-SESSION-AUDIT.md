# Authentication and session audit

## Scope and architecture

The API validates JWT issuer, audience, lifetime, and signing key in `Codeji.CMS.API/Program.cs`. Login in `AccountServices.VerifyAndGenerateToken` creates an access JWT plus a SHA-256-hashed refresh-token record. Refresh rotates the token and revokes the previous record; logout scopes revocation to the current user. Password reset and invite tokens are hashed and marked used after password creation.

The React client stores both access and refresh tokens in local storage (`modules/auth/core/AuthHelpers.ts`) and sends the access token through the Axios request interceptor. This creates an XSS blast-radius risk by design, but it is a security design risk rather than a newly confirmed exploit.

## Confirmed bug

`BUG-4-01` in `QA-BUG-REPORT-4.md`: the Axios 401 handler queues the refresh request behind itself if the refresh endpoint returns 401, leaving requests pending indefinitely.

## Confirmed controls

- Access-token validation enables issuer, audience, lifetime, and signing-key checks.
- Refresh tokens are stored hashed and rotated on a successful refresh.
- Logout binds a supplied refresh token to the authenticated user.
- Password-reset enumeration is reduced by returning an accepted-style response for an unknown email.

## Needs validation

- Whether disabled users, deleted users, disabled companies, or role changes invalidate an already-issued access token before expiry.
- Refresh-token concurrency and reuse detection under two simultaneous refresh requests.
- Browser behavior for cross-tab logout and the failed-refresh redirect.
- Whether every state-changing endpoint receives the required antiforgery token from the frontend.

## Current tests and gaps

`AccountAuthTests.cs` covers valid/invalid login, hashed refresh-token persistence, normal rotation, logout, and password-token consumption. It does not exercise the browser interceptor, refresh failure, multi-session revocation, claims changes after issuance, or endpoint middleware ordering.

## Priority

1. Fix and test the failed-refresh queue deadlock.
2. Add isolated tests for concurrent refresh/reuse and account/company disablement.
3. Reassess local-storage token storage as part of an XSS threat-model decision.
