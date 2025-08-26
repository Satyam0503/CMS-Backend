using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
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

    // company department actions

    [HttpPost]
    [Route("UpdateDepartment")]
    public async Task<Result> AddEditDepartment(List<DepartmentRequestDto> data)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        Result result = await _companyMasterService.UpdateDepartments(data, userId);
        return result;
    }

    [HttpGet]
    [Route("GetDepartmentList")]
    public async Task<Result<DepartmentResponseDto>> GetDepartmentList([FromQuery] bool? isActive)
    {
        var data = await _companyMasterService.GetDepartmentList(isActive);
        return data;
    }

    [HttpDelete]
    [Route("DeleteDepartment/{departmentId}")]
    public async Task<Result> DeleteDepartment(string departmentId)
    {
        Result result = new();
        var success = await _companyMasterService.DeleteDepartment(departmentId);
        if (!success)
        {
            return result;
        }
        result.Success = true;
        return result;
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

    // company job title actions
    [HttpPost]
    [Route("AddUpdateJobTitle")]
    public async Task<Result> AddUpdateJobTitle(List<JobTitleRequestDto> data)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        var result = await _companyMasterService.AddUpdateJobTitle(data, userId);
        return result;
    }

    [HttpGet]
    [Route("GetJobTitles")]
    public async Task<Result<JobTitleResponseDto>> GetAllJobTitles([FromQuery] bool? isActive)
    {
        var result = await _companyMasterService.GetJobTitles(isActive);
        return result;
    }

    [HttpDelete]
    [Route("DeleteJobTitle/{jobTitleId}")]
    public async Task<Result> DeleteJobTitle(string jobTitleId)
    {
        Result result = new();
        result.Success = await _companyMasterService.DeleteJobTitle(jobTitleId);
        return result;
    }
}
