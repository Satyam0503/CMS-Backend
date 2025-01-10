using Codeji.CMS.Domain.Models;
using System.Net;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO;
using Codeji.CMS.Services;
using Codeji.CMS.Repository.Entities.RolePermissions;

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
            var roles = await _roleService.GetRoles(CurrentContext.CurrentUserCompanyId(_httpContextAccessor));
            return new Result<RoleModel>()
            {
                MethodResults = roles ?? new List<RoleModel>(),
                Success = true
            };
        }
    }
}
