using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserController(IUserService userService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }
    [Route("AddEditEmployees")]
    [HttpPost]
    public async Task<Result<UserModel>> AddEditEmployees([FromBody] UserModel user)
    {
        return await _userService.AddEmployee(user);
    }
    [Route("GetAllEmployees")]
    [HttpGet]
    public async Task<Result<UserModel>> GetAllEmployees()
    {
        var data = await _userService.GetAllEmployees();
        var result = new Result<UserModel>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }
    [Route("ChangePassword")]
    [HttpGet]
    public async Task<Result> ChangePassword(string password, string oldPassword)
    {
        var result = new Result();
        string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
        result.Success = await _userService.ResetPassword(userId, password, oldPassword);
        return result;
    }
}



