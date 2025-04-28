using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
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
    [Route("AddEditDepartment")]
    public async Task<Result> AddEditDepartment(DepartmentDTO model)
    {
        Result data = await _companyMasterService.AddEditDepartment(model);
        Result result = new()
        {
            Success = data.Success,
            StatusCode = data.StatusCode,
            Message = data.Message
        };
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
    public async Task<Result> UpdateModuleAccess(string moduleId, [FromBody] bool hasAccess)
    {
        if (string.IsNullOrEmpty(moduleId))
        {
            return new Result()
            {
                Success = false,
            };
        }
        Result result = await _companyMasterService.UpdateModuleAccess(moduleId, hasAccess);
        return result;
    }

    [HttpGet]
    [Route("GetAllModules")]
    public async Task<Result<ModuleDTO>> GetAllModules()
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var data = await _companyMasterService.GetAllModules(companyId);
        return new Result<ModuleDTO>()
        {
            MethodResults = data,
            Success = true,
            StatusCode = 200,
        };
    }
}
