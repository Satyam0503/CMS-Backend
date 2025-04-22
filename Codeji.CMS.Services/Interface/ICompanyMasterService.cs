using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;

namespace Codeji.CMS.Services.Interface;

public interface ICompanyMasterService
{
    Task<Result> AddEditDepartment(DepartmentDTO model);
    Task<Result<DepartmentDTO>> GetDepartmentList();
    Task<bool> DeleteDepartment(string departmentId);
}
