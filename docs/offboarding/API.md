# Offboarding API Documentation

**Last verified against code:** 2026-09-07

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

Lifecycle management endpoints are intentionally separate from the EmployeeExit case endpoints. Availability changes never create an exit or former-employee access, and lifecycle history is restricted to authorized HR/admin actors.

| Method | Endpoint | Purpose | Authorization |
|---|---|---|---|
| `POST` | `api/User/EmployeeLifecycle/Availability` | Change temporary availability with effective date, expected return date, reason, and optimistic version | Employees.Edit route plus Offboarding CaseManage/Edit/Override service authorization |
| `GET` | `api/User/EmployeeLifecycle/History/{employeeUserId}` | Read lifecycle audit timeline | Employees.Edit route plus Offboarding CaseManage/Edit/Override service authorization; tenant and employee ownership enforced |

`GET api/User/EmployeeExitQueue` returns active offboarding summaries. Case-management users receive their company queue; reporting managers receive only cases where they are the assigned reporting manager. It returns no evidence, notes, or other sensitive case details.

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/public/former-employee/redeem` | `PublicFormerEmployeePortalController` | `Redeem` | `portal.RedeemInvite()` |
| 2 | `POST` | `api/public/former-employee/otp/request` | `PublicFormerEmployeePortalController` | `RequestOtp` | `portal.RequestOtp()` |
| 3 | `POST` | `api/public/former-employee/otp/verify` | `PublicFormerEmployeePortalController` | `VerifyOtp` | `portal.VerifyOtp()` |
| 4 | `POST` | `api/public/former-employee/logout` | `PublicFormerEmployeePortalController` | `Logout` | `portal.Logout()` |
| 5 | `POST` | `api/public/former-employee/verify-contact` | `PublicFormerEmployeePortalController` | `VerifyContact` | `portal.VerifyPostExitContact()` |
| 6 | `GET` | `api/public/former-employee/case` | `PublicFormerEmployeePortalController` | `GetCase` | `portal.GetCase()` |
| 7 | `POST` | `api/public/former-employee/requests` | `PublicFormerEmployeePortalController` | `CreateRequest` | `portal.CreateRequest()` |
| 8 | `GET` | `api/public/former-employee/documents/{evidenceId}` | `PublicFormerEmployeePortalController` | `DownloadDocument` | `portal.GetDocument()` |
| 9 | `GET` | `api/public/former-employee/documents/{evidenceId}/preview` | `PublicFormerEmployeePortalController` | `PreviewDocument` | `portal.GetDocument()` |
| 10 | `POST` | `api/User/InitiateEmployeeExit` | `UserController` | `InitiateEmployeeExit` | `_employeeService.InitiateEmployeeExit()` |
| 11 | `POST` | `api/User/SubmitOwnResignation` | `UserController` | `SubmitOwnResignation` | `_employeeService.SubmitOwnResignation()` |
| 12 | `GET` | `api/User/EmployeeExit/{employeeUserId}` | `UserController` | `GetEmployeeExitCase` | `_employeeService.GetEmployeeExitCase()` |
| 13 | `GET` | `api/User/EmployeeExitHistory/{employeeUserId}` | `UserController` | `GetOwnEmployeeExitHistory` | `_employeeService.GetOwnEmployeeExitHistory()` |
| 14 | `GET` | `api/User/EmployeeExitManagement/{employeeUserId}` | `UserController` | `GetEmployeeExitCaseForManagement` | `_employeeService.GetEmployeeExitCaseForManagement()` |
| 15 | `POST` | `api/User/CompleteEmployeeExit` | `UserController` | `CompleteEmployeeExit` | `_employeeService.CompleteEmployeeExit()` |
| 16 | `POST` | `api/User/ReviewEmployeeExit` | `UserController` | `ReviewEmployeeExit` | `_employeeService.ReviewEmployeeExit()` |
| 17 | `POST` | `api/User/RespondToEmployeeExitDiscussion` | `UserController` | `RespondToEmployeeExitDiscussion` | `_employeeService.RespondToEmployeeExitDiscussion()` |
| 18 | `POST` | `api/User/RespondToEmployeeExitJointDiscussion` | `UserController` | `RespondToEmployeeExitJointDiscussion` | `_employeeService.RespondToEmployeeExitJointDiscussion()` |
| 19 | `POST` | `api/User/WithdrawOwnEmployeeExit` | `UserController` | `WithdrawOwnEmployeeExit` | `_employeeService.WithdrawOwnEmployeeExit()` |
| 20 | `POST` | `api/User/DecideEmployeeExitWithdrawal` | `UserController` | `DecideEmployeeExitWithdrawal` | `_employeeService.DecideEmployeeExitWithdrawal()` |
| 21 | `POST` | `api/User/CancelEmployeeExit` | `UserController` | `CancelEmployeeExit` | `_employeeService.CancelEmployeeExit()` |
| 22 | `POST` | `api/User/UpdateEmployeeExitDate` | `UserController` | `UpdateEmployeeExitDate` | `_employeeService.UpdateEmployeeExitDate()` |
| 23 | `POST` | `api/User/RequestEmployeeExitNoticeAdjustment` | `UserController` | `RequestEmployeeExitNoticeAdjustment` | `_employeeService.RequestEmployeeExitNoticeAdjustment()` |
| 24 | `POST` | `api/User/DecideEmployeeExitNoticeAdjustment` | `UserController` | `DecideEmployeeExitNoticeAdjustment` | `_employeeService.DecideEmployeeExitNoticeAdjustment()` |
| 25 | `POST` | `api/User/DecideOwnEmployeeExitNoticeOffer` | `UserController` | `DecideOwnEmployeeExitNoticeOffer` | `_employeeService.DecideOwnEmployeeExitNoticeOffer()` |
| 26 | `POST` | `api/User/EmployeeExitPolicy` | `UserController` | `UpsertEmployeeExitPolicy` | `_employeeService.UpsertEmployeeExitPolicy()` |
| 27 | `GET` | `api/User/EmployeeExitPolicy/{policyId}` | `UserController` | `GetEmployeeExitPolicy` | `_employeeService.GetEmployeeExitPolicy()` |
| 28 | `GET` | `api/User/EmployeeExitClearanceTemplate/{templateId}` | `UserController` | `GetEmployeeExitClearanceTemplate` | `_employeeService.GetEmployeeExitClearanceTemplate()` |
| 29 | `GET` | `api/User/EmployeeExitPolicies` | `UserController` | `GetEmployeeExitPolicies` | `_employeeService.GetEmployeeExitPolicies()` |
| 30 | `GET` | `api/User/EmployeeExitClearanceTemplates` | `UserController` | `GetEmployeeExitClearanceTemplates` | `_employeeService.GetEmployeeExitClearanceTemplates()` |
| 31 | `POST` | `api/User/EmployeeExitClearanceTemplate` | `UserController` | `UpsertEmployeeExitClearanceTemplate` | `_employeeService.UpsertEmployeeExitClearanceTemplate()` |
| 32 | `DELETE` | `api/User/EmployeeExitClearanceTemplate/{templateId}` | `UserController` | `DeleteEmployeeExitClearanceTemplate` | `_employeeService.DeleteEmployeeExitClearanceTemplate()` |
| 33 | `POST` | `api/User/ApproveEmployeeFinalSettlement` | `UserController` | `ApproveEmployeeFinalSettlement` | `_employeeService.ApproveEmployeeFinalSettlement()` |
| 34 | `POST` | `api/User/UpdateEmployeeFinalSettlementPayment` | `UserController` | `UpdateEmployeeFinalSettlementPayment` | `_employeeService.UpdateEmployeeFinalSettlementPayment()` |
| 35 | `POST` | `api/User/CalculateEmployeeFinalSettlement` | `UserController` | `CalculateEmployeeFinalSettlement` | `_employeeService.CalculateEmployeeFinalSettlement()` |
| 36 | `POST` | `api/User/MarkEmployeeFinalSettlementPaid` | `UserController` | `MarkEmployeeFinalSettlementPaid` | `_employeeService.MarkEmployeeFinalSettlementPaid()` |
| 37 | `POST` | `api/User/RecordEmployeeExitInterview` | `UserController` | `RecordEmployeeExitInterview` | `_employeeService.RecordEmployeeExitInterview()` |
| 38 | `POST` | `api/User/EmployeeExitReassignment` | `UserController` | `UpsertEmployeeExitReassignment` | `_employeeService.UpsertEmployeeExitReassignment()` |
| 39 | `POST` | `api/User/GenerateEmployeeExitDocument` | `UserController` | `GenerateEmployeeExitDocument` | `_employeeService.GenerateEmployeeExitDocument()` |
| 40 | `POST` | `api/User/IssueEmployeeExitDocument` | `UserController` | `IssueEmployeeExitDocument` | `_employeeService.IssueEmployeeExitDocument()` |
| 41 | `POST` | `api/User/CompleteEmployeeExitClearance` | `UserController` | `CompleteEmployeeExitClearance` | `_employeeService.CompleteEmployeeExitClearance()` |
| 42 | `POST` | `api/User/UpdateEmployeeExitClearanceStatus` | `UserController` | `UpdateEmployeeExitClearanceStatus` | `_employeeService.UpdateEmployeeExitClearanceStatus()` |
| 43 | `POST` | `api/User/WaiveEmployeeExitClearance` | `UserController` | `WaiveEmployeeExitClearance` | `_employeeService.CompleteEmployeeExitClearance()` |
| 44 | `POST` | `api/User/EmployeeExitHandoverTask` | `UserController` | `UpsertEmployeeExitHandoverTask` | `_employeeService.UpsertEmployeeExitHandoverTask()` |
| 45 | `POST` | `api/User/ReviewEmployeeExitHandoverTask` | `UserController` | `ReviewEmployeeExitHandoverTask` | `_employeeService.ReviewEmployeeExitHandoverTask()` |
| 46 | `POST` | `api/User/EmployeeExitAsset` | `UserController` | `UpsertEmployeeExitAsset` | `_employeeService.UpsertEmployeeExitAsset()` |
| 47 | `POST` | `api/User/EmployeeExitAccessItem` | `UserController` | `UpsertEmployeeExitAccessItem` | `_employeeService.UpsertEmployeeExitAccessItem()` |
| 48 | `POST` | `api/User/EmployeeExitFinanceItem` | `UserController` | `UpsertEmployeeExitFinanceItem` | `_employeeService.UpsertEmployeeExitFinanceItem()` |
| 49 | `POST` | `api/User/EmployeeExitBenefitItem` | `UserController` | `UpsertEmployeeExitBenefitItem` | `_employeeService.UpsertEmployeeExitBenefitItem()` |
| 50 | `POST` | `api/User/EmployeeExitEvidence` | `UserController` | `AddEmployeeExitEvidence` | `_employeeService.AddEmployeeExitEvidence()` |
| 51 | `POST` | `api/User/EmployeeExitEvidenceUpload` | `UserController` | `UploadEmployeeExitEvidence` | `_employeeService.GetEmployeeExitCaseForEvidence()` |
| 52 | `GET` | `api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download` | `UserController` | `DownloadEmployeeExitEvidence` | `_employeeService.GetEmployeeExitEvidence()` |
| 53 | `POST` | `api/User/VerifyEmployeeExitEvidence` | `UserController` | `VerifyEmployeeExitEvidence` | `_employeeService.VerifyEmployeeExitEvidence()` |
| 54 | `POST` | `api/User/SupersedeEmployeeExitEvidence` | `UserController` | `SupersedeEmployeeExitEvidence` | `_employeeService.SupersedeEmployeeExitEvidence()` |
| 55 | `POST` | `api/User/EmployeeExitPostExitContact` | `UserController` | `UpdateEmployeeExitPostExitContact` | `_employeeService.UpdateEmployeeExitPostExitContact()` |
| 56 | `POST` | `api/User/AcknowledgeEmployeeExit` | `UserController` | `AcknowledgeEmployeeExit` | `_employeeService.AcknowledgeEmployeeExit()` |
| 57 | `POST` | `api/User/EmployeeExitFormerEmployeeRequest` | `UserController` | `CreateEmployeeExitFormerEmployeeRequest` | `_employeeService.CreateEmployeeExitFormerEmployeeRequest()` |
| 58 | `POST` | `api/User/EmployeeExitFormerEmployeeRequestResolve` | `UserController` | `ResolveEmployeeExitFormerEmployeeRequest` | `_employeeService.ResolveEmployeeExitFormerEmployeeRequest()` |
| 59 | `POST` | `api/User/EmployeeExitFormerEmployeePortalInvite` | `UserController` | `CreateFormerEmployeePortalInvite` | `_formerEmployeePortalService.CreateInvite()` |
| 60 | `POST` | `api/User/EmployeeExitFormerEmployeePortalRevoke` | `UserController` | `RevokeFormerEmployeePortalAccess` | `_formerEmployeePortalService.RevokeAccess()` |
| 61 | `POST` | `api/User/EmployeeExitFormerEmployeePortalAccess` | `UserController` | `SetFormerEmployeePortalAccess` | `_formerEmployeePortalService.SetAccess()` |
| 62 | `POST` | `api/User/CompanyFormerEmployeePortalAccess` | `UserController` | `SetCompanyFormerEmployeePortalAccess` | `_formerEmployeePortalService.SetCompanyAccess()` |
| 63 | `POST` | `api/User/RejoinEmployee` | `UserController` | `RejoinEmployee` | `_employeeService.RejoinEmployee()` |
| 64 | `POST` | `api/User/ExceptionalCompleteEmployeeExit` | `UserController` | `ExceptionalCompleteEmployeeExit` | `_employeeService.ExceptionalCompleteEmployeeExit()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/public/former-employee/redeem

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.Redeem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `Redeem()`
- Approximate source line at generation: 16
- Direct delegated call found: `portal.RedeemInvite()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/redeem`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] FormerEmployeePortalTokenRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("redeem")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] FormerEmployeePortalTokenRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/redeem`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.Redeem()` receives the bound values.
4. The action delegates to `portal.RedeemInvite()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RedeemInvite()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:110`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED, FORMER_PORTAL_ACCESS_DISABLED, FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `RedeemInvite()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:10`.
- Static signals in the inspected method window: .
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

## 2. POST api/public/former-employee/otp/request

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.RequestOtp()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `RequestOtp()`
- Approximate source line at generation: 24
- Direct delegated call found: `portal.RequestOtp()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/otp/request`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] FormerEmployeePortalOtpRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("otp/request")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] FormerEmployeePortalOtpRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/otp/request`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.RequestOtp()` receives the bound values.
4. The action delegates to `portal.RequestOtp()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RequestOtp()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:128`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED, FORMER_PORTAL_ACCESS_DISABLED, FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `RequestOtp()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:11`.
- Static signals in the inspected method window: .
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

## 3. POST api/public/former-employee/otp/verify

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.VerifyOtp()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `VerifyOtp()`
- Approximate source line at generation: 28
- Direct delegated call found: `portal.VerifyOtp()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/otp/verify`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] FormerEmployeePortalOtpVerifyRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("otp/verify")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] FormerEmployeePortalOtpVerifyRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/otp/verify`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.VerifyOtp()` receives the bound values.
4. The action delegates to `portal.VerifyOtp()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `VerifyOtp()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:175`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED, FORMER_PORTAL_ACCESS_DISABLED, FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `VerifyOtp()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:12`.
- Static signals in the inspected method window: .
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

## 4. POST api/public/former-employee/logout

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.Logout()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `Logout()`
- Approximate source line at generation: 35
- Direct delegated call found: `portal.Logout()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/logout`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("logout")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/logout`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.Logout()` receives the bound values.
4. The action delegates to `portal.Logout()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Logout()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:189`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `Logout()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:13`.
- Static signals in the inspected method window: .
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

## 5. POST api/public/former-employee/verify-contact

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.VerifyContact()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `VerifyContact()`
- Approximate source line at generation: 39
- Direct delegated call found: `portal.VerifyPostExitContact()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/verify-contact`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] FormerEmployeePortalTokenRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("verify-contact")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] FormerEmployeePortalTokenRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/verify-contact`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.VerifyContact()` receives the bound values.
4. The action delegates to `portal.VerifyPostExitContact()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `VerifyPostExitContact()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:227`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `VerifyPostExitContact()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:16`.
- Static signals in the inspected method window: .
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

## 6. GET api/public/former-employee/case

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.GetCase()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `GetCase()`
- Approximate source line at generation: 46
- Direct delegated call found: `portal.GetCase()`

### HTTP contract

- Method: `GET`
- Route: `api/public/former-employee/case`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpGet("case")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/former-employee/case`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.GetCase()` receives the bound values.
4. The action delegates to `portal.GetCase()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetCase()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:259`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Verified, Resigned, Terminated.
- Method match: `GetCase()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:17`.
- Static signals in the inspected method window: .
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

## 7. POST api/public/former-employee/requests

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.CreateRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `CreateRequest()`
- Approximate source line at generation: 54
- Direct delegated call found: `portal.CreateRequest()`

### HTTP contract

- Method: `POST`
- Route: `api/public/former-employee/requests`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromBody] FormerEmployeePortalRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpPost("requests")] [EnableRateLimiting("account-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] FormerEmployeePortalRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/former-employee/requests`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.CreateRequest()` receives the bound values.
4. The action delegates to `portal.CreateRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateRequest()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:285`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `CreateRequest()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:18`.
- Static signals in the inspected method window: .
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

## 8. GET api/public/former-employee/documents/{evidenceId}

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.DownloadDocument()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `DownloadDocument()`
- Approximate source line at generation: 61
- Direct delegated call found: `portal.GetDocument()`

### HTTP contract

- Method: `GET`
- Route: `api/public/former-employee/documents/{evidenceId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string evidenceId`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpGet("documents/{evidenceId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string evidenceId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/former-employee/documents/{evidenceId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.DownloadDocument()` receives the bound values.
4. The action delegates to `portal.GetDocument()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetDocument()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:300`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Verified, Resigned, Terminated.
- Method match: `GetDocument()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:19`.
- Static signals in the inspected method window: .
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

## 9. GET api/public/former-employee/documents/{evidenceId}/preview

### Purpose

This endpoint is implemented by `PublicFormerEmployeePortalController.PreviewDocument()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicFormerEmployeePortalController.cs`
- Controller: `PublicFormerEmployeePortalController`
- Action: `PreviewDocument()`
- Approximate source line at generation: 70
- Direct delegated call found: `portal.GetDocument()`

### HTTP contract

- Method: `GET`
- Route: `api/public/former-employee/documents/{evidenceId}/preview`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string evidenceId`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/former-employee")] [HttpGet("documents/{evidenceId}/preview")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string evidenceId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/former-employee/documents/{evidenceId}/preview`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicFormerEmployeePortalController.PreviewDocument()` receives the bound values.
4. The action delegates to `portal.GetDocument()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetDocument()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:300`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Verified, Resigned, Terminated.
- Method match: `GetDocument()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:19`.
- Static signals in the inspected method window: .
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

## 10. POST api/User/InitiateEmployeeExit

### Purpose

This endpoint is implemented by `UserController.InitiateEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `InitiateEmployeeExit()`
- Approximate source line at generation: 214
- Direct delegated call found: `_employeeService.InitiateEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/InitiateEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] InitiateEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("InitiateEmployeeExit")] [HttpPost] [ModulePermission(AppModule.Employees, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] InitiateEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/InitiateEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.InitiateEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.InitiateEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `InitiateEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:950`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY, EXIT_REVIEW_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active, Pending.
- Method match: `InitiateEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:25`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 11. POST api/User/SubmitOwnResignation

### Purpose

This endpoint is implemented by `UserController.SubmitOwnResignation()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SubmitOwnResignation()`
- Approximate source line at generation: 221
- Direct delegated call found: `_employeeService.SubmitOwnResignation()`

### HTTP contract

- Method: `POST`
- Route: `api/User/SubmitOwnResignation`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] InitiateEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("SubmitOwnResignation")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] InitiateEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/SubmitOwnResignation`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SubmitOwnResignation()` receives the bound values.
4. The action delegates to `_employeeService.SubmitOwnResignation()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SubmitOwnResignation()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:953`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, SELF_EXIT_ONLY, SELF_EXIT_NOT_ALLOWED, SELF_SUBMISSION_MUST_BE_RESIGNATION, EMPLOYEE_NOT_FOUND, EXIT_ALREADY_OPEN_OR_COMPLETED, EXIT_LAST_WORKING_DATE_IN_PAST, EXIT_BEFORE_JOINING, OFFBOARDING_DISABLED_FOR_COMPANY, EXIT_REVIEW_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Active, Pending, Rejected.
- Method match: `SubmitOwnResignation()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:26`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 12. GET api/User/EmployeeExit/{employeeUserId}

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitCase()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitCase()`
- Approximate source line at generation: 226
- Direct delegated call found: `_employeeService.GetEmployeeExitCase()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExit/{employeeUserId}`
- Return declaration: `Task<Result<EmployeeExitCaseDto>>`
- Parameters exactly as declared: `string employeeUserId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExit/{employeeUserId}")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeUserId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExit/{employeeUserId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitCase()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitCase()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitCase()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2336`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN.
- Explicit workflow/status literals found: Withdrawn, Approved, NoticePeriod, Verified, Completed, Submitted.
- Method match: `GetEmployeeExitCase()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:72`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitCaseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 13. GET api/User/EmployeeExitHistory/{employeeUserId}

### Purpose

This endpoint is implemented by `UserController.GetOwnEmployeeExitHistory()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetOwnEmployeeExitHistory()`
- Approximate source line at generation: 231
- Direct delegated call found: `_employeeService.GetOwnEmployeeExitHistory()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitHistory/{employeeUserId}`
- Return declaration: `Task<Result<List<EmployeeExitHistoryDto>>>`
- Parameters exactly as declared: `string employeeUserId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitHistory/{employeeUserId}")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeUserId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitHistory/{employeeUserId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetOwnEmployeeExitHistory()` receives the bound values.
4. The action delegates to `_employeeService.GetOwnEmployeeExitHistory()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetOwnEmployeeExitHistory()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2398`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Withdrawn, Resigned, Terminated, Approved, NoticePeriod, Pending.
- Method match: `GetOwnEmployeeExitHistory()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:73`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<List<EmployeeExitHistoryDto>>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 14. GET api/User/EmployeeExitManagement/{employeeUserId}

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitCaseForManagement()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitCaseForManagement()`
- Approximate source line at generation: 236
- Direct delegated call found: `_employeeService.GetEmployeeExitCaseForManagement()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitManagement/{employeeUserId}`
- Return declaration: `Task<Result<EmployeeExitCaseDto>>`
- Parameters exactly as declared: `string employeeUserId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitManagement/{employeeUserId}")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeUserId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitManagement/{employeeUserId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitCaseForManagement()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitCaseForManagement()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitCaseForManagement()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2425`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_COMPLETION_FORBIDDEN, EXIT_REQUEST_INVALID, OPEN_EXIT_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED.
- Explicit workflow/status literals found: Resigned, Terminated, Withdrawn, Approved, NoticePeriod, Pending.
- Method match: `GetEmployeeExitCaseForManagement()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:74`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitCaseDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. POST api/User/CompleteEmployeeExit

### Purpose

This endpoint is implemented by `UserController.CompleteEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CompleteEmployeeExit()`
- Approximate source line at generation: 241
- Direct delegated call found: `_employeeService.CompleteEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/CompleteEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CompleteEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CompleteEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CompleteEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/CompleteEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CompleteEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.CompleteEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompleteEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2328`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN.
- Explicit workflow/status literals found: Withdrawn, Approved, NoticePeriod, Verified, Completed, Submitted.
- Method match: `CompleteEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:27`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 16. POST api/User/ReviewEmployeeExit

### Purpose

This endpoint is implemented by `UserController.ReviewEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ReviewEmployeeExit()`
- Approximate source line at generation: 246
- Direct delegated call found: `_employeeService.ReviewEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ReviewEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] ReviewEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ReviewEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ReviewEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ReviewEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ReviewEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.ReviewEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ReviewEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1099`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REVIEW_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, SELF_EXIT_REVIEW_FORBIDDEN, EXIT_MANAGER_REVIEW_FORBIDDEN, EXIT_HR_REVIEW_FORBIDDEN, EXIT_DISCUSSION_DECISION_FORBIDDEN, EXIT_REVIEW_NOT_ALLOWED_IN_STATE, EXIT_NOTICE_ADJUSTMENT_DECISION_PENDING, EXIT_DISCUSSION_DISABLED_BY_POLICY, EXIT_DISCUSSION_NOTE_REQUIRED, EXIT_JOINT_DISCUSSION_PARTICIPANT_REQUIRED, EXIT_JOINT_DISCUSSION_PARTICIPANT_INVALID.
- Explicit workflow/status literals found: Pending, Rejected, NoticePeriod, Withdrawn, Cancelled, Active.
- Method match: `ReviewEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:28`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 17. POST api/User/RespondToEmployeeExitDiscussion

### Purpose

This endpoint is implemented by `UserController.RespondToEmployeeExitDiscussion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RespondToEmployeeExitDiscussion()`
- Approximate source line at generation: 251
- Direct delegated call found: `_employeeService.RespondToEmployeeExitDiscussion()`

### HTTP contract

- Method: `POST`
- Route: `api/User/RespondToEmployeeExitDiscussion`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RespondToEmployeeExitDiscussionRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("RespondToEmployeeExitDiscussion")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RespondToEmployeeExitDiscussionRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/RespondToEmployeeExitDiscussion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RespondToEmployeeExitDiscussion()` receives the bound values.
4. The action delegates to `_employeeService.RespondToEmployeeExitDiscussion()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RespondToEmployeeExitDiscussion()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1175`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_DISCUSSION_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_DISCUSSION_RESPONSE_FORBIDDEN, EXIT_DISCUSSION_RESPONSE_NOTE_REQUIRED, EXIT_JOINT_DISCUSSION_INVALID, EXIT_JOINT_DISCUSSION_FORBIDDEN, EXIT_CANCEL_INVALID, EXIT_WITHDRAW_FORBIDDEN, EXIT_CANCEL_FORBIDDEN, EXIT_WITHDRAWAL_ALREADY_PENDING.
- Explicit workflow/status literals found: Withdrawn, Cancelled, NoticePeriod, Pending, Active, Approved.
- Method match: `RespondToEmployeeExitDiscussion()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:29`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 18. POST api/User/RespondToEmployeeExitJointDiscussion

### Purpose

This endpoint is implemented by `UserController.RespondToEmployeeExitJointDiscussion()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RespondToEmployeeExitJointDiscussion()`
- Approximate source line at generation: 256
- Direct delegated call found: `_employeeService.RespondToEmployeeExitJointDiscussion()`

### HTTP contract

- Method: `POST`
- Route: `api/User/RespondToEmployeeExitJointDiscussion`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RespondToEmployeeExitJointDiscussionRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("RespondToEmployeeExitJointDiscussion")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RespondToEmployeeExitJointDiscussionRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/RespondToEmployeeExitJointDiscussion`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RespondToEmployeeExitJointDiscussion()` receives the bound values.
4. The action delegates to `_employeeService.RespondToEmployeeExitJointDiscussion()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RespondToEmployeeExitJointDiscussion()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1209`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_JOINT_DISCUSSION_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_JOINT_DISCUSSION_FORBIDDEN, EXIT_CANCEL_INVALID, EXIT_WITHDRAW_FORBIDDEN, EXIT_CANCEL_FORBIDDEN, EXIT_WITHDRAWAL_ALREADY_PENDING, EXIT_WITHDRAW_NOT_ALLOWED_IN_STATE, EXIT_WITHDRAWAL_INVALID, EXIT_WITHDRAWAL_REVIEW_FORBIDDEN.
- Explicit workflow/status literals found: Withdrawn, Cancelled, NoticePeriod, Pending, Active, Approved.
- Method match: `RespondToEmployeeExitJointDiscussion()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:30`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 19. POST api/User/WithdrawOwnEmployeeExit

### Purpose

This endpoint is implemented by `UserController.WithdrawOwnEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `WithdrawOwnEmployeeExit()`
- Approximate source line at generation: 261
- Direct delegated call found: `_employeeService.WithdrawOwnEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/WithdrawOwnEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CancelEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("WithdrawOwnEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CancelEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/WithdrawOwnEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.WithdrawOwnEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.WithdrawOwnEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `WithdrawOwnEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1237`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CANCEL_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_WITHDRAW_FORBIDDEN, EXIT_CANCEL_FORBIDDEN, EXIT_WITHDRAWAL_ALREADY_PENDING, EXIT_WITHDRAW_NOT_ALLOWED_IN_STATE, EXIT_WITHDRAWAL_INVALID, EXIT_WITHDRAWAL_REVIEW_FORBIDDEN, EMPLOYEE_NOT_FOUND_AFTER_WITHDRAWAL.
- Explicit workflow/status literals found: Withdrawn, Cancelled, NoticePeriod, Pending, Active, Approved.
- Method match: `WithdrawOwnEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:34`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 20. POST api/User/DecideEmployeeExitWithdrawal

### Purpose

This endpoint is implemented by `UserController.DecideEmployeeExitWithdrawal()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DecideEmployeeExitWithdrawal()`
- Approximate source line at generation: 266
- Direct delegated call found: `_employeeService.DecideEmployeeExitWithdrawal()`

### HTTP contract

- Method: `POST`
- Route: `api/User/DecideEmployeeExitWithdrawal`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] DecideEmployeeExitWithdrawalRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DecideEmployeeExitWithdrawal")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] DecideEmployeeExitWithdrawalRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/DecideEmployeeExitWithdrawal`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DecideEmployeeExitWithdrawal()` receives the bound values.
4. The action delegates to `_employeeService.DecideEmployeeExitWithdrawal()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DecideEmployeeExitWithdrawal()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1291`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_WITHDRAWAL_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_WITHDRAWAL_REVIEW_FORBIDDEN, EMPLOYEE_NOT_FOUND_AFTER_WITHDRAWAL, EXIT_OVERRIDE_FORBIDDEN, EXIT_DATE_INVALID, EXIT_DATE_CHANGE_NOT_ALLOWED, EXIT_BEFORE_JOINING, EXIT_NOTICE_PERIOD_NOT_MET, EXIT_NOTICE_OVERRIDE_REASON_REQUIRED.
- Explicit workflow/status literals found: Pending, Approved, Withdrawn, Active, NoticePeriod.
- Method match: `DecideEmployeeExitWithdrawal()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:35`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 21. POST api/User/CancelEmployeeExit

### Purpose

This endpoint is implemented by `UserController.CancelEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CancelEmployeeExit()`
- Approximate source line at generation: 271
- Direct delegated call found: `_employeeService.CancelEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/CancelEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CancelEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CancelEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CancelEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/CancelEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CancelEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.CancelEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CancelEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1240`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CANCEL_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_WITHDRAW_FORBIDDEN, EXIT_CANCEL_FORBIDDEN, EXIT_WITHDRAWAL_ALREADY_PENDING, EXIT_WITHDRAW_NOT_ALLOWED_IN_STATE, EXIT_WITHDRAWAL_INVALID, EXIT_WITHDRAWAL_REVIEW_FORBIDDEN, EMPLOYEE_NOT_FOUND_AFTER_WITHDRAWAL.
- Explicit workflow/status literals found: Cancelled, NoticePeriod, Pending, Withdrawn, Active, Approved.
- Method match: `CancelEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:36`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 22. POST api/User/UpdateEmployeeExitDate

### Purpose

This endpoint is implemented by `UserController.UpdateEmployeeExitDate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpdateEmployeeExitDate()`
- Approximate source line at generation: 276
- Direct delegated call found: `_employeeService.UpdateEmployeeExitDate()`

### HTTP contract

- Method: `POST`
- Route: `api/User/UpdateEmployeeExitDate`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateEmployeeExitDateRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateEmployeeExitDate")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateEmployeeExitDateRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/UpdateEmployeeExitDate`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpdateEmployeeExitDate()` receives the bound values.
4. The action delegates to `_employeeService.UpdateEmployeeExitDate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateEmployeeExitDate()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1451`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_OVERRIDE_FORBIDDEN, EXIT_DATE_INVALID, EXIT_DATE_CHANGE_NOT_ALLOWED, EXIT_BEFORE_JOINING, EXIT_NOTICE_PERIOD_NOT_MET, EXIT_NOTICE_OVERRIDE_REASON_REQUIRED, EXIT_NOTICE_ADJUSTMENT_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_NOTICE_ADJUSTMENT_FORBIDDEN, EXIT_NOTICE_ADJUSTMENT_DATE_INVALID, EXIT_NOTICE_ADJUSTMENT_ALREADY_PENDING, EXIT_EMPLOYEE_REQUEST_ALREADY_PENDING.
- Explicit workflow/status literals found: NoticePeriod, Pending, Approved.
- Method match: `UpdateEmployeeExitDate()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:37`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 23. POST api/User/RequestEmployeeExitNoticeAdjustment

### Purpose

This endpoint is implemented by `UserController.RequestEmployeeExitNoticeAdjustment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RequestEmployeeExitNoticeAdjustment()`
- Approximate source line at generation: 281
- Direct delegated call found: `_employeeService.RequestEmployeeExitNoticeAdjustment()`

### HTTP contract

- Method: `POST`
- Route: `api/User/RequestEmployeeExitNoticeAdjustment`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RequestEmployeeExitNoticeAdjustmentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("RequestEmployeeExitNoticeAdjustment")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RequestEmployeeExitNoticeAdjustmentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/RequestEmployeeExitNoticeAdjustment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RequestEmployeeExitNoticeAdjustment()` receives the bound values.
4. The action delegates to `_employeeService.RequestEmployeeExitNoticeAdjustment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RequestEmployeeExitNoticeAdjustment()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1494`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_NOTICE_ADJUSTMENT_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_NOTICE_ADJUSTMENT_FORBIDDEN, EXIT_NOTICE_ADJUSTMENT_DATE_INVALID, EXIT_NOTICE_ADJUSTMENT_ALREADY_PENDING, EXIT_EMPLOYEE_REQUEST_ALREADY_PENDING, EXIT_NOTICE_ADJUSTMENT_DECISION_RESERVED_FOR_HR, EXIT_NOTICE_ADJUSTMENT_TERMS_INVALID, EXIT_NOTICE_EXTENSION_DATE_INVALID.
- Explicit workflow/status literals found: NoticePeriod, Pending, Approved.
- Method match: `RequestEmployeeExitNoticeAdjustment()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:38`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 24. POST api/User/DecideEmployeeExitNoticeAdjustment

### Purpose

This endpoint is implemented by `UserController.DecideEmployeeExitNoticeAdjustment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DecideEmployeeExitNoticeAdjustment()`
- Approximate source line at generation: 286
- Direct delegated call found: `_employeeService.DecideEmployeeExitNoticeAdjustment()`

### HTTP contract

- Method: `POST`
- Route: `api/User/DecideEmployeeExitNoticeAdjustment`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] DecideEmployeeExitNoticeAdjustmentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DecideEmployeeExitNoticeAdjustment")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] DecideEmployeeExitNoticeAdjustmentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/DecideEmployeeExitNoticeAdjustment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DecideEmployeeExitNoticeAdjustment()` receives the bound values.
4. The action delegates to `_employeeService.DecideEmployeeExitNoticeAdjustment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DecideEmployeeExitNoticeAdjustment()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1525`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_NOTICE_ADJUSTMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_NOTICE_ADJUSTMENT_TERMS_INVALID, EXIT_NOTICE_EXTENSION_DATE_INVALID, EXIT_EARLY_RELEASE_DATE_INVALID, NOTICE_ADJUSTMENT_DATE_IN_PAST, NOTICE_EXTENSION_EXCEEDS_MAXIMUM, EXIT_NOTICE_PARTIAL_WAIVER_DAYS_INVALID, EXIT_NOTICE_PAYROLL_BASIS_NOT_FOUND, EMPLOYEE_NOT_FOUND_AFTER_NOTICE_ADJUSTMENT, EXIT_NOTICE_OFFER_INVALID.
- Explicit workflow/status literals found: Pending, Approved, NoticePeriod, Cancelled.
- Method match: `DecideEmployeeExitNoticeAdjustment()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:39`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 25. POST api/User/DecideOwnEmployeeExitNoticeOffer

### Purpose

This endpoint is implemented by `UserController.DecideOwnEmployeeExitNoticeOffer()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DecideOwnEmployeeExitNoticeOffer()`
- Approximate source line at generation: 291
- Direct delegated call found: `_employeeService.DecideOwnEmployeeExitNoticeOffer()`

### HTTP contract

- Method: `POST`
- Route: `api/User/DecideOwnEmployeeExitNoticeOffer`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] DecideOwnEmployeeExitNoticeOfferRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DecideOwnEmployeeExitNoticeOffer")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] DecideOwnEmployeeExitNoticeOfferRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/DecideOwnEmployeeExitNoticeOffer`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DecideOwnEmployeeExitNoticeOffer()` receives the bound values.
4. The action delegates to `_employeeService.DecideOwnEmployeeExitNoticeOffer()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DecideOwnEmployeeExitNoticeOffer()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1624`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_NOTICE_OFFER_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EMPLOYEE_NOT_FOUND_AFTER_NOTICE_ADJUSTMENT, EXIT_NOTICE_ADJUSTMENT_INVALID, NOTICE_ADJUSTMENT_NOT_FOUND, NOTICE_ADJUSTMENT_CANNOT_CANCEL_IN_STATE, EXIT_NOTICE_ADJUSTMENT_CANCEL_FORBIDDEN, EXIT_POLICY_INVALID, EXIT_POLICY_FORBIDDEN, EXIT_POLICY_SCOPE_MUST_BE_COMPANY_DEPARTMENT_OR_ROLE.
- Explicit workflow/status literals found: Approved, NoticePeriod, Pending, Cancelled.
- Method match: `DecideOwnEmployeeExitNoticeOffer()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:40`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 26. POST api/User/EmployeeExitPolicy

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitPolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitPolicy()`
- Approximate source line at generation: 296
- Direct delegated call found: `_employeeService.UpsertEmployeeExitPolicy()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitPolicy`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitPolicyRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitPolicy")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitPolicyRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitPolicy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitPolicy()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitPolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitPolicy()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1705`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_POLICY_INVALID, EXIT_POLICY_FORBIDDEN, EXIT_POLICY_SCOPE_MUST_BE_COMPANY_DEPARTMENT_OR_ROLE, EXIT_POLICY_DEPARTMENT_NOT_FOUND, EXIT_POLICY_ROLE_NOT_FOUND, EXIT_POLICY_VERSION_CONFLICT, EXIT_POLICY_DEPARTMENT_ALREADY_CONFIGURED, EXIT_POLICY_DEPARTMENT_GROUP_CONFLICT, EXIT_POLICY_DEPARTMENT_CONFIGURATION_DIFFERS, EXIT_POLICY_CONSOLIDATION_PARTIAL, EXIT_POLICY_NOT_FOUND.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved.
- Method match: `UpsertEmployeeExitPolicy()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 27. GET api/User/EmployeeExitPolicy/{policyId}

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitPolicy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitPolicy()`
- Approximate source line at generation: 301
- Direct delegated call found: `_employeeService.GetEmployeeExitPolicy()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitPolicy/{policyId}`
- Return declaration: `Task<Result<EmployeeExitPolicy>>`
- Parameters exactly as declared: `string policyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitPolicy/{policyId}")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string policyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitPolicy/{policyId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitPolicy()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitPolicy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitPolicy()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1768`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_POLICY_NOT_FOUND, EXIT_POLICY_FORBIDDEN, EXIT_CLEARANCE_TEMPLATE_NOT_FOUND, EXIT_CLEARANCE_TEMPLATE_INVALID, EXIT_CLEARANCE_TEMPLATE_GATE_INVALID, EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `GetEmployeeExitPolicy()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:44`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitPolicy>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 28. GET api/User/EmployeeExitClearanceTemplate/{templateId}

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitClearanceTemplate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitClearanceTemplate()`
- Approximate source line at generation: 306
- Direct delegated call found: `_employeeService.GetEmployeeExitClearanceTemplate()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitClearanceTemplate/{templateId}`
- Return declaration: `Task<Result<EmployeeExitClearanceTemplate>>`
- Parameters exactly as declared: `string templateId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitClearanceTemplate/{templateId}")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string templateId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitClearanceTemplate/{templateId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitClearanceTemplate()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitClearanceTemplate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1778`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_TEMPLATE_NOT_FOUND, EXIT_POLICY_FORBIDDEN, EXIT_CLEARANCE_TEMPLATE_INVALID, EXIT_CLEARANCE_TEMPLATE_GATE_INVALID, EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID, EXIT_CLEARANCE_TEMPLATE_EFFECTIVE_RANGE_INVALID, EXIT_CLEARANCE_TEMPLATE_DEPARTMENT_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_INVALID.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `GetEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:45`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitClearanceTemplate>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 29. GET api/User/EmployeeExitPolicies

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitPolicies()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitPolicies()`
- Approximate source line at generation: 311
- Direct delegated call found: `_employeeService.GetEmployeeExitPolicies()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitPolicies`
- Return declaration: `Task<Result<EmployeeExitPolicy>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitPolicies")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitPolicies`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitPolicies()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitPolicies()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitPolicies()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1788`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_POLICY_FORBIDDEN, EXIT_CLEARANCE_TEMPLATE_INVALID, EXIT_CLEARANCE_TEMPLATE_GATE_INVALID, EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID, EXIT_CLEARANCE_TEMPLATE_EFFECTIVE_RANGE_INVALID, EXIT_CLEARANCE_TEMPLATE_DEPARTMENT_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_ALREADY_EXISTS, EXIT_CLEARANCE_TEMPLATE_VERSION_CONFLICT, EXIT_CLEARANCE_TEMPLATE_NOT_FOUND.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `GetEmployeeExitPolicies()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:46`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitPolicy>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 30. GET api/User/EmployeeExitClearanceTemplates

### Purpose

This endpoint is implemented by `UserController.GetEmployeeExitClearanceTemplates()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GetEmployeeExitClearanceTemplates()`
- Approximate source line at generation: 316
- Direct delegated call found: `_employeeService.GetEmployeeExitClearanceTemplates()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitClearanceTemplates`
- Return declaration: `Task<Result<EmployeeExitClearanceTemplate>>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitClearanceTemplates")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitClearanceTemplates`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GetEmployeeExitClearanceTemplates()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitClearanceTemplates()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitClearanceTemplates()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1797`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_POLICY_FORBIDDEN, EXIT_CLEARANCE_TEMPLATE_INVALID, EXIT_CLEARANCE_TEMPLATE_GATE_INVALID, EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID, EXIT_CLEARANCE_TEMPLATE_EFFECTIVE_RANGE_INVALID, EXIT_CLEARANCE_TEMPLATE_DEPARTMENT_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_ALREADY_EXISTS, EXIT_CLEARANCE_TEMPLATE_VERSION_CONFLICT, EXIT_CLEARANCE_TEMPLATE_NOT_FOUND.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `GetEmployeeExitClearanceTemplates()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:47`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<EmployeeExitClearanceTemplate>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 31. POST api/User/EmployeeExitClearanceTemplate

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitClearanceTemplate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitClearanceTemplate()`
- Approximate source line at generation: 321
- Direct delegated call found: `_employeeService.UpsertEmployeeExitClearanceTemplate()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitClearanceTemplate`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitClearanceTemplateRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitClearanceTemplate")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitClearanceTemplateRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitClearanceTemplate`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitClearanceTemplate()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitClearanceTemplate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1806`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_TEMPLATE_INVALID, EXIT_POLICY_FORBIDDEN, EXIT_CLEARANCE_TEMPLATE_GATE_INVALID, EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID, EXIT_CLEARANCE_TEMPLATE_EFFECTIVE_RANGE_INVALID, EXIT_CLEARANCE_TEMPLATE_DEPARTMENT_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_INVALID, EXIT_CLEARANCE_TEMPLATE_KEY_ALREADY_EXISTS, EXIT_CLEARANCE_TEMPLATE_VERSION_CONFLICT, EXIT_CLEARANCE_TEMPLATE_NOT_FOUND.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `UpsertEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:42`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 32. DELETE api/User/EmployeeExitClearanceTemplate/{templateId}

### Purpose

This endpoint is implemented by `UserController.DeleteEmployeeExitClearanceTemplate()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DeleteEmployeeExitClearanceTemplate()`
- Approximate source line at generation: 326
- Direct delegated call found: `_employeeService.DeleteEmployeeExitClearanceTemplate()`

### HTTP contract

- Method: `DELETE`
- Route: `api/User/EmployeeExitClearanceTemplate/{templateId}`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `string templateId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitClearanceTemplate/{templateId}")] [HttpDelete]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string templateId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/User/EmployeeExitClearanceTemplate/{templateId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DeleteEmployeeExitClearanceTemplate()` receives the bound values.
4. The action delegates to `_employeeService.DeleteEmployeeExitClearanceTemplate()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1839`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_TEMPLATE_NOT_FOUND, EXIT_POLICY_FORBIDDEN, FINAL_SETTLEMENT_INVALID, FINAL_SETTLEMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED, FINAL_SETTLEMENT_NOT_CALCULATED, FINAL_SETTLEMENT_PAYMENT_FORBIDDEN, FINAL_SETTLEMENT_NOT_APPROVED.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `DeleteEmployeeExitClearanceTemplate()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:43`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 33. POST api/User/ApproveEmployeeFinalSettlement

### Purpose

This endpoint is implemented by `UserController.ApproveEmployeeFinalSettlement()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ApproveEmployeeFinalSettlement()`
- Approximate source line at generation: 331
- Direct delegated call found: `_employeeService.ApproveEmployeeFinalSettlement()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ApproveEmployeeFinalSettlement`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] ApproveEmployeeFinalSettlementRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ApproveEmployeeFinalSettlement")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ApproveEmployeeFinalSettlementRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ApproveEmployeeFinalSettlement`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ApproveEmployeeFinalSettlement()` receives the bound values.
4. The action delegates to `_employeeService.ApproveEmployeeFinalSettlement()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ApproveEmployeeFinalSettlement()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1865`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FINAL_SETTLEMENT_INVALID, FINAL_SETTLEMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED, FINAL_SETTLEMENT_NOT_CALCULATED, FINAL_SETTLEMENT_PAYMENT_FORBIDDEN, FINAL_SETTLEMENT_NOT_APPROVED, FINAL_PAYROLL_NOT_CONFIGURED, EMPLOYEE_NOT_FOUND.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `ApproveEmployeeFinalSettlement()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:48`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 34. POST api/User/UpdateEmployeeFinalSettlementPayment

### Purpose

This endpoint is implemented by `UserController.UpdateEmployeeFinalSettlementPayment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpdateEmployeeFinalSettlementPayment()`
- Approximate source line at generation: 336
- Direct delegated call found: `_employeeService.UpdateEmployeeFinalSettlementPayment()`

### HTTP contract

- Method: `POST`
- Route: `api/User/UpdateEmployeeFinalSettlementPayment`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateEmployeeFinalSettlementPaymentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateEmployeeFinalSettlementPayment")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateEmployeeFinalSettlementPaymentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/UpdateEmployeeFinalSettlementPayment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpdateEmployeeFinalSettlementPayment()` receives the bound values.
4. The action delegates to `_employeeService.UpdateEmployeeFinalSettlementPayment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateEmployeeFinalSettlementPayment()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1884`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FINAL_SETTLEMENT_PAYMENT_FORBIDDEN, FINAL_SETTLEMENT_NOT_APPROVED, FINAL_SETTLEMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED, FINAL_PAYROLL_NOT_CONFIGURED, EMPLOYEE_NOT_FOUND, FINAL_MONTH_PAYROLL_NOT_AVAILABLE, EXIT_DOCUMENT_INVALID_OR_FORBIDDEN.
- Explicit workflow/status literals found: Paid, Approved, NoticePeriod, Resigned, Terminated.
- Method match: `UpdateEmployeeFinalSettlementPayment()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:49`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 35. POST api/User/CalculateEmployeeFinalSettlement

### Purpose

This endpoint is implemented by `UserController.CalculateEmployeeFinalSettlement()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CalculateEmployeeFinalSettlement()`
- Approximate source line at generation: 340
- Direct delegated call found: `_employeeService.CalculateEmployeeFinalSettlement()`

### HTTP contract

- Method: `POST`
- Route: `api/User/CalculateEmployeeFinalSettlement`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CalculateEmployeeFinalSettlementRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CalculateEmployeeFinalSettlement")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CalculateEmployeeFinalSettlementRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/CalculateEmployeeFinalSettlement`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CalculateEmployeeFinalSettlement()` receives the bound values.
4. The action delegates to `_employeeService.CalculateEmployeeFinalSettlement()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CalculateEmployeeFinalSettlement()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1898`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FINAL_SETTLEMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED, FINAL_PAYROLL_NOT_CONFIGURED, EMPLOYEE_NOT_FOUND, FINAL_MONTH_PAYROLL_NOT_AVAILABLE, FINAL_SETTLEMENT_NOT_APPROVED, EXIT_DOCUMENT_INVALID_OR_FORBIDDEN, EXIT_DOCUMENT_NOT_AVAILABLE, FORMER_DOCUMENT_ISSUE_FORBIDDEN.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid.
- Method match: `CalculateEmployeeFinalSettlement()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:50`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 36. POST api/User/MarkEmployeeFinalSettlementPaid

### Purpose

This endpoint is implemented by `UserController.MarkEmployeeFinalSettlementPaid()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `MarkEmployeeFinalSettlementPaid()`
- Approximate source line at generation: 345
- Direct delegated call found: `_employeeService.MarkEmployeeFinalSettlementPaid()`

### HTTP contract

- Method: `POST`
- Route: `api/User/MarkEmployeeFinalSettlementPaid`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] MarkEmployeeFinalSettlementPaidRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("MarkEmployeeFinalSettlementPaid")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] MarkEmployeeFinalSettlementPaidRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/MarkEmployeeFinalSettlementPaid`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.MarkEmployeeFinalSettlementPaid()` receives the bound values.
4. The action delegates to `_employeeService.MarkEmployeeFinalSettlementPaid()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `MarkEmployeeFinalSettlementPaid()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1938`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FINAL_SETTLEMENT_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, FINAL_SETTLEMENT_NOT_APPROVED, EXIT_DOCUMENT_INVALID_OR_FORBIDDEN, EXIT_DOCUMENT_NOT_AVAILABLE, FORMER_DOCUMENT_ISSUE_FORBIDDEN, FORMER_DOCUMENT_NOT_READY, POST_EXIT_CONTACT_VERIFICATION_REQUIRED, FORMER_DOCUMENT_ALREADY_ISSUED, EXIT_CLEARANCE_INVALID, EXIT_CLEARANCE_NOT_ACTIONABLE.
- Explicit workflow/status literals found: NoticePeriod, Resigned, Terminated, Approved, Paid, Verified, Issued, Pending.
- Method match: `MarkEmployeeFinalSettlementPaid()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:51`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 37. POST api/User/RecordEmployeeExitInterview

### Purpose

This endpoint is implemented by `UserController.RecordEmployeeExitInterview()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RecordEmployeeExitInterview()`
- Approximate source line at generation: 350
- Direct delegated call found: `_employeeService.RecordEmployeeExitInterview()`

### HTTP contract

- Method: `POST`
- Route: `api/User/RecordEmployeeExitInterview`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RecordEmployeeExitInterviewRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("RecordEmployeeExitInterview")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RecordEmployeeExitInterviewRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/RecordEmployeeExitInterview`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RecordEmployeeExitInterview()` receives the bound values.
4. The action delegates to `_employeeService.RecordEmployeeExitInterview()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RecordEmployeeExitInterview()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2276`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_INTERVIEW_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_REASSIGNMENT_INVALID_OR_FORBIDDEN, EXIT_REASSIGNMENT_OWNER_INVALID, REJOIN_FORBIDDEN, REJOIN_REASON_REQUIRED, REJOIN_NOT_ALLOWED, EMPLOYEE_NOT_FOUND, EXIT_EXCEPTION_FORBIDDEN, EXIT_EXCEPTION_REASON_REQUIRED, EXIT_REQUEST_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod, Verified, Completed.
- Method match: `RecordEmployeeExitInterview()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:32`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 38. POST api/User/EmployeeExitReassignment

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitReassignment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitReassignment()`
- Approximate source line at generation: 355
- Direct delegated call found: `_employeeService.UpsertEmployeeExitReassignment()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitReassignment`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitReassignmentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitReassignment")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitReassignmentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitReassignment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitReassignment()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitReassignment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitReassignment()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2287`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_REASSIGNMENT_INVALID_OR_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_REASSIGNMENT_OWNER_INVALID, REJOIN_FORBIDDEN, REJOIN_REASON_REQUIRED, REJOIN_NOT_ALLOWED, EMPLOYEE_NOT_FOUND, EXIT_EXCEPTION_FORBIDDEN, EXIT_EXCEPTION_REASON_REQUIRED, EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod, Verified, Completed.
- Method match: `UpsertEmployeeExitReassignment()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:33`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 39. POST api/User/GenerateEmployeeExitDocument

### Purpose

This endpoint is implemented by `UserController.GenerateEmployeeExitDocument()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `GenerateEmployeeExitDocument()`
- Approximate source line at generation: 360
- Direct delegated call found: `_employeeService.GenerateEmployeeExitDocument()`

### HTTP contract

- Method: `POST`
- Route: `api/User/GenerateEmployeeExitDocument`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] GenerateEmployeeExitDocumentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GenerateEmployeeExitDocument")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] GenerateEmployeeExitDocumentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/GenerateEmployeeExitDocument`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.GenerateEmployeeExitDocument()` receives the bound values.
4. The action delegates to `_employeeService.GenerateEmployeeExitDocument()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GenerateEmployeeExitDocument()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1953`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_DOCUMENT_INVALID_OR_FORBIDDEN, EXIT_DOCUMENT_NOT_AVAILABLE, FORMER_DOCUMENT_ISSUE_FORBIDDEN, FORMER_DOCUMENT_NOT_READY, POST_EXIT_CONTACT_VERIFICATION_REQUIRED, FORMER_DOCUMENT_ALREADY_ISSUED, EXIT_CLEARANCE_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_CLEARANCE_WAIVER_REASON_REQUIRED.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued, Pending, Completed.
- Method match: `GenerateEmployeeExitDocument()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:52`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 40. POST api/User/IssueEmployeeExitDocument

### Purpose

This endpoint is implemented by `UserController.IssueEmployeeExitDocument()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `IssueEmployeeExitDocument()`
- Approximate source line at generation: 365
- Direct delegated call found: `_employeeService.IssueEmployeeExitDocument()`

### HTTP contract

- Method: `POST`
- Route: `api/User/IssueEmployeeExitDocument`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] IssueEmployeeExitDocumentRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("IssueEmployeeExitDocument")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] IssueEmployeeExitDocumentRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/IssueEmployeeExitDocument`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.IssueEmployeeExitDocument()` receives the bound values.
4. The action delegates to `_employeeService.IssueEmployeeExitDocument()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `IssueEmployeeExitDocument()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1966`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_DOCUMENT_ISSUE_FORBIDDEN, FORMER_DOCUMENT_NOT_READY, POST_EXIT_CONTACT_VERIFICATION_REQUIRED, FORMER_DOCUMENT_ALREADY_ISSUED, EXIT_CLEARANCE_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_CLEARANCE_WAIVER_REASON_REQUIRED, EXIT_HANDOVER_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued, Pending, Completed, NoticePeriod.
- Method match: `IssueEmployeeExitDocument()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:53`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 41. POST api/User/CompleteEmployeeExitClearance

### Purpose

This endpoint is implemented by `UserController.CompleteEmployeeExitClearance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CompleteEmployeeExitClearance()`
- Approximate source line at generation: 370
- Direct delegated call found: `_employeeService.CompleteEmployeeExitClearance()`

### HTTP contract

- Method: `POST`
- Route: `api/User/CompleteEmployeeExitClearance`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CompleteEmployeeExitClearanceRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CompleteEmployeeExitClearance")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CompleteEmployeeExitClearanceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/CompleteEmployeeExitClearance`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CompleteEmployeeExitClearance()` receives the bound values.
4. The action delegates to `_employeeService.CompleteEmployeeExitClearance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompleteEmployeeExitClearance()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1988`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_CLEARANCE_WAIVER_REASON_REQUIRED, EXIT_HANDOVER_INVALID, EXIT_HANDOVER_FORBIDDEN, EXIT_HANDOVER_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Completed, Resigned, Terminated, NoticePeriod, Submitted.
- Method match: `CompleteEmployeeExitClearance()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:54`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 42. POST api/User/UpdateEmployeeExitClearanceStatus

### Purpose

This endpoint is implemented by `UserController.UpdateEmployeeExitClearanceStatus()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpdateEmployeeExitClearanceStatus()`
- Approximate source line at generation: 375
- Direct delegated call found: `_employeeService.UpdateEmployeeExitClearanceStatus()`

### HTTP contract

- Method: `POST`
- Route: `api/User/UpdateEmployeeExitClearanceStatus`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateEmployeeExitClearanceStatusRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("UpdateEmployeeExitClearanceStatus")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateEmployeeExitClearanceStatusRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/UpdateEmployeeExitClearanceStatus`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpdateEmployeeExitClearanceStatus()` receives the bound values.
4. The action delegates to `_employeeService.UpdateEmployeeExitClearanceStatus()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateEmployeeExitClearanceStatus()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2261`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_STATUS_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_INTERVIEW_FORBIDDEN, EXIT_REASSIGNMENT_INVALID_OR_FORBIDDEN, EXIT_REASSIGNMENT_OWNER_INVALID, REJOIN_FORBIDDEN, REJOIN_REASON_REQUIRED, REJOIN_NOT_ALLOWED.
- Explicit workflow/status literals found: Completed, Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod.
- Method match: `UpdateEmployeeExitClearanceStatus()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:31`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 43. POST api/User/WaiveEmployeeExitClearance

### Purpose

This endpoint is implemented by `UserController.WaiveEmployeeExitClearance()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `WaiveEmployeeExitClearance()`
- Approximate source line at generation: 380
- Direct delegated call found: `_employeeService.CompleteEmployeeExitClearance()`

### HTTP contract

- Method: `POST`
- Route: `api/User/WaiveEmployeeExitClearance`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CompleteEmployeeExitClearanceRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("WaiveEmployeeExitClearance")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CompleteEmployeeExitClearanceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/WaiveEmployeeExitClearance`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.WaiveEmployeeExitClearance()` receives the bound values.
4. The action delegates to `_employeeService.CompleteEmployeeExitClearance()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CompleteEmployeeExitClearance()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:1988`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CLEARANCE_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_CLEARANCE_WAIVER_REASON_REQUIRED, EXIT_HANDOVER_INVALID, EXIT_HANDOVER_FORBIDDEN, EXIT_HANDOVER_NOT_FOUND.
- Explicit workflow/status literals found: Pending, Completed, Resigned, Terminated, NoticePeriod, Submitted.
- Method match: `CompleteEmployeeExitClearance()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:54`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 44. POST api/User/EmployeeExitHandoverTask

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitHandoverTask()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitHandoverTask()`
- Approximate source line at generation: 385
- Direct delegated call found: `_employeeService.UpsertEmployeeExitHandoverTask()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitHandoverTask`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitHandoverTaskRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitHandoverTask")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitHandoverTaskRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitHandoverTask`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitHandoverTask()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitHandoverTask()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitHandoverTask()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2017`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_HANDOVER_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_HANDOVER_FORBIDDEN, EXIT_HANDOVER_NOT_FOUND, EXIT_ASSET_INVALID, EXIT_ASSET_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, NoticePeriod, Submitted, Approved.
- Method match: `UpsertEmployeeExitHandoverTask()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:55`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 45. POST api/User/ReviewEmployeeExitHandoverTask

### Purpose

This endpoint is implemented by `UserController.ReviewEmployeeExitHandoverTask()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ReviewEmployeeExitHandoverTask()`
- Approximate source line at generation: 390
- Direct delegated call found: `_employeeService.ReviewEmployeeExitHandoverTask()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ReviewEmployeeExitHandoverTask`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] ReviewEmployeeExitHandoverTaskRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ReviewEmployeeExitHandoverTask")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ReviewEmployeeExitHandoverTaskRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ReviewEmployeeExitHandoverTask`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ReviewEmployeeExitHandoverTask()` receives the bound values.
4. The action delegates to `_employeeService.ReviewEmployeeExitHandoverTask()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ReviewEmployeeExitHandoverTask()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2040`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_HANDOVER_INVALID, EXIT_HANDOVER_NOT_FOUND, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_HANDOVER_FORBIDDEN, EXIT_ASSET_INVALID, EXIT_ASSET_FORBIDDEN, EXIT_ACCESS_INVALID, EXIT_ACCESS_FORBIDDEN, EXIT_FINANCE_INVALID, EXIT_FINANCE_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Submitted, Approved, NoticePeriod, Verified.
- Method match: `ReviewEmployeeExitHandoverTask()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:56`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 46. POST api/User/EmployeeExitAsset

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitAsset()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitAsset()`
- Approximate source line at generation: 395
- Direct delegated call found: `_employeeService.UpsertEmployeeExitAsset()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitAsset`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitAssetRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitAsset")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitAssetRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitAsset`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitAsset()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitAsset()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitAsset()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2063`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_ASSET_INVALID, EXIT_ASSET_FORBIDDEN, EXIT_ACCESS_INVALID, EXIT_ACCESS_FORBIDDEN, EXIT_FINANCE_INVALID, EXIT_FINANCE_FORBIDDEN, EXIT_BENEFIT_INVALID, EXIT_BENEFIT_FORBIDDEN, EXIT_EVIDENCE_INVALID, EXIT_EVIDENCE_VISIBILITY_INVALID, EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED, EXIT_EVIDENCE_REFERENCE_REQUIRED.
- Explicit workflow/status literals found: NoticePeriod, Verified, Resigned, Terminated.
- Method match: `UpsertEmployeeExitAsset()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:57`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 47. POST api/User/EmployeeExitAccessItem

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitAccessItem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitAccessItem()`
- Approximate source line at generation: 400
- Direct delegated call found: `_employeeService.UpsertEmployeeExitAccessItem()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitAccessItem`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitAccessItemRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitAccessItem")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitAccessItemRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitAccessItem`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitAccessItem()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitAccessItem()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitAccessItem()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2077`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_ACCESS_INVALID, EXIT_ACCESS_FORBIDDEN, EXIT_FINANCE_INVALID, EXIT_FINANCE_FORBIDDEN, EXIT_BENEFIT_INVALID, EXIT_BENEFIT_FORBIDDEN, EXIT_EVIDENCE_INVALID, EXIT_EVIDENCE_VISIBILITY_INVALID, EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED, EXIT_EVIDENCE_REFERENCE_REQUIRED, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_FILE_METADATA_REQUIRED.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued.
- Method match: `UpsertEmployeeExitAccessItem()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:58`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 48. POST api/User/EmployeeExitFinanceItem

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitFinanceItem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitFinanceItem()`
- Approximate source line at generation: 405
- Direct delegated call found: `_employeeService.UpsertEmployeeExitFinanceItem()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFinanceItem`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitFinanceItemRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFinanceItem")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitFinanceItemRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFinanceItem`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitFinanceItem()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitFinanceItem()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitFinanceItem()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2090`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_FINANCE_INVALID, EXIT_FINANCE_FORBIDDEN, EXIT_BENEFIT_INVALID, EXIT_BENEFIT_FORBIDDEN, EXIT_EVIDENCE_INVALID, EXIT_EVIDENCE_VISIBILITY_INVALID, EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED, EXIT_EVIDENCE_REFERENCE_REQUIRED, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_FILE_METADATA_REQUIRED, EXIT_EVIDENCE_DUPLICATE, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued.
- Method match: `UpsertEmployeeExitFinanceItem()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:59`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 49. POST api/User/EmployeeExitBenefitItem

### Purpose

This endpoint is implemented by `UserController.UpsertEmployeeExitBenefitItem()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpsertEmployeeExitBenefitItem()`
- Approximate source line at generation: 410
- Direct delegated call found: `_employeeService.UpsertEmployeeExitBenefitItem()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitBenefitItem`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpsertEmployeeExitBenefitItemRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitBenefitItem")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpsertEmployeeExitBenefitItemRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitBenefitItem`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpsertEmployeeExitBenefitItem()` receives the bound values.
4. The action delegates to `_employeeService.UpsertEmployeeExitBenefitItem()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpsertEmployeeExitBenefitItem()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2103`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_BENEFIT_INVALID, EXIT_BENEFIT_FORBIDDEN, EXIT_EVIDENCE_INVALID, EXIT_EVIDENCE_VISIBILITY_INVALID, EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED, EXIT_EVIDENCE_REFERENCE_REQUIRED, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_FILE_METADATA_REQUIRED, EXIT_EVIDENCE_DUPLICATE, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued.
- Method match: `UpsertEmployeeExitBenefitItem()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:60`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 50. POST api/User/EmployeeExitEvidence

### Purpose

This endpoint is implemented by `UserController.AddEmployeeExitEvidence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AddEmployeeExitEvidence()`
- Approximate source line at generation: 415
- Direct delegated call found: `_employeeService.AddEmployeeExitEvidence()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitEvidence`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] AddEmployeeExitEvidenceRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitEvidence")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AddEmployeeExitEvidenceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitEvidence`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AddEmployeeExitEvidence()` receives the bound values.
4. The action delegates to `_employeeService.AddEmployeeExitEvidence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2116`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EVIDENCE_INVALID, EXIT_EVIDENCE_VISIBILITY_INVALID, EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED, EXIT_EVIDENCE_REFERENCE_REQUIRED, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_FILE_METADATA_REQUIRED, EXIT_EVIDENCE_DUPLICATE, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued.
- Method match: `AddEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:61`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 51. POST api/User/EmployeeExitEvidenceUpload

### Purpose

This endpoint is implemented by `UserController.UploadEmployeeExitEvidence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UploadEmployeeExitEvidence()`
- Approximate source line at generation: 420
- Direct delegated call found: `_employeeService.GetEmployeeExitCaseForEvidence()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitEvidenceUpload`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromForm] string employeeExitId, [FromForm] string category, [FromForm] string visibility, [FromForm] IFormFile file`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitEvidenceUpload")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] string employeeExitId, [FromForm] string category, [FromForm] string visibility, [FromForm] IFormFile file`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitEvidenceUpload`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UploadEmployeeExitEvidence()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitCaseForEvidence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitCaseForEvidence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2420`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_CASE_NOT_FOUND, EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN, EXIT_CASE_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_COMPLETION_FORBIDDEN, EXIT_REQUEST_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Withdrawn, Approved, NoticePeriod, Pending.
- Method match: `GetEmployeeExitCaseForEvidence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:68`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
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

## 52. GET api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download

### Purpose

This endpoint is implemented by `UserController.DownloadEmployeeExitEvidence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `DownloadEmployeeExitEvidence()`
- Approximate source line at generation: 442
- Direct delegated call found: `_employeeService.GetEmployeeExitEvidence()`

### HTTP contract

- Method: `GET`
- Route: `api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string employeeExitId, string evidenceId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string employeeExitId, string evidenceId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/User/EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.DownloadEmployeeExitEvidence()` receives the bound values.
4. The action delegates to `_employeeService.GetEmployeeExitEvidence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2428`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_COMPLETION_FORBIDDEN, EXIT_REQUEST_INVALID, OPEN_EXIT_NOT_FOUND, LAST_WORKING_DATE_NOT_REACHED.
- Explicit workflow/status literals found: Resigned, Terminated, Withdrawn, Approved, NoticePeriod, Pending.
- Method match: `GetEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:69`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
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

## 53. POST api/User/VerifyEmployeeExitEvidence

### Purpose

This endpoint is implemented by `UserController.VerifyEmployeeExitEvidence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `VerifyEmployeeExitEvidence()`
- Approximate source line at generation: 453
- Direct delegated call found: `_employeeService.VerifyEmployeeExitEvidence()`

### HTTP contract

- Method: `POST`
- Route: `api/User/VerifyEmployeeExitEvidence`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] VerifyEmployeeExitEvidenceRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("VerifyEmployeeExitEvidence")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] VerifyEmployeeExitEvidenceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/VerifyEmployeeExitEvidence`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.VerifyEmployeeExitEvidence()` receives the bound values.
4. The action delegates to `_employeeService.VerifyEmployeeExitEvidence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `VerifyEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2144`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EVIDENCE_FORBIDDEN, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_EVIDENCE_NOT_ACTIONABLE, EXIT_POST_EXIT_CONTACT_INVALID, EXIT_POST_EXIT_CONTACT_FORBIDDEN, EXIT_ACKNOWLEDGEMENT_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_INVALID.
- Explicit workflow/status literals found: Resigned, Terminated, Verified, Issued, Completed.
- Method match: `VerifyEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:62`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 54. POST api/User/SupersedeEmployeeExitEvidence

### Purpose

This endpoint is implemented by `UserController.SupersedeEmployeeExitEvidence()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SupersedeEmployeeExitEvidence()`
- Approximate source line at generation: 458
- Direct delegated call found: `_employeeService.SupersedeEmployeeExitEvidence()`

### HTTP contract

- Method: `POST`
- Route: `api/User/SupersedeEmployeeExitEvidence`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] SupersedeEmployeeExitEvidenceRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("SupersedeEmployeeExitEvidence")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] SupersedeEmployeeExitEvidenceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/SupersedeEmployeeExitEvidence`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SupersedeEmployeeExitEvidence()` receives the bound values.
4. The action delegates to `_employeeService.SupersedeEmployeeExitEvidence()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SupersedeEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2165`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_NOT_ACTIONABLE, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_POST_EXIT_CONTACT_INVALID, EXIT_POST_EXIT_CONTACT_FORBIDDEN, EXIT_ACKNOWLEDGEMENT_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_INVALID, FORMER_EMPLOYEE_REQUEST_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID, FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Issued, Completed, Active.
- Method match: `SupersedeEmployeeExitEvidence()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:63`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 55. POST api/User/EmployeeExitPostExitContact

### Purpose

This endpoint is implemented by `UserController.UpdateEmployeeExitPostExitContact()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `UpdateEmployeeExitPostExitContact()`
- Approximate source line at generation: 463
- Direct delegated call found: `_employeeService.UpdateEmployeeExitPostExitContact()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitPostExitContact`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] UpdateEmployeeExitPostExitContactRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitPostExitContact")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] UpdateEmployeeExitPostExitContactRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitPostExitContact`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.UpdateEmployeeExitPostExitContact()` receives the bound values.
4. The action delegates to `_employeeService.UpdateEmployeeExitPostExitContact()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateEmployeeExitPostExitContact()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2185`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_POST_EXIT_CONTACT_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_POST_EXIT_CONTACT_FORBIDDEN, EXIT_ACKNOWLEDGEMENT_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_INVALID, FORMER_EMPLOYEE_REQUEST_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID, FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_CHANGED_OR_NOT_FOUND, FORMER_EMPLOYEE_REQUEST_ALREADY_FINALIZED, EXIT_CLEARANCE_STATUS_INVALID.
- Explicit workflow/status literals found: Issued, Completed, Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod.
- Method match: `UpdateEmployeeExitPostExitContact()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:64`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 56. POST api/User/AcknowledgeEmployeeExit

### Purpose

This endpoint is implemented by `UserController.AcknowledgeEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `AcknowledgeEmployeeExit()`
- Approximate source line at generation: 468
- Direct delegated call found: `_employeeService.AcknowledgeEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/AcknowledgeEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] AcknowledgeEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AcknowledgeEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] AcknowledgeEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/AcknowledgeEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.AcknowledgeEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.AcknowledgeEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AcknowledgeEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2218`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_ACKNOWLEDGEMENT_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_INVALID, FORMER_EMPLOYEE_REQUEST_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID, FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_CHANGED_OR_NOT_FOUND, FORMER_EMPLOYEE_REQUEST_ALREADY_FINALIZED, EXIT_CLEARANCE_STATUS_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_INTERVIEW_FORBIDDEN.
- Explicit workflow/status literals found: Issued, Completed, Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod.
- Method match: `AcknowledgeEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:65`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 57. POST api/User/EmployeeExitFormerEmployeeRequest

### Purpose

This endpoint is implemented by `UserController.CreateEmployeeExitFormerEmployeeRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CreateEmployeeExitFormerEmployeeRequest()`
- Approximate source line at generation: 473
- Direct delegated call found: `_employeeService.CreateEmployeeExitFormerEmployeeRequest()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFormerEmployeeRequest`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] CreateEmployeeExitFormerEmployeeRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFormerEmployeeRequest")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CreateEmployeeExitFormerEmployeeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFormerEmployeeRequest`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CreateEmployeeExitFormerEmployeeRequest()` receives the bound values.
4. The action delegates to `_employeeService.CreateEmployeeExitFormerEmployeeRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateEmployeeExitFormerEmployeeRequest()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2228`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_EMPLOYEE_REQUEST_INVALID, FORMER_EMPLOYEE_REQUEST_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID, FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_CHANGED_OR_NOT_FOUND, FORMER_EMPLOYEE_REQUEST_ALREADY_FINALIZED, EXIT_CLEARANCE_STATUS_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_INTERVIEW_FORBIDDEN.
- Explicit workflow/status literals found: Issued, Completed, Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod.
- Method match: `CreateEmployeeExitFormerEmployeeRequest()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:66`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 58. POST api/User/EmployeeExitFormerEmployeeRequestResolve

### Purpose

This endpoint is implemented by `UserController.ResolveEmployeeExitFormerEmployeeRequest()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ResolveEmployeeExitFormerEmployeeRequest()`
- Approximate source line at generation: 478
- Direct delegated call found: `_employeeService.ResolveEmployeeExitFormerEmployeeRequest()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFormerEmployeeRequestResolve`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] ResolveEmployeeExitFormerEmployeeRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFormerEmployeeRequestResolve")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ResolveEmployeeExitFormerEmployeeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFormerEmployeeRequestResolve`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ResolveEmployeeExitFormerEmployeeRequest()` receives the bound values.
4. The action delegates to `_employeeService.ResolveEmployeeExitFormerEmployeeRequest()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ResolveEmployeeExitFormerEmployeeRequest()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2240`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID, FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN, FORMER_EMPLOYEE_REQUEST_CHANGED_OR_NOT_FOUND, FORMER_EMPLOYEE_REQUEST_ALREADY_FINALIZED, EXIT_CLEARANCE_STATUS_INVALID, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_CLEARANCE_NOT_ACTIONABLE, EXIT_CLEARANCE_FORBIDDEN, EXIT_INTERVIEW_FORBIDDEN, EXIT_REASSIGNMENT_INVALID_OR_FORBIDDEN.
- Explicit workflow/status literals found: Issued, Completed, Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod.
- Method match: `ResolveEmployeeExitFormerEmployeeRequest()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:67`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 59. POST api/User/EmployeeExitFormerEmployeePortalInvite

### Purpose

This endpoint is implemented by `UserController.CreateFormerEmployeePortalInvite()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `CreateFormerEmployeePortalInvite()`
- Approximate source line at generation: 483
- Direct delegated call found: `_formerEmployeePortalService.CreateInvite()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFormerEmployeePortalInvite`
- Return declaration: `Task<Result<FormerEmployeePortalInviteDto>>`
- Parameters exactly as declared: `[FromBody] CreateFormerEmployeePortalInviteRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFormerEmployeePortalInvite")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] CreateFormerEmployeePortalInviteRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFormerEmployeePortalInvite`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.CreateFormerEmployeePortalInvite()` receives the bound values.
4. The action delegates to `_formerEmployeePortalService.CreateInvite()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateInvite()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:38`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_INVITE_FORBIDDEN, FORMER_PORTAL_COMPANY_DISABLED, FORMER_PORTAL_NOT_READY, FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED, FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED, FORMER_PORTAL_TOKEN_INVALID, FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_OTP_CREATE_FAILED, FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED, FORMER_PORTAL_OTP_INVALID, FORMER_PORTAL_OTP_INVALID_OR_EXPIRED.
- Explicit workflow/status literals found: Resigned, Terminated.
- Method match: `CreateInvite()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:8`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `Task<Result<FormerEmployeePortalInviteDto>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 60. POST api/User/EmployeeExitFormerEmployeePortalRevoke

### Purpose

This endpoint is implemented by `UserController.RevokeFormerEmployeePortalAccess()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RevokeFormerEmployeePortalAccess()`
- Approximate source line at generation: 488
- Direct delegated call found: `_formerEmployeePortalService.RevokeAccess()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFormerEmployeePortalRevoke`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RevokeFormerEmployeePortalAccessRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFormerEmployeePortalRevoke")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RevokeFormerEmployeePortalAccessRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFormerEmployeePortalRevoke`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RevokeFormerEmployeePortalAccess()` receives the bound values.
4. The action delegates to `_formerEmployeePortalService.RevokeAccess()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RevokeAccess()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:238`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `RevokeAccess()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:9`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
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

## 61. POST api/User/EmployeeExitFormerEmployeePortalAccess

### Purpose

This endpoint is implemented by `UserController.SetFormerEmployeePortalAccess()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SetFormerEmployeePortalAccess()`
- Approximate source line at generation: 493
- Direct delegated call found: `_formerEmployeePortalService.SetAccess()`

### HTTP contract

- Method: `POST`
- Route: `api/User/EmployeeExitFormerEmployeePortalAccess`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] SetFormerEmployeePortalAccessRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EmployeeExitFormerEmployeePortalAccess")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] SetFormerEmployeePortalAccessRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/EmployeeExitFormerEmployeePortalAccess`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SetFormerEmployeePortalAccess()` receives the bound values.
4. The action delegates to `_formerEmployeePortalService.SetAccess()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SetAccess()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:203`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_ACCESS_FORBIDDEN, FORMER_PORTAL_ACCESS_NOT_READY, FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `SetAccess()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:14`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
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

## 62. POST api/User/CompanyFormerEmployeePortalAccess

### Purpose

This endpoint is implemented by `UserController.SetCompanyFormerEmployeePortalAccess()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `SetCompanyFormerEmployeePortalAccess()`
- Approximate source line at generation: 498
- Direct delegated call found: `_formerEmployeePortalService.SetCompanyAccess()`

### HTTP contract

- Method: `POST`
- Route: `api/User/CompanyFormerEmployeePortalAccess`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] SetCompanyFormerEmployeePortalRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("CompanyFormerEmployeePortalAccess")] [HttpPost] [Authorize(Policy = "AdminOnly")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] SetCompanyFormerEmployeePortalRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/CompanyFormerEmployeePortalAccess`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.SetCompanyFormerEmployeePortalAccess()` receives the bound values.
4. The action delegates to `_formerEmployeePortalService.SetCompanyAccess()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SetCompanyAccess()` at `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs:215`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN, COMPANY_NOT_FOUND, POST_EXIT_CONTACT_TOKEN_INVALID, POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED, FORMER_PORTAL_REVOKE_FORBIDDEN, FORMER_PORTAL_REVOKE_NOT_FOUND, FORMER_PORTAL_SESSION_INVALID, FORMER_PORTAL_REQUEST_INVALID, FORMER_PORTAL_DOCUMENT_FORBIDDEN.
- Explicit workflow/status literals found: Resigned, Terminated, Verified.
- Method match: `SetCompanyAccess()` at `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs:15`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
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

## 63. POST api/User/RejoinEmployee

### Purpose

This endpoint is implemented by `UserController.RejoinEmployee()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `RejoinEmployee()`
- Approximate source line at generation: 502
- Direct delegated call found: `_employeeService.RejoinEmployee()`

### HTTP contract

- Method: `POST`
- Route: `api/User/RejoinEmployee`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] RejoinEmployeeRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("RejoinEmployee")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] RejoinEmployeeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/RejoinEmployee`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.RejoinEmployee()` receives the bound values.
4. The action delegates to `_employeeService.RejoinEmployee()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RejoinEmployee()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2300`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: REJOIN_FORBIDDEN, REJOIN_REASON_REQUIRED, REJOIN_NOT_ALLOWED, EMPLOYEE_NOT_FOUND, EXIT_EXCEPTION_FORBIDDEN, EXIT_EXCEPTION_REASON_REQUIRED, EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_CHANGED_OR_NOT_FOUND.
- Explicit workflow/status literals found: Resigned, Terminated, Active, Withdrawn, Approved, NoticePeriod, Verified, Completed.
- Method match: `RejoinEmployee()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:70`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## 64. POST api/User/ExceptionalCompleteEmployeeExit

### Purpose

This endpoint is implemented by `UserController.ExceptionalCompleteEmployeeExit()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/UserController.cs`
- Controller: `UserController`
- Action: `ExceptionalCompleteEmployeeExit()`
- Approximate source line at generation: 507
- Direct delegated call found: `_employeeService.ExceptionalCompleteEmployeeExit()`

### HTTP contract

- Method: `POST`
- Route: `api/User/ExceptionalCompleteEmployeeExit`
- Return declaration: `Task<Result>`
- Parameters exactly as declared: `[FromBody] ExceptionalCompleteEmployeeExitRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("ExceptionalCompleteEmployeeExit")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ExceptionalCompleteEmployeeExitRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/User/ExceptionalCompleteEmployeeExit`.
2. Authentication, controller attributes, filters, and model binding run.
3. `UserController.ExceptionalCompleteEmployeeExit()` receives the bound values.
4. The action delegates to `_employeeService.ExceptionalCompleteEmployeeExit()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ExceptionalCompleteEmployeeExit()` at `Codeji.CMS.Services/Employees/EmployeeService.cs:2320`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, permission reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: EXIT_EXCEPTION_FORBIDDEN, EXIT_EXCEPTION_REASON_REQUIRED, EXIT_REQUEST_INVALID, EXIT_CASE_FORBIDDEN, EXIT_CASE_NOT_FOUND, EXIT_CASE_CHANGED_OR_NOT_FOUND, EXIT_EVIDENCE_NOT_FOUND, EXIT_EVIDENCE_FORBIDDEN, EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN.
- Explicit workflow/status literals found: Withdrawn, Approved, NoticePeriod, Verified, Completed, Submitted.
- Method match: `ExceptionalCompleteEmployeeExit()` at `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs:71`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
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

## Service Source Inventory

### `Codeji.CMS.Services/Employees/EmployeeExitOutboxProcessor.cs`

- Lines: 146
- Extracted symbols: IEmployeeExitOutboxProcessor, ProcessPendingAsync, QueueDueRemindersAsync, now, collection, filter, item, FindOneAndUpdateOptions, notification, savedNotification, recipientNotification, savedRecipient, delay, state, error, dueCases, claim, BsonDocument, UpdateOptions, queued, finalStatus
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeeExitOutboxWorker.cs`

- Lines: 23
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeePresenceService.cs`

- Lines: 50
- Extracted symbols: ConcurrentDictionary, AddUserToGroup, group, RemoveUserFromGroup, GetOnlineUsersInGroup
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmployeeService.cs`

- Lines: 3847
- Extracted symbols: IMongoDbRepository, ILogger
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/EmploymentPeriod.cs`

- Lines: 60
- Extracted symbols: monthStart, monthEnd, employmentStatus, lastWorkingDate, isCompletedExit, day
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/FormerEmployeePortalService.cs`

- Lines: 341
- Extracted symbols: Task, companyId, actorId, company, exit, updated, now, rawToken, expiresAt, savedToken, portalUrl, delivery, failedAt, rollback, rawSession, sessionExpiry, update, FindOneAndUpdateOptions, RequestOtp, email, candidate, otp, saved
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeeExitEvidenceStorage.cs`

- Lines: 12
- Extracted symbols: IEmployeeExitEvidenceStorage, SaveAsync, OpenReadAsync, DeleteAsync
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeePresenceService.cs`

- Lines: 8
- Extracted symbols: IEmployeePresenceService, AddUserToGroup, RemoveUserFromGroup, GetOnlineUsersInGroup
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs`

- Lines: 118
- Extracted symbols: IEmployeeService, IsUserActive, GetEmployeeById, GetEmployeeNameById, InitiateEmployeeExit, SubmitOwnResignation, CompleteEmployeeExit, ReviewEmployeeExit, RespondToEmployeeExitDiscussion, RespondToEmployeeExitJointDiscussion, UpdateEmployeeExitClearanceStatus, RecordEmployeeExitInterview, UpsertEmployeeExitReassignment, WithdrawOwnEmployeeExit, DecideEmployeeExitWithdrawal, CancelEmployeeExit, UpdateEmployeeExitDate, RequestEmployeeExitNoticeAdjustment, DecideEmployeeExitNoticeAdjustment, DecideOwnEmployeeExitNoticeOffer, UpsertEmployeeExitPolicy, UpsertEmployeeExitClearanceTemplate, DeleteEmployeeExitClearanceTemplate, ApproveEmployeeFinalSettlement, UpdateEmployeeFinalSettlementPayment, CalculateEmployeeFinalSettlement, MarkEmployeeFinalSettlementPaid, GenerateEmployeeExitDocument, IssueEmployeeExitDocument, CompleteEmployeeExitClearance
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IFormerEmployeePortalService.cs`

- Lines: 20
- Extracted symbols: IFormerEmployeePortalService, RevokeAccess, RequestOtp, Logout, SetAccess, SetCompanyAccess, VerifyPostExitContact, CreateRequest
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/Interface/IRoleService.cs`

- Lines: 27
- Extracted symbols: IRoleService, AddEditRoles, GetRoleById, GetRolePermissionOfuser, CheckRoleDependancyForDeletion, VerifyUserAccess, UpdateAppAccessForRole, IsRoleTypeMatch
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/PrivateEmployeeExitEvidenceStorage.cs`

- Lines: 65
- Extracted symbols: PrivateEmployeeExitEvidenceStorage, SaveAsync, extension, key, path, using, OpenReadAsync, rootPrefix, contentType, DeleteAsync
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Employees/RoleServices.cs`

- Lines: 705
- Extracted symbols: AppModule, Module, PermissionConst, readonly, IMongoDbRepository, RoleServices, AddEditRoles, result, totalRoles, roleWithModulePermission, newRole, rolePermissions, newPermission, Task, roles, RestoreCompanyRolePermissionsAsync, GetRoleById, role, moduleWithPermissionsModel, modulePermissions, permissions
- Use: follow the controller delegation into this file when the symbol matches the action.

