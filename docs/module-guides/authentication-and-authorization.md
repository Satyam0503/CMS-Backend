# Authentication and Authorization Module Guide

## Purpose
The authentication module covers tenant registration, sign-in, token management, password reset, and the permission gate used by the rest of the API.

## Core flow
1. A client calls one of the public auth endpoints in the account controller.
2. The controller validates the request body and delegates to the account service.
3. The account service validates credentials, writes or refreshes tokens, and returns a result wrapper.
4. Downstream controllers read user, role, and company claims from the current request context.

## Key components
- Controller: [Codeji.CMS.API/Controllers/AccountController.cs](../Codeji.CMS.API/Controllers/AccountController.cs)
- Service: [Codeji.CMS.Services/Account/AccountServices.cs](../Codeji.CMS.Services/Account/AccountServices.cs)
- Supporting helpers: [Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs)

## Main APIs
- POST /api/account/login
- POST /api/account/register
- POST /api/account/refresh-token
- POST /api/account/logout
- POST /api/account/CreateNewPassword
- POST /api/account/ResetPassword
- GET /api/account/verify-email

## Permission and security model
- Anonymous endpoints are used for sign-in, registration, and password recovery.
- Authenticated endpoints depend on JWT claims and the current tenant context.
- Module permissions are enforced by custom authorization middleware and the current request context helpers.

## Notes for contributors
- The auth controller is a central integration point and should remain thin.
- Prefer moving any hardcoded values, such as the antiforgery app key, to configuration.
