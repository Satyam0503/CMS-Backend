# Recruitment and Career Portal API Documentation

**Last verified against code:** 2026-09-02

This document inventories active controller actions assigned to this module. Routes, signatures, attributes and direct delegation names are extracted from current controller source. Service-level claims remain explicitly traceable or marked uncertain.

## 1. Endpoint Summary

| # | Method | Endpoint | Controller | Action | Direct call |
|---:|---|---|---|---|---|
| 1 | `POST` | `api/Applicants/GetApplicantList` | `ApplicantsController` | `GetApplicantList` | `_applicantsService.GetApplicantsList()` |
| 2 | `GET` | `api/Applicants/ApplicantById` | `ApplicantsController` | `ApplicantById` | `_applicantsService.ApplicantById()` |
| 3 | `POST` | `api/Applicants/AddApplicant` | `ApplicantsController` | `AddApplicant` | `_applicantsService.RegisterApplicants()` |
| 4 | `POST` | `api/Applicants/EditApplicant` | `ApplicantsController` | `EditApplicants` | `_applicantsService.UpdateApplicants()` |
| 5 | `POST` | `api/Applicants/UploadResume` | `ApplicantsController` | `UploadResume` | `Path.GetExtension()` |
| 6 | `POST` | `api/Applicants/AddComment` | `ApplicantsController` | `AddComment` | `CurrentContext.UserId()` |
| 7 | `GET` | `api/Applicants/GetAllComment/{applicantId}` | `ApplicantsController` | `GetAllComment` | `_applicantsService.GetAllComment()` |
| 8 | `POST` | `api/Applicants/GetProcessLogData` | `ApplicantsController` | `GetProcessLogData` | `_applicantsService.GetProcessLogData()` |
| 9 | `GET` | `api/CareerProfile` | `CareerProfileController` | `Get` | `service.GetCompanyProfile()` |
| 10 | `PUT` | `api/CareerProfile` | `CareerProfileController` | `Save` | `service.SaveCompanyProfile()` |
| 11 | `PUT` | `api/CareerProfile/theme` | `CareerProfileController` | `UpdateTheme` | `service.UpdateCompanyTheme()` |
| 12 | `POST` | `api/JobVacancy/AddJobVacancy` | `JobVacancyController` | `AddJobVacancy` | `_jobVacancyService.AddJobVacancy()` |
| 13 | `POST` | `api/JobVacancy/EditJobVacancy` | `JobVacancyController` | `EditJobVacancy` | `_jobVacancyService.EditJobVacancy()` |
| 14 | `POST` | `api/JobVacancy/GetAllVacancy` | `JobVacancyController` | `GetAllVacancy` | `_jobVacancyService.GetAllVacancy()` |
| 15 | `POST` | `api/JobVacancy/GetVacancyById` | `JobVacancyController` | `GetVacancyById` | `_jobVacancyService.GetVacancyById()` |
| 16 | `DELETE` | `api/JobVacancy/DeleteJobVacancy` | `JobVacancyController` | `DeleteJobVacancy` | `_jobVacancyService.DeleteJobVacancy()` |
| 17 | `GET` | `api/public/jobs` | `PublicCareerController` | `GetJobs` | `_publicCareerService.GetMasterJobs()` |
| 18 | `GET` | `api/public/jobs/{publicJobId}` | `PublicCareerController` | `GetJobByPublicId` | `_publicCareerService.GetJobByPublicId()` |
| 19 | `GET` | `api/public/jobs/by-slug/{jobSlug}` | `PublicCareerController` | `GetJobBySlug` | `_publicCareerService.GetJobBySlug()` |
| 20 | `GET` | `api/public/companies/{companyCode}/jobs` | `PublicCareerController` | `GetCompanyJobs` | `_publicCareerService.GetCompanyJobs()` |
| 21 | `GET` | `api/public/job-locations` | `PublicCareerController` | `GetLocations` | `_publicCareerService.GetLocations()` |
| 22 | `POST` | `api/public/jobs/{publicJobId}/applications` | `PublicCareerController` | `Apply` | `_publicCareerService.Apply()` |
| 23 | `POST` | `api/public/applications/{applicationReference}/resume` | `PublicCareerController` | `UploadResume` | `_publicCareerService.UploadResume()` |
| 24 | `POST` | `api/public/career/jobs/{publicJobId}/save` | `PublicCareerEngagementController` | `Save` | `service.SaveJob()` |
| 25 | `DELETE` | `api/public/career/jobs/{publicJobId}/save` | `PublicCareerEngagementController` | `Unsave` | `service.RemoveSavedJob()` |
| 26 | `GET` | `api/public/career/jobs/saved` | `PublicCareerEngagementController` | `Saved` | `service.GetSavedJobs()` |
| 27 | `POST` | `api/public/career/jobs/merge-anonymous-saves` | `PublicCareerEngagementController` | `Merge` | `service.MergeAnonymousSaves()` |
| 28 | `POST` | `api/public/career/companies/{companyCode}/follow` | `PublicCareerEngagementController` | `Follow` | `service.FollowCompany()` |
| 29 | `DELETE` | `api/public/career/companies/{companyCode}/follow` | `PublicCareerEngagementController` | `Unfollow` | `service.UnfollowCompany()` |
| 30 | `GET` | `api/public/career/companies/{companyCode}/follow-status` | `PublicCareerEngagementController` | `FollowStatus` | `service.GetFollowStatus()` |
| 31 | `POST` | `api/public/career/job-alerts` | `PublicCareerEngagementController` | `CreateAlert` | `service.CreateAlert()` |
| 32 | `GET` | `api/public/career/job-alerts` | `PublicCareerEngagementController` | `GetAlerts` | `service.GetAlerts()` |
| 33 | `PUT` | `api/public/career/job-alerts/{alertId}` | `PublicCareerEngagementController` | `UpdateAlert` | `service.UpdateAlert()` |
| 34 | `DELETE` | `api/public/career/job-alerts/{alertId}` | `PublicCareerEngagementController` | `DeleteAlert` | `service.DeleteAlert()` |
| 35 | `POST` | `api/public/career/analytics` | `PublicCareerEngagementController` | `Track` | `service.Track()` |
| 36 | `PUT` | `api/public/career/visitor-preferences` | `PublicCareerEngagementController` | `VisitorPreferences` | `service.UpdateVisitorPreference()` |
| 37 | `GET` | `api/public/career/visitor-preferences` | `PublicCareerEngagementController` | `VisitorPreferences` | `service.GetVisitorPreference()` |
| 38 | `POST` | `api/public/career/subscribers` | `PublicCareerSubscriptionController` | `Subscribe` | `service.Subscribe()` |
| 39 | `POST` | `api/public/career/subscribers/verify` | `PublicCareerSubscriptionController` | `Verify` | `service.Verify()` |
| 40 | `POST` | `api/public/career/subscribers/resend-verification` | `PublicCareerSubscriptionController` | `Resend` | `service.Resend()` |
| 41 | `POST` | `api/public/career/subscribers/unsubscribe` | `PublicCareerSubscriptionController` | `Unsubscribe` | `service.Unsubscribe()` |
| 42 | `GET` | `api/public/career/subscribers/preferences` | `PublicCareerSubscriptionController` | `GetPreferences` | `service.GetPreferences()` |
| 43 | `PUT` | `api/public/career/subscribers/preferences` | `PublicCareerSubscriptionController` | `UpdatePreferences` | `service.UpdatePreferences()` |
| 44 | `GET` | `api/public/career/companies` | `PublicCompanyProfileController` | `GetCompanies` | `service.GetPublishedProfiles()` |
| 45 | `GET` | `api/public/career/companies/{companyCode}/profile` | `PublicCompanyProfileController` | `GetProfile` | `service.GetPublishedProfile()` |

## 2. Common interpretation rules

- Route text is composed from controller and action attributes. Absolute action routes override the controller base.
- Authorization shown per endpoint includes explicit controller/action attributes; inherited base-controller and middleware behavior still requires inspection.
- DTO field rules come from DTO declarations and service validation, not from property names.
- Database and side-effect behavior comes from service/repository code, not HTTP naming.

## 1. POST api/Applicants/GetApplicantList

### Purpose

This endpoint is implemented by `ApplicantsController.GetApplicantList()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `GetApplicantList()`
- Approximate source line at generation: 31
- Direct delegated call found: `_applicantsService.GetApplicantsList()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/GetApplicantList`
- Return declaration: `async Task<Result<ApplicantViewModel>>`
- Parameters exactly as declared: `ApplicantResultFilters? filters`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetApplicantList")] [ModulePermission(AppModule.Applications, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `ApplicantResultFilters? filters`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/GetApplicantList`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.GetApplicantList()` receives the bound values.
4. The action delegates to `_applicantsService.GetApplicantsList()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetApplicantsList()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:320`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetApplicantsList()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:15`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ApplicantViewModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 2. GET api/Applicants/ApplicantById

### Purpose

This endpoint is implemented by `ApplicantsController.ApplicantById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `ApplicantById()`
- Approximate source line at generation: 39
- Direct delegated call found: `_applicantsService.ApplicantById()`

### HTTP contract

- Method: `GET`
- Route: `api/Applicants/ApplicantById`
- Return declaration: `async Task<Result<ApplicantViewModel>>`
- Parameters exactly as declared: `string id`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpGet] [Route("ApplicantById")] [ModulePermission(AppModule.Applications, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string id`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Applicants/ApplicantById`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.ApplicantById()` receives the bound values.
4. The action delegates to `_applicantsService.ApplicantById()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `ApplicantById()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:376`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `ApplicantById()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:16`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ApplicantViewModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 3. POST api/Applicants/AddApplicant

### Purpose

This endpoint is implemented by `ApplicantsController.AddApplicant()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `AddApplicant()`
- Approximate source line at generation: 47
- Direct delegated call found: `_applicantsService.RegisterApplicants()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/AddApplicant`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] ApplicantAddEditModel applicantRegisterModel`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("AddApplicant")] [ModulePermission(AppModule.Applications, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ApplicantAddEditModel applicantRegisterModel`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/AddApplicant`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.AddApplicant()` receives the bound values.
4. The action delegates to `_applicantsService.RegisterApplicants()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RegisterApplicants()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:73`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `RegisterApplicants()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:12`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
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

## 4. POST api/Applicants/EditApplicant

### Purpose

This endpoint is implemented by `ApplicantsController.EditApplicants()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `EditApplicants()`
- Approximate source line at generation: 58
- Direct delegated call found: `_applicantsService.UpdateApplicants()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/EditApplicant`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromBody] ApplicantAddEditModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("EditApplicant")] [ModulePermission(AppModule.Applications, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromBody] ApplicantAddEditModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/EditApplicant`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.EditApplicants()` receives the bound values.
4. The action delegates to `_applicantsService.UpdateApplicants()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateApplicants()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:126`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateApplicants()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:14`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
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

## 5. POST api/Applicants/UploadResume

### Purpose

This endpoint is implemented by `ApplicantsController.UploadResume()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `UploadResume()`
- Approximate source line at generation: 72
- Direct delegated call found: `Path.GetExtension()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/UploadResume`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromForm] ResumeApplicantModel model, string email`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("UploadResume")] [AllowAnonymous] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromForm] ResumeApplicantModel model, string email`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/UploadResume`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.UploadResume()` receives the bound values.
4. The action delegates to `Path.GetExtension()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetExtension()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:296`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetExtension()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:259`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 6. POST api/Applicants/AddComment

### Purpose

This endpoint is implemented by `ApplicantsController.AddComment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `AddComment()`
- Approximate source line at generation: 97
- Direct delegated call found: `CurrentContext.UserId()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/AddComment`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `CommentRequestModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [Route("AddComment")] [HttpPost] [ModulePermission(AppModule.Applications, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CommentRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/AddComment`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.AddComment()` receives the bound values.
4. The action delegates to `CurrentContext.UserId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UserId()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:516`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
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

## 7. GET api/Applicants/GetAllComment/{applicantId}

### Purpose

This endpoint is implemented by `ApplicantsController.GetAllComment()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `GetAllComment()`
- Approximate source line at generation: 112
- Direct delegated call found: `_applicantsService.GetAllComment()`

### HTTP contract

- Method: `GET`
- Route: `api/Applicants/GetAllComment/{applicantId}`
- Return declaration: `async Task<Result<ApplicantLogResponseModel>>`
- Parameters exactly as declared: `string applicantId, [FromQuery] int pageNo, [FromQuery] int pageSize`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [Route("GetAllComment/{applicantId}")] [HttpGet] [ModulePermission(AppModule.Applications, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string applicantId, [FromQuery] int pageNo, [FromQuery] int pageSize`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/Applicants/GetAllComment/{applicantId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.GetAllComment()` receives the bound values.
4. The action delegates to `_applicantsService.GetAllComment()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAllComment()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:434`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAllComment()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:17`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ApplicantLogResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 8. POST api/Applicants/GetProcessLogData

### Purpose

This endpoint is implemented by `ApplicantsController.GetProcessLogData()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/ApplicantsController.cs`
- Controller: `ApplicantsController`
- Action: `GetProcessLogData()`
- Approximate source line at generation: 121
- Direct delegated call found: `_applicantsService.GetProcessLogData()`

### HTTP contract

- Method: `POST`
- Route: `api/Applicants/GetProcessLogData`
- Return declaration: `async Task<Result<ApplicantLogResponseModel>>`
- Parameters exactly as declared: `ApplicantLogFilterModel model`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[Route("api/[controller]")] [Authorize] [HttpPost] [Route("GetProcessLogData")] [ModulePermission(AppModule.ProcessLog, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `ApplicantLogFilterModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/Applicants/GetProcessLogData`.
2. Authentication, controller attributes, filters, and model binding run.
3. `ApplicantsController.GetProcessLogData()` receives the bound values.
4. The action delegates to `_applicantsService.GetProcessLogData()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetProcessLogData()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:470`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetProcessLogData()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:19`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<ApplicantLogResponseModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 9. GET api/CareerProfile

### Purpose

This endpoint is implemented by `CareerProfileController.Get()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
- Controller: `CareerProfileController`
- Action: `Get()`
- Approximate source line at generation: 22
- Direct delegated call found: `service.GetCompanyProfile()`

### HTTP contract

- Method: `GET`
- Route: `api/CareerProfile`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpGet] [ModulePermission(AppModule.CareerProfile, Permission.View)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/CareerProfile`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CareerProfileController.Get()` receives the bound values.
4. The action delegates to `service.GetCompanyProfile()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetCompanyProfile()` at `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:10`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetCompanyProfile()` at `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:70`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 10. PUT api/CareerProfile

### Purpose

This endpoint is implemented by `CareerProfileController.Save()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
- Controller: `CareerProfileController`
- Action: `Save()`
- Approximate source line at generation: 30
- Direct delegated call found: `service.SaveCompanyProfile()`

### HTTP contract

- Method: `PUT`
- Route: `api/CareerProfile`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `UpsertPublicCompanyProfileRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpPut] [ModulePermission(AppModule.CareerProfile, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `UpsertPublicCompanyProfileRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/CareerProfile`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CareerProfileController.Save()` receives the bound values.
4. The action delegates to `service.SaveCompanyProfile()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SaveCompanyProfile()` at `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:11`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `SaveCompanyProfile()` at `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:78`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 11. PUT api/CareerProfile/theme

### Purpose

This endpoint is implemented by `CareerProfileController.UpdateTheme()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/CareerProfileController.cs`
- Controller: `CareerProfileController`
- Action: `UpdateTheme()`
- Approximate source line at generation: 39
- Direct delegated call found: `service.UpdateCompanyTheme()`

### HTTP contract

- Method: `PUT`
- Route: `api/CareerProfile/theme`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `UpdateCompanyThemeRequest request`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Authorize] [Route("api/[controller]")] [HttpPut("theme")] [ModulePermission(AppModule.CareerProfile, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `UpdateCompanyThemeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/CareerProfile/theme`.
2. Authentication, controller attributes, filters, and model binding run.
3. `CareerProfileController.UpdateTheme()` receives the bound values.
4. The action delegates to `service.UpdateCompanyTheme()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateCompanyTheme()` at `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:15`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateCompanyTheme()` at `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:260`.
- Static signals in the inspected method window: company/tenant context reference, version/concurrency or update-result reference, repository/data-access reference, audit/history metadata reference.
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

## 12. POST api/JobVacancy/AddJobVacancy

### Purpose

This endpoint is implemented by `JobVacancyController.AddJobVacancy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Controller: `JobVacancyController`
- Action: `AddJobVacancy()`
- Approximate source line at generation: 34
- Direct delegated call found: `_jobVacancyService.AddJobVacancy()`

### HTTP contract

- Method: `POST`
- Route: `api/JobVacancy/AddJobVacancy`
- Return declaration: `async Task<Result<JobVacancyModel>>`
- Parameters exactly as declared: `JobVacancyModel jobVacancy`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("AddJobVacancy")] [HttpPost] [ModulePermission(AppModule.Jobs, Permission.Create)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `JobVacancyModel jobVacancy`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/JobVacancy/AddJobVacancy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `JobVacancyController.AddJobVacancy()` receives the bound values.
4. The action delegates to `_jobVacancyService.AddJobVacancy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `AddJobVacancy()` at `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:8`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `AddJobVacancy()` at `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:30`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<JobVacancyModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 13. POST api/JobVacancy/EditJobVacancy

### Purpose

This endpoint is implemented by `JobVacancyController.EditJobVacancy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Controller: `JobVacancyController`
- Action: `EditJobVacancy()`
- Approximate source line at generation: 42
- Direct delegated call found: `_jobVacancyService.EditJobVacancy()`

### HTTP contract

- Method: `POST`
- Route: `api/JobVacancy/EditJobVacancy`
- Return declaration: `async Task<Result<JobVacancyModel>>`
- Parameters exactly as declared: `JobVacancyModel jobVacancy, string jobId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("EditJobVacancy")] [HttpPost] [ModulePermission(AppModule.Jobs, Permission.Edit)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `JobVacancyModel jobVacancy, string jobId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/JobVacancy/EditJobVacancy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `JobVacancyController.EditJobVacancy()` receives the bound values.
4. The action delegates to `_jobVacancyService.EditJobVacancy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `EditJobVacancy()` at `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:9`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `EditJobVacancy()` at `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:79`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<JobVacancyModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 14. POST api/JobVacancy/GetAllVacancy

### Purpose

This endpoint is implemented by `JobVacancyController.GetAllVacancy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Controller: `JobVacancyController`
- Action: `GetAllVacancy()`
- Approximate source line at generation: 50
- Direct delegated call found: `_jobVacancyService.GetAllVacancy()`

### HTTP contract

- Method: `POST`
- Route: `api/JobVacancy/GetAllVacancy`
- Return declaration: `async Task<Result<GetJobVacancyModel>>`
- Parameters exactly as declared: `JobRequestModel model`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetAllVacancy")] [HttpPost] [AllowAnonymous]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `JobRequestModel model`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/JobVacancy/GetAllVacancy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `JobVacancyController.GetAllVacancy()` receives the bound values.
4. The action delegates to `_jobVacancyService.GetAllVacancy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAllVacancy()` at `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:10`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAllVacancy()` at `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:138`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<Result<GetJobVacancyModel>>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 15. POST api/JobVacancy/GetVacancyById

### Purpose

This endpoint is implemented by `JobVacancyController.GetVacancyById()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Controller: `JobVacancyController`
- Action: `GetVacancyById()`
- Approximate source line at generation: 59
- Direct delegated call found: `_jobVacancyService.GetVacancyById()`

### HTTP contract

- Method: `POST`
- Route: `api/JobVacancy/GetVacancyById`
- Return declaration: `async Task<string>`
- Parameters exactly as declared: `string vacancyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("GetVacancyById")] [HttpPost]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string vacancyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/JobVacancy/GetVacancyById`.
2. Authentication, controller attributes, filters, and model binding run.
3. `JobVacancyController.GetVacancyById()` receives the bound values.
4. The action delegates to `_jobVacancyService.GetVacancyById()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetVacancyById()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:109`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetVacancyById()` at `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:11`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetVacancyById()` at `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:208`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .

### Database impact

> WARNING: The controller signature alone does not conclusively determine collections, writes, transactions, or indexes. Follow the delegated service call into repository/entity code before changing or manually asserting persistence behavior.

### Response and errors

The compile-time return declaration is `async Task<string>`. Application success/error envelopes and status translation must be verified in the action, shared result handling, middleware, and delegated service. Do not fabricate status codes from conventional REST expectations.

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

## 16. DELETE api/JobVacancy/DeleteJobVacancy

### Purpose

This endpoint is implemented by `JobVacancyController.DeleteJobVacancy()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/JobVacancyController.cs`
- Controller: `JobVacancyController`
- Action: `DeleteJobVacancy()`
- Approximate source line at generation: 68
- Direct delegated call found: `_jobVacancyService.DeleteJobVacancy()`

### HTTP contract

- Method: `DELETE`
- Route: `api/JobVacancy/DeleteJobVacancy`
- Return declaration: `async Task<Result>`
- Parameters exactly as declared: `[FromQuery] string vacancyId`

### Authentication and authorization

Authentication/authorization attributes are present; read the exact attributes below.

- Controller/action attributes: `[ApiController] [Route("api/[controller]")] [Authorize] [Route("DeleteJobVacancy")] [HttpDelete] [ModulePermission(AppModule.Jobs, Permission.Delete)]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string vacancyId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/JobVacancy/DeleteJobVacancy`.
2. Authentication, controller attributes, filters, and model binding run.
3. `JobVacancyController.DeleteJobVacancy()` receives the bound values.
4. The action delegates to `_jobVacancyService.DeleteJobVacancy()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteJobVacancy()` at `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs:12`.
- Static signals in the inspected method window: repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteJobVacancy()` at `Codeji.CMS.Services/Recruitments/JobVacancyService.cs:216`.
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

## 17. GET api/public/jobs

### Purpose

This endpoint is implemented by `PublicCareerController.GetJobs()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `GetJobs()`
- Approximate source line at generation: 22
- Direct delegated call found: `_publicCareerService.GetMasterJobs()`

### HTTP contract

- Method: `GET`
- Route: `api/public/jobs`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] PublicJobSearchRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] PublicJobSearchRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/jobs`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.GetJobs()` receives the bound values.
4. The action delegates to `_publicCareerService.GetMasterJobs()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetMasterJobs()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:9`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetMasterJobs()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:70`.
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

## 18. GET api/public/jobs/{publicJobId}

### Purpose

This endpoint is implemented by `PublicCareerController.GetJobByPublicId()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `GetJobByPublicId()`
- Approximate source line at generation: 29
- Direct delegated call found: `_publicCareerService.GetJobByPublicId()`

### HTTP contract

- Method: `GET`
- Route: `api/public/jobs/{publicJobId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string publicJobId`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs/{publicJobId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string publicJobId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/jobs/{publicJobId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.GetJobByPublicId()` receives the bound values.
4. The action delegates to `_publicCareerService.GetJobByPublicId()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetJobByPublicId()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:12`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetJobByPublicId()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:148`.
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

## 19. GET api/public/jobs/by-slug/{jobSlug}

### Purpose

This endpoint is implemented by `PublicCareerController.GetJobBySlug()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `GetJobBySlug()`
- Approximate source line at generation: 36
- Direct delegated call found: `_publicCareerService.GetJobBySlug()`

### HTTP contract

- Method: `GET`
- Route: `api/public/jobs/by-slug/{jobSlug}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string jobSlug`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("jobs/by-slug/{jobSlug}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string jobSlug`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/jobs/by-slug/{jobSlug}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.GetJobBySlug()` receives the bound values.
4. The action delegates to `_publicCareerService.GetJobBySlug()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetJobBySlug()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:13`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetJobBySlug()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:153`.
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

## 20. GET api/public/companies/{companyCode}/jobs

### Purpose

This endpoint is implemented by `PublicCareerController.GetCompanyJobs()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `GetCompanyJobs()`
- Approximate source line at generation: 43
- Direct delegated call found: `_publicCareerService.GetCompanyJobs()`

### HTTP contract

- Method: `GET`
- Route: `api/public/companies/{companyCode}/jobs`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string companyCode, [FromQuery] PublicJobSearchRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("companies/{companyCode}/jobs")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string companyCode, [FromQuery] PublicJobSearchRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/companies/{companyCode}/jobs`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.GetCompanyJobs()` receives the bound values.
4. The action delegates to `_publicCareerService.GetCompanyJobs()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetCompanyJobs()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:10`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetCompanyJobs()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:76`.
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

## 21. GET api/public/job-locations

### Purpose

This endpoint is implemented by `PublicCareerController.GetLocations()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `GetLocations()`
- Approximate source line at generation: 50
- Direct delegated call found: `_publicCareerService.GetLocations()`

### HTTP contract

- Method: `GET`
- Route: `api/public/job-locations`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] string? companyCode, [FromQuery] string? search`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpGet("job-locations")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string? companyCode, [FromQuery] string? search`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/job-locations`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.GetLocations()` receives the bound values.
4. The action delegates to `_publicCareerService.GetLocations()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetLocations()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:11`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetLocations()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:107`.
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

## 22. POST api/public/jobs/{publicJobId}/applications

### Purpose

This endpoint is implemented by `PublicCareerController.Apply()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `Apply()`
- Approximate source line at generation: 58
- Direct delegated call found: `_publicCareerService.Apply()`

### HTTP contract

- Method: `POST`
- Route: `api/public/jobs/{publicJobId}/applications`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string publicJobId, [FromBody] PublicJobApplicationRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpPost("jobs/{publicJobId}/applications")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string publicJobId, [FromBody] PublicJobApplicationRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/jobs/{publicJobId}/applications`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.Apply()` receives the bound values.
4. The action delegates to `_publicCareerService.Apply()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Apply()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:14`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Apply()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:159`.
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

## 23. POST api/public/applications/{applicationReference}/resume

### Purpose

This endpoint is implemented by `PublicCareerController.UploadResume()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerController.cs`
- Controller: `PublicCareerController`
- Action: `UploadResume()`
- Approximate source line at generation: 66
- Direct delegated call found: `_publicCareerService.UploadResume()`

### HTTP contract

- Method: `POST`
- Route: `api/public/applications/{applicationReference}/resume`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string applicationReference, [FromForm] string token, [FromForm] IFormFile resume`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public")] [HttpPost("applications/{applicationReference}/resume")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string applicationReference, [FromForm] string token, [FromForm] IFormFile resume`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/applications/{applicationReference}/resume`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerController.UploadResume()` receives the bound values.
4. The action delegates to `_publicCareerService.UploadResume()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UploadResume()` at `Codeji.CMS.Services/Recruitments/ApplicantServices.cs:284`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference, audit/history metadata reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UploadResume()` at `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs:22`.
- Static signals in the inspected method window: notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UploadResume()` at `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs:15`.
- Static signals in the inspected method window: .
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UploadResume()` at `Codeji.CMS.Services/Recruitments/PublicCareerService.cs:255`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 24. POST api/public/career/jobs/{publicJobId}/save

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Save()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Save()`
- Approximate source line at generation: 16
- Direct delegated call found: `service.SaveJob()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/jobs/{publicJobId}/save`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string publicJobId, SaveJobRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("jobs/{publicJobId}/save")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string publicJobId, SaveJobRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/jobs/{publicJobId}/save`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Save()` receives the bound values.
4. The action delegates to `service.SaveJob()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `SaveJob()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:55`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `SaveJob()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:8`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 25. DELETE api/public/career/jobs/{publicJobId}/save

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Unsave()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Unsave()`
- Approximate source line at generation: 23
- Direct delegated call found: `service.RemoveSavedJob()`

### HTTP contract

- Method: `DELETE`
- Route: `api/public/career/jobs/{publicJobId}/save`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string publicJobId, [FromQuery] string? visitorToken`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("jobs/{publicJobId}/save")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string publicJobId, [FromQuery] string? visitorToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/public/career/jobs/{publicJobId}/save`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Unsave()` receives the bound values.
4. The action delegates to `service.RemoveSavedJob()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `RemoveSavedJob()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:80`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `RemoveSavedJob()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:9`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 26. GET api/public/career/jobs/saved

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Saved()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Saved()`
- Approximate source line at generation: 30
- Direct delegated call found: `service.GetSavedJobs()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/jobs/saved`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] string? visitorToken`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("jobs/saved")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string? visitorToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/jobs/saved`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Saved()` receives the bound values.
4. The action delegates to `service.GetSavedJobs()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetSavedJobs()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:95`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetSavedJobs()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:10`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 27. POST api/public/career/jobs/merge-anonymous-saves

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Merge()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Merge()`
- Approximate source line at generation: 37
- Direct delegated call found: `service.MergeAnonymousSaves()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/jobs/merge-anonymous-saves`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `MergeAnonymousSavesRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("jobs/merge-anonymous-saves")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `MergeAnonymousSavesRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/jobs/merge-anonymous-saves`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Merge()` receives the bound values.
4. The action delegates to `service.MergeAnonymousSaves()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `MergeAnonymousSaves()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:125`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `MergeAnonymousSaves()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:11`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 28. POST api/public/career/companies/{companyCode}/follow

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Follow()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Follow()`
- Approximate source line at generation: 45
- Direct delegated call found: `service.FollowCompany()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/companies/{companyCode}/follow`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string companyCode, FollowCompanyRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("companies/{companyCode}/follow")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string companyCode, FollowCompanyRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/companies/{companyCode}/follow`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Follow()` receives the bound values.
4. The action delegates to `service.FollowCompany()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `FollowCompany()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:147`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `FollowCompany()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:12`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 29. DELETE api/public/career/companies/{companyCode}/follow

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Unfollow()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Unfollow()`
- Approximate source line at generation: 52
- Direct delegated call found: `service.UnfollowCompany()`

### HTTP contract

- Method: `DELETE`
- Route: `api/public/career/companies/{companyCode}/follow`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string companyCode`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("companies/{companyCode}/follow")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string companyCode`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/public/career/companies/{companyCode}/follow`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Unfollow()` receives the bound values.
4. The action delegates to `service.UnfollowCompany()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UnfollowCompany()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:168`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UnfollowCompany()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:13`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 30. GET api/public/career/companies/{companyCode}/follow-status

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.FollowStatus()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `FollowStatus()`
- Approximate source line at generation: 59
- Direct delegated call found: `service.GetFollowStatus()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/companies/{companyCode}/follow-status`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string companyCode`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("companies/{companyCode}/follow-status")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string companyCode`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/companies/{companyCode}/follow-status`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.FollowStatus()` receives the bound values.
4. The action delegates to `service.GetFollowStatus()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetFollowStatus()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:179`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetFollowStatus()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:14`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 31. POST api/public/career/job-alerts

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.CreateAlert()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `CreateAlert()`
- Approximate source line at generation: 66
- Direct delegated call found: `service.CreateAlert()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/job-alerts`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `JobAlertRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("job-alerts")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `JobAlertRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/job-alerts`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.CreateAlert()` receives the bound values.
4. The action delegates to `service.CreateAlert()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `CreateAlert()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:197`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `CreateAlert()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:15`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 32. GET api/public/career/job-alerts

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.GetAlerts()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `GetAlerts()`
- Approximate source line at generation: 73
- Direct delegated call found: `service.GetAlerts()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/job-alerts`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("job-alerts")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/job-alerts`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.GetAlerts()` receives the bound values.
4. The action delegates to `service.GetAlerts()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetAlerts()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:208`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetAlerts()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:16`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 33. PUT api/public/career/job-alerts/{alertId}

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.UpdateAlert()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `UpdateAlert()`
- Approximate source line at generation: 80
- Direct delegated call found: `service.UpdateAlert()`

### HTTP contract

- Method: `PUT`
- Route: `api/public/career/job-alerts/{alertId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string alertId, JobAlertRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPut("job-alerts/{alertId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string alertId, JobAlertRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/public/career/job-alerts/{alertId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.UpdateAlert()` receives the bound values.
4. The action delegates to `service.UpdateAlert()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateAlert()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:216`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateAlert()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:17`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 34. DELETE api/public/career/job-alerts/{alertId}

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.DeleteAlert()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `DeleteAlert()`
- Approximate source line at generation: 87
- Direct delegated call found: `service.DeleteAlert()`

### HTTP contract

- Method: `DELETE`
- Route: `api/public/career/job-alerts/{alertId}`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string alertId`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpDelete("job-alerts/{alertId}")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string alertId`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `DELETE api/public/career/job-alerts/{alertId}`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.DeleteAlert()` receives the bound values.
4. The action delegates to `service.DeleteAlert()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `DeleteAlert()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:231`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `DeleteAlert()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:18`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 35. POST api/public/career/analytics

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.Track()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `Track()`
- Approximate source line at generation: 95
- Direct delegated call found: `service.Track()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/analytics`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `CareerAnalyticsRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPost("analytics")] [EnableRateLimiting("career-analytics")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CareerAnalyticsRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/analytics`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.Track()` receives the bound values.
4. The action delegates to `service.Track()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Track()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:240`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Track()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:19`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 36. PUT api/public/career/visitor-preferences

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.VisitorPreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `VisitorPreferences()`
- Approximate source line at generation: 102
- Direct delegated call found: `service.UpdateVisitorPreference()`

### HTTP contract

- Method: `PUT`
- Route: `api/public/career/visitor-preferences`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `VisitorPreferenceRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpPut("visitor-preferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `VisitorPreferenceRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/public/career/visitor-preferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.VisitorPreferences()` receives the bound values.
4. The action delegates to `service.UpdateVisitorPreference()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdateVisitorPreference()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:299`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdateVisitorPreference()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:20`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 37. GET api/public/career/visitor-preferences

### Purpose

This endpoint is implemented by `PublicCareerEngagementController.VisitorPreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerEngagementController.cs`
- Controller: `PublicCareerEngagementController`
- Action: `VisitorPreferences()`
- Approximate source line at generation: 109
- Direct delegated call found: `service.GetVisitorPreference()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/visitor-preferences`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `[FromQuery] string visitorToken`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career")] [HttpGet("visitor-preferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `[FromQuery] string visitorToken`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/visitor-preferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerEngagementController.VisitorPreferences()` receives the bound values.
4. The action delegates to `service.GetVisitorPreference()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetVisitorPreference()` at `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs:317`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetVisitorPreference()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs:21`.
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

## 38. POST api/public/career/subscribers

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.Subscribe()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `Subscribe()`
- Approximate source line at generation: 16
- Direct delegated call found: `service.Subscribe()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/subscribers`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `CareerSubscribeRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CareerSubscribeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/subscribers`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.Subscribe()` receives the bound values.
4. The action delegates to `service.Subscribe()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Subscribe()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:41`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Subscribe()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:9`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
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

## 39. POST api/public/career/subscribers/verify

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.Verify()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `Verify()`
- Approximate source line at generation: 24
- Direct delegated call found: `service.Verify()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/subscribers/verify`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `CareerTokenRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("verify")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CareerTokenRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/subscribers/verify`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.Verify()` receives the bound values.
4. The action delegates to `service.Verify()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Verify()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:96`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Verify()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:10`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
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

## 40. POST api/public/career/subscribers/resend-verification

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.Resend()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `Resend()`
- Approximate source line at generation: 32
- Direct delegated call found: `service.Resend()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/subscribers/resend-verification`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `CareerSubscribeRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("resend-verification")] [EnableRateLimiting("career-sensitive")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CareerSubscribeRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/subscribers/resend-verification`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.Resend()` receives the bound values.
4. The action delegates to `service.Resend()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Resend()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:123`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Resend()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:11`.
- Static signals in the inspected method window: repository/data-access reference, notification/email/outbox reference.
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

## 41. POST api/public/career/subscribers/unsubscribe

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.Unsubscribe()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `Unsubscribe()`
- Approximate source line at generation: 39
- Direct delegated call found: `service.Unsubscribe()`

### HTTP contract

- Method: `POST`
- Route: `api/public/career/subscribers/unsubscribe`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `CareerTokenRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPost("unsubscribe")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `CareerTokenRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `POST api/public/career/subscribers/unsubscribe`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.Unsubscribe()` receives the bound values.
4. The action delegates to `service.Unsubscribe()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `Unsubscribe()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:126`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `Unsubscribe()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:12`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 42. GET api/public/career/subscribers/preferences

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.GetPreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `GetPreferences()`
- Approximate source line at generation: 46
- Direct delegated call found: `service.GetPreferences()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/subscribers/preferences`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpGet("preferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/subscribers/preferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.GetPreferences()` receives the bound values.
4. The action delegates to `service.GetPreferences()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPreferences()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:148`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetPreferences()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:13`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 43. PUT api/public/career/subscribers/preferences

### Purpose

This endpoint is implemented by `PublicCareerSubscriptionController.UpdatePreferences()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCareerSubscriptionController.cs`
- Controller: `PublicCareerSubscriptionController`
- Action: `UpdatePreferences()`
- Approximate source line at generation: 53
- Direct delegated call found: `service.UpdatePreferences()`

### HTTP contract

- Method: `PUT`
- Route: `api/public/career/subscribers/preferences`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `UpdateCareerPreferencesRequest request`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/subscribers")] [HttpPut("preferences")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `UpdateCareerPreferencesRequest request`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `PUT api/public/career/subscribers/preferences`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCareerSubscriptionController.UpdatePreferences()` receives the bound values.
4. The action delegates to `service.UpdatePreferences()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `UpdatePreferences()` at `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs:156`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, notification/email/outbox reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `UpdatePreferences()` at `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs:14`.
- Static signals in the inspected method window: repository/data-access reference.
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

## 44. GET api/public/career/companies

### Purpose

This endpoint is implemented by `PublicCompanyProfileController.GetCompanies()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs`
- Controller: `PublicCompanyProfileController`
- Action: `GetCompanies()`
- Approximate source line at generation: 13
- Direct delegated call found: `service.GetPublishedProfiles()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/companies`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: ``

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/companies")] [HttpGet]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: ``. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/companies`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCompanyProfileController.GetCompanies()` receives the bound values.
4. The action delegates to `service.GetPublishedProfiles()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPublishedProfiles()` at `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:8`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetPublishedProfiles()` at `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:27`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## 45. GET api/public/career/companies/{companyCode}/profile

### Purpose

This endpoint is implemented by `PublicCompanyProfileController.GetProfile()`. Its business purpose follows that action and the delegated service method; no alternate route or behavior is invented here.

### Source

- Controller file: `Codeji.CMS.API/Controllers/PublicCompanyProfileController.cs`
- Controller: `PublicCompanyProfileController`
- Action: `GetProfile()`
- Approximate source line at generation: 20
- Direct delegated call found: `service.GetPublishedProfile()`

### HTTP contract

- Method: `GET`
- Route: `api/public/career/companies/{companyCode}/profile`
- Return declaration: `async Task<IActionResult>`
- Parameters exactly as declared: `string companyCode`

### Authentication and authorization

The action is explicitly anonymous.

- Controller/action attributes: `[ApiController] [AllowAnonymous] [Route("api/public/career/companies")] [HttpGet("{companyCode}/profile")]`
- Backend enforcement must include any service-level company, ownership, relationship, state, and version checks; controller visibility alone is not sufficient evidence.

### Request binding and DTO trace

The declared parameters are: `string companyCode`. Types and validation must be read from the referenced DTO/entity source. A field is not documented as required unless its type, validation attribute, or service validation proves that requirement.

### Processing flow

1. ASP.NET Core matches `GET api/public/career/companies/{companyCode}/profile`.
2. Authentication, controller attributes, filters, and model binding run.
3. `PublicCompanyProfileController.GetProfile()` receives the bound values.
4. The action delegates to `service.GetPublishedProfile()` or executes its inspected body.
5. The service applies module rules and repository operations.
6. The action returns its declared result/action result.

### Business rules and tenant boundary

Business rules are service-owned. Verify the delegated method for company filters, actor ownership, relationship scope, status transitions, optimistic version predicates, date rules, and duplicate handling. If none are present, record that as a potential issue rather than assuming global middleware supplies them.

### Extracted implementation evidence

- Method match: `GetPublishedProfile()` at `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs:9`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference.
- Explicit uppercase result/error tokens found: .
- Explicit workflow/status literals found: .
- Method match: `GetPublishedProfile()` at `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs:53`.
- Static signals in the inspected method window: company/tenant context reference, repository/data-access reference, audit/history metadata reference.
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

## Service Source Inventory

### `Codeji.CMS.Services/CareerPortal/CareerEngagementService.cs`

- Lines: 387
- Extracted symbols: IMongoCollection, CareerEngagementService, SaveJob, job, subscriber, visitorHash, identityFilter, filter, update, TryTrack, Ok, RemoveSavedJob, identity, Task, saved, jobs, companies
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/CareerNotificationService.cs`

- Lines: 363
- Extracted symbols: IMongoCollection, CareerNotificationService, HandleJobSaved, company, now, companyVisible, masterVisible, previousCompanyVisible, previousMasterVisible, QueueNewJob, if, QueueJobUpdate, ProcessNext, QueueSavedDeadlineReminders, claimable, item, FindOneAndUpdateOptions, processingItems, subscriber, pending, MarkSent, payload
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/CareerNotificationWorker.cs`

- Lines: 26
- Extracted symbols: 
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/CareerSubscriptionService.cs`

- Lines: 243
- Extracted symbols: IMongoCollection, CareerSubscriptionService, Subscribe, generic, email, now, rawVerification, rawUnsubscribe, existing, TryTrack, Task, subscriber, session, Resend, Unsubscribe, subscriberId, Preferences, Authorize, QueueVerification, baseUrl, url
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/CareerTokenProtector.cs`

- Lines: 45
- Extracted symbols: expires, payload, separator, parts, key, Encode, padded
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/Interface/ICareerEngagementService.cs`

- Lines: 22
- Extracted symbols: ICareerEngagementService, SaveJob, RemoveSavedJob, MergeAnonymousSaves, FollowCompany, UnfollowCompany, DeleteAlert, Track, UpdateVisitorPreference
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/Interface/ICareerNotificationService.cs`

- Lines: 9
- Extracted symbols: ICareerNotificationService, HandleJobSaved, ProcessNext
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/Interface/ICareerSubscriptionService.cs`

- Lines: 16
- Extracted symbols: ICareerSubscriptionService, Subscribe, Resend, Unsubscribe, Authorize
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/Interface/IPublicCompanyProfileService.cs`

- Lines: 16
- Extracted symbols: IPublicCompanyProfileService, UpdateCompanyTheme
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/CareerPortal/PublicCompanyProfileService.cs`

- Lines: 366
- Extracted symbols: IMongoCollection, PublicCompanyProfileService, Task, companies, resolvedCompanies, resolved, companyIds, profiles, profileMap, code, company, profile, lengthError, urlError, now, existing, socialLinksResult, socialValues, urls, invalidAbsolute, shortValues, richValues
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/ApplicantServices.cs`

- Lines: 630
- Extracted symbols: IMongoDbRepository, ApplicantServices, RegisterApplicants, result, companyId, isApplicantExist, applicant, LogNewApplication, SendApplicationAlertToRecruitmentTeam, SendEmailToApplicant, vacancy, log, UpdateApplicants, actor, previousActivityType, AddStatusHistory, name, ApplyNowService
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs`

- Lines: 25
- Extracted symbols: IApplicantsService, RegisterApplicants, ApplyNowService, UpdateApplicants, AddComment, UploadResume
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs`

- Lines: 14
- Extracted symbols: IJobVacancy, GetVacancyById, DeleteJobVacancy
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/Interface/IPublicCareerService.cs`

- Lines: 16
- Extracted symbols: IPublicCareerService
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/JobVacancyService.cs`

- Lines: 248
- Extracted symbols: IMongoDbRepository, JobVacancyService, Task, vacancy, saved, editedJob, jobList, count, jobIds, applicationCounts, data, result, GetVacancyById, DeleteJobVacancy, BuildUniqueSlug, baseSlug, slug, counter
- Use: follow the controller delegation into this file when the symbol matches the action.

### `Codeji.CMS.Services/Recruitments/PublicCareerService.cs`

- Lines: 594
- Extracted symbols: IMongoCollection, PublicCareerService, Task, companies, code, cf, companyFilter, company, normalizedCompanyCode, f, filter, BsonRegularExpression, locations, slug
- Use: follow the controller delegation into this file when the symbol matches the action.

