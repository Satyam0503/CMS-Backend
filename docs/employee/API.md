# Employee API Documentation

**Last verified against code:** 2026-09-07

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

Lifecycle endpoints owned by the employee module:

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `api/User/EmployeeLifecycle/Availability` | Authorized HR/admin update of temporary availability with optimistic lifecycle version |
| `GET` | `api/User/EmployeeLifecycle/History/{employeeUserId}` | Authorized HR/admin lifecycle audit history; tenant and employee scoped |

These endpoints do not replace EmployeeExit endpoints. They cannot turn a temporary leave/sabbatical/absence into a former employee, and history is not included in the broad employee-directory payload.

`POST api/User/GetAllEmployees` also accepts the optional `AvailabilityStatus` filter. When supplied, results are additionally matched against the lifecycle availability value, such as `Working`, `Sabbatical`, `MaternityLeave`, `UnauthorizedAbsence`, or `Suspended`.

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/User/InviteNewEmployee` | `UserController` | `InviteNewEmployee` | `CurrentContext.UserId()` |
| 2 | `POST` | `api/User/BulkImportEmployees` | `UserController` | `BulkImportEmployees` | `CurrentContext.UserId()` |
| 3 | `GET` | `api/User/GetNextEmployeeId` | `UserController` | `GetNextEmployeeId` | `CurrentContext.CompanyId()` |
| 4 | `POST` | `api/User/EditEmployees` | `UserController` | `EditEmployees` | `Employees.Edit()` |
| 5 | `POST` | `api/User/EditOwnProfile` | `UserController` | `EditOwnProfile` | `CurrentContext.UserId()` |
| 6 | `GET` | `api/User/OfficeScheduleAssignment/{userId}` | `UserController` | `GetOfficeScheduleAssignment` | `_officeScheduleAssignments.GetEmployeeAsync()` |
| 7 | `GET` | `api/User/MyOfficeScheduleAssignment` | `UserController` | `GetMyOfficeScheduleAssignment` | `_officeScheduleAssignments.GetEmployeeAsync()` |
| 8 | `GET` | `api/User/AvailableOfficeSchedules` | `UserController` | `GetAvailableOfficeSchedules` | `_officeScheduleSettings.GetAllAsync()` |
| 9 | `PUT` | `api/User/OfficeScheduleAssignment/{userId}` | `UserController` | `SetOfficeScheduleAssignment` | `_officeScheduleAssignments.SetEmployeeAsync()` |
| 10 | `GET` | `api/User/EmployeeIdSequence` | `UserController` | `GetEmployeeIdSequence` | `_employeeService.GetEmployeeIdSequence()` |
| 11 | `POST` | `api/User/EmployeeIdSequence/Skip` | `UserController` | `SkipEmployeeIds` | `_employeeService.SkipEmployeeIds()` |
| 12 | `POST` | `api/User/EmployeeIdSequence/StartFrom` | `UserController` | `StartEmployeeIdSequence` | `_employeeService.StartEmployeeIdSequence()` |
| 13 | `POST` | `api/User/Employees/{userId}/AssignEmployeeId` | `UserController` | `AssignMissingEmployeeId` | `_employeeService.AssignMissingEmployeeId()` |
| 14 | `POST` | `api/User/GetAllEmployees` | `UserController` | `GetAllEmployees` | `_employeeService.GetAllEmployees()` |
| 15 | `POST` | `api/User/GetExEmployees` | `UserController` | `GetExEmployees` | `_employeeService.GetExEmployees()` |
| 16 | `GET` | `api/User/GetDirectoryDepartments` | `UserController` | `GetDirectoryDepartments` | `_employeeService.GetDirectoryDepartments()` |
| 17 | `POST` | `api/User/ChangePassword` | `UserController` | `ChangePassword` | `CurrentContext.UserId()` |
| 18 | `GET` | `api/User/GetEmployeeById` | `UserController` | `GetEmployeeById` | `string.IsNullOrEmpty()` |
| 19 | `GET` | `api/User/Search` | `UserController` | `SearchEmployeeByName` | `string.IsNullOrEmpty()` |
| 20 | `POST` | `api/User/AddEditEmployeeSummary` | `UserController` | `AddEditEmployeeSummary` | `string.IsNullOrEmpty()` |
| 21 | `POST` | `api/User/AddEmployeeEducation` | `UserController` | `AddEmployeeEducation` | `string.IsNullOrEmpty()` |
| 22 | `POST` | `api/User/EditEmployeeEducation` | `UserController` | `EditEmployeeEducation` | `string.IsNullOrEmpty()` |
| 23 | `POST` | `api/User/AddEmployeeCertification` | `UserController` | `AddEmployeeCertification` | `string.IsNullOrEmpty()` |
| 24 | `POST` | `api/User/EditEmployeeCertification` | `UserController` | `EditEmployeeCertification` | `string.IsNullOrEmpty()` |
| 25 | `GET` | `api/User/GetEmployeeSummary` | `UserController` | `GetEmployeeSummary` | `string.IsNullOrEmpty()` |
| 26 | `GET` | `api/User/GetEmployeeEducationDetails` | `UserController` | `GetEmployeeEducationDetails` | `string.IsNullOrEmpty()` |
| 27 | `GET` | `api/User/GetEmployeeCertificationDetails` | `UserController` | `GetEmployeeCertificationDetails` | `string.IsNullOrEmpty()` |
| 28 | `POST` | `api/User/UploadUserImage` | `UserController` | `UploadUserImage` | `CurrentContext.UserId()` |
| 29 | `POST` | `api/User/AddEditEmpSkills` | `UserController` | `AddEditSkills` | `string.IsNullOrEmpty()` |
| 30 | `GET` | `api/User/GetEmployeeSkills` | `UserController` | `GetEmployeeSkills` | `string.IsNullOrEmpty()` |
| 31 | `DELETE` | `api/User/DeleteEducationDetails/{educationId}` | `UserController` | `DeleteEducationDetails` | `string.IsNullOrEmpty()` |
| 32 | `DELETE` | `api/User/DeleteCertificationDetails/{certificationId}` | `UserController` | `DeleteCertificationDetails` | `string.IsNullOrEmpty()` |
| 33 | `DELETE` | `api/User/DeleteEmployee` | `UserController` | `DeleteEmployee` | `_employeeService.DeleteEmployee()` |
| 34 | `GET` | `api/User/GetSuggestedSkills` | `UserController` | `GetSuggestedSkills` | `query.Trim()` |
| 35 | `POST` | `api/User/AddSkill` | `UserController` | `AddSkill` | `skill.Trim()` |
| 36 | `GET` | `api/User/GetAllNotifications` | `UserController` | `GetAllNotifications` | `supportedType.Contains()` |
| 37 | `PATCH` | `api/User/Notification/{userNotificationId}/MarkAsRead` | `UserController` | `MarkNotificationAsRead` | `string.IsNullOrEmpty()` |
| 38 | `POST` | `api/User/AddUpdateWorkHistory` | `UserController` | `AddWorkHistory` | `string.IsNullOrEmpty()` |
| 39 | `GET` | `api/User/GetEmpWorkHistory` | `UserController` | `GetEmpWorkHistory` | `string.IsNullOrEmpty()` |
| 40 | `DELETE` | `api/User/DeleteWorkHistory/{workId}` | `UserController` | `DeleteWorkHistory` | `string.IsNullOrEmpty()` |
| 41 | `POST` | `api/User/MarkAllNotificationAsRead` | `UserController` | `MarkAllNotificationAsRead` | `CurrentContext.UserId()` |
| 42 | `DELETE` | `api/User/RemoveProfileImage` | `UserController` | `RemoveProfileImage` | `CurrentContext.UserId()` |
| 43 | `GET` | `api/User/GetCollegeList` | `UserController` | `GetCollegeList` | `_employeeService.GetCollegeNameSuggestions()` |
| 44 | `GET` | `api/User/GetNotificationPreferences` | `UserController` | `GetNotificationPreferences` | `CurrentContext.UserId()` |
| 45 | `POST` | `api/User/UpdateNotificationPreferences` | `UserController` | `UpdateNotificationPreferences` | `CurrentContext.UserId()` |
| 46 | `POST` | `api/User/ResendInvite/{userId}` | `UserController` | `ResendInviteLink` | `CurrentContext.UserId()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/User/InviteNewEmployee

### Purpose

This endpoint is implemented by `UserController.InviteNewEmployee()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `InviteNewEmployee()`
- Approximate source line at generation: 53
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/InviteNewEmployee`
- Return declaration: `async Task<Result<InviteEmployeeDto>>`
- Parameters exactly as declared: `[FromBody] InviteEmployeeDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("InviteNewEmployee")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] InviteEmployeeDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/InviteNewEmployee`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.InviteNewEmployee()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<InviteEmployeeDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 2. POST api/User/BulkImportEmployees

### Purpose

This endpoint is implemented by `UserController.BulkImportEmployees()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `BulkImportEmployees()`
- Approximate source line at generation: 67
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/BulkImportEmployees`
- Return declaration: `async Task<Result<BulkImportEmployeesResponseDto>>`
- Parameters exactly as declared: `[FromBody] BulkImportEmployeesRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("BulkImportEmployees")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] BulkImportEmployeesRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/BulkImportEmployees`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.BulkImportEmployees()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<BulkImportEmployeesResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 3. GET api/User/GetNextEmployeeId

### Purpose

This endpoint is implemented by `UserController.GetNextEmployeeId()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetNextEmployeeId()`
- Approximate source line at generation: 82
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetNextEmployeeId`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetNextEmployeeId")] [HttpGet] [ModulePermission(AppModule.Employees, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetNextEmployeeId`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetNextEmployeeId()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:684`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:40`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

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

## 4. POST api/User/EditEmployees

### Purpose

This endpoint is implemented by `UserController.EditEmployees()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `EditEmployees()`
- Approximate source line at generation: 91
- Direct delegated call found: `Employees.Edit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EditEmployees`
- Return declaration: `async Task<Result<UserModel>>`
- Parameters exactly as declared: `EmployeePersonalInfo user, string userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditEmployees")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeePersonalInfo user, string userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EditEmployees`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.EditEmployees()` receives the bound values.
4. The action delegates to `Employees.Edit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UserModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 5. POST api/User/EditOwnProfile

### Purpose

This endpoint is implemented by `UserController.EditOwnProfile()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `EditOwnProfile()`
- Approximate source line at generation: 118
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EditOwnProfile`
- Return declaration: `async Task<Result<UserModel>>`
- Parameters exactly as declared: `EmployeeSelfEditDto user`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditOwnProfile")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeSelfEditDto user`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EditOwnProfile`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.EditOwnProfile()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UserModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 6. GET api/User/OfficeScheduleAssignment/{userId}

### Purpose

This endpoint is implemented by `UserController.GetOfficeScheduleAssignment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetOfficeScheduleAssignment()`
- Approximate source line at generation: 127
- Direct delegated call found: `_officeScheduleAssignments.GetEmployeeAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/User/OfficeScheduleAssignment/{userId}`
- Return declaration: `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`
- Parameters exactly as declared: `string userId, CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("OfficeScheduleAssignment/{userId}")] [HttpGet] [ModulePermission(AppModule.Employees, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId, CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/OfficeScheduleAssignment/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetOfficeScheduleAssignment()` receives the bound values.
4. The action delegates to `_officeScheduleAssignments.GetEmployeeAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 7. GET api/User/MyOfficeScheduleAssignment

### Purpose

This endpoint is implemented by `UserController.GetMyOfficeScheduleAssignment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetMyOfficeScheduleAssignment()`
- Approximate source line at generation: 132
- Direct delegated call found: `_officeScheduleAssignments.GetEmployeeAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/User/MyOfficeScheduleAssignment`
- Return declaration: `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`
- Parameters exactly as declared: `CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("MyOfficeScheduleAssignment")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/MyOfficeScheduleAssignment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetMyOfficeScheduleAssignment()` receives the bound values.
4. The action delegates to `_officeScheduleAssignments.GetEmployeeAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 8. GET api/User/AvailableOfficeSchedules

### Purpose

This endpoint is implemented by `UserController.GetAvailableOfficeSchedules()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetAvailableOfficeSchedules()`
- Approximate source line at generation: 141
- Direct delegated call found: `_officeScheduleSettings.GetAllAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/User/AvailableOfficeSchedules`
- Return declaration: `Task<Result<OfficeScheduleSettingsDto>>`
- Parameters exactly as declared: `CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AvailableOfficeSchedules")] [HttpGet] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/AvailableOfficeSchedules`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetAvailableOfficeSchedules()` receives the bound values.
4. The action delegates to `_officeScheduleSettings.GetAllAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<OfficeScheduleSettingsDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 9. PUT api/User/OfficeScheduleAssignment/{userId}

### Purpose

This endpoint is implemented by `UserController.SetOfficeScheduleAssignment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SetOfficeScheduleAssignment()`
- Approximate source line at generation: 147
- Direct delegated call found: `_officeScheduleAssignments.SetEmployeeAsync()`

### HTTP contract

- Method: `PUT`
- Route: `api/User/OfficeScheduleAssignment/{userId}`
- Return declaration: `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`
- Parameters exactly as declared: `string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("OfficeScheduleAssignment/{userId}")] [HttpPut] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/User/OfficeScheduleAssignment/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SetOfficeScheduleAssignment()` receives the bound values.
4. The action delegates to `_officeScheduleAssignments.SetEmployeeAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeOfficeScheduleAssignmentDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 10. GET api/User/EmployeeIdSequence

### Purpose

This endpoint is implemented by `UserController.GetEmployeeIdSequence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeIdSequence()`
- Approximate source line at generation: 153
- Direct delegated call found: `_employeeService.GetEmployeeIdSequence()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeIdSequence`
- Return declaration: `Task<Result<EmployeeIdSequenceResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeIdSequence")] [HttpGet] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeIdSequence`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeIdSequence()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeIdSequence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeIdSequence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3669`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetEmployeeIdSequence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:112`.
- Static signals in the inspected method window: company/tenant context reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeIdSequenceResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 11. POST api/User/EmployeeIdSequence/Skip

### Purpose

This endpoint is implemented by `UserController.SkipEmployeeIds()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SkipEmployeeIds()`
- Approximate source line at generation: 159
- Direct delegated call found: `_employeeService.SkipEmployeeIds()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeIdSequence/Skip`
- Return declaration: `Task<Result<EmployeeIdSequenceResponseDto>>`
- Parameters exactly as declared: `[FromBody] SkipEmployeeIdsRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeIdSequence/Skip")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] SkipEmployeeIdsRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeIdSequence/Skip`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SkipEmployeeIds()` receives the bound values.
4. The action delegates to `_employeeService.SkipEmployeeIds()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SkipEmployeeIds()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3676`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `SkipEmployeeIds()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:113`.
- Static signals in the inspected method window: company/tenant context reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeIdSequenceResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 12. POST api/User/EmployeeIdSequence/StartFrom

### Purpose

This endpoint is implemented by `UserController.StartEmployeeIdSequence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `StartEmployeeIdSequence()`
- Approximate source line at generation: 165
- Direct delegated call found: `_employeeService.StartEmployeeIdSequence()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeIdSequence/StartFrom`
- Return declaration: `Task<Result<EmployeeIdSequenceResponseDto>>`
- Parameters exactly as declared: `[FromBody] StartEmployeeIdSequenceRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeIdSequence/StartFrom")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] StartEmployeeIdSequenceRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeIdSequence/StartFrom`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.StartEmployeeIdSequence()` receives the bound values.
4. The action delegates to `_employeeService.StartEmployeeIdSequence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `StartEmployeeIdSequence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3690`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `StartEmployeeIdSequence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:114`.
- Static signals in the inspected method window: company/tenant context reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeIdSequenceResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 13. POST api/User/Employees/{userId}/AssignEmployeeId

### Purpose

This endpoint is implemented by `UserController.AssignMissingEmployeeId()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AssignMissingEmployeeId()`
- Approximate source line at generation: 171
- Direct delegated call found: `_employeeService.AssignMissingEmployeeId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/Employees/{userId}/AssignEmployeeId`
- Return declaration: `Task<Result<AssignMissingEmployeeIdResponseDto>>`
- Parameters exactly as declared: `string userId, [FromBody] AssignMissingEmployeeIdRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("Employees/{userId}/AssignEmployeeId")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId, [FromBody] AssignMissingEmployeeIdRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/Employees/{userId}/AssignEmployeeId`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AssignMissingEmployeeId()` receives the bound values.
4. The action delegates to `_employeeService.AssignMissingEmployeeId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AssignMissingEmployeeId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3713`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `AssignMissingEmployeeId()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:115`.
- Static signals in the inspected method window: company/tenant context reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AssignMissingEmployeeIdResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 14. POST api/User/GetAllEmployees

### Purpose

This endpoint is implemented by `UserController.GetAllEmployees()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetAllEmployees()`
- Approximate source line at generation: 205
- Direct delegated call found: `_employeeService.GetAllEmployees()`

### HTTP contract

- Method: `POST`
- Route: `api/User/GetAllEmployees`
- Return declaration: `async Task<Result<GetAllEmployeeResponseModel>>`
- Parameters exactly as declared: `GetAllEmployeeRequestModel? filters`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllEmployees")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `GetAllEmployeeRequestModel? filters`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/GetAllEmployees`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetAllEmployees()` receives the bound values.
4. The action delegates to `_employeeService.GetAllEmployees()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAllEmployees()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:850`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `GetAllEmployees()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:24`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<GetAllEmployeeResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. POST api/User/GetExEmployees

### Purpose

This endpoint is implemented by `UserController.GetExEmployees()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetExEmployees()`
- Approximate source line at generation: 513
- Direct delegated call found: `_employeeService.GetExEmployees()`

### HTTP contract

- Method: `POST`
- Route: `api/User/GetExEmployees`
- Return declaration: `Task<Result<GetAllEmployeeResponseModel>>`
- Parameters exactly as declared: `GetAllEmployeeRequestModel? filters`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetExEmployees")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `GetAllEmployeeRequestModel? filters`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/GetExEmployees`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetExEmployees()` receives the bound values.
4. The action delegates to `_employeeService.GetExEmployees()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetExEmployees()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:943`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY, EXIT_REVIEW_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active, Pending.
- Method match: `GetExEmployees()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:94`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<GetAllEmployeeResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 16. GET api/User/GetDirectoryDepartments

### Purpose

This endpoint is implemented by `UserController.GetDirectoryDepartments()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetDirectoryDepartments()`
- Approximate source line at generation: 521
- Direct delegated call found: `_employeeService.GetDirectoryDepartments()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetDirectoryDepartments`
- Return declaration: `Task<Result<DepartmentEmpResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetDirectoryDepartments")] [HttpGet] [ModulePermission(AppModule.Employees, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetDirectoryDepartments`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetDirectoryDepartments()` receives the bound values.
4. The action delegates to `_employeeService.GetDirectoryDepartments()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetDirectoryDepartments()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2968`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetDirectoryDepartments()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:75`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<DepartmentEmpResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 17. POST api/User/ChangePassword

### Purpose

This endpoint is implemented by `UserController.ChangePassword()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ChangePassword()`
- Approximate source line at generation: 526
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ChangePassword`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `ChangePasswordRequest passwordModel`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ChangePassword")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `ChangePasswordRequest passwordModel`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ChangePassword`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ChangePassword()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

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

## 18. GET api/User/GetEmployeeById

### Purpose

This endpoint is implemented by `UserController.GetEmployeeById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeById()`
- Approximate source line at generation: 537
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmployeeById`
- Return declaration: `async Task<Result<UserModel>>`
- Parameters exactly as declared: `[FromQuery] string userId = null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeById")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string userId = null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmployeeById`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeById()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UserModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 19. GET api/User/Search

### Purpose

This endpoint is implemented by `UserController.SearchEmployeeByName()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SearchEmployeeByName()`
- Approximate source line at generation: 550
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/Search`
- Return declaration: `async Task<Result<EmployeeSearchResponseDTO>>`
- Parameters exactly as declared: `[FromQuery] string name`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("Search")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string name`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/Search`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SearchEmployeeByName()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeSearchResponseDTO>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 20. POST api/User/AddEditEmployeeSummary

### Purpose

This endpoint is implemented by `UserController.AddEditEmployeeSummary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddEditEmployeeSummary()`
- Approximate source line at generation: 568
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddEditEmployeeSummary`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeSummaryRequestModel userSummary`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddEditEmployeeSummary")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeSummaryRequestModel userSummary`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddEditEmployeeSummary`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddEditEmployeeSummary()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 21. POST api/User/AddEmployeeEducation

### Purpose

This endpoint is implemented by `UserController.AddEmployeeEducation()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddEmployeeEducation()`
- Approximate source line at generation: 576
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddEmployeeEducation`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeEducationRequestModel educationDetails`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddEmployeeEducation")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeEducationRequestModel educationDetails`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddEmployeeEducation`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddEmployeeEducation()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 22. POST api/User/EditEmployeeEducation

### Purpose

This endpoint is implemented by `UserController.EditEmployeeEducation()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `EditEmployeeEducation()`
- Approximate source line at generation: 584
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EditEmployeeEducation`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeEducationRequestModel educationDetails`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditEmployeeEducation")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeEducationRequestModel educationDetails`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EditEmployeeEducation`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.EditEmployeeEducation()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 23. POST api/User/AddEmployeeCertification

### Purpose

This endpoint is implemented by `UserController.AddEmployeeCertification()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddEmployeeCertification()`
- Approximate source line at generation: 592
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddEmployeeCertification`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeCertificationRequestModel certificationDetails`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddEmployeeCertification")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeCertificationRequestModel certificationDetails`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddEmployeeCertification`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddEmployeeCertification()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 24. POST api/User/EditEmployeeCertification

### Purpose

This endpoint is implemented by `UserController.EditEmployeeCertification()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `EditEmployeeCertification()`
- Approximate source line at generation: 600
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EditEmployeeCertification`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeCertificationRequestModel certificationDetails`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditEmployeeCertification")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeCertificationRequestModel certificationDetails`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EditEmployeeCertification`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.EditEmployeeCertification()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 25. GET api/User/GetEmployeeSummary

### Purpose

This endpoint is implemented by `UserController.GetEmployeeSummary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeSummary()`
- Approximate source line at generation: 608
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmployeeSummary`
- Return declaration: `async Task<Result<EmployeeSummaryRequestModel>>`
- Parameters exactly as declared: `[FromQuery] string userId = null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeSummary")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string userId = null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmployeeSummary`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeSummary()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeSummaryRequestModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 26. GET api/User/GetEmployeeEducationDetails

### Purpose

This endpoint is implemented by `UserController.GetEmployeeEducationDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeEducationDetails()`
- Approximate source line at generation: 621
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmployeeEducationDetails`
- Return declaration: `async Task<Result<EmpEducationDetails>>`
- Parameters exactly as declared: `[FromQuery] string userId = null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeEducationDetails")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string userId = null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmployeeEducationDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeEducationDetails()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmpEducationDetails>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 27. GET api/User/GetEmployeeCertificationDetails

### Purpose

This endpoint is implemented by `UserController.GetEmployeeCertificationDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeCertificationDetails()`
- Approximate source line at generation: 634
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmployeeCertificationDetails`
- Return declaration: `async Task<Result<EmpCertificationDetails>>`
- Parameters exactly as declared: `[FromQuery] string userId = null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeCertificationDetails")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string userId = null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmployeeCertificationDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeCertificationDetails()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmpCertificationDetails>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 28. POST api/User/UploadUserImage

### Purpose

This endpoint is implemented by `UserController.UploadUserImage()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UploadUserImage()`
- Approximate source line at generation: 648
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/UploadUserImage`
- Return declaration: `async Task<Result<string>>`
- Parameters exactly as declared: `IFormFile profilePicture`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UploadUserImage")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `IFormFile profilePicture`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/UploadUserImage`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UploadUserImage()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<string>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 29. POST api/User/AddEditEmpSkills

### Purpose

This endpoint is implemented by `UserController.AddEditSkills()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddEditSkills()`
- Approximate source line at generation: 697
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddEditEmpSkills`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `SkillsRequestModel skillsModel`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddEditEmpSkills")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `SkillsRequestModel skillsModel`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddEditEmpSkills`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddEditSkills()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 30. GET api/User/GetEmployeeSkills

### Purpose

This endpoint is implemented by `UserController.GetEmployeeSkills()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeSkills()`
- Approximate source line at generation: 705
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmployeeSkills`
- Return declaration: `async Task<Result<EmployeeSkillsDTO>>`
- Parameters exactly as declared: `[FromQuery] string userId = null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeSkills")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string userId = null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmployeeSkills`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeSkills()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeSkillsDTO>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 31. DELETE api/User/DeleteEducationDetails/{educationId}

### Purpose

This endpoint is implemented by `UserController.DeleteEducationDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DeleteEducationDetails()`
- Approximate source line at generation: 719
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/DeleteEducationDetails/{educationId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string educationId, [FromBody] string userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteEducationDetails/{educationId}")] [HttpDelete]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string educationId, [FromBody] string userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/DeleteEducationDetails/{educationId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DeleteEducationDetails()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 32. DELETE api/User/DeleteCertificationDetails/{certificationId}

### Purpose

This endpoint is implemented by `UserController.DeleteCertificationDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DeleteCertificationDetails()`
- Approximate source line at generation: 727
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/DeleteCertificationDetails/{certificationId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string certificationId, [FromBody] string userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteCertificationDetails/{certificationId}")] [HttpDelete]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string certificationId, [FromBody] string userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/DeleteCertificationDetails/{certificationId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DeleteCertificationDetails()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 33. DELETE api/User/DeleteEmployee

### Purpose

This endpoint is implemented by `UserController.DeleteEmployee()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DeleteEmployee()`
- Approximate source line at generation: 736
- Direct delegated call found: `_employeeService.DeleteEmployee()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/DeleteEmployee`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromQuery] string employeeId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteEmployee")] [HttpDelete] [ModulePermission(AppModule.Employees, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string employeeId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/DeleteEmployee`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DeleteEmployee()` receives the bound values.
4. The action delegates to `_employeeService.DeleteEmployee()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteEmployee()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3252`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Active, NoticePeriod.
- Method match: `DeleteEmployee()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:93`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
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

## 34. GET api/User/GetSuggestedSkills

### Purpose

This endpoint is implemented by `UserController.GetSuggestedSkills()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetSuggestedSkills()`
- Approximate source line at generation: 756
- Direct delegated call found: `query.Trim()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetSuggestedSkills`
- Return declaration: `async Task<Result<Skills>>`
- Parameters exactly as declared: `[FromQuery] string query`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetSuggestedSkills")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string query`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetSuggestedSkills`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetSuggestedSkills()` receives the bound values.
4. The action delegates to `query.Trim()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Trim()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:192`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Trim()` at `Codeji.CMS.Services/Employees/EmploymentPeriod.cs:31`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Resigned, Terminated.
- Method match: `Trim()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:132`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED, FORMER_PORTAL_ACCESS_DISABLED, FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Skills>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 35. POST api/User/AddSkill

### Purpose

This endpoint is implemented by `UserController.AddSkill()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddSkill()`
- Approximate source line at generation: 773
- Direct delegated call found: `skill.Trim()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddSkill`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] string skill`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddSkill")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] string skill`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddSkill`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddSkill()` receives the bound values.
4. The action delegates to `skill.Trim()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Trim()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:192`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Trim()` at `Codeji.CMS.Services/Employees/EmploymentPeriod.cs:31`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Resigned, Terminated.
- Method match: `Trim()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:132`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED, FORMER_PORTAL_ACCESS_DISABLED, FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.

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

## 36. GET api/User/GetAllNotifications

### Purpose

This endpoint is implemented by `UserController.GetAllNotifications()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetAllNotifications()`
- Approximate source line at generation: 794
- Direct delegated call found: `supportedType.Contains()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetAllNotifications`
- Return declaration: `async Task<Result<NotificationResponseModel>>`
- Parameters exactly as declared: `[FromQuery] NotificationRequestDTO model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetAllNotifications")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] NotificationRequestDTO model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetAllNotifications`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetAllNotifications()` receives the bound values.
4. The action delegates to `supportedType.Contains()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Contains()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:830`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `Contains()` at `Codeji.CMS.Services/Employees/PrivateEmployeeExitEvidenceStorage.cs:18`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Contains()` at `Codeji.CMS.Services/Employees/RoleServices.cs:328`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<NotificationResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 37. PATCH api/User/Notification/{userNotificationId}/MarkAsRead

### Purpose

This endpoint is implemented by `UserController.MarkNotificationAsRead()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `MarkNotificationAsRead()`
- Approximate source line at generation: 812
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `PATCH`
- Route: `api/User/Notification/{userNotificationId}/MarkAsRead`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string userNotificationId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPatch] [Route("Notification/{userNotificationId}/MarkAsRead")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userNotificationId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/User/Notification/{userNotificationId}/MarkAsRead`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.MarkNotificationAsRead()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 38. POST api/User/AddUpdateWorkHistory

### Purpose

This endpoint is implemented by `UserController.AddWorkHistory()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddWorkHistory()`
- Approximate source line at generation: 824
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AddUpdateWorkHistory`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `EmployeeWorkHistoryModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddUpdateWorkHistory")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeeWorkHistoryModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AddUpdateWorkHistory`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddWorkHistory()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 39. GET api/User/GetEmpWorkHistory

### Purpose

This endpoint is implemented by `UserController.GetEmpWorkHistory()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmpWorkHistory()`
- Approximate source line at generation: 837
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetEmpWorkHistory`
- Return declaration: `async Task<Result<EmployeeWorkHistoryModel>>`
- Parameters exactly as declared: `[FromQuery] string? userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetEmpWorkHistory")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string? userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetEmpWorkHistory`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmpWorkHistory()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeWorkHistoryModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 40. DELETE api/User/DeleteWorkHistory/{workId}

### Purpose

This endpoint is implemented by `UserController.DeleteWorkHistory()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DeleteWorkHistory()`
- Approximate source line at generation: 850
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/DeleteWorkHistory/{workId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string workId, [FromBody] string? userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteWorkHistory/{workId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string workId, [FromBody] string? userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/DeleteWorkHistory/{workId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DeleteWorkHistory()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:883`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active.
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Employees/RoleServices.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference.
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

## 41. POST api/User/MarkAllNotificationAsRead

### Purpose

This endpoint is implemented by `UserController.MarkAllNotificationAsRead()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `MarkAllNotificationAsRead()`
- Approximate source line at generation: 858
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/MarkAllNotificationAsRead`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("MarkAllNotificationAsRead")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/MarkAllNotificationAsRead`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.MarkAllNotificationAsRead()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

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

## 42. DELETE api/User/RemoveProfileImage

### Purpose

This endpoint is implemented by `UserController.RemoveProfileImage()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RemoveProfileImage()`
- Approximate source line at generation: 866
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/RemoveProfileImage`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("RemoveProfileImage")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/RemoveProfileImage`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RemoveProfileImage()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

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

## 43. GET api/User/GetCollegeList

### Purpose

This endpoint is implemented by `UserController.GetCollegeList()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetCollegeList()`
- Approximate source line at generation: 874
- Direct delegated call found: `_employeeService.GetCollegeNameSuggestions()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetCollegeList`
- Return declaration: `async Task<Result<string>>`
- Parameters exactly as declared: `[FromQuery] string searchValue`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetCollegeList")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string searchValue`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetCollegeList`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetCollegeList()` receives the bound values.
4. The action delegates to `_employeeService.GetCollegeNameSuggestions()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetCollegeNameSuggestions()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:3485`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetCollegeNameSuggestions()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:104`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<string>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 44. GET api/User/GetNotificationPreferences

### Purpose

This endpoint is implemented by `UserController.GetNotificationPreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetNotificationPreferences()`
- Approximate source line at generation: 888
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `GET`
- Route: `api/User/GetNotificationPreferences`
- Return declaration: `async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetNotificationPreferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/GetNotificationPreferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetNotificationPreferences()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 45. POST api/User/UpdateNotificationPreferences

### Purpose

This endpoint is implemented by `UserController.UpdateNotificationPreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpdateNotificationPreferences()`
- Approximate source line at generation: 898
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/UpdateNotificationPreferences`
- Return declaration: `async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>>`
- Parameters exactly as declared: `[FromBody] Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("UpdateNotificationPreferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/UpdateNotificationPreferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpdateNotificationPreferences()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 46. POST api/User/ResendInvite/{userId}

### Purpose

This endpoint is implemented by `UserController.ResendInviteLink()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ResendInviteLink()`
- Approximate source line at generation: 907
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ResendInvite/{userId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("ResendInvite/{userId}")] [ModulePermission(AppModule.Employees, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ResendInvite/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ResendInviteLink()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:853`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY.
- Explicit workflow/status literals found: Active, NoticePeriod, Resigned, Terminated.
- Method match: `UserId()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.

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

### `Codeji.CMS.Services/Employees/EmployeeExitOutboxProcessor.cs`

- Lines: 146
- Extracted symbols: IEmployeeExitOutboxProcessor, ProcessPendingAsync, QueueDueRemindersAsync, now, collection, filter, item, FindOneAndUpdateOptions, notification, savedNotification, recipientNotification, savedRecipient, delay, state, error, dueCases, claim, BsonDocument, UpdateOptions, queued, finalStatus
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeeExitOutboxWorker.cs`

- Lines: 23
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeePresenceService.cs`

- Lines: 50
- Extracted symbols: ConcurrentDictionary, AddUserToGroup, group, RemoveUserFromGroup, GetOnlineUsersInGroup
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeeService.cs`

- Lines: 3847
- Extracted symbols: IMongoDbRepository, ILogger
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmploymentPeriod.cs`

- Lines: 60
- Extracted symbols: monthStart, monthEnd, employmentStatus, lastWorkingDate, isCompletedExit, day
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs`

- Lines: 341
- Extracted symbols: Task, companyId, actorId, company, exit, updated, now, rawToken, expiresAt, savedToken, portalUrl, delivery, failedAt, rollback, rawSession, sessionExpiry, update, FindOneAndUpdateOptions, RequestOtp, email, candidate, otp, saved
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeeExitEvidenceStorage.cs`

- Lines: 12
- Extracted symbols: IEmployeeExitEvidenceStorage, SaveAsync, OpenReadAsync, DeleteAsync
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeePresenceService.cs`

- Lines: 8
- Extracted symbols: IEmployeePresenceService, AddUserToGroup, RemoveUserFromGroup, GetOnlineUsersInGroup
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs`

- Lines: 118
- Extracted symbols: IEmployeeService, IsUserActive, GetEmployeeById, GetEmployeeNameById, InitiateEmployeeExit, SubmitOwnResignation, CompleteEmployeeExit, ReviewEmployeeExit, RespondToEmployeeExitDiscussion, RespondToEmployeeExitJointDiscussion, UpdateEmployeeExitClearanceStatus, RecordEmployeeExitInterview, UpsertEmployeeExitReassignment, WithdrawOwnEmployeeExit, DecideEmployeeExitWithdrawal, CancelEmployeeExit, UpdateEmployeeExitDate, RequestEmployeeExitNoticeAdjustment, DecideEmployeeExitNoticeAdjustment, DecideOwnEmployeeExitNoticeOffer, UpsertEmployeeExitPolicy, UpsertEmployeeExitClearanceTemplate, DeleteEmployeeExitClearanceTemplate, ApproveEmployeeFinalSettlement, UpdateEmployeeFinalSettlementPayment, CalculateEmployeeFinalSettlement, MarkEmployeeFinalSettlementPaid, GenerateEmployeeExitDocument, IssueEmployeeExitDocument, CompleteEmployeeExitClearance
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs`

- Lines: 20
- Extracted symbols: IFormerEmployeePortalService, RevokeAccess, RequestOtp, Logout, SetAccess, SetCompanyAccess, VerifyPostExitContact, CreateRequest
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IRoleService.cs`

- Lines: 27
- Extracted symbols: IRoleService, AddEditRoles, GetRoleById, GetRolePermissionOfuser, CheckRoleDependancyForDeletion, VerifyUserAccess, UpdateAppAccessForRole, IsRoleTypeMatch
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/PrivateEmployeeExitEvidenceStorage.cs`

- Lines: 65
- Extracted symbols: PrivateEmployeeExitEvidenceStorage, SaveAsync, extension, key, path, using, OpenReadAsync, rootPrefix, contentType, DeleteAsync
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/RoleServices.cs`

- Lines: 705
- Extracted symbols: AppModule, Module, PermissionConst, readonly, IMongoDbRepository, RoleServices, AddEditRoles, result, totalRoles, roleWithModulePermission, newRole, rolePermissions, newPermission, Task, roles, RestoreCompanyRolePermissionsAsync, GetRoleById, role, moduleWithPermissionsModel, modulePermissions, permissions
- Use: follow the controller delegation into this file when the symbol matches the action.

