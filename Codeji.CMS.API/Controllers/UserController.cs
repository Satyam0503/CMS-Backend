using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : BaseApiController
{
    private readonly IEmployeeService _employeeService;

    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserController(IHttpContextAccessor httpContextAccessor, IEmployeeService employeeService)
    {
        _httpContextAccessor = httpContextAccessor;
        _employeeService = employeeService;
    }
    [Route("AddEmployees")]
    [HttpPost]
    [Authorize]
    public async Task<Result<UserModel>> AddEmployees(UserModel user)
    {
        bool isEmailExist = await _employeeService.IsEmailExist(user.Email);
        if (isEmailExist)
        {
            return new Result<UserModel>
            {
                Success = false,
                Message = "User Already Exist"
            };
        }
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);

        return await _employeeService.AddEmployee(user, companyId);
    }

    [Route("EditEmployees")]
    [HttpPost]
    [Authorize]
    public async Task<Result<UserModel>> EditEmployees(UserModel user, string userId)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        UserModel isUserExist = await _employeeService.GetEmployeeById(userId);
        if (userId != isUserExist.UserId)
        {
            return new Result<UserModel>
            {
                Message = "User Not Exist"
            };
        }
        return await _employeeService.EditEmployee(user, userId, companyId);
    }

    [Route("GetAllEmployees")]
    [HttpGet]
    [Authorize]
    public async Task<Result<UserModel>> GetAllEmployees()
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        List<UserModel> data = await _employeeService.GetAllEmployees(companyId);
        Result<UserModel> result = new Result<UserModel>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }
    [Route("ChangePassword")]
    [HttpPost]
    [Authorize]
    public async Task<Result> ChangePassword(ChangePasswordRequest passwordModel)
    {
        Result result = new Result();
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        result.Success = await _employeeService.ResetPassword(userId, companyId, passwordModel.Password, passwordModel.OldPassword);
        return result;
    }

    [Route("CreateNewPassword")]
    [HttpPost]
    public async Task<Result> CreateNewPassword(string password, string userId, string companyId)
    {
        Result result = new Result();
        result.Success = await _employeeService.CreateNewPassword(userId, companyId, password);
        result.Message = "User Verified and Password Created Successfully";
        return result;

    }

    [Route("GetEmployeeById")]
    [HttpGet]
    [Authorize]
    public async Task<Result<UserModel>> GetEmployeeById()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        UserModel result = await _employeeService.GetEmployeeById(userId);
        return new Result<UserModel>()
        {
            Success = true,
            MethodResult = result
        };
    }

    //Employee Details APIs

    [Route("AddEditEmployeeSummary")]
    [HttpPost]
    [Authorize]
    public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        return await _employeeService.AddEditEmployeeSummary(userSummary, userId, companyId);
    }

    [Route("AddEmployeeEducation")]
    [HttpPost]
    [Authorize]
    public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        return await _employeeService.AddEmployeeEducation(educationDetails, userId, companyId);

    }

    [Route("AddEmployeeCertification")]
    [HttpPost]
    [Authorize]
    public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        return await _employeeService.AddEmployeeCertification(certificationDetails, userId, companyId);

    }

    [Route("GetEmployeeSummary")]
    [HttpGet]
    [Authorize]
    public async Task<Result<EmployeeSummaryRequestModel>> GetEmployeeSummary()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        EmployeeSummaryRequestModel summary = await _employeeService.GetEmployeeSummary(userId, companyId);
        return new Result<EmployeeSummaryRequestModel>()
        {
            Success = true,
            MethodResult = summary
        };
    }

    [Route("GetEmployeeEducationDetails")]
    [HttpGet]
    [Authorize]
    public async Task<Result<EmployeeEducationRequestModel>> GetEmployeeEducationDetails()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        List<EmployeeEducationRequestModel> data = await _employeeService.GetEmployeeEducationDetails(userId);
        Result<EmployeeEducationRequestModel> result = new Result<EmployeeEducationRequestModel>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }


    [Route("GetEmployeeCertificationDetails")]
    [HttpGet]
    [Authorize]
    public async Task<Result<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        List<EmployeeCertificationRequestModel> data = await _employeeService.GetEmployeeCertificationDetails(userId);
        Result<EmployeeCertificationRequestModel> result = new Result<EmployeeCertificationRequestModel>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }

    [Route("AddComment")]
    [HttpPost]
    [Authorize]
    public async Task<Result> AddComment(CommentRequestModel model)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        await _employeeService.AddComment(companyId, model);
        Result result = new Result()
        {
            Success = true,
            Message = "Comment Added Successfully",
            StatusCode = StatusCodes.Status200OK,
        };
        return result;
    }

    [Route("GetAllComment")]
    [HttpGet]
    [Authorize]
    public async Task<Result<Comments>> GetAllComment([FromQuery] string applicantId)
    {
        List<Comments> list = await _employeeService.GetAllComment(applicantId);
        Result<Comments> result = new()
        {
            Success = true,
            Message = "All Comments",
            StatusCode = StatusCodes.Status200OK,
            MethodResults = [.. list]
        };
        return result;
    }

    [Route("UploadUserImage")]
    [HttpPost]
    [FilesExtensions([".jpg", ".jpeg", ".png"])]
    [Authorize]
    public async Task<Result> UploadUserImage(IFormFile profilePicture)
    {
        Result result = new Result();
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
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
        };
        string profile = await _employeeService.GetUserExistingProfile(userId);
        if (string.IsNullOrEmpty(profile))
        {
            result = await _employeeService.AddUserProfileImage(fileName, userId, filePath);
        }
        else
        {
            string oldPath = Path.Combine(uploadFolder, profile);
            FileInfo fileInfo = new(oldPath);
            fileInfo.Delete();
            result = await _employeeService.AddUserProfileImage(fileName, userId, filePath);
        }
        result.Success = true;
        return result;
    }

    [Route("AddSkills")]
    [HttpPost]
    [Authorize]
    public async Task<Result> AddEditSkills(SkillsRequestModel skillsModel)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        return await _employeeService.AddEditEmployeeSkills(skillsModel, companyId, userId);

    }

    [Route("GetEmployeeSkills")]
    [HttpGet]
    [Authorize]
    public async Task<Result<EmployeeSkills>> GetEmployeeSkills()
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        EmployeeSkills result = await _employeeService.GetEmployeeSkills(companyId, userId);
        return new Result<EmployeeSkills>()
        {
            Success = true,
            MethodResult = result
        };

    }
}



