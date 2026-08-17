using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public RolesController(IRoleService roleService, IHttpContextAccessor httpContextAccessor)
        {
            _roleService = roleService;
            _httpContextAccessor = httpContextAccessor;

        }

        [HttpGet]
        [Route("GetRoles")]
        public async Task<Result<RoleModel>> GetRoles([FromQuery] bool? excludeAdmin)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            List<RoleModel> roles = await _roleService.GetRoles(companyId, excludeAdmin);
            return new Result<RoleModel>()
            {
                MethodResults = roles ?? [],
                TotalRecords = roles != null ? roles.Count : 0,
                Success = true
            };
        }
        [HttpGet]
        [Route("GetRolePermission")]
        public async Task<Result<ModuleWithPermissionsModel>> GetRolePermission()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
            List<ModuleWithPermissionsModel> role = await _roleService.GetRoleWithPermissions(roleId, companyId);
            return new Result<ModuleWithPermissionsModel>()
            {
                TotalRecords = role.Count,
                MethodResults = role ?? new List<ModuleWithPermissionsModel>(),
                Success = true
            };
        }

        [HttpPost]
        [Route("AddEditRole")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result> AddRole(RoleWithModuleAndPermissions model)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            return await _roleService.AddEditRoles(model, companyId);
        }

        [HttpGet]
        [Route("GetAllRolesWithPermission")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<ModuleWithPermissionsModel>> GetModulePermission()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            List<ModuleWithPermissionsModel> data = await _roleService.GetAllRolesWithPermission(companyId);
            return new Result<ModuleWithPermissionsModel>()
            {
                TotalRecords = data.Count,
                MethodResults = data,
                Success = true
            };
        }

        [HttpGet]
        [Route("GetRoleWithPermissionById")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<ModuleWithPermissionsModel>> GetModulePermissionyId([FromQuery] string roleId)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            List<ModuleWithPermissionsModel> data = await _roleService.GetRoleWithPermissions(roleId, companyId);
            return new Result<ModuleWithPermissionsModel>()
            {
                TotalRecords = data.Count,
                Success = true,
                MethodResults = data,

            };
        }

        [HttpGet]
        [Route("GetRoleById/{roleId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<RoleModel>> GetRoleById(string roleId)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var data = await _roleService.GetRoleById(roleId, companyId);
            return new Result<RoleModel>()
            {
                StatusCode = 200,
                Success = true,
                MethodResult = data,
            };
        }

        [HttpPatch]
        [Route("UpdateAppAccessForRole/{roleId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result> UpdateAppAccessForRole(string roleId, [FromBody] bool hasAppAccess)
        {
            var result = await _roleService.UpdateAppAccessForRole(roleId, hasAppAccess);
            return result;
        }
    }
}
