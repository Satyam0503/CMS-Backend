using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;

namespace Codeji.CMS.Services.Interface;

public interface ICompanyMasterService
{
    Task<Result> UpdateDepartments(List<DepartmentDTO> model, string userId);
    Task<Result<DepartmentResponseDto>> GetDepartmentList(bool? isActive);
    Task<bool> DeleteDepartment(string departmentId);
    Task<Result<string[]>> UpdateModuleAccess(string moduleId);
    Task<List<AllModuleDetailsResponseModel>> GetAllModulesDetails(string companyId);
}
