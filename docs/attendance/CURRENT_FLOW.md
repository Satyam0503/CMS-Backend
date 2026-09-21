# Attendance and Work From Home Functional and Current Flow

**Last verified against code:** 2026-09-02

This is a source-trace document. Each flow begins at an extracted controller action and names the direct service call when the controller exposes one. Persistence, side effects and failure behavior are not guessed when the delegated source must still be inspected.

## Attendance Report roster selection

1. An authorized attendance viewer opens the Attendance Report.
2. `EmployeeAttendance.tsx` fetches `api/User/GetAllEmployees` with `employmentStatus: "AttendanceRoster"` and the existing tenant context.
3. `EmployeeService.GetAllEmployees` keeps active, blank-status, and notice-period employees, and excludes `Resigned` and `Terminated` employees.
4. The UI renders and paginates only this returned operational roster. Completed exits therefore take no attendance-card space; historical employee/offboarding reporting remains separate.

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

### FLOW-001 - Add

#### Trigger

An HTTP client calls `POST api/admin/attendance`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost] [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.Add()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] AdminAttendanceCreateDto dto`.
4. The controller executes/delegates to `_service.AddManualAttendance()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:16`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-002 - GetByUser

#### Trigger

An HTTP client calls `GET api/admin/attendance/{userId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("{userId}")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.GetByUser()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string userId`.
4. The controller executes/delegates to `_service.GetByUser()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:20`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-003 - GetByUserAndDate

#### Trigger

An HTTP client calls `GET api/admin/attendance/{userId}/date`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("{userId}/date")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.GetByUserAndDate()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string userId, [FromQuery] DateTime date`.
4. The controller executes/delegates to `_service.GetByUserAndDate()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:18`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-004 - Update

#### Trigger

An HTTP client calls `PUT api/admin/attendance/{userId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPut("{userId}")] [ModulePermission(AppModule.Attendance, Permission.Override)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.Update()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string userId, [FromQuery] DateTime date, [FromBody] AttendanceUpdateDto dto`.
4. The controller executes/delegates to `_service.UpdateAttendance()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:22`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-005 - GetAll

#### Trigger

An HTTP client calls `POST api/admin/attendance/GetAllAttendanceItems`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("GetAllAttendanceItems")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.GetAll()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] AttendanceCalendarRequestDto dto`.
4. The controller executes/delegates to `_service.GetAttendanceByDateRange()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:24`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-006 - GetEffectiveSchedules

#### Trigger

An HTTP client calls `POST api/admin/attendance/effective-schedules`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("effective-schedules")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.GetEffectiveSchedules()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] EffectiveOfficeSchedulesRequest request, [FromServices] IEffectiveOfficeScheduleService effectiveSchedules`.
4. The controller executes/delegates to `effectiveSchedules.ResolveManyAsync()`.
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

- `Codeji.CMS.Services/Attendance/EffectiveOfficeScheduleService.cs:12`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:39`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND; statuses: .

### FLOW-007 - ExportMonthly

#### Trigger

An HTTP client calls `GET api/admin/attendance/export/monthly`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("export/monthly")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.ExportMonthly()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] int year, [FromQuery] int month`.
4. The controller executes/delegates to `_service.ExportMonthlyAttendanceAsync()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceService.cs:30`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: CREATED, UPDATED; statuses: .

### FLOW-008 - InitializeMonth

#### Trigger

An HTTP client calls `POST api/admin/attendance/initialize-month`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPost("initialize-month")] [ModulePermission(AppModule.Attendance, Permission.Override)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.InitializeMonth()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] AttendanceInitializationRequestDto request, [FromServices] IAttendanceInitializationService initialization`.
4. The controller executes/delegates to `initialization.InitializeMonthAsync()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:11`; signals: company/tenant context reference, repository/data-access reference; errors: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED; statuses: .
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs:181`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: SECOND_HALF, FIRST_HALF, LEAVE, LEAVE_ATTENDANCE_SEGMENT_CONFLICT, BLOCKING, PENDING_REVIEW; statuses: Completed.
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:71`; signals: company/tenant context reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:379`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference, transaction/session reference; errors: WFH_RECOVERY_REQUIRED, WFH_REQUEST_CHANGED, WFH_REQUEST_NOT_AVAILABLE, FIRST_HALF, SECOND_HALF, WFH_REQUEST, WFH_INSUFFICIENT_HOURS, MISSING_CHECKOUT, INVALID_TIME_ORDER, WFH_EXCEPTION_NOT_FOUND, PENDING_REVIEW; statuses: Cancelled, Approved.

### FLOW-009 - CorrectionRequests

#### Trigger

An HTTP client calls `GET api/admin/attendance/correction-requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("correction-requests")] [ModulePermission(AppModule.Attendance, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.CorrectionRequests()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections`.
4. The controller executes/delegates to `corrections.GetAll()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`; signals: company/tenant context reference, repository/data-access reference; errors: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE; statuses: .
- `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-010 - ReviewCorrection

#### Trigger

An HTTP client calls `PUT api/admin/attendance/correction-requests/{requestId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpPut("correction-requests/{requestId}")] [ModulePermission(AppModule.Attendance, Permission.Override)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.ReviewCorrection()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string requestId, [FromBody] AttendanceCorrectionReviewDto model, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications, [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned, Rejected, Cancelled.

### FLOW-011 - GetMyCalendar

#### Trigger

An HTTP client calls `GET api/admin/attendance/my-calendar`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/admin/attendance")] [Authorize] [HttpGet("my-calendar")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AdminAttendanceController.GetMyCalendar()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] int year, [FromQuery] int month`.
4. The controller executes/delegates to `fromDate.AddMonths()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:41`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:17`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:120`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendanceService.cs:190`; signals: company/tenant context reference, repository/data-access reference; errors: FIRST_HALF, SECOND_HALF, FCE5E5, EEF1F4, F4F4F5, FFFFFF, D9DEE7; statuses: .

### FLOW-012 - GetEmployeePolicy

#### Trigger

An HTTP client calls `GET api/attendance/penalty/employees/{employeeUserId}/policy`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpGet("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.GetEmployeePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string employeeUserId,[FromQuery]DateTime? effectiveOn=null`.
4. The controller executes/delegates to `_service.GetEmployeePolicy()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:13`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-013 - SaveEmployeePolicy

#### Trigger

An HTTP client calls `POST api/attendance/penalty/employees/{employeeUserId}/policy`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.SaveEmployeePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string employeeUserId,AttendancePenaltyPolicyDto dto`.
4. The controller executes/delegates to `_service.SaveEmployeePolicy()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:14`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-014 - Recalculate

#### Trigger

An HTTP client calls `POST api/attendance/penalty/exceptions/recalculate`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/recalculate"),ModulePermission(AppModule.Attendance,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.Recalculate()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AttendanceMonthRequestDto dto`.
4. The controller executes/delegates to `_service.Recalculate()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:15`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-015 - Search

#### Trigger

An HTTP client calls `POST api/attendance/penalty/exceptions/search`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/search"),ModulePermission(AppModule.Attendance,Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.Search()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AttendanceExceptionFilterDto dto`.
4. The controller executes/delegates to `_service.GetExceptions()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:16`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-016 - Review

#### Trigger

An HTTP client calls `PATCH api/attendance/penalty/exceptions/{id}/review`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPatch("exceptions/{id}/review"),ModulePermission(AppModule.Attendance,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.Review()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id,AttendanceExceptionReviewDto dto`.
4. The controller executes/delegates to `_service.Review()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:17`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-017 - ResolveAttendance

#### Trigger

An HTTP client calls `POST api/attendance/penalty/exceptions/{id}/resolve-attendance`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("exceptions/{id}/resolve-attendance"),ModulePermission(AppModule.Attendance,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.ResolveAttendance()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, AttendanceExceptionResolutionDto dto`.
4. The controller executes/delegates to `_service.ResolveAttendanceException()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:18`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-018 - Override

#### Trigger

An HTTP client calls `PATCH api/attendance/penalty/exceptions/{id}/override`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPatch("exceptions/{id}/override"),ModulePermission(AppModule.Attendance,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.Override()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, AttendanceExceptionOverrideDto dto`.
4. The controller executes/delegates to `_service.OverrideException()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:19`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-019 - GetMonthLockStatus

#### Trigger

An HTTP client calls `GET api/attendance/penalty/month/lock-status`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpGet("month/lock-status"),ModulePermission(AppModule.Attendance,Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.GetMonthLockStatus()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery]DateTime month`.
4. The controller executes/delegates to `_service.GetMonthLockStatus()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:20`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-020 - ValidateAndLock

#### Trigger

An HTTP client calls `POST api/attendance/penalty/month/validate-lock`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance/penalty"), Authorize] [HttpPost("month/validate-lock"),ModulePermission(AppModule.Attendance,Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendancePenaltyController.ValidateAndLock()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AttendanceMonthRequestDto dto`.
4. The controller executes/delegates to `_service.ValidateAndLock()`.
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

- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:21`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.

### FLOW-021 - Get

#### Trigger

An HTTP client calls `GET api/attendance-status-settings`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpGet]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendanceStatusSettingsController.Get()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] bool activeOnly=true`.
4. The controller executes/delegates to `_service.Get()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:8`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:40`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND; statuses: .
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:11`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-022 - Save

#### Trigger

An HTTP client calls `POST api/attendance-status-settings`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpPost, Authorize(Policy="AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendanceStatusSettingsController.Save()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AttendanceStatusSettingDto dto`.
4. The controller executes/delegates to `_service.Save()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:10`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:12`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-023 - Delete

#### Trigger

An HTTP client calls `DELETE api/attendance-status-settings/{statusId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/attendance-status-settings"), Authorize] [HttpDelete("{statusId}"), Authorize(Policy="AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AttendanceStatusSettingsController.Delete()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string statusId`.
4. The controller executes/delegates to `_service.Delete()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:11`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs:102`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_ATTENDANCE_SEGMENT_CONFLICT, BLOCKING, PENDING_REVIEW, LEAVE, LEAVE_ATTENDANCE_CONFLICT; statuses: Completed.
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:57`; signals: company/tenant context reference, repository/data-access reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .
- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:216`; signals: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_DATE_ALREADY_OWNED, WFH_INVALID_DATE_RANGE, WFH_INVALID_STATUS, WFH_REQUEST_CHANGED, WFH_SELF_APPROVAL_NOT_ALLOWED, WFH_APPROVER_NOT_AUTHORIZED, WFH_LEAVE_OVERLAP, WFH_POLICY_DISABLED, WFH_EMPLOYEE_NOT_ELIGIBLE; statuses: Approved, Pending, Rejected, Returned, Cancelled.

### FLOW-024 - Grid

#### Trigger

An HTTP client calls `GET api/attendance/me/grid`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("grid")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `MyAttendanceController.Grid()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] int year, [FromQuery] int month`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned, Rejected, Cancelled.

### FLOW-025 - Calendar

#### Trigger

An HTTP client calls `GET api/attendance/me/calendar`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("calendar")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `MyAttendanceController.Calendar()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] int year, [FromQuery] int month`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned, Rejected, Cancelled.

### FLOW-026 - RequestCorrection

#### Trigger

An HTTP client calls `POST api/attendance/me/correction-requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/me")] [HttpPost("correction-requests")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `MyAttendanceController.RequestCorrection()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] AttendanceCorrectionRequestDto model`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:82`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned, Rejected, Cancelled.

### FLOW-027 - MyCorrectionRequests

#### Trigger

An HTTP client calls `GET api/attendance/me/correction-requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/me")] [HttpGet("correction-requests")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `MyAttendanceController.MyCorrectionRequests()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `corrections.GetAll()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`; signals: company/tenant context reference, repository/data-access reference; errors: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE; statuses: .
- `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-028 - Get

#### Trigger

An HTTP client calls `GET api/attendance/weekly-offs`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/weekly-offs")] [HttpGet, ModulePermission(AppModule.Attendance, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WeeklyOffSettingsController.Get()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.Get()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:8`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs:40`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: OFFICE_SCHEDULE_ACTOR_REQUIRED, EMPLOYEE_NOT_FOUND, OFFICE_SCHEDULE_ASSIGNMENT_UPDATE_FAILED, OFFICE_SCHEDULE_ASSIGNMENT_SAVE_FAILED, DEPARTMENT_NOT_FOUND, OFFICE_SCHEDULE_NOT_FOUND; statuses: .
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:11`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-029 - Save

#### Trigger

An HTTP client calls `POST api/attendance/weekly-offs`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Authorize, Route("api/attendance/weekly-offs")] [HttpPost, ModulePermission(AppModule.Attendance, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WeeklyOffSettingsController.Save()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `WeeklyOffSettingDto dto`.
4. The controller executes/delegates to `service.Save()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs:10`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs:12`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-030 - Policy

#### Trigger

An HTTP client calls `GET api/wfh/policy`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("policy")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Policy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetPolicy()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:28`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-031 - Context

#### Trigger

An HTTP client calls `GET api/wfh/context`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("context")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Context()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetEmployeeContext()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:29`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-032 - SavePolicy

#### Trigger

An HTTP client calls `PUT api/wfh/policy`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPut("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.SavePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `WorkFromHomePolicyDto dto`.
4. The controller executes/delegates to `service.SavePolicy()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:30`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-033 - SavePolicyPost

#### Trigger

An HTTP client calls `POST api/wfh/policy`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.SavePolicyPost()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `WorkFromHomePolicyDto dto`.
4. The controller executes/delegates to `service.SavePolicy()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:30`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-034 - Allocations

#### Trigger

An HTTP client calls `GET api/wfh/allocations`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Allocations()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetEmployeeAllocations()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:31`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-035 - SaveAllocation

#### Trigger

An HTTP client calls `PUT api/wfh/allocations`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPut("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.SaveAllocation()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `WorkFromHomeEmployeeAllocationDto dto`.
4. The controller executes/delegates to `service.SaveEmployeeAllocation()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:32`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-036 - Create

#### Trigger

An HTTP client calls `POST api/wfh/requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Create()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `WorkFromHomeRequestDto dto`.
4. The controller executes/delegates to `service.Create()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:33`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-037 - Mine

#### Trigger

An HTTP client calls `GET api/wfh/requests/my`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/my")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Mine()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetMy()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:34`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-038 - Team

#### Trigger

An HTTP client calls `GET api/wfh/requests/team`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/team"), ModulePermission(AppModule.WorkFromHome, Permission.ViewTeam)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Team()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] WorkFromHomeRequestFilterDto filter`.
4. The controller executes/delegates to `service.GetTeam()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:35`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-039 - All

#### Trigger

An HTTP client calls `GET api/wfh/requests/all`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/all"), ModulePermission(AppModule.WorkFromHome, Permission.ViewAll)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.All()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] WorkFromHomeRequestFilterDto filter`.
4. The controller executes/delegates to `service.GetAll()`.
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

- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs:23`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs:26`; signals: company/tenant context reference, repository/data-access reference; errors: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED; statuses: .
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs:81`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ATTENDANCE_TARGET_UNAVAILABLE, ATTENDANCE_AFTER_EXIT, ATTENDANCE_STATUS_INVALID, ADMIN_MANUAL, ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST, ATTENDANCE_TIMING_REQUIRED, LEAVE, WFH_REQUEST, ATTENDANCE_SOURCE_OWNED_LEAVE; statuses: .
- `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs:19`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs:72`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: REVIEW_REQUIRED, LHD_ED_LIMIT_EXCEEDED, PENDING_REVIEW, CANCELLED, LHD_ED_MONTHLY_LIMIT_EXCEEDED, RESOLVED; statuses: Pending.
- `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs:43`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: SYSTEM_OFFICE_START_PRESENT; statuses: .

### FLOW-040 - Approve

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/approve`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/approve"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Approve()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeDecisionDto dto`.
4. The controller executes/delegates to `service.Decide()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-041 - Reject

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/reject`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/reject"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Reject()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeDecisionDto dto`.
4. The controller executes/delegates to `service.Decide()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-042 - Return

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/return`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/return"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Return()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeDecisionDto dto`.
4. The controller executes/delegates to `service.Decide()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:37`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-043 - Cancel

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/cancel`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/cancel")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Cancel()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeCancelDto dto`.
4. The controller executes/delegates to `service.Cancel()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:38`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-044 - CheckIn

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/check-in`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/check-in")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.CheckIn()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id`.
4. The controller executes/delegates to `service.CheckIn()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:39`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-045 - CheckOut

#### Trigger

An HTTP client calls `POST api/wfh/requests/{id}/check-out`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("requests/{id}/check-out")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.CheckOut()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id`.
4. The controller executes/delegates to `service.CheckOut()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:40`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-046 - Timing

#### Trigger

An HTTP client calls `GET api/wfh/requests/{id}/timing`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("requests/{id}/timing")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.Timing()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id`.
4. The controller executes/delegates to `service.GetTiming()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:41`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-047 - CorrectHours

#### Trigger

An HTTP client calls `PATCH api/wfh/attendance-exceptions/{id}/hours`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPatch("attendance-exceptions/{id}/hours"), ModulePermission(AppModule.Attendance, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.CorrectHours()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeHoursCorrectionDto dto`.
4. The controller executes/delegates to `service.CorrectInsufficientHours()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:42`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-048 - ResolveConflict

#### Trigger

An HTTP client calls `PATCH api/wfh/attendance-exceptions/{id}/resolve-conflict`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPatch("attendance-exceptions/{id}/resolve-conflict"), ModulePermission(AppModule.Attendance, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.ResolveConflict()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id, WorkFromHomeConflictResolutionDto dto`.
4. The controller executes/delegates to `service.ResolveAttendanceConflict()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:43`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-049 - RemarkOptions

#### Trigger

An HTTP client calls `GET api/attendance/remark-options`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpGet("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.RemarkOptions()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetRemarkOptions()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:44`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

### FLOW-050 - SaveRemarkOption

#### Trigger

An HTTP client calls `POST api/attendance/remark-options`.

#### Actor and preconditions

Authorization evidence: `[ApiController, Route("api/wfh"), Authorize] [HttpPost("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `WorkFromHomeController.SaveRemarkOption()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `AttendanceRemarkOptionDto dto`.
4. The controller executes/delegates to `service.SaveRemarkOption()`.
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

- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs:45`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference; errors: WFH_EMPLOYEE_NOT_ELIGIBLE, WFH_POLICY_DISABLED, WFH_POLICY_NOT_ASSIGNED, WFH_WEEKLY_LIMIT_USED, WFH_INVALID_ATTENDANCE_STATUS, WFH_INVALID_POLICY, WFH_INVALID_EMPLOYEE_ALLOCATION, WFH_EMPLOYEE_NOT_FOUND; statuses: Pending, Approved, Returned.

## 3. Service and UI Evidence Inventory

- `Codeji.CMS.Services/Attendance/AttendanceAuditWriter.cs` - symbols: IAttendanceAuditWriter, WriteMutationAsync, snapshot
- `Codeji.CMS.Services/Attendance/AttendanceEditGuard.cs` - symbols: IAttendanceEditGuard, EnsureEditableWorkingDayAsync, employee, month, cutoff, weekly, offDays, holidays
- `Codeji.CMS.Services/Attendance/AttendanceInitializationService.cs` - symbols: IAttendanceInitializationService, ResolvePresentStatusCodeAsync, present, Task, result, presentCode, start, businessToday, end, output, summariesToInvalidate, workingDates, date, prepared
- `Codeji.CMS.Services/Attendance/AttendanceMutationValidator.cs` - symbols: AttendanceTimingMode, ExistingAttendancePolicy, CompanyId, ActorUserId, TargetUserId, AttendanceDate, StatusCode, TimingMode, RemarkCode, PreserveRequestedStatus, ExistingRecordPolicy, ShouldWrite, IAttendanceMutationValidator, Task, result, Fail, date, employee, lastWorkingDate, code, status, if, existing, sourceOwnedSegment, row
- `Codeji.CMS.Services/Attendance/AttendanceOutboxProcessor.cs` - symbols: IAttendanceOutboxProcessor, ProcessNextAsync, now, collection, filter, item, FindOneAndUpdateOptions, delay, state, error
- `Codeji.CMS.Services/Attendance/AttendanceOutboxWorker.cs` - symbols: 
- `Codeji.CMS.Services/Attendance/AttendancePayrollReadinessService.cs` - symbols: IAttendancePayrollReadinessService, EvaluateAsync, start, selected, pending, userIds, people, names
- `Codeji.CMS.Services/Attendance/AttendancePayrollRules.cs` - symbols: exitDate, eligibleFrom
- `Codeji.CMS.Services/Attendance/AttendancePenaltyService.cs` - symbols: IAttendancePenaltyService, SaveEmployeePolicy, Review, OverrideException, GetMonthLockStatus, IMongoDbRepository, AttendancePenaltyService, Task, employee, on, policy, dto, result, versions, existing, requestedEnd, tenantScoped
- `Codeji.CMS.Services/Attendance/AttendanceReminderEvaluator.cs` - symbols: IReadOnlyList, attendanceByUser
- `Codeji.CMS.Services/Attendance/AttendanceReminderHostedService.cs` - symbols: indianNow, processor
- `Codeji.CMS.Services/Attendance/AttendanceReminderProcessor.cs` - symbols: IAttendanceReminderProcessor, ProcessAsync, ProcessAutomaticPresentAsync, ProcessReviewReminderAsync, reference, today, companyIds, presentCode, created, existing, schedule, prepared, save, autoMarked, activeEmployees, companyId, targetId, reviewReminderAlreadyCreated, reviewers, autoMarkedCount, notification, userItems, Task, reviewerRoleIds
- `Codeji.CMS.Services/Attendance/AttendanceService.cs` - symbols: IAdminAttendanceService, AddManualAttendance, GetByUserAndDate, UpdateAttendance, ExportMonthlyAttendanceAsync, IMongoDbRepository, AdminAttendanceService, normalizedDate, prepared, result, existing, PersistUpdate, Prepare, hasTimes, validation, updated, attendance, Task
- `Codeji.CMS.Services/Attendance/AttendanceSourceTransitionPolicy.cs` - symbols: 
- `Codeji.CMS.Services/Attendance/AttendanceStatusCalculationService.cs` - symbols: IAttendanceStatusCalculationService, CalculateOfficeStatus, grace, lateArrivalStart, earlyDepartureStart, isLate, isEarly, halfDayCheckInBoundary, halfDayCheckOutBoundary, exceedsLateLimit, exceedsEarlyLimit
- `Codeji.CMS.Services/Attendance/AttendanceStatusService.cs` - symbols: IAttendanceStatusService, EnsureCompanyDefaults, Save, Delete, IMongoDbRepository, readonly, AttendanceStatusService, Task, items, existingStatuses, expected, existingCodes, missingDefaults, combinedStatus, halfDay, halfDayFilter, defaults, existing, status
- `Codeji.CMS.Services/Attendance/CalendarDateHelpers.cs` - symbols: utc, ist, businessDate
- `Codeji.CMS.Services/Attendance/CompanyWorkingCalendarService.cs` - symbols: ICompanyWorkingCalendarService, IsWorkingDayAsync, CountWorkingDaysAsync, dates, Task, weekly, offDays, startDate, endDate, holidays, result, isWeekend, isHoliday
- `Codeji.CMS.Services/Attendance/EffectiveOfficeScheduleService.cs` - symbols: IEffectiveOfficeScheduleService, ResolveAsync, day, Effective, employeeAssignment, departmentAssignment, scheduleId, candidates, Task, ids, employeeOverrides, employeeByUser, companyEmployees, departments, departmentByName, availableSchedules, defaultSchedule, schedulesById, source, if, schedule
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationHostedService.cs` - symbols: ProcessDueRetriesAsync, leaveRepository, reconciliation, now, due, outcome, nextAttempt
- `Codeji.CMS.Services/Attendance/LeaveAttendanceReconciliationService.cs` - symbols: ILeaveAttendanceReconciliationService, ReconcileAcceptedLeaveAsync, ReverseLeaveAttendanceAsync, result, request, employee, policy, isUnpaidLeave, mappedStatus, statusCode, status, dates, leaveDates, ownedRows, hasConflict, date, existingSegments, month, existingSegment, conflict, existing, entity, affectedMonths, now, filter
- `Codeji.CMS.Services/Attendance/OfficeScheduleAssignmentService.cs` - symbols: IOfficeScheduleAssignmentService, Task, employee, now, employeeOverride, effective, companyWeeklyOffs, scheduleId, schedule, existing, current, close, insert, active, companySchedules, names, Fail, Employee, Schedule
- `Codeji.CMS.Services/Attendance/OfficeScheduleSettingsService.cs` - symbols: IOfficeScheduleSettingsService, DeleteAsync, Task, schedule, entities, defaultSchedule, createResult, if, demoteResult, items, companySchedules, sameNameExists, Fail, scheduledMinutes, existing, entity, writeResult, hasEmployeeAssignments
- `Codeji.CMS.Services/Attendance/WeeklyOffService.cs` - symbols: IWeeklyOffService, Get, Save, WeeklyOffService, setting, days, previousDays, automaticRows, invalidatedSummaries, month, isLocked, isPayrolled, removed
- `Codeji.CMS.Services/Attendance/WorkFromHomeService.cs` - symbols: IWorkFromHomeService, SavePolicy, SaveEmployeeAllocation, Decide, Cancel, CheckIn, CheckOut, CorrectInsufficientHours, ResolveAttendanceConflict, SaveRemarkOption, CompanyId, UserId, Task, policy, Ok, employee, schedule, companyNow, today, officeStart, week, eligible, used, weeklyLimit, remaining, reason
- `src/app/modules/attendance/AttendanceCalendarLayout.test.ts` - symbols: style
- `src/app/modules/attendance/attendanceGridLayout.ts` - symbols: type, ATTENDANCE_EMPLOYEE_COLUMN_WIDTH, ATTENDANCE_DATE_COLUMN_WIDTH, ATTENDANCE_GRID_MAX_HEIGHT, buildAttendanceGridScrollContainerSx
- `src/app/modules/attendance/AttendanceModels.ts` - symbols: AttendanceStatus, AttendancePayload, AttendanceModel, CalendarAttendanceRow, GetAllAttendanceData, AdminAttendanceCreateDto
- `src/app/modules/attendance/attendancePeriod.ts` - symbols: attendancePeriods, AttendancePeriod, AttendanceDateRange, resolveAttendanceDateRange, today, previousMonth
- `src/app/modules/attendance/AttendanceServices.ts` - symbols: getAllAttendance, postRequest, addEditAttendance, deleteAttendance, deleteRequest, exportMonthlyAttendance, getRequest
- `src/app/modules/attendance/attendanceStatusColors.test.ts` - symbols: map
- `src/app/modules/attendance/attendanceStatusColors.ts` - symbols: DEFAULT_ATTENDANCE_STATUS_COLOR, AttendanceStatusSettingColorInput, normalizeAttendanceStatusCode, toAlphaHex, cleaned, alpha, normalizedAlpha, buildAttendanceStatusColorMap, code, color, resolveAttendanceStatusColor, normalized
- `src/app/modules/attendance/AttendanceStatusSettings.test.tsx` - symbols: StatusPayload, makeStatus, getMockedGetRequest, getMockedPostRequest, renderComponent, container, root, act, flush, response, activeCheckbox, editButton, checkbox, saveButton, payload
- `src/app/modules/attendance/AttendanceStatusSettings.tsx` - symbols: AttendanceStatusSetting, cloneStatuses, sectionSx, load, result, loaded, addDraft, requestDelete, confirmDelete, item, handleCancelEdit, saveAll, changedItems, original, statusTableGrid, statusTableHeaders
- `src/app/modules/attendance/component/AttendanceCalendar.tsx` - symbols: Employee, AttendanceRow, AttendanceData, AttendanceSegmentData, attendanceStatusLabels, statusesWithTimes, formatTimePreset, timeToMinutes, hours, createPunchPresets, start, end, minutes, workflowOwnedStatusCodes, isManualAttendanceStatus, manualAttendanceStatusOptions, Props, theme, isCompactAttendanceGrid, employeeColumnWidth, dateColumnWidth, fitDateColumns, attendanceDatesPaneRef, attendanceHorizontalScrollbarRef, attendanceEmployeesPaneRef, isSynchronizingAttendanceScroll, isSynchronizingAttendanceVerticalScroll, syncAttendanceHorizontalScroll, syncAttendanceVerticalScroll, attendanceStatusColorMap
- `src/app/modules/attendance/component/MonthlyAttendanceExceptions.tsx` - symbols: PayrollException, AttendanceStatusOption, ExistingAttendance, AttendanceResolution, Props, exceptionDescription, name, PenaltyReviewModal, exceptionStatusColor, checkInPresets, checkOutPresets, monthlyExceptionsExpandedStorageKey, isValidTime, formatTimeLabel, load, payrollMonth, postRequest, review, resolveAllFixed, reviewAllPenaltyExceptions, penalties, patchRequest, submitPenaltyReview
- `src/app/modules/attendance/EmployeeAttendance.tsx` - symbols: useDebounce, handler, EmployeeAttendance, intl, location, theme, userData, isAdministrator, isHrAttendanceRole, hasAnyAttendancePermission, canViewAllAttendance, canManageAttendance, debouncedSearch, fetchEmployees, response, getRequest, loadMyCorrectionRequests, requestId, request, handleSearchChange, dateRange, calendarMonth, calendarOffset, todayOverview, cancelled, postRequest, schedules
- `src/app/modules/attendance/WorkFromHome.tsx` - symbols: Policy, Request, RequestPage, RequestFilters, Timing, Context, showLegacyRequestForm, canManagePolicy, saving, savePolicy, load, loadReviewRequests, queryString, weekStart, weekEnd, rosterFilters, response, rosterResponse, submit, timing, act, cancelRequest, currentWeekStart, currentWeekEnd, companyBusinessDate, currentWeekWfh
- `src/app/modules/holidays/Calendar.tsx` - symbols: permissions, canImport, handleCategoryChange, handleRefresh, handleSelectedDateChange, readImportFile, file, workbook, sheetRows, normaliseColumn, headerIndex, columns, headers, dateColumn, nameColumn, typeColumn, rows, rawDate, formattedDate, name, sourceType, submitImport, fetchedHolidayData
- `src/app/modules/holidays/calendarServices.ts` - symbols: getAllCalendarItem, postRequest, addEditCalendarItem, deleteCalendarItem, deleteRequest, getHolidays, body, importCalendarItems
- `src/app/modules/holidays/components/CalendarDays.tsx` - symbols: Props, muiTheme, popoverOpen, isSmallScreen, weekDayShort, open, getDaysInCurrentMonth, activeDate, firstDateOfMonth, daysInCurrentMonth, checkIsSameDay, today, dayjs, findOccasionsForMonth, occassionMonth, handlePopoverOpen, handlePopoverClose, checkForOccasionForDate, holidayDate, showDayOccassion, handleCloseSmlPopover, updatedDate, date, PropType, ToolTipPopOver
- `src/app/modules/holidays/components/CalendarHeader.tsx` - symbols: Props, increaseMonth, decreaseMonth
- `src/app/modules/holidays/components/CalendarSidebar.tsx` - symbols: Props, CalendarItems, modulePermission, hasAddPermission, fetchedHolidayData, response, handleClose, findUpComingOccassions, today, thisMonthHolidaysFinder, occDate, getMonthSummary, holidayCounter, eventCounter, birthdayCounter, anniversaryCounter, holidayMonth, findHolidaysForDay, occassionDate
- `src/app/modules/holidays/components/HolidayFilter.tsx` - symbols: PropsType, dispatch, appliedHolFilters, size, value
- `src/app/modules/holidays/components/HolidayForm.tsx` - symbols: PropsTypes, handleFileChange, files, selectedFile, handleAddUpdateHoliday, formData
- `src/app/modules/holidays/components/UpcomingHolidayCard.tsx` - symbols: Props, UpcomingHolidayCard, modulePermission, hasEditPermission, hasDeletePermission, handleActionButtonClick, handleActionMenuClose, handleHolidayDeleteClick, handleHolidayEdit, holiday, handleCloseModal, handleDeleteModalClose, handleHolidayDeletion, date
- `src/app/modules/holidays/models.ts` - symbols: OccasionModel, OccasionResponseModel, CalendarItemType, GetCalendarItemPayload, CalendarItem, HolidayFilterModel, HolidayPayload, HolidayModel, GetAllCalendarItemData, AddHolidayResponseData, CalendarImportItem

## 4. Cross-Module and Recovery Rules

- Preserve tenant/company filters at every service/repository boundary.
- Preserve optimistic version/state checks for stateful workflows.
- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.
- Verify whether Mongo transactions are available before describing rollback as atomic.
- Verify outbox/job retry behavior from the actual processor before promising delivery.

