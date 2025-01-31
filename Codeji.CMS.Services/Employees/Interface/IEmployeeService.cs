using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels.EmployeeData;

namespace Codeji.CMS.Services.Employees.Interface
{
    public interface IEmployeeService
    {
        Task<Result<UserModel>> AddEmployee(UserModel user, string companyId);
        Task<Result<UserModel>> EditEmployee(UserModel user, string userId, string companyId, string userPassword);

        Task<UserModel> GetEmployeeById(string userId);
        Task<List<UserModel>> GetAllEmployees(string companyId);
        Task<bool> IsEmailExist(string email);
        Task<bool> ResetPassword(string userId, string password, string oldPassword = "");
        Task<string> GetVerificationToken(string email, string password);
        Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId);
        Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId);
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId);
        Task<List<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string id);
        Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId);
        Task<List<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails(string id);
    }
}
