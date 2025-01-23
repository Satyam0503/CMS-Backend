using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IEmployeeService _employeeService;

    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserController(IUserService userService, IHttpContextAccessor httpContextAccessor, IEmployeeService employeeService)
    {
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
        _employeeService = employeeService;
    }
    [Route("AddEmployees")]
    [HttpPost]
    [Authorize]
    public async Task<Result<UserModel>> AddEmployees(UserModel user)
    {
        bool isEmailExist = await _userService.IsEmailExist(user.Email);
        if (isEmailExist)
        {
            return new Result<UserModel>
            {
                Success = false,
                Message = "User Already Exist"
            };
        }
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        return await _userService.AddEmployee(user, companyId);
    }

    [Route("EditEmployees")]
    [HttpPost]
    [Authorize]
    public async Task<Result<UserModel>> EditEmployees(UserModel user, string userId)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        UserModel isUserExist = await _userService.GetEmployeeById(userId);
        if (userId != isUserExist.UserId)
        {
            return new Result<UserModel>
            {
                Message = "User Not Exist"
            };
        }
        return await _userService.EditEmployee(user, userId, companyId, isUserExist.Password); //Pasword Field Need to change when create Verify Mail feature of User.
    }

    [Route("GetAllEmployees")]
    [HttpGet]
    [Authorize]
    public async Task<Result<UserModel>> GetAllEmployees()
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        List<UserModel> data = await _userService.GetAllEmployees(companyId);
        Result<UserModel> result = new Result<UserModel>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }
    [Route("ChangePassword")]
    [HttpGet]
    public async Task<Result> ChangePassword(string password, string oldPassword)
    {
        Result result = new Result();
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        result.Success = await _userService.ResetPassword(userId, password, oldPassword);
        return result;
    }

    [Route("GetEmployeeById")]
    [HttpGet]
    [Authorize]
    public async Task<Result<UserModel>> GetEmployeeById()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        UserModel result = await _userService.GetEmployeeById(userId);
        return new Result<UserModel>()
        {
            Success = true,
            MethodResult = result
        };
    }

    //Employee Details APIs

    [Route("AddEmployeeSummary")]
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
    [HttpPost]
    [Authorize]
    public async Task<Result<EmployeeSummaryRequestModel>> GetEmployeeSummary()
    {
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        EmployeeSummaryRequestModel summary = await _employeeService.GetEmployeeSummary(userId);
        return new Result<EmployeeSummaryRequestModel>()
        {
            Success = true,
            MethodResult = summary
        };
    }

    [Route("GetEmployeeEducationDetails")]
    [HttpPost]
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
    [HttpPost]
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

}



