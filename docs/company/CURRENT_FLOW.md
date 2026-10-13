# Company, Calendar and Schedules Functional and Current Flow

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

### FLOW-001 - GetAllCalendarItems

#### Trigger

An HTTP client calls `POST api/Calendar/GetAllCalendarItems`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetAllCalendarItems")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CalendarController.GetAllCalendarItems()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] CalendarFilters? filters`.
4. The controller executes/delegates to `_calendarServices.GetAllCalendarItems()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:38`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:10`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-002 - AddUpdateCalendarItem

#### Trigger

An HTTP client calls `POST api/Calendar/AddUpdateCalendarItem`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddUpdateCalendarItem")] [HttpPost] [ModulePermission(AppModule.Calendar, [Permission.Create, Permission.Edit]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CalendarController.AddUpdateCalendarItem()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromForm] CalendarRequestDto model`.
4. The controller executes/delegates to `_calendarServices.AddUpdateCalendarItem()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:140`; signals: company/tenant context reference, repository/data-access reference; errors: CALENDAR_IMPORT_NO_NEW_ITEMS; statuses: .
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:8`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-003 - ImportCalendarItems

#### Trigger

An HTTP client calls `POST api/Calendar/ImportCalendarItems`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ImportCalendarItems")] [HttpPost] [ModulePermission(AppModule.Calendar, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CalendarController.ImportCalendarItems()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] CalendarBulkImportRequest model`.
4. The controller executes/delegates to `_calendarServices.ImportCalendarItems()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:201`; signals: company/tenant context reference, repository/data-access reference; errors: CALENDAR_IMPORT_NO_NEW_ITEMS; statuses: .
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:9`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-004 - DeleteCalendarItem

#### Trigger

An HTTP client calls `DELETE api/Calendar/DeleteCalendarItem/{itemId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteCalendarItem/{itemId}")] [HttpDelete] [ModulePermission(AppModule.Calendar, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CalendarController.DeleteCalendarItem()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string itemId`.
4. The controller executes/delegates to `_calendarServices.DeleteItem()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:240`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:11`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-005 - GetHolidays

#### Trigger

An HTTP client calls `POST api/Calendar/GetHolidays`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetHolidays")] [HttpPost]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CalendarController.GetHolidays()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] HolidayFilter filter`.
4. The controller executes/delegates to `_calendarServices.GetHolidays()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:284`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs:12`; signals: ; errors: ; statuses: .

### FLOW-006 - GetAllCompanyList

#### Trigger

An HTTP client calls `GET api/Company/GetAllCompanyList`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllCompanyList")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetAllCompanyList()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_companyService.GetAllCompanyList()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:259`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:13`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-007 - GetCompanyDetails

#### Trigger

An HTTP client calls `GET api/Company/GetCompanyDetails`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetCompanyDetails")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetCompanyDetails()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-008 - GetPublicCareerCompany

#### Trigger

An HTTP client calls `GET api/Company/career`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("career")] [AllowAnonymous]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetPublicCareerCompany()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_companyService.GetPublicCareerCompany()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:271`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:16`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-009 - GetPublicCareerCompany

#### Trigger

An HTTP client calls `GET api/Company/career/{publicCompanyCode}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("career/{publicCompanyCode}")] [AllowAnonymous]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetPublicCareerCompany()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string publicCompanyCode`.
4. The controller executes/delegates to `_companyService.GetPublicCareerCompany()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:271`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:16`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-010 - GetCurrentCompanyTheme

#### Trigger

An HTTP client calls `GET api/Company/theme`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet("theme")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetCurrentCompanyTheme()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `_companyService.GetCompanyDetails()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:375`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:15`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-011 - UpdateCompanyDetails

#### Trigger

An HTTP client calls `POST api/Company/UpdateCompanyDetails`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("UpdateCompanyDetails")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.UpdateCompanyDetails()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromForm] UpdateCompanyInfoRequestModel model`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-012 - AddPolicy

#### Trigger

An HTTP client calls `POST api/Company/AddPolicy`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("AddPolicy")] [ModulePermission(AppModule.Policy, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.AddPolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] CreatePolicyRequestModel model`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-013 - UpdatePolicy

#### Trigger

An HTTP client calls `POST api/Company/UpdatePolicy`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("UpdatePolicy")] [ModulePermission(AppModule.Policy, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.UpdatePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] UpdatePolicyRequestModel model`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-014 - GetAllPolicies

#### Trigger

An HTTP client calls `GET api/Company/GetAllPolicies`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllPolicies")] [ModulePermission(AppModule.Policy, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetAllPolicies()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-015 - DeletePolicy

#### Trigger

An HTTP client calls `DELETE api/Company/DeletePolicy/{policyId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpDelete] [Route("DeletePolicy/{policyId}")] [ModulePermission(AppModule.Policy, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.DeletePolicy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string policyId`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-016 - AddPolicyVersion

#### Trigger

An HTTP client calls `POST api/Company/CreatePolicyVersion`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("CreatePolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.AddPolicyVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromForm] PolicyVersionRequestModel model`.
4. The controller executes/delegates to `_companyService.AddPolicyVersion()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:632`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, transaction/session reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:34`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-017 - EditPolicyVersion

#### Trigger

An HTTP client calls `POST api/Company/EditPolicyVersion`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("EditPolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.EditPolicyVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromForm] PolicyVersionUpdateModel model`.
4. The controller executes/delegates to `_companyService.EditPolicyVersion()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:720`; signals: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:35`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-018 - SetCurrentPolicyVersion

#### Trigger

An HTTP client calls `POST api/Company/SetCurrentPolicyVersion`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("SetCurrentPolicyVersion")] [ModulePermission(AppModule.Policy, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.SetCurrentPolicyVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] SetCurrentPolicyVersionRequestModel model`.
4. The controller executes/delegates to `_companyService.SetCurrentPolicyVersion()`.
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

- `Codeji.CMS.Services/Companies/CompanyService.cs:895`; signals: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs:36`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-019 - GetAllPolicyVersion

#### Trigger

An HTTP client calls `GET api/Company/GetAllPolicyVersion/{policyId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllPolicyVersion/{policyId}")] [ModulePermission(AppModule.Policy, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetAllPolicyVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string policyId`.
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

### FLOW-020 - DeletePolicyVersion

#### Trigger

An HTTP client calls `DELETE api/Company/DeletePolicyVersion/{policyVersionId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpDelete] [Route("DeletePolicyVersion/{policyVersionId}")] [ModulePermission(AppModule.Policy, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.DeletePolicyVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string policyVersionId`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-021 - GetPolicyDocument

#### Trigger

An HTTP client calls `GET api/Company/GetPolicyDocument/{policyVersionId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetPolicyDocument/{policyVersionId}")] [ModulePermission(AppModule.Policy, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyController.GetPolicyDocument()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string policyVersionId`.
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

### FLOW-022 - GetOfficeSchedule

#### Trigger

An HTTP client calls `GET api/CompanyMaster/OfficeSchedule`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("OfficeSchedule")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetOfficeSchedule()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleSettings.GetAsync()`.
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

### FLOW-023 - GetOfficeSchedules

#### Trigger

An HTTP client calls `GET api/CompanyMaster/OfficeSchedules`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("OfficeSchedules")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetOfficeSchedules()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleSettings.GetAllAsync()`.
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

### FLOW-024 - SaveOfficeSchedule

#### Trigger

An HTTP client calls `PUT api/CompanyMaster/OfficeSchedule`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("OfficeSchedule")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.SaveOfficeSchedule()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `OfficeScheduleSettingsDto request, CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleSettings.SaveAsync()`.
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

### FLOW-025 - DeleteOfficeSchedule

#### Trigger

An HTTP client calls `DELETE api/CompanyMaster/OfficeSchedule/{scheduleId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("OfficeSchedule/{scheduleId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.DeleteOfficeSchedule()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string scheduleId, CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleSettings.DeleteAsync()`.
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

### FLOW-026 - GetDepartmentOfficeScheduleAssignments

#### Trigger

An HTTP client calls `GET api/CompanyMaster/DepartmentOfficeScheduleAssignments`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("DepartmentOfficeScheduleAssignments")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetDepartmentOfficeScheduleAssignments()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleAssignments.GetDepartmentsAsync()`.
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

### FLOW-027 - SetDepartmentOfficeScheduleAssignment

#### Trigger

An HTTP client calls `PUT api/CompanyMaster/DepartmentOfficeScheduleAssignment/{departmentId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("DepartmentOfficeScheduleAssignment/{departmentId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.SetDepartmentOfficeScheduleAssignment()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string departmentId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken`.
4. The controller executes/delegates to `_officeScheduleAssignments.SetDepartmentAsync()`.
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

### FLOW-028 - AddEditDepartment

#### Trigger

An HTTP client calls `POST api/CompanyMaster/UpdateDepartment`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("UpdateDepartment")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.AddEditDepartment()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `List<DepartmentRequestDto> data`.
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

### FLOW-029 - GetDepartmentList

#### Trigger

An HTTP client calls `GET api/CompanyMaster/GetDepartmentList`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetDepartmentList")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetDepartmentList()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] bool? isActive`.
4. The controller executes/delegates to `_companyMasterService.GetDepartmentList()`.
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

- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:85`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:15`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-030 - DeleteDepartment

#### Trigger

An HTTP client calls `DELETE api/CompanyMaster/DeleteDepartment/{departmentId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteDepartment/{departmentId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.DeleteDepartment()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string departmentId`.
4. The controller executes/delegates to `_companyMasterService.DeleteDepartment()`.
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

- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:107`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:16`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-031 - UpdateModuleAccess

#### Trigger

An HTTP client calls `PATCH api/CompanyMaster/UpdateModuleAccess/{moduleId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPatch] [Route("UpdateModuleAccess/{moduleId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.UpdateModuleAccess()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string moduleId`.
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

#### Service evidence found

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:143`; signals: company/tenant context reference, repository/data-access reference; errors: CALENDAR_IMPORT_NO_NEW_ITEMS; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:69`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyService.cs:332`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-032 - GetAllModuleDetails

#### Trigger

An HTTP client calls `GET api/CompanyMaster/GetAllModuleDetails`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetAllModuleDetails")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetAllModuleDetails()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-033 - AddUpdateJobTitle

#### Trigger

An HTTP client calls `POST api/CompanyMaster/AddUpdateJobTitle`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddUpdateJobTitle")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.AddUpdateJobTitle()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `List<JobTitleRequestDto> data`.
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

### FLOW-034 - GetAllJobTitles

#### Trigger

An HTTP client calls `GET api/CompanyMaster/GetJobTitles`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetJobTitles")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetAllJobTitles()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] bool? isActive, [FromQuery] string? departmentId`.
4. The controller executes/delegates to `_companyMasterService.GetJobTitles()`.
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

- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:175`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:22`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-035 - DeleteJobTitle

#### Trigger

An HTTP client calls `DELETE api/CompanyMaster/DeleteJobTitle/{jobTitleId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteJobTitle/{jobTitleId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.DeleteJobTitle()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string jobTitleId`.
4. The controller executes/delegates to `_companyMasterService.DeleteJobTitle()`.
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

- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:301`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: CUSTOM_ATTRIBUTE_ID_REQUIRED, CUSTOM_ATTRIBUTE_TITLE_REQUIRED, CUSTOM_ATTRIBUTE_NOT_FOUND; statuses: .
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs:23`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

### FLOW-036 - CreateCustomAttribute

#### Trigger

An HTTP client calls `POST api/CompanyMaster/CreateCustomAttribute`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPost] [Route("CreateCustomAttribute")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.CreateCustomAttribute()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-037 - GetAllCustomAttributes

#### Trigger

An HTTP client calls `GET api/CompanyMaster/GetAllCustomAttribute`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetAllCustomAttribute")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetAllCustomAttributes()`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-038 - GetCustomAttributeById

#### Trigger

An HTTP client calls `GET api/CompanyMaster/GetCustomAttributeById/{customAttributeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpGet] [Route("GetCustomAttributeById/{customAttributeId}")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.GetCustomAttributeById()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string customAttributeId, [FromQuery] bool? active`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-039 - UpdateCustomAttribute

#### Trigger

An HTTP client calls `PUT api/CompanyMaster/UpdateCustomAttribute`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpPut] [Route("UpdateCustomAttribute")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.UpdateCustomAttribute()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] CustomAttributeRequestDto data`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-040 - DeleteCustomAttribute

#### Trigger

An HTTP client calls `DELETE api/CompanyMaster/DeleteCustomAttribute/{customAttributeId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteCustomAttribute/{customAttributeId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.DeleteCustomAttribute()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string customAttributeId`.
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

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:28`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:63`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-041 - DeleteCustomAttributeValue

#### Trigger

An HTTP client calls `DELETE api/CompanyMaster/DeleteCustomAttributeValue/{customAttributeValueId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [HttpDelete] [Route("DeleteCustomAttributeValue/{customAttributeValueId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CompanyMasterController.DeleteCustomAttributeValue()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string customAttributeValueId`.
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

#### Service evidence found

- `Codeji.CMS.Services/Calendar/CalendarServices.cs:143`; signals: company/tenant context reference, repository/data-access reference; errors: CALENDAR_IMPORT_NO_NEW_ITEMS; statuses: .
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs:69`; signals: company/tenant context reference, permission reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Companies/CompanyService.cs:332`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .

## 3. Service and UI Evidence Inventory

- `Codeji.CMS.Services/Calendar/CalendarServices.cs` - symbols: IMongoDbRepository, ILogger, CompanyId, CalendarServices, Task, result, calendarItems, year, if, date, today, dateFormat, employeesList, birthdaysThisYear, thisYearBirthday, workAnniversariesThisYear, thisYearAnniversary, AddUpdateCalendarItem, newItem, existingItem, existingCalendarItem, ImportCalendarItems, normalized, duplicateRows, existing, additions, saved, DeleteItem
- `Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs` - symbols: ICalendarServices, AddUpdateCalendarItem, ImportCalendarItems, DeleteItem
- `Codeji.CMS.Services/Companies/CompanyMasterService.cs` - symbols: IMongoDbRepository, CompanyMasterService, UpdateDepartments, companyId, departments, result, Task, data, DeleteDepartment, modulesPermission, rolePermissions, roleId, hasAccess
- `Codeji.CMS.Services/Companies/CompanyService.cs` - symbols: IMongoDbRepository, ILogger, CompanyService, Register, result, emailExists, companyId, adminRole, administratorRole, user, company, notificationPreferenceSetting, userResult, SendCompanyUserVerificationEmail, SeedDefaultDepartmentsAndJobTitles, token, tokenHash, tokenExpiryHours, securityToken
- `Codeji.CMS.Services/Companies/DefaultCompanySeeds.cs` - symbols: readonly, List, langs, now, Dictionary, result
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyMasterService.cs` - symbols: ICompanyMasterService, UpdateDepartments, DeleteDepartment, AddUpdateJobTitle, DeleteJobTitle, CreateCustomAttribute, UpdateCustomAttribute, DeleteCustomAttribute, DeleteCustomAttributeValue
- `Codeji.CMS.Services/Companies/Interfaces/ICompanyService.cs` - symbols: ICompanyService, Register, IsActiveCompanyExist, UpdateCompanyLogo, GetCompanyExistingLogo, AddCompanyLogo, AddPolicy, UpdatePolicy, DeletePolicy, DeletePolicyVersion
- `src/app/modules/admin/companydetails/CompanyDetails.tsx` - symbols: CompanyDetailsTab, CompanyDetails, location
- `src/app/modules/admin/companydetails/components/ApplicationModules.tsx` - symbols: ActionType, dispatch, user, friendlyModuleLabel, translated, closeConfirmationModal, updateModuleAccessApi, updateModuleAccess, updateSingleModule, response, updateBulkModules, requestAction, handleConfirmation, getAllModules, payrollValues, recruitmentValues, payroll, recruitment, remaining, moduleDesc, isDisabling
- `src/app/modules/admin/companydetails/components/CompanyInfo.tsx` - symbols: SupportedImageFormat, MAX_FILE_SIZE, CompanyInfo, applyEmployeeIdSequence, userData, dispatch, defaultLanguage, applicationLanguages, handleImageCropDiscard, handleImageCropSuccess, handleUpdateCompanyDetails, handleFileChange, files, selectedFile, fileSizeInMB, fileFormat, reader, formData, base64Image, blob, Uint8Array, response, refreshEmployeeIdSequence, handleEditCancel, getEmployeeIdSequence, toggleFormerEmployeePortal, confirmSkip
- `src/app/modules/admin/companydetails/components/CompanyInfoSkeleton.tsx` - symbols: CompanyInfoSkeleton
- `src/app/modules/admin/companydetails/components/ModuleCard.tsx` - symbols: ModuleCardProps, installedModule, isInstalled, isHardDisabled, isDisabled
- `src/app/modules/admin/companymaster/CompanyMaster.tsx` - symbols: getCustomAttributeTitle, CompanyMasterTab, CompanyMaster, navigate, location, AddNewCustomAttribute, existingDraft, UpdateCustomAttributeValue, exists, RemoveCustomAttribute, namedCustomAttributes, active, path
- `src/app/modules/admin/companymaster/components/AddNewAttributeItem.tsx` - symbols: Props, AddNewAttributeItem, defaultCompanyLanguage, currentLang, translationLanguageTitle, currentLanguageTitle, handleChange, titles, validateAndUpdateNewItemData, index, currentLanguageName, hasDefaultLangTitle, handleSwitchChange, updatedItem, handleSelectChange
- `src/app/modules/admin/companymaster/components/AttributeItemRow.tsx` - symbols: Props, ReqProps, AttributeItemRow, defaultCompanyLanguage, currentLanguage, translatedTitle, currentTitle, handleChange, updatedLanguage, updatedTitles, updatedItem, handleItemDelete, handleDeleteAttribute, handleSwitchChange, updateItem, hasDefaultLangTitle, handleSelectChange, validateAndUpdateNewItemData, currentLanguageName
- `src/app/modules/admin/companymaster/components/AttributeTable.tsx` - symbols: Props, AttributeTable, defaultCompanyLanguage, currentLanguage, itemsRef, newItemsRef, initialAttributeTitle, handleReset, UpdateExistingItemData, updated, UpdateNewItemsList, handleUpdate, allItems, shouldUpdate, handleAttributeUpdate, savedItems, updateAttributeTitle, persistedItems, id, value, selectedLanguage, language, uuid
- `src/app/modules/admin/companymaster/components/AttributeTitleRow.tsx` - symbols: AttributeTitleProps, AttributeTitleRow, defaultCompanyLanguage, defaultLanguage, translationLanguageTitle, currentLanguageTitle, handleChange, updatedTitle, validateAndUpdateNewItemData, hasDefaultLangTitle
- `src/app/modules/admin/companymaster/components/CustomAttribute.tsx` - symbols: CustomAttribute, navigate, param, customAttributeId, customAttributeName, attributeValues, fetchCustomAttribute, handleCustomAttributeUpdate, message, handleCustomAttributeItemDelete, response, handleCustomAttributeDelete
- `src/app/modules/admin/companymaster/components/CustomAttributeWrapper.tsx` - symbols: CustomAttributeWrapper, customAttributeId
- `src/app/modules/admin/companymaster/components/Department.tsx` - symbols: Departments, handleRefresh, handleDepartmentUpdate, handleDeleteDepartment, response, getAllDeps
- `src/app/modules/admin/companymaster/components/JobTitles.tsx` - symbols: JobTitles, handleRefresh, handleJobTitleUpdate, resp, handleDeleteJobTitle, response, getAllJobTitles
- `src/app/modules/admin/companymaster/components/OfficeSchedule.tsx` - symbols: OfficeScheduleForm, DepartmentOption, DepartmentShiftAssignment, toMinutes, formatTime, suffix, twelveHour, formatMinutes, safeMinutes, hours, remainder, ScheduleMetric, OfficeSchedule, visibleSchedules, nextShiftNumber, set, loadSchedules, items, selected, saveWeeklyOffs, schedule, start, end, span, available, required, assignedDepartmentIds, duplicateTimingSchedule, save, saved
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

