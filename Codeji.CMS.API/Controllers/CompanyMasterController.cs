using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyMasterController : BaseApiController
{
    readonly ICompanyMasterService _companyMasterService;
    readonly IHttpContextAccessor _httpContextAccessor;
    public CompanyMasterController(ICompanyMasterService companyService, IHttpContextAccessor httpContextAccessor)
    {
        _companyMasterService = companyService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [Route("UpdateDepartment")]
    public async Task<Result> AddEditDepartment(List<DepartmentDTO> model)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        Result result = await _companyMasterService.UpdateDepartments(model, userId);
        return result;
    }

    [HttpGet]
    [Route("GetDepartmentList")]
    public async Task<Result<DepartmentDTO>> GetDepartmentList()
    {
        Result<DepartmentDTO> data = await _companyMasterService.GetDepartmentList();
        return data;
    }

    [HttpDelete]
    [Route("DeleteDepartment/{departmentId}")]
    public async Task<Result> DeleteDepartment(string departmentId)
    {
        var success = await _companyMasterService.DeleteDepartment(departmentId);
        if (!success)
        {
            return new Result()
            {
                Message = "Department Not Found",
                Success = false,
                StatusCode = 200,
            };
        }
        return new Result()
        {
            Message = "Department Deleted Successfully",
            StatusCode = 200,
            Success = true,
        };
    }

    [HttpPatch]
    [Route("UpdateModuleAccess/{moduleId}")]
    public async Task<Result<string[]>> UpdateModuleAccess(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
        {
            return new Result<string[]>()
            {
                Success = false,
            };
        }
        var result = await _companyMasterService.UpdateModuleAccess(moduleId);
        return result;
    }

    [HttpGet]
    [Route("GetAllModuleDetails")]
    [Authorize]
    public async Task<Result<AllModuleDetailsResponseModel>> GetAllModuleDetails()
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var data = await _companyMasterService.GetAllModulesDetails(companyId);
        return new Result<AllModuleDetailsResponseModel>()
        {
            MethodResults = data,
            Success = true
        };
    }

}
