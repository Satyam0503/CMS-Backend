# Dashboard and Notices Functional and Current Flow

**Last verified against code:** 2026-09-02

This is a source-trace document. Each flow begins at an extracted controller action and names the direct service call when the controller exposes one. Persistence, side effects and failure behavior are not guessed when the delegated source must still be inspected.

## 1. End-to-End Architecture

```mermaid
flowchart LR
  Actor[User or system actor] --> UI[React route/page/component]
  UI --> Client[Frontend API client]
  Client --> Controller[ASP.NET Core controller action]
  Controller --> Guard[Authentication, permission, binding]
  Guard --> Service[Domain service]
  Service --> Repository[Repository / MongoDB]
  Service --> Effects[Audit, notification, outbox, job when implemented]
```

## 2. Major Action Flows

### FLOW-001 - GetAllGenderDetails

#### Trigger

An HTTP client calls `GET api/Dashboard/GetAllGenderDetails`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllGenderDetails")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `DashboardController.GetAllGenderDetails()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `CurrentContext.CompanyId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

#### Service evidence found

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs:84`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-002 - GetApplicationStatusData

#### Trigger

An HTTP client calls `GET api/Dashboard/GetApplicationStatusData`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetApplicationStatusData")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `DashboardController.GetApplicationStatusData()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string? vacancyId`.
4. The controller executes/delegates to `_dashboardService.GetApplicationStatusData()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

#### Service evidence found

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs:81`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:10`; signals: ; errors: ; statuses: .

### FLOW-003 - GetAllDepartmentDetails

#### Trigger

An HTTP client calls `GET api/Dashboard/GetAllDepartmentDetails`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllDepartmentDetails")] [ModulePermission(AppModule.Dashboard, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `DashboardController.GetAllDepartmentDetails()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `CurrentContext.CompanyId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

#### Service evidence found

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs:84`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-004 - GetUpComingHolidays

#### Trigger

An HTTP client calls `GET api/Dashboard/GetUpComingHolidayAndEvents`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetUpComingHolidayAndEvents")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `DashboardController.GetUpComingHolidays()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_dashboardService.GetUpComingHolidayAndEvents()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

#### Service evidence found

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs:108`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:11`; signals: ; errors: ; statuses: .

### FLOW-005 - GetUpComingCelebrations

#### Trigger

An HTTP client calls `GET api/Dashboard/GetUpComingCelebrations`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetUpComingCelebrations")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `DashboardController.GetUpComingCelebrations()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_dashboardService.GetUpComingCelebrations()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

#### Service evidence found

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs:158`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs:12`; signals: ; errors: ; statuses: .

### FLOW-006 - PostNotice

#### Trigger

An HTTP client calls `POST api/NoticeBoard/PostNotice`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("PostNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.PostNotice()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AddNoticeRequestModel notice`.
4. The controller executes/delegates to `CurrentContext.UserId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

### FLOW-007 - GetAllNotices

#### Trigger

An HTTP client calls `POST api/NoticeBoard/GetAllNotice`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetAllNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.GetAllNotices()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] GetNoticeRequest filter`.
4. The controller executes/delegates to `CurrentContext.UserId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

### FLOW-008 - GetNoticeById

#### Trigger

An HTTP client calls `GET api/NoticeBoard/GetNoticeById/{noticeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetNoticeById/{noticeId}")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.GetNoticeById()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string noticeId`.
4. The controller executes/delegates to `CurrentContext.UserId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

### FLOW-009 - GetMyNotices

#### Trigger

An HTTP client calls `GET api/NoticeBoard/GetMyNotices`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetMyNotices")] [ModulePermission(AppModule.NoticeBoard, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.GetMyNotices()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] int pageNo, [FromQuery] int records`.
4. The controller executes/delegates to `CurrentContext.UserId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

### FLOW-010 - UpdateMyNotice

#### Trigger

An HTTP client calls `PUT api/NoticeBoard/UpdateMyNotice`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("UpdateMyNotice")] [ModulePermission(AppModule.NoticeBoard, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.UpdateMyNotice()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] UpdateNoticeDto notice`.
4. The controller executes/delegates to `CurrentContext.UserId()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

### FLOW-011 - DeleteNotice

#### Trigger

An HTTP client calls `DELETE api/NoticeBoard/DeleteNotice/{noticeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteNotice/{noticeId}")] [ModulePermission(AppModule.NoticeBoard, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `NoticeBoardController.DeleteNotice()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string noticeId`.
4. The controller executes/delegates to `string.IsNullOrEmpty()`.
5. The service validates domain rules and resolves company-owned records.
6. Repository operations read or mutate the applicable Mongo records.
7. Audit, notification, outbox or background effects occur only where the inspected implementation performs them.
8. The result flows through the shared API response/error pipeline.

#### Frontend flow

React route/page -> component handler -> frontend service/client -> this endpoint. The exact caller must be confirmed from the frontend source inventory.

#### Permission flow

Authenticated principal -> controller/module guard -> service company/ownership/relationship rule -> allowed mutation/read. UI visibility is not an authorization boundary.

#### Database changes

> WARNING: Follow the direct service call into repository/entity code. The HTTP verb does not prove insert/update/delete semantics.

#### Final state and failure paths

- Success state: the state returned/persisted by the service.
- Binding/validation failure: determined by DTO/model validation and service checks.
- Unauthorized/forbidden: determined by authentication, permission and relationship guards.
- Not found/company mismatch: must not reveal another company record.
- Conflict/stale state: verify version and transition rules where applicable.
- Partial side-effect failure: inspect transaction/outbox/retry implementation before asserting recovery.

## 3. Service and UI Evidence Inventory

- `Codeji.CMS.Services/Dashboard/DashboardServices.cs` - symbols: IMongoDbRepository, DashboardServices, Task, allDepartments, allEmpUser, result, companyId, applicantList, groupedApplicantData, recurringItems, upcomingEventOrHolidays, date, adjustedDate, upcomingHoliday, upcomingEvent, combinedItems, CalendarItemDto
- `Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs` - symbols: IDashboardService
- `Codeji.CMS.Services/NoticeBoard/Interface/INoticeBoardService.cs` - symbols: INoticeBoardService, PostNotice, GetNoticeById, UpdateMyNotice, DeleteNotice
- `Codeji.CMS.Services/NoticeBoard/NoticeBoardServices.cs` - symbols: IMongoDbRepository, NoticeBoardServices, PostNotice, notice, result, notification, result1, empUsers, filteredUsers, userNotifications, notificationsTasks, userNotification, UpdateMyNotice, updateDefinition, Task, empIdsList, postedBy, totalRecords, noticeList, empIdList, jobTitles, data, GetNoticeById, hasViewed
- `src/app/modules/dashboard/components/ApplicationsChart.tsx` - symbols: ApplicationsChart, theme, getActiveJobs, response, getAllApplicationsData, key, i
- `src/app/modules/dashboard/components/CardSkeleton.tsx` - symbols: CardSkeleton
- `src/app/modules/dashboard/components/DashboardHeader.tsx` - symbols: DashboardHeader, intl
- `src/app/modules/dashboard/components/DepartmentChart.tsx` - symbols: defaultLanguage, hasAssignedEmployees, chartSeries
- `src/app/modules/dashboard/components/EmployeeChart.tsx` - symbols: createChartData, countTotalEmployee, totalEmployee, intl, empData
- `src/app/modules/dashboard/components/EmployeePerformance.tsx` - symbols: createData, rows, intl
- `src/app/modules/dashboard/components/EmpOfMonth.tsx` - symbols: intl
- `src/app/modules/dashboard/components/EmpOnLeave.tsx` - symbols: defaultLanguage, fetchLeaveRequest
- `src/app/modules/dashboard/components/Help.tsx` - symbols: intl
- `src/app/modules/dashboard/components/NoticeBoard.tsx` - symbols: updateNoticeViewedStatus
- `src/app/modules/dashboard/components/OpenPositions.tsx` - symbols: getActiveJobs, response
- `src/app/modules/dashboard/components/Punctual.tsx` - symbols: intl
- `src/app/modules/dashboard/components/PushNotification.tsx` - symbols: PushNotification, isAuthenticated, requestUserPermission
- `src/app/modules/dashboard/components/pushNotificationUtils.ts` - symbols: getNotificationOption, hostName, createNotifications, option, title, notification
- `src/app/modules/dashboard/components/UpcomingCelebration.tsx` - symbols: UpComingCelebration
- `src/app/modules/dashboard/components/UpComingHolidayAndEvent.tsx` - symbols: UpComingHolidayAndEvents
- `src/app/modules/dashboard/Dashboard.tsx` - symbols: user, userModulesPermissions, hasAccessToEmpModule, canViewCompanyDashboard, hasAccessToJob, hasAccessToNoticeBoard
- `src/app/modules/dashboard/dashboardQueries.test.ts` - symbols: createTestClient, QueryClient, response, data, client, options, keys
- `src/app/modules/dashboard/dashboardQueries.ts` - symbols: dashboardQueryOptions, useGenderQuery, useDepartmentsQuery, useUpcomingEventsQuery, useUpcomingCelebrationsQuery
- `src/app/modules/dashboard/dashboardservices.ts` - symbols: getAllGenderInfo, getRequest, getAllDepartmentInfo, getAllApplicationStatus, getUpcomingHolidayAndEvents, getUpcomingCelebration
- `src/app/modules/dashboard/model.ts` - symbols: GenderModel, DepartmentResponseModel, ApplicationStatusModel, UpComingHolidayAndEventModel, CalendarModel, UpcomingCelebration, CelebrationModule, GetAllGenderDetailsResponseData, GetAllDepartmentDetailsResponseData, GetAllApplicationStatusData, GetUpComingHolidayEvent, GetUpComingCelebration
- `src/app/modules/noticeboard/components/AddNotice.tsx` - symbols: PropsTypes, defaultLang, handleAddUpdateNotice
- `src/app/modules/noticeboard/components/MyNotices.tsx` - symbols: MyNotices, userModulesPermissions, navigate, hasAddPermission, handleClose, handleRefresh, handlePageChange, handleRowPerPageChange, handleConfirmationOpen, handleRowClick, handleNoticeDelete
- `src/app/modules/noticeboard/components/NoticeBoardFilter.tsx` - symbols: isTimeOptionOpen, noticeBoardFilter, dispatch, handleTimeOptionMenuClose, handleClick, handleTimePeriodChange, currentFilters, selectedTimePeriod, updatedFromDate, updatedToDate, size, value
- `src/app/modules/noticeboard/components/NoticeViewModel.tsx` - symbols: Props, defaultLanguage, designation, designationLabel
- `src/app/modules/noticeboard/models.ts` - symbols: NoticeModel, NoticeBoardFilterModel, NoticeResponseModel, GetAllNoticePayload, ViewModel, MyNoticeModel, GetAllNoticeResponse, GetMyNoticesResponse
- `src/app/modules/noticeboard/NoticeBoard.tsx` - symbols: NoticeBoard, userModulesPermissions, navigate, noticeBoardFilter, noticeIdFromRedirect, hasAddPermission, handleClose, handleRefresh, handleRowClick, handleFilterOpen, handleSearchChange, handlePageChange, handleRowPerPageChange, handleNoticeViewClose, updateNoticeViewedStatus, noticeType, tone, designation
- `src/app/modules/noticeboard/services.ts` - symbols: postNotice, postRequest, updateNotice, putRequest, getAllNotices, getMyPostedNotices, getRequest, deleteMyNotice, deleteRequest, getNoticeById
- `src/app/routing/NoticeBoardRoutes.tsx` - symbols: NoticeBoardRoutes

## 4. Cross-Module and Recovery Rules

- Preserve tenant/company filters at every service/repository boundary.
- Preserve optimistic version/state checks for stateful workflows.
- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.
- Verify whether Mongo transactions are available before describing rollback as atomic.
- Verify outbox/job retry behavior from the actual processor before promising delivery.

