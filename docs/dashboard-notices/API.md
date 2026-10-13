# Dashboard and Notices API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `GET` | `api/Dashboard/GetAllGenderDetails` | `DashboardController` | `GetAllGenderDetails` | `CurrentContext.CompanyId()` |
| 2 | `GET` | `api/Dashboard/GetApplicationStatusData` | `DashboardController` | `GetApplicationStatusData` | `_dashboardService.GetApplicationStatusData()` |
| 3 | `GET` | `api/Dashboard/GetAllDepartmentDetails` | `DashboardController` | `GetAllDepartmentDetails` | `CurrentContext.CompanyId()` |
| 4 | `GET` | `api/Dashboard/GetUpComingHolidayAndEvents` | `DashboardController` | `GetUpComingHolidays` | `_dashboardService.GetUpComingHolidayAndEvents()` |
| 5 | `GET` | `api/Dashboard/GetUpComingCelebrations` | `DashboardController` | `GetUpComingCelebrations` | `_dashboardService.GetUpComingCelebrations()` |
| 6 | `POST` | `api/NoticeBoard/PostNotice` | `NoticeBoardController` | `PostNotice` | `CurrentContext.UserId()` |
| 7 | `POST` | `api/NoticeBoard/GetAllNotice` | `NoticeBoardController` | `GetAllNotices` | `CurrentContext.UserId()` |
| 8 | `GET` | `api/NoticeBoard/GetNoticeById/{noticeId}` | `NoticeBoardController` | `GetNoticeById` | `CurrentContext.UserId()` |
| 9 | `GET` | `api/NoticeBoard/GetMyNotices` | `NoticeBoardController` | `GetMyNotices` | `CurrentContext.UserId()` |
| 10 | `PUT` | `api/NoticeBoard/UpdateMyNotice` | `NoticeBoardController` | `UpdateMyNotice` | `CurrentContext.UserId()` |
| 11 | `DELETE` | `api/NoticeBoard/DeleteNotice/{noticeId}` | `NoticeBoardController` | `DeleteNotice` | `string.IsNullOrEmpty()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. GET api/Dashboard/GetAllGenderDetails

### Purpose

This endpoint is implemented by `DashboardController.GetAllGenderDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Controller: `DashboardController`
- Action: `GetAllGenderDetails()`
- Approximate source line at generation: 46
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Dashboard/GetAllGenderDetails`
- Return declaration: `async Task<Result<GenderDetailsResponseModel>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllGenderDetails")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Dashboard/GetAllGenderDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `DashboardController.GetAllGenderDetails()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Dashboard/DashboardServices.cs:84`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<GenderDetailsResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 2. GET api/Dashboard/GetApplicationStatusData

### Purpose

This endpoint is implemented by `DashboardController.GetApplicationStatusData()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Controller: `DashboardController`
- Action: `GetApplicationStatusData()`
- Approximate source line at generation: 61
- Direct delegated call found: `_dashboardService.GetApplicationStatusData()`

### HTTP contract

- Method: `GET`
- Route: `api/Dashboard/GetApplicationStatusData`
- Return declaration: `async Task<Result<ApplicationDataResponseDto>>`
- Parameters exactly as declared: `[FromQuery] string? vacancyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetApplicationStatusData")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string? vacancyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Dashboard/GetApplicationStatusData`.
2. Authentication, controller attributes, filters, and model binding run.
3. `DashboardController.GetApplicationStatusData()` receives the bound values.
4. The action delegates to `_dashboardService.GetApplicationStatusData()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetApplicationStatusData()` at `Codeji.CMS.Services/Dashboard/DashboardServices.cs:81`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetApplicationStatusData()` at `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:10`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ApplicationDataResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 3. GET api/Dashboard/GetAllDepartmentDetails

### Purpose

This endpoint is implemented by `DashboardController.GetAllDepartmentDetails()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Controller: `DashboardController`
- Action: `GetAllDepartmentDetails()`
- Approximate source line at generation: 70
- Direct delegated call found: `CurrentContext.CompanyId()`

### HTTP contract

- Method: `GET`
- Route: `api/Dashboard/GetAllDepartmentDetails`
- Return declaration: `async Task<Result<DepartmentEmpResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllDepartmentDetails")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Dashboard/GetAllDepartmentDetails`.
2. Authentication, controller attributes, filters, and model binding run.
3. `DashboardController.GetAllDepartmentDetails()` receives the bound values.
4. The action delegates to `CurrentContext.CompanyId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompanyId()` at `Codeji.CMS.Services/Dashboard/DashboardServices.cs:84`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<DepartmentEmpResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 4. GET api/Dashboard/GetUpComingHolidayAndEvents

### Purpose

This endpoint is implemented by `DashboardController.GetUpComingHolidays()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Controller: `DashboardController`
- Action: `GetUpComingHolidays()`
- Approximate source line at generation: 84
- Direct delegated call found: `_dashboardService.GetUpComingHolidayAndEvents()`

### HTTP contract

- Method: `GET`
- Route: `api/Dashboard/GetUpComingHolidayAndEvents`
- Return declaration: `async Task<Result<UpComingHolidayEventResponseDto>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetUpComingHolidayAndEvents")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Dashboard/GetUpComingHolidayAndEvents`.
2. Authentication, controller attributes, filters, and model binding run.
3. `DashboardController.GetUpComingHolidays()` receives the bound values.
4. The action delegates to `_dashboardService.GetUpComingHolidayAndEvents()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetUpComingHolidayAndEvents()` at `Codeji.CMS.Services/Dashboard/DashboardServices.cs:108`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetUpComingHolidayAndEvents()` at `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:11`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UpComingHolidayEventResponseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 5. GET api/Dashboard/GetUpComingCelebrations

### Purpose

This endpoint is implemented by `DashboardController.GetUpComingCelebrations()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/DashboardController.cs`
- Controller: `DashboardController`
- Action: `GetUpComingCelebrations()`
- Approximate source line at generation: 92
- Direct delegated call found: `_dashboardService.GetUpComingCelebrations()`

### HTTP contract

- Method: `GET`
- Route: `api/Dashboard/GetUpComingCelebrations`
- Return declaration: `async Task<Result<UpcomingCelebrations>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetUpComingCelebrations")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Dashboard/GetUpComingCelebrations`.
2. Authentication, controller attributes, filters, and model binding run.
3. `DashboardController.GetUpComingCelebrations()` receives the bound values.
4. The action delegates to `_dashboardService.GetUpComingCelebrations()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetUpComingCelebrations()` at `Codeji.CMS.Services/Dashboard/DashboardServices.cs:158`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetUpComingCelebrations()` at `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:12`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<UpcomingCelebrations>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 6. POST api/NoticeBoard/PostNotice

### Purpose

This endpoint is implemented by `NoticeBoardController.PostNotice()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `PostNotice()`
- Approximate source line at generation: 28
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/NoticeBoard/PostNotice`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `AddNoticeRequestModel notice`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("PostNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `AddNoticeRequestModel notice`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/NoticeBoard/PostNotice`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.PostNotice()` receives the bound values.
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

## 7. POST api/NoticeBoard/GetAllNotice

### Purpose

This endpoint is implemented by `NoticeBoardController.GetAllNotices()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `GetAllNotices()`
- Approximate source line at generation: 45
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/NoticeBoard/GetAllNotice`
- Return declaration: `async Task<Result<NoticeViewModel>>`
- Parameters exactly as declared: `[FromBody] GetNoticeRequest filter`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetAllNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] GetNoticeRequest filter`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/NoticeBoard/GetAllNotice`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.GetAllNotices()` receives the bound values.
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

The compile-time return declaration is `async Task<Result<NoticeViewModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 8. GET api/NoticeBoard/GetNoticeById/{noticeId}

### Purpose

This endpoint is implemented by `NoticeBoardController.GetNoticeById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `GetNoticeById()`
- Approximate source line at generation: 54
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `GET`
- Route: `api/NoticeBoard/GetNoticeById/{noticeId}`
- Return declaration: `async Task<Result<NoticeViewModel>>`
- Parameters exactly as declared: `string noticeId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetNoticeById/{noticeId}")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string noticeId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/NoticeBoard/GetNoticeById/{noticeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.GetNoticeById()` receives the bound values.
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

The compile-time return declaration is `async Task<Result<NoticeViewModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 9. GET api/NoticeBoard/GetMyNotices

### Purpose

This endpoint is implemented by `NoticeBoardController.GetMyNotices()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `GetMyNotices()`
- Approximate source line at generation: 74
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `GET`
- Route: `api/NoticeBoard/GetMyNotices`
- Return declaration: `async Task<Result<MyNoticeDTO>>`
- Parameters exactly as declared: `[FromQuery] int pageNo, [FromQuery] int records`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetMyNotices")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] int pageNo, [FromQuery] int records`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/NoticeBoard/GetMyNotices`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.GetMyNotices()` receives the bound values.
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

The compile-time return declaration is `async Task<Result<MyNoticeDTO>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 10. PUT api/NoticeBoard/UpdateMyNotice

### Purpose

This endpoint is implemented by `NoticeBoardController.UpdateMyNotice()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `UpdateMyNotice()`
- Approximate source line at generation: 83
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `PUT`
- Route: `api/NoticeBoard/UpdateMyNotice`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateNoticeDto notice`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("UpdateMyNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateNoticeDto notice`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/NoticeBoard/UpdateMyNotice`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.UpdateMyNotice()` receives the bound values.
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

## 11. DELETE api/NoticeBoard/DeleteNotice/{noticeId}

### Purpose

This endpoint is implemented by `NoticeBoardController.DeleteNotice()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/NoticeBoardController.cs`
- Controller: `NoticeBoardController`
- Action: `DeleteNotice()`
- Approximate source line at generation: 96
- Direct delegated call found: `string.IsNullOrEmpty()`

### HTTP contract

- Method: `DELETE`
- Route: `api/NoticeBoard/DeleteNotice/{noticeId}`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `string noticeId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteNotice/{noticeId}")] [ModulePermission(AppModule.NoticeBoard, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string noticeId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/NoticeBoard/DeleteNotice/{noticeId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `NoticeBoardController.DeleteNotice()` receives the bound values.
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

## Service Source Inventory

### `Codeji.CMS.Services/Dashboard/DashboardServices.cs`

- Lines: 243
- Extracted symbols: IMongoDbRepository, DashboardServices, Task, allDepartments, allEmpUser, result, companyId, applicantList, groupedApplicantData, recurringItems, upcomingEventOrHolidays, date, adjustedDate, upcomingHoliday, upcomingEvent, combinedItems, CalendarItemDto
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs`

- Lines: 14
- Extracted symbols: IDashboardService
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/NoticeBoard/Interface/INoticeBoardService.cs`

- Lines: 16
- Extracted symbols: INoticeBoardService, PostNotice, GetNoticeById, UpdateMyNotice, DeleteNotice
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/NoticeBoard/NoticeBoardServices.cs`

- Lines: 304
- Extracted symbols: IMongoDbRepository, NoticeBoardServices, PostNotice, notice, result, notification, result1, empUsers, filteredUsers, userNotifications, notificationsTasks, userNotification, UpdateMyNotice, updateDefinition, Task, empIdsList, postedBy, totalRecords, noticeList, empIdList, jobTitles, data, GetNoticeById, hasViewed
- Use: follow the controller delegation into this file when the symbol matches the action.

