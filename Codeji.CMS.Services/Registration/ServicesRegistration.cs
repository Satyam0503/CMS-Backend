
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Holiday;
using Codeji.CMS.Services.Holiday.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.NoticeBoard;
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
        services.AddScoped<IHolidayService, HolidayService>();

        return services;
    }
}