using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.EmployeeData;

namespace Codeji.CMS.Services.Interface
{
    public interface IEmployeeService
    {
        Task<Result<EmployeeSummaryRequestModel>> AddEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId);
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId);
        Task<List<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string id);
        Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId);
        Task<List<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails(string id);
    }
}
