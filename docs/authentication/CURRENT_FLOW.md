# Authentication and Authorization Functional and Current Flow

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

### FLOW-001 - GetAntiForgeryToken

#### Trigger

An HTTP client calls `GET api/account/antiforgerytoken/{appKey}`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpGet] [Route("account/antiforgerytoken/{appKey}")] [AllowAnonymous]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.GetAntiForgeryToken()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string appKey`.
4. The controller executes/delegates to `_antiforgery.GetAndStoreTokens()`.
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

### FLOW-002 - CheckAppVersion

#### Trigger

An HTTP client calls `GET api/app/checkAppVersion`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpGet] [Route("app/checkAppVersion")] [AllowAnonymous]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.CheckAppVersion()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string appVersion`.
4. The controller executes/delegates to `_employeeService.IsEmailExist()`.
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

### FLOW-003 - Register

#### Trigger

An HTTP client calls `POST api/account/register`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpPost] [Route("account/register")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.Register()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] CompanyRequestModel companyModel`.
4. The controller executes/delegates to `_employeeService.IsEmailExist()`.
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

### FLOW-004 - Login

#### Trigger

An HTTP client calls `POST api/account/login`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpPost] [Route("account/login")] [AllowAnonymous] [EnableRateLimiting("account-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.Login()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] LoginModel model`.
4. The controller executes/delegates to `_accountService.VerifyAndGenerateToken()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs:80`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: Active, NoticePeriod.
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:12`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-005 - RefreshToken

#### Trigger

An HTTP client calls `POST api/account/refresh-token`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/refresh-token")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.RefreshToken()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] RefreshTokenRequestDto model`.
4. The controller executes/delegates to `_accountService.RefreshToken()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs:387`; signals: company/tenant context reference, repository/data-access reference; errors: ; statuses: .
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:13`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-006 - LogOut

#### Trigger

An HTTP client calls `POST api/account/logout`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpPost] [Route("account/logout")] [Authorize]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.LogOut()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] RefreshTokenRequestDto model`.
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

### FLOW-007 - GetUserByToken

#### Trigger

An HTTP client calls `POST api/account/getSignedUserDetails`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [HttpPost] [Route("account/getSignedUserDetails")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.GetUserByToken()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from ``.
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

### FLOW-008 - CreateNewPassword

#### Trigger

An HTTP client calls `POST api/CreateNewPassword`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [Route("CreateNewPassword")] [Route("account/CreateNewPassword")] [HttpPost]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.CreateNewPassword()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `CreateNewPasswordRequest model`.
4. The controller executes/delegates to `_accountService.CreateNewPassword()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs:239`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:16`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-009 - VerifyEmail

#### Trigger

An HTTP client calls `GET api/account/verify-email`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [HttpGet] [Route("account/verify-email")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.VerifyEmail()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string token`.
4. The controller executes/delegates to `_accountService.VerifyEmail()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs:283`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:17`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-010 - ResendEmailVerification

#### Trigger

An HTTP client calls `POST api/account/resend-email-verification`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/resend-email-verification")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.ResendEmailVerification()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] EmailVerificationRequest model`.
4. The controller executes/delegates to `_accountService.ResendEmailVerification()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs:336`; signals: company/tenant context reference, repository/data-access reference, notification/email/outbox reference; errors: ; statuses: .
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs:18`; signals: notification/email/outbox reference; errors: ; statuses: .

### FLOW-011 - ResetPassword

#### Trigger

An HTTP client calls `POST api/account/ResetPassword`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("account/ResetPassword")] [EnableRateLimiting("account-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.ResetPassword()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] JsonElement body`.
4. The controller executes/delegates to `body.GetString()`.
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

### FLOW-012 - RegisterApplicants

#### Trigger

An HTTP client calls `POST api/applicant/applyJob`.

#### Actor and preconditions

Authorization evidence: `[ApiController] [Route("api")] [AllowAnonymous] [HttpPost] [Route("applicant/applyJob")] [EnableRateLimiting("career-sensitive")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `AccountController.RegisterApplicants()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromBody] ApplicantAddEditModel applicantRegisterModel`.
4. The controller executes/delegates to `_applicantsServices.ApplyNowService()`.
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

### FLOW-013 - GetRoles

#### Trigger

An HTTP client calls `GET api/Roles/GetRoles`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoles")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.GetRoles()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] bool? excludeAdmin`.
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

### FLOW-014 - GetRolePermission

#### Trigger

An HTTP client calls `GET api/Roles/GetRolePermission`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRolePermission")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.GetRolePermission()`.
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

### FLOW-015 - AddRole

#### Trigger

An HTTP client calls `POST api/Roles/AddEditRole`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPost] [Route("AddEditRole")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.AddRole()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `RoleWithModuleAndPermissions model`.
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

### FLOW-016 - GetModulePermission

#### Trigger

An HTTP client calls `GET api/Roles/GetAllRolesWithPermission`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetAllRolesWithPermission")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.GetModulePermission()`.
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

### FLOW-017 - GetModulePermissionyId

#### Trigger

An HTTP client calls `GET api/Roles/GetRoleWithPermissionById`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoleWithPermissionById")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.GetModulePermissionyId()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `[FromQuery] string roleId`.
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

### FLOW-018 - GetRoleById

#### Trigger

An HTTP client calls `GET api/Roles/GetRoleById/{roleId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpGet] [Route("GetRoleById/{roleId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.GetRoleById()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string roleId`.
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

### FLOW-019 - UpdateAppAccessForRole

#### Trigger

An HTTP client calls `PATCH api/Roles/UpdateAppAccessForRole/{roleId}`.

#### Actor and preconditions

Authorization evidence: `[Route("api/[controller]")] [ApiController] [Authorize] [HttpPatch] [Route("UpdateAppAccessForRole/{roleId}")] [Authorize(Policy = "AdminOnly")]`.
The actor must also satisfy any company, ownership, relationship, state and concurrency checks in the delegated service.

#### Step-by-step flow

1. Routing selects `RolesController.UpdateAppAccessForRole()`.
2. ASP.NET Core applies authentication/authorization filters and binds the request.
3. Parameters are bound from `string roleId, [FromBody] bool hasAppAccess`.
4. The controller executes/delegates to `_roleService.UpdateAppAccessForRole()`.
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

- `Codeji.CMS.Services/Account/AccountServices.cs` - symbols: IMongoDbRepository, AccountServices, ResetPassword, result, if, Task, company, isPasswordValid, roles, token, refreshToken, hashedRefreshToken, refreshTokenEntity, result2, GenerateTokenAndSendEmail, emp, hashedToken, tokenExpiryMinutes, securityToken, tokenResult, resetLink
- `Codeji.CMS.Services/Account/Interface/IAccountServices.cs` - symbols: IAccountServices, ResetPassword, LogOut, GenerateTokenAndSendEmail, CreateNewPassword, VerifyEmail, ResendEmailVerification
- `Codeji.CMS.Services/Registration/EmailService.cs` - symbols: SendPasswordEmail, subject, body, message
- `Codeji.CMS.Services/Registration/Interface/IEmailService.cs` - symbols: IEmailService, SendPasswordEmail
- `Codeji.CMS.Services/Registration/MapsterConfig.cs` - symbols: Register
- `Codeji.CMS.Services/Registration/ServicesRegistration.cs` - symbols: 
- `src/app/modules/auth/AuthLayout.tsx` - symbols: benefits, AuthLayout, isDark, isDawn
- `src/app/modules/auth/AuthPage.tsx` - symbols: AuthPage
- `src/app/modules/auth/components/CreatePassword.tsx` - symbols: validationSchema, initialValues, CreatePassword, navigate, token, handlePasswordChange, payload, response
- `src/app/modules/auth/components/ForgotPassword.tsx` - symbols: forgotPasswordSchema, ForgotPassword, navigate, location, autoSubmitStarted, navigationState, emailFromLogin, formik, response, type
- `src/app/modules/auth/components/Login.tsx` - symbols: loginSchema, initialValues, Login, theme, dispatch, fieldSx, handleLogin, handleResendVerification, back
- `src/app/modules/auth/components/Registration.tsx` - symbols: initialValues, Registration, navigate, handleRegistration, payload
- `src/app/modules/auth/core/_models.ts` - symbols: AuthModel, CompanyRegisterModel, UserLoginModel, UserAddressModel, UserCommunicationModel, AuthTokens, RefreshTokenPayload, LogInResponseModel
- `src/app/modules/auth/core/_requests.ts` - symbols: registerCompany, postRequest, login, resendEmailVerification, logoutUser, refreshTokenAsync, getSignedUserDetails, createPassword, resetPassword
- `src/app/modules/auth/core/Auth.tsx` - symbols: currentUser, dispatch, navigate, requestUserDetails, savedLanguage, defaultLanguage
- `src/app/modules/auth/core/AuthHelpers.ts` - symbols: getLocalKey, setLocalKey, lsValue, removeLocalKey
- `src/app/modules/auth/index.ts` - symbols: 
- `src/app/modules/auth/Logout.tsx` - symbols: Logout, dispatch, signalRInstance, chatHubInstance, logOutUser, refreshToken
- `src/app/routing/AppRoutes.tsx` - symbols: 
- `src/app/routing/LeaveManagementRoutes.tsx` - symbols: LeaveManagementRoutes
- `src/app/routing/NoticeBoardRoutes.tsx` - symbols: NoticeBoardRoutes
- `src/app/routing/PoliciesRoutes.tsx` - symbols: Policies, PolicyVersions, SuspensedView, PoliciesRoutes
- `src/app/routing/PrivateRoutes.tsx` - symbols: Admin, Recruitment, Employees, Dashboard, Profile, Calendar, NoticeBoard, Policies, Attendance, LeaveManagement, PayRollManagement, PayrollSettings, AttendanceStatusSettings, PrivateRoutes, PropsType, SuspensedView, theme
- `src/app/routing/ProfileRoutes.tsx` - symbols: UserProfile, AboutUser, UserPerformance, WorkHistory, Setting, LeaveTracker, SuspensedView, ProfileTabs, empId, roleType, modulePermission, hasLeavePanelAccess
- `src/app/routing/RecruitmentRoutes.tsx` - symbols: RecruitmentRoutes

## 4. Cross-Module and Recovery Rules

- Preserve tenant/company filters at every service/repository boundary.
- Preserve optimistic version/state checks for stateful workflows.
- Treat derived attendance/payroll/notification state as a cross-module dependency, not a UI-only effect.
- Verify whether Mongo transactions are available before describing rollback as atomic.
- Verify outbox/job retry behavior from the actual processor before promising delivery.

