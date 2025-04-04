using System;
using System.Text;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Newtonsoft.Json;

namespace Codeji.CMS.API.App_Start
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
  public class ModulePermissionAttribute : Attribute
  {
    public string Module { get; }
    public string[] Permissions { get; }

    public ModulePermissionAttribute(string module, params string[] permissions)
    {
      Module = module;
      Permissions = permissions;
    }
  }
  public class PermissionMiddleware
  {
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var endpoint = context.GetEndpoint();
      if (endpoint != null)
      {
        // Check if the endpoint has the ModulePermissionAttribute
        var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<ModulePermissionAttribute>();
        IRoleService? _roleServices = context.RequestServices.GetService(typeof(IRoleService)) as IRoleService;
        try
        {
          // Read the request body to get the userForEdit object
          HttpRequest? request = context.Request;
          request.EnableBuffering();
          using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, true))
          {
            string content = await reader.ReadToEndAsync();
            var userForEdit = JsonConvert.DeserializeObject<UserCheckModel>(content) ?? new UserCheckModel();
            request.Body.Position = 0;
          }
        }
        catch (Exception) { }
        var permissionAttribute = permissionAttributes.FirstOrDefault();
        string userId = context.User.FindFirst("user_id")?.Value ?? "";
        string companyId = context.User.FindFirst("company_id")?.Value ?? "";

        bool isValid = await _roleServices.VerifyUserAccess(permissionAttribute.Module, permissionAttribute.Permissions, userId, companyId);
        // Check if the user has the required permissions for the module
        if (!isValid)
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          await context.Response.WriteAsync("Access Denied");
          return;
        }


      }

      await _next(context);
    }
  }
}