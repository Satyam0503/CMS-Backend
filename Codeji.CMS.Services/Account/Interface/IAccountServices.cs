using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;

namespace Codeji.CMS.Services.Account.Interface;

public interface IAccountServices
{
    Task<bool> ResetPassword(string userId, string password, string oldPassword = "");
    Task<Result<TokenResponseDto>> VerifyAndGenerateToken(LoginModel model);
    Task<Result<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto model);
    Task<Result> LogOut(string refreshToken, string userId);
    Task<Result> GenerateTokenAndSendEmail(string email);
    Task<Result> CreateNewPassword(CreateNewPasswordRequest model);
}
