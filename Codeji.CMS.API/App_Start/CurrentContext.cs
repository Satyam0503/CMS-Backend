using System.Net.Http.Headers;
using System.Security.Claims;

namespace Codeji.CMS.API.App_Start;

public enum ClaimTypesEnum
{
    CompanyId,
    role_id,
    UserPermissionRole,
    user_id,
    company_id,
    admin_id,
    admin_company_id,
}
public static class CurrentContext
{
    public static string CurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.user_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }

    public static string CurrentUserRoleId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.role_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }

    public static string CurrentUserCompanyId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.company_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }
    public static string CurrentAdminUserId(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims.Where(a => a.Type == ClaimTypesEnum.admin_id.ToString()).Select(a => a.Value).SingleOrDefault() ?? "";
    }
    public static string GetUsersSelectedLang(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.Headers["lang"] ?? "fr";
    }
    public static string GetDefaultLanguage(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.Headers["d-lang"] ?? "fr";
    }

    //Used for client access.
    public static bool IsClientAccess(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity;
        return identity?.Claims?.Any(a => a.Type == ClaimTypesEnum.admin_id.ToString()) ?? false;
    }

    //Used to check Partner account
    public static bool IsPartnerAccount(IHttpContextAccessor httpContextAccessor)
    {
        ClaimsIdentity? identity = httpContextAccessor?.HttpContext?.User.Identity as ClaimsIdentity ?? null;
        return identity?.Claims.Any(c => c.Type.ToLower() == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value.ToLower() == "partner") ?? false;
    }

    public async static Task<string> GetEndpointtokenAsync(string uri, IHttpContextAccessor httpContext)
    {
        string content = "";
        using (HttpClient client = new HttpClient())
        {
            Microsoft.Extensions.Primitives.StringValues token = httpContext.HttpContext.Request.Headers["Authorization"];
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Add("Authorization", token);
            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = response.Content.ReadAsStringAsync().Result ?? "";
                //content = JsonConvert.DeserializeObject<responseData>(responseContent).data;
            }
        }
        return content;
    }
}