# Codeji CMS Backend Core — Complete Project Documentation

> Code-aligned project guide for the repository snapshot inspected on 2026-07-23.
>
> This document describes the implemented backend, its modules, dependencies, request flows, persistence model, security model, background processing, setup, testing, and important limitations. It is not a product-requirements document. Where intended behavior and current code differ, the current implementation is called out.

## 1. Project purpose

Codeji CMS Backend Core is a multi-tenant employee and HR management backend. It supports:

- company registration and company-level configuration;
- authentication, password recovery, refresh tokens, and logout;
- role-based and module-permission-based authorization;
- employee records, profile data, skills, education, certifications, work history, invitations, and notifications;
- attendance entry, status settings, weekly offs, penalties, exception review, and monthly attendance locking;
- leave policies, balances, requests, approval, accrual, and attendance reconciliation;
- salary structures, payroll generation, review, processing, employee payroll views, and PDF payslips;
- recruitment vacancies, applicants, resumes, comments, process logs, and recruitment reporting;
- departments, job titles, custom attributes, company policies, and versioned policy documents;
- calendar events, holidays, notice-board posts, dashboard aggregations, real-time notifications, and chat.

The solution is API-only. A frontend, identity provider, object store, job scheduler, and production deployment environment are not included in this repository.

## 2. Technology stack

| Area | Implementation |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| API style | Attribute-routed REST-like controllers |
| Database | MongoDB |
| Authentication | JWT bearer tokens |
| Authorization | ASP.NET authorization plus custom role/module-permission checks |
| Mapping | Mapster |
| API discovery | Swagger/OpenAPI (debug configuration only) |
| Real-time | ASP.NET Core SignalR |
| Email | Email helper/configuration plus persisted mail templates and email logs |
| Background processing | `BackgroundService` hosted services and an in-memory priority queue |
| File storage | Local `Uploads` directory, publicly served below `/fs` |
| HTML safety | `HtmlSanitizer` through the custom sanitizer utility |
| PDF | HTML-template-based payslip PDF generation |
| Testing | xUnit service tests |
| CI/CD | Azure-style pipeline in `backend-pipeline.yml` |

The SDK version is pinned through `global.json`. NuGet sources are configured by `nuget.config`.

## 3. Solution and project structure

```text
CodejiCMSCore.sln
├── Codeji.CMS.API
│   ├── Controllers/               HTTP endpoints
│   ├── App_Start/                auth, permissions, exception and CSRF middleware
│   ├── Notification/             notification SignalR hub and sender
│   ├── ChatHub/                  chat SignalR hub
│   ├── Templates/                HTML payslip template
│   └── Program.cs                composition root and middleware pipeline
├── Codeji.CMS.Services
│   ├── Account/                  login and security-token workflows
│   ├── Employees/                employee and role business logic
│   ├── Companies/                tenant and company-master logic
│   ├── Attendance/               daily attendance and payroll-readiness logic
│   ├── LeaveManagement/          leave policy, balance, and request logic
│   ├── PayRoll/                  payroll calculation, generation, and publication
│   ├── Recruitments/             vacancies and applicants
│   ├── Calendar/                 holidays and events
│   ├── NoticeBoard/              company notices
│   ├── Dashboard/                dashboard aggregations
│   ├── BackgroundTasks/          queue and scheduled hosted services
│   └── Registration/             service dependency registration and Mapster setup
├── Codeji.CMS.Repository
│   ├── Entities/                 MongoDB document types
│   ├── Repositories/             generic and specialized repositories
│   ├── Interfaces/               repository/entity capability contracts
│   ├── Domain/                   Result, paging, and filtering wrappers
│   └── Registration/             repository dependency registration
├── Codeji.CMS.DTO                API request/response contracts
├── Codeji.CMS.Utility            shared context, JWT, email, templates, enums, sanitization
├── Codeji.CMS.Migrations         ordered MongoDB seed/backfill/data-fix runner
├── Codeji.CMS.Services.Tests     service and business-rule tests
└── docs                          focused design, flow, and audit documents
```

### Project dependency direction

```text
API
├── Services
├── Repository
├── DTO
└── Utility

Services
├── Repository
├── DTO
└── Utility

Repository
└── shared generic-repository package/project references

Migrations
└── Repository entities + MongoDB driver
```

Controllers should stay thin: validate transport concerns, obtain current context, invoke a service, and return the service result. Business decisions belong in services. MongoDB access belongs in repositories.

## 4. Application startup and request lifecycle

`Codeji.CMS.API/Program.cs` is the composition root.

### Startup sequence

1. Register enum serialization for notification preference values.
2. Add controllers and the `codeji` CORS policy.
3. Configure Swagger with bearer-token support.
4. Configure forwarded headers.
5. Register SignalR, notification services, antiforgery support, task queues, and hosted services.
6. Parse `ConnectionStrings:mongodb`; the URI must include a database name.
7. Register a singleton `IMongoClient` and scoped `IMongoDatabase`.
8. initialize the static configuration helper.
9. Register business services, repositories, Mapster, HTTP clients, PDF support, attendance, salary, payroll, and company services.
10. Configure JWT bearer authentication and the `AdminOnly` authorization policy.
11. Ensure the runtime `Uploads` directory exists.
12. Build and run the middleware pipeline and map controllers/hubs.

Some services are registered more than once in `Program.cs` and service-registration extensions. The last compatible registration is what dependency injection resolves for a single service; duplicate scoped registrations should be cleaned up to reduce ambiguity.

### Middleware pipeline

The effective request path is:

```text
Request
  → global exception middleware
  → Swagger (debug only)
  → CORS
  → static files under /fs
  → HTTPS redirection
  → JWT authentication
  → authorization
  → antiforgery middleware
  → SignalR hub or controller endpoint
  → forwarded-header/security-header middleware registered later in the pipeline
  → response
```

Important ordering note: forwarded-header processing is registered after endpoint mapping in the current code. In conventional ASP.NET deployments it should run near the beginning, before components that consume scheme, host, or client IP.

The final middleware adds HSTS, frame denial, MIME sniffing prevention, a same-origin referrer policy, a restrictive CSP, and removes common server-identification headers. Because it is placed after mapped endpoints, verify in integration tests that every controller and hub response actually receives these headers.

### CORS and static files

Credentialed CORS is allowed for `localhost` and hosts ending in `.codeji.in`. Static files are served separately from the runtime `Uploads` directory at `/fs`. Static-file responses manually mirror the origin allow-list because static-file middleware can terminate the pipeline.

## 5. Shared runtime context and multi-tenancy

The system uses `CompanyId` as its tenant key. Authenticated requests normally obtain it from the JWT `company_id` claim. Current user and role are similarly read from claims through `CurrentContext`.

Typical service usage:

```text
controller
  → CurrentContext.UserId(...)
  → CurrentContext.CompanyId(...)
  → CurrentContext.UserRoleId(...)
  → tenant-aware service/repository operation
```

Entities deriving from `BaseClass` carry:

- `CompanyId`;
- creation and update timestamps;
- creator and updater IDs;
- `IsDeleted`.

The generic repository can stamp audit/tenant values and apply tenant and soft-delete filters. This is convenient, but it is not a database-enforced security boundary:

- direct `GetCollection()` access bypasses repository defaults;
- some query APIs have flags that disable default filters;
- anonymous endpoints must obtain a trustworthy company context without a JWT;
- global lookup collections intentionally do not use company filtering.

Every new query must be reviewed for tenant scoping. Public recruitment calls are especially sensitive because vacancy listing, application submission, and resume upload are anonymous and rely on request context to establish the company.

## 6. Authentication and account module

### Main components

| Component | Location |
|---|---|
| Controller | `Codeji.CMS.API/Controllers/AccountController.cs` |
| Service | `Codeji.CMS.Services/Account/AccountServices.cs` |
| Contract | `Codeji.CMS.Services/Account/Interface/IAccountServices.cs` |
| JWT helper | `Codeji.CMS.Utility/Helpers/TokenHelper.cs` |
| JWT configuration | `Codeji.CMS.Utility/Helpers/JwtSettingModel.cs` |
| Context extraction | `Codeji.CMS.Utility/middlewares/CurrentContext.cs` |
| Entities | `EmpUser`, `RefreshToken`, `UserSecurityToken` |

### API surface

| Method and route | Access | Purpose |
|---|---|---|
| `GET api/account/antiforgerytoken/{appKey}` | Anonymous | Issues antiforgery data |
| `GET api/app/checkAppVersion` | Anonymous | Checks client application version |
| `POST api/account/register` | Controller default | Registers a company and initial account |
| `POST api/account/login` | Anonymous | Verifies credentials and returns access/refresh tokens |
| `POST api/account/refresh-token` | Anonymous | Rotates/renews tokens |
| `POST api/account/logout` | Authenticated | Revokes/deletes refresh-token state |
| `POST api/account/getSignedUserDetails` | Controller default | Returns the signed-in user's view |
| `POST api/account/CreateNewPassword` | Anonymous | Consumes a security token and creates a password |
| `GET api/account/verify-email` | Anonymous | Verifies an email token |
| `POST api/account/ResetPassword` | Anonymous | Starts password recovery |
| `POST api/applicant/applyJob` | Anonymous | Public recruitment application |

### Login flow

```text
email/password
  → AccountController.Login
  → AccountServices.VerifyAndGenerateToken
  → load active employee
  → verify stored password hash
  → load role/company context
  → create signed JWT claims
  → create and persist refresh token
  → return TokenResponseDto
```

The JWT contains a subject/JTI and application claims including user, company, role ID, and role names. JWT validation checks issuer, audience, lifetime, signing key, and the configured symmetric secret.

### Refresh and logout flow

```text
access token + refresh token
  → locate persisted refresh token
  → validate ownership, expiry, and state
  → issue replacement token pair
  → persist new refresh state
```

Logout operates on the presented refresh token and user ID. Token revocation depends on persisted refresh-token state; already-issued access tokens remain valid until their short expiry unless another revocation check is introduced.

### Password reset and email verification

Security links use `UserSecurityToken` documents containing a hash, expiry, token type, used flag, creation time, and optional usage time. The raw token is sent to the user; its stored representation is hashed. Consumption verifies validity and marks the token used.

### Authentication dependencies

- `EmpUser` is the credential and employee identity source.
- `Roles` determines role identity and app access.
- `RefreshToken` supports session continuation.
- `UserSecurityToken` supports one-time email/password workflows.
- email templates, `IMiddlewareService`, and the background queue support outbound links.
- company configuration supplies URLs and tenant state.

## 7. Authorization, administrator behavior, and permissions

Authentication answers “who is calling?” Authorization is implemented at three levels:

1. `[Authorize]` requires a valid JWT.
2. `[Authorize(Policy = "AdminOnly")]` uses `RoleRequirement` and `RoleHandler`.
3. `[ModulePermission(AppModule.X, Permission.Y)]` checks the caller's role permission for a module/action.

### Permission data model

```text
Module
  1 ── * ModulePermission * ── 1 Permission
                         |
                         * RolePermission * ── 1 Roles
```

- `Module`: global feature catalog such as Employees or Attendance.
- `Permission`: global action catalog, normally View/Create/Edit/Delete.
- `ModulePermission`: global pairing of one module with one permission.
- `Roles`: company-scoped role definition and assigned users.
- `RolePermission`: company/role-specific access decision for a module-permission pair.

`ModulePermissionAttribute` reads current role and company context, resolves the configured module/permission, and rejects missing or inaccessible grants.

### Administrator scope

“Administrator” is a role/policy concept, not a separate admin service. Admin-only endpoints manage company data, departments, job titles, custom attributes, modules, attendance settings, and sensitive employee/role operations. A user may also be authorized through granular module permissions without being an Administrator where the controller uses `ModulePermission`.

### Seeded modules

Migrations seed or add modules for Employees, Attendance, Leave Management, Calendar, Notice Board, Jobs, Applications, Process Log, Payroll, Payroll Settings, and Policy. Default roles and grants are created/backfilled by migrations and company seeding.

### Authorization caveats

- Controller-level `[Authorize]` does not imply a business-module permission.
- Some authenticated read endpoints only require authentication.
- Global repositories and raw Mongo collection access require explicit tenant checks.
- Adding an enum value alone does not create database module/permission records; a migration and role backfill are required.

## 8. Company and company-master modules

### Responsibilities

- register and maintain tenant/company details;
- upload and expose a company logo;
- seed default departments, job titles, roles, and permissions;
- manage department and job-title catalogs;
- manage configurable employee custom attributes;
- enable/disable company modules;
- create policies and versioned policy documents.

### Main components

| Area | Controller | Service | Entities |
|---|---|---|---|
| Company | `CompanyController` | `CompanyService` | `Company`, `Policy`, `PolicyVersion` |
| Company master | `CompanyMasterController` | `CompanyMasterService` | `Department`, `JobTitles`, `CustomAttributes`, `CustomAttributeValue` |
| Defaults | — | `DefaultCompanySeeds` | seed definitions |

### Company registration flow

```text
registration request
  → validate company/account input
  → create Company
  → create initial administrator employee
  → seed company defaults
      ├── roles and role permissions
      ├── departments
      └── job titles
  → start verification/invitation workflow
```

Default department/job-title labels are selected using the company's application language.

### Company API

- company list, details, and update are administrator-only;
- policy CRUD uses `Policy` module permissions;
- policy-version create/edit/delete uses matching policy permissions;
- policy documents are returned as files after tenant and permission validation.

### Company-master API

- departments: add/update, list, delete;
- module access: toggle module availability and return affected roles/users;
- modules: list complete module/access information;
- job titles: add/update, list, delete;
- custom attributes: create a shell, list, fetch, update values/configuration, and delete a value.

Departments, job titles, custom attributes, policies, and versions are company-scoped dependencies used by employees, notices, recruitment labels, and dashboards.

## 9. Employee module

### Responsibilities

- list, search, create/invite, import, update, activate/deactivate, and remove employees;
- expose signed-in employee details;
- maintain personal data and employment summary;
- maintain education, certification, skills, and work history;
- manage profile images and password changes;
- provide notifications and notification preferences;
- maintain real-time presence for chat;
- trigger birthday and work-anniversary notifications.

### Components

| Component | Location |
|---|---|
| Controller | `Codeji.CMS.API/Controllers/UserController.cs` |
| Service | `Codeji.CMS.Services/Employees/EmployeeService.cs` |
| Contract | `IEmployeeService.cs` |
| Presence | `EmployeePresenceService.cs` |
| Entities | `EmpUser`, `EmpSummary`, `EmpEducationDetails`, `EmpCertificationDetails`, `EmpSkills`, `EmpWorkHistory` |
| Supporting entities | `Skills`, `Notification`, `UserNotifications`, `NotificationPreference` |

### Employee aggregate

`EmpUser` is the root identity and employment record. Related collections are linked by user ID:

```text
EmpUser
├── EmpSummary
├── EmpEducationDetails[]
├── EmpCertificationDetails[]
├── EmpSkills[]
├── EmpWorkHistory[]
├── Role / reporting manager / department / job title references
├── salary structure and payroll records
├── leave balances and requests
├── attendance records
└── notifications and preferences
```

The data is document-oriented rather than transactionally joined. Services perform application-side lookups and composition.

### Invitation flow

```text
authorized administrator/HR
  → validate company, email, employee ID, role, manager, department/job title
  → create inactive or invitation-pending EmpUser
  → create one-time security token
  → render invitation email
  → queue/send and log email
  → employee follows link
  → sets password and verifies account
  → account becomes usable
```

The service can resend an invite and obtain the next/last employee ID. Bulk import performs row-level processing and returns successes/errors through the bulk response DTO.

### Profile flows

Self-service and administrative edit DTOs are distinct in parts of the API. Profile image upload stores a local file and records its path. Education, certification, skills, and work history use separate CRUD-like service methods and collections.

### Employee dependencies

- Company must exist and be active.
- Role controls application and module access.
- Department, job title, reporting manager, and custom-attribute values must belong to the company.
- Leave, attendance, salary, payroll, dashboards, notices, chat, and celebrations all depend on `EmpUser`.

### Notifications

A notification definition can be linked to many `UserNotifications` rows. Per-user rows track read/deleted state. Preferences are a dictionary keyed by `NotificationPreferenceType`; missing settings fall back to defaults from middleware service. The employee service supports paging, mark-one-read, mark-all-read, and preference updates.

## 10. Roles module

`RolesController` and `RoleServices` implement company-scoped role administration:

- retrieve roles and role details;
- create or update roles;
- assign module permissions;
- check role usage;
- delete and optionally reassign affected employees;
- support default/non-editable roles.

Role changes affect authorization immediately when the permission attribute reads persisted grants, but role claims embedded in an existing JWT may remain stale until token renewal. Deleting a role must account for its `UserRoles`/employee assignments, which is why the API includes a delete-and-reassign request.

## 11. Attendance module

### Responsibilities

- administrator-created manual attendance;
- attendance retrieval by employee/date/range;
- guarded edits;
- configurable attendance status codes;
- configurable weekly offs;
- late/early/missing-attendance penalty policy;
- payroll exception generation and review;
- monthly validation and lock.

### Main components

| Component | Location |
|---|---|
| Daily API | `AttendanceController.cs`, base `api/admin/attendance` |
| Settings APIs | `AttendanceStatusSettingsController.cs`, `WeeklyOffSettingsController.cs` |
| Penalty/lock API | `AttendancePenaltyController.cs` |
| Service | `AttendanceService.cs` |
| Repository | `AttendanceRepository.cs` |
| Rules | `AttendancePayrollRules.cs`, `AttendanceEditGuard.cs` |
| Calendar dependency | `CompanyWorkingCalendarService.cs` |
| Leave bridge | `LeaveAttendanceReconciliationService.cs` |
| Entities | `AttendanceModel`, `AttendanceStatusSetting`, `WeeklyOffSetting`, `AttendancePenaltyPolicy`, `AttendancePayrollException`, `MonthlyAttendanceSummary` |

### Daily attendance flow

```text
Attendance/Create or Edit permission
  → resolve company and employee
  → normalize attendance date and time values
  → verify working-day/edit rules
  → prevent duplicate company + user + date record
  → calculate duration/status-dependent fields
  → persist AttendanceModel with audit data
```

Read APIs fetch all records for one employee, one employee/date, or a filtered calendar/date range.

`AttendanceModel` is the source for daily status and check-in/check-out data. The implementation recognizes compact status codes such as present/absent/half-day/leave values and allows configured status definitions.

### Working calendar

`CompanyWorkingCalendarService` combines:

- weekly-off settings;
- holidays/calendar records;
- requested date range;
- options determining whether weekends/holidays count as leave dates.

It supplies working-date lists and counts used by leave validation, attendance editing, and payroll.

### Leave reconciliation

Accepted leave is materialized into linked attendance records:

```text
leave accepted
  → select covered working dates
  → create/update attendance rows with leave source metadata
  → preserve source version for safe reversal

leave reversed/cancelled
  → find attendance rows created by that leave/version
  → restore or remove only matching source-linked effects
```

This dependency prevents approved leave from being treated as unexplained absence.

### Penalties, exceptions, and month locking

```text
attendance penalty policy
  → recalculate month
  → derive payroll-impacting exceptions
  → reviewer resolves/waives exceptions
  → validate completeness and unresolved items
  → create approved MonthlyAttendanceSummary
  → lock month against ordinary attendance edits
```

Payroll generation requires an approved and locked monthly summary. `AttendanceEditGuard` blocks edits that would mutate a locked payroll source period.

### Attendance API groups

- `api/admin/attendance`: add, employee list, employee/date, update, filtered all-items query.
- attendance status settings: read and administrator-only save.
- weekly-off settings: read and administrator-only save.
- attendance penalties: policy read/save, exception recalculate/search/review, month lock status, validate-and-lock.

For exact endpoint names and an implementation audit, see `docs/attendance-module-current-flow-and-audit.md`.

## 12. Leave management module

### Responsibilities

- define leave types/policies;
- maintain per-employee balances;
- submit, edit, list, approve/reject, and delete leave requests;
- show personal and administrative summaries;
- accrue balances monthly;
- synchronize accepted leave into attendance.

### Main components

| Component | Location |
|---|---|
| Controller | `LeaveManagementController.cs` |
| Service | `LeaveManagementService.cs` |
| Contract | `ILeaveManagementService.cs` |
| Hosted job | `LeaveAccrualHostedService.cs` |
| Entities | `LeavePolicy`, `LeaveRequest`, `EmployeeLeaveBalance` |

### Policy and balance flow

```text
company leave policy
  → leave type, entitlement, status, and accrual behavior
  → EmployeeLeaveBalance per employee/type
  → monthly LeaveAccrualHostedService adjustments
  → available balance during request validation
```

The API supports create/update/list policies, one employee's balance, bulk balance updates, and a filtered company-wide balance view.

### Leave request flow

```text
employee submits request
  → validate dates and policy
  → calculate eligible leave dates using working calendar
  → validate overlap and balance
  → save pending LeaveRequest
  → notify reviewer

reviewer changes status
  → validate transition and authority
  → update request
  → on acceptance: reconcile attendance
  → on reversal: reverse source-linked attendance
  → update/restore leave balance as applicable
  → notify employee
```

Leave requests therefore depend on employees, policies, balances, company calendar/weekly offs, notifications, and attendance.

### Leave API

- create/update/list leave policies;
- retrieve employee leave balance;
- create/update/admin-list/my-list/delete leave requests;
- patch request status;
- get request summary and monthly taken-leave summary;
- bulk update balances and search all balances.

The detailed upstream/downstream rules are documented in `docs/leave-attendance-payroll-end-to-end-flow.md`.

## 13. Salary and payroll modules

Salary structure and payroll execution are related but separate:

- Salary defines compensation terms effective for an employee.
- Payroll creates a monthly computed result using salary plus attendance inputs.

### Components

| Area | Components |
|---|---|
| Salary API | `SalaryController.cs` |
| Salary service/repository | `SalaryService.cs`, `SalaryRepository.cs` |
| Payroll API | `PayRollController.cs` |
| Automatic payroll API | `AutoPayrollController.cs` |
| Services | `PayRollServices.cs`, `AutoPayRollServices.cs` |
| Rules | `SalaryCalculator.cs`, `PayrollCalculationRules.cs`, `PayrollPeriodRules.cs`, `PayrollDivisorPolicyService.cs` |
| Scheduler | `PayRollHostedServices.cs` |
| PDF | `PdfService.cs`, `Templates/SalarySlipTemplate.html` |
| Entities | salary model/document, `EmpPayRoll`, `PayrollDivisorPolicy` |

### Salary structure

The salary model contains:

- basic pay;
- HRA and other allowances;
- LTA and bonus;
- health insurance and deductions;
- gross salary, net salary, and CTC;
- effective-from/effective-to;
- payment frequency, currency, and active status.

Salary APIs create, update, retrieve by employee, filter company salary records, and delete/deactivate as implemented by the controller/service.

### Payroll readiness and calculation flow

```text
active employee
  + effective salary structure
  + company payroll divisor policy
  + approved and locked monthly attendance summary
  + resolved attendance payroll exceptions
  → payroll calculation rules
  → earnings and deduction breakdown
  → EmpPayRoll draft
```

The divisor policy determines how monthly compensation is prorated (for example calendar-day versus working-day basis, according to configured DTO/entity values). Period rules normalize a requested payroll month and prevent invalid/future/duplicate processing scenarios.

### Payroll lifecycle

```text
Generate
  → validate company/month lock
  → calculate employee payroll drafts
  → HR retrieves and reviews payroll
  → update allowed payroll components/status
  → process/publish payroll
  → employee can retrieve own payroll
  → render HTML payslip
  → generate PDF bytes
```

Employee visibility is status-gated. A draft should not become an employee-visible payslip until it is processed/published.

### Automatic payroll

`AutoPayrollServices.GeneratePayrollForMonthAsync` can generate a company's month in bulk. `PayrollHostedService` invokes scheduled payroll behavior. Because hosted services are in-process, running multiple API instances can execute the same schedule concurrently unless database idempotency/uniqueness prevents duplicates.

### PDF dependencies

Payslip generation loads the HTML template at runtime, replaces template placeholders, converts amounts (including Indian currency words), and passes HTML to `PdfService`. Template files are configured to copy to the build output.

### Critical dependency chain

```text
Leave policy/balance
  → accepted leave
  → reconciled attendance
  → attendance exception review
  → monthly attendance lock
  → payroll draft
  → payroll processing
  → employee payslip
```

See `docs/leave-attendance-payroll-end-to-end-flow.md` and `docs/lhd-ed-payroll-implementation-impact.md` for deeper payroll-rule details.

## 14. Recruitment module

### Scope

Implemented recruitment covers vacancy CRUD, public applications, applicant management, PDF resume upload, comments/process logs, stage email notifications, and dashboard stage counts.

It does not implement interviews, scorecards, offers, approvals, onboarding conversion, or a fully enforced recruitment state machine.

### Components

| Area | Components |
|---|---|
| Vacancy | `JobVacancyController`, `JobVacancyService`, `JobVacancy` |
| Applicants | `ApplicantsController`, `ApplicantServices`, `Applicant`, `ApplicantLogs` |
| Public apply | `AccountController.RegisterApplicants` |
| Resume | local `Uploads/Resume` storage |
| Email | `MailTemplate`, priority queue, middleware email sender/log |
| Reporting | `DashboardServices.GetApplicationStatusData` |

### Vacancy flow

```text
Jobs/Create
  → create company-scoped JobVacancy
  → edit or activate/deactivate
  → anonymous company-scoped vacancy listing
  → application counts added per vacancy
  → soft/hard deletion behavior through repository service
```

Vacancies contain title, vacancy count, job type, status, and description.

### Candidate application flow

```text
public candidate
  → POST api/applicant/applyJob with company context
  → check prior applicant by email and six-month rule
  → insert active Applicant at New stage
  → load vacancy and acknowledgement template
  → enqueue acknowledgement email
  → upload resume separately using applicant email
```

Applicant data includes identity/contact fields, vacancy reference, state, experience, resume URL, pipeline activity type, and active status.

### HR applicant flow

```text
Applications/View
  → filter/page candidates and resolve vacancy names

Applications/Create
  → create candidate
  → add initial applicant log
  → queue acknowledgement

Applications/Edit
  → edit profile/vacancy/stage/status
  → optionally send New/Selected/Rejected email

Applications/Edit
  → add comment/process-log row
```

Pipeline values include New, In Progress, On Hold, Shortlisted, Selected, Rejected, and ReApply. Current code accepts direct enum updates rather than validating a transition graph.

### Recruitment limitations and security concerns

- Anonymous tenant isolation depends on correctly populated company context.
- Resume replacement is anonymous and identifies the applicant by email.
- File checking is primarily extension/size based and should verify PDF content and exact byte limits.
- Public applications do not create the same initial process log as HR-created applicants.
- Stage edits do not create a complete immutable old/new-value audit trail.
- Vacancy existence, active state, tenant ownership, and remaining capacity need stronger validation during public apply.
- Applicant result joins can omit applicants whose vacancy is missing while counts were computed earlier.
- Email queueing is in-memory, not a durable outbox.

The complete audit and target remediation flow are in `docs/recruitment-module-current-flow-and-audit.md`.

## 15. Notice board module

### Components

- `NoticeBoardController`;
- `NoticeBoardServices`;
- `Notice` entity;
- add/update/filter/view DTOs;
- `Sanitizer`.

### Flow

```text
authorized author
  → sanitize title/message properties
  → select target and optional departments
  → create company-scoped Notice
  → employees request visible notices
  → service filters by target/department/type/date
  → view status is recorded
  → author can inspect “my notices” and viewers
```

The API supports post, update, list visible notices, get one notice, list notices created by the current user, and delete. `NoticeType`, target, departments, date range, poster, and paging drive filtering.

Dependencies include employees (author/viewers), departments (audience), company context, module permissions, and HTML sanitization.

## 16. Policy module

Policies are company-scoped categories with separately versioned documents:

```text
Policy
└── PolicyVersion[]
    ├── version metadata
    ├── effective/publication information
    └── uploaded policy document path
```

Flow:

1. A user with `Policy/Create` creates a policy.
2. A user adds a version through multipart form data.
3. The service validates the parent policy/company and saves the document.
4. Later edits add/update version metadata and possibly the file.
5. Authorized employees list policies/versions and download a selected version.
6. Delete operations validate tenant ownership.

The policy module depends on local file storage, company context, employee identity/audit fields, and migration-seeded Policy permissions. It is distinct from leave and attendance “policies,” which use their own entities and APIs.

## 17. Calendar and holiday module

`CalendarController` and `CalendarServices` manage company events and holidays.

### Flow

```text
Calendar Create/Edit
  → multipart calendar DTO
  → event or holiday record
  → optional image in local storage
  → recurring flag controls yearly interpretation
  → filtered calendar response
```

The combined calendar response can include holidays, events, birthdays, and work anniversaries. Birthdays/anniversaries are derived from employees, whereas administrator-created items use `CalendarEntity`/holiday data.

The working-calendar attendance service consumes holiday information, so calendar changes can affect leave-day counting, attendance eligibility, and downstream payroll readiness.

## 18. Dashboard module

Dashboard endpoints are authenticated read aggregations:

- employee counts grouped by gender;
- employee counts grouped by department;
- applicant totals grouped by recruitment activity stage, optionally by vacancy;
- upcoming holidays and events;
- upcoming birthdays and work anniversaries.

`DashboardServices` depends on employee, department, applicant, vacancy/calendar, and holiday collections. These are current-state aggregations rather than immutable historical analytics. Tenant filtering must remain enabled on every contributing query.

## 19. Real-time notifications and chat

### Hubs

| Hub | Route | Purpose |
|---|---|---|
| `NotificationHub` | `/notificationhub` | server-to-user notification delivery |
| `ChatHub` | `/chathub` | employee chat and online-presence behavior |

SignalR clients pass the JWT through `access_token` in the query string for these hub paths. The JWT bearer handler extracts it only for `/notificationhub` and `/chathub`.

`GetUserIdProvider` maps claims to the SignalR user identity. `EmployeePresenceService` tracks connections grouped by application/company context and returns online users. Presence is in-memory; it is instance-local and resets on process restart. Multi-instance production needs a SignalR backplane and distributed presence store.

## 20. Background processing

### Priority queue

`PriorityTaskQueue` accepts `Func<CancellationToken, Task>` items with an integer priority. `PriorityQueuedHostedService` dequeues and executes them. It is used for non-blocking email/notification work.

Properties:

- fast and simple;
- in-memory only;
- work is lost on process termination;
- no durable retry/dead-letter record;
- multiple API instances have independent queues.

### Scheduled hosted services

| Service | Responsibility |
|---|---|
| `LeaveAccrualHostedService` | monthly employee leave accrual |
| `BirthDayAndAnniversaryNotificationHostedServices` | celebration notifications |
| `PayrollHostedService` | monthly payroll generation behavior |

Hosted jobs must be idempotent because application restarts and multi-instance execution can produce repeated attempts.

## 21. Persistence and repository design

### Generic repository

`MongoRepository<TEntity>` exposes:

- composable and materialized reads;
- first/existence/count operations;
- single/bulk inserts;
- whole-document update;
- field-level bulk update;
- hard delete;
- aggregate projection and paging;
- raw Mongo collection access.

It uses entity interfaces/reflection to support IDs, auditing, filtering, and soft deletion. Collection names are derived by the repository/context conventions.

### Specialized repositories

- `AttendanceRepository` adds attendance-specific queries.
- `SalaryRepository` adds salary-specific operations/projections.

### Update semantics

The generic `Update` operation replaces the document. Callers must load/preserve fields that should survive an update. Prefer `UpdateMany` with explicit field setters for patch behavior and concurrency-sensitive changes.

### Result model

Services normally return `Result` or `Result<T>`, carrying success/failure information, messages/status, payload (`MethodResults`), and paging information where applicable. Controllers sometimes return `IActionResult` for file/HTTP-specific responses.

### Core entity catalog

| Domain | Main documents |
|---|---|
| Identity | `EmpUser`, `RefreshToken`, `UserSecurityToken` |
| Employee profile | `EmpSummary`, `EmpEducationDetails`, `EmpCertificationDetails`, `EmpSkills`, `EmpWorkHistory`, `Skills` |
| Company | `Company`, `Department`, `JobTitles`, `CustomAttributes`, `CustomAttributeValue` |
| Authorization | `Roles`, `RolePermission`, `Module`, `Permission`, `ModulePermission` |
| Attendance | `AttendanceModel`, `AttendanceStatusSetting`, `WeeklyOffSetting`, `AttendancePenaltyPolicy`, `AttendancePayrollException`, `MonthlyAttendanceSummary` |
| Leave | `LeavePolicy`, `LeaveRequest`, `EmployeeLeaveBalance` |
| Compensation | salary document/model, `EmpPayRoll`, `PayrollDivisorPolicy` |
| Recruitment | `JobVacancy`, `Applicant`, `ApplicantLogs`, `Resume`, `MailTemplate` |
| Content | `Notice`, `Policy`, `PolicyVersion`, `CalendarEntity`, `Holidays` |
| Notifications | `Notification`, `UserNotifications`, `NotificationPreference`, `EmpEmailLogs` |
| Operations | `Migration` |

### Indexing

Migrations create/harden indexes for tenant-sensitive HR data and attendance/payroll/policy paths, including attendance uniqueness and company-oriented access. New high-volume queries should be reviewed with Mongo explain plans and backed by compound indexes beginning with `CompanyId` where appropriate.

## 22. File, email, HTML, and PDF infrastructure

### File storage

Uploads are written beneath the API process working directory:

```text
Uploads/
├── CompanyLogo/
├── Profile/
├── Resume/
└── policy/calendar-related paths as used by services
```

Files are exposed through `/fs`. Database documents generally store a filename or relative/public URL.

Production implications:

- files disappear if the container/host filesystem is ephemeral;
- multiple API instances do not share files;
- public paths require careful authorization for sensitive policy/resume documents;
- database update and filesystem operations are not transactional;
- antivirus/content validation and object-storage migration are advisable.

### Email

`IMiddlewareService` renders/sends email and saves `EmpEmailLogs`. `MailTemplate` records are keyed by `MailType` and use placeholder replacement through `HtmlTemplate`. Application services enqueue many emails to the priority queue.

Configuration supplies sender/support/BCC/API settings. Never commit real secrets; use environment variables or a secret manager.

### Sanitization

The custom sanitizer permits a restricted rich-text tag/attribute set. Notice content uses it explicitly. Any new user-authored HTML must be sanitized on the server and safely rendered by clients.

### PDF

Payslips are generated from `SalarySlipTemplate.html`. Runtime template copy behavior is declared in the API project file. PDF generation should be tested in the deployment image because browser/rendering dependencies can differ from development machines.

## 23. Configuration reference

Configuration is read from `appsettings.json`, environment-specific files, environment variables, and deployment overrides.

Expected groups include:

| Group/key | Purpose |
|---|---|
| `ConnectionStrings:mongodb` | MongoDB URI including database name |
| `AppSettings:isForDebug` | Swagger/debug behavior |
| `AppSettings:AppVersion` | client version comparison |
| `AppSettings:APIUrl` | JWT issuer and backend links |
| `AppSettings:AppUrl` | JWT audience and frontend links |
| `Jwt:SecretKey` | access-token signing secret |
| `Jwt:Expiry` | access-token lifetime in minutes |
| `Jwt:RefreshTokenExpiry` | refresh-token lifetime in days |
| email settings | provider key, from address, support/BCC values |
| reCAPTCHA settings | CAPTCHA verification secret/config |
| file settings | upload roots and public URL/path conventions |

Do not copy repository example credentials into documentation, commits, tickets, or production. Rotate any secret that has ever been committed.

## 24. Database migrations

The migration executable is independent from API startup.

### Runner design

```text
MigrationLoader
  → reflect all IMigration implementations
  → sort by string Id
  → MigrationRunner checks Migration collection
  → execute unapplied migration
  → record Id + ExecutedAt
```

Migration IDs use sortable date/name prefixes. Migrations seed modules/permissions/templates/default skills and roles, add new modules, create indexes, backfill company IDs, normalize attendance, repair assignments/test attendance, and revoke/restore permission sets.

### Running migrations

From the repository root:

```powershell
dotnet run --project Codeji.CMS.Migrations
```

Run against a backed-up/staged database first. The runner has no automatic rollback. Every migration must be idempotent not only after success, but also after partial execution.

### When a migration is required

- adding a property with a safe C# default often needs no Mongo schema migration;
- backfilling existing documents does;
- adding a global module/permission/template does;
- adding a unique or query-critical index does;
- correcting stored values does.

See `docs/migrations.md` for migration-by-migration detail.

## 25. Build, run, and test

### Prerequisites

- .NET SDK matching `global.json`;
- accessible MongoDB instance;
- valid non-committed configuration/secrets;
- any PDF-renderer runtime dependencies;
- optional frontend/SignalR client for end-to-end use.

### Restore and build

```powershell
dotnet restore CodejiCMSCore.sln
dotnet build CodejiCMSCore.sln
```

### Configure locally

1. Copy the development settings template to a local ignored settings file if required by the environment.
2. Set the Mongo connection URI with a database name.
3. Set JWT issuer, audience, and a strong secret.
4. Set email/file settings or disable external integrations for local work.
5. Run migrations.

Prefer .NET user secrets or environment variables:

```powershell
$env:ConnectionStrings__mongodb = "mongodb://localhost:27017/codeji-cms-dev"
$env:Jwt__SecretKey = "<strong-local-development-secret>"
```

### Run the API

```powershell
dotnet run --project Codeji.CMS.API
```

When debug configuration is enabled, Swagger is available at the runtime Swagger UI path. Launch URLs/ports are defined in `Properties/launchSettings.json`.

### Run tests

```powershell
dotnet test CodejiCMSCore.sln
```

Current tests cover account authentication, role permissions, attendance/payroll rules, payroll calculation rules, and payroll period rules. The suite is not a complete integration test of MongoDB, files, hosted jobs, SignalR, tenant isolation, or all controllers.

## 26. Testing strategy and missing coverage

Recommended layers:

1. Pure unit tests for calculators, period rules, transitions, and validators.
2. Service tests with isolated repositories for success/failure paths.
3. MongoDB integration tests for default tenant filters, unique indexes, soft delete, and update semantics.
4. API tests for JWT, permissions, antiforgery, file upload/download, and response status.
5. End-to-end tests for leave → attendance → lock → payroll → payslip.
6. Security tests for cross-company IDs and anonymous recruitment flows.
7. hosted-job idempotency and restart tests.

High-priority missing scenarios:

- every endpoint denies cross-company document IDs;
- public recruitment fails closed without a valid tenant;
- resume/policy files cannot be replaced or read by unauthorized users;
- concurrent leave approval or payroll generation does not double-apply data;
- locked attendance cannot be changed through any alternate service path;
- duplicate company/user/date attendance is rejected at the database level;
- role changes and token refresh behave consistently;
- notification/email job failures are visible and retryable.

## 27. Operational and security considerations

### Strong parts already present

- JWT issuer/audience/lifetime/signature validation;
- hashed passwords and hashed one-time security tokens;
- role and granular module permissions;
- tenant/audit fields and repository defaults;
- soft-delete support;
- exception middleware and standard result wrappers;
- CORS allow-list;
- security headers;
- HTML sanitization;
- attendance lock before payroll;
- migration tracking and several tenant/index hardening migrations.

### Risks to address

1. Ensure public company context is cryptographically/trustfully resolved and fails closed.
2. Replace email-only anonymous resume authorization with an expiring application token.
3. Move sensitive/local uploads to protected object storage with scanning.
4. Move queued email/notifications to a durable outbox/worker.
5. add rate limiting to login, reset, apply, resume, and other public endpoints.
6. Add health/readiness checks for MongoDB, storage, mail, and background workers.
7. Correct forwarded/security middleware ordering and verify headers.
8. Coordinate scheduled jobs across multiple instances.
9. Add optimistic concurrency/version fields for critical leave, attendance, applicant, role, and payroll edits.
10. Audit raw collection access and every `withDefaultFilter: false` use.
11. Avoid exposing secrets in tracked appsettings and rotate any historical credentials.
12. Add structured logs with correlation IDs while excluding tokens, passwords, payroll details, and PII.

## 28. Cross-module dependency map

```text
Company
├── Roles & Permissions
│   └── Employee authorization
├── Departments / Job Titles / Custom Attributes
│   ├── Employees
│   ├── Notices
│   └── Dashboard
├── Employees
│   ├── Attendance
│   ├── Leave balances and requests
│   ├── Salary and Payroll
│   ├── Notifications / Chat
│   └── Celebrations
├── Calendar / Weekly Off
│   ├── Leave date calculation
│   ├── Attendance working-day rules
│   └── Payroll readiness
├── Leave
│   └── Attendance reconciliation
├── Attendance
│   └── Monthly lock and Payroll input
├── Salary
│   └── Payroll calculation
├── Policies
│   └── Versioned company documents
└── Recruitment
    ├── Vacancies
    ├── Applicants / resumes / process logs
    └── Dashboard stage counts
```

### Most important transactional flow

```text
Employee requests leave
  → manager approves
  → balance is consumed
  → working dates become leave attendance
  → attendance month is reviewed
  → exceptions are resolved
  → month is locked
  → payroll is generated from salary + attendance
  → HR processes payroll
  → employee sees payslip
```

## 29. How to add a new module

1. Add repository entities with `BaseClass` when tenant/audit/soft-delete behavior is required.
2. Add request/response DTOs; do not expose persistence documents unintentionally.
3. Add repository specialization only when generic operations are insufficient.
4. Add a service interface and implementation.
5. Register the service in `ServicesRegistration`.
6. Add a thin authorized controller.
7. Add an `AppModule` value if module-level authorization is needed.
8. Add a migration for the module, module-permission pairs, and per-company role grants.
9. Add Mapster mappings when manual projection is not used.
10. Add validation, tenant checks, auditing, soft-delete behavior, and indexes.
11. Add unit, integration, permission, and cross-tenant tests.
12. Update this guide and the focused module documentation.

Do not assume repository defaults protect raw collection queries. Do not add a public endpoint without an explicit tenant-resolution and abuse-prevention design.

## 30. Source-of-truth navigation

| Question | Start here |
|---|---|
| What executes at startup? | `Codeji.CMS.API/Program.cs` |
| What routes exist? | `Codeji.CMS.API/Controllers` |
| What business rules execute? | matching folder in `Codeji.CMS.Services` |
| How is data stored? | `Codeji.CMS.Repository/Entities` and `Repositories` |
| What does an endpoint accept/return? | `Codeji.CMS.DTO` |
| What permissions exist? | `ModulePermissionAttribute`, enums, and migrations |
| How is tenant context obtained? | `CurrentContext` and generic repository |
| What scheduled work runs? | `BackgroundTasks` and `PayRollHostedServices` |
| How is data upgraded? | `Codeji.CMS.Migrations/Migrations` |
| What behavior is verified? | `Codeji.CMS.Services.Tests` |

Focused companion documents:

- `docs/architecture.md`
- `docs/modules.md`
- `docs/auth-and-permissions.md`
- `docs/data-layer.md`
- `docs/cross-cutting.md`
- `docs/migrations.md`
- `docs/attendance-module-current-flow-and-audit.md`
- `docs/recruitment-module-current-flow-and-audit.md`
- `docs/leave-attendance-payroll-end-to-end-flow.md`
- `docs/lhd-ed-payroll-implementation-impact.md`
- `docs/dashboard-design.md`
- `docs/add-new-feature.md`
- `docs/conventions.md`

## 31. Final architectural summary

Codeji CMS Backend Core is a layered, MongoDB-based HR platform centered on a company tenant and employee identity. Company configuration and authorization sit at the root. Employees feed attendance, leave, compensation, notifications, dashboards, and chat. Calendar and weekly-off rules determine working days. Accepted leave alters attendance, reviewed attendance is locked, and only then can payroll be safely generated and published. Recruitment and company content modules share the same tenant, permission, file, email, and audit infrastructure.

The most important maintenance rules are:

- always preserve tenant isolation;
- treat repository defaults as assistance, not the sole security boundary;
- enforce state transitions and idempotency for leave, attendance, recruitment, and payroll;
- keep critical background work durable;
- keep files protected and externally durable;
- add migrations for stored catalog/index/backfill changes;
- test the complete cross-module flow, not only individual service methods.
