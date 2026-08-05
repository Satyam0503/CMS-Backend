# Codeji CMS Backend Core — Expanded Complete Project Documentation

> Comprehensive backend reference for the `CMS-Backend-Core` repository, covering architecture, startup flow, tenancy, modules, business workflows, cross-cutting concerns, and extension guidance.

## 1. Overview

The Codeji CMS Backend Core repository implements the server-side API for an HRMS and recruitment platform. It is built with **ASP.NET Core 10**, **MongoDB**, **SignalR**, **Mapster**, and a service-oriented architecture.

This backend serves:
- authenticated employee and HR workflows,
- company administration,
- attendance and leave management,
- payroll and salary processing,
- recruitment and public career pages,
- notifications, email, calendar, and dashboard reporting.

## 2. Solution structure

```
CodejiCMSCore.sln
├─ Codeji.CMS.API             // HTTP API, controllers, SignalR, middleware
├─ Codeji.CMS.Services        // Business services, workflows, hosted jobs
├─ Codeji.CMS.Repository      // MongoDB entities, repository implementations
├─ Codeji.CMS.DTO             // Request and response contract DTOs
├─ Codeji.CMS.Utility         // shared helpers, enums, auth, email, sanitization
├─ Codeji.CMS.Migrations      // migration runner and data/index backfills
├─ Codeji.CMS.Services.Tests  // service-level tests and rule validation
└─ docs                       // architecture and module documentation
```

## 3. Startup and request pipeline

### 3.1 Composition root

`Codeji.CMS.API/Program.cs` is the application entry point. It configures:
- controllers and Swagger,
- CORS for `localhost` and `*.codeji.in`,
- forwarded headers,
- MongoDB client and database scoping,
- repository and business-service registration,
- Mapster mapping config,
- JWT bearer authentication,
- policy authorization,
- SignalR hubs,
- antiforgery,
- hosted background workers,
- request rate limiting.

### 3.2 Request execution flow

A typical authenticated request flows through:
1. CORS and forwarded header processing.
2. JWT bearer authentication.
3. authorization and `ModulePermission` checks.
4. controller action binding.
5. service business logic.
6. repository persistence.
7. optional notification / email / SignalR side effects.

The code centralizes tenant context extraction using `CurrentContext` helpers rather than reading claims directly.

### 3.3 MongoDB configuration

The API requires a MongoDB connection string with a database name. `Program.cs` validates this at startup. The DI container exposes:
- `IMongoClient` as a singleton,
- `IMongoDatabase` scoped per request.

Repositories and services resolve `IMongoDatabase` and use typed collections or generic repositories.

## 4. Authentication and authorization

### 4.1 JWT and refresh tokens

Authentication uses `JwtBearer` middleware configured in `Program.cs`. The JWT token validates issuer, audience, lifetime, and signature using `ConfigManager.Jwt.SecretKey`.

Tokens carry claims for:
- `user_id` (current user),
- `company_id` (tenant),
- `role_id`,
- `ClaimTypes.Role` values.

Refresh token state is stored in MongoDB with expiration and revocation metadata. Login issues both JWT and refresh token.

### 4.2 Password hashing

Passwords are hashed with `BCrypt.Net-Next`. Verification is timing-safe and the hash includes salt.

### 4.3 Permission layers

Access control is enforced in three layers:
- `[Authorize]` for authenticated requests,
- policy handlers for role-based authorization,
- `ModulePermissionAttribute` for fine-grained module permissions.

`AuthenticateUserRequest` evaluates `ModulePermissionAttribute` metadata on endpoints and validates the current user’s role permissions.

## 5. Multi-tenancy and tenant isolation

### 5.1 Tenant model

The backend is multi-tenant. Every business entity uses `CompanyId` as the tenant key. Most documents inherit shared base fields such as `CompanyId`, `CreatedAt`, `IsDeleted`, and `Version`.

### 5.2 Tenant enforcement

Tenant isolation is enforced by:
- controller/service logic using `CurrentContext.CompanyId`,
- repository query filters,
- explicit company checks in public-facing and cross-module flows.

Never allow a client to supply `CompanyId` for protected workflows; the backend must derive it from the authenticated token.

## 6. Data layer and repository pattern

### 6.1 Generic repository

`Codeji.CMS.Repository` implements generic Mongo repositories and specialized repositories for attendance, salary, and more. Common features include:
- soft delete support,
- tenant filtering,
- compound filters,
- version checking.

### 6.2 Entity organization

Entities are grouped by domain under `Codeji.CMS.Repository/Entities/`:
- `Employees`,
- `Company`,
- `Attendance`,
- `Leave`,
- `Recruitments`,
- `PayRoll`,
- `Calendar`,
- `RolePermissions`,
- `CareerPortal`.

### 6.3 DTO contracts

`Codeji.CMS.DTO` defines request and response models used by controllers. Separate DTOs reduce coupling between HTTP contracts and persistence entities.

## 7. Module guide and business workflows

### 7.1 Account and authentication

**Purpose:** registration, login, refresh token, logout, password reset, antiforgery issuance.

**Key files:**
- `AccountController.cs`
- `AccountServices.cs`
- `EmpUser`, `RefreshToken`, `UserSecurityToken`
- `AuthenticationHandler.cs`

**Flow:**
1. user submits login/register request.
2. credentials are verified or account is created.
3. JWT + refresh token are generated.
4. refresh requests validate stored refresh token state.
5. logout revokes the refresh token.

**Important details:**
- registration creates tenant company data, default roles, and master lookups.
- the antiforgery endpoint returns XSRF header/cookie pair.

### 7.2 Employees and user profiles

**Purpose:** employee master data, invitations, self-profile editing, bulk upload.

**Key files:**
- `UserController.cs`
- `EmployeeService.cs`
- `EmpUser`, `EmpSummary`, education, certification, skills, work history entities

**Flow:**
1. admin creates or invites an employee.
2. employee profile data is stored in tenant scope.
3. profile edit requests update personal or professional sections.
4. bulk upload parses CSV and creates employees with validation.

**Working rules:**
- custom attributes are defined per company.
- employee self-service endpoints derive the user ID from token.
- employee records include role, department, job title and notification preferences.

### 7.3 Roles and permissions

**Purpose:** define per-company roles and module permissions.

**Key files:**
- `RolesController.cs`
- `RoleServices.cs`
- `Roles`, `Permission`, `Module`, `ModulePermission`, `RolePermission`

**Flow:**
1. company registration seeds default roles and permissions.
2. admin creates or edits role definitions.
3. role-to-permission assignments determine access to module actions.
4. `AuthenticateUserRequest` checks module permissions at runtime.

**Important details:**
- roles are not global; they belong to tenant companies.
- modules are seeded through migrations.
- permission checks are OR-ed across permissions provided to the action.

### 7.4 Company administration

**Purpose:** manage tenant company data, departments, job titles, custom fields, and policy documents.

**Key files:**
- `CompanyController.cs`
- `CompanyMasterController.cs`
- `CompanyService.cs`
- master lookup entities in `Company/`

**Flow:**
1. create or update company master records.
2. configure department, job title, and custom attribute lookup values.
3. manage company policies and policy versions.
4. ensure company profile content and public career settings are consistent.

**Business rules:**
- company master values are tenant-specific.
- department and job title changes may impact employee eligibility and reporting.
- policy versions are preserved for audit.

### 7.5 Attendance

**Purpose:** capture daily attendance, status settings, weekly offs, calendar mapping, penalties, and attendance summaries.

**Key files:**
- `AttendanceController.cs`
- `AttendanceService.cs`
- `AttendanceRepository.cs`
- `AttendanceModel`

**Flow:**
1. managers or employees create/update attendance rows.
2. attendance is validated against weekly off rules and calendar events.
3. attendance status definitions determine how hours are interpreted.
4. exceptions and penalties are tracked and included in summaries.

**Working details:**
- attendance rows may have source provenance, such as leave or WFH.
- the service avoids overwriting source-owned attendance unless explicitly permitted.
- payroll reads attendance summaries, not raw unvalidated records.

### 7.6 Leave management

**Purpose:** leave policy definition, employee leave requests, approvals, balance calculation, and attendance reconciliation.

**Key files:**
- `LeaveManagementController.cs`
- `LeaveManagementService.cs`
- `LeaveAccrualHostedService.cs`
- `LeavePolicy`, `LeaveRequest`, `EmployeeLeaveBalance`

**Flow:**
1. create leave policies with validation and calendar mapping.
2. employee submits leave request.
3. HR/Admin reviews and accepts or rejects.
4. accepted leave reconciles into attendance rows with `SourceType=LEAVE`.
5. declined or withdrawn requests reverse attendance reconciliation.

**Operational invariants:**
- attendance reconciliation is distinct from approval.
- leave balance deduction happens on accepted status.
- approved leave without reconciliation is not payroll-ready.
- `CompanyWorkingCalendar` evaluation uses weekly offs and holidays.

### 7.7 Work from home (WFH)

**Purpose:** managed WFH requests with policy-based eligibility, optional approval, and source-owned attendance.

**Key files:**
- `WorkFromHomeController.cs`
- `WorkFromHomeService.cs`
- `WorkFromHomeRequest`

**Flow:**
1. employee requests WFH for a date.
2. policy eligibility, quotas, advance notice, and holidays are validated.
3. requests are either approved immediately or sent for manager approval.
4. approved requests create attendance rows with `SourceType=WFH_REQUEST`.
5. employees check in/out through the WFH workflow.

**Important details:**
- WFH is a Leave Management tab, not a standalone module in the UI.
- attendance for WFH is protected from manual overwrite.
- clock-in/out uses server-side time and configured check windows.
- insufficient hours can create payroll exceptions.

### 7.8 Calendar and holidays

**Purpose:** manage tenant holiday calendars, events, and company-specific working-day rules.

**Key files:**
- `CalendarController.cs`
- `CalendarServices.cs`
- `CalendarEntity`, `Holidays`

**Flow:**
1. create company-specific holidays and calendar events.
2. calendar data is consumed by attendance, leave, and payroll calculations.
3. dashboard uses upcoming holidays and celebrations.

**Business rules:**
- holiday and weekly off configuration determine leave and WFH validation.
- calendar entries are tenant-specific.

### 7.9 Notice board

**Purpose:** publish tenant announcements and track user notifications.

**Key files:**
- `NoticeBoardController.cs`
- `NoticeBoardService.cs`
- `Notifications`, `UserNotifications`

**Flow:**
1. authorized users create notices.
2. notices persist in tenant scope.
3. notifications push via SignalR and remain visible in the user notification history.

### 7.10 Job vacancies

**Purpose:** create, publish, and manage job vacancies that feed the public career portal.

**Key files:**
- `JobVacancyController.cs`
- `JobVacancyService.cs`
- `JobVacancy`

**Flow:**
1. HR creates a vacancy and sets publish state.
2. vacancy may be exposed to the public career portal.
3. vacancy details are served to internal and public clients.

**Public behavior:**
- public routes use the tenant’s `PublicCompanyCode` rather than internal IDs.
- internal and external application modes are supported.

### 7.11 Applicants and recruitment

**Purpose:** applicant tracking, status transitions, resume handling, and application logs.

**Key files:**
- `ApplicantsController.cs`
- `ApplicantServices.cs`
- `Applicant`, `ApplicantLogs`, `Resume`

**Flow:**
1. applicants are created for a vacancy.
2. status transitions are recorded in applicant logs.
3. email notifications are sent for stage changes.
4. public and internal application paths are distinguished.

**Important details:**
- applications inherit tenant company from the selected vacancy.
- logs preserve history of status changes, actors, and comments.
- external application URLs are validated as non-loopback HTTPS.

### 7.12 Payroll

**Purpose:** monthly payroll execution, salary slip generation, and payroll period processing.

**Key files:**
- `PayRollController.cs`
- `AutoPayrollController.cs`
- `PayRollServices.cs`
- `AutoPayRollServices.cs`
- `SalaryService.cs`
- `SalaryCalculator.cs`
- `EmpPayRoll`

**Flow:**
1. salary structure is defined for employees.
2. payroll month is generated or uploaded.
3. payroll rows are processed and payslips are created.
4. closed payroll months are enforced.

**Dependencies:**
- attendance and leave reconciliation,
- salary data,
- holiday and weekly off configuration,
- payroll divisor policies.

**Output:**
- PDF payslips rendered through `PdfService` and PuppeteerSharp.

### 7.13 Salary

**Purpose:** persist employee salary compensation details.

**Key files:**
- `SalaryController.cs`
- `SalaryService.cs`
- `SalaryRepository.cs`

**Flow:**
1. HR sets employee salary components.
2. salary records are read by payroll generation.
3. salary changes affect future payroll not locked past periods.

### 7.14 Career profile and public company pages

**Purpose:** company branding and public career page management.

**Key files:**
- `CareerProfileController.cs`
- `PublicCompanyProfileService.cs`

**Flow:**
1. company edits career profile content.
2. profile content is sanitized and saved.
3. public career pages use the saved tenant branding and metadata.

**Public routing:**
- uses stable `PublicCompanyCode`.
- supports publish/unpublish and external career URL fallback.

### 7.15 Dashboard

**Purpose:** provide aggregated metrics and reports for authorized users.

**Key files:**
- `DashboardController.cs`
- `DashboardServices.cs`

**Flow:**
1. collect employee, attendance, holiday, and recruitment statistics.
2. serve summary payloads for charts and dashboards.
3. provide upcoming holidays and celebrations.

## 8. Cross-cutting concerns

### 8.1 Notifications and SignalR

**Purpose:** in-app notification delivery, chat, and real-time alerts.

**Key files:**
- `NotificationHub`
- `NotificationService`
- `UserNotifications`
- `Notifications`

**Flow:**
1. business actions create notification records.
2. recipient entries are created per user.
3. SignalR pushes the notification to connected clients.
4. offline users can read history later.

### 8.2 Background hosted services

**Key workers:**
- `PriorityQueuedHostedService`,
- `BirthDayAndAnniversaryNotificationHostedServices`,
- `LeaveAccrualHostedService`,
- `CareerNotificationWorker`,
- `AttendanceOutboxWorker`,
- `AttendanceReminderHostedService`,
- `PayRollHostedService`.

**Behavior:**
- queued tasks are processed out of HTTP request context,
- hosted services must not rely on request-scoped HttpContext,
- state is captured before queueing.

### 8.3 Email and mail delivery

**Implementation:**
- `SendGrid` and SMTP helper packages are used,
- `EmpEmailLogs` tracks email persistence and delivery attempts,
- email content is built with a shared branded wrapper.

**Working note:**
- email is queued and persisted first; external delivery errors are recorded but do not roll back business persistence.

### 8.4 Validation, results, and error handling

Services use `Result` and `Result<T>` patterns. Business failures return structured codes and messages rather than raw exceptions.

### 8.5 File uploads

Uploads are stored in `Codeji.CMS.API/Uploads` and served via configured static file middleware. File access is tenant-aware in business flows.

## 9. Migrations and database changes

`Codeji.CMS.Migrations` contains migration runner logic and incremental migration classes. Each migration should be:
- idempotent,
- tenant-aware,
- safe to rerun when its condition already exists.

Typical migration tasks:
- seed modules and permissions,
- add master lookup values,
- backfill new public career identifiers,
- create or repair indexes,
- migrate legacy attendance date encodings.

## 10. Testing and verification

### 10.1 Service tests

`Codeji.CMS.Services.Tests` contains business-rule tests. These tests exercise service logic without needing a real MongoDB.

### 10.2 Recommended manual test flows

- authentication and refresh token behavior,
- employee creation and profile access,
- role/permission checks and module gating,
- attendance entry and locked month behavior,
- leave request creation, approval, reconciliation, and payroll dependency,
- WFH request lifecycle, clock-in/out, and attendance provenance,
- payroll generation and payslip creation,
- public career page routing and applicant submission.

## 11. Deployment and operational checklist

1. build `Codeji.CMS.API`, `Codeji.CMS.Services`, and `Codeji.CMS.Migrations`.
2. ensure MongoDB connection string includes database name.
3. run migrations against the target database.
4. configure JWT, email provider, and public URLs.
5. restart the API.
6. verify login, protected endpoints, SignalR hub negotiation, and a safe WFH/leave payroll path.

## 12. Adding a new feature

1. add controller in `Codeji.CMS.API/Controllers`.
2. add service implementation in `Codeji.CMS.Services` with an interface.
3. add entity classes under `Codeji.CMS.Repository/Entities`.
4. add request/response DTOs under `Codeji.CMS.DTO`.
5. register services in `Codeji.CMS.Services/Registration/ServicesRegistration.cs`.
6. add module permissions or update module seed migrations.
7. document the new module in `docs/modules.md` and this documentation file.

## 13. Related documentation

- `docs/README.md`
- `docs/modules.md`
- `docs/architecture.md`
- `docs/data-layer.md`
- `docs/auth-and-permissions.md`
- `docs/migrations.md`
- `docs/cross-cutting.md`
- `docs/add-new-feature.md`
- `docs/leave-management-current-flow.md`
- `docs/work-from-home-module.md`
- `docs/payroll-module-current-flow.md`
- `docs/recruitment-current-flow-and-working-context.md`
- `docs/attendance-module-current-flow-and-audit.md`
