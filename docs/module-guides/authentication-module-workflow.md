# Authentication Module Workflow

## Scope
The authentication module covers the first boundary of the platform: tenant registration, user sign-in, token issuance, refresh, logout, password recovery, and session validation. It is the bridge between public access and the protected business modules.

## Architectural role
This module sits at the top of the request lifecycle. Before any business module can safely operate, the request must be authenticated and the current tenant context must be resolved. The controller is deliberately thin and delegates the real logic to the service layer.

## Main entry points
- Controller: [Codeji.CMS.API/Controllers/AccountController.cs](../Codeji.CMS.API/Controllers/AccountController.cs)
- Service: [Codeji.CMS.Services/Account/AccountServices.cs](../Codeji.CMS.Services/Account/AccountServices.cs)
- Supporting helpers: [Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs)

## End-to-end flow
1. A client calls a public endpoint such as login, registration, refresh-token, or password reset.
2. The controller validates the incoming DTO and checks model state.
3. The account service verifies credentials, creates or refreshes tokens, and returns a standardized result object.
4. The response includes auth metadata and, for login, the JWT and refresh token pair.
5. Every later request uses the JWT to resolve user, role, company, and module permissions from the request context.

## Detailed behavior
### Registration flow
- The registration endpoint validates the company email and prevents duplicate tenants from being created.
- The company and its initial admin profile are created through the company and employee services.
- Default roles, departments, and job titles are seeded as part of tenant onboarding.

### Login flow
- The login endpoint accepts an email/password model.
- Credentials are verified by the account service.
- If valid, the service issues a JWT token and a refresh token tied to the active user and company context.

### Token refresh flow
- The refresh-token endpoint accepts a refresh token payload.
- If the token is valid and not expired, the service returns a new access token.
- This keeps the user session alive without requiring another password challenge.

### Password recovery flow
- The reset and create-new-password endpoints accept recovery tokens and new passwords.
- The service validates the token and updates the password hash if the token is still valid.
- Email verification and resend flows are also handled here.

## Dependencies
- Employee service: used to validate email existence and retrieve signed-in user details.
- Company service: used during tenant registration.
- Middleware service and antiforgery helpers: used for token and request security.
- CurrentContext helpers: used to resolve the authenticated user, role, and company from the HTTP context.

## Core business rules
- Public endpoints must remain accessible without a user session.
- Protected endpoints depend on the auth context rather than trusting client data.
- Password and token operations must stay inside the service layer to avoid leaking security logic into the controller.

## Integration points with other modules
- Employee management depends on authentication to know who is signed in.
- Leave, attendance, payroll, and company modules all rely on the current user/company claims.
- Role-based permissions and module permissions are evaluated after auth context is established.
- The authentication context also controls tenant scoping for data access, which makes this module foundational for multi-tenancy.

## Request and response shape
- Login returns a token payload that includes the signed-in user’s identity and related security metadata.
- Refresh-token operations return a fresh access token without requiring the user to re-enter credentials.
- Password-reset and verification responses are typically slim result wrappers that indicate success or failure and the relevant status code.

## Failure modes and edge cases
- Invalid credentials return a failed result rather than throwing.
- Missing or malformed refresh tokens should result in a failed authentication outcome.
- Verification and password-reset flows may fail if the token is expired, missing, or already used.
- The antiforgery endpoint relies on a hardcoded app key, which is a security-sensitive point for future hardening.

## Contributor checklist
- Keep controller code thin and move business logic into the service layer.
- Preserve tenant-specific behavior and do not trust the client to supply company or user values.
- Verify login, refresh, logout, and protected-route access behavior whenever changing auth logic.

## Operational notes
- The antiforgery token endpoint uses a hardcoded app key and should eventually be moved to configuration.
- Any change in auth behavior should be verified against login, refresh, and protected-module access paths.
