using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Utility.middlewares;


public static class CurrentContext
{
    public static string UserId(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor == null || !(httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false))
            return string.Empty;
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.user_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";

    }

    public static string UserRoleId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.role_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }

    public static string CompanyId(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor != null && (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false))
        {
            ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
            return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.company_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
        }
        else
            return httpContextAccessor?.HttpContext?.Request.Headers["cId"].ToString() ?? string.Empty;
    }
    public static string AdminUserId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.admin_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }

    public static string GetLanguage(IHttpContextAccessor httpContextAccessor)
    {
        var acceptLanguage = httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].ToString();
        return acceptLanguage ?? string.Empty;
    }
}