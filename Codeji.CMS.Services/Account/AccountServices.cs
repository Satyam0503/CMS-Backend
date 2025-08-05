using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Account;

public class AccountServices : IAccountServices
{
    readonly IEmployeeService _employeeService;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    readonly IMongoDbRepository<RefreshToken> _refreshTokenRepository;
    readonly IMongoDbRepository<Roles> _rolesRepository;
    readonly IMongoDbRepository<PasswordResetTokens> _passwordResetTokens;
    readonly IMongoDbRepository<Company> _companyRepository;
    readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
    private readonly IPriorityTaskQueue _priorityTaskQueue;
    private readonly IMiddlewareService _middlewareService;
    public AccountServices(
     IEmployeeService employeeService,
     IMongoDbRepository<EmpUser> employeeRepository,
     IMongoDbRepository<RefreshToken> refreshTokenRepository,
    IMongoDbRepository<Roles> rolesRepository,
    IMongoDbRepository<PasswordResetTokens> passwordResetTokens,
     IMongoDbRepository<Company> companyRepository,
      IMongoDbRepository<MailTemplate> mailTemplateRepository,
      IPriorityTaskQueue priorityTaskQueue,
       IMiddlewareService middlewareService
    )
    {
        _employeeService = employeeService;
        _employeeRepository = employeeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _rolesRepository = rolesRepository;
        _passwordResetTokens = passwordResetTokens;
        _companyRepository = companyRepository;
        _mailTemplateRepository = mailTemplateRepository;
        _priorityTaskQueue = priorityTaskQueue;
        _middlewareService = middlewareService;
    }
    public async Task<bool> ResetPassword(string userId, string password, string oldPassword)
    {
        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
        if (user is null)
            return false;
        if (!string.IsNullOrEmpty(oldPassword))
        {
            if (!AuthenticationHandler.VerifyPassword(oldPassword, user.Password))
                return false;
        }

        user.Password = AuthenticationHandler.HashedPassword(password);
        user.UpdatedDate = DateTime.Now;
        Expression<Func<EmpUser, bool>> whereCondition = x => user.UserId == x.UserId;
        await _employeeRepository.Update(whereCondition, user);
        return true;
    }
    public async Task<Result<TokenResponseDto>> VerifyAndGenerateToken(LoginModel model)
    {
        Result<TokenResponseDto> result = new();
        bool isEmailExist = await _employeeService.IsEmailExist(model.Email);
        if (!isEmailExist)
        {
            result.Message = "Invalid credentials";
            result.Success = false;
            result.StatusCode = 400;
            return result;
        }
        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
        if (!user.IsEmailVerified)
        {
            result.Message = "Please Verify Email First";
            result.Success = false;
            return result;
        }
        Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == user.RoleId);
        if (!role.HasAppAccess)
        {
            result.Message = "You Don't have app access";
            result.Success = false;
            result.StatusCode = 403;
            return result;
        }
        bool isPasswordValid = AuthenticationHandler.VerifyPassword(model.Password, user.Password);
        if (!isPasswordValid)
        {
            result.Message = "Invalid credentials";
            result.Success = false;
            result.StatusCode = 400;
            return result;
        }
        List<string> roles = ["admin", "employee"];
        string token = AuthenticationHandler.GenerateJwtToken(user.UserId, user.CompanyId, user.RoleId, roles);
        string refreshToken = TokenHelper.GenerateToken();
        string hashedRefreshToken = TokenHelper.ComputeSha256Hash(refreshToken);

        RefreshToken refreshTokenEntity = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.UserId,
            Token = hashedRefreshToken,
            ExpireAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        var result2 = await _refreshTokenRepository.AddOne(refreshTokenEntity);
        if (!result2.Success)
        {
            result.Success = false;
            return result;
        }
        result.MethodResult = new TokenResponseDto()
        {
            Token = token,
            RefreshToken = refreshToken
        };
        result.Success = true;
        result.StatusCode = 200;
        return result;
    }

    public async Task<Result> GenerateTokenAndSendEmail(string email)
    {
        Result result = new();
        var emp = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email));
        if (emp is null) return result;
        var token = TokenHelper.GenerateToken();
        var hashedToken = TokenHelper.ComputeSha256Hash(token);

        PasswordResetTokens passwordResetTokens = new()
        {
            UserId = emp.UserId,
            TokenHash = hashedToken,
            IsUsed = false,
            Expiry = DateTime.UtcNow.AddMinutes(10),
        };
        var result2 = await _passwordResetTokens.AddOne(passwordResetTokens);
        if (!result2.Success)
        {
            return result;
        }
        Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == emp.CompanyId);
        MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.ResetPassword);
        string replacedBody = HtmlTemplate.Render(emailContent?.body, new
        {
            EmployeeName = emp.FirstName + " " + emp.LastName,
            PasswordResetLink = $"{ConfigManager.AppSettings.AppUrl}auth/createpassword?token={Uri.EscapeDataString(token)}&uid={emp.UserId}",
            CompanyName = company != null ? company.CompanyName : "",
        });
        string replacedSubject = HtmlTemplate.Render(emailContent.subject, new
        {
            CompanyName = company != null ? company.CompanyName : ""
        });

        _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
        {
            _middlewareService.EmailSendAndSave(new EmpEmailLogs()
            {
                UserTo = emp.Email,
                Subject = replacedSubject,
                Body = replacedBody,
                EmailLogType = EnumsHelper.MailType.ResetPassword,
                Email = emp.Email,
                UserFrom = "",
            });
        }, priority: 1);

        result.Success = true;
        result.Message = "Password reset link has been sent. Please check your mail";
        return result;
    }

    public async Task<Result> CreateNewPassword(CreateNewPasswordRequest model)
    {
        Result result = new();
        var tokenHash = TokenHelper.ComputeSha256Hash(model.Token);
        PasswordResetTokens? token = await _passwordResetTokens.FirstOrDefault(x => x.UserId == model.Uid && x.TokenHash == tokenHash && !x.IsUsed);
        if (token is null)
        {
            result.Message = "Invalid or expired token";
            return result;
        }
        if (token?.Expiry < DateTime.UtcNow)
        {
            result.Message = "This password reset link has expired. Please request a new one.";
            return result;
        }

        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == model.Uid);
        if (user is null)
        {
            return result;
        }
        user.Password = AuthenticationHandler.HashedPassword(model.NewPassword);
        user.IsEmailVerified = true;
        Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == user.UserId;
        await _employeeRepository.Update(whereCondition, user);
        Expression<Func<PasswordResetTokens, bool>> whereCondition2 = x => x.Id == token.Id;
        var result2 = await _passwordResetTokens.Delete(whereCondition2);
        result.Success = true;
        result.Message = "Password has been reset";
        return result;
    }
    // Logic for Login User and Employee by Email and Password

    public async Task<Result<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto model)
    {
        Result<TokenResponseDto> result = new()
        {
            Success = false
        };
        string hashedRequestToken = TokenHelper.ComputeSha256Hash(model.RefreshToken);
        Expression<Func<RefreshToken, bool>> whereCondition = rt => rt.Token == hashedRequestToken;
        RefreshToken? storedRefreshToken = await _refreshTokenRepository.FirstOrDefault(whereCondition);
        if (storedRefreshToken == null)
        {
            result.Message = "Invalid Refresh Token";
            return result;
        }
        if (storedRefreshToken.IsRevoked)
        {
            result.Message = "Refresh token has been revoked";
            return result;
        }
        if (storedRefreshToken.ExpireAt < DateTime.UtcNow)
        {
            result.Message = "Refresh Token Expired";
            return result;
        }
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(x => x.UserId == storedRefreshToken.UserId);
        if (empUser == null)
        {
            result.Message = "Invalid Refresh Token";
            return result;
        }
        string newRefreshToken = TokenHelper.GenerateToken();
        string newRefreshTokenHashed = TokenHelper.ComputeSha256Hash(newRefreshToken);
        List<string> roles = ["admin", "employee"];
        string newJwtToken = AuthenticationHandler.GenerateJwtToken(empUser.UserId, empUser.CompanyId, empUser.RoleId, roles);
        RefreshToken refreshToken = new()
        {
            UserId = empUser.UserId,
            Token = newRefreshTokenHashed,
            ExpireAt = DateTime.UtcNow.AddDays(7),
        };
        Result result1 = await _refreshTokenRepository.AddOne(refreshToken);
        if (!result1.Success) return result;
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.Update(whereCondition, storedRefreshToken);
        result.MethodResult = new TokenResponseDto()
        {
            RefreshToken = newRefreshToken,
            Token = newJwtToken
        };
        result.Success = true;
        return result;
    }

    public async Task<Result> LogOut(string refreshToken, string userid)
    {
        Result result = new();
        string hashedToken = TokenHelper.ComputeSha256Hash(refreshToken);
        Expression<Func<RefreshToken, bool>> whereCondition = rt => rt.Token == hashedToken && rt.UserId == userid;
        RefreshToken? storedRefreshToken = await _refreshTokenRepository.FirstOrDefault(whereCondition);
        if (storedRefreshToken == null)
        {
            result.Message = "Invalid Refresh Token";
            return result;
        }
        if (storedRefreshToken.IsRevoked)
        {
            result.Message = "Refresh token already revoked";
            return result;
        }
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        return await _refreshTokenRepository.Update(whereCondition, storedRefreshToken);
    }

}
