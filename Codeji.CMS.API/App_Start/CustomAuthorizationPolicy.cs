// custom authorization policy to authorize user request 

using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.API.App_Start;

public class RoleRequirement : IAuthorizationRequirement
{
    public Roles RoleType { get; set; }
    public RoleRequirement(Roles roleType)
    {
        RoleType = roleType;
    }
}

// Authorization handler to validate authorization logic

public class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public RoleHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }
        // Get current user role id 
        string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        IRoleService? _roleService = httpContext?.RequestServices.GetService(typeof(IRoleService)) as IRoleService;
        if (_roleService == null)
        {
            context.Fail();
            return;
        }
        bool isMatched = await _roleService.IsRoleTypeMatch(roleId, requirement.RoleType, companyId);
        if (!isMatched)
        {
            context.Fail();
            return;
        }
        context.Succeed(requirement);
        return;
    }
}
