using System;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Newtonsoft.Json;

namespace Codeji.CMS.API.App_Start
{
    public class CompanyIdMiddleware
    {
        public async static Task<HttpContext?> AuthenticateUserRequest(IHttpContextAccessor _IhttpContextAccessor)
        {
            HttpContext context = _IhttpContextAccessor?.HttpContext;
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            // check if user id is present in request and path is not in pathForNOCompanyIdRequired then return unauthorized

            string[] pathForNOCompanyIdRequired = [
                 "/api/account/antiforgerytoken/" ,
                 "/api/app/checkAppVersion",
                 "/api/account/register",
                 "/api/account/login",
                 "/api/account/refresh-token",
                 "/api/account/verify-email",
                 "/api/CreateNewPassword",
                 "/api/account/CreateNewPassword",
                 "/api/account/ResetPassword",
                 "/api/company/career",
                 "/api/public",
                 "/api/VerificationCaptch",
                 "/api/SendEmail",
                 "/fs/",
                ];
            IEmployeeService? _employeeService = context.RequestServices.GetService(typeof(IEmployeeService)) as IEmployeeService;
            ICompanyService? _companyService = context.RequestServices.GetService(typeof(ICompanyService)) as ICompanyService;

            string company_Id = CurrentContext.CompanyId(_IhttpContextAccessor);
            string userId = CurrentContext.UserId(_IhttpContextAccessor);
            #region check company id and user id are active
            //  company id not present in request and path not in pathForNOCompanyIdRequired then return unauthorized
            if (string.IsNullOrEmpty(company_Id) && !pathForNOCompanyIdRequired.Any(x =>
                context.Request.Path.Value?.Contains(x, StringComparison.OrdinalIgnoreCase) == true))
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Required Company Id");
                return null;
            }
            //check if company is active
            if (!string.IsNullOrEmpty(company_Id))
            {
                bool company = await _companyService.IsActiveCompanyExist(company_Id);
                if (!company)
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid Company Id");
                    return null;
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
                    return null;
                }
            }
            #endregion
            //Check if user have valid role and permission
            #region "Check if user have valid role and permission"
            var endpoint = context.GetEndpoint();
            if (endpoint != null && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(company_Id))
            {
                var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<ModulePermissionAttribute>();
                // Check if the endpoint has the ModulePermissionAttribute
                if (permissionAttributes == null || !permissionAttributes.Any())
                    return context;

                IRoleService? _roleServices = context.RequestServices.GetService(typeof(IRoleService)) as IRoleService;
                // try
                // {
                //     // Read the request body to get the userForEdit object
                //     HttpRequest? request = context.Request;
                //     request.EnableBuffering();
                //     using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, true))
                //     {
                //         string content = await reader.ReadToEndAsync();
                //         var userForEdit = JsonConvert.DeserializeObject<UserCheckModel>(content) ?? new UserCheckModel();
                //         request.Body.Position = 0;
                //     }
                // }
                // catch (Exception) { }
                var permissionAttribute = permissionAttributes.FirstOrDefault();
                bool isValid = await _roleServices.VerifyUserAccess(permissionAttribute.Module, permissionAttribute.Permissions, userId, company_Id);
                // Check if the user has the required permissions for the module
                if (!isValid)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access Denied");
                    return null;
                }
            }
            #endregion
            return context;

        }
    }
}
