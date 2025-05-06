using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Employees.Interface
{
    public interface IEmployeeService
    {
        Task<Result<UserModel>> AddEmployee(UserModel user, string currentUserId);
        Task<Result<UserModel>> EditEmployee(UserModel user, string userId);
        Task<bool> IsUserActive(string userId);
        Task<UserModel> GetEmployeeById(string userId);
        Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(int pageNo, int records);
        Task<bool> IsEmailExist(string email);
        Task<bool> ResetPassword(string userId, string password, string oldPassword = "");
        Task<string> GetVerificationToken(string email, string password);
        Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId, string companyId);
        Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId);
        Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId);
        Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId);
        Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId);
        Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails, string userId);
        Task<List<EmpEducationDetails>> GetEmployeeEducationDetails(string id);
        Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId);
        Task<List<EmpCertificationDetails>> GetEmployeeCertificationDetails(string id);
        Task<string> GetUserExistingProfile(string userId);
        Task<string> AddUserProfileImage(string fileName, string userId, string filePath);
        Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string userId);
        Task<EmployeeSkillsDTO> GetEmployeeSkills(string userId);
        Task<bool> CreateNewPassword(string password, string statusNumber);
        Task<Result> DeleteEducationDetails(string educationId, string userId);
        Task<Result> DeleteCertificationDetails(string certificationId, string userId);
        Task<Result> DeleteEmployee(string employeeId);
        Task<Result<Skills>> GetSuggestedSkills(string query);
        Task<Result> AddSkill(string skill);
    }
}
