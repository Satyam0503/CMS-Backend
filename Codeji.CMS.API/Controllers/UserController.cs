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
    [Route("AddEditEmployees")]
    [HttpPost]
    [Authorize]
    public async Task<Result<UserModel>> AddEditEmployees(UserModel user)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        return await _userService.AddEmployee(user, companyId);
    }

    [Route("GetAllEmployees")]
    [HttpGet]
    public async Task<Result<UserModel>> GetAllEmployees()
    {
        List<UserModel> data = await _userService.GetAllEmployees();
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
    [HttpPost]
    public async Task<Result<UserModel>> GetEmployeeById(string id)
    {
        UserModel result = await _userService.GetEmployeeById(id);
        return new Result<UserModel>()
        {
            Success = true,
            MethodResult = result
        };
    }

    //Employee Details APIs

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



