
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    public async Task<Result<UserModel>> AddEmployees(UserModel user)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        bool isEmailExist = await _employeeService.IsEmailExist(user.Email);

        if (isEmailExist)
        {
            return new Result<UserModel>
            {
                Success = false,
                Message = "User Already Exist"
            };
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.AddEmployee(user,currentUserId);
    }

    [Route("EditEmployees")]
    [HttpPost]
    public async Task<Result<UserModel>> EditEmployees(UserModel user, string userId)
    {
        UserModel isUserExist = await _employeeService.GetEmployeeById(userId);
        if (userId != isUserExist.UserId)
        {
            return new Result<UserModel>
            {
                Message = "User Not Exist"
            };
        }
        return await _employeeService.EditEmployee(user, userId);
    }

    [Route("GetAllEmployees")]
    [HttpGet]
    public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees([FromQuery] int pageNo, [FromQuery] int records)
    {
        Result<GetAllEmployeeResponseModel> data = await _employeeService.GetAllEmployees(pageNo, records);
        return data;
    }
    [Route("ChangePassword")]
    [HttpPost]
    public async Task<Result> ChangePassword(ChangePasswordRequest passwordModel)
    {
        Result result = new Result();
        string userId = CurrentContext.UserId(_httpContextAccessor);
        result.Success = await _employeeService.ResetPassword(userId, passwordModel.Password, passwordModel.OldPassword);
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

    //Employee Details APIs

    [Route("AddEditEmployeeSummary")]
    [HttpPost]
    public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.AddEditEmployeeSummary(userSummary, userId);
    }

    [Route("AddEmployeeEducation")]
    [HttpPost]
    public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.AddEmployeeEducation(educationDetails, userId);

    }

    [Route("EditEmployeeEducation")]
    [HttpPost]
    public async Task<Result> EditEmployeeEducation(EmpEducationDetails educationDetails)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.EditEmployeeEducation(educationDetails, userId);
    }

    [Route("AddEmployeeCertification")]
    [HttpPost]
    public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.AddEmployeeCertification(certificationDetails, userId);

    }

    [Route("EditEmployeeCertification")]
    [HttpPost]
    public async Task<Result> EditEmployeeCertification(EmpCertificationDetails certificationDetails)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
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

    [Route("AddSkills")]
    [HttpPost]
    public async Task<Result> AddEditSkills(SkillsRequestModel skillsModel)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.AddEditEmployeeSkills(skillsModel, userId);

    }

    [Route("GetEmployeeSkills")]
    [HttpGet]
    public async Task<Result<EmpSkills>> GetEmployeeSkills([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        EmpSkills result = await _employeeService.GetEmployeeSkills(userId);
        return new Result<EmpSkills>()
        {
            Success = true,
            MethodResult = result
        };

    }

    [Route("DeleteEducationDetails")]
    [HttpDelete]
    public async Task<Result> DeleteEducationDetails([FromQuery] string educationId)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        Result data = await _employeeService.DeleteEducationDetails(educationId, userId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                Message = "Education Detail Not Deleted ",
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            Message = "Education Detail Deleted Successfully",
            StatusCode = 200
        };
    }

    [Route("DeleteCertificationDetails")]
    [HttpDelete]
    public async Task<Result> DeleteCertificationDetails([FromQuery] string certificationId)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        Result data = await _employeeService.DeleteCertificationDetails(certificationId, userId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                Message = "Certification Detail Not Deleted ",
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            Message = "Certification Detail Deleted Successfully",
            StatusCode = 200
        };
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


}
