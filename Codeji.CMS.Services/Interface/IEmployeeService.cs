using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.EmployeeData;

namespace Codeji.CMS.Services.Interface
{
    public interface IEmployeeService
    {
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId);
        //Task<Result<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string id);
    }
}
