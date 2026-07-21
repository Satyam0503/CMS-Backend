
using System.Text.RegularExpressions;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Notification;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseApiController
{
    readonly IAccountServices _accountServices;
    private readonly IEmployeeService _employeeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserController(IHttpContextAccessor httpContextAccessor, IEmployeeService employeeService, IAccountServices accountServices)
    {
        _httpContextAccessor = httpContextAccessor;
        _employeeService = employeeService;
        _accountServices = accountServices;
    }

    [Route("InviteNewEmployee")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result<InviteEmployeeDto>> InviteNewEmployee([FromBody] InviteEmployeeDto model)
    {
        Result<InviteEmployeeDto> result = new() { Success = false };
        if (!ModelState.IsValid)
        {
            return result;
        }
        var currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.InviteNewEmployee(model, currentUserId);
    }

    [Route("BulkImportEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result<BulkImportEmployeesResponseDto>> BulkImportEmployees([FromBody] BulkImportEmployeesRequestDto model)
    {
        Result<BulkImportEmployeesResponseDto> result = new() { Success = false };
        if (model is null || !ModelState.IsValid || model.Employees.Count == 0 || model.Employees.Count > BulkImportEmployeesRequestDto.MaxBatchSize)
        {
            result.Message = $"Invalid request. Employees list must contain 1 to {BulkImportEmployeesRequestDto.MaxBatchSize} records.";
            return result;
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.BulkImportEmployees(model, currentUserId);
    }

    [Route("GetLastEmployeeId")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public async Task<Result> GetLastEmployeeId()
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        return await _employeeService.GetLastEmployeeId(companyId);
    }

    [Route("EditEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public async Task<Result<UserModel>> EditEmployees(EmployeePersonalInfo user, string userId)
    {
        UserModel isUserExist = await _employeeService.GetEmployeeById(userId);
        if (userId != isUserExist.UserId)
        {
            return new Result<UserModel>
            {
                Success = false,
                StatusCode = CustomStatusCode.EmployeeNotExist
            };
        }
        return await _employeeService.EditEmployee(user, userId);
    }

    // No [ModulePermission] - any authenticated user may edit their own profile regardless of
    // their Employees module permission (that permission governs editing *other* employees).
    // Always targets the caller's own userId from the auth token, never a client-supplied one,
    // and EmployeeSelfEditDto deliberately omits privileged fields (role, job, bank details, etc).
    [Route("EditOwnProfile")]
    [HttpPost]
    public async Task<Result<UserModel>> EditOwnProfile(EmployeeSelfEditDto user)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        EmployeePersonalInfo mapped = new()
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = "",
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber,
            BloodGroup = user.BloodGroup,
            PersonalEmail = user.PersonalEmail,
            EmergencyContact = user.EmergencyContact,
            Address = user.Address,
        };
        return await _employeeService.EditEmployee(mapped, currentUserId);
    }

    [Route("GetAllEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters)
    {
        Result<GetAllEmployeeResponseModel> data = await _employeeService.GetAllEmployees(filters);
        return data;
    }

    [Route("ChangePassword")]
    [HttpPost]
    public async Task<Result> ChangePassword(ChangePasswordRequest passwordModel)
    {
        Result result = new();
        if (!ModelState.IsValid) return result;
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _accountServices.ResetPassword(userId, passwordModel);
    }


    [Route("GetEmployeeById")]
    [HttpGet]
    public async Task<Result<UserModel>> GetEmployeeById([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        UserModel result = await _employeeService.GetEmployeeById(userId);
        return new Result<UserModel>()
        {
            Success = true,
            MethodResult = result
        };
    }

    [Route("Search")]
    [HttpGet]
    public async Task<Result<EmployeeSearchResponseDTO>> SearchEmployeeByName([FromQuery] string name)
    {
        Result<EmployeeSearchResponseDTO> result = new();
        if (string.IsNullOrEmpty(name))
        {
            result.Success = false;
            return result;
        }
        var empList = await _employeeService.SearchEmployeeByName(name);
        result.MethodResults = empList;
        result.Success = true;
        return result;
    }

    //Employee Details APIs

    [Route("AddEditEmployeeSummary")]
    [HttpPost]
    public async Task<Result> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary)
    {
        string userId = string.IsNullOrEmpty(userSummary.UserId) ? CurrentContext.UserId(_httpContextAccessor) : userSummary.UserId;
        return await _employeeService.AddEditEmployeeSummary(userSummary, userId);
    }

    [Route("AddEmployeeEducation")]
    [HttpPost]
    public async Task<Result> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string userId = string.IsNullOrEmpty(educationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : educationDetails.UserId;
        return await _employeeService.AddEmployeeEducation(educationDetails, userId);
    }

    [Route("EditEmployeeEducation")]
    [HttpPost]
    public async Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string userId = string.IsNullOrEmpty(educationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : educationDetails.UserId;
        return await _employeeService.EditEmployeeEducation(educationDetails, userId);
    }

    [Route("AddEmployeeCertification")]
    [HttpPost]
    public async Task<Result> AddEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string userId = string.IsNullOrEmpty(certificationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : certificationDetails.UserId;
        return await _employeeService.AddEmployeeCertification(certificationDetails, userId);
    }

    [Route("EditEmployeeCertification")]
    [HttpPost]
    public async Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string userId = string.IsNullOrEmpty(certificationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : certificationDetails.UserId;
        return await _employeeService.EditEmployeeCertification(certificationDetails, userId);
    }

    [Route("GetEmployeeSummary")]
    [HttpGet]
    public async Task<Result<EmployeeSummaryRequestModel>> GetEmployeeSummary([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        EmployeeSummaryRequestModel summary = await _employeeService.GetEmployeeSummary(userId);
        return new Result<EmployeeSummaryRequestModel>()
        {
            Success = true,
            MethodResult = summary
        };
    }

    [Route("GetEmployeeEducationDetails")]
    [HttpGet]
    public async Task<Result<EmpEducationDetails>> GetEmployeeEducationDetails([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        List<EmpEducationDetails> data = await _employeeService.GetEmployeeEducationDetails(userId);
        Result<EmpEducationDetails> result = new Result<EmpEducationDetails>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }


    [Route("GetEmployeeCertificationDetails")]
    [HttpGet]
    public async Task<Result<EmpCertificationDetails>> GetEmployeeCertificationDetails([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        List<EmpCertificationDetails> data = await _employeeService.GetEmployeeCertificationDetails(userId);
        Result<EmpCertificationDetails> result = new Result<EmpCertificationDetails>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }


    [Route("UploadUserImage")]
    [HttpPost]
    //[FilesExtensions([".jpg", ".jpeg", ".png"])]
    public async Task<Result<string>> UploadUserImage(IFormFile profilePicture)
    {

        string userId = CurrentContext.UserId(_httpContextAccessor);
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "ProfileImage");
        string fileExtension = Path.GetExtension(profilePicture.FileName);
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }
        string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
        string filePath = Path.Combine(uploadFolder, fileName);
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(fileStream);
        }
        ;
        string profile = await _employeeService.GetUserExistingProfile(userId);
        if (string.IsNullOrEmpty(profile))
        {
            string data = await _employeeService.AddUserProfileImage(fileName, userId, filePath);
            Result<string> result = new()
            {
                MethodResult = data,
                Success = true,
                StatusCode = 201

            };
            return result;
        }
        else
        {
            string oldPath = Path.Combine(uploadFolder, profile);
            FileInfo fileInfo = new(oldPath);
            fileInfo.Delete();
            string data = await _employeeService.AddUserProfileImage(fileName, userId, filePath);

            Result<string> result = new()
            {
                MethodResult = data,
                Success = true,
                StatusCode = 200
            };
            return result;
        }
    }

    [Route("AddEditEmpSkills")]
    [HttpPost]
    public async Task<Result> AddEditSkills(SkillsRequestModel skillsModel)
    {
        string userId = string.IsNullOrEmpty(skillsModel.UserId) ? CurrentContext.UserId(_httpContextAccessor) : skillsModel.UserId;
        return await _employeeService.AddEditEmployeeSkills(skillsModel, userId);
    }

    [Route("GetEmployeeSkills")]
    [HttpGet]
    public async Task<Result<EmployeeSkillsDTO>> GetEmployeeSkills([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        var result = await _employeeService.GetEmployeeSkills(userId);
        return new Result<EmployeeSkillsDTO>()
        {
            Success = true,
            MethodResult = result
        };

    }

    [Route("DeleteEducationDetails/{educationId}")]
    [HttpDelete]
    public async Task<Result> DeleteEducationDetails(string educationId, [FromBody] string userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteEducationDetails(educationId, userId);
    }

    [Route("DeleteCertificationDetails/{certificationId}")]
    [HttpDelete]
    public async Task<Result> DeleteCertificationDetails(string certificationId, [FromBody] string userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteCertificationDetails(certificationId, userId);
    }

    [Route("DeleteEmployee")]
    [HttpDelete]
    [ModulePermission(AppModule.Employees, Permission.Delete)]
    public async Task<Result> DeleteEmployee([FromQuery] string employeeId)
    {
        Result data = await _employeeService.DeleteEmployee(employeeId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            StatusCode = 200
        };
    }

    [HttpGet]
    [Route("GetSuggestedSkills")]
    public async Task<Result<Skills>> GetSuggestedSkills([FromQuery] string query)
    {
        var sanitizedQuery = query.Trim().ToLower();
        if (string.IsNullOrEmpty(sanitizedQuery))
        {
            return new Result<Skills>()
            {
                Success = true,
                MethodResults = [],
                StatusCode = 200,
            };
        }
        return await _employeeService.GetSuggestedSkills(sanitizedQuery);
    }

    [HttpPost]
    [Route("AddSkill")]
    public async Task<Result> AddSkill([FromBody] string skill)
    {
        var sanitizeSkill = skill.Trim().ToLower();
        var regex = new Regex(@"^[a-zA-Z0-9\s\+\#\.\-]{2,30}$");
        var isValid = regex.IsMatch(sanitizeSkill);

        if (string.IsNullOrEmpty(sanitizeSkill) || !isValid)
        {
            return new Result()
            {
                StatusCode = 200,
                Success = false,
                Message = "InValid Skill Name"
            };
        }
        var result = await _employeeService.AddSkill(sanitizeSkill);
        return result;
    }

    [HttpGet]
    [Route("GetAllNotifications")]
    public async Task<Result<NotificationResponseModel>> GetAllNotifications([FromQuery] NotificationRequestDTO model)
    {
        string[] supportedType = ["all", "unread"];
        if (!supportedType.Contains(model.Type))
        {
            return new Result<NotificationResponseModel>();
        }
        string userId = CurrentContext.UserId(_httpContextAccessor);
        var data = await _employeeService.GetAllNotifications(model, userId);
        return new Result<NotificationResponseModel>()
        {
            Success = true,
            MethodResult = data,
        };
    }

    [HttpPatch]
    [Route("Notification/{userNotificationId}/MarkAsRead")]
    public async Task<Result> MarkNotificationAsRead(string userNotificationId)
    {
        if (string.IsNullOrEmpty(userNotificationId))
        {
            return new Result();
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.MarkNotificationAsRead(currentUserId, userNotificationId);
    }

    [HttpPost]
    [Route("AddUpdateWorkHistory")]
    public async Task<Result> AddWorkHistory(EmployeeWorkHistoryModel model)
    {
        if (!ModelState.IsValid)
        {
            return new Result();
        }
        if (string.IsNullOrEmpty(model.UserId)) model.UserId = CurrentContext.UserId(_httpContextAccessor);
        Result result = await _employeeService.AddUpdateWorkHistory(model);
        return result;
    }

    [HttpGet]
    [Route("GetEmpWorkHistory")]
    public async Task<Result<EmployeeWorkHistoryModel>> GetEmpWorkHistory([FromQuery] string? userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        var data = await _employeeService.GetEmpWorkHistory(userId);
        return new Result<EmployeeWorkHistoryModel>()
        {
            MethodResults = data,
            TotalRecords = data.Count
        };
    }

    [HttpDelete]
    [Route("DeleteWorkHistory/{workId}")]
    public async Task<Result> DeleteWorkHistory(string workId, [FromBody] string? userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteWorkHistory(workId, userId);
    }

    [HttpPost]
    [Route("MarkAllNotificationAsRead")]
    public async Task<Result> MarkAllNotificationAsRead()
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.MarkAllNotificationAsRead(userId);
    }

    [HttpDelete]
    [Route("RemoveProfileImage")]
    public async Task<Result> RemoveProfileImage()
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.RemoveProfileImage(userId);
    }

    [HttpGet]
    [Route("GetCollegeList")]
    public async Task<Result<string>> GetCollegeList([FromQuery] string searchValue)
    {
        List<string> collegeList = [];
        collegeList = await _employeeService.GetCollegeNameSuggestions(searchValue);
        return new Result<string>()
        {
            Success = true,
            MethodResults = collegeList,
            TotalRecords = collegeList.Count,
        };
    }

    [HttpGet]
    [Route("GetNotificationPreferences")]
    public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> GetNotificationPreferences()
    {
        Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> result = new();
        string userId = CurrentContext.UserId(_httpContextAccessor);
        result.MethodResult = await _employeeService.GetNotificationPreferences(userId);
        return result;
    }

    [HttpPost]
    [Route("UpdateNotificationPreferences")]
    public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> UpdateNotificationPreferences([FromBody] Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.UpdateNotificationPreferences(userId, preferences);
    }

    [HttpPost]
    [Route("ResendInvite/{userId}")]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result> ResendInviteLink(string userId)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.ResendInviteLink(userId, currentUserId);
    }
}

