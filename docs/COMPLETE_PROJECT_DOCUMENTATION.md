# Codeji CMS Backend Core — Complete Technical Documentation

> Source-aligned documentation for the repository as inspected on 29 July 2026. This is an implementation guide: when this document and code disagree, the code is authoritative.

## 1. What this system is

Codeji CMS Backend Core is a multi-tenant HRMS and recruitment API. A single MongoDB database can hold documents for many companies; company isolation is carried by `CompanyId` and the authenticated request context. The sibling React application (`../CMS-React`) is the private user interface. Public recruitment endpoints are also exposed from this API.

The product areas are:

- company registration, company master data and policy documents;
- account, JWT session, refresh token, email verification and password recovery;
- employees, roles, module permissions, profile data and notifications;
- attendance, weekly offs, attendance statuses, penalties, exceptions and month locking;
- leave policies, balances, approvals, attendance reconciliation and accrual;
- work from home (WFH) policy, requests, clocking, attendance provenance and review;
- salary structures, payroll, payslips and payroll-period controls;
- recruitment, public jobs, applicants, resumes and career profiles;
- calendar, holidays, notice board, dashboard, chat and real-time notifications.

## 2. Technology and solution map

| Concern | Implementation |
|---|---|
| Runtime/API | .NET 10, ASP.NET Core controllers |
| Persistence | MongoDB, generic repository plus attendance/payroll specializations |
| Authentication | JWT bearer tokens and refresh-token endpoints |
| Authorization | `[Authorize]`, policy handlers, `ModulePermissionAttribute`, service-level ownership checks |
| Realtime | SignalR notification and chat hubs |
| Background work | hosted services and `IPriorityTaskQueue` |
| Email | configured email helper, `EmpEmailLogs`, queued delivery |
| Mapping | Mapster |
| Files | local `Uploads` directory, served below `/fs` |
| Tests | xUnit service tests |

```text
CodejiCMSCore.sln
├─ Codeji.CMS.API             HTTP composition root, controllers, middleware, hubs
├─ Codeji.CMS.Services        business workflows and background services
├─ Codeji.CMS.Repository      Mongo documents, repositories and data contracts
├─ Codeji.CMS.DTO             request/response contracts
├─ Codeji.CMS.Utility         JWT, current-context, mail, enums and sanitization
├─ Codeji.CMS.Migrations      ordered data/schema seed and backfill runner
├─ Codeji.CMS.Services.Tests  database-free service tests
└─ docs                       maintained architecture and module documentation
```

## 3. Startup and request lifecycle

`Codeji.CMS.API/Program.cs` is the composition root. It registers controllers, CORS, Swagger, Mongo client/database, repository and business services, authentication, authorization handlers, SignalR, the priority queue and hosted workers. The Mongo connection must include a database name; startup fails otherwise.

The normal protected request flow is:

```text
React/API client
  → CORS + forwarded-header handling
  → JWT bearer authentication
  → authorization / ModulePermission where applied
  → controller route binding
  → service business validation and current CompanyId/UserId lookup
  → Mongo repository operation
  → Result or Result<T> response
  → optional queue, SignalR and email side effects
```

`CurrentContext` derives the active company and user from the authenticated request. Do not accept a tenant identifier from an ordinary client request as a substitute for this context.

### Result handling

Services commonly return `Result` or `Result<T>`. `Success` indicates application success; `MethodResult` holds a typed result. Business validation normally returns a stable code and readable message, for example `WFH_WEEKLY_QUOTA_EXCEEDED`. Callers must not assume every unsuccessful business result is an HTTP 500.

## 4. Data and tenancy model

Most business documents inherit `BaseClass`, which includes tenant/lifecycle fields. Repositories apply their default filters unless an operation explicitly asks for `withDefaultFilter: false`. A service must still include `CompanyId` in its own cross-tenant-sensitive query and update filter.

Important rules:

1. Read the tenant from `CurrentContext` for protected workflows.
2. Include `CompanyId` in lookups and optimistic updates.
3. Do not use raw collection access for ordinary business operations.
4. Preserve source/provenance fields on derived records, especially attendance.
5. Background work has no request HTTP context. Capture `CompanyId`, actor and target data before queueing it.

### Main collections

| Area | Key documents |
|---|---|
| Identity | `EmpUser`, `Roles`, `RolePermission`, `Permission`, refresh/security tokens |
| Company | `Company`, departments, job titles, policies, policy versions, custom attributes |
| Attendance | `AttendanceModel`, status settings, weekly off, summaries, payroll exceptions, penalties |
| Leave | leave policy, employee leave balance, leave request and logs |
| WFH | `WorkFromHomePolicy`, `WorkFromHomeRequest`, `WorkFromHomeRequestLog`, attendance remarks |
| Payroll | salary/payroll documents, period/divisor configuration and generated payslips |
| Recruitment | job vacancies, applicants, resumes, applicant logs and career profile data |
| Notifications | `Notifications`, `UserNotifications`, `EmpEmailLogs`, preferences |

## 5. Authentication, roles and permissions

`AccountController` exposes registration, login, refresh-token, logout, signed-user detail, verification and password recovery endpoints. JWT validation checks issuer, audience, lifetime and signing key configured through `ConfigManager`.

Authorization is layered:

- `[Authorize]` establishes that the caller has a valid token.
- Role/policy authorization handles configured high-level cases.
- `ModulePermission(AppModule, Permission)` performs module-action authorization where present.
- Services protect row ownership and workflow state even when a controller endpoint intentionally permits employee self-service access.

For example, the WFH employee endpoints allow an authenticated employee to create and read only their own requests; policy editing and team/all-request views remain module-permission controlled. This separation prevents a generic permission resolver issue from blocking self-service while retaining data ownership checks in the service.

## 6. Module catalogue and working flows

### 6.1 Company and company master

`CompanyController` manages company details and policy documents/versioning. `CompanyMasterController` manages departments, job titles, custom attributes and module access. Company master values are dependencies for employees, recruitment and reporting; deleting or renaming them requires checking consumers.

Public-career identity uses a stable six-digit `PublicCompanyCode`, not a slug. The authenticated career profile and public company profile are separate surfaces; publishing state controls public enrichment.

### 6.2 Employees, profiles and notifications

`UserController` and employee services manage employee identity, profile data, skills, education, experience, invitations and notification state. `RolesController` manages role and permission assignments. Employee records carry role, department and job role values used by WFH eligibility and HR notification targeting.

The notification model is durable first: a `Notifications` document is created, one `UserNotifications` document is created per recipient, and SignalR pushes the same notification to connected recipients. This means an offline recipient can still retrieve notification history later.

### 6.3 Attendance

`AttendanceController`, `AttendanceStatusSettingsController`, `WeeklyOffSettingsController` and `AttendancePenaltyController` cover daily attendance administration, status definitions, weekly off setup, penalties/exceptions and lock checks.

Attendance is not merely a status grid. It is downstream of approved leave and WFH workflows. Rows may carry a `SourceType`, `SourceId` and `SourceVersion`. A manually administered attendance record must not overwrite a source-owned `LEAVE` or `WFH_REQUEST` row. Locked months reject workflow changes. Payroll reads the controlled attendance result.

See [attendance-module-current-flow-and-audit.md](./attendance-module-current-flow-and-audit.md) for operational detail.

### 6.4 Leave management

`LeaveManagementController` manages policy, balances, employee requests, status decisions and summaries. Approval/rejection drives `LeaveAttendanceReconciliationService`, which creates or removes source-linked attendance safely. The accrual hosted service updates eligible balances on schedule.

```text
Leave policy/balance → employee request → reviewer decision
  → reconcile leave-owned attendance → attendance summary/exception
  → payroll reads locked/validated attendance
```

### 6.5 Work from home (Leave Management workflow)

WFH is a policy-controlled attendance workflow, not a free-form attendance code. Its frontend is the **Work from home** tab within Leave Management (`/leavemanagement/work-from-home`), with no standalone sidebar tab or `/work-from-home` route. Each company can choose direct WFH or manager approval, eligibility by everyone/department/employee, advance notice, one-day-per-week quota, work hours and allowed check-in window.

The employee-facing context is calculated on the server (`GET /api/wfh/context`), rather than inferred by React. It returns only safe effective information: feature/eligibility state, weekly limit, used/remaining allowance, disabled reason, next available date, approval mode and effective schedule windows. Policy configuration remains restricted to authorized users.

```text
Employee WFH form
  → policy/eligibility/calendar/quota validation
  → Pending (approval on) OR Approved (approval off)
  → source-owned WFH attendance row
  → HR/Admin durable + realtime notification and email
  → employee clock-in / clock-out
  → total hours, exception if insufficient, later HR review
```

The detailed contract is in [work-from-home-module.md](./work-from-home-module.md).

### 6.6 Payroll and salary

`SalaryController`, `PayRollController`, `AutoPayrollController` and `PayrollDivisorPolicyController` manage salary components, payroll inputs, generation, review/processing, payslips and payroll rules. Payroll depends on attendance/leave correctness and period locking. Fix upstream attendance conflicts before regenerating payroll; do not treat payroll as the source of truth for attendance.

### 6.7 Recruitment and public careers

`JobVacancyController` and `ApplicantsController` manage authenticated recruitment. Public controllers expose discoverable jobs, company job lists, applications, resume uploads, saved jobs, company follows and job alerts. Sensitive public endpoints are rate-limited using the `career-sensitive` policy. Recruitment mail is queued through `IPriorityTaskQueue` and `IMiddlewareService.EmailSendAndSave`.

### 6.8 Calendar, notices, dashboard and chat

`CalendarController` manages events/holidays consumed by attendance and WFH validation. `NoticeBoardController` persists notices and user notification records. `DashboardController` aggregates gender, department, application and upcoming-event/celebration information. SignalR hubs provide chat and user notification delivery.

## 7. Background jobs, email and realtime delivery

Registered hosted services include the priority-queue worker, birthday/work-anniversary notifications, leave accrual and career notification processing. Jobs must be idempotent or guarded by workflow state because a process restart/retry can occur.

Email is recorded in `EmpEmailLogs` through the middleware mail service. An email send result depends on the configured mail provider, so a successful API request proves queueing/persistence, not external inbox delivery. WFH notification jobs capture all request information before they execute, which avoids losing tenant context after the HTTP request completes.

## 8. Migrations and deployment

`Codeji.CMS.Migrations` is an ordered Mongo migration runner. Migrations are application code, not Entity Framework migrations. They must be idempotent, narrow in scope and safe to re-run. Current WFH changes include workflow collections/indexes, a policy seed attempt and employee self-service permission grants.

Deployment checklist:

1. Back up and identify the target Mongo database.
2. Build the services/migrations projects.
3. Run migrations once against the intended database.
4. Restart the API so new controller/service code and hosted workers load.
5. Verify login, a protected endpoint, SignalR negotiation and a non-destructive WFH flow.
6. Verify email configuration separately using a safe recipient.

## 9. Testing strategy

`Codeji.CMS.Services.Tests` is appropriate for deterministic business rules without MongoDB. A successful unit build/test does not prove JWT authorization, tenant filtering, Mongo indexes, background delivery, SignalR or mail-provider delivery.

Minimum WFH smoke test after release:

1. Login as HR/Admin and configure an enabled policy.
2. Login as an eligible employee and create a permitted request.
3. Confirm the employee cannot see other employees' requests.
4. Confirm HR/Admin sees the request and receives notification history/bell update; confirm email log.
5. For direct WFH, confirm it becomes approved immediately. For approval mode, approve it as a different authorized user.
6. On the covered date, use the single clock action to check in then check out.
7. Confirm the attendance row has `SourceType=WFH_REQUEST`, correct source ID, hours and exception behavior.
8. Confirm manual attendance edit and locked-month changes are rejected.

## 10. Operating and extension rules

- Keep controller routes, DTOs, services, entities, permissions and frontend calls in sync.
- Add a migration for persisted schema/index/backfill changes and document it in `migrations.md`.
- Do not bypass approval, quota or attendance provenance by writing attendance directly.
- Do not put HTTP-context lookups inside queued delegates.
- Use optimistic version checks for mutable workflow decisions.
- Update this document, the module document and `docs/README.md` whenever a workflow contract changes.

## 11. Related documentation

- [Documentation index](./README.md)
- [Architecture](./architecture.md)
- [Authentication and permissions](./auth-and-permissions.md)
- [Roles and permissions complete reference](./roles-and-permissions-complete-reference.md)
- [Attendance flow and audit](./attendance-module-current-flow-and-audit.md)
- [WFH module](./work-from-home-module.md)
- [Leave-attendance-payroll flow](./leave-attendance-payroll-end-to-end-flow.md)
- [Migrations](./migrations.md)
- [Recruitment current flow](./recruitment-current-flow-and-working-context.md)
