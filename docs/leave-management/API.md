# Leave Management API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/LeaveManagement/CreateLeavePolicy` | `LeaveManagementController` | `CreateNewLeavePolicy` | `No direct delegated call conclusively extracted; inspect the action body.` |
| 2 | `POST` | `api/LeaveManagement/UpdateLeavePolicy` | `LeaveManagementController` | `UpdateLeavePolicy` | `No direct delegated call conclusively extracted; inspect the action body.` |
| 3 | `POST` | `api/LeaveManagement/GetAllLeavePolicies` | `LeaveManagementController` | `GetAllLeavePolicies` | `CurrentContext.CompanyId()` |
| 4 | `GET` | `api/LeaveManagement/me/policies` | `LeaveManagementController` | `GetMySelfServicePolicies` | `_leaveManagementService.GetMySelfServicePolicies()` |
| 5 | `GET` | `api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}` | `LeaveManagementController` | `GetEmployeeLeaveBalance` | `_leaveManagementService.GetEmployeeLeaveBalance()` |
| 6 | `GET` | `api/LeaveManagement/me/balances` | `LeaveManagementController` | `GetMyLeaveBalance` | `_leaveManagementService.GetEmployeeLeaveBalance()` |
| 7 | `POST` | `api/LeaveManagement/CreateLeaveRequest` | `LeaveManagementController` | `CreateLeaveRequest` | `CurrentContext.UserId()` |
| 8 | `POST` | `api/LeaveManagement/me/requests` | `LeaveManagementController` | `CreateMyLeaveRequest` | `CurrentContext.UserId()` |
| 9 | `POST` | `api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}` | `LeaveManagementController` | `CreateEmployeeLeaveRequest` | `string.IsNullOrWhiteSpace()` |
| 10 | `POST` | `api/LeaveManagement/UpdateLeaveRequest` | `LeaveManagementController` | `UpdateLeaveRequest` | `CurrentContext.UserId()` |
| 11 | `PATCH` | `api/LeaveManagement/me/requests` | `LeaveManagementController` | `UpdateMyLeaveRequest` | `CurrentContext.UserId()` |
| 12 | `POST` | `api/LeaveManagement/GetLeaveRequests` | `LeaveManagementController` | `GetLeaveRequest` | `_leaveManagementService.GetLeaveRequest()` |
| 13 | `POST` | `api/LeaveManagement/GetMyLeaveRequests` | `LeaveManagementController` | `GetMyLeaveRequests` | `CurrentContext.UserId()` |
| 14 | `POST` | `api/LeaveManagement/me/requests/search` | `LeaveManagementController` | `GetMyProfileLeaveRequests` | `CurrentContext.UserId()` |
| 15 | `DELETE` | `api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}` | `LeaveManagementController` | `DeleteLeaveRequest` | `_leaveManagementService.DeleteLeaveRequest()` |
| 16 | `DELETE` | `api/LeaveManagement/me/requests/{leaveRequestId}` | `LeaveManagementController` | `DeleteMyLeaveRequest` | `_leaveManagementService.DeleteLeaveRequest()` |
| 17 | `PATCH` | `api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status` | `LeaveManagementController` | `UpdateLeaveRequestStatus` | `_leaveManagementService.UpdateLeaveRequestStatus()` |
| 18 | `GET` | `api/LeaveManagement/GetLeaveRequestSummary` | `LeaveManagementController` | `GetLeaveRequestSummary` | `_leaveManagementService.GetLeaveRequestSummary()` |
| 19 | `GET` | `api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}` | `LeaveManagementController` | `GetMonthlyTakenLeaveSummary` | `_leaveManagementService.GetMonthlyTakenLeaveSummary()` |
| 20 | `POST` | `api/LeaveManagement/UpdateEmployeeLeaveBalance` | `LeaveManagementController` | `UpdateEmpLeaveBalance` | `_leaveManagementService.UpdateEmployeeLeaveBalance()` |
| 21 | `POST` | `api/LeaveManagement/GetAllEmployeeLeaveBalances` | `LeaveManagementController` | `GetAllEmployeeLeaveBalances` | `CurrentContext.CompanyId()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/LeaveManagement/CreateLeavePolicy

### Purpose

This endpoint is implemented by `LeaveManagementController.CreateNewLeavePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `CreateNewLeavePolicy()`
- Approximate source line at generation: 38
- Direct delegated call found: `No direct delegated call conclusively extracted; inspect the action body.`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/CreateLeavePolicy`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] LeavePolicyRequest model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateLeavePolicy")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] LeavePolicyRequest model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/CreateLeavePolicy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.CreateNewLeavePolicy()` receives the bound values.
4. The action delegates to `No direct delegated call conclusively extracted; inspect the action body.` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateNewLeavePolicy()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:13`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CreateNewLeavePolicy()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:93`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 2. POST api/LeaveManagement/UpdateLeavePolicy

### Purpose

This endpoint is implemented by `LeaveManagementController.UpdateLeavePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `UpdateLeavePolicy()`
- Approximate source line at generation: 72
- Direct delegated call found: `No direct delegated call conclusively extracted; inspect the action body.`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/UpdateLeavePolicy`
- Return declaration: `async Task<Result<UpdateLeavePolicyRequest>>`
- Parameters exactly as declared: `[FromBody] UpdateLeavePolicyRequest model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateLeavePolicy")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateLeavePolicyRequest model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/UpdateLeavePolicy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.UpdateLeavePolicy()` receives the bound values.
4. The action delegates to `No direct delegated call conclusively extracted; inspect the action body.` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateLeavePolicy()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:14`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateLeavePolicy()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:148`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UpdateLeavePolicyRequest>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 3. POST api/LeaveManagement/GetAllLeavePolicies

### Purpose

This endpoint is implemented by `LeaveManagementController.GetAllLeavePolicies()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetAllLeavePolicies()`
- Approximate source line at generation: 103
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/GetAllLeavePolicies`
- Return declaration: `async Task<Result<UpdateLeavePolicyRequest>>`
- Parameters exactly as declared: `[FromQuery] bool? status`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllLeavePolicies")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] bool? status`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/GetAllLeavePolicies`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetAllLeavePolicies()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:152`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UpdateLeavePolicyRequest>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 4. GET api/LeaveManagement/me/policies

### Purpose

This endpoint is implemented by `LeaveManagementController.GetMySelfServicePolicies()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetMySelfServicePolicies()`
- Approximate source line at generation: 114
- Direct delegated call found: `_leaveManagementService.GetMySelfServicePolicies()`

### HTTP contract

- Method: `GET`
- Route: `api/LeaveManagement/me/policies`
- Return declaration: `Task<Result<UpdateLeavePolicyRequest>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/policies")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/LeaveManagement/me/policies`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetMySelfServicePolicies()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetMySelfServicePolicies()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetMySelfServicePolicies()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetMySelfServicePolicies()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:358`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<UpdateLeavePolicyRequest>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 5. GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}

### Purpose

This endpoint is implemented by `LeaveManagementController.GetEmployeeLeaveBalance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetEmployeeLeaveBalance()`
- Approximate source line at generation: 120
- Direct delegated call found: `_leaveManagementService.GetEmployeeLeaveBalance()`

### HTTP contract

- Method: `GET`
- Route: `api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}`
- Return declaration: `async Task<Result<EmployeeLeaveBalanceResponseDto>>`
- Parameters exactly as declared: `string employeeId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeLeaveBalance/{employeeId}")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetEmployeeLeaveBalance()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetEmployeeLeaveBalance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1412`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SECOND_HALF.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeLeaveBalanceResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 6. GET api/LeaveManagement/me/balances

### Purpose

This endpoint is implemented by `LeaveManagementController.GetMyLeaveBalance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetMyLeaveBalance()`
- Approximate source line at generation: 130
- Direct delegated call found: `_leaveManagementService.GetEmployeeLeaveBalance()`

### HTTP contract

- Method: `GET`
- Route: `api/LeaveManagement/me/balances`
- Return declaration: `async Task<Result<EmployeeLeaveBalanceResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/balances")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/LeaveManagement/me/balances`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetMyLeaveBalance()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetEmployeeLeaveBalance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1412`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SECOND_HALF.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<EmployeeLeaveBalanceResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 7. POST api/LeaveManagement/CreateLeaveRequest

### Purpose

This endpoint is implemented by `LeaveManagementController.CreateLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `CreateLeaveRequest()`
- Approximate source line at generation: 140
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/CreateLeaveRequest`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] LeaveRequestDto leaveRequest`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateLeaveRequest")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] LeaveRequestDto leaveRequest`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/CreateLeaveRequest`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.CreateLeaveRequest()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
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

## 8. POST api/LeaveManagement/me/requests

### Purpose

This endpoint is implemented by `LeaveManagementController.CreateMyLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `CreateMyLeaveRequest()`
- Approximate source line at generation: 149
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/me/requests`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] LeaveRequestDto leaveRequest`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] LeaveRequestDto leaveRequest`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/me/requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.CreateMyLeaveRequest()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
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

## 9. POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}

### Purpose

This endpoint is implemented by `LeaveManagementController.CreateEmployeeLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `CreateEmployeeLeaveRequest()`
- Approximate source line at generation: 159
- Direct delegated call found: `string.IsNullOrWhiteSpace()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string employeeId, [FromBody] LeaveRequestDto leaveRequest`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateEmployeeLeaveRequest/{employeeId}")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeId, [FromBody] LeaveRequestDto leaveRequest`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.CreateEmployeeLeaveRequest()` receives the bound values.
4. The action delegates to `string.IsNullOrWhiteSpace()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrWhiteSpace()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:114`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
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

## 10. POST api/LeaveManagement/UpdateLeaveRequest

### Purpose

This endpoint is implemented by `LeaveManagementController.UpdateLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `UpdateLeaveRequest()`
- Approximate source line at generation: 175
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/UpdateLeaveRequest`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateLeaveRequestDto leaveRequest`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateLeaveRequest")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateLeaveRequestDto leaveRequest`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/UpdateLeaveRequest`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.UpdateLeaveRequest()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
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

## 11. PATCH api/LeaveManagement/me/requests

### Purpose

This endpoint is implemented by `LeaveManagementController.UpdateMyLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `UpdateMyLeaveRequest()`
- Approximate source line at generation: 184
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `PATCH`
- Route: `api/LeaveManagement/me/requests`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateLeaveRequestDto leaveRequest`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests")] [HttpPatch]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateLeaveRequestDto leaveRequest`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/LeaveManagement/me/requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.UpdateMyLeaveRequest()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
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

## 12. POST api/LeaveManagement/GetLeaveRequests

### Purpose

This endpoint is implemented by `LeaveManagementController.GetLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetLeaveRequest()`
- Approximate source line at generation: 195
- Direct delegated call found: `_leaveManagementService.GetLeaveRequest()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/GetLeaveRequests`
- Return declaration: `async Task<Result<LeaveResponseDto>>`
- Parameters exactly as declared: `LeaveRequestFilter? leaveFilter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetLeaveRequests")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `LeaveRequestFilter? leaveFilter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/GetLeaveRequests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetLeaveRequest()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetLeaveRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:23`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:622`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<LeaveResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 13. POST api/LeaveManagement/GetMyLeaveRequests

### Purpose

This endpoint is implemented by `LeaveManagementController.GetMyLeaveRequests()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetMyLeaveRequests()`
- Approximate source line at generation: 203
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/GetMyLeaveRequests`
- Return declaration: `async Task<Result<MyLeaveRequestResponse>>`
- Parameters exactly as declared: `[FromBody] EmpLeaveRequestFilter leaveFilter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetMyLeaveRequests")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] EmpLeaveRequestFilter leaveFilter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/GetMyLeaveRequests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetMyLeaveRequests()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<MyLeaveRequestResponse>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 14. POST api/LeaveManagement/me/requests/search

### Purpose

This endpoint is implemented by `LeaveManagementController.GetMyProfileLeaveRequests()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetMyProfileLeaveRequests()`
- Approximate source line at generation: 211
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/me/requests/search`
- Return declaration: `async Task<Result<MyLeaveRequestResponse>>`
- Parameters exactly as declared: `[FromBody] EmpLeaveRequestFilter leaveFilter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests/search")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] EmpLeaveRequestFilter leaveFilter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/me/requests/search`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetMyProfileLeaveRequests()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_AFTER_EXIT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<MyLeaveRequestResponse>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. DELETE api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}

### Purpose

This endpoint is implemented by `LeaveManagementController.DeleteLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `DeleteLeaveRequest()`
- Approximate source line at generation: 220
- Direct delegated call found: `_leaveManagementService.DeleteLeaveRequest()`

### HTTP contract

- Method: `DELETE`
- Route: `api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string leaveRequestId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteLeaveRequest/{leaveRequestId}")] [HttpDelete] [ModulePermission(AppModule.LeaveManagement, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string leaveRequestId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.DeleteLeaveRequest()` receives the bound values.
4. The action delegates to `_leaveManagementService.DeleteLeaveRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:25`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:729`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.

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

## 16. DELETE api/LeaveManagement/me/requests/{leaveRequestId}

### Purpose

This endpoint is implemented by `LeaveManagementController.DeleteMyLeaveRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `DeleteMyLeaveRequest()`
- Approximate source line at generation: 227
- Direct delegated call found: `_leaveManagementService.DeleteLeaveRequest()`

### HTTP contract

- Method: `DELETE`
- Route: `api/LeaveManagement/me/requests/{leaveRequestId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string leaveRequestId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests/{leaveRequestId}")] [HttpDelete]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string leaveRequestId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/LeaveManagement/me/requests/{leaveRequestId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.DeleteMyLeaveRequest()` receives the bound values.
4. The action delegates to `_leaveManagementService.DeleteLeaveRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:25`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteLeaveRequest()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:729`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.

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

## 17. PATCH api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status

### Purpose

This endpoint is implemented by `LeaveManagementController.UpdateLeaveRequestStatus()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `UpdateLeaveRequestStatus()`
- Approximate source line at generation: 235
- Direct delegated call found: `_leaveManagementService.UpdateLeaveRequestStatus()`

### HTTP contract

- Method: `PATCH`
- Route: `api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string leaveRequestId, [FromBody] LeaveRequestUpdateDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("LeaveRequest/{leaveRequestId}/Status")] [HttpPatch] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string leaveRequestId, [FromBody] LeaveRequestUpdateDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.UpdateLeaveRequestStatus()` receives the bound values.
4. The action delegates to `_leaveManagementService.UpdateLeaveRequestStatus()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateLeaveRequestStatus()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:26`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateLeaveRequestStatus()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:746`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.

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

## 18. GET api/LeaveManagement/GetLeaveRequestSummary

### Purpose

This endpoint is implemented by `LeaveManagementController.GetLeaveRequestSummary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetLeaveRequestSummary()`
- Approximate source line at generation: 243
- Direct delegated call found: `_leaveManagementService.GetLeaveRequestSummary()`

### HTTP contract

- Method: `GET`
- Route: `api/LeaveManagement/GetLeaveRequestSummary`
- Return declaration: `async Task<Result<LeaveRequestSummaryResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetLeaveRequestSummary")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/LeaveManagement/GetLeaveRequestSummary`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetLeaveRequestSummary()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetLeaveRequestSummary()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetLeaveRequestSummary()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:27`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetLeaveRequestSummary()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1230`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<LeaveRequestSummaryResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 19. GET api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}

### Purpose

This endpoint is implemented by `LeaveManagementController.GetMonthlyTakenLeaveSummary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetMonthlyTakenLeaveSummary()`
- Approximate source line at generation: 251
- Direct delegated call found: `_leaveManagementService.GetMonthlyTakenLeaveSummary()`

### HTTP contract

- Method: `GET`
- Route: `api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}`
- Return declaration: `async Task<Result<MonthlyTakenLeaveSummaryResponseDto>>`
- Parameters exactly as declared: `int? year`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetMonthlyTakenLeaveSummary/{year}")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `int? year`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetMonthlyTakenLeaveSummary()` receives the bound values.
4. The action delegates to `_leaveManagementService.GetMonthlyTakenLeaveSummary()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetMonthlyTakenLeaveSummary()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetMonthlyTakenLeaveSummary()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1267`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<MonthlyTakenLeaveSummaryResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 20. POST api/LeaveManagement/UpdateEmployeeLeaveBalance

### Purpose

This endpoint is implemented by `LeaveManagementController.UpdateEmpLeaveBalance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `UpdateEmpLeaveBalance()`
- Approximate source line at generation: 264
- Direct delegated call found: `_leaveManagementService.UpdateEmployeeLeaveBalance()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/UpdateEmployeeLeaveBalance`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `List<LeaveBalanceRequestDto> LeaveBalanceRequestDto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateEmployeeLeaveBalance")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `List<LeaveBalanceRequestDto> LeaveBalanceRequestDto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/UpdateEmployeeLeaveBalance`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.UpdateEmpLeaveBalance()` receives the bound values.
4. The action delegates to `_leaveManagementService.UpdateEmployeeLeaveBalance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:31`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateEmployeeLeaveBalance()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1291`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
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

## 21. POST api/LeaveManagement/GetAllEmployeeLeaveBalances

### Purpose

This endpoint is implemented by `LeaveManagementController.GetAllEmployeeLeaveBalances()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/LeaveManagementController.cs`
- Controller: `LeaveManagementController`
- Action: `GetAllEmployeeLeaveBalances()`
- Approximate source line at generation: 272
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/LeaveManagement/GetAllEmployeeLeaveBalances`
- Return declaration: `async Task<Result<AllEmployeeLeaveBalance>>`
- Parameters exactly as declared: `[FromBody] LeaveBalanceFilter filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllEmployeeLeaveBalances")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] LeaveBalanceFilter filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/LeaveManagement/GetAllEmployeeLeaveBalances`.
2. Authentication, controller attributes, filters, and model binding run.
3. `LeaveManagementController.GetAllEmployeeLeaveBalances()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:152`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<AllEmployeeLeaveBalance>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

### `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs`

- Lines: 36
- Extracted symbols: ILeaveManagementService, CreateNewLeavePolicy, CreateLeaveRequest, UpdateLeaveRequest, DeleteLeaveRequest, UpdateLeaveRequestStatus, UpdateEmployeeLeaveBalance, EmployeeLeaveBalanceAccrual
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs`

- Lines: 1761
- Extracted symbols: IMongoDbRepository, LeaveManagementService, CreateNewLeavePolicy, result, exist, leavePolicy, sync, Task, companyId, hasBalances, hasRequests, duplicateLeavePolicy, policyBalances, duplicateEmployeeCount, updatedPolicyModel, updateResult, if
- Use: follow the controller delegation into this file when the symbol matches the action.

