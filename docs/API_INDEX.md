# API Index

Last verified against code: 2026-09-02. Controller annotations are the authoritative endpoint inventory; routes can combine class `[Route]`, method `[Route]`, and HTTP attributes. This index assigns every active controller to a documentation module and avoids inventing normalised route names.

| Module | Controllers | Primary source |
|---|---|---|
| Authentication | `AccountController` | `Codeji.CMS.API/Controllers/AccountController.cs` |
| Employee / offboarding | `UserController`, `PublicFormerEmployeePortalController` | `offboarding/API.md` |
| Attendance / WFH | `AttendanceController`, `MyAttendanceController`, `AttendancePenaltyController`, `AttendanceStatusSettingsController`, `WeeklyOffSettingsController`, `WorkFromHomeController` | `attendance/README.md` |
| Leave | `LeaveManagementController` | `leave-management/README.md` |
| Payroll | `PayRollController`, `AutoPayrollController`, `PayrollDivisorPolicyController`, `SalaryController` | `payroll/README.md` |
| Company / calendar | `CompanyController`, `CompanyMasterController`, `CalendarController` | `company/README.md` |
| Recruitment / careers | `ApplicantsController`, `JobVacancyController`, `PublicCareerController`, `PublicCareerEngagementController`, `PublicCareerSubscriptionController`, `PublicCompanyProfileController`, `CareerProfileController` | `recruitment/README.md` |
| Roles | `RolesController` | `authentication/README.md` |
| Dashboard / notices | `DashboardController`, `NoticeBoardController` | `dashboard-notices/README.md` |

Manual testing: authenticate first unless the controller/action is deliberately public; use the exact request DTO in `Codeji.CMS.DTO`; test a cross-company identifier and a stale `Version` for stateful employee-exit writes. Automated coverage is not claimed here.
