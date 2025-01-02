
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;

namespace Codeji.CMS.Services.Interface;

public interface IUserService
{
    Task<Result<UserModel>> AddEmployee(UserModel user);
    Task<Result<UserModel>> EditEmployee(UserModel user);

    Task<UserModel> GetEmployeeById(string id);
    Task<List<UserModel>> GetAllEmployees();
    Task<bool> IsEmailExist(string email);
    Task<bool> ResetPassword(string userId, string password,string oldPassword="");
    Task<string> GetVerificationToken(string email, string password);

}
