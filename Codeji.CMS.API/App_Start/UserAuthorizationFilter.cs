
using Microsoft.AspNetCore.Mvc.Filters;

namespace Codeji.CMS.API.App_Start
{
    public class UserAuthorizationFilter : ActionFilterAttribute, IActionFilter
    {
        public bool isUserRole { get; set; }
        public bool isPlanPermission { get; set; }
        public bool IsCheckUserStatus { get; set; }
        public bool IsClientAdminCheck { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // IUserBusiness? _userBusiness = filterContext.HttpContext.RequestServices.GetService(typeof(IUserBusiness)) as IUserBusiness;
            // ICompanyBusiness? _companyBusiness = filterContext.HttpContext.RequestServices.GetService(typeof(ICompanyBusiness)) as ICompanyBusiness;
            // IUserAdminBusiness? _userAdminBusiness = filterContext.HttpContext.RequestServices.GetService(typeof(IUserAdminBusiness)) as IUserAdminBusiness;
            // IHttpContextAccessor? _httpContextAccessor = filterContext.HttpContext.RequestServices.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
            // IMiddleware? _middleware = filterContext.HttpContext.RequestServices.GetService(typeof(IMiddleware)) as IMiddleware;
            // IRoleBusiness? _roleBusiness = filterContext.HttpContext.RequestServices.GetService(typeof(IRoleBusiness)) as IRoleBusiness;
            // IMapper? _mapper = filterContext.HttpContext.RequestServices.GetService(typeof(IMapper)) as IMapper;

            // if (_httpContextAccessor == null || _companyBusiness == null || _middleware == null || _userBusiness == null || _roleBusiness == null || _mapper == null || _userAdminBusiness == null) return;

            // string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
            // string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            // bool isDemo = ConfigManager.DemoUserId == userId && ConfigManager.DemoUserCompanyId == companyId;
            // if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId))
            // {
            //     CompanyModel company = _companyBusiness.GetCompanyById(companyId);
            //     if (null == company || string.IsNullOrEmpty(company.CompanyId))
            //     {
            //         filterContext.Result = new UnauthorizedObjectResult("");
            //         return;
            //     }
            //     UserDetailModel user = _middleware.GetUserById(userId);
            //     if (IsClientAdminCheck && user != null && !string.IsNullOrEmpty(user.RoleId) && !_userBusiness.CheckForAdminUser(user.RoleId, user.CompanyId))
            //     {
            //         filterContext.Result = new UnauthorizedObjectResult("Unauthorized Access");
            //         return;
            //     }
            //     bool result = false;
            //     if (user == null || company.CompanyStatus == false)
            //     {
            //         filterContext.Result = new UnauthorizedObjectResult("Unauthorized Access");
            //         return;
            //     }
            //     else
            //     {
            //         if (!string.IsNullOrEmpty(user.RoleId))
            //         {
            //             List<ModuleWithPermissionsModel> modulePremissions = _roleBusiness.GetRoleWithPermissions(user.RoleId, companyId);
            //             List<ModulePremissionModal> modulePremissionModal = new();
            //             List<ModulePremissionModal> userPermissionList = new();
            //             _mapper.Map(modulePremissions, modulePremissionModal);
            //             _mapper.Map(modulePremissions, userPermissionList);
            //             result = ScrambledEquals(modulePremissionModal, userPermissionList);
            //         }
            //         if (isPlanPermission)
            //         {
            //             result = _userAdminBusiness.GetApplicationModule(companyId, applicationModuleType.GetHashCode().ToString(), isDemo);
            //             if (!result)
            //             {
            //                 result = _userAdminBusiness.GetApplicationModule(companyId, applicationModuleType2.GetHashCode().ToString(), isDemo);
            //                 if (!result)
            //                 {
            //                     filterContext.Result = new UnauthorizedObjectResult("Unauthorized Access");
            //                     return;
            //                 }
            //             }
            //         }
            //         if ((user.isActive || IsCheckUserStatus) && result)
            //         {
            //             base.OnActionExecuting(filterContext);
            //         }
            //         else
            //         {
            //             //filterContext.Result = new HttpResponseMessage(HttpStatusCode.UpgradeRequired) { Content = new StringContent("Something went wrong") };
            //             filterContext.Result = new BadRequestObjectResult("Something went wrong");
            //         }
            //     }
            // }
            // else if (!string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(companyId))
            // {
            //     filterContext.Result = new BadRequestObjectResult("1000");
            //     return;
            // }
            // else
            // {
            //     filterContext.Result = new UnauthorizedObjectResult("Unauthorized Access");
            //     return;
            // }
        }
    }
}
