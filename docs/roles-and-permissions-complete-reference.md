# Roles and Permissions — Complete Project Reference

> Code-aligned reference for the authorization model in CMS-Backend-Core, inspected on 29 July 2026. It documents what the server currently enforces; it is not a substitute for checking the target environment's `Roles`, `ModulePermission` and `RolePermission` data.

## 1. Core principle

Access is tenant-scoped and permission-based. A role name such as **HR** or **Administrator** is not by itself an authorization decision. Effective access is created through this chain:

```text
Authenticated EmpUser
  → role_id claim
  → Roles document
  → RolePermission grant for the company
  → ModulePermission (module + permission)
  → controller attribute and service ownership/workflow checks
```

Every protected service must obtain `CompanyId` and `UserId` from `CurrentContext`; client-supplied company, employee or approver identifiers are not a tenant boundary.

## 2. Authentication and request layers

| Layer | Source | Purpose | Failure |
|---|---|---|---|
| Authentication | JWT bearer middleware | Validates issuer, audience, expiry and signature | 401 |
| ASP.NET policy | `[Authorize]`, `AdminOnly` | Restricts an action to a policy/role rule | 401/403 |
| Module action | `ModulePermissionAttribute` | Checks a role grant for `AppModule` + `Permission` | 403 |
| Tenant/ownership | service query and update filters | Ensures user/company owns the record or is the intended reviewer | generic business failure/403 |
| Workflow state | service validation + optimistic version | Prevents invalid transitions/races | business failure |

JWT claims used by protected business services are `user_id`, `company_id` and `role_id`. SignalR uses the same bearer token on the notification/chat hub query string only.

## 3. Permission vocabulary

The base permission constants are `View`, `Create`, `Edit` and `Delete`. WFH adds a deliberate finer-grained vocabulary:

| Permission | Meaning |
|---|---|
| `ViewOwn` / `CreateOwn` / `EditOwn` / `CancelOwn` | Employee self-service scope; service must also confirm current user ownership. |
| `ViewTeam` | View requests assigned to the caller as approver. |
| `ApproveTeam` | Approve, reject or return an assigned request; requester self-approval is rejected. |
| `ViewAll` | Company-wide WFH review view, still scoped by current company. |
| `CreateForEmployee` | Administrative creation on behalf of another employee, when an endpoint uses it. |
| `Override` / `Revoke` | Controlled exceptional action; not equivalent to arbitrary attendance editing. |
| `PolicyView` / `PolicyEdit` | Read/change company WFH policy settings. |

The permission constants are declared in `Codeji.CMS.Utility/Constraints/ConstraintHelper.cs`; the WFH migration creates the corresponding module/permission records.

## 4. Default role intent

| Role type | Intended responsibility | Important limitation |
|---|---|---|
| Administrator | Company master data, roles, policies, high-risk HR/payroll operations | Must still hold the relevant `RolePermission`; tenant filter still applies. |
| HR | Employee lifecycle, attendance/leave/payroll/recruitment operations and WFH review | Must not rely on role name alone; policy configuration needs its module action grant. |
| Employee | Own profile/self-service and employee-visible information | May never derive access to another employee by passing their identifier. |
| Manager | Not a distinct built-in enum in this implementation | WFH team decisions require deliberate role-permission grant and assigned-approver relationship. |

The exact role matrix is data-driven. Use the Roles administration endpoints to inspect or update it; do not hard-code permission assumptions in React.

## 5. Module and endpoint matrix

### Account and authentication

`AccountController` mixes public session flows and authenticated account operations.

| Area | Access | Notes |
|---|---|---|
| Login, registration, refresh token, password reset, email verification, antiforgery | `AllowAnonymous` | Input validation and token/provider controls remain required. |
| Logout, signed-user details, self account actions | authenticated route/service behavior | Bound to current token/user. |
| Public job application handoff | anonymous/public contract | Must not expose private tenant records. |

### Employees

`UserController` uses `Employees` permissions.

| Operation | Required action | Additional boundary |
|---|---|---|
| Create/invite employee | `Employees.Create` | Tenant is current company. |
| Employee list/detail | `Employees.View` | Service must not reveal another tenant. |
| Update employee | `Employees.Edit` | Self-edit DTOs intentionally restrict sensitive employment fields. |
| Delete employee | `Employees.Delete` | Lifecycle effects must be reviewed against payroll/history. |
| Salary-related employee changes | `Employees.Create` or `Edit` as route declares | Payroll setting reads may use `Payroll_Settings.View`. |

### Roles and permissions

`RolesController` administration endpoints use `AdminOnly` policy.

| Operation | Access | Notes |
|---|---|---|
| List/create/update/delete roles | `AdminOnly` | Company administrator manages role definitions. |
| Read modules/permissions | `AdminOnly` | Use this as the authoritative configuration screen. |
| Assign role permissions | `AdminOnly` | Creates/updates company-scoped `RolePermission` grants. |

### Company and company master

| Controller area | Gate | Actions |
|---|---|---|
| Company list/detail/update | `AdminOnly` | Tenant/company administration. |
| Company policies and versions | `Policy.Create/View/Edit/Delete` | Create, version, list, edit, delete and read policy documents. |
| Departments/job titles/custom attributes/module access | `AdminOnly` | `CompanyMasterController`; affects downstream employee/recruitment forms. |
| Public company/career profiles | `AllowAnonymous` | Public code/publish filters are the boundary, not an HR permission. |

### Attendance

| Operation | Gate |
|---|---|
| Create/admin attendance | `Attendance.Create` |
| Read user/date/grid attendance | `Attendance.View` |
| Edit attendance | `Attendance.Edit` |
| Weekly-off settings | `Attendance.View` / `Attendance.Edit` |
| Attendance status setting write | `AdminOnly` |
| Penalty policy, exceptions, lock validation/review | `Attendance.View` / `Attendance.Edit` by route |
| Attendance remark options | `Attendance.View` / `Attendance.Edit` |

Source-linked `LEAVE` and `WFH_REQUEST` attendance rows are not editable through ordinary attendance operations even if the caller has `Attendance.Edit`. Provenance and month locks are additional enforcement layers.

### Leave management

`LeaveManagementController` uses `Leave_Management` module actions.

| Operation | Gate |
|---|---|
| Create/update leave policy | `Create` / `Edit` |
| List policies, balances, requests and summaries | `View` |
| Create own/admin-for-employee request | `Create` or `Edit` according to route |
| Update/decide request | `Edit` |
| Delete request | `Delete` |
| Update employee balance | `Create` or `Edit` |

Approval and attendance reconciliation additionally validate request status, reviewer workflow, company and month lock.

### Work from home

`WorkFromHomeController` route prefix is `api/wfh`.

The React entry point is the **Work from home** tab within Leave Management (`/leavemanagement/work-from-home`); it is not a separate sidebar module. This navigation grouping does not weaken the WFH API's tenant, ownership, policy or reviewer checks listed below.

| Route/action | Controller gate | Service boundary |
|---|---|---|
| `GET policy` | authenticated | Effective policy is tenant-scoped. Employee UI must not render management fields. |
| `GET context` | authenticated | Returns only own eligibility/quota/safe schedule context. |
| `PUT/POST policy` | `Work_From_Home.PolicyEdit` | Company policy only; validates status codes/settings. |
| `POST requests` | authenticated self-service | Current employee only; policy, eligibility, quota, calendar, lock and overlap recalculated server-side. |
| `GET requests/my` | authenticated self-service | Current user only. |
| `GET requests/team` | `ViewTeam` | Assigned approver only. |
| `GET requests/all` | `ViewAll` | Current company only. |
| approve/reject/return | `ApproveTeam` | Assigned approver or company reviewer; self-approval denied; expected version required. |
| cancel | authenticated self-service | Current request owner and valid state/version only. |
| check-in/check-out/timing | authenticated self-service | Current owner, approved request, company-local work date, WFH-owned attendance row and clock windows. |

Employee roles receive own WFH permission records through `GrantWorkFromHomeEmployeeSelfServicePermissions`. The current controller intentionally leaves employee self-service endpoints without a generic module attribute because ownership validation in the service is the reliable security boundary for these routes. Policy/team/all/decision operations retain module attributes.

### Calendar

| Operation | Gate |
|---|---|
| List calendar/holidays | authenticated controller/service behavior |
| Add/update event | `Calendar.Create` or `Calendar.Edit` |
| Delete event | `Calendar.Delete` |

Calendar visibility affects WFH/attendance validation, so calendar service queries must be tenant-filtered.

### Notice board

| Operation | Gate |
|---|---|
| Post notice | `Notice_Board.Create` |
| Read list/detail/my notices | `Notice_Board.View` |
| Edit own/allowed notice | `Notice_Board.Edit` |
| Delete notice | `Notice_Board.Delete` |

Notice publication also creates user notification records; delivery recipients must remain company-scoped.

### Recruitment and public careers

| Area | Gate | Notes |
|---|---|---|
| Job vacancy create/edit/delete | `Jobs.Create/Edit/Delete` | Authenticated tenant recruitment operations. |
| Vacancy list | route-specific; current controller includes anonymous listing | Public/private filtering is required. |
| Applicants list/detail | `Applications.View` | Applicant data is sensitive and company-scoped. |
| Applicant create/edit/comment | `Applications.Create/Edit` | Resume upload is intentionally anonymous/public in the current controller; validate application token/ownership in service. |
| Applicant process log | `Process_Log.View` | Recruitment workflow audit. |
| Career profile | `Career_Profile.View/Edit` | Authenticated tenant configuration. |
| Public jobs/company pages/applications | `AllowAnonymous` plus rate limiting | Public IDs/codes and active/published filters are the boundary. |

### Payroll, salary and payroll settings

| Operation | Gate |
|---|---|
| View payroll data / employee payslip | `PayRoll.View` |
| Generate/upload/update/process payroll | `PayRoll.Create` or `PayRoll.Edit` |
| Automatic monthly payroll generation | `Payroll_Settings.Create` or `Payroll_Settings.Edit` |
| Payroll divisor policy read/write | `Payroll_Settings.View` / `Payroll_Settings.Edit` |
| Salary update | `Employees.Create` or `Employees.Edit` |
| Salary configuration read | `Payroll_Settings.View` |

Payroll permissions do not bypass attendance locks, approved summaries, salary structure validation or tenant boundaries.

### Dashboard, chat and notifications

`DashboardController` is authenticated; it returns aggregate data and must not include cross-company counts. Notification/chat hubs authenticate using the JWT path-specific query-token handling. A `UserNotifications` record is a user-scoped durable inbox item; a SignalR event is delivery, not authorization.

## 6. Implementing a new protected action

1. Decide the module and least-privileged action.
2. Add a controller `ModulePermission` or policy where the pattern applies.
3. Enforce company and ownership in the service query/update filter.
4. Validate workflow state and expected version for mutable records.
5. Add the module/permission/role grants through an idempotent migration when needed.
6. Hide the UI action using the existing permission helpers, but do not rely on that hide as security.
7. Test allowed, denied, cross-company and self-versus-other-user cases.

## 7. Review checklist

- Every tenant-owned repository filter contains `CompanyId`.
- A client cannot select another employee/company/approver by ID.
- Public routes expose only explicitly public, published/active data.
- An employee DTO excludes reviewer notes, recipient settings and other employees' data.
- Sensitive free-text data is not put into broad notification payloads.
- Direct attendance update cannot overwrite leave/WFH-owned rows.
- Role assignment changes are audited and validated before deployment.

## 8. Related documents

- [Authentication and permissions](./auth-and-permissions.md)
- [Current permission flow](./permissions-current-flow.md)
- [Complete project documentation](./COMPLETE_PROJECT_DOCUMENTATION.md)
- [Work from home module](./work-from-home-module.md)
- [Migrations](./migrations.md)
