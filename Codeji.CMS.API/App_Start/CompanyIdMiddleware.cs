using System;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Newtonsoft.Json;

namespace Codeji.CMS.API.App_Start
{
    public class CompanyIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CompanyIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string[] pathForNOCompanyIdRequired = new string[] {
                 "/api/account/antiforgerytoken/" ,
                 "/api/app/checkAppVersion",
                 "/api/account/register",
                 "/api/account/login",
                 "/api/account/getSignedUserDetails",
                 "/api/CreateNewPassword",
                 "/api/VerificationCaptch",
                 "/fs/"};
            IEmployeeService? _employeeService = context.RequestServices.GetService(typeof(IEmployeeService)) as IEmployeeService;
            ICompanyService? _companyService = context.RequestServices.GetService(typeof(ICompanyService)) as ICompanyService;
            string company_Id = string.Empty;
            string userId = string.Empty;
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                company_Id = context.User.Claims.FirstOrDefault(c => c.Type == "company_id")?.Value ?? string.Empty;
                userId = context.User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(company_Id))
                {
                    string token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                    JwtSecurityToken? jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                    if (jwtToken != null)
                        company_Id = jwtToken.Claims?.FirstOrDefault(c => c.Type == "company_id")?.Value ?? string.Empty;
                }
            }
            else
                company_Id = context.Request.Headers["cId"].ToString();
            //  company id not present in request and path not in pathForNOCompanyIdRequired then return unauthorized
            if (string.IsNullOrEmpty(company_Id) && !pathForNOCompanyIdRequired.Any(x => context.Request.Path.Value.Contains(x)))
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Required Company Id");
                return;
            }
            context.Items["CompanyId"] = company_Id;
            //check if company is active
            if (!string.IsNullOrEmpty(company_Id))
            {
                bool company = await _companyService.IsActiveCompanyExist(company_Id);
                if (!company)
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid Company Id");
                    return;
                }
            }
            //check if user is active
            if (!string.IsNullOrEmpty(userId))
            {
                bool user = await _employeeService.IsUserActive(userId);
                if (!user)
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid User Id");
                    return;
                }
            }

            Console.WriteLine("CompanyId: " + context.Items["CompanyId"]);
            await _next(context);
        }
    }
}

