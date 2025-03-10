using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public RolesController(IRoleService roleService, IHttpContextAccessor httpContextAccessor)
        {
            _roleService = roleService;
            _httpContextAccessor = httpContextAccessor;

        }

        //Add New Roles
        //[Route("AddEditRoles")]
        //[HttpPost]
        //public async Task<Result<RoleModel>> AddEditRoles(RoleModel roles)
        //{
        //    return await _roleService.AddEditRoles(roles);
        //}

        [HttpGet]
        [Route("GetRoles")]
        [Authorize]
        public async Task<Result<RoleModel>> GetRoles()
        {
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            List<RoleModel> roles = await _roleService.GetRoles(companyId);
            return new Result<RoleModel>()
            {
                MethodResults = roles ?? new List<RoleModel>(),
                Success = true
            };
        }
        [HttpGet]
        [Route("GetRolePermission")]
        [Authorize]
        public async Task<Result<ModuleWithPermissionsModel>> GetRolePermission()
        {
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            string roleId = CurrentContext.CurrentUserRoleId(_httpContextAccessor);
            List<ModuleWithPermissionsModel> role = await _roleService.GetRoleWithPermissions(roleId, companyId);
            return new Result<ModuleWithPermissionsModel>()
            {
                TotalRecords = role.Count,
                MethodResults = role ?? new List<ModuleWithPermissionsModel>(),
                Success = true
            };
        }
    }
}
