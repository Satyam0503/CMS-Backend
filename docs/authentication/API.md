# Authentication and Authorization API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `GET` | `api/account/antiforgerytoken/{appKey}` | `AccountController` | `GetAntiForgeryToken` | `_antiforgery.GetAndStoreTokens()` |
| 2 | `GET` | `api/app/checkAppVersion` | `AccountController` | `CheckAppVersion` | `_employeeService.IsEmailExist()` |
| 3 | `POST` | `api/account/register` | `AccountController` | `Register` | `_employeeService.IsEmailExist()` |
| 4 | `POST` | `api/account/login` | `AccountController` | `Login` | `_accountService.VerifyAndGenerateToken()` |
| 5 | `POST` | `api/account/refresh-token` | `AccountController` | `RefreshToken` | `_accountService.RefreshToken()` |
| 6 | `POST` | `api/account/logout` | `AccountController` | `LogOut` | `string.IsNullOrEmpty()` |
| 7 | `POST` | `api/account/getSignedUserDetails` | `AccountController` | `GetUserByToken` | `CurrentContext.UserId()` |
| 8 | `POST` | `api/CreateNewPassword` | `AccountController` | `CreateNewPassword` | `_accountService.CreateNewPassword()` |
| 9 | `GET` | `api/account/verify-email` | `AccountController` | `VerifyEmail` | `_accountService.VerifyEmail()` |
| 10 | `POST` | `api/account/resend-email-verification` | `AccountController` | `ResendEmailVerification` | `_accountService.ResendEmailVerification()` |
| 11 | `POST` | `api/account/ResetPassword` | `AccountController` | `ResetPassword` | `body.GetString()` |
| 12 | `POST` | `api/applicant/applyJob` | `AccountController` | `RegisterApplicants` | `_applicantsServices.ApplyNowService()` |
| 13 | `GET` | `api/Roles/GetRoles` | `RolesController` | `GetRoles` | `CurrentContext.CompanyId()` |
| 14 | `GET` | `api/Roles/GetRolePermission` | `RolesController` | `GetRolePermission` | `CurrentContext.CompanyId()` |
| 15 | `POST` | `api/Roles/AddEditRole` | `RolesController` | `AddRole` | `CurrentContext.CompanyId()` |
| 16 | `GET` | `api/Roles/GetAllRolesWithPermission` | `RolesController` | `GetModulePermission` | `CurrentContext.CompanyId()` |
| 17 | `GET` | `api/Roles/GetRoleWithPermissionById` | `RolesController` | `GetModulePermissionyId` | `CurrentContext.CompanyId()` |
| 18 | `GET` | `api/Roles/GetRoleById/{roleId}` | `RolesController` | `GetRoleById` | `CurrentContext.CompanyId()` |
| 19 | `PATCH` | `api/Roles/UpdateAppAccessForRole/{roleId}` | `RolesController` | `UpdateAppAccessForRole` | `_roleService.UpdateAppAccessForRole()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. GET api/account/antiforgerytoken/{appKey}

### Purpose

This endpoint is implemented by `AccountController.GetAntiForgeryToken()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `GetAntiForgeryToken()`
- Approximate source line at generation: 75
- Direct delegated call found: `_antiforgery.GetAndStoreTokens()`

### HTTP contract

- Method: `GET`
- Route: `api/account/antiforgerytoken/{appKey}`
- Return declaration: `Result<AntiForgeryResponse>`
- Parameters exactly as declared: `string appKey`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpGet] [Route("account/antiforgerytoken/{appKey}")] [AllowAnonymous]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string appKey`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/account/antiforgerytoken/{appKey}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.GetAntiForgeryToken()` receives the bound values.
4. The action delegates to `_antiforgery.GetAndStoreTokens()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Result<AntiForgeryResponse>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 2. GET api/app/checkAppVersion

### Purpose

This endpoint is implemented by `AccountController.CheckAppVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `CheckAppVersion()`
- Approximate source line at generation: 99
- Direct delegated call found: `_employeeService.IsEmailExist()`

### HTTP contract

- Method: `GET`
- Route: `api/app/checkAppVersion`
- Return declaration: `Result`
- Parameters exactly as declared: `string appVersion`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpGet] [Route("app/checkAppVersion")] [AllowAnonymous]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string appVersion`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/app/checkAppVersion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.CheckAppVersion()` receives the bound values.
4. The action delegates to `_employeeService.IsEmailExist()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Result`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 3. POST api/account/register

### Purpose

This endpoint is implemented by `AccountController.Register()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `Register()`
- Approximate source line at generation: 113
- Direct delegated call found: `_employeeService.IsEmailExist()`

### HTTP contract

- Method: `POST`
- Route: `api/account/register`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] CompanyRequestModel companyModel`

### Authentication and authorization

No action-level conclusion is safe; include inherited/base-controller and middleware behavior in verification.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpPost] [Route("account/register")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CompanyRequestModel companyModel`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/register`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.Register()` receives the bound values.
4. The action delegates to `_employeeService.IsEmailExist()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 4. POST api/account/login

### Purpose

This endpoint is implemented by `AccountController.Login()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `Login()`
- Approximate source line at generation: 134
- Direct delegated call found: `_accountService.VerifyAndGenerateToken()`

### HTTP contract

- Method: `POST`
- Route: `api/account/login`
- Return declaration: `async Task<Result<TokenResponseDto>>`
- Parameters exactly as declared: `[FromBody] LoginModel model`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpPost] [Route("account/login")] [AllowAnonymous] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] LoginModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/login`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.Login()` receives the bound values.
4. The action delegates to `_accountService.VerifyAndGenerateToken()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `VerifyAndGenerateToken()` at `Codeji.CMS.Services/Account/AccountServices.cs:80`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Active, NoticePeriod.
- Method match: `VerifyAndGenerateToken()` at `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:12`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<TokenResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 5. POST api/account/refresh-token

### Purpose

This endpoint is implemented by `AccountController.RefreshToken()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `RefreshToken()`
- Approximate source line at generation: 149
- Direct delegated call found: `_accountService.RefreshToken()`

### HTTP contract

- Method: `POST`
- Route: `api/account/refresh-token`
- Return declaration: `async Task<Result<TokenResponseDto>>`
- Parameters exactly as declared: `[FromBody] RefreshTokenRequestDto model`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/refresh-token")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RefreshTokenRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/refresh-token`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.RefreshToken()` receives the bound values.
4. The action delegates to `_accountService.RefreshToken()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RefreshToken()` at `Codeji.CMS.Services/Account/AccountServices.cs:387`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `RefreshToken()` at `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:13`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<TokenResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 6. POST api/account/logout

### Purpose

This endpoint is implemented by `AccountController.LogOut()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `LogOut()`
- Approximate source line at generation: 163
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/account/logout`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] RefreshTokenRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpPost] [Route("account/logout")] [Authorize]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RefreshTokenRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/logout`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.LogOut()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 7. POST api/account/getSignedUserDetails

### Purpose

This endpoint is implemented by `AccountController.GetUserByToken()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `GetUserByToken()`
- Approximate source line at generation: 176
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/account/getSignedUserDetails`
- Return declaration: `async Task<Result<LoginUserViewModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

No action-level conclusion is safe; include inherited/base-controller and middleware behavior in verification.

- Controller/action attributes: `[ApiController] [Route("api")] [HttpPost] [Route("account/getSignedUserDetails")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/getSignedUserDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.GetUserByToken()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<LoginUserViewModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 8. POST api/CreateNewPassword

### Purpose

This endpoint is implemented by `AccountController.CreateNewPassword()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `CreateNewPassword()`
- Approximate source line at generation: 197
- Direct delegated call found: `_accountService.CreateNewPassword()`

### HTTP contract

- Method: `POST`
- Route: `api/CreateNewPassword`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `CreateNewPasswordRequest model`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [Route("CreateNewPassword")] [Route("account/CreateNewPassword")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CreateNewPasswordRequest model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/CreateNewPassword`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.CreateNewPassword()` receives the bound values.
4. The action delegates to `_accountService.CreateNewPassword()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateNewPassword()` at `Codeji.CMS.Services/Account/AccountServices.cs:239`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CreateNewPassword()` at `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:16`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 9. GET api/account/verify-email

### Purpose

This endpoint is implemented by `AccountController.VerifyEmail()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `VerifyEmail()`
- Approximate source line at generation: 208
- Direct delegated call found: `_accountService.VerifyEmail()`

### HTTP contract

- Method: `GET`
- Route: `api/account/verify-email`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] string token`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [HttpGet] [Route("account/verify-email")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string token`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/account/verify-email`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.VerifyEmail()` receives the bound values.
4. The action delegates to `_accountService.VerifyEmail()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `VerifyEmail()` at `Codeji.CMS.Services/Account/AccountServices.cs:283`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `VerifyEmail()` at `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:17`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<IActionResult>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 10. POST api/account/resend-email-verification

### Purpose

This endpoint is implemented by `AccountController.ResendEmailVerification()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `ResendEmailVerification()`
- Approximate source line at generation: 222
- Direct delegated call found: `_accountService.ResendEmailVerification()`

### HTTP contract

- Method: `POST`
- Route: `api/account/resend-email-verification`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] EmailVerificationRequest model`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/resend-email-verification")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] EmailVerificationRequest model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/resend-email-verification`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.ResendEmailVerification()` receives the bound values.
4. The action delegates to `_accountService.ResendEmailVerification()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ResendEmailVerification()` at `Codeji.CMS.Services/Account/AccountServices.cs:336`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `ResendEmailVerification()` at `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:18`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 11. POST api/account/ResetPassword

### Purpose

This endpoint is implemented by `AccountController.ResetPassword()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `ResetPassword()`
- Approximate source line at generation: 234
- Direct delegated call found: `body.GetString()`

### HTTP contract

- Method: `POST`
- Route: `api/account/ResetPassword`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] JsonElement body`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/ResetPassword")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] JsonElement body`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/account/ResetPassword`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.ResetPassword()` receives the bound values.
4. The action delegates to `body.GetString()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 12. POST api/applicant/applyJob

### Purpose

This endpoint is implemented by `AccountController.RegisterApplicants()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AccountController.cs`
- Controller: `AccountController`
- Action: `RegisterApplicants()`
- Approximate source line at generation: 253
- Direct delegated call found: `_applicantsServices.ApplyNowService()`

### HTTP contract

- Method: `POST`
- Route: `api/applicant/applyJob`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] ApplicantAddEditModel applicantRegisterModel`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("applicant/applyJob")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ApplicantAddEditModel applicantRegisterModel`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/applicant/applyJob`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AccountController.RegisterApplicants()` receives the bound values.
4. The action delegates to `_applicantsServices.ApplyNowService()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 13. GET api/Roles/GetRoles

### Purpose

This endpoint is implemented by `RolesController.GetRoles()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `GetRoles()`
- Approximate source line at generation: 26
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Roles/GetRoles`
- Return declaration: `async Task<Result<RoleModel>>`
- Parameters exactly as declared: `[FromQuery] bool? excludeAdmin`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoles")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] bool? excludeAdmin`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Roles/GetRoles`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.GetRoles()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<RoleModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 14. GET api/Roles/GetRolePermission

### Purpose

This endpoint is implemented by `RolesController.GetRolePermission()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `GetRolePermission()`
- Approximate source line at generation: 39
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Roles/GetRolePermission`
- Return declaration: `async Task<Result<ModuleWithPermissionsModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRolePermission")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Roles/GetRolePermission`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.GetRolePermission()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ModuleWithPermissionsModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 15. POST api/Roles/AddEditRole

### Purpose

This endpoint is implemented by `RolesController.AddRole()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `AddRole()`
- Approximate source line at generation: 55
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/Roles/AddEditRole`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `RoleWithModuleAndPermissions model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("AddEditRole")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `RoleWithModuleAndPermissions model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Roles/AddEditRole`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.AddRole()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 16. GET api/Roles/GetAllRolesWithPermission

### Purpose

This endpoint is implemented by `RolesController.GetModulePermission()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `GetModulePermission()`
- Approximate source line at generation: 64
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Roles/GetAllRolesWithPermission`
- Return declaration: `async Task<Result<ModuleWithPermissionsModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllRolesWithPermission")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Roles/GetAllRolesWithPermission`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.GetModulePermission()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ModuleWithPermissionsModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 17. GET api/Roles/GetRoleWithPermissionById

### Purpose

This endpoint is implemented by `RolesController.GetModulePermissionyId()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `GetModulePermissionyId()`
- Approximate source line at generation: 79
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Roles/GetRoleWithPermissionById`
- Return declaration: `async Task<Result<ModuleWithPermissionsModel>>`
- Parameters exactly as declared: `[FromQuery] string roleId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoleWithPermissionById")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string roleId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Roles/GetRoleWithPermissionById`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.GetModulePermissionyId()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ModuleWithPermissionsModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 18. GET api/Roles/GetRoleById/{roleId}

### Purpose

This endpoint is implemented by `RolesController.GetRoleById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `GetRoleById()`
- Approximate source line at generation: 95
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Roles/GetRoleById/{roleId}`
- Return declaration: `async Task<Result<RoleModel>>`
- Parameters exactly as declared: `string roleId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoleById/{roleId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string roleId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Roles/GetRoleById/{roleId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.GetRoleById()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<RoleModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## 19. PATCH api/Roles/UpdateAppAccessForRole/{roleId}

### Purpose

This endpoint is implemented by `RolesController.UpdateAppAccessForRole()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/RolesController.cs`
- Controller: `RolesController`
- Action: `UpdateAppAccessForRole()`
- Approximate source line at generation: 110
- Direct delegated call found: `_roleService.UpdateAppAccessForRole()`

### HTTP contract

- Method: `PATCH`
- Route: `api/Roles/UpdateAppAccessForRole/{roleId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string roleId, [FromBody] bool hasAppAccess`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPatch] [Route("UpdateAppAccessForRole/{roleId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string roleId, [FromBody] bool hasAppAccess`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/Roles/UpdateAppAccessForRole/{roleId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `RolesController.UpdateAppAccessForRole()` receives the bound values.
4. The action delegates to `_roleService.UpdateAppAccessForRole()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

### Side effects

> WARNING: Notifications, email, audit, outbox, reconciliation, and background work are documented only when the service implementation proves them. Inspect the delegated call and any processor it writes to.

### UI mapping

Search the module frontend sources for the exact route fragment and client function. If no caller exists, classify the endpoint as backend-only, legacy, or currently unmapped only after a repository-wide search.

### Testing guide

- Happy path: prepare a company-scoped record and a user permitted for this action.
- Authentication: call without a valid session unless the action is explicitly anonymous.
- Authorization: call with an authenticated user lacking the required grant/relationship.
- Tenant isolation: use an identifier owned by a different company.
- Validation: omit or invalidate each DTO field that source validation treats as required.
- State/concurrency: test duplicate, invalid transition, and stale version where the service supports them.
- Persistence: verify the expected collection before and after the call.
- Side effects: verify outbox/notification/job records independently of the synchronous response.

## Service Source Inventory

### `Codeji.CMS.Services/Account/AccountServices.cs`

- Lines: 503
- Extracted symbols: IMongoDbRepository, AccountServices, ResetPassword, result, if, Task, company, isPasswordValid, roles, token, refreshToken, hashedRefreshToken, refreshTokenEntity, result2, GenerateTokenAndSendEmail, emp, hashedToken, tokenExpiryMinutes, securityToken, tokenResult, resetLink
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Account/Interface/IAccountServices.cs`

- Lines: 19
- Extracted symbols: IAccountServices, ResetPassword, LogOut, GenerateTokenAndSendEmail, CreateNewPassword, VerifyEmail, ResendEmailVerification
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/EmailService.cs`

- Lines: 35
- Extracted symbols: SendPasswordEmail, subject, body, message
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/Interface/IEmailService.cs`

- Lines: 7
- Extracted symbols: IEmailService, SendPasswordEmail
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/MapsterConfig.cs`

- Lines: 72
- Extracted symbols: Register
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Registration/ServicesRegistration.cs`

- Lines: 67
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

