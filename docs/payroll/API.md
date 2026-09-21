# Payroll and Salary API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/AutoPayroll/GeneratePayRollMonthly` | `AutoPayrollController` | `GeneratePayRollForMonthly` | `PayrollPeriodRules.IsClosedPeriod()` |
| 2 | `POST` | `api/PayRoll/GetEmpPayRollData` | `PayRollController` | `GetEmpPayRollData` | `PayrollPeriodRules.IsClosedPeriod()` |
| 3 | `POST` | `api/PayRoll/GeneratePaySlip` | `PayRollController` | `GetSalarySlip` | `PayrollPeriodRules.IsClosedPeriod()` |
| 4 | `POST` | `api/PayRoll/GenerateEmployeePaySlip` | `PayRollController` | `GetEmployeeSalarySlip` | `PayrollPeriodRules.IsClosedPeriod()` |
| 5 | `POST` | `api/PayRoll/UploadPayloadData` | `PayRollController` | `UploadPayloadData` | `PayrollPeriodRules.IsClosedPeriod()` |
| 6 | `POST` | `api/PayRoll/AddUpdatePayRoll` | `PayRollController` | `AddUpdatePayRoll` | `PayrollPeriodRules.IsClosedPeriod()` |
| 7 | `POST` | `api/PayRoll/ProcessPayrollMonth` | `PayRollController` | `ProcessPayrollMonth` | `PayrollPeriodRules.IsClosedPeriod()` |
| 8 | `GET` | `api/payroll/divisor-policy` | `PayrollDivisorPolicyController` | `Get` | `service.GetEffective()` |
| 9 | `POST` | `api/payroll/divisor-policy` | `PayrollDivisorPolicyController` | `Save` | `service.Save()` |
| 10 | `POST` | `api/Salary/UpdateSalary` | `SalaryController` | `CreateSalary` | `_salaryService.CreateSalaryAsync()` |
| 11 | `GET` | `api/Salary/active/{userId}` | `SalaryController` | `GetActiveSalary` | `_salaryService.GetActiveSalaryAsync()` |
| 12 | `GET` | `api/Salary/history/{userId}` | `SalaryController` | `GetSalaryHistory` | `_salaryService.GetSalaryHistoryAsync()` |
| 13 | `POST` | `api/Salary/GetCompanySalaries` | `SalaryController` | `GetCompanySalaries` | `CurrentContext.CompanyId()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/AutoPayroll/GeneratePayRollMonthly

### Purpose

This endpoint is implemented by `AutoPayrollController.GeneratePayRollForMonthly()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AutoPayrollController.cs`
- Controller: `AutoPayrollController`
- Action: `GeneratePayRollForMonthly()`
- Approximate source line at generation: 37
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/AutoPayroll/GeneratePayRollMonthly`
- Return declaration: `async Task<ActionResult>`
- Parameters exactly as declared: `[FromBody] ProcessPayrollRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GeneratePayRollMonthly")] [ModulePermission(AppModule.PayrollSettings, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ProcessPayrollRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/AutoPayroll/GeneratePayRollMonthly`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AutoPayrollController.GeneratePayRollForMonthly()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<ActionResult>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 2. POST api/PayRoll/GetEmpPayRollData

### Purpose

This endpoint is implemented by `PayRollController.GetEmpPayRollData()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `GetEmpPayRollData()`
- Approximate source line at generation: 33
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/GetEmpPayRollData`
- Return declaration: `async Task<ActionResult<Result<GetEmpPayRollResponseDto>>>`
- Parameters exactly as declared: `[FromBody] GetEmpPayRollRequestDto payload`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetEmpPayRollData")] [ModulePermission(AppModule.PayRoll, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] GetEmpPayRollRequestDto payload`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/GetEmpPayRollData`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.GetEmpPayRollData()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<ActionResult<Result<GetEmpPayRollResponseDto>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 3. POST api/PayRoll/GeneratePaySlip

### Purpose

This endpoint is implemented by `PayRollController.GetSalarySlip()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `GetSalarySlip()`
- Approximate source line at generation: 51
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/GeneratePaySlip`
- Return declaration: `async Task<ActionResult>`
- Parameters exactly as declared: `PayslipRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GeneratePaySlip")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `PayslipRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/GeneratePaySlip`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.GetSalarySlip()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<ActionResult>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 4. POST api/PayRoll/GenerateEmployeePaySlip

### Purpose

This endpoint is implemented by `PayRollController.GetEmployeeSalarySlip()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `GetEmployeeSalarySlip()`
- Approximate source line at generation: 80
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/GenerateEmployeePaySlip`
- Return declaration: `async Task<ActionResult>`
- Parameters exactly as declared: `EmployeePayslipRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GenerateEmployeePaySlip")] [ModulePermission(AppModule.PayRoll, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `EmployeePayslipRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/GenerateEmployeePaySlip`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.GetEmployeeSalarySlip()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<ActionResult>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 5. POST api/PayRoll/UploadPayloadData

### Purpose

This endpoint is implemented by `PayRollController.UploadPayloadData()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `UploadPayloadData()`
- Approximate source line at generation: 105
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/UploadPayloadData`
- Return declaration: `async Task<ActionResult<Result<string>>>`
- Parameters exactly as declared: `[FromBody] EmplyeePayRollRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("UploadPayloadData")] [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] EmplyeePayRollRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/UploadPayloadData`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.UploadPayloadData()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<ActionResult<Result<string>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 6. POST api/PayRoll/AddUpdatePayRoll

### Purpose

This endpoint is implemented by `PayRollController.AddUpdatePayRoll()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `AddUpdatePayRoll()`
- Approximate source line at generation: 117
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/AddUpdatePayRoll`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] AddUpdatePayRollRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddUpdatePayRoll")] [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AddUpdatePayRollRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/AddUpdatePayRoll`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.AddUpdatePayRoll()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
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

## 7. POST api/PayRoll/ProcessPayrollMonth

### Purpose

This endpoint is implemented by `PayRollController.ProcessPayrollMonth()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayRollController.cs`
- Controller: `PayRollController`
- Action: `ProcessPayrollMonth()`
- Approximate source line at generation: 134
- Direct delegated call found: `PayrollPeriodRules.IsClosedPeriod()`

### HTTP contract

- Method: `POST`
- Route: `api/PayRoll/ProcessPayrollMonth`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] ProcessPayrollRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("ProcessPayrollMonth")] [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ProcessPayrollRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/PayRoll/ProcessPayrollMonth`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayRollController.ProcessPayrollMonth()` receives the bound values.
4. The action delegates to `PayrollPeriodRules.IsClosedPeriod()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsClosedPeriod()` at `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs:4`.
- Static signals in the inspected method window: .
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

## 8. GET api/payroll/divisor-policy

### Purpose

This endpoint is implemented by `PayrollDivisorPolicyController.Get()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayrollDivisorPolicyController.cs`
- Controller: `PayrollDivisorPolicyController`
- Action: `Get()`
- Approximate source line at generation: 13
- Direct delegated call found: `service.GetEffective()`

### HTTP contract

- Method: `GET`
- Route: `api/payroll/divisor-policy`
- Return declaration: `Task<PayrollDivisorPolicyDto>`
- Parameters exactly as declared: `[FromQuery]DateTime month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController,Authorize,Route("api/payroll/divisor-policy")] [HttpGet,ModulePermission(AppModule.PayrollSettings,Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery]DateTime month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/payroll/divisor-policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayrollDivisorPolicyController.Get()` receives the bound values.
4. The action delegates to `service.GetEffective()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEffective()` at `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs:69`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: PENDING_REVIEW, LHD_ED_LIMIT_EXCEEDED, CANCELLED, DEDUCTION_APPROVED, APPLIED_TO_PAYROLL, LHD_ED_POLICY.
- Explicit workflow/status literals found: .
- Method match: `GetEffective()` at `Codeji.CMS.Services/PayRoll/PayrollDivisorPolicyService.cs:9`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CALENDAR_DAYS, WORKING_DAYS, FIXED_30_DAYS.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<PayrollDivisorPolicyDto>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 9. POST api/payroll/divisor-policy

### Purpose

This endpoint is implemented by `PayrollDivisorPolicyController.Save()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PayrollDivisorPolicyController.cs`
- Controller: `PayrollDivisorPolicyController`
- Action: `Save()`
- Approximate source line at generation: 15
- Direct delegated call found: `service.Save()`

### HTTP contract

- Method: `POST`
- Route: `api/payroll/divisor-policy`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `PayrollDivisorPolicyDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController,Authorize,Route("api/payroll/divisor-policy")] [HttpPost,ModulePermission(AppModule.PayrollSettings,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `PayrollDivisorPolicyDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/payroll/divisor-policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PayrollDivisorPolicyController.Save()` receives the bound values.
4. The action delegates to `service.Save()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Save()` at `Codeji.CMS.Services/PayRoll/PayrollDivisorPolicyService.cs:10`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CALENDAR_DAYS, WORKING_DAYS, FIXED_30_DAYS.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 10. POST api/Salary/UpdateSalary

### Purpose

This endpoint is implemented by `SalaryController.CreateSalary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Controller: `SalaryController`
- Action: `CreateSalary()`
- Approximate source line at generation: 33
- Direct delegated call found: `_salaryService.CreateSalaryAsync()`

### HTTP contract

- Method: `POST`
- Route: `api/Salary/UpdateSalary`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] CreateSalaryDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("UpdateSalary")] [ModulePermission(AppModule.Employees, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CreateSalaryDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Salary/UpdateSalary`.
2. Authentication, controller attributes, filters, and model binding run.
3. `SalaryController.CreateSalary()` receives the bound values.
4. The action delegates to `_salaryService.CreateSalaryAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

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

## 11. GET api/Salary/active/{userId}

### Purpose

This endpoint is implemented by `SalaryController.GetActiveSalary()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Controller: `SalaryController`
- Action: `GetActiveSalary()`
- Approximate source line at generation: 51
- Direct delegated call found: `_salaryService.GetActiveSalaryAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/Salary/active/{userId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `Guid userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpGet("active/{userId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `Guid userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Salary/active/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `SalaryController.GetActiveSalary()` receives the bound values.
4. The action delegates to `_salaryService.GetActiveSalaryAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

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

## 12. GET api/Salary/history/{userId}

### Purpose

This endpoint is implemented by `SalaryController.GetSalaryHistory()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Controller: `SalaryController`
- Action: `GetSalaryHistory()`
- Approximate source line at generation: 59
- Direct delegated call found: `_salaryService.GetSalaryHistoryAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/Salary/history/{userId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `Guid userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpGet("history/{userId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `Guid userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Salary/history/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `SalaryController.GetSalaryHistory()` receives the bound values.
4. The action delegates to `_salaryService.GetSalaryHistoryAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

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

## 13. POST api/Salary/GetCompanySalaries

### Purpose

This endpoint is implemented by `SalaryController.GetCompanySalaries()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/SalaryController.cs`
- Controller: `SalaryController`
- Action: `GetCompanySalaries()`
- Approximate source line at generation: 68
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/Salary/GetCompanySalaries`
- Return declaration: `async Task<ActionResult<Result<CompanySalaryResponseDto>>>`
- Parameters exactly as declared: `[FromBody] CompanySalaryFilterDto filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetCompanySalaries")] [ModulePermission(AppModule.PayrollSettings, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CompanySalaryFilterDto filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Salary/GetCompanySalaries`.
2. Authentication, controller attributes, filters, and model binding run.
3. `SalaryController.GetCompanySalaries()` receives the bound values.
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

The compile-time return declaration is `async Task<ActionResult<Result<CompanySalaryResponseDto>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

### `Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs`

- Lines: 385
- Extracted symbols: IMongoDbRepository, AutoPayrollServices, GeneratePayrollForMonthAsync, monthStart, monthEnd, employees, divisorSetting, divisorPolicy, validationErrors, exitDate, summary, processedCount, skippedNoSalaryCount, consideredCount, payrollMutations, eligibleFrom, eligibleTo, attendanceSummary, RollbackPayrollRunAsync, attendanceException, salaryRecords, daysInMonth
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/Interface/IAutoPayRollServices.cs`

- Lines: 10
- Extracted symbols: IAutoPayRollServices, GeneratePayrollForMonthAsync
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/Interface/IPayRollServices.cs`

- Lines: 14
- Extracted symbols: IPayRollServices, GenerateEmpSalarySlip, GenerateEmployeeSalarySlip, AddUpdatePayRoll, ProcessPayrollMonth
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/PayrollCalculationRules.cs`

- Lines: 15
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/PayrollDivisorPolicyService.cs`

- Lines: 39
- Extracted symbols: IPayrollDivisorPolicyService, GetEffective, Save, date, item, allowed, versions, newStart, open
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/PayRollHostedServices.cs`

- Lines: 101
- Extracted symbols: ILogger, PayrollHostedService, indiaZone, nowUtc, nowIndia, scheduledTimeIndia, scheduledUtc, delay, RunPayroll, autoPayroll, companyService, companies, currentMonth
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/PayrollPeriodRules.cs`

- Lines: 5
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/PayRoll/PayRollServices.cs`

- Lines: 592
- Extracted symbols: IMongoDbRepository, PayRollServices, GenerateEmpSalarySlip, GenerateEmployeeSalarySlip, BuildSalarySlipAsync, salarySlipModel, monthStart, templateFilePath, fileContent, logoPath, templateContent, pdfName, readonly, List, errors, missingColumns, missingColumnLabels, row, rowMissingFields, Task, result, validationErrors, payData, employeeIds, empUsers, existingEmployeeIds
- Use: follow the controller delegation into this file when the symbol matches the action.

