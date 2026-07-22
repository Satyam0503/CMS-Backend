
using Codeji.CMS.Services.Account;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.Calendar;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.Extensions.DependencyInjection;
namespace Codeji.CMS.Services.Registration;

public static class ServicesRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        //   services.AddSingleton<IColleagueBusiness, ColleagueBusiness>();
        services.AddScoped<IMiddlewareService, MiddlewareService>();
        services.AddScoped<IApplicantsService, ApplicantServices>();
        services.AddScoped<IRoleService, RoleServices>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IJobVacancy, JobVacancyService>();
        services.AddScoped<ICompanyMasterService, CompanyMasterService>();
        services.AddScoped<IDashboardService, DashboardServices>();
        services.AddScoped<INoticeBoardService, NoticeBoardServices>();
        services.AddScoped<ICalendarServices, CalendarServices>();
        services.AddScoped<ILeaveManagementService, LeaveManagementService>();
        services.AddScoped<IAccountServices, AccountServices>();
        services.AddScoped<IPayRollServices, PayRollServices>();
        services.AddScoped<IAttendanceStatusService, AttendanceStatusService>();
        services.AddScoped<IAttendancePenaltyService, AttendancePenaltyService>();
        services.AddScoped<IWeeklyOffService, WeeklyOffService>();
        services.AddScoped<IAttendanceEditGuard, AttendanceEditGuard>();
        services.AddScoped<ICompanyWorkingCalendarService, CompanyWorkingCalendarService>();
        services.AddScoped<ILeaveAttendanceReconciliationService, LeaveAttendanceReconciliationService>();
        services.AddScoped<IPayrollDivisorPolicyService, PayrollDivisorPolicyService>();
        return services;
    }
}
