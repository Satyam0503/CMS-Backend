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

        Task<UserModel> GetEmployeeById(string userId);
        Task<List<UserModel>> GetAllEmployees(string companyId);
        Task<bool> IsEmailExist(string email);
        Task<bool> ResetPassword(string userId, string companyId, string password, string oldPassword = "");
        Task<string> GetVerificationToken(string email, string password);
        Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId);
        Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId);
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId);
        Task<List<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string id);
        Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId, string companyId);
        Task<List<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails(string id);
        Task<Result> AddComment(string companyId, CommentRequestModel model);
        Task<List<Comments>> GetAllComment(string applicantId);
        Task<string> GetUserExistingProfile(string userId);
        Task<Result> AddUserProfileImage(string fileName, string userId, string filePath);
        Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string companyId, string userId);
        Task<EmployeeSkills> GetEmployeeSkills(string companyId, string userId);
        Task<bool> CreateNewPassword(string userId, string companyId, string password);
    }
}
