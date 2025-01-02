
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Codeji.CMS.API.App_Start
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        UserEditRoleCheckModel userForEdit; 
        public CustomAuthorizeAttribute()
        {
            Module = string.Empty;
            Modules = Array.Empty<string>();
            Role = Array.Empty<string>();
        }
        public string Module { get; set; }
        public string[] Role { get; set; }
        public string[] Modules { get; set; }
        //For Voting Survey
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context != null)
            {
                try
                {
                    HttpRequest? request = context.HttpContext.Request;
                    request.EnableBuffering();
                    using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, true))
                    {
                        string content = await reader.ReadToEndAsync();
                        userForEdit = JsonConvert.DeserializeObject<UserEditRoleCheckModel>(content) ?? new UserEditRoleCheckModel();
                        request.Body.Position = 0;
                    }
                }
                catch (Exception) { }


                if (!await IsValidRole(context))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = Convert.ToInt32(HttpStatusCode.Unauthorized),
                        Content = "Access Denied"
                    };
                }
            }
        }
        /// <summary>
        /// It will return boolean value that user has valid role or not.
        /// </summary>
        /// <param name="userForEdit"></param>
        /// <param name="filterContext"></param>
        /// <returns></returns>
        private  async Task<bool> IsValidRole(AuthorizationFilterContext filterContext)
        {
            IHttpContextAccessor? _httpContextAccessor = filterContext.HttpContext.RequestServices.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
            IRoleBusiness? _roleBusiness = filterContext.HttpContext.RequestServices.GetService(typeof(IRoleBusiness)) as IRoleBusiness;

            string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);

            if ( _httpContextAccessor == null ||_roleBusiness==null)
                return false;
           return await _roleBusiness.VerifyUserAccess(Module,Role,userId,companyId,userForEdit);
           
        }
    }
        public class UserRoleModel
        {
            public UserRoleModel()
            {
                UserId = "";
                RoleId = "";
                CompanyId = "";
            }
            public string UserId { get; set; }
            public string RoleId { get; set; }
            public string CompanyId { get; set; }
        }
        public class FilesExtensionsAttribute : Attribute, IAsyncActionFilter
        {
            private string[] _extensions;
            public FilesExtensionsAttribute(string[] extensions) { _extensions = extensions; }
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                IFormFile? file = context.HttpContext.Request.Form.Files.Count > 0 ? context.HttpContext.Request.Form.Files[0] : null;
                if (file != null)
                {
                    string extension = Path.GetExtension(file.FileName);
                    if (!_extensions.Contains(extension.ToLower()))
                    {
                        context.Result = new UnsupportedMediaTypeResult();
                    }
                    else { await next(); }
                }
                else { await next(); }
            }
        }
    
}
