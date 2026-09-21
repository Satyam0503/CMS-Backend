# Attendance and Work From Home API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## Attendance Report employee roster

The Attendance Report obtains its cards from `api/User/GetAllEmployees` using `employmentStatus=AttendanceRoster`. This tenant-scoped roster option returns active employees, legacy blank-status employees, and `NoticePeriod` employees. It excludes `Resigned` and `Terminated` employees so completed exits do not affect attendance-card pagination. `All` remains available for employee-management views that intentionally need historical records.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/admin/attendance` | `AdminAttendanceController` | `Add` | `_service.AddManualAttendance()` |
| 2 | `GET` | `api/admin/attendance/{userId}` | `AdminAttendanceController` | `GetByUser` | `_service.GetByUser()` |
| 3 | `GET` | `api/admin/attendance/{userId}/date` | `AdminAttendanceController` | `GetByUserAndDate` | `_service.GetByUserAndDate()` |
| 4 | `PUT` | `api/admin/attendance/{userId}` | `AdminAttendanceController` | `Update` | `_service.UpdateAttendance()` |
| 5 | `POST` | `api/admin/attendance/GetAllAttendanceItems` | `AdminAttendanceController` | `GetAll` | `_service.GetAttendanceByDateRange()` |
| 6 | `POST` | `api/admin/attendance/effective-schedules` | `AdminAttendanceController` | `GetEffectiveSchedules` | `effectiveSchedules.ResolveManyAsync()` |
| 7 | `GET` | `api/admin/attendance/export/monthly` | `AdminAttendanceController` | `ExportMonthly` | `_service.ExportMonthlyAttendanceAsync()` |
| 8 | `POST` | `api/admin/attendance/initialize-month` | `AdminAttendanceController` | `InitializeMonth` | `initialization.InitializeMonthAsync()` |
| 9 | `GET` | `api/admin/attendance/correction-requests` | `AdminAttendanceController` | `CorrectionRequests` | `corrections.GetAll()` |
| 10 | `PUT` | `api/admin/attendance/correction-requests/{requestId}` | `AdminAttendanceController` | `ReviewCorrection` | `CurrentContext.CompanyId()` |
| 11 | `GET` | `api/admin/attendance/my-calendar` | `AdminAttendanceController` | `GetMyCalendar` | `fromDate.AddMonths()` |
| 12 | `GET` | `api/attendance/penalty/employees/{employeeUserId}/policy` | `AttendancePenaltyController` | `GetEmployeePolicy` | `_service.GetEmployeePolicy()` |
| 13 | `POST` | `api/attendance/penalty/employees/{employeeUserId}/policy` | `AttendancePenaltyController` | `SaveEmployeePolicy` | `_service.SaveEmployeePolicy()` |
| 14 | `POST` | `api/attendance/penalty/exceptions/recalculate` | `AttendancePenaltyController` | `Recalculate` | `_service.Recalculate()` |
| 15 | `POST` | `api/attendance/penalty/exceptions/search` | `AttendancePenaltyController` | `Search` | `_service.GetExceptions()` |
| 16 | `PATCH` | `api/attendance/penalty/exceptions/{id}/review` | `AttendancePenaltyController` | `Review` | `_service.Review()` |
| 17 | `POST` | `api/attendance/penalty/exceptions/{id}/resolve-attendance` | `AttendancePenaltyController` | `ResolveAttendance` | `_service.ResolveAttendanceException()` |
| 18 | `PATCH` | `api/attendance/penalty/exceptions/{id}/override` | `AttendancePenaltyController` | `Override` | `_service.OverrideException()` |
| 19 | `GET` | `api/attendance/penalty/month/lock-status` | `AttendancePenaltyController` | `GetMonthLockStatus` | `_service.GetMonthLockStatus()` |
| 20 | `POST` | `api/attendance/penalty/month/validate-lock` | `AttendancePenaltyController` | `ValidateAndLock` | `_service.ValidateAndLock()` |
| 21 | `GET` | `api/attendance-status-settings` | `AttendanceStatusSettingsController` | `Get` | `_service.Get()` |
| 22 | `POST` | `api/attendance-status-settings` | `AttendanceStatusSettingsController` | `Save` | `_service.Save()` |
| 23 | `DELETE` | `api/attendance-status-settings/{statusId}` | `AttendanceStatusSettingsController` | `Delete` | `_service.Delete()` |
| 24 | `GET` | `api/attendance/me/grid` | `MyAttendanceController` | `Grid` | `CurrentContext.CompanyId()` |
| 25 | `GET` | `api/attendance/me/calendar` | `MyAttendanceController` | `Calendar` | `CurrentContext.CompanyId()` |
| 26 | `POST` | `api/attendance/me/correction-requests` | `MyAttendanceController` | `RequestCorrection` | `CurrentContext.CompanyId()` |
| 27 | `GET` | `api/attendance/me/correction-requests` | `MyAttendanceController` | `MyCorrectionRequests` | `corrections.GetAll()` |
| 28 | `GET` | `api/attendance/weekly-offs` | `WeeklyOffSettingsController` | `Get` | `service.Get()` |
| 29 | `POST` | `api/attendance/weekly-offs` | `WeeklyOffSettingsController` | `Save` | `service.Save()` |
| 30 | `GET` | `api/wfh/policy` | `WorkFromHomeController` | `Policy` | `service.GetPolicy()` |
| 31 | `GET` | `api/wfh/context` | `WorkFromHomeController` | `Context` | `service.GetEmployeeContext()` |
| 32 | `PUT` | `api/wfh/policy` | `WorkFromHomeController` | `SavePolicy` | `service.SavePolicy()` |
| 33 | `POST` | `api/wfh/policy` | `WorkFromHomeController` | `SavePolicyPost` | `service.SavePolicy()` |
| 34 | `GET` | `api/wfh/allocations` | `WorkFromHomeController` | `Allocations` | `service.GetEmployeeAllocations()` |
| 35 | `PUT` | `api/wfh/allocations` | `WorkFromHomeController` | `SaveAllocation` | `service.SaveEmployeeAllocation()` |
| 36 | `POST` | `api/wfh/requests` | `WorkFromHomeController` | `Create` | `service.Create()` |
| 37 | `GET` | `api/wfh/requests/my` | `WorkFromHomeController` | `Mine` | `service.GetMy()` |
| 38 | `GET` | `api/wfh/requests/team` | `WorkFromHomeController` | `Team` | `service.GetTeam()` |
| 39 | `GET` | `api/wfh/requests/all` | `WorkFromHomeController` | `All` | `service.GetAll()` |
| 40 | `POST` | `api/wfh/requests/{id}/approve` | `WorkFromHomeController` | `Approve` | `service.Decide()` |
| 41 | `POST` | `api/wfh/requests/{id}/reject` | `WorkFromHomeController` | `Reject` | `service.Decide()` |
| 42 | `POST` | `api/wfh/requests/{id}/return` | `WorkFromHomeController` | `Return` | `service.Decide()` |
| 43 | `POST` | `api/wfh/requests/{id}/cancel` | `WorkFromHomeController` | `Cancel` | `service.Cancel()` |
| 44 | `POST` | `api/wfh/requests/{id}/check-in` | `WorkFromHomeController` | `CheckIn` | `service.CheckIn()` |
| 45 | `POST` | `api/wfh/requests/{id}/check-out` | `WorkFromHomeController` | `CheckOut` | `service.CheckOut()` |
| 46 | `GET` | `api/wfh/requests/{id}/timing` | `WorkFromHomeController` | `Timing` | `service.GetTiming()` |
| 47 | `PATCH` | `api/wfh/attendance-exceptions/{id}/hours` | `WorkFromHomeController` | `CorrectHours` | `service.CorrectInsufficientHours()` |
| 48 | `PATCH` | `api/wfh/attendance-exceptions/{id}/resolve-conflict` | `WorkFromHomeController` | `ResolveConflict` | `service.ResolveAttendanceConflict()` |
| 49 | `GET` | `api/attendance/remark-options` | `WorkFromHomeController` | `RemarkOptions` | `service.GetRemarkOptions()` |
| 50 | `POST` | `api/attendance/remark-options` | `WorkFromHomeController` | `SaveRemarkOption` | `service.SaveRemarkOption()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/admin/attendance

### Purpose

This endpoint is implemented by `AdminAttendanceController.Add()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `Add()`
- Approximate source line at generation: 28
- Direct delegated call found: `_service.AddManualAttendance()`

### HTTP contract

- Method: `POST`
- Route: `api/admin/attendance`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] AdminAttendanceCreateDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost] [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AdminAttendanceCreateDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/admin/attendance`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.Add()` receives the bound values.
4. The action delegates to `_service.AddManualAttendance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddManualAttendance()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 2. GET api/admin/attendance/{userId}

### Purpose

This endpoint is implemented by `AdminAttendanceController.GetByUser()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `GetByUser()`
- Approximate source line at generation: 54
- Direct delegated call found: `_service.GetByUser()`

### HTTP contract

- Method: `GET`
- Route: `api/admin/attendance/{userId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string userId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("{userId}")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/admin/attendance/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.GetByUser()` receives the bound values.
4. The action delegates to `_service.GetByUser()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetByUser()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:20`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 3. GET api/admin/attendance/{userId}/date

### Purpose

This endpoint is implemented by `AdminAttendanceController.GetByUserAndDate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `GetByUserAndDate()`
- Approximate source line at generation: 62
- Direct delegated call found: `_service.GetByUserAndDate()`

### HTTP contract

- Method: `GET`
- Route: `api/admin/attendance/{userId}/date`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string userId, [FromQuery] DateTime date`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("{userId}/date")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId, [FromQuery] DateTime date`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/admin/attendance/{userId}/date`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.GetByUserAndDate()` receives the bound values.
4. The action delegates to `_service.GetByUserAndDate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetByUserAndDate()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:18`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 4. PUT api/admin/attendance/{userId}

### Purpose

This endpoint is implemented by `AdminAttendanceController.Update()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `Update()`
- Approximate source line at generation: 73
- Direct delegated call found: `_service.UpdateAttendance()`

### HTTP contract

- Method: `PUT`
- Route: `api/admin/attendance/{userId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string userId, [FromQuery] DateTime date, [FromBody] AttendanceUpdateDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPut("{userId}")] [ModulePermission(AppModule.Attendance, Permission.Override)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string userId, [FromQuery] DateTime date, [FromBody] AttendanceUpdateDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/admin/attendance/{userId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.Update()` receives the bound values.
4. The action delegates to `_service.UpdateAttendance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateAttendance()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:22`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 5. POST api/admin/attendance/GetAllAttendanceItems

### Purpose

This endpoint is implemented by `AdminAttendanceController.GetAll()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `GetAll()`
- Approximate source line at generation: 93
- Direct delegated call found: `_service.GetAttendanceByDateRange()`

### HTTP contract

- Method: `POST`
- Route: `api/admin/attendance/GetAllAttendanceItems`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] AttendanceCalendarRequestDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("GetAllAttendanceItems")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AttendanceCalendarRequestDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/admin/attendance/GetAllAttendanceItems`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.GetAll()` receives the bound values.
4. The action delegates to `_service.GetAttendanceByDateRange()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAttendanceByDateRange()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:24`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 6. POST api/admin/attendance/effective-schedules

### Purpose

This endpoint is implemented by `AdminAttendanceController.GetEffectiveSchedules()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `GetEffectiveSchedules()`
- Approximate source line at generation: 112
- Direct delegated call found: `effectiveSchedules.ResolveManyAsync()`

### HTTP contract

- Method: `POST`
- Route: `api/admin/attendance/effective-schedules`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] EffectiveOfficeSchedulesRequest request, [FromServices] IEffectiveOfficeScheduleService effectiveSchedules`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("effective-schedules")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] EffectiveOfficeSchedulesRequest request, [FromServices] IEffectiveOfficeScheduleService effectiveSchedules`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/admin/attendance/effective-schedules`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.GetEffectiveSchedules()` receives the bound values.
4. The action delegates to `effectiveSchedules.ResolveManyAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ResolveManyAsync()` at `Codeji.CMS.Services/Attendance/EffectiveOfficeScheduleService.cs:12`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `ResolveManyAsync()` at `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:39`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND.
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

## 7. GET api/admin/attendance/export/monthly

### Purpose

This endpoint is implemented by `AdminAttendanceController.ExportMonthly()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `ExportMonthly()`
- Approximate source line at generation: 130
- Direct delegated call found: `_service.ExportMonthlyAttendanceAsync()`

### HTTP contract

- Method: `GET`
- Route: `api/admin/attendance/export/monthly`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] int year, [FromQuery] int month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("export/monthly")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] int year, [FromQuery] int month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/admin/attendance/export/monthly`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.ExportMonthly()` receives the bound values.
4. The action delegates to `_service.ExportMonthlyAttendanceAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ExportMonthlyAttendanceAsync()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: CREATED, UPDATED.
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

## 8. POST api/admin/attendance/initialize-month

### Purpose

This endpoint is implemented by `AdminAttendanceController.InitializeMonth()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `InitializeMonth()`
- Approximate source line at generation: 144
- Direct delegated call found: `initialization.InitializeMonthAsync()`

### HTTP contract

- Method: `POST`
- Route: `api/admin/attendance/initialize-month`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] AttendanceInitializationRequestDto request, [FromServices] IAttendanceInitializationService initialization`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("initialize-month")] [ModulePermission(AppModule.Attendance, Permission.Override)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AttendanceInitializationRequestDto request, [FromServices] IAttendanceInitializationService initialization`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/admin/attendance/initialize-month`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.InitializeMonth()` receives the bound values.
4. The action delegates to `initialization.InitializeMonthAsync()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `InitializeMonthAsync()` at `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:11`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED.
- Explicit workflow/status literals found: .
- Method match: `InitializeMonthAsync()` at `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs:181`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SECOND_HALF, FIRST_HALF, LEAVE, LEAVE_ATTENDANCE_SEGMENT_CONFLICT, BLOCKING, PENDING_REVIEW.
- Explicit workflow/status literals found: Completed.
- Method match: `InitializeMonthAsync()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:71`.
- Static signals in the inspected method window: company/tenant context reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `InitializeMonthAsync()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:379`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference, transaction/session reference.
- Explicit uppercase result/error tokens found: WFH_RECOVERY_REQUIRED, WFH_REQUEST_CHANGED, WFH_REQUEST_NOT_AVAILABLE, FIRST_HALF, SECOND_HALF, WFH_REQUEST, WFH_INSUFFICIENT_HOURS, MISSING_CHECKOUT, INVALID_TIME_ORDER, WFH_EXCEPTION_NOT_FOUND, PENDING_REVIEW.
- Explicit workflow/status literals found: Cancelled, Approved.

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

## 9. GET api/admin/attendance/correction-requests

### Purpose

This endpoint is implemented by `AdminAttendanceController.CorrectionRequests()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `CorrectionRequests()`
- Approximate source line at generation: 154
- Direct delegated call found: `corrections.GetAll()`

### HTTP contract

- Method: `GET`
- Route: `api/admin/attendance/correction-requests`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("correction-requests")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/admin/attendance/correction-requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.CorrectionRequests()` receives the bound values.
4. The action delegates to `corrections.GetAll()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
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

## 10. PUT api/admin/attendance/correction-requests/{requestId}

### Purpose

This endpoint is implemented by `AdminAttendanceController.ReviewCorrection()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `ReviewCorrection()`
- Approximate source line at generation: 159
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `PUT`
- Route: `api/admin/attendance/correction-requests/{requestId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string requestId, [FromBody] AttendanceCorrectionReviewDto model, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPut("correction-requests/{requestId}")] [ModulePermission(AppModule.Attendance, Permission.Override)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string requestId, [FromBody] AttendanceCorrectionReviewDto model, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/admin/attendance/correction-requests/{requestId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.ReviewCorrection()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned, Rejected, Cancelled.

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

## 11. GET api/admin/attendance/my-calendar

### Purpose

This endpoint is implemented by `AdminAttendanceController.GetMyCalendar()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceController.cs`
- Controller: `AdminAttendanceController`
- Action: `GetMyCalendar()`
- Approximate source line at generation: 178
- Direct delegated call found: `fromDate.AddMonths()`

### HTTP contract

- Method: `GET`
- Route: `api/admin/attendance/my-calendar`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] int year, [FromQuery] int month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("my-calendar")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] int year, [FromQuery] int month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/admin/attendance/my-calendar`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AdminAttendanceController.GetMyCalendar()` receives the bound values.
4. The action delegates to `fromDate.AddMonths()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddMonths()` at `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `AddMonths()` at `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:17`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.
- Method match: `AddMonths()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:120`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.
- Method match: `AddMonths()` at `Codeji.CMS.Services/Attendance/AttendanceService.cs:190`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: FIRST_HALF, SECOND_HALF, FCE5E5, EEF1F4, F4F4F5, FFFFFF, D9DEE7.
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

## 12. GET api/attendance/penalty/employees/{employeeUserId}/policy

### Purpose

This endpoint is implemented by `AttendancePenaltyController.GetEmployeePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `GetEmployeePolicy()`
- Approximate source line at generation: 16
- Direct delegated call found: `_service.GetEmployeePolicy()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/penalty/employees/{employeeUserId}/policy`
- Return declaration: `Task<Result<AttendancePenaltyPolicyDto>>`
- Parameters exactly as declared: `string employeeUserId,[FromQuery]DateTime? effectiveOn=null`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpGet("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeUserId,[FromQuery]DateTime? effectiveOn=null`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/penalty/employees/{employeeUserId}/policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.GetEmployeePolicy()` receives the bound values.
4. The action delegates to `_service.GetEmployeePolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeePolicy()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:13`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AttendancePenaltyPolicyDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 13. POST api/attendance/penalty/employees/{employeeUserId}/policy

### Purpose

This endpoint is implemented by `AttendancePenaltyController.SaveEmployeePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `SaveEmployeePolicy()`
- Approximate source line at generation: 19
- Direct delegated call found: `_service.SaveEmployeePolicy()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/penalty/employees/{employeeUserId}/policy`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string employeeUserId,AttendancePenaltyPolicyDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeUserId,AttendancePenaltyPolicyDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/penalty/employees/{employeeUserId}/policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.SaveEmployeePolicy()` receives the bound values.
4. The action delegates to `_service.SaveEmployeePolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SaveEmployeePolicy()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:14`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

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

## 14. POST api/attendance/penalty/exceptions/recalculate

### Purpose

This endpoint is implemented by `AttendancePenaltyController.Recalculate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `Recalculate()`
- Approximate source line at generation: 22
- Direct delegated call found: `_service.Recalculate()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/penalty/exceptions/recalculate`
- Return declaration: `Task<Result<AttendancePayrollException>>`
- Parameters exactly as declared: `AttendanceMonthRequestDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/recalculate"),ModulePermission(AppModule.Attendance,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AttendanceMonthRequestDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/penalty/exceptions/recalculate`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.Recalculate()` receives the bound values.
4. The action delegates to `_service.Recalculate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Recalculate()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:15`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AttendancePayrollException>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. POST api/attendance/penalty/exceptions/search

### Purpose

This endpoint is implemented by `AttendancePenaltyController.Search()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `Search()`
- Approximate source line at generation: 24
- Direct delegated call found: `_service.GetExceptions()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/penalty/exceptions/search`
- Return declaration: `Task<Result<AttendancePayrollException>>`
- Parameters exactly as declared: `AttendanceExceptionFilterDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/search"),ModulePermission(AppModule.Attendance,Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AttendanceExceptionFilterDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/penalty/exceptions/search`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.Search()` receives the bound values.
4. The action delegates to `_service.GetExceptions()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetExceptions()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:16`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AttendancePayrollException>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 16. PATCH api/attendance/penalty/exceptions/{id}/review

### Purpose

This endpoint is implemented by `AttendancePenaltyController.Review()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `Review()`
- Approximate source line at generation: 26
- Direct delegated call found: `_service.Review()`

### HTTP contract

- Method: `PATCH`
- Route: `api/attendance/penalty/exceptions/{id}/review`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id,AttendanceExceptionReviewDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPatch("exceptions/{id}/review"),ModulePermission(AppModule.Attendance,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id,AttendanceExceptionReviewDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/attendance/penalty/exceptions/{id}/review`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.Review()` receives the bound values.
4. The action delegates to `_service.Review()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Review()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:17`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

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

## 17. POST api/attendance/penalty/exceptions/{id}/resolve-attendance

### Purpose

This endpoint is implemented by `AttendancePenaltyController.ResolveAttendance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `ResolveAttendance()`
- Approximate source line at generation: 28
- Direct delegated call found: `_service.ResolveAttendanceException()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/penalty/exceptions/{id}/resolve-attendance`
- Return declaration: `Task<Result<AttendanceExceptionResolutionResultDto>>`
- Parameters exactly as declared: `string id, AttendanceExceptionResolutionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/{id}/resolve-attendance"),ModulePermission(AppModule.Attendance,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, AttendanceExceptionResolutionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/penalty/exceptions/{id}/resolve-attendance`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.ResolveAttendance()` receives the bound values.
4. The action delegates to `_service.ResolveAttendanceException()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ResolveAttendanceException()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:18`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AttendanceExceptionResolutionResultDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 18. PATCH api/attendance/penalty/exceptions/{id}/override

### Purpose

This endpoint is implemented by `AttendancePenaltyController.Override()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `Override()`
- Approximate source line at generation: 31
- Direct delegated call found: `_service.OverrideException()`

### HTTP contract

- Method: `PATCH`
- Route: `api/attendance/penalty/exceptions/{id}/override`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id, AttendanceExceptionOverrideDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPatch("exceptions/{id}/override"),ModulePermission(AppModule.Attendance,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, AttendanceExceptionOverrideDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/attendance/penalty/exceptions/{id}/override`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.Override()` receives the bound values.
4. The action delegates to `_service.OverrideException()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `OverrideException()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:19`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

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

## 19. GET api/attendance/penalty/month/lock-status

### Purpose

This endpoint is implemented by `AttendancePenaltyController.GetMonthLockStatus()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `GetMonthLockStatus()`
- Approximate source line at generation: 34
- Direct delegated call found: `_service.GetMonthLockStatus()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/penalty/month/lock-status`
- Return declaration: `Task<AttendanceMonthLockStatusDto>`
- Parameters exactly as declared: `[FromQuery]DateTime month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpGet("month/lock-status"),ModulePermission(AppModule.Attendance,Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery]DateTime month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/penalty/month/lock-status`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.GetMonthLockStatus()` receives the bound values.
4. The action delegates to `_service.GetMonthLockStatus()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetMonthLockStatus()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:20`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<AttendanceMonthLockStatusDto>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 20. POST api/attendance/penalty/month/validate-lock

### Purpose

This endpoint is implemented by `AttendancePenaltyController.ValidateAndLock()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendancePenaltyController.cs`
- Controller: `AttendancePenaltyController`
- Action: `ValidateAndLock()`
- Approximate source line at generation: 36
- Direct delegated call found: `_service.ValidateAndLock()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/penalty/month/validate-lock`
- Return declaration: `Task<Result<AttendanceMonthLockValidationResultDto>>`
- Parameters exactly as declared: `AttendanceMonthRequestDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("month/validate-lock"),ModulePermission(AppModule.Attendance,Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AttendanceMonthRequestDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/penalty/month/validate-lock`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendancePenaltyController.ValidateAndLock()` receives the bound values.
4. The action delegates to `_service.ValidateAndLock()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ValidateAndLock()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:21`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<AttendanceMonthLockValidationResultDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 21. GET api/attendance-status-settings

### Purpose

This endpoint is implemented by `AttendanceStatusSettingsController.Get()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Controller: `AttendanceStatusSettingsController`
- Action: `Get()`
- Approximate source line at generation: 13
- Direct delegated call found: `_service.Get()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance-status-settings`
- Return declaration: `async Task<Result<AttendanceStatusSettingDto>>`
- Parameters exactly as declared: `[FromQuery] bool activeOnly=true`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] bool activeOnly=true`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance-status-settings`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendanceStatusSettingsController.Get()` receives the bound values.
4. The action delegates to `_service.Get()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Get()` at `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:8`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Get()` at `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:40`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND.
- Explicit workflow/status literals found: .
- Method match: `Get()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:11`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<AttendanceStatusSettingDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 22. POST api/attendance-status-settings

### Purpose

This endpoint is implemented by `AttendanceStatusSettingsController.Save()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Controller: `AttendanceStatusSettingsController`
- Action: `Save()`
- Approximate source line at generation: 15
- Direct delegated call found: `_service.Save()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance-status-settings`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `AttendanceStatusSettingDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpPost, Authorize(Policy="AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AttendanceStatusSettingDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance-status-settings`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendanceStatusSettingsController.Save()` receives the bound values.
4. The action delegates to `_service.Save()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Save()` at `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:10`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Save()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:12`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
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

## 23. DELETE api/attendance-status-settings/{statusId}

### Purpose

This endpoint is implemented by `AttendanceStatusSettingsController.Delete()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/AttendanceStatusSettingsController.cs`
- Controller: `AttendanceStatusSettingsController`
- Action: `Delete()`
- Approximate source line at generation: 17
- Direct delegated call found: `_service.Delete()`

### HTTP contract

- Method: `DELETE`
- Route: `api/attendance-status-settings/{statusId}`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string statusId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpDelete("{statusId}"), Authorize(Policy="AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string statusId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/attendance-status-settings/{statusId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `AttendanceStatusSettingsController.Delete()` receives the bound values.
4. The action delegates to `_service.Delete()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Delete()` at `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:11`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Delete()` at `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs:102`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: LEAVE_ATTENDANCE_SEGMENT_CONFLICT, BLOCKING, PENDING_REVIEW, LEAVE, LEAVE_ATTENDANCE_CONFLICT.
- Explicit workflow/status literals found: Completed.
- Method match: `Delete()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:57`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
- Explicit workflow/status literals found: .
- Method match: `Delete()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:216`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_DATE_ALREADY_OWNED, WFH_INVALID_DATE_RANGE, WFH_INVALID_STATUS, WFH_REQUEST_CHANGED, WFH_SELF_APPROVAL_NOT_ALLOWED, WFH_APPROVER_NOT_AUTHORIZED, WFH_LEAVE_OVERLAP, WFH_POLICY_DISABLED, WFH_EMPLOYEE_NOT_ELIGIBLE.
- Explicit workflow/status literals found: Approved, Pending, Rejected, Returned, Cancelled.

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

## 24. GET api/attendance/me/grid

### Purpose

This endpoint is implemented by `MyAttendanceController.Grid()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Controller: `MyAttendanceController`
- Action: `Grid()`
- Approximate source line at generation: 29
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/me/grid`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] int year, [FromQuery] int month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("grid")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] int year, [FromQuery] int month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/me/grid`.
2. Authentication, controller attributes, filters, and model binding run.
3. `MyAttendanceController.Grid()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned, Rejected, Cancelled.

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

## 25. GET api/attendance/me/calendar

### Purpose

This endpoint is implemented by `MyAttendanceController.Calendar()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Controller: `MyAttendanceController`
- Action: `Calendar()`
- Approximate source line at generation: 44
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/me/calendar`
- Return declaration: `Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] int year, [FromQuery] int month`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("calendar")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] int year, [FromQuery] int month`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/me/calendar`.
2. Authentication, controller attributes, filters, and model binding run.
3. `MyAttendanceController.Calendar()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned, Rejected, Cancelled.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<IActionResult>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 26. POST api/attendance/me/correction-requests

### Purpose

This endpoint is implemented by `MyAttendanceController.RequestCorrection()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Controller: `MyAttendanceController`
- Action: `RequestCorrection()`
- Approximate source line at generation: 47
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/me/correction-requests`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] AttendanceCorrectionRequestDto model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/me")] [HttpPost("correction-requests")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AttendanceCorrectionRequestDto model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/me/correction-requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `MyAttendanceController.RequestCorrection()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned, Rejected, Cancelled.

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

## 27. GET api/attendance/me/correction-requests

### Purpose

This endpoint is implemented by `MyAttendanceController.MyCorrectionRequests()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/MyAttendanceController.cs`
- Controller: `MyAttendanceController`
- Action: `MyCorrectionRequests()`
- Approximate source line at generation: 79
- Direct delegated call found: `corrections.GetAll()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/me/correction-requests`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("correction-requests")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/me/correction-requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `MyAttendanceController.MyCorrectionRequests()` receives the bound values.
4. The action delegates to `corrections.GetAll()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
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

## 28. GET api/attendance/weekly-offs

### Purpose

This endpoint is implemented by `WeeklyOffSettingsController.Get()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WeeklyOffSettingsController.cs`
- Controller: `WeeklyOffSettingsController`
- Action: `Get()`
- Approximate source line at generation: 12
- Direct delegated call found: `service.Get()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/weekly-offs`
- Return declaration: `Task<WeeklyOffSettingDto>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/weekly-offs")] [HttpGet, ModulePermission(AppModule.Attendance, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/weekly-offs`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WeeklyOffSettingsController.Get()` receives the bound values.
4. The action delegates to `service.Get()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Get()` at `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:8`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Get()` at `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:40`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND.
- Explicit workflow/status literals found: .
- Method match: `Get()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:11`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<WeeklyOffSettingDto>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 29. POST api/attendance/weekly-offs

### Purpose

This endpoint is implemented by `WeeklyOffSettingsController.Save()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WeeklyOffSettingsController.cs`
- Controller: `WeeklyOffSettingsController`
- Action: `Save()`
- Approximate source line at generation: 15
- Direct delegated call found: `service.Save()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/weekly-offs`
- Return declaration: `Task<Codeji.CMS.Domain.Models.Result>`
- Parameters exactly as declared: `WeeklyOffSettingDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Authorize, Route("api/attendance/weekly-offs")] [HttpPost, ModulePermission(AppModule.Attendance, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `WeeklyOffSettingDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/weekly-offs`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WeeklyOffSettingsController.Save()` receives the bound values.
4. The action delegates to `service.Save()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Save()` at `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:10`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Save()` at `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:12`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Codeji.CMS.Domain.Models.Result>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 30. GET api/wfh/policy

### Purpose

This endpoint is implemented by `WorkFromHomeController.Policy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Policy()`
- Approximate source line at generation: 10
- Direct delegated call found: `service.GetPolicy()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/policy`
- Return declaration: `Task<Result<WorkFromHomePolicyDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("policy")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Policy()` receives the bound values.
4. The action delegates to `service.GetPolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPolicy()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomePolicyDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 31. GET api/wfh/context

### Purpose

This endpoint is implemented by `WorkFromHomeController.Context()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Context()`
- Approximate source line at generation: 11
- Direct delegated call found: `service.GetEmployeeContext()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/context`
- Return declaration: `Task<Result<EmployeeWfhContextDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("context")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/context`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Context()` receives the bound values.
4. The action delegates to `service.GetEmployeeContext()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeContext()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:29`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeWfhContextDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 32. PUT api/wfh/policy

### Purpose

This endpoint is implemented by `WorkFromHomeController.SavePolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `SavePolicy()`
- Approximate source line at generation: 12
- Direct delegated call found: `service.SavePolicy()`

### HTTP contract

- Method: `PUT`
- Route: `api/wfh/policy`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `WorkFromHomePolicyDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPut("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `WorkFromHomePolicyDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/wfh/policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.SavePolicy()` receives the bound values.
4. The action delegates to `service.SavePolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SavePolicy()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 33. POST api/wfh/policy

### Purpose

This endpoint is implemented by `WorkFromHomeController.SavePolicyPost()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `SavePolicyPost()`
- Approximate source line at generation: 14
- Direct delegated call found: `service.SavePolicy()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/policy`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `WorkFromHomePolicyDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `WorkFromHomePolicyDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/policy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.SavePolicyPost()` receives the bound values.
4. The action delegates to `service.SavePolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SavePolicy()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 34. GET api/wfh/allocations

### Purpose

This endpoint is implemented by `WorkFromHomeController.Allocations()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Allocations()`
- Approximate source line at generation: 15
- Direct delegated call found: `service.GetEmployeeAllocations()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/allocations`
- Return declaration: `Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/allocations`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Allocations()` receives the bound values.
4. The action delegates to `service.GetEmployeeAllocations()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeAllocations()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:31`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 35. PUT api/wfh/allocations

### Purpose

This endpoint is implemented by `WorkFromHomeController.SaveAllocation()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `SaveAllocation()`
- Approximate source line at generation: 16
- Direct delegated call found: `service.SaveEmployeeAllocation()`

### HTTP contract

- Method: `PUT`
- Route: `api/wfh/allocations`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `WorkFromHomeEmployeeAllocationDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPut("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `WorkFromHomeEmployeeAllocationDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/wfh/allocations`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.SaveAllocation()` receives the bound values.
4. The action delegates to `service.SaveEmployeeAllocation()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SaveEmployeeAllocation()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:32`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 36. POST api/wfh/requests

### Purpose

This endpoint is implemented by `WorkFromHomeController.Create()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Create()`
- Approximate source line at generation: 17
- Direct delegated call found: `service.Create()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests`
- Return declaration: `Task<Result<WorkFromHomeResponseDto>>`
- Parameters exactly as declared: `WorkFromHomeRequestDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `WorkFromHomeRequestDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Create()` receives the bound values.
4. The action delegates to `service.Create()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Create()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:33`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 37. GET api/wfh/requests/my

### Purpose

This endpoint is implemented by `WorkFromHomeController.Mine()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Mine()`
- Approximate source line at generation: 18
- Direct delegated call found: `service.GetMy()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/requests/my`
- Return declaration: `Task<Result<WorkFromHomeResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/my")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/requests/my`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Mine()` receives the bound values.
4. The action delegates to `service.GetMy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetMy()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:34`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 38. GET api/wfh/requests/team

### Purpose

This endpoint is implemented by `WorkFromHomeController.Team()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Team()`
- Approximate source line at generation: 19
- Direct delegated call found: `service.GetTeam()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/requests/team`
- Return declaration: `Task<Result<WorkFromHomeResponseDto>>`
- Parameters exactly as declared: `[FromQuery] WorkFromHomeRequestFilterDto filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/team"), ModulePermission(AppModule.WorkFromHome, Permission.ViewTeam)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] WorkFromHomeRequestFilterDto filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/requests/team`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Team()` receives the bound values.
4. The action delegates to `service.GetTeam()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetTeam()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:35`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 39. GET api/wfh/requests/all

### Purpose

This endpoint is implemented by `WorkFromHomeController.All()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `All()`
- Approximate source line at generation: 20
- Direct delegated call found: `service.GetAll()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/requests/all`
- Return declaration: `Task<Result<WorkFromHomeResponseDto>>`
- Parameters exactly as declared: `[FromQuery] WorkFromHomeRequestFilterDto filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/all"), ModulePermission(AppModule.WorkFromHome, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] WorkFromHomeRequestFilterDto filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/requests/all`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.All()` receives the bound values.
4. The action delegates to `service.GetAll()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE.
- Explicit workflow/status literals found: .
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED.
- Explicit workflow/status literals found: Pending.
- Method match: `GetAll()` at `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: SYSTEM_OFFICE_START_PRESENT.
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomeResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 40. POST api/wfh/requests/{id}/approve

### Purpose

This endpoint is implemented by `WorkFromHomeController.Approve()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Approve()`
- Approximate source line at generation: 21
- Direct delegated call found: `service.Decide()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/approve`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id, WorkFromHomeDecisionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/approve"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeDecisionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/approve`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Approve()` receives the bound values.
4. The action delegates to `service.Decide()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Decide()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 41. POST api/wfh/requests/{id}/reject

### Purpose

This endpoint is implemented by `WorkFromHomeController.Reject()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Reject()`
- Approximate source line at generation: 22
- Direct delegated call found: `service.Decide()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/reject`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id, WorkFromHomeDecisionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/reject"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeDecisionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/reject`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Reject()` receives the bound values.
4. The action delegates to `service.Decide()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Decide()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 42. POST api/wfh/requests/{id}/return

### Purpose

This endpoint is implemented by `WorkFromHomeController.Return()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Return()`
- Approximate source line at generation: 23
- Direct delegated call found: `service.Decide()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/return`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id, WorkFromHomeDecisionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/return"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeDecisionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/return`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Return()` receives the bound values.
4. The action delegates to `service.Decide()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Decide()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 43. POST api/wfh/requests/{id}/cancel

### Purpose

This endpoint is implemented by `WorkFromHomeController.Cancel()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Cancel()`
- Approximate source line at generation: 24
- Direct delegated call found: `service.Cancel()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/cancel`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id, WorkFromHomeCancelDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/cancel")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeCancelDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/cancel`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Cancel()` receives the bound values.
4. The action delegates to `service.Cancel()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Cancel()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:38`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 44. POST api/wfh/requests/{id}/check-in

### Purpose

This endpoint is implemented by `WorkFromHomeController.CheckIn()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `CheckIn()`
- Approximate source line at generation: 25
- Direct delegated call found: `service.CheckIn()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/check-in`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/check-in")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/check-in`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.CheckIn()` receives the bound values.
4. The action delegates to `service.CheckIn()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CheckIn()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:39`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 45. POST api/wfh/requests/{id}/check-out

### Purpose

This endpoint is implemented by `WorkFromHomeController.CheckOut()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `CheckOut()`
- Approximate source line at generation: 26
- Direct delegated call found: `service.CheckOut()`

### HTTP contract

- Method: `POST`
- Route: `api/wfh/requests/{id}/check-out`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string id`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/check-out")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/wfh/requests/{id}/check-out`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.CheckOut()` receives the bound values.
4. The action delegates to `service.CheckOut()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CheckOut()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:40`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 46. GET api/wfh/requests/{id}/timing

### Purpose

This endpoint is implemented by `WorkFromHomeController.Timing()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `Timing()`
- Approximate source line at generation: 27
- Direct delegated call found: `service.GetTiming()`

### HTTP contract

- Method: `GET`
- Route: `api/wfh/requests/{id}/timing`
- Return declaration: `Task<Result<WorkFromHomeTimingDto>>`
- Parameters exactly as declared: `string id`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/{id}/timing")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/wfh/requests/{id}/timing`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.Timing()` receives the bound values.
4. The action delegates to `service.GetTiming()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetTiming()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<WorkFromHomeTimingDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 47. PATCH api/wfh/attendance-exceptions/{id}/hours

### Purpose

This endpoint is implemented by `WorkFromHomeController.CorrectHours()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `CorrectHours()`
- Approximate source line at generation: 29
- Direct delegated call found: `service.CorrectInsufficientHours()`

### HTTP contract

- Method: `PATCH`
- Route: `api/wfh/attendance-exceptions/{id}/hours`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string id, WorkFromHomeHoursCorrectionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPatch("attendance-exceptions/{id}/hours"), ModulePermission(AppModule.Attendance, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeHoursCorrectionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/wfh/attendance-exceptions/{id}/hours`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.CorrectHours()` receives the bound values.
4. The action delegates to `service.CorrectInsufficientHours()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CorrectInsufficientHours()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:42`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 48. PATCH api/wfh/attendance-exceptions/{id}/resolve-conflict

### Purpose

This endpoint is implemented by `WorkFromHomeController.ResolveConflict()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `ResolveConflict()`
- Approximate source line at generation: 35
- Direct delegated call found: `service.ResolveAttendanceConflict()`

### HTTP contract

- Method: `PATCH`
- Route: `api/wfh/attendance-exceptions/{id}/resolve-conflict`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string id, WorkFromHomeConflictResolutionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPatch("attendance-exceptions/{id}/resolve-conflict"), ModulePermission(AppModule.Attendance, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id, WorkFromHomeConflictResolutionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PATCH api/wfh/attendance-exceptions/{id}/resolve-conflict`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.ResolveConflict()` receives the bound values.
4. The action delegates to `service.ResolveAttendanceConflict()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ResolveAttendanceConflict()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:43`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## 49. GET api/attendance/remark-options

### Purpose

This endpoint is implemented by `WorkFromHomeController.RemarkOptions()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `RemarkOptions()`
- Approximate source line at generation: 41
- Direct delegated call found: `service.GetRemarkOptions()`

### HTTP contract

- Method: `GET`
- Route: `api/attendance/remark-options`
- Return declaration: `Task<Result<IEnumerable<AttendanceRemarkOptionDto>>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/attendance/remark-options`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.RemarkOptions()` receives the bound values.
4. The action delegates to `service.GetRemarkOptions()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetRemarkOptions()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:44`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<IEnumerable<AttendanceRemarkOptionDto>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 50. POST api/attendance/remark-options

### Purpose

This endpoint is implemented by `WorkFromHomeController.SaveRemarkOption()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/WorkFromHomeController.cs`
- Controller: `WorkFromHomeController`
- Action: `SaveRemarkOption()`
- Approximate source line at generation: 42
- Direct delegated call found: `service.SaveRemarkOption()`

### HTTP contract

- Method: `POST`
- Route: `api/attendance/remark-options`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `AttendanceRemarkOptionDto dto`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AttendanceRemarkOptionDto dto`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/attendance/remark-options`.
2. Authentication, controller attributes, filters, and model binding run.
3. `WorkFromHomeController.SaveRemarkOption()` receives the bound values.
4. The action delegates to `service.SaveRemarkOption()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SaveRemarkOption()` at `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:45`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Approved, Returned.

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

## Service Source Inventory

### `Codeji.CMS.Services/Attendance/AttendanceAuditWriter.cs`

- Lines: 23
- Extracted symbols: IAttendanceAuditWriter, WriteMutationAsync, snapshot
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs`

- Lines: 50
- Extracted symbols: IAttendanceEditGuard, EnsureEditableWorkingDayAsync, employee, month, cutoff, weekly, offDays, holidays
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs`

- Lines: 85
- Extracted symbols: IAttendanceInitializationService, ResolvePresentStatusCodeAsync, present, Task, result, presentCode, start, businessToday, end, output, summariesToInvalidate, workingDates, date, prepared
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs`

- Lines: 290
- Extracted symbols: AttendanceTimingMode, ExistingAttendancePolicy, CompanyId, ActorUserId, TargetUserId, AttendanceDate, StatusCode, TimingMode, RemarkCode, PreserveRequestedStatus, ExistingRecordPolicy, ShouldWrite, IAttendanceMutationValidator, Task, result, Fail, date, employee, lastWorkingDate, code, status, if, existing, sourceOwnedSegment, row
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceOutboxProcessor.cs`

- Lines: 40
- Extracted symbols: IAttendanceOutboxProcessor, ProcessNextAsync, now, collection, filter, item, FindOneAndUpdateOptions, delay, state, error
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceOutboxWorker.cs`

- Lines: 19
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs`

- Lines: 26
- Extracted symbols: IAttendancePayrollReadinessService, EvaluateAsync, start, selected, pending, userIds, people, names
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendancePayrollRules.cs`

- Lines: 26
- Extracted symbols: exitDate, eligibleFrom
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs`

- Lines: 613
- Extracted symbols: IAttendancePenaltyService, SaveEmployeePolicy, Review, OverrideException, GetMonthLockStatus, IMongoDbRepository, AttendancePenaltyService, Task, employee, on, policy, dto, result, versions, existing, requestedEnd, tenantScoped
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceReminderEvaluator.cs`

- Lines: 31
- Extracted symbols: IReadOnlyList, attendanceByUser
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceReminderHostedService.cs`

- Lines: 57
- Extracted symbols: indianNow, processor
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs`

- Lines: 297
- Extracted symbols: IAttendanceReminderProcessor, ProcessAsync, ProcessAutomaticPresentAsync, ProcessReviewReminderAsync, reference, today, companyIds, presentCode, created, existing, schedule, prepared, save, autoMarked, activeEmployees, companyId, targetId, reviewReminderAlreadyCreated, reviewers, autoMarkedCount, notification, userItems, Task, reviewerRoleIds
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceService.cs`

- Lines: 330
- Extracted symbols: IAdminAttendanceService, AddManualAttendance, GetByUserAndDate, UpdateAttendance, ExportMonthlyAttendanceAsync, IMongoDbRepository, AdminAttendanceService, normalizedDate, prepared, result, existing, PersistUpdate, Prepare, hasTimes, validation, updated, attendance, Task
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceSourceTransitionPolicy.cs`

- Lines: 10
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceStatusCalculationService.cs`

- Lines: 40
- Extracted symbols: IAttendanceStatusCalculationService, CalculateOfficeStatus, grace, lateArrivalStart, earlyDepartureStart, isLate, isEarly, halfDayCheckInBoundary, halfDayCheckOutBoundary, exceedsLateLimit, exceedsEarlyLimit
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs`

- Lines: 164
- Extracted symbols: IAttendanceStatusService, EnsureCompanyDefaults, Save, Delete, IMongoDbRepository, readonly, AttendanceStatusService, Task, items, existingStatuses, expected, existingCodes, missingDefaults, combinedStatus, halfDay, halfDayFilter, defaults, existing, status
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/CalendarDateHelpers.cs`

- Lines: 57
- Extracted symbols: utc, ist, businessDate
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/CompanyWorkingCalendarService.cs`

- Lines: 73
- Extracted symbols: ICompanyWorkingCalendarService, IsWorkingDayAsync, CountWorkingDaysAsync, dates, Task, weekly, offDays, startDate, endDate, holidays, result, isWeekend, isHoliday
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/EffectiveOfficeScheduleService.cs`

- Lines: 92
- Extracted symbols: IEffectiveOfficeScheduleService, ResolveAsync, day, Effective, employeeAssignment, departmentAssignment, scheduleId, candidates, Task, ids, employeeOverrides, employeeByUser, companyEmployees, departments, departmentByName, availableSchedules, defaultSchedule, schedulesById, source, if, schedule
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationHostedService.cs`

- Lines: 75
- Extracted symbols: ProcessDueRetriesAsync, leaveRepository, reconciliation, now, due, outcome, nextAttempt
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs`

- Lines: 220
- Extracted symbols: ILeaveAttendanceReconciliationService, ReconcileAcceptedLeaveAsync, ReverseLeaveAttendanceAsync, result, request, employee, policy, isUnpaidLeave, mappedStatus, statusCode, status, dates, leaveDates, ownedRows, hasConflict, date, existingSegments, month, existingSegment, conflict, existing, entity, affectedMonths, now, filter
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs`

- Lines: 163
- Extracted symbols: IOfficeScheduleAssignmentService, Task, employee, now, employeeOverride, effective, companyWeeklyOffs, scheduleId, schedule, existing, current, close, insert, active, companySchedules, names, Fail, Employee, Schedule
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/OfficeScheduleSettingsService.cs`

- Lines: 207
- Extracted symbols: IOfficeScheduleSettingsService, DeleteAsync, Task, schedule, entities, defaultSchedule, createResult, if, demoteResult, items, companySchedules, sameNameExists, Fail, scheduledMinutes, existing, entity, writeResult, hasEmployeeAssignments
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/WeeklyOffService.cs`

- Lines: 75
- Extracted symbols: IWeeklyOffService, Get, Save, WeeklyOffService, setting, days, previousDays, automaticRows, invalidatedSummaries, month, isLocked, isPayrolled, removed
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs`

- Lines: 1023
- Extracted symbols: IWorkFromHomeService, SavePolicy, SaveEmployeeAllocation, Decide, Cancel, CheckIn, CheckOut, CorrectInsufficientHours, ResolveAttendanceConflict, SaveRemarkOption, CompanyId, UserId, Task, policy, Ok, employee, schedule, companyNow, today, officeStart, week, eligible, used, weeklyLimit, remaining, reason
- Use: follow the controller delegation into this file when the symbol matches the action.

