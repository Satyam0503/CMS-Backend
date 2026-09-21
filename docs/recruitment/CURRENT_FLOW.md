# Recruitment and Career Portal Functional and Current Flow

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

### FLOW-001 - GetApplicantList

#### Trigger

An HTTP client calls `POST api/Applicants/GetApplicantList`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetApplicantList")] [ModulePermission(AppModule.Applications, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.GetApplicantList()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `ApplicantResultFilters? filters`.
4. The controller executes/delegates to `_applicantsService.GetApplicantsList()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:320`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:15`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-002 - ApplicantById

#### Trigger

An HTTP client calls `GET api/Applicants/ApplicantById`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpGet] [Route("ApplicantById")] [ModulePermission(AppModule.Applications, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.ApplicantById()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string id`.
4. The controller executes/delegates to `_applicantsService.ApplicantById()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:376`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:16`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-003 - AddApplicant

#### Trigger

An HTTP client calls `POST api/Applicants/AddApplicant`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddApplicant")] [ModulePermission(AppModule.Applications, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.AddApplicant()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] ApplicantAddEditModel applicantRegisterModel`.
4. The controller executes/delegates to `_applicantsService.RegisterApplicants()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:73`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:12`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-004 - EditApplicants

#### Trigger

An HTTP client calls `POST api/Applicants/EditApplicant`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("EditApplicant")] [ModulePermission(AppModule.Applications, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.EditApplicants()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] ApplicantAddEditModel model`.
4. The controller executes/delegates to `_applicantsService.UpdateApplicants()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:126`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:14`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-005 - UploadResume

#### Trigger

An HTTP client calls `POST api/Applicants/UploadResume`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("UploadResume")] [AllowAnonymous] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.UploadResume()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromForm] ResumeApplicantModel model, string email`.
4. The controller executes/delegates to `Path.GetExtension()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:296`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:259`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-006 - AddComment

#### Trigger

An HTTP client calls `POST api/Applicants/AddComment`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [Route("AddComment")] [HttpPost] [ModulePermission(AppModule.Applications, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.AddComment()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CommentRequestModel model`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:516`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-007 - GetAllComment

#### Trigger

An HTTP client calls `GET api/Applicants/GetAllComment/{applicantId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [Route("GetAllComment/{applicantId}")] [HttpGet] [ModulePermission(AppModule.Applications, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.GetAllComment()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string applicantId, [FromQuery] int pageNo, [FromQuery] int pageSize`.
4. The controller executes/delegates to `_applicantsService.GetAllComment()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:434`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:17`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-008 - GetProcessLogData

#### Trigger

An HTTP client calls `POST api/Applicants/GetProcessLogData`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetProcessLogData")] [ModulePermission(AppModule.ProcessLog, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `ApplicantsController.GetProcessLogData()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `ApplicantLogFilterModel model`.
4. The controller executes/delegates to `_applicantsService.GetProcessLogData()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:470`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:19`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-009 - Get

#### Trigger

An HTTP client calls `GET api/CareerProfile`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpGet] [ModulePermission(AppModule.CareerProfile, Permission.View)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CareerProfileController.Get()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetCompanyProfile()`.
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

- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:10`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:70`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-010 - Save

#### Trigger

An HTTP client calls `PUT api/CareerProfile`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpPut] [ModulePermission(AppModule.CareerProfile, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CareerProfileController.Save()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `UpsertPublicCompanyProfileRequest request`.
4. The controller executes/delegates to `service.SaveCompanyProfile()`.
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

- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:11`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:78`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-011 - UpdateTheme

#### Trigger

An HTTP client calls `PUT api/CareerProfile/theme`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpPut("theme")] [ModulePermission(AppModule.CareerProfile, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `CareerProfileController.UpdateTheme()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `UpdateCompanyThemeRequest request`.
4. The controller executes/delegates to `service.UpdateCompanyTheme()`.
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

- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:15`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:260`; signals: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-012 - AddJobVacancy

#### Trigger

An HTTP client calls `POST api/JobVacancy/AddJobVacancy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddJobVacancy")] [HttpPost] [ModulePermission(AppModule.Jobs, Permission.Create)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `JobVacancyController.AddJobVacancy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `JobVacancyModel jobVacancy`.
4. The controller executes/delegates to `_jobVacancyService.AddJobVacancy()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:8`; signals: repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:30`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-013 - EditJobVacancy

#### Trigger

An HTTP client calls `POST api/JobVacancy/EditJobVacancy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditJobVacancy")] [HttpPost] [ModulePermission(AppModule.Jobs, Permission.Edit)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `JobVacancyController.EditJobVacancy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `JobVacancyModel jobVacancy, string jobId`.
4. The controller executes/delegates to `_jobVacancyService.EditJobVacancy()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:9`; signals: repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:79`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-014 - GetAllVacancy

#### Trigger

An HTTP client calls `POST api/JobVacancy/GetAllVacancy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllVacancy")] [HttpPost] [AllowAnonymous]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `JobVacancyController.GetAllVacancy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `JobRequestModel model`.
4. The controller executes/delegates to `_jobVacancyService.GetAllVacancy()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:10`; signals: repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:138`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-015 - GetVacancyById

#### Trigger

An HTTP client calls `POST api/JobVacancy/GetVacancyById`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetVacancyById")] [HttpPost]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `JobVacancyController.GetVacancyById()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string vacancyId`.
4. The controller executes/delegates to `_jobVacancyService.GetVacancyById()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:109`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:11`; signals: repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:208`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-016 - DeleteJobVacancy

#### Trigger

An HTTP client calls `DELETE api/JobVacancy/DeleteJobVacancy`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteJobVacancy")] [HttpDelete] [ModulePermission(AppModule.Jobs, Permission.Delete)]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `JobVacancyController.DeleteJobVacancy()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string vacancyId`.
4. The controller executes/delegates to `_jobVacancyService.DeleteJobVacancy()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:12`; signals: repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:216`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-017 - GetJobs

#### Trigger

An HTTP client calls `GET api/public/jobs`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.GetJobs()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] PublicJobSearchRequest request`.
4. The controller executes/delegates to `_publicCareerService.GetMasterJobs()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:9`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:70`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-018 - GetJobByPublicId

#### Trigger

An HTTP client calls `GET api/public/jobs/{publicJobId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs/{publicJobId}")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.GetJobByPublicId()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string publicJobId`.
4. The controller executes/delegates to `_publicCareerService.GetJobByPublicId()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:12`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:148`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-019 - GetJobBySlug

#### Trigger

An HTTP client calls `GET api/public/jobs/by-slug/{jobSlug}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs/by-slug/{jobSlug}")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.GetJobBySlug()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string jobSlug`.
4. The controller executes/delegates to `_publicCareerService.GetJobBySlug()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:13`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:153`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-020 - GetCompanyJobs

#### Trigger

An HTTP client calls `GET api/public/companies/{companyCode}/jobs`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("companies/{companyCode}/jobs")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.GetCompanyJobs()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string companyCode, [FromQuery] PublicJobSearchRequest request`.
4. The controller executes/delegates to `_publicCareerService.GetCompanyJobs()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:10`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:76`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-021 - GetLocations

#### Trigger

An HTTP client calls `GET api/public/job-locations`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("job-locations")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.GetLocations()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string? companyCode, [FromQuery] string? search`.
4. The controller executes/delegates to `_publicCareerService.GetLocations()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:11`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:107`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-022 - Apply

#### Trigger

An HTTP client calls `POST api/public/jobs/{publicJobId}/applications`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpPost("jobs/{publicJobId}/applications")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.Apply()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string publicJobId, [FromBody] PublicJobApplicationRequest request`.
4. The controller executes/delegates to `_publicCareerService.Apply()`.
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

- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:14`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:159`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-023 - UploadResume

#### Trigger

An HTTP client calls `POST api/public/applications/{applicationReference}/resume`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpPost("applications/{applicationReference}/resume")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerController.UploadResume()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string applicationReference, [FromForm] string token, [FromForm] IFormFile resume`.
4. The controller executes/delegates to `_publicCareerService.UploadResume()`.
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

- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:284`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:22`; signals: notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:15`; signals: ; errors: ; statuses: .
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:255`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-024 - Save

#### Trigger

An HTTP client calls `POST api/public/career/jobs/{publicJobId}/save`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("jobs/{publicJobId}/save")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Save()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string publicJobId, SaveJobRequest request`.
4. The controller executes/delegates to `service.SaveJob()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:55`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:8`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-025 - Unsave

#### Trigger

An HTTP client calls `DELETE api/public/career/jobs/{publicJobId}/save`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("jobs/{publicJobId}/save")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Unsave()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string publicJobId, [FromQuery] string? visitorToken`.
4. The controller executes/delegates to `service.RemoveSavedJob()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:80`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:9`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-026 - Saved

#### Trigger

An HTTP client calls `GET api/public/career/jobs/saved`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("jobs/saved")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Saved()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string? visitorToken`.
4. The controller executes/delegates to `service.GetSavedJobs()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:95`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:10`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-027 - Merge

#### Trigger

An HTTP client calls `POST api/public/career/jobs/merge-anonymous-saves`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("jobs/merge-anonymous-saves")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Merge()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `MergeAnonymousSavesRequest request`.
4. The controller executes/delegates to `service.MergeAnonymousSaves()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:125`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:11`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-028 - Follow

#### Trigger

An HTTP client calls `POST api/public/career/companies/{companyCode}/follow`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("companies/{companyCode}/follow")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Follow()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string companyCode, FollowCompanyRequest request`.
4. The controller executes/delegates to `service.FollowCompany()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:147`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:12`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-029 - Unfollow

#### Trigger

An HTTP client calls `DELETE api/public/career/companies/{companyCode}/follow`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("companies/{companyCode}/follow")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Unfollow()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string companyCode`.
4. The controller executes/delegates to `service.UnfollowCompany()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:168`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:13`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-030 - FollowStatus

#### Trigger

An HTTP client calls `GET api/public/career/companies/{companyCode}/follow-status`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("companies/{companyCode}/follow-status")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.FollowStatus()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string companyCode`.
4. The controller executes/delegates to `service.GetFollowStatus()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:179`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:14`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-031 - CreateAlert

#### Trigger

An HTTP client calls `POST api/public/career/job-alerts`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("job-alerts")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.CreateAlert()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `JobAlertRequest request`.
4. The controller executes/delegates to `service.CreateAlert()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:197`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:15`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-032 - GetAlerts

#### Trigger

An HTTP client calls `GET api/public/career/job-alerts`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("job-alerts")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.GetAlerts()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetAlerts()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:208`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:16`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-033 - UpdateAlert

#### Trigger

An HTTP client calls `PUT api/public/career/job-alerts/{alertId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPut("job-alerts/{alertId}")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.UpdateAlert()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string alertId, JobAlertRequest request`.
4. The controller executes/delegates to `service.UpdateAlert()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:216`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:17`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-034 - DeleteAlert

#### Trigger

An HTTP client calls `DELETE api/public/career/job-alerts/{alertId}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("job-alerts/{alertId}")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.DeleteAlert()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string alertId`.
4. The controller executes/delegates to `service.DeleteAlert()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:231`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:18`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-035 - Track

#### Trigger

An HTTP client calls `POST api/public/career/analytics`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("analytics")] [EnableRateLimiting("career-analytics")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.Track()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CareerAnalyticsRequest request`.
4. The controller executes/delegates to `service.Track()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:240`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:19`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-036 - VisitorPreferences

#### Trigger

An HTTP client calls `PUT api/public/career/visitor-preferences`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPut("visitor-preferences")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.VisitorPreferences()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `VisitorPreferenceRequest request`.
4. The controller executes/delegates to `service.UpdateVisitorPreference()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:299`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:20`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-037 - VisitorPreferences

#### Trigger

An HTTP client calls `GET api/public/career/visitor-preferences`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("visitor-preferences")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerEngagementController.VisitorPreferences()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string visitorToken`.
4. The controller executes/delegates to `service.GetVisitorPreference()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:317`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:21`; signals: ; errors: ; statuses: .

### FLOW-038 - Subscribe

#### Trigger

An HTTP client calls `POST api/public/career/subscribers`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.Subscribe()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CareerSubscribeRequest request`.
4. The controller executes/delegates to `service.Subscribe()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:41`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:9`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-039 - Verify

#### Trigger

An HTTP client calls `POST api/public/career/subscribers/verify`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("verify")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.Verify()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CareerTokenRequest request`.
4. The controller executes/delegates to `service.Verify()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:96`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:10`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-040 - Resend

#### Trigger

An HTTP client calls `POST api/public/career/subscribers/resend-verification`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("resend-verification")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.Resend()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CareerSubscribeRequest request`.
4. The controller executes/delegates to `service.Resend()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:123`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:11`; signals: repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .

### FLOW-041 - Unsubscribe

#### Trigger

An HTTP client calls `POST api/public/career/subscribers/unsubscribe`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("unsubscribe")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.Unsubscribe()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CareerTokenRequest request`.
4. The controller executes/delegates to `service.Unsubscribe()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:126`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:12`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-042 - GetPreferences

#### Trigger

An HTTP client calls `GET api/public/career/subscribers/preferences`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpGet("preferences")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.GetPreferences()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetPreferences()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:148`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:13`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-043 - UpdatePreferences

#### Trigger

An HTTP client calls `PUT api/public/career/subscribers/preferences`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPut("preferences")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCareerSubscriptionController.UpdatePreferences()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `UpdateCareerPreferencesRequest request`.
4. The controller executes/delegates to `service.UpdatePreferences()`.
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

- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:156`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:14`; signals: repository/data-access reference; errors: ; statuses: .

### FLOW-044 - GetCompanies

#### Trigger

An HTTP client calls `GET api/public/career/companies`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/companies")] [HttpGet]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCompanyProfileController.GetCompanies()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
4. The controller executes/delegates to `service.GetPublishedProfiles()`.
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

- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:8`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:27`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

### FLOW-045 - GetProfile

#### Trigger

An HTTP client calls `GET api/public/career/companies/{companyCode}/profile`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [AllowAnonymous] [Route("api/public/career/companies")] [HttpGet("{companyCode}/profile")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `PublicCompanyProfileController.GetProfile()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string companyCode`.
4. The controller executes/delegates to `service.GetPublishedProfile()`.
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

- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:9`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:53`; signals: company/tenant context reference, repository/data-access reference, audit/history metadata reference; errors: ; statuses: .

## 3. Service and UI Evidence Inventory

- `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs` - symbols: IMongoCollection, CareerEngagementService, SaveJob, job, subscriber, visitorHash, identityFilter, filter, update, TryTrack, Ok, RemoveSavedJob, identity, Task, saved, jobs, companies
- `Codeji.CMS.Services/CareerPortal/CareerNotificationService.cs` - symbols: IMongoCollection, CareerNotificationService, HandleJobSaved, company, now, companyVisible, masterVisible, previousCompanyVisible, previousMasterVisible, QueueNewJob, if, QueueJobUpdate, ProcessNext, QueueSavedDeadlineReminders, claimable, item, FindOneAndUpdateOptions, processingItems, subscriber, pending, MarkSent, payload
- `Codeji.CMS.Services/CareerPortal/CareerNotificationWorker.cs` - symbols: 
- `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs` - symbols: IMongoCollection, CareerSubscriptionService, Subscribe, generic, email, now, rawVerification, rawUnsubscribe, existing, TryTrack, Task, subscriber, session, Resend, Unsubscribe, subscriberId, Preferences, Authorize, QueueVerification, baseUrl, url
- `Codeji.CMS.Services/CareerPortal/CareerTokenProtector.cs` - symbols: expires, payload, separator, parts, key, Encode, padded
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs` - symbols: ICareerEngagementService, SaveJob, RemoveSavedJob, MergeAnonymousSaves, FollowCompany, UnfollowCompany, DeleteAlert, Track, UpdateVisitorPreference
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerNotificationService.cs` - symbols: ICareerNotificationService, HandleJobSaved, ProcessNext
- `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs` - symbols: ICareerSubscriptionService, Subscribe, Resend, Unsubscribe, Authorize
- `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs` - symbols: IPublicCompanyProfileService, UpdateCompanyTheme
- `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs` - symbols: IMongoCollection, PublicCompanyProfileService, Task, companies, resolvedCompanies, resolved, companyIds, profiles, profileMap, code, company, profile, lengthError, urlError, now, existing, socialLinksResult, socialValues, urls, invalidAbsolute, shortValues, richValues
- `Codeji.CMS.Services/Recruitments/ApplicantServices.cs` - symbols: IMongoDbRepository, ApplicantServices, RegisterApplicants, result, companyId, isApplicantExist, applicant, LogNewApplication, SendApplicationAlertToRecruitmentTeam, SendEmailToApplicant, vacancy, log, UpdateApplicants, actor, previousActivityType, AddStatusHistory, name, ApplyNowService
- `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs` - symbols: IApplicantsService, RegisterApplicants, ApplyNowService, UpdateApplicants, AddComment, UploadResume
- `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs` - symbols: IJobVacancy, GetVacancyById, DeleteJobVacancy
- `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs` - symbols: IPublicCareerService
- `Codeji.CMS.Services/Recruitments/JobVacancyService.cs` - symbols: IMongoDbRepository, JobVacancyService, Task, vacancy, saved, editedJob, jobList, count, jobIds, applicationCounts, data, result, GetVacancyById, DeleteJobVacancy, BuildUniqueSlug, baseSlug, slug, counter
- `Codeji.CMS.Services/Recruitments/PublicCareerService.cs` - symbols: IMongoCollection, PublicCareerService, Task, companies, code, cf, companyFilter, company, normalizedCompanyCode, f, filter, BsonRegularExpression, locations, slug
- `src/app/modules/applynow/ApplicationForm.tsx` - symbols: Props, intl, type
- `src/app/modules/applynow/Career.tsx` - symbols: PublicCareerCompany, Career, active, resolveCompany
- `src/app/modules/applynow/Component/ApplyJobForm.tsx` - symbols: Props, handleFileChange, files, selectedFile, handleApplicationSubmission, formData, uploadResumeResponse, responseData, initialValues, label, check
- `src/app/modules/applynow/Component/CareerHeader.tsx` - symbols: CareerHeader, theme, isSmallScreen, toggleSmallScreenNavar, navlisnks
- `src/app/modules/applynow/Component/CareerHero.tsx` - symbols: CareerHero
- `src/app/modules/applynow/Component/FindJobsHeader.tsx` - symbols: 
- `src/app/modules/applynow/Component/Footer.tsx` - symbols: intl
- `src/app/modules/applynow/Component/JobInfo.tsx` - symbols: Props, handleClose
- `src/app/modules/applynow/Component/JobSearchCareer.tsx` - symbols: FilterProps, debouncedValue, handleInputChange, handleSelectChange, getActiveVacancy, response
- `src/app/modules/applynow/Component/JobsSection.tsx` - symbols: Props, theme, isSmallScreen, handleChangePostion, selectedJob, isSelectedJob, isJobSelectedOnSmallScreen
- `src/app/modules/applynow/Component/NoVacancy.tsx` - symbols: intl, sx
- `src/app/modules/applynow/Component/WhyJoin.tsx` - symbols: WhyJoin
- `src/app/modules/applynow/MasterCareers.tsx` - symbols: MasterCareers, navigate, validCompanyCode, active, request, companyName, value
- `src/app/modules/applynow/publicCareerService.ts` - symbols: getPublicJobs, getCompanyPublicJobs, getPublicJobBySlug, applyForPublicJob, uploadPublicResume, formData, postRequest
- `src/app/modules/applynow/publicCareerTypes.ts` - symbols: PublicJobSummary, PublicJobDetails, PublicJobApplicationPayload, PublicJobApplicationResponse
- `src/app/modules/applynow/PublicJobApplyModal.tsx` - symbols: Props, initialForm, PublicJobApplyModal, update, submit, uploadPublicResume, fieldSx, maxWidth, options, renderInput, inputProps
- `src/app/modules/applynow/PublicJobDetails.tsx` - symbols: PublicJobDetails, description, active
- `src/app/modules/applynow/services.ts` - symbols: applyNow, postRequest
- `src/app/modules/careerPortal/components/CareerHeader.tsx` - symbols: CareerHeader
- `src/app/modules/careerPortal/components/CompanyCareerTheme.tsx` - symbols: Props, CompanyCareerTheme, companyTheme, muiTheme
- `src/app/modules/careerPortal/components/CompanyLogo.tsx` - symbols: CompanyLogoProps, CompanyLogo, showImage
- `src/app/modules/careerPortal/components/JobCard.tsx` - symbols: JobCard, navigate
- `src/app/modules/careerPortal/components/SaveJobButton.tsx` - symbols: Props, SaveJobButton, saved, toggle
- `src/app/modules/careerPortal/components/SubscribeModal.tsx` - symbols: Props, SubscribeModal, close, doNotShowAgain, submit, subscribe
- `src/app/modules/careerPortal/context/SavedJobsContext.tsx` - symbols: SavedJobsContextValue, SavedJobsContext, SavedJobsProvider, refreshSavedJobs, setJobSaved, next, value, useSavedJobs, context
- `src/app/modules/careerPortal/hooks/useCareerVisitor.ts` - symbols: VISITOR_KEY, SESSION_KEY, createToken, getCareerVisitorToken, value, getCareerSession, setCareerSession, clearCareerSession, useCareerVisitor
- `src/app/modules/careerPortal/pages/CareerJobDetails.tsx` - symbols: DetailRow, ListSection, CareerJobDetails, description, active, session, value, recent, toggleFollow, nextFollowing
- `src/app/modules/careerPortal/pages/CareerMarketplace.tsx` - symbols: Filters, FilterContent, options, CareerMarketplace, theme, desktop, validCode, timer, active, params, activeFilters, companyName
- `src/app/modules/careerPortal/pages/CareerPreferencesPage.tsx` - symbols: CareerPreferencesPage, load, verify, unsubscribeToken, start, subscribe, savePreferences, updatePreferences, addAlert, value, createAlert, removeAlert, deleteAlert
- `src/app/modules/careerPortal/pages/CareerProfileAdminPage.tsx` - symbols: type, parseCommaSeparatedValues, ProfileFieldProps, ProfileField, help, label, HtmlEditor, editorRef, safeValue, updateValue, command, insertLink, url, minRows, role, CsvField, parsedDraft, matchesValue, nextDraft, CareerProfileAdminPage, userData, canEdit, pageHeaderActionsTarget, set, save, saved, publicPageUrl
- `src/app/modules/careerPortal/pages/SavedJobsPage.tsx` - symbols: SavedJobsPage, load, remove, unsaveJob
- `src/app/modules/careerPortal/services/careerPortalService.ts` - symbols: sessionConfig, discoverJobs, getCompanyProfile, getCompanies, getJobLocations, getManagedCompanyProfile, saveManagedCompanyProfile, saveJob, unsaveJob, getSavedJobs, subscribe, verifySubscription, unsubscribe, getPreferences, updatePreferences, followCompany, unfollowCompany, getFollowStatus, getAlerts, createAlert, deleteAlert, dismissSubscribePopup, getVisitorPreferences, trackCareerEvent
- `src/app/modules/careerPortal/types/careerPortalTypes.ts` - symbols: PublicCompanyProfile, SavedCareerJob, CareerPreferences, CompanyFollowStatus, JobAlert
- `src/app/modules/careerPortal/utils/careerSearch.test.ts` - symbols: first, params
- `src/app/modules/careerPortal/utils/careerSearch.ts` - symbols: CareerSearchFilters, buildCareerSearchParams
- `src/app/modules/recruitment/applications/Applications.tsx` - symbols: CustomDataGrid, useApplicantTableColumnList, values, experience, ActionsCell, closePreview, Applications, dispatch, intl, columnList, compactView, responsiveColumns, navigate, applicationFilters, open, id, totalAppliedFilter, userModulesPermissions, jobId, userPermissionsForModule, handleFilterClick, handleSearchChange, rowClickHandler, handlePaginationModelChange, payload, getApplicantList
- `src/app/modules/recruitment/applications/components/AddApplicant.tsx` - symbols: PropsTypes, handleClose, handleFileChange, files, selectedFile, handleAddApplicant, formData, payload, uploadResumeResponse, responseData, getAllVacancy, rowSpacing
- `src/app/modules/recruitment/applications/components/AddComment.tsx` - symbols: PropTypes, intl
- `src/app/modules/recruitment/applications/components/ApplicantDetails.tsx` - symbols: ApplicantDetails, navigate, currentUser, param, userPermissions, canEditApplicant, payload, getAllComments, response, handleCommentChange, applicantId, handleEditApplicant, handleAddComment, hasStatusChange, commentPayload, commentResponse, handleClose, handleStatusChange, getStatusLabel, configuredStatus, isStatusChangePending, isTerminalStatus, handlePageChange, handleRowPerPageChange, spacing
- `src/app/modules/recruitment/applications/components/ApplicationFilter.tsx` - symbols: dispatch, applicationFilters, isTimeOptionOpen, getActiveJobs, response, handleTimeOptionMenuClose, handleClick, handleTimePeriodChange, currentFilters, selectedTimePeriod, updatedFromDate, updatedToDate, size, value, error, type
- `src/app/modules/recruitment/applications/components/ApplicationPreview.tsx` - symbols: PropsType, experience, direction
- `src/app/modules/recruitment/applications/components/EditApplicant.tsx` - symbols: EditApplicant, navigate, user, param, applicantId, getAllVacancy, response, handleCommentChange, handleAddComment, actionCategory, payload, handleFileChange, files, selectedFile, handleEditApplicant, formData, vacancy, totalExperienceInMonths, resumeUploadResponse, handleModalClose, direction
- `src/app/modules/recruitment/jobs/components/AddEditJob.tsx` - symbols: PropsTypes, commaSeparatedJobFields, CommaSeparatedJobField, serializeCommaSeparatedList, parseCommaSeparatedList, FormSwitch, defaultLanguage, handleJobDescriptionChange, setListValue, departmentName, placeholder, error, jobType, name, timeSteps
- `src/app/modules/recruitment/jobs/components/JobFilter.tsx` - symbols: jobFilters, dispatch, value, size
- `src/app/modules/recruitment/jobs/components/ViewJobDetails.tsx` - symbols: PropsTypes, handleClose
- `src/app/modules/recruitment/jobs/Jobs.tsx` - symbols: CustomDataGrid, useJobTableColumnList, values, ActionsCell, closePreview, handleJobDetailView, open, id, userModulesPermissions, jobFilters, columnList, compactView, responsiveColumns, hasAddPermission, hasEditPermission, handleSearchChange, handleClose, handleEditJob, closeConfirmationModal, handleFilterClick, handledeleteJob, response, handleAddEditJob, payload, jobId, handlePaginationModelChange
- `src/app/modules/recruitment/models.ts` - symbols: ApplicantModel, EditApplicantModel, Applicant, JobsFilterModel, JobModal, CommentModel, GetAllVacancyPayload, CommentResponseModel
- `src/app/modules/recruitment/processlog/components/ProcessLogFilter.tsx` - symbols: processLogFilters, dispatch, getActiveJobs, response, size, value
- `src/app/modules/recruitment/processlog/ProcessLog.tsx` - symbols: ProcessLogs, intl, open, processLogFilters, totalAppliedFilters, handleSearchChange, handlePageChange, handleRowPerPageChange, handleFilterClick, handleCloseFilter
- `src/app/modules/recruitment/services.ts` - symbols: addJobVacancy, postRequest, editJobVacancy, getAllJobVacancies, deleteJobVacancy, deleteRequest, getAllApplicantList, addApplicant, editApplicant, getApplicantById, getRequest, uploadResume, getProcessLogData, addComment, getApplicantComments
- `src/app/routing/RecruitmentRoutes.tsx` - symbols: RecruitmentRoutes

## 4. Cross-Module and Recovery Rules

- Preserve tenant/company filters at every service/repository boundary.
- Preserve optimistic version/state checks for stateful workflows.
- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.
- Verify whether Mongo transactions are available before describing rollback as atomic.
- Verify outbox/job retry behavior from the actual processor before promising delivery.

