# Business Modules

The catalog of every domain in the API. Each entry lists the controller, service, key entities, notable DTOs, the permission gate, and notable endpoints.

## Module index

| Module | Controller | Permission gate |
|---|---|---|
| [Account / Auth](#account--auth) | [AccountController.cs](../Codeji.CMS.API/Controllers/AccountController.cs) | (mixed — public + authed) |
| [Employees](#employees) | [UserController.cs](../Codeji.CMS.API/Controllers/UserController.cs) | `Employees` |
| [Roles & Permissions](#roles--permissions) | [RolesController.cs](../Codeji.CMS.API/Controllers/RolesController.cs) | (varies) |
| [Company](#company) | [CompanyController.cs](../Codeji.CMS.API/Controllers/CompanyController.cs), [CompanyMasterController.cs](../Codeji.CMS.API/Controllers/CompanyMasterController.cs) | mixed (`Policy` for policies, AdminOnly for master) |
| [Attendance](#attendance) | [AttendanceController.cs](../Codeji.CMS.API/Controllers/AttendanceController.cs) | `Attendance` |
| [Leave Management](#leave-management) | [LeaveManagementController.cs](../Codeji.CMS.API/Controllers/LeaveManagementController.cs) | `Leave_Management` |
| [Calendar / Holidays](#calendar--holidays) | [CalendarController.cs](../Codeji.CMS.API/Controllers/CalendarController.cs) | `Calendar` |
| [Notice Board](#notice-board) | [NoticeBoardController.cs](../Codeji.CMS.API/Controllers/NoticeBoardController.cs) | `Notice_Board` |
| [Job Vacancies](#job-vacancies) | [JobVacancyController.cs](../Codeji.CMS.API/Controllers/JobVacancyController.cs) | `Jobs` |
| [Applicants](#applicants) | [ApplicantsController.cs](../Codeji.CMS.API/Controllers/ApplicantsController.cs) | `Applications` |
| [Payroll](#payroll) | [PayRollController.cs](../Codeji.CMS.API/Controllers/PayRollController.cs), [AutoPayrollController.cs](../Codeji.CMS.API/Controllers/AutoPayrollController.cs) | `PayRoll` |
| [Salary](#salary) | [SalaryController.cs](../Codeji.CMS.API/Controllers/SalaryController.cs) | `Employees` |
| [Dashboard](#dashboard) | [DashboardController.cs](../Codeji.CMS.API/Controllers/DashboardController.cs) | (authed only) |

---

## Account / Auth 

**Purpose.** Login, registration (creates a new company + admin user), refresh-token, password reset, anti-forgery token issuance.

| What | Where |
|---|---|
| Controller | [AccountController.cs](../Codeji.CMS.API/Controllers/AccountController.cs) — route prefix `api/...` (custom per action) |
| Service | [AccountServices.cs](../Codeji.CMS.Services/Account/AccountServices.cs) / [IAccountServices.cs](../Codeji.CMS.Services/Account/Interface/IAccountServices.cs) |
| Entities | [EmpUser.cs](../Codeji.CMS.Repository/Entities/Employees/EmpUser.cs), [RefreshToken.cs](../Codeji.CMS.Repository/Entities/RefreshToken.cs), [UserSecurityToken.cs](../Codeji.CMS.Repository/Entities/UserSecurityToken.cs) |
| Notable DTOs | `LoginModel`, `LoginUserViewModel`, `TokenResponseDto`, `RefreshTokenRequestDto`, `ChangePasswordRequest`, `CreateNewPasswordRequest` |
| Helper | [AuthenticationHandler.cs](../Codeji.CMS.Utility/Helpers/AuthenticationHandler.cs) — BCrypt + JWT |

Notable endpoints:
- `GET  api/account/antiforgerytoken/{appKey}` — `[AllowAnonymous]` — XSRF token pair
- `GET  api/app/checkAppVersion` — `[AllowAnonymous]` — version check
- `POST api/account/login` — `[AllowAnonymous]` — email + password → JWT + refresh token
- `POST api/account/register` — `[AllowAnonymous]` — registers a new company and its admin user (calls `_roleService.AddDefaultRole` + creates admin `EmpUser` + seeds default departments / job titles via [`DefaultCompanySeeds`](../Codeji.CMS.Services/Companies/DefaultCompanySeeds.cs))
- `POST api/account/refresh-token` — `[AllowAnonymous]` — exchange refresh token for new JWT
- `POST api/account/forgot-password` — sends a password-reset email
- `POST api/account/CreateNewPassword` — sets a new password using the reset token

> **Caveat.** The `appKey` accepted by the antiforgery endpoint is hardcoded to `"uiploutssh-817181871"` — see [AccountController.cs:76](../Codeji.CMS.API/Controllers/AccountController.cs#L76). Move to config before this becomes a real secret.

---

## Employees

**Purpose.** Employee master CRUD, invite-new-employee flow, profile editing (personal info, education, certifications, skills, work history), bulk CSV upload.

| What | Where |
|---|---|
| Controller | [UserController.cs](../Codeji.CMS.API/Controllers/UserController.cs) — route `api/user` |
| Service | [EmployeeService.cs](../Codeji.CMS.Services/Employees/EmployeeService.cs) / [IEmployeeService.cs](../Codeji.CMS.Services/Employees/Interface/IEmployeeService.cs) |
| Entities | [EmpUser](../Codeji.CMS.Repository/Entities/Employees/EmpUser.cs), [EmpSummary](../Codeji.CMS.Repository/Entities/Employees/EmpSummary.cs), [EmpEducationDetails](../Codeji.CMS.Repository/Entities/Employees/EmpEducationDetails.cs), [EmpCertificationDetails](../Codeji.CMS.Repository/Entities/Employees/EmpCertificationDetails.cs), [EmpSkills](../Codeji.CMS.Repository/Entities/Employees/EmpSkills.cs), [EmpWorkHistory](../Codeji.CMS.Repository/Entities/Employees/EmpWorkHistory.cs) |
| Notable DTOs | `InviteEmployeeDto`, `GetAllEmployeeRequestModel`, `GetAllEmployeeResponseModel`, `EmployeePersonalInfo`, `EmployeeSummaryRequestModel`, `EmployeeEducationRequestModel`, `EmployeeCertificationRequestModel`, `EmployeeSkillsRequestModel`, `EmployeeWorkHistoryModel`, `EmpUserCustomAttribute` |

Notable endpoints (all gated by `[ModulePermission(AppModule.Employees, ...)]`):
- `POST api/user/InviteNewEmployee` — `Create` — creates user + sends welcome email via `EmployeeWelcomeMail` template
- `GET  api/user/GetLastEmployeeId` — `View`
- `POST api/user/EditEmployees` — `Edit`
- `POST api/user/GetAllEmployees` — `View` — paginated, filterable
- `POST api/user/BulkUploadEmployee` — CSV import
- `POST api/user/GetEmployeesByDepartment` — `View`
- `GET  api/user/GetMyProfile` — current user
- `POST api/user/UpdateMyProfile` — self-edit
- `POST api/user/EditEducationDetails`, `EditCertificationDetails`, `EditSkills`, `EditWorkHistory`

Custom attributes per employee live in `EmpUser.CustomAttributes` as a list — defined per company via the [Company](#company) module.

> **Caveats.** The custom-attribute fetch loop in [EmployeeService.cs:236-254](../Codeji.CMS.Services/Employees/EmployeeService.cs#L236-L254) is N+1; the birthday/anniversary notification path in [EmployeeService.cs:830-838](../Codeji.CMS.Services/Employees/EmployeeService.cs#L830-L838) is N+1 and nested. Batch the queries when you next touch them.

---

## Roles & Permissions

**Purpose.** Per-company roles and what those roles can do, modeled as `Roles → RolePermission → ModulePermission → Module + Permission`.

| What | Where |
|---|---|
| Controller | [RolesController.cs](../Codeji.CMS.API/Controllers/RolesController.cs) — route `api/roles` |
| Service | [RoleServices.cs](../Codeji.CMS.Services/Employees/RoleServices.cs) / [IRoleService.cs](../Codeji.CMS.Services/Employees/Interface/IRoleService.cs) |
| Entities | [Roles](../Codeji.CMS.Repository/Entities/RolePermissions/Roles.cs), [Permission](../Codeji.CMS.Repository/Entities/RolePermissions/Permission.cs), [Module](../Codeji.CMS.Repository/Entities/RolePermissions/Module.cs), [ModulePermission](../Codeji.CMS.Repository/Entities/RolePermissions/ModulePermission.cs), [RolePermission](../Codeji.CMS.Repository/Entities/RolePermissions/RolePermission.cs) |
| Notable DTOs | `RoleModel`, `RoleWithModuleAndPermissions`, `ModuleWithPermissionsModel`, `UserCheckModel` |
| Seed migration | [SeedBaseModulesAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/SeedBaseModulesAndPermissions.cs) — global; [AddPolicyModuleAndPermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPolicyModuleAndPermissions.cs) and [AddPayrollSettingseAndItsModulePermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPayrollSettingseAndItsModulePermissions.cs) — module-add migrations |

Notable endpoints:
- `POST api/roles/CreateRole` — admin creates a custom role
- `GET  api/roles/GetAllRoles` — list of roles for current company
- `POST api/roles/AssignPermissionToRole` — grants/revokes module-permissions
- `POST api/roles/VerifyUserAccess` — used by `AuthenticateUserRequest` middleware (not typically called from clients)

Standard role types (mirrors `EnumsHelper.Roles`): Administrator (1), HR (2), Employee (3). Per-company `Roles` and `RolePermission` rows are created in [`RoleServices.AddDefaultRole`](../Codeji.CMS.Services/Employees/RoleServices.cs) during company registration.

---

## Company

**Purpose.** Company master data plus the lookups attached to each company: departments, job titles, custom attributes, leave settings, and policies (with version history).

| What | Where |
|---|---|
| Controller (per-company ops) | [CompanyController.cs](../Codeji.CMS.API/Controllers/CompanyController.cs) — route `api/company` |
| Controller (admin/global ops) | [CompanyMasterController.cs](../Codeji.CMS.API/Controllers/CompanyMasterController.cs) |
| Service | [CompanyService.cs](../Codeji.CMS.Services/Companies/CompanyService.cs), [CompanyMasterService.cs](../Codeji.CMS.Services/Companies/CompanyMasterService.cs) |
| Default seeding | [DefaultCompanySeeds.cs](../Codeji.CMS.Services/Companies/DefaultCompanySeeds.cs) — generic departments + job titles, translated for the company's `ApplicationLanguage` |
| Entities | [Company](../Codeji.CMS.Repository/Entities/Company/Company.cs), [Department](../Codeji.CMS.Repository/Entities/Company/Department.cs), [JobTitles](../Codeji.CMS.Repository/Entities/Company/JobTitles.cs), [LeaveSettings](../Codeji.CMS.Repository/Entities/Company/LeaveSettings.cs), [CustomAttributes](../Codeji.CMS.Repository/Entities/Company/CustomAttributes.cs), [CustomAttributeValue](../Codeji.CMS.Repository/Entities/Company/CustomAttributeValue.cs), [Policy](../Codeji.CMS.Repository/Entities/Company/Policy.cs), [PolicyVersion](../Codeji.CMS.Repository/Entities/Company/PolicyVersion.cs) |
| Notable DTOs | `CompanyRequestModel`, `UpdateCompanyInfoRequestModel`, `DepartmentRequestDto/ResponseDto`, `JobTitleRequestDto/ResponseDto`, `CustomAttributeRequestDto/ResponseDto`, `PolicyRequestModel/ResponseModel`, `PolicyVersionModels` |

Notable endpoints (departments/job titles/custom attributes use `CompanyMaster`'s admin-only endpoints; policies sit on `CompanyController`):
- `POST api/companymaster/AddEditDepartment`, `GetDepartmentList`, `DeleteDepartment`
- `POST api/companymaster/AddEditJobTitles`, `GetJobTitlesList`, `DeleteJobTitle`
- `POST api/companymaster/AddEditCustomAttribute`, `GetCustomAttributeList`
- `POST api/company/CreatePolicy` — `[ModulePermission(Policy, Create)]`
- `PUT  api/company/UpdatePolicy` — `[ModulePermission(Policy, Edit)]`
- `GET  api/company/GetPolicy/{id}` — `[ModulePermission(Policy, View)]`
- `DELETE api/company/DeletePolicy/{id}` — `[ModulePermission(Policy, Delete)]`

`CompanyService.Register()` is the single entry point for new tenants. It chains: `_roleService.AddDefaultRole(companyId)` → creates admin `EmpUser` → creates `Company` row → creates default `NotificationPreference` → seeds default departments + job titles via `SeedDefaultDepartmentsAndJobTitles()` (idempotent + non-fatal).

---

## Attendance

**Purpose.** Daily attendance: mark-in / mark-out, edits, monthly calendar view, attendance summaries used by payroll.

| What | Where |
|---|---|
| Controller | [AttendanceController.cs](../Codeji.CMS.API/Controllers/AttendanceController.cs) — route `api/attendance` |
| Service | [AttendanceService.cs](../Codeji.CMS.Services/Attendance/AttendanceService.cs) / [IAttendanceServices.cs](../Codeji.CMS.Services/Attendance/Interface/IAttendanceServices.cs) |
| Repository | [AttendanceRepository.cs](../Codeji.CMS.Repository/Repositories/AttendanceRepository.cs) (specialty queries on top of `MongoRepository<AttendanceModel>`) |
| Entities | [AttendanceModel.cs](../Codeji.CMS.Repository/Entities/Attendance/AttendanceModel.cs) |
| Notable DTOs | `AttendanceCreateDto`, `AttendanceUpdateDto`, `AttendanceResponseDto`, `AttendanceCalendarRequestDto`, `AttendanceSummary` |

Notable endpoints (all `[ModulePermission(Attendance, ...)]`):
- `POST api/attendance/CreateAttendance` — `Create`
- `POST api/attendance/UpdateAttendance` — `Edit`
- `POST api/attendance/GetAttendanceCalendar` — `View` — month grid view
- `POST api/attendance/GetAllAttendance` — `View` — paginated list

Total hours auto-calculated from in/out times. Mapster config does null-coalesce on update DTOs so partial updates work.

---

## Leave Management

**Purpose.** Leave types, leave policies, request → approval workflow, balance calculation, accrual.

| What | Where |
|---|---|
| Controller | [LeaveManagementController.cs](../Codeji.CMS.API/Controllers/LeaveManagementController.cs) — route `api/leavemanagement` |
| Service | [LeaveManagementService.cs](../Codeji.CMS.Services/LeaveManagement/LeaveManagementService.cs) / [ILeaveManagementService.cs](../Codeji.CMS.Services/LeaveManagement/Interface/ILeaveManagementService.cs) |
| Background job | [LeaveAccrualHostedService.cs](../Codeji.CMS.Services/BackgroundTasks/LeaveAccrualHostedService.cs) — runs on day-1 of each month |
| Entities | [LeavePolicy](../Codeji.CMS.Repository/Entities/Leave/LeavePolicy.cs), [LeaveRequest](../Codeji.CMS.Repository/Entities/Leave/LeaveRequest.cs), [EmployeeLeaveBalance](../Codeji.CMS.Repository/Entities/Leave/EmployeeLeaveBalance.cs) |
| Notable DTOs | `LeavePolicyRequest`, `UpdateLeavePolicyRequest`, `LeaveRequestDto`, `MyLeaveRequestDto`, `LeaveResponseDto`, `EmployeeLeaveBalanceResponseDto`, `LeaveBalanceFilter`, `LeaveTypeRequestDto` |

Leave types (`EnumsHelper.LeaveTypes`): `Sick`, `Casual`, `Earned`, `Maternity`, `Paternity`. Statuses: `Pending`, `Accepted`, `Rejected`, `WithDrawn`. Accrual periods (`LeaveAccrualPeriod`): `Monthly`, `Yearly`, `None`.

Notable endpoints (`[ModulePermission(Leave_Management, ...)]`):
- `POST api/leavemanagement/CreateLeavePolicy` — `Create`
- `POST api/leavemanagement/CreateLeaveRequest` — employee submits
- `POST api/leavemanagement/GetMyLeaveRequests` — `View`
- `POST api/leavemanagement/ApproveLeaveRequest` — `Edit` — HR approves/rejects, triggers `LeaveReplyMail`
- `POST api/leavemanagement/GetLeaveBalance` — `View`
- `POST api/leavemanagement/GetAllLeaveRequests` — `View` — HR view

---

## Calendar / Holidays

**Purpose.** Per-company holiday calendar plus events. Calendar feed also exposes employee birthdays and work anniversaries (computed at read time).

| What | Where |
|---|---|
| Controller | [CalendarController.cs](../Codeji.CMS.API/Controllers/CalendarController.cs) — route `api/calendar` |
| Service | [CalendarServices.cs](../Codeji.CMS.Services/Calendar/CalendarServices.cs) / [ICalendarServices.cs](../Codeji.CMS.Services/Calendar/Interface/ICalendarServices.cs) |
| Entities | [CalendarEntity](../Codeji.CMS.Repository/Entities/Calendar/CalendarEntity.cs), [Holidays](../Codeji.CMS.Repository/Entities/Holiday/Holidays.cs) |
| Notable DTOs | `CalendarRequestDto`, `CalendarResponseDto`, `HolidayResponseDto`, `CalendarFilters` |
| Enums | `CalendarItem` (`Holiday=1, Event=2`), `CalendarResponseItem` (`Holiday, Event, Birthday, WorkAnniversary`) |

Permissions: `[ModulePermission(Calendar, ...)]`.

---

## Notice Board

**Purpose.** Internal company-wide announcements with priority. Body is HTML; sanitized server-side.

| What | Where |
|---|---|
| Controller | [NoticeBoardController.cs](../Codeji.CMS.API/Controllers/NoticeBoardController.cs) — route `api/noticeboard` |
| Service | [NoticeBoardServices.cs](../Codeji.CMS.Services/NoticeBoard/NoticeBoardServices.cs) / [INoticeBoardService.cs](../Codeji.CMS.Services/NoticeBoard/Interface/INoticeBoardService.cs) |
| Entity | [Notice.cs](../Codeji.CMS.Repository/Entities/NoticeBoard/Notice.cs) |
| Notable DTOs | `AddNoticeRequest`, `UpdateNoticeDto`, `GetNoticeRequest`, `NoticeViewModel`, `MyNoticeDTO` |
| Sanitization | DTO body field decorated with `[Sanitize]`; service calls `Sanitizer.SanitizeProperties(request)` before save — see [Sanitizer.cs](../Codeji.CMS.Utility/Sanitizer.cs) |

Permissions: `[ModulePermission(Notice_Board, ...)]`. Priority levels: `General`, `Important`, `Urgent` (`EnumsHelper.NoticeType`).

---

## Job Vacancies

**Purpose.** CRUD for job postings displayed on the public career portal (frontend `/career/:id`).

| What | Where |
|---|---|
| Controller | [JobVacancyController.cs](../Codeji.CMS.API/Controllers/JobVacancyController.cs) — route `api/jobvacancy` |
| Service | [JobVacancyService.cs](../Codeji.CMS.Services/Recruitments/JobVacancyService.cs) / [IJobVacancy.cs](../Codeji.CMS.Services/Recruitments/Interface/IJobVacancy.cs) |
| Entity | [JobVacancy.cs](../Codeji.CMS.Repository/Entities/Recruitments/JobVacancy.cs) |
| Notable DTOs | `JobVacancyModel`, `GetJobVacancyModel`, `JobSearchModel` |

Notable endpoints (`[ModulePermission(Jobs, ...)]`):
- `POST api/jobvacancy/CreateJobVacancy` — `Create`
- `POST api/jobvacancy/GetAllJobVacancy` — `View`
- `PUT  api/jobvacancy/UpdateJobVacancy` — `Edit`
- `DELETE api/jobvacancy/DeleteJobVacancy` — `Delete`
- `POST api/jobvacancy/GetVacancyForApplyNow` — `[AllowAnonymous]` — used by the public career page (with `cId` header)

---

## Applicants

**Purpose.** Applicant tracking — pipeline (`New → InProgress → OnHold → Shortlisted → Selected → Rejected → ReApply`), status transitions trigger emails, full audit log.

| What | Where |
|---|---|
| Controller | [ApplicantsController.cs](../Codeji.CMS.API/Controllers/ApplicantsController.cs) — route `api/applicants` |
| Service | [ApplicantServices.cs](../Codeji.CMS.Services/Recruitments/ApplicantServices.cs) / [IApplicantsService.cs](../Codeji.CMS.Services/Recruitments/Interface/IApplicantsService.cs) |
| Entities | [Applicant](../Codeji.CMS.Repository/Entities/Recruitments/Applicant.cs), [ApplicantLogs](../Codeji.CMS.Repository/Entities/Recruitments/ApplicantLogs.cs), [Resume](../Codeji.CMS.Repository/Entities/Recruitments/Resume.cs), [MailTemplate](../Codeji.CMS.Repository/Entities/Recruitments/MailTemplate.cs) |
| Notable DTOs | `ApplicantAddEditModel`, `ApplicantRegisterModel`, `ApplicantViewModel`, `ApplicantResultFilters`, `ApplicantLogFilterModel`, `ResumeApplicantModel` |

Notable endpoints (`[ModulePermission(Applications, ...)]`):
- `POST api/applicants/GetAllApplicants` — `View`
- `POST api/applicants/AddEditApplicant` — `Create` / `Edit`
- `POST api/applicants/UpdateApplicantStatus` — `Edit` — moves stage; sends one candidate email when `ActivityType` changes, using `SelectedMail` / `RejectedMail` where defined and fallback applicant-stage messaging for the other supported stages
- `POST api/applicants/DeleteApplicant` — `Delete` (soft)
- `POST api/applicants/SubmitApplyNow` — `[AllowAnonymous]` — used by public career page

Public and authenticated application creation also notify internal recruitment recipients using `ApplyNowMailToHR`. Recipients are resolved from `JobVacancy.RecruiterContactEmail` plus active verified company users in `Administrator` and `HR` roles.

`ApplicantLogs` records every status change with actor + timestamp + comment — drives the [Process Log](#applicants) view in the frontend.

---

## Payroll

**Purpose.** Per-employee monthly salary structure (basic + allowances − deductions), monthly batch generation, payslip PDFs.

| What | Where |
|---|---|
| Controllers | [PayRollController.cs](../Codeji.CMS.API/Controllers/PayRollController.cs), [AutoPayrollController.cs](../Codeji.CMS.API/Controllers/AutoPayrollController.cs) — route `api/payroll` |
| Service | [PayRollServices.cs](../Codeji.CMS.Services/PayRoll/PayRollServices.cs), [AutoPayRollServices.cs](../Codeji.CMS.Services/PayRoll/AutoPayRollServices.cs), [SalaryService.cs](../Codeji.CMS.Services/SalaryService.cs), [SalaryCalculator.cs](../Codeji.CMS.Services/SalaryCalculator.cs) |
| Background job | [PayRollHostedServices.cs](../Codeji.CMS.Services/PayRoll/PayRollHostedServices.cs) — scheduled monthly run |
| PDF generation | [PdfService.cs](../Codeji.CMS.Services/PdfService.cs) — PuppeteerSharp (headless Chrome) renders an HTML template via `HtmlTemplate.Render()` |
| Entity | [EmpPayRoll.cs](../Codeji.CMS.Repository/Entities/Employees/EmpPayRoll.cs) |
| Notable DTOs | `AddUpdatePayRollRequestDto`, `EmployeePayRollRequestDto`, `GetEmpPayRollRequestDto/ResponseDto`, `PaySlipRequestDto`, `SalarySlipTemplateModel` |

Notable endpoints (`[ModulePermission(PayRoll, ...)]`):
- `POST api/payroll/CreatePayroll` — `Create`
- `POST api/payroll/GetAllPayroll` — `View` — paginated
- `PUT  api/payroll/UpdatePayroll` — `Edit`
- `DELETE api/payroll/DeletePayroll` — `Delete`
- `POST api/payroll/GenerateSalarySlip` — returns PDF bytes

Payroll Settings (per-company defaults like accrual + tax rules) is gated under `Payroll_Settings` — module added by [AddPayrollSettingseAndItsModulePermissions.cs](../Codeji.CMS.Migrations/Migrations/AddPayrollSettingseAndItsModulePermissions.cs).

---

## Salary

**Purpose.** Per-employee compensation record (separate from monthly payroll). HR sets it; the employee can read their own.

| What | Where |
|---|---|
| Controller | [SalaryController.cs](../Codeji.CMS.API/Controllers/SalaryController.cs) — route `api/salary` |
| Service | [SalaryService.cs](../Codeji.CMS.Services/SalaryService.cs) / [ISalaryServices.cs](../Codeji.CMS.Services/Interface/ISalaryServices.cs) |
| Repository | [SalaryRepository.cs](../Codeji.CMS.Repository/Repositories/SalaryRepository.cs) |
| Notable DTOs | `CreateSalaryDto`, `SalaryModel`, `SalaryResponseDto` |

Permissions: `[ModulePermission(Employees, Create | Edit)]` (intentional — salary is part of an employee record).

---

## Dashboard

**Purpose.** Aggregated read-only stats for the home page.

| What | Where |
|---|---|
| Controller | [DashboardController.cs](../Codeji.CMS.API/Controllers/DashboardController.cs) — route `api/dashboard` |
| Service | [DashboardServices.cs](../Codeji.CMS.Services/Dashboard/DashboardServices.cs) / [IDashboardService.cs](../Codeji.CMS.Services/Dashboard/Interface/IDashboardService.cs) |
| Notable DTOs | `ApplicationDataResponseDto`, `GenderDetailsResponseModel`, `UpComingHolidayResponseDto`, `UpcomingCelebrationDto`, `DepartmentEmpResponseDto` |

Notable endpoints:
- `GET api/dashboard/GetDashboard` — combined stats payload
- `GET api/dashboard/GenderDetails` — pie-chart data
- `GET api/dashboard/UpcomingHoliday`
- `GET api/dashboard/UpcomingCelebrations` — birthdays + work anniversaries

---

## Where to look when adding a new business module

1. Add a controller next to the existing ones in [Codeji.CMS.API/Controllers/](../Codeji.CMS.API/Controllers/).
2. Add a service folder under [Codeji.CMS.Services/](../Codeji.CMS.Services/) with `XxxService.cs` + `Interface/IXxxService.cs`.
3. Add entity classes under [Codeji.CMS.Repository/Entities/<Domain>/](../Codeji.CMS.Repository/Entities/) — they must inherit `BaseClass` for multi-tenancy + soft delete.
4. Add request/response DTOs under [Codeji.CMS.DTO/<Domain>/](../Codeji.CMS.DTO/).
5. Register the service in [ServicesRegistration.cs](../Codeji.CMS.Services/Registration/ServicesRegistration.cs).
6. Add an `AppModule` constant in [ConstraintHelper.cs](../Codeji.CMS.Utility/Constraints/ConstraintHelper.cs) and a migration to seed the new `Module` + `ModulePermission` rows — see [add-new-feature.md](./add-new-feature.md).
