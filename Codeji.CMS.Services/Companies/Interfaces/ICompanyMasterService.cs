using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.CustomAttribute;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;

namespace Codeji.CMS.Services.Interface;

public interface ICompanyMasterService
{
    // Company Department service
    Task<Result> UpdateDepartments(List<DepartmentRequestDto> model, string userId);
    Task<Result<DepartmentResponseDto>> GetDepartmentList(bool? isActive);
    Task<bool> DeleteDepartment(string departmentId);
    Task<Result<string[]>> UpdateModuleAccess(string moduleId);
    Task<List<AllModuleDetailsResponseModel>> GetAllModulesDetails(string companyId);

    // Company Job Titles 
    Task<Result> AddUpdateJobTitle(List<JobTitleRequestDto> data, string userId);
    Task<Result<JobTitleResponseDto>> GetJobTitles(bool? isActive, string? departmentId);
    Task<bool> DeleteJobTitle(string jobTitleId);

    // custom attributes
    Task<CustomAttributeResponseDto?> CreateCustomAttribute(string companyId);
    Task<List<CustomAttributeResponseDto>> GetAllCustomAttribute(string companyId);
    Task<Result<CustomAttributeByIdResponseDto>> GetCustomAttributeById(string customAttributeId, string companyId, bool? active);
    Task<Result> UpdateCustomAttribute(CustomAttributeRequestDto data, string companyId, string userId);
    Task<Result> DeleteCustomAttribute(string customAttributeId, string companyId, string userId);
    Task<Result> DeleteCustomAttributeValue(string customAttributeValueId, string companyId);
}
