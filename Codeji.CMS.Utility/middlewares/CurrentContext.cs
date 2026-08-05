using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Utility.middlewares;


public static class CurrentContext
{
    private static string? ResolveSingleClaim(IHttpContextAccessor? httpContextAccessor, string claimType)
    {
        if (httpContextAccessor?.HttpContext?.User?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return null;
        }

        var matches = identity.Claims
            .Where(a => a.Type == claimType)
            .Select(a => a.Value)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    public static string UserId(IHttpContextAccessor httpContextAccessor)
    {
        return ResolveSingleClaim(httpContextAccessor, ClaimTypesEnum.user_id.ToString()) ?? string.Empty;
    }

    public static string UserRoleId(IHttpContextAccessor httpContextAccessor)
    {
        return ResolveSingleClaim(httpContextAccessor, ClaimTypesEnum.role_id.ToString()) ?? string.Empty;
    }

    public static string CompanyId(IHttpContextAccessor httpContextAccessor)
    {
        var authenticatedCompanyId = ResolveSingleClaim(httpContextAccessor, ClaimTypesEnum.company_id.ToString());
        if (!string.IsNullOrWhiteSpace(authenticatedCompanyId))
        {
            return authenticatedCompanyId;
        }

        // Tenant context is authoritative only when it comes from the authenticated
        // JWT. Anonymous endpoints must resolve a company from a trusted route value
        // (for example, a public company code) or an already-owned resource, never a
        // caller-controlled request header.
        return string.Empty;
    }
    public static string AdminUserId(IHttpContextAccessor httpContextAccessor)
    {
        return ResolveSingleClaim(httpContextAccessor, ClaimTypesEnum.admin_id.ToString()) ?? string.Empty;
    }

    public static string GetLanguage(IHttpContextAccessor httpContextAccessor)
    {
        var acceptLanguage = httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].ToString();
        return acceptLanguage ?? string.Empty;
    }
}
