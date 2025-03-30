using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Employees.Interface
{
    public interface IEmployeeService
    {
        Task<Result<UserModel>> AddEmployee(UserModel user, string companyId);
        Task<Result<UserModel>> EditEmployee(UserModel user, string userId, string companyId);
        Task<bool> IsUserActive(string userId);
        Task<UserModel> GetEmployeeById(string userId);
        Task<Result<UserModel>> GetAllEmployees(string companyId, int pageNo, int records);
        Task<bool> IsEmailExist(string email);
        Task<bool> ResetPassword(string userId, string companyId, string password, string oldPassword = "");
        Task<string> GetVerificationToken(string email, string password);
        Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId);
        Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId);
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId);
        Task<Result> EditEmployeeEducation(EmpEducationDetails educationDetails, string companyId, string userId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId);
        Task<Result> EditEmployeeCertification(EmpCertificationDetails certificationDetails, string companyId, string userId);
        Task<List<EmpEducationDetails>> GetEmployeeEducationDetails(string id);
        Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId, string companyId);
        Task<List<EmpCertificationDetails>> GetEmployeeCertificationDetails(string id);
        Task<Result> AddComment(string companyId, string userId, CommentRequestModel model);
        Task<List<Comments>> GetAllComment(string applicantId);
        Task<string> GetUserExistingProfile(string userId);
        Task<string> AddUserProfileImage(string fileName, string userId, string filePath);
        Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string companyId, string userId);
        Task<EmpSkills> GetEmployeeSkills(string companyId, string userId);
        Task<bool> CreateNewPassword(string password, string statusNumber);
        Task<Result> DeleteEducationDetails(string educationId, string companyId, string userId);
        Task<Result> DeleteCertificationDetails(string certificationId, string companyId, string userId);
        Task<Result> DeleteEmployee(string employeeId, string companyId);
        Task<List<ProcessLogs>> GetProcessLogData(string companyId);
    }
}
