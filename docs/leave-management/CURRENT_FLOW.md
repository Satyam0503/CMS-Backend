# Leave Management Functional and Current Flow

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

### FLOW-001 - CreateNewLeavePolicy

#### Trigger

An HTTP client calls `POST api/LeaveManagement/CreateLeavePolicy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateLeavePolicy")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.CreateNewLeavePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] LeavePolicyRequest model`.
4. The controller executes/delegates to `No direct delegated call conclusively extracted; inspect the action body.`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:13`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:93`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-002 - UpdateLeavePolicy

#### Trigger

An HTTP client calls `POST api/LeaveManagement/UpdateLeavePolicy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateLeavePolicy")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.UpdateLeavePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] UpdateLeavePolicyRequest model`.
4. The controller executes/delegates to `No direct delegated call conclusively extracted; inspect the action body.`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:14`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:148`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-003 - GetAllLeavePolicies

#### Trigger

An HTTP client calls `POST api/LeaveManagement/GetAllLeavePolicies`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllLeavePolicies")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetAllLeavePolicies()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] bool? status`.
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

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:152`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-004 - GetMySelfServicePolicies

#### Trigger

An HTTP client calls `GET api/LeaveManagement/me/policies`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/policies")] [HttpGet]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetMySelfServicePolicies()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_leaveManagementService.GetMySelfServicePolicies()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:16`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:358`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-005 - GetEmployeeLeaveBalance

#### Trigger

An HTTP client calls `GET api/LeaveManagement/GetEmployeeLeaveBalance/{employeeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetEmployeeLeaveBalance/{employeeId}")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetEmployeeLeaveBalance()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string employeeId`.
4. The controller executes/delegates to `_leaveManagementService.GetEmployeeLeaveBalance()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:30`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1412`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: SECOND_HALF; statuses: .

### FLOW-006 - GetMyLeaveBalance

#### Trigger

An HTTP client calls `GET api/LeaveManagement/me/balances`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/balances")] [HttpGet]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetMyLeaveBalance()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_leaveManagementService.GetEmployeeLeaveBalance()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:30`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1412`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: SECOND_HALF; statuses: .

### FLOW-007 - CreateLeaveRequest

#### Trigger

An HTTP client calls `POST api/LeaveManagement/CreateLeaveRequest`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateLeaveRequest")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.CreateLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] LeaveRequestDto leaveRequest`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-008 - CreateMyLeaveRequest

#### Trigger

An HTTP client calls `POST api/LeaveManagement/me/requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests")] [HttpPost]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.CreateMyLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] LeaveRequestDto leaveRequest`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-009 - CreateEmployeeLeaveRequest

#### Trigger

An HTTP client calls `POST api/LeaveManagement/CreateEmployeeLeaveRequest/{employeeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CreateEmployeeLeaveRequest/{employeeId}")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.CreateEmployeeLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string employeeId, [FromBody] LeaveRequestDto leaveRequest`.
4. The controller executes/delegates to `string.IsNullOrWhiteSpace()`.
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

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:114`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-010 - UpdateLeaveRequest

#### Trigger

An HTTP client calls `POST api/LeaveManagement/UpdateLeaveRequest`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateLeaveRequest")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.UpdateLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] UpdateLeaveRequestDto leaveRequest`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-011 - UpdateMyLeaveRequest

#### Trigger

An HTTP client calls `PATCH api/LeaveManagement/me/requests`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests")] [HttpPatch]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.UpdateMyLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] UpdateLeaveRequestDto leaveRequest`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-012 - GetLeaveRequest

#### Trigger

An HTTP client calls `POST api/LeaveManagement/GetLeaveRequests`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetLeaveRequests")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `LeaveRequestFilter? leaveFilter`.
4. The controller executes/delegates to `_leaveManagementService.GetLeaveRequest()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:23`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:622`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ; statuses: .

### FLOW-013 - GetMyLeaveRequests

#### Trigger

An HTTP client calls `POST api/LeaveManagement/GetMyLeaveRequests`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetMyLeaveRequests")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetMyLeaveRequests()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] EmpLeaveRequestFilter leaveFilter`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-014 - GetMyProfileLeaveRequests

#### Trigger

An HTTP client calls `POST api/LeaveManagement/me/requests/search`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests/search")] [HttpPost]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetMyProfileLeaveRequests()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] EmpLeaveRequestFilter leaveFilter`.
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

#### Service evidence found

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:233`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: LEAVE_AFTER_EXIT; statuses: .

### FLOW-015 - DeleteLeaveRequest

#### Trigger

An HTTP client calls `DELETE api/LeaveManagement/DeleteLeaveRequest/{leaveRequestId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteLeaveRequest/{leaveRequestId}")] [HttpDelete] [ModulePermission(AppModule.LeaveManagement, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.DeleteLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string leaveRequestId`.
4. The controller executes/delegates to `_leaveManagementService.DeleteLeaveRequest()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:25`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:729`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference; errors: ; statuses: Pending.

### FLOW-016 - DeleteMyLeaveRequest

#### Trigger

An HTTP client calls `DELETE api/LeaveManagement/me/requests/{leaveRequestId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("me/requests/{leaveRequestId}")] [HttpDelete]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.DeleteMyLeaveRequest()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string leaveRequestId`.
4. The controller executes/delegates to `_leaveManagementService.DeleteLeaveRequest()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:25`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:729`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference; errors: ; statuses: Pending.

### FLOW-017 - UpdateLeaveRequestStatus

#### Trigger

An HTTP client calls `PATCH api/LeaveManagement/LeaveRequest/{leaveRequestId}/Status`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("LeaveRequest/{leaveRequestId}/Status")] [HttpPatch] [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.UpdateLeaveRequestStatus()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string leaveRequestId, [FromBody] LeaveRequestUpdateDto model`.
4. The controller executes/delegates to `_leaveManagementService.UpdateLeaveRequestStatus()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:26`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:746`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference; errors: ; statuses: Pending.

### FLOW-018 - GetLeaveRequestSummary

#### Trigger

An HTTP client calls `GET api/LeaveManagement/GetLeaveRequestSummary`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetLeaveRequestSummary")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetLeaveRequestSummary()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_leaveManagementService.GetLeaveRequestSummary()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:27`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1230`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ; statuses: .

### FLOW-019 - GetMonthlyTakenLeaveSummary

#### Trigger

An HTTP client calls `GET api/LeaveManagement/GetMonthlyTakenLeaveSummary/{year}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetMonthlyTakenLeaveSummary/{year}")] [HttpGet] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetMonthlyTakenLeaveSummary()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `int? year`.
4. The controller executes/delegates to `_leaveManagementService.GetMonthlyTakenLeaveSummary()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1267`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ; statuses: .

### FLOW-020 - UpdateEmpLeaveBalance

#### Trigger

An HTTP client calls `POST api/LeaveManagement/UpdateEmployeeLeaveBalance`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateEmployeeLeaveBalance")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, [Permission.Create, Permission.Edit]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.UpdateEmpLeaveBalance()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `List<LeaveBalanceRequestDto> LeaveBalanceRequestDto`.
4. The controller executes/delegates to `_leaveManagementService.UpdateEmployeeLeaveBalance()`.
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

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs:31`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:1291`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ; statuses: .

### FLOW-021 - GetAllEmployeeLeaveBalances

#### Trigger

An HTTP client calls `POST api/LeaveManagement/GetAllEmployeeLeaveBalances`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllEmployeeLeaveBalances")] [HttpPost] [ModulePermission(AppModule.LeaveManagement, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `LeaveManagementController.GetAllEmployeeLeaveBalances()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] LeaveBalanceFilter filter`.
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

- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs:152`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

## 3. Service and UI Evidence Inventory

- `Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs` - symbols: ILeaveManagementService, CreateNewLeavePolicy, CreateLeaveRequest, UpdateLeaveRequest, DeleteLeaveRequest, UpdateLeaveRequestStatus, UpdateEmployeeLeaveBalance, EmployeeLeaveBalanceAccrual
- `Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs` - symbols: IMongoDbRepository, LeaveManagementService, CreateNewLeavePolicy, result, exist, leavePolicy, sync, Task, companyId, hasBalances, hasRequests, duplicateLeavePolicy, policyBalances, duplicateEmployeeCount, updatedPolicyModel, updateResult, if
- `src/app/modules/leaves/components/AddLeavePolicy.tsx` - symbols: AttendanceStatusOption, Props, renderInfoIcon, handleCreateNewPolicy, handleUpdateLeavePolicy, message, handleListOpen, getAllEmployeesList, handleClose, paperSx, halfDayWfhEnabled, applicableTo, policy, policyType, label, limitTags, ids, Leave, Active, id, selectedOptionId, optionValues, sx
- `src/app/modules/leaves/components/AddLeaveTypes.tsx` - symbols: Props, handleLeaveTypeSubmit
- `src/app/modules/leaves/components/EditEmployeePoliciesForm.tsx` - symbols: Props, InitialValueType, GetLeaveBalanceValue, allocation, policyLimit, limit, eligible, handleLeaveBalanceSubmission, message, statusMessage, changedLeaveBalances, original, selectedLeave, handleLeavePolicyChange, existingIndex, exist, handleLeaveBalanceChange, label
- `src/app/modules/leaves/components/EmployeeLeaveTypeMonthlyChart.tsx` - symbols: Props, EmployeeLeaveTypeMonthlyChart, theme, targetYear, start, end, res, items, map, policy, m, lbls, seriesData
- `src/app/modules/leaves/components/EmployeesLeaveBalance.tsx` - symbols: EmployeesLeaveBalance, defaultLang, debouncedValue, open, handleClickOptions, handleFilterClick, handleClose, setRowPerPage, setPage, handleRefresh, handleSelectingPolicies, getLeaveBalanceFunc, byEmployeeId, allocation
- `src/app/modules/leaves/components/FilterPolicySearch.tsx` - symbols: Props, size, value
- `src/app/modules/leaves/components/LeaveManagementTabs.tsx` - symbols: LeaveManagementTabs, navigate
- `src/app/modules/leaves/components/LeavePolicies.tsx` - symbols: LeavePolicies, handleClose, handleRefresh, handleEdit
- `src/app/modules/leaves/components/LeaveRequest.tsx` - symbols: LeaveRequest, modulePermissions, canCreateForEmployee, appliedLeaveRequestFilter, fetchLeaveRequest, handlePageChange, handleRowPerPageChange, handleRefresh, CreateEmployeeLeaveModalProps, CreateEmployeeLeaveModal, loadEmployees, continueToApplication, initialValues, handleClose, PropsType, LeaveApprovalModal, isRejection, friendlyMessage, handleLeaveStatusUpdate, responseMessage, error, handleChange, type
- `src/app/modules/leaves/components/LeaveRequestFilter.tsx` - symbols: LeaveRequestFilter, dispatch, appliedLeaveRequestFilter, debouncedValues, statusValue, handleResetFilter
- `src/app/modules/leaves/components/LeaveRequestSummaryChart.tsx` - symbols: CreateChartDataType, LeaveRequestSummaryChart, intl
- `src/app/modules/leaves/components/MonthlyLeaveTakenLineChart.tsx` - symbols: MonthlyLeaveTakenLineChart, theme, monthShort
- `src/app/modules/leaves/components/Summary.tsx` - symbols: createData, rows
- `src/app/modules/leaves/LeaveManagement.tsx` - symbols: intl
- `src/app/modules/leaves/leaveServices.ts` - symbols: createUpdateLeaveType, postRequest, getLeavePolicies, getLeaveTypes, getRequest, createLeavePolicy, updateLeavePolicy, getLeaveBalance, serializeLeaveRequestPayload, createLeaveRequest, createEmployeeLeaveRequest, createMyProfileLeaveRequest, WorkFromHomeRequestPayload, createWorkFromHomeRequest, WorkFromHomeEmployeeAllocation, WorkFromHomeEmployeeAllocationsResponse, getMySelfServiceLeavePolicies, getWorkFromHomeEmployeeAllocations, saveWorkFromHomeEmployeeAllocation
- `src/app/modules/leaves/model.ts` - symbols: LeaveType, RequestLeaveModel, MyLeaveRequestModel, LeaveRequestModel, LeaveRequestsFilterModel, GetLeaveRequestPayLoad, LeaveBalanceFilter, LeaveTypeBalance, LeaveBalanceType, LeaveBalance, EmployeeLeaveTypeBalance, LeaveRequestApprovalPayload, LeaveRequestSummary, MonthlyLeaveTakenSummary, LeavePolicyModel, EmployeeLeaveBalanceFilter, EmployeeLeaveInfo, EmployeeLeaveBalanceData, GetLeaveTypeResponse, GetLeaveBalanceResponse, GetLeaveRequestsResponse, GetMyLeaveRequestsResponse, GetLeaveRequestsSummary, GetEmployeeLeaveBalanceResponse
- `src/app/routing/LeaveManagementRoutes.tsx` - symbols: LeaveManagementRoutes

## 4. Cross-Module and Recovery Rules

- Preserve tenant/company filters at every service/repository boundary.
- Preserve optimistic version/state checks for stateful workflows.
- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.
- Verify whether Mongo transactions are available before describing rollback as atomic.
- Verify outbox/job retry behavior from the actual processor before promising delivery.

