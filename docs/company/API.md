# Company, Calendar and Schedules API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/Calendar/GetAllCalendarItems` | `CalendarController` | `GetAllCalendarItems` | `_calendarServices.GetAllCalendarItems()` |
| 2 | `POST` | `api/Calendar/AddUpdateCalendarItem` | `CalendarController` | `AddUpdateCalendarItem` | `_calendarServices.AddUpdateCalendarItem()` |
| 3 | `POST` | `api/Calendar/ImportCalendarItems` | `CalendarController` | `ImportCalendarItems` | `_calendarServices.ImportCalendarItems()` |
| 4 | `DELETE` | `api/Calendar/DeleteCalendarItem/{itemId}` | `CalendarController` | `DeleteCalendarItem` | `_calendarServices.DeleteItem()` |
| 5 | `POST` | `api/Calendar/GetHolidays` | `CalendarController` | `GetHolidays` | `_calendarServices.GetHolidays()` |
| 6 | `GET` | `api/Company/GetAllCompanyList` | `CompanyController` | `GetAllCompanyList` | `_companyService.GetAllCompanyList()` |
| 7 | `GET` | `api/Company/GetCompanyDetails` | `CompanyController` | `GetCompanyDetails` | `CurrentContext.CompanyId()` |
| 8 | `GET` | `api/Company/career` | `CompanyController` | `GetPublicCareerCompany` | `_companyService.GetPublicCareerCompany()` |
| 9 | `GET` | `api/Company/career/{publicCompanyCode}` | `CompanyController` | `GetPublicCareerCompany` | `_companyService.GetPublicCareerCompany()` |
| 10 | `GET` | `api/Company/theme` | `CompanyController` | `GetCurrentCompanyTheme` | `_companyService.GetCompanyDetails()` |
| 11 | `POST` | `api/Company/UpdateCompanyDetails` | `CompanyController` | `UpdateCompanyDetails` | `CurrentContext.CompanyId()` |
| 12 | `POST` | `api/Company/AddPolicy` | `CompanyController` | `AddPolicy` | `CurrentContext.CompanyId()` |
| 13 | `POST` | `api/Company/UpdatePolicy` | `CompanyController` | `UpdatePolicy` | `CurrentContext.CompanyId()` |
| 14 | `GET` | `api/Company/GetAllPolicies` | `CompanyController` | `GetAllPolicies` | `CurrentContext.CompanyId()` |
| 15 | `DELETE` | `api/Company/DeletePolicy/{policyId}` | `CompanyController` | `DeletePolicy` | `CurrentContext.CompanyId()` |
| 16 | `POST` | `api/Company/CreatePolicyVersion` | `CompanyController` | `AddPolicyVersion` | `_companyService.AddPolicyVersion()` |
| 17 | `POST` | `api/Company/EditPolicyVersion` | `CompanyController` | `EditPolicyVersion` | `_companyService.EditPolicyVersion()` |
| 18 | `POST` | `api/Company/SetCurrentPolicyVersion` | `CompanyController` | `SetCurrentPolicyVersion` | `_companyService.SetCurrentPolicyVersion()` |
| 19 | `GET` | `api/Company/GetAllPolicyVersion/{policyId}` | `CompanyController` | `GetAllPolicyVersion` | `CurrentContext.UserId()` |
| 20 | `DELETE` | `api/Company/DeletePolicyVersion/{policyVersionId}` | `CompanyController` | `DeletePolicyVersion` | `CurrentContext.CompanyId()` |
| 21 | `GET` | `api/Company/GetPolicyDocument/{policyVersionId}` | `CompanyController` | `GetPolicyDocument` | `CurrentContext.UserId()` |
| 22 | `GET` | `api/CompanyMaster/OfficeSchedule` | `CompanyMasterController` | `GetOfficeSchedule` | `_officeScheduleSettings.GetAsync()` |
| 23 | `GET` | `api/CompanyMaster/OfficeSchedules` | `CompanyMasterController` | `GetOfficeSchedules` | `_officeScheduleSettings.GetAllAsync()` |
| 24 | `PUT` | `api/CompanyMaster/OfficeSchedule` | `CompanyMasterController` | `SaveOfficeSchedule` | `_officeScheduleSettings.SaveAsync()` |
| 25 | `DELETE` | `api/CompanyMaster/OfficeSchedule/{scheduleId}` | `CompanyMasterController` | `DeleteOfficeSchedule` | `_officeScheduleSettings.DeleteAsync()` |
| 26 | `GET` | `api/CompanyMaster/DepartmentOfficeScheduleAssignments` | `CompanyMasterController` | `GetDepartmentOfficeScheduleAssignments` | `_officeScheduleAssignments.GetDepartmentsAsync()` |
| 27 | `PUT` | `api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}` | `CompanyMasterController` | `SetDepartmentOfficeScheduleAssignment` | `_officeScheduleAssignments.SetDepartmentAsync()` |
| 28 | `POST` | `api/CompanyMaster/UpdateDepartment` | `CompanyMasterController` | `AddEditDepartment` | `CurrentContext.UserId()` |
| 29 | `GET` | `api/CompanyMaster/GetDepartmentList` | `CompanyMasterController` | `GetDepartmentList` | `_companyMasterService.GetDepartmentList()` |
| 30 | `DELETE` | `api/CompanyMaster/DeleteDepartment/{departmentId}` | `CompanyMasterController` | `DeleteDepartment` | `_companyMasterService.DeleteDepartment()` |
| 31 | `PATCH` | `api/CompanyMaster/UpdateModuleAccess/{moduleId}` | `CompanyMasterController` | `UpdateModuleAccess` | `string.IsNullOrEmpty()` |
| 32 | `GET` | `api/CompanyMaster/GetAllModuleDetails` | `CompanyMasterController` | `GetAllModuleDetails` | `CurrentContext.CompanyId()` |
| 33 | `POST` | `api/CompanyMaster/AddUpdateJobTitle` | `CompanyMasterController` | `AddUpdateJobTitle` | `CurrentContext.UserId()` |
| 34 | `GET` | `api/CompanyMaster/GetJobTitles` | `CompanyMasterController` | `GetAllJobTitles` | `_companyMasterService.GetJobTitles()` |
| 35 | `DELETE` | `api/CompanyMaster/DeleteJobTitle/{jobTitleId}` | `CompanyMasterController` | `DeleteJobTitle` | `_companyMasterService.DeleteJobTitle()` |
| 36 | `POST` | `api/CompanyMaster/CreateCustomAttribute` | `CompanyMasterController` | `CreateCustomAttribute` | `CurrentContext.CompanyId()` |
| 37 | `GET` | `api/CompanyMaster/GetAllCustomAttribute` | `CompanyMasterController` | `GetAllCustomAttributes` | `CurrentContext.CompanyId()` |
| 38 | `GET` | `api/CompanyMaster/GetCustomAttributeById/{customAttributeId}` | `CompanyMasterController` | `GetCustomAttributeById` | `CurrentContext.CompanyId()` |
| 39 | `PUT` | `api/CompanyMaster/UpdateCustomAttribute` | `CompanyMasterController` | `UpdateCustomAttribute` | `CurrentContext.CompanyId()` |
| 40 | `DELETE` | `api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}` | `CompanyMasterController` | `DeleteCustomAttribute` | `CurrentContext.CompanyId()` |
| 41 | `DELETE` | `api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}` | `CompanyMasterController` | `DeleteCustomAttributeValue` | `string.IsNullOrEmpty()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/Calendar/GetAllCalendarItems

### Purpose

This endpoint is implemented by `CalendarController.GetAllCalendarItems()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Controller: `CalendarController`
- Action: `GetAllCalendarItems()`
- Approximate source line at generation: 26
- Direct delegated call found: `_calendarServices.GetAllCalendarItems()`

### HTTP contract

- Method: `POST`
- Route: `api/Calendar/GetAllCalendarItems`
- Return declaration: `async Task<Result<CalendarResponseDto>>`
- Parameters exactly as declared: `[FromBody] CalendarFilters? filters`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetAllCalendarItems")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CalendarFilters? filters`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Calendar/GetAllCalendarItems`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CalendarController.GetAllCalendarItems()` receives the bound values.
4. The action delegates to `_calendarServices.GetAllCalendarItems()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAllCalendarItems()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:38`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAllCalendarItems()` at `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:10`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<CalendarResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 2. POST api/Calendar/AddUpdateCalendarItem

### Purpose

This endpoint is implemented by `CalendarController.AddUpdateCalendarItem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Controller: `CalendarController`
- Action: `AddUpdateCalendarItem()`
- Approximate source line at generation: 40
- Direct delegated call found: `_calendarServices.AddUpdateCalendarItem()`

### HTTP contract

- Method: `POST`
- Route: `api/Calendar/AddUpdateCalendarItem`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromForm] CalendarRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddUpdateCalendarItem")] [HttpPost] [ModulePermission(AppModule.Calendar, [Permission.Create, Permission.Edit]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] CalendarRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Calendar/AddUpdateCalendarItem`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CalendarController.AddUpdateCalendarItem()` receives the bound values.
4. The action delegates to `_calendarServices.AddUpdateCalendarItem()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddUpdateCalendarItem()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:140`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: CALENDAR_IMPORT_NO_NEW_ITEMS.
- Explicit workflow/status literals found: .
- Method match: `AddUpdateCalendarItem()` at `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:8`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 3. POST api/Calendar/ImportCalendarItems

### Purpose

This endpoint is implemented by `CalendarController.ImportCalendarItems()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Controller: `CalendarController`
- Action: `ImportCalendarItems()`
- Approximate source line at generation: 50
- Direct delegated call found: `_calendarServices.ImportCalendarItems()`

### HTTP contract

- Method: `POST`
- Route: `api/Calendar/ImportCalendarItems`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] CalendarBulkImportRequest model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ImportCalendarItems")] [HttpPost] [ModulePermission(AppModule.Calendar, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CalendarBulkImportRequest model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Calendar/ImportCalendarItems`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CalendarController.ImportCalendarItems()` receives the bound values.
4. The action delegates to `_calendarServices.ImportCalendarItems()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ImportCalendarItems()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:201`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: CALENDAR_IMPORT_NO_NEW_ITEMS.
- Explicit workflow/status literals found: .
- Method match: `ImportCalendarItems()` at `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:9`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 4. DELETE api/Calendar/DeleteCalendarItem/{itemId}

### Purpose

This endpoint is implemented by `CalendarController.DeleteCalendarItem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Controller: `CalendarController`
- Action: `DeleteCalendarItem()`
- Approximate source line at generation: 59
- Direct delegated call found: `_calendarServices.DeleteItem()`

### HTTP contract

- Method: `DELETE`
- Route: `api/Calendar/DeleteCalendarItem/{itemId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string itemId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteCalendarItem/{itemId}")] [HttpDelete] [ModulePermission(AppModule.Calendar, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string itemId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/Calendar/DeleteCalendarItem/{itemId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CalendarController.DeleteCalendarItem()` receives the bound values.
4. The action delegates to `_calendarServices.DeleteItem()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteItem()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:240`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteItem()` at `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:11`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 5. POST api/Calendar/GetHolidays

### Purpose

This endpoint is implemented by `CalendarController.GetHolidays()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CalendarController.cs`
- Controller: `CalendarController`
- Action: `GetHolidays()`
- Approximate source line at generation: 68
- Direct delegated call found: `_calendarServices.GetHolidays()`

### HTTP contract

- Method: `POST`
- Route: `api/Calendar/GetHolidays`
- Return declaration: `async Task<Result<HolidayResponseDto>>`
- Parameters exactly as declared: `[FromBody] HolidayFilter filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetHolidays")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] HolidayFilter filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Calendar/GetHolidays`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CalendarController.GetHolidays()` receives the bound values.
4. The action delegates to `_calendarServices.GetHolidays()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetHolidays()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:284`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetHolidays()` at `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:12`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<HolidayResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 6. GET api/Company/GetAllCompanyList

### Purpose

This endpoint is implemented by `CompanyController.GetAllCompanyList()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetAllCompanyList()`
- Approximate source line at generation: 30
- Direct delegated call found: `_companyService.GetAllCompanyList()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/GetAllCompanyList`
- Return declaration: `async Task<Result<Company>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllCompanyList")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/GetAllCompanyList`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetAllCompanyList()` receives the bound values.
4. The action delegates to `_companyService.GetAllCompanyList()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAllCompanyList()` at `Codeji.CMS.Services/Companies/CompanyService.cs:259`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAllCompanyList()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:13`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Company>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 7. GET api/Company/GetCompanyDetails

### Purpose

This endpoint is implemented by `CompanyController.GetCompanyDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetCompanyDetails()`
- Approximate source line at generation: 42
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/GetCompanyDetails`
- Return declaration: `async Task<Result<Company>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetCompanyDetails")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/GetCompanyDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetCompanyDetails()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Company>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 8. GET api/Company/career

### Purpose

This endpoint is implemented by `CompanyController.GetPublicCareerCompany()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetPublicCareerCompany()`
- Approximate source line at generation: 52
- Direct delegated call found: `_companyService.GetPublicCareerCompany()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/career`
- Return declaration: `Task<Result<PublicCareerCompanyDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("career")] [AllowAnonymous]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/career`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetPublicCareerCompany()` receives the bound values.
4. The action delegates to `_companyService.GetPublicCareerCompany()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPublicCareerCompany()` at `Codeji.CMS.Services/Companies/CompanyService.cs:271`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetPublicCareerCompany()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<PublicCareerCompanyDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 9. GET api/Company/career/{publicCompanyCode}

### Purpose

This endpoint is implemented by `CompanyController.GetPublicCareerCompany()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetPublicCareerCompany()`
- Approximate source line at generation: 58
- Direct delegated call found: `_companyService.GetPublicCareerCompany()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/career/{publicCompanyCode}`
- Return declaration: `Task<Result<PublicCareerCompanyDto>>`
- Parameters exactly as declared: `string publicCompanyCode`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("career/{publicCompanyCode}")] [AllowAnonymous]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string publicCompanyCode`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/career/{publicCompanyCode}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetPublicCareerCompany()` receives the bound values.
4. The action delegates to `_companyService.GetPublicCareerCompany()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPublicCareerCompany()` at `Codeji.CMS.Services/Companies/CompanyService.cs:271`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetPublicCareerCompany()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<PublicCareerCompanyDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 10. GET api/Company/theme

### Purpose

This endpoint is implemented by `CompanyController.GetCurrentCompanyTheme()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetCurrentCompanyTheme()`
- Approximate source line at generation: 62
- Direct delegated call found: `_companyService.GetCompanyDetails()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/theme`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet("theme")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/theme`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetCurrentCompanyTheme()` receives the bound values.
4. The action delegates to `_companyService.GetCompanyDetails()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetCompanyDetails()` at `Codeji.CMS.Services/Companies/CompanyService.cs:375`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetCompanyDetails()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:15`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
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

## 11. POST api/Company/UpdateCompanyDetails

### Purpose

This endpoint is implemented by `CompanyController.UpdateCompanyDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `UpdateCompanyDetails()`
- Approximate source line at generation: 71
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/UpdateCompanyDetails`
- Return declaration: `async Task<Result<Company>>`
- Parameters exactly as declared: `[FromForm] UpdateCompanyInfoRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("UpdateCompanyDetails")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] UpdateCompanyInfoRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/UpdateCompanyDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.UpdateCompanyDetails()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<Company>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 12. POST api/Company/AddPolicy

### Purpose

This endpoint is implemented by `CompanyController.AddPolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `AddPolicy()`
- Approximate source line at generation: 82
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/AddPolicy`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] CreatePolicyRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("AddPolicy")] [ModulePermission(AppModule.Policy, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CreatePolicyRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/AddPolicy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.AddPolicy()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 13. POST api/Company/UpdatePolicy

### Purpose

This endpoint is implemented by `CompanyController.UpdatePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `UpdatePolicy()`
- Approximate source line at generation: 92
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/UpdatePolicy`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdatePolicyRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("UpdatePolicy")] [ModulePermission(AppModule.Policy, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdatePolicyRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/UpdatePolicy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.UpdatePolicy()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 14. GET api/Company/GetAllPolicies

### Purpose

This endpoint is implemented by `CompanyController.GetAllPolicies()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetAllPolicies()`
- Approximate source line at generation: 102
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/GetAllPolicies`
- Return declaration: `async Task<Result<PolicyResponseModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllPolicies")] [ModulePermission(AppModule.Policy, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/GetAllPolicies`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetAllPolicies()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<PolicyResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. DELETE api/Company/DeletePolicy/{policyId}

### Purpose

This endpoint is implemented by `CompanyController.DeletePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `DeletePolicy()`
- Approximate source line at generation: 113
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `DELETE`
- Route: `api/Company/DeletePolicy/{policyId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string policyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpDelete] [Route("DeletePolicy/{policyId}")] [ModulePermission(AppModule.Policy, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string policyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/Company/DeletePolicy/{policyId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.DeletePolicy()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 16. POST api/Company/CreatePolicyVersion

### Purpose

This endpoint is implemented by `CompanyController.AddPolicyVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `AddPolicyVersion()`
- Approximate source line at generation: 122
- Direct delegated call found: `_companyService.AddPolicyVersion()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/CreatePolicyVersion`
- Return declaration: `async Task<Result<PolicyVersionResponseModel>>`
- Parameters exactly as declared: `[FromForm] PolicyVersionRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("CreatePolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] PolicyVersionRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/CreatePolicyVersion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.AddPolicyVersion()` receives the bound values.
4. The action delegates to `_companyService.AddPolicyVersion()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddPolicyVersion()` at `Codeji.CMS.Services/Companies/CompanyService.cs:632`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `AddPolicyVersion()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:34`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<PolicyVersionResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 17. POST api/Company/EditPolicyVersion

### Purpose

This endpoint is implemented by `CompanyController.EditPolicyVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `EditPolicyVersion()`
- Approximate source line at generation: 130
- Direct delegated call found: `_companyService.EditPolicyVersion()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/EditPolicyVersion`
- Return declaration: `async Task<Result<PolicyVersionResponseModel>>`
- Parameters exactly as declared: `[FromForm] PolicyVersionUpdateModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("EditPolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] PolicyVersionUpdateModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/EditPolicyVersion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.EditPolicyVersion()` receives the bound values.
4. The action delegates to `_companyService.EditPolicyVersion()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `EditPolicyVersion()` at `Codeji.CMS.Services/Companies/CompanyService.cs:720`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `EditPolicyVersion()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:35`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<PolicyVersionResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 18. POST api/Company/SetCurrentPolicyVersion

### Purpose

This endpoint is implemented by `CompanyController.SetCurrentPolicyVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `SetCurrentPolicyVersion()`
- Approximate source line at generation: 138
- Direct delegated call found: `_companyService.SetCurrentPolicyVersion()`

### HTTP contract

- Method: `POST`
- Route: `api/Company/SetCurrentPolicyVersion`
- Return declaration: `Task<Result<PolicyVersionResponseModel>>`
- Parameters exactly as declared: `[FromBody] SetCurrentPolicyVersionRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("SetCurrentPolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] SetCurrentPolicyVersionRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Company/SetCurrentPolicyVersion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.SetCurrentPolicyVersion()` receives the bound values.
4. The action delegates to `_companyService.SetCurrentPolicyVersion()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SetCurrentPolicyVersion()` at `Codeji.CMS.Services/Companies/CompanyService.cs:895`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `SetCurrentPolicyVersion()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:36`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<PolicyVersionResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 19. GET api/Company/GetAllPolicyVersion/{policyId}

### Purpose

This endpoint is implemented by `CompanyController.GetAllPolicyVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetAllPolicyVersion()`
- Approximate source line at generation: 146
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/GetAllPolicyVersion/{policyId}`
- Return declaration: `async Task<Result<PolicyVersionResponseModel>>`
- Parameters exactly as declared: `string policyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllPolicyVersion/{policyId}")] [ModulePermission(AppModule.Policy, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string policyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/GetAllPolicyVersion/{policyId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetAllPolicyVersion()` receives the bound values.
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

The compile-time return declaration is `async Task<Result<PolicyVersionResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 20. DELETE api/Company/DeletePolicyVersion/{policyVersionId}

### Purpose

This endpoint is implemented by `CompanyController.DeletePolicyVersion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `DeletePolicyVersion()`
- Approximate source line at generation: 156
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `DELETE`
- Route: `api/Company/DeletePolicyVersion/{policyVersionId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string policyVersionId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpDelete] [Route("DeletePolicyVersion/{policyVersionId}")] [ModulePermission(AppModule.Policy, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string policyVersionId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/Company/DeletePolicyVersion/{policyVersionId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.DeletePolicyVersion()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 21. GET api/Company/GetPolicyDocument/{policyVersionId}

### Purpose

This endpoint is implemented by `CompanyController.GetPolicyDocument()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyController.cs`
- Controller: `CompanyController`
- Action: `GetPolicyDocument()`
- Approximate source line at generation: 165
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `GET`
- Route: `api/Company/GetPolicyDocument/{policyVersionId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string policyVersionId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetPolicyDocument/{policyVersionId}")] [ModulePermission(AppModule.Policy, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string policyVersionId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Company/GetPolicyDocument/{policyVersionId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyController.GetPolicyDocument()` receives the bound values.
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

## 22. GET api/CompanyMaster/OfficeSchedule

### Purpose

This endpoint is implemented by `CompanyMasterController.GetOfficeSchedule()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetOfficeSchedule()`
- Approximate source line at generation: 38
- Direct delegated call found: `_officeScheduleSettings.GetAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/OfficeSchedule`
- Return declaration: `Task<Result<OfficeScheduleSettingsDto>>`
- Parameters exactly as declared: `CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("OfficeSchedule")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/OfficeSchedule`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetOfficeSchedule()` receives the bound values.
4. The action delegates to `_officeScheduleSettings.GetAsync()` or executes its inspected body.
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

## 23. GET api/CompanyMaster/OfficeSchedules

### Purpose

This endpoint is implemented by `CompanyMasterController.GetOfficeSchedules()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetOfficeSchedules()`
- Approximate source line at generation: 44
- Direct delegated call found: `_officeScheduleSettings.GetAllAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/OfficeSchedules`
- Return declaration: `Task<Result<OfficeScheduleSettingsDto>>`
- Parameters exactly as declared: `CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("OfficeSchedules")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/OfficeSchedules`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetOfficeSchedules()` receives the bound values.
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

## 24. PUT api/CompanyMaster/OfficeSchedule

### Purpose

This endpoint is implemented by `CompanyMasterController.SaveOfficeSchedule()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `SaveOfficeSchedule()`
- Approximate source line at generation: 50
- Direct delegated call found: `_officeScheduleSettings.SaveAsync()`

### HTTP contract

- Method: `PUT`
- Route: `api/CompanyMaster/OfficeSchedule`
- Return declaration: `Task<Result<OfficeScheduleSettingsDto>>`
- Parameters exactly as declared: `OfficeScheduleSettingsDto request, CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("OfficeSchedule")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `OfficeScheduleSettingsDto request, CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/CompanyMaster/OfficeSchedule`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.SaveOfficeSchedule()` receives the bound values.
4. The action delegates to `_officeScheduleSettings.SaveAsync()` or executes its inspected body.
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

## 25. DELETE api/CompanyMaster/OfficeSchedule/{scheduleId}

### Purpose

This endpoint is implemented by `CompanyMasterController.DeleteOfficeSchedule()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `DeleteOfficeSchedule()`
- Approximate source line at generation: 56
- Direct delegated call found: `_officeScheduleSettings.DeleteAsync()`

### HTTP contract

- Method: `DELETE`
- Route: `api/CompanyMaster/OfficeSchedule/{scheduleId}`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string scheduleId, CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("OfficeSchedule/{scheduleId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string scheduleId, CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/CompanyMaster/OfficeSchedule/{scheduleId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.DeleteOfficeSchedule()` receives the bound values.
4. The action delegates to `_officeScheduleSettings.DeleteAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

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

## 26. GET api/CompanyMaster/DepartmentOfficeScheduleAssignments

### Purpose

This endpoint is implemented by `CompanyMasterController.GetDepartmentOfficeScheduleAssignments()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetDepartmentOfficeScheduleAssignments()`
- Approximate source line at generation: 62
- Direct delegated call found: `_officeScheduleAssignments.GetDepartmentsAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/DepartmentOfficeScheduleAssignments`
- Return declaration: `Task<Result<DepartmentOfficeScheduleAssignmentDto>>`
- Parameters exactly as declared: `CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("DepartmentOfficeScheduleAssignments")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/DepartmentOfficeScheduleAssignments`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetDepartmentOfficeScheduleAssignments()` receives the bound values.
4. The action delegates to `_officeScheduleAssignments.GetDepartmentsAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<DepartmentOfficeScheduleAssignmentDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 27. PUT api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}

### Purpose

This endpoint is implemented by `CompanyMasterController.SetDepartmentOfficeScheduleAssignment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `SetDepartmentOfficeScheduleAssignment()`
- Approximate source line at generation: 68
- Direct delegated call found: `_officeScheduleAssignments.SetDepartmentAsync()`

### HTTP contract

- Method: `PUT`
- Route: `api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}`
- Return declaration: `Task<Result<DepartmentOfficeScheduleAssignmentDto>>`
- Parameters exactly as declared: `string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("DepartmentOfficeScheduleAssignment/{departmentId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.SetDepartmentOfficeScheduleAssignment()` receives the bound values.
4. The action delegates to `_officeScheduleAssignments.SetDepartmentAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- No implementation definition was conclusively matched in the configured service source set; inspect the action body, injected interface and registration.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<DepartmentOfficeScheduleAssignmentDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 28. POST api/CompanyMaster/UpdateDepartment

### Purpose

This endpoint is implemented by `CompanyMasterController.AddEditDepartment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `AddEditDepartment()`
- Approximate source line at generation: 76
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/CompanyMaster/UpdateDepartment`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `List<DepartmentRequestDto> data`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("UpdateDepartment")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `List<DepartmentRequestDto> data`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/CompanyMaster/UpdateDepartment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.AddEditDepartment()` receives the bound values.
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

## 29. GET api/CompanyMaster/GetDepartmentList

### Purpose

This endpoint is implemented by `CompanyMasterController.GetDepartmentList()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetDepartmentList()`
- Approximate source line at generation: 85
- Direct delegated call found: `_companyMasterService.GetDepartmentList()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/GetDepartmentList`
- Return declaration: `async Task<Result<DepartmentResponseDto>>`
- Parameters exactly as declared: `[FromQuery] bool? isActive`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetDepartmentList")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] bool? isActive`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/GetDepartmentList`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetDepartmentList()` receives the bound values.
4. The action delegates to `_companyMasterService.GetDepartmentList()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetDepartmentList()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:85`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetDepartmentList()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:15`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<DepartmentResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 30. DELETE api/CompanyMaster/DeleteDepartment/{departmentId}

### Purpose

This endpoint is implemented by `CompanyMasterController.DeleteDepartment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `DeleteDepartment()`
- Approximate source line at generation: 94
- Direct delegated call found: `_companyMasterService.DeleteDepartment()`

### HTTP contract

- Method: `DELETE`
- Route: `api/CompanyMaster/DeleteDepartment/{departmentId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string departmentId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteDepartment/{departmentId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string departmentId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/CompanyMaster/DeleteDepartment/{departmentId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.DeleteDepartment()` receives the bound values.
4. The action delegates to `_companyMasterService.DeleteDepartment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteDepartment()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:107`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteDepartment()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
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

## 31. PATCH api/CompanyMaster/UpdateModuleAccess/{moduleId}

### Purpose

This endpoint is implemented by `CompanyMasterController.UpdateModuleAccess()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `UpdateModuleAccess()`
- Approximate source line at generation: 109
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `PATCH`
- Route: `api/CompanyMaster/UpdateModuleAccess/{moduleId}`
- Return declaration: `async Task<Result<string[]>>`
- Parameters exactly as declared: `string moduleId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPatch] [Route("UpdateModuleAccess/{moduleId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string moduleId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/CompanyMaster/UpdateModuleAccess/{moduleId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.UpdateModuleAccess()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:143`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: CALENDAR_IMPORT_NO_NEW_ITEMS.
- Explicit workflow/status literals found: .
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:69`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Companies/CompanyService.cs:332`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<string[]>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 32. GET api/CompanyMaster/GetAllModuleDetails

### Purpose

This endpoint is implemented by `CompanyMasterController.GetAllModuleDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetAllModuleDetails()`
- Approximate source line at generation: 125
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/GetAllModuleDetails`
- Return declaration: `async Task<Result<AllModuleDetailsResponseModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetAllModuleDetails")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/GetAllModuleDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetAllModuleDetails()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<AllModuleDetailsResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 33. POST api/CompanyMaster/AddUpdateJobTitle

### Purpose

This endpoint is implemented by `CompanyMasterController.AddUpdateJobTitle()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `AddUpdateJobTitle()`
- Approximate source line at generation: 140
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/CompanyMaster/AddUpdateJobTitle`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `List<JobTitleRequestDto> data`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddUpdateJobTitle")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `List<JobTitleRequestDto> data`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/CompanyMaster/AddUpdateJobTitle`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.AddUpdateJobTitle()` receives the bound values.
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

## 34. GET api/CompanyMaster/GetJobTitles

### Purpose

This endpoint is implemented by `CompanyMasterController.GetAllJobTitles()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetAllJobTitles()`
- Approximate source line at generation: 149
- Direct delegated call found: `_companyMasterService.GetJobTitles()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/GetJobTitles`
- Return declaration: `async Task<Result<JobTitleResponseDto>>`
- Parameters exactly as declared: `[FromQuery] bool? isActive, [FromQuery] string? departmentId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetJobTitles")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] bool? isActive, [FromQuery] string? departmentId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/GetJobTitles`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetAllJobTitles()` receives the bound values.
4. The action delegates to `_companyMasterService.GetJobTitles()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetJobTitles()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:175`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetJobTitles()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:22`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<JobTitleResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 35. DELETE api/CompanyMaster/DeleteJobTitle/{jobTitleId}

### Purpose

This endpoint is implemented by `CompanyMasterController.DeleteJobTitle()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `DeleteJobTitle()`
- Approximate source line at generation: 158
- Direct delegated call found: `_companyMasterService.DeleteJobTitle()`

### HTTP contract

- Method: `DELETE`
- Route: `api/CompanyMaster/DeleteJobTitle/{jobTitleId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string jobTitleId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteJobTitle/{jobTitleId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string jobTitleId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/CompanyMaster/DeleteJobTitle/{jobTitleId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.DeleteJobTitle()` receives the bound values.
4. The action delegates to `_companyMasterService.DeleteJobTitle()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteJobTitle()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:301`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CUSTOM_ATTRIBUTE_ID_REQUIRED, CUSTOM_ATTRIBUTE_TITLE_REQUIRED, CUSTOM_ATTRIBUTE_NOT_FOUND.
- Explicit workflow/status literals found: .
- Method match: `DeleteJobTitle()` at `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:23`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
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

## 36. POST api/CompanyMaster/CreateCustomAttribute

### Purpose

This endpoint is implemented by `CompanyMasterController.CreateCustomAttribute()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `CreateCustomAttribute()`
- Approximate source line at generation: 169
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/CompanyMaster/CreateCustomAttribute`
- Return declaration: `async Task<Result<CustomAttributeResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("CreateCustomAttribute")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/CompanyMaster/CreateCustomAttribute`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.CreateCustomAttribute()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<CustomAttributeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 37. GET api/CompanyMaster/GetAllCustomAttribute

### Purpose

This endpoint is implemented by `CompanyMasterController.GetAllCustomAttributes()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetAllCustomAttributes()`
- Approximate source line at generation: 187
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/GetAllCustomAttribute`
- Return declaration: `async Task<Result<CustomAttributeResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetAllCustomAttribute")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/GetAllCustomAttribute`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetAllCustomAttributes()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<CustomAttributeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 38. GET api/CompanyMaster/GetCustomAttributeById/{customAttributeId}

### Purpose

This endpoint is implemented by `CompanyMasterController.GetCustomAttributeById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `GetCustomAttributeById()`
- Approximate source line at generation: 199
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/CompanyMaster/GetCustomAttributeById/{customAttributeId}`
- Return declaration: `async Task<Result<CustomAttributeByIdResponseDto>>`
- Parameters exactly as declared: `string customAttributeId, [FromQuery] bool? active`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetCustomAttributeById/{customAttributeId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string customAttributeId, [FromQuery] bool? active`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CompanyMaster/GetCustomAttributeById/{customAttributeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.GetCustomAttributeById()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<CustomAttributeByIdResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 39. PUT api/CompanyMaster/UpdateCustomAttribute

### Purpose

This endpoint is implemented by `CompanyMasterController.UpdateCustomAttribute()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `UpdateCustomAttribute()`
- Approximate source line at generation: 209
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `PUT`
- Route: `api/CompanyMaster/UpdateCustomAttribute`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] CustomAttributeRequestDto data`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("UpdateCustomAttribute")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CustomAttributeRequestDto data`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/CompanyMaster/UpdateCustomAttribute`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.UpdateCustomAttribute()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 40. DELETE api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}

### Purpose

This endpoint is implemented by `CompanyMasterController.DeleteCustomAttribute()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `DeleteCustomAttribute()`
- Approximate source line at generation: 219
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `DELETE`
- Route: `api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string customAttributeId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteCustomAttribute/{customAttributeId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string customAttributeId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.DeleteCustomAttribute()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CompanyId()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
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

## 41. DELETE api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}

### Purpose

This endpoint is implemented by `CompanyMasterController.DeleteCustomAttributeValue()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CompanyMasterController.cs`
- Controller: `CompanyMasterController`
- Action: `DeleteCustomAttributeValue()`
- Approximate source line at generation: 229
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `DELETE`
- Route: `api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string customAttributeValueId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteCustomAttributeValue/{customAttributeValueId}")] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string customAttributeValueId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CompanyMasterController.DeleteCustomAttributeValue()` receives the bound values.
4. The action delegates to `string.IsNullOrEmpty()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Calendar/CalendarServices.cs:143`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: CALENDAR_IMPORT_NO_NEW_ITEMS.
- Explicit workflow/status literals found: .
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Companies/CompanyMasterService.cs:69`.
- Static signals in the inspected method window: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `IsNullOrEmpty()` at `Codeji.CMS.Services/Companies/CompanyService.cs:332`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
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

## Service Source Inventory

### `Codeji.CMS.Services/Calendar/CalendarServices.cs`

- Lines: 341
- Extracted symbols: IMongoDbRepository, ILogger, CompanyId, CalendarServices, Task, result, calendarItems, year, if, date, today, dateFormat, employeesList, birthdaysThisYear, thisYearBirthday, workAnniversariesThisYear, thisYearAnniversary, AddUpdateCalendarItem, newItem, existingItem, existingCalendarItem, ImportCalendarItems, normalized, duplicateRows, existing, additions, saved, DeleteItem
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs`

- Lines: 13
- Extracted symbols: ICalendarServices, AddUpdateCalendarItem, ImportCalendarItems, DeleteItem
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Companies/CompanyMasterService.cs`

- Lines: 579
- Extracted symbols: IMongoDbRepository, CompanyMasterService, UpdateDepartments, companyId, departments, result, Task, data, DeleteDepartment, modulesPermission, rolePermissions, roleId, hasAccess
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Companies/CompanyService.cs`

- Lines: 1002
- Extracted symbols: IMongoDbRepository, ILogger, CompanyService, Register, result, emailExists, companyId, adminRole, administratorRole, user, company, notificationPreferenceSetting, userResult, SendCompanyUserVerificationEmail, SeedDefaultDepartmentsAndJobTitles, token, tokenHash, tokenExpiryHours, securityToken
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Companies/DefaultCompanySeeds.cs`

- Lines: 224
- Extracted symbols: readonly, List, langs, now, Dictionary, result
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs`

- Lines: 32
- Extracted symbols: ICompanyMasterService, UpdateDepartments, DeleteDepartment, AddUpdateJobTitle, DeleteJobTitle, CreateCustomAttribute, UpdateCustomAttribute, DeleteCustomAttribute, DeleteCustomAttributeValue
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs`

- Lines: 41
- Extracted symbols: ICompanyService, Register, IsActiveCompanyExist, UpdateCompanyLogo, GetCompanyExistingLogo, AddCompanyLogo, AddPolicy, UpdatePolicy, DeletePolicy, DeletePolicyVersion
- Use: follow the controller delegation into this file when the symbol matches the action.

