
using Codeji.CMS.Services.Interface;
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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApplicantsService, ApplicantServices>();
        services.AddScoped<IRoleBusiness, RoleBusiness>();

        return services;
  }
}