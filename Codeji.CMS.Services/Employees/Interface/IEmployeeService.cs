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
                Task<Result<UserModel>> EditEmployee(EmployeePersonalInfo user, string userId);
                Task<bool> IsUserActive(string userId);
                Task<UserModel> GetEmployeeById(string userId);
                Task<string> GetEmployeeNameById(string employeeId);
                Task<List<EmployeeSearchResponseDTO>> SearchEmployeeByName(string name);
                Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters);
                Task<bool> IsEmailExist(string email);
                Task<bool> IsEmpExistAndActive(string email);
                Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId, string companyId);
                Task<Result> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId);
                Task<Result> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId);
                Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId);
                Task<Result> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId);
                Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails, string userId);
                Task<List<EmpEducationDetails>> GetEmployeeEducationDetails(string id);
                Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId);
                Task<List<EmpCertificationDetails>> GetEmployeeCertificationDetails(string id);
                Task<string> GetUserExistingProfile(string userId);
                Task<string> AddUserProfileImage(string fileName, string userId, string filePath);
                Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string userId);
                Task<EmployeeSkillsDTO> GetEmployeeSkills(string userId);
                Task<Result> DeleteEducationDetails(string educationId, string userId);
                Task<Result> DeleteCertificationDetails(string certificationId, string userId);
                Task<Result> DeleteEmployee(string employeeId);
                Task<Result<Skills>> GetSuggestedSkills(string query);
                Task<Result> AddSkill(string skill);
                Task<Result> AddUpdateWorkHistory(EmployeeWorkHistoryModel model);
                Task<List<EmployeeWorkHistoryModel>> GetEmpWorkHistory(string userId);
                Task<Result> DeleteWorkHistory(string workId, string userId);
                Task<NotificationResponseModel> GetAllNotifications(NotificationRequestDTO model, string userId);
                Task<Result> MarkNotificationAsRead(string userId, string userNotificationId);
                Task<Result> MarkAllNotificationAsRead(string userId);
                Task<Result> RemoveProfileImage(string userId);
                Task<List<string>> GetCollegeNameSuggestions(string searchValue);
                Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId);

                Task<Result> UploadPayrollData(EmplyeePayRollRequestDto model, string companyId);
        }
}
