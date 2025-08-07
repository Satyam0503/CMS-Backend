
using System.Text.RegularExpressions;
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

    [Route("AddEmployees")]
    [HttpPost]
    public async Task<Result<UserModel>> AddEmployees(UserModel user)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        bool isEmailExist = await _employeeService.IsEmailExist(user.Email);

        if (isEmailExist)
        {
            return new Result<UserModel>
            {
                StatusCode = CustomStatusCode.EmployeeAlreadyExist,
                Success = false
            };
        }
        return await _employeeService.AddEmployee(user, currentUserId);
    }

    [Route("EditEmployees")]
    [HttpPost]
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

    [Route("GetAllEmployees")]
    [HttpPost]
    public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters)
    {
        Result<GetAllEmployeeResponseModel> data = await _employeeService.GetAllEmployees(filters);
        return data;
    }

    [Route("ChangePassword")]
    [HttpPost]
    public async Task<Result> ChangePassword(ChangePasswordRequest passwordModel)
    {
        Result result = new Result();
        string userId = CurrentContext.UserId(_httpContextAccessor);
        result.Success = await _accountServices.ResetPassword(userId, passwordModel.Password, passwordModel.OldPassword);
        return result;
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
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\ProfileImage\\");
        string fileExtension = Path.GetExtension(profilePicture.FileName);
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }
        string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
        string filePath = Path.Combine(uploadFolder + fileName);
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
    public async Task<Result> DeleteEmployee([FromQuery] string employeeId)
    {
        Result data = await _employeeService.DeleteEmployee(employeeId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                Message = "Employee Not Deleted ",
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            Message = "Employee Deleted Successfully",
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

    [HttpGet]
    [Route("GetCollegeList")]
    public async Task<Result<string>> GetCollegeList()
    {
        return await _employeeService.GetCollegeList();
    }
}
