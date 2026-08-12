# Account and Authentication APIs

## Controller
- Source: [Codeji.CMS.API/Controllers/AccountController.cs](../Codeji.CMS.API/Controllers/AccountController.cs)

## Overview
This controller handles public authentication, company onboarding, password recovery, and session renewal. It is one of the few controllers that mixes anonymous and authenticated endpoints in the same surface.

## Endpoint map

### Public identity and app checks
- GET /api/account/antiforgerytoken/{appKey}
  - Purpose: returns request/cookie antiforgery tokens for the supplied app key.
  - Auth: AllowAnonymous.
  - Flow: validates the hardcoded app key, calls the antiforgery middleware, and returns a result wrapper.

- GET /api/app/checkAppVersion
  - Purpose: checks whether the client app version matches the configured server version.
  - Auth: AllowAnonymous.
  - Notes: returns a success flag and a message when the version is outdated.

### Company registration and login
- POST /api/account/register
  - Purpose: registers a new tenant/company and its initial admin user.
  - Auth: public.
  - Flow: validates that the email is not already in use, then delegates to the company registration service.

- POST /api/account/login
  - Purpose: authenticates a user and issues JWT and refresh tokens.
  - Auth: AllowAnonymous.
  - Flow: validates model state, then calls the account service to verify credentials and generate the token pair.

- POST /api/account/refresh-token
  - Purpose: exchanges a valid refresh token for a new access token.
  - Auth: AllowAnonymous.
  - Flow: validates request data and delegates to the token refresh service.

### Session and profile endpoints
- POST /api/account/logout
  - Purpose: invalidates the provided refresh token for the current user.
  - Auth: Authorized.
  - Flow: reads the current user from the JWT context and calls the logout service.

- POST /api/account/getSignedUserDetails
  - Purpose: returns the signed-in user’s profile metadata and context.
  - Auth: Authorized.
  - Flow: resolves the current user ID, role ID, and company ID from the request context and then loads the user details.

### Password recovery and email verification
- POST /api/account/CreateNewPassword
  - Purpose: sets a new password after a reset token is validated.
  - Auth: AllowAnonymous.

- GET /api/account/verify-email
  - Purpose: verifies a user email token and redirects the browser to the login page on success or failure.
  - Auth: AllowAnonymous.

- POST /api/account/resend-email-verification
  - Purpose: resends the email verification message.
  - Auth: AllowAnonymous.

- POST /api/account/ResetPassword
  - Purpose: initiates a password reset flow by email.
  - Auth: AllowAnonymous.

### Applicant application entry point
- POST /api/applicant/applyJob
  - Purpose: public applicant submission for jobs.
  - Auth: AllowAnonymous.
  - Flow: validates the model and delegates to the applicant service.

## Service dependencies
- IAccountServices
- IEmployeeService
- ICompanyService
- IApplicantsService
- IMiddlewareService
- IPriorityTaskQueue

## Implementation notes
- This controller is responsible for the first authentication boundary in the system.
- Several flows depend on the shared CurrentContext helpers for tenant and user resolution.
- The antiforgery endpoint uses a hardcoded app key and should eventually move to configuration.
