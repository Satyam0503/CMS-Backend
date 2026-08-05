using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.CustomAttribute;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Attendance;
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
    readonly IOfficeScheduleSettingsService _officeScheduleSettings;
    public CompanyMasterController(ICompanyMasterService companyService, IHttpContextAccessor httpContextAccessor, IOfficeScheduleSettingsService officeScheduleSettings)
    {
        _companyMasterService = companyService;
        _httpContextAccessor = httpContextAccessor;
        _officeScheduleSettings = officeScheduleSettings;
    }

    [HttpGet]
    [Route("OfficeSchedule")]
    [Authorize(Policy = "AdminOnly")]
    public Task<Result<OfficeScheduleSettingsDto>> GetOfficeSchedule(CancellationToken cancellationToken) =>
        _officeScheduleSettings.GetAsync(CurrentContext.CompanyId(_httpContextAccessor), cancellationToken);

    [HttpPut]
    [Route("OfficeSchedule")]
    [Authorize(Policy = "AdminOnly")]
    public Task<Result<OfficeScheduleSettingsDto>> SaveOfficeSchedule(OfficeScheduleSettingsDto request, CancellationToken cancellationToken) =>
        _officeScheduleSettings.SaveAsync(CurrentContext.CompanyId(_httpContextAccessor), CurrentContext.UserId(_httpContextAccessor), request, cancellationToken);

    // company department actions

    [HttpPost]
    [Route("UpdateDepartment")]
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<Result> DeleteJobTitle(string jobTitleId)
    {
        Result result = new();
        result.Success = await _companyMasterService.DeleteJobTitle(jobTitleId);
        return result;
    }

    // custom attributes actions
    [HttpPost]
    [Route("CreateCustomAttribute")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<Result<CustomAttributeResponseDto>> CreateCustomAttribute()
    {
        Result<CustomAttributeResponseDto> result = new()
        {
            Success = false
        };
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var data = await _companyMasterService.CreateCustomAttribute(companyId);
        if (data != null)
        {
            result.Success = true;
            result.MethodResult = data;
        }
        return result;
    }

    [HttpGet]
    [Route("GetAllCustomAttribute")]
    public async Task<Result<CustomAttributeResponseDto>> GetAllCustomAttributes()
    {
        Result<CustomAttributeResponseDto> result = new();
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var dataList = await _companyMasterService.GetAllCustomAttribute(companyId);
        result.TotalRecords = dataList.Count;
        result.MethodResults = dataList;
        return result;
    }

    [HttpGet]
    [Route("GetCustomAttributeById/{customAttributeId}")]
    public async Task<Result<CustomAttributeByIdResponseDto>> GetCustomAttributeById(string customAttributeId, [FromQuery] bool? active)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var result = await _companyMasterService.GetCustomAttributeById(customAttributeId, companyId, active);
        return result;
    }

    [HttpPut]
    [Route("UpdateCustomAttribute")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<Result> UpdateCustomAttribute([FromBody] CustomAttributeRequestDto data)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        string userId = CurrentContext.CompanyId(_httpContextAccessor);
        return await _companyMasterService.UpdateCustomAttribute(data, companyId, userId);
    }

    [HttpDelete]
    [Route("DeleteCustomAttributeValue/{customAttributeValueId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<Result> DeleteCustomAttributeValue(string customAttributeValueId)
    {
        Result result = new();
        if (string.IsNullOrEmpty(customAttributeValueId)) return result;
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        result = await _companyMasterService.DeleteCustomAttributeValue(customAttributeValueId, companyId);
        return result;
    }
}
