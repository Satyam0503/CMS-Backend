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
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Account;

public class AccountServices : IAccountServices
{
    readonly IEmployeeService _employeeService;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    readonly IMongoDbRepository<RefreshToken> _refreshTokenRepository;
    readonly IMongoDbRepository<Roles> _rolesRepository;
    readonly IMongoDbRepository<Company> _companyRepository;
    readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
    readonly IPriorityTaskQueue _priorityTaskQueue;
    readonly IMiddlewareService _middlewareService;
    readonly IMongoDbRepository<UserSecurityToken> _userSecurityTokenRepository;
    public AccountServices(
        IEmployeeService employeeService,
        IMongoDbRepository<EmpUser> employeeRepository,
        IMongoDbRepository<RefreshToken> refreshTokenRepository,
        IMongoDbRepository<Roles> rolesRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<MailTemplate> mailTemplateRepository,
        IPriorityTaskQueue priorityTaskQueue,
        IMiddlewareService middlewareService,
        IMongoDbRepository<UserSecurityToken> userSecurityTokenRepository
    )
    {
        _employeeService = employeeService;
        _employeeRepository = employeeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _rolesRepository = rolesRepository;
        _companyRepository = companyRepository;
        _mailTemplateRepository = mailTemplateRepository;
        _priorityTaskQueue = priorityTaskQueue;
        _middlewareService = middlewareService;
        _userSecurityTokenRepository = userSecurityTokenRepository;
    }
    public async Task<Result> ResetPassword(string userId, ChangePasswordRequest password)
    {
        Result result = new();
        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
        if (user == null) return result;
        else if (user.Password == null) return result;
        else if (!AuthenticationHandler.VerifyPassword(password.OldPassword, user.Password))
        {
            result.StatusCode = CustomStatusCode.InvalidCredential;
            return result;
        }
        else
        {
            user.Password = AuthenticationHandler.HashedPassword(password.NewPassword);
            user.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> whereCondition = x => user.UserId == x.UserId;
            result = await _employeeRepository.Update(whereCondition, user);
            return result;
        }
    }

    public async Task<Result<TokenResponseDto>> VerifyAndGenerateToken(LoginModel model)
    {
        Result<TokenResponseDto> result = new();
        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase) && x.Status);
        if (user == null)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InvalidCredential;
            return result;
        }
        if (!user.IsEmailVerified)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.UnVerifiedMail;
            return result;
        }
        Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == user.RoleId);
        if (role == null)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InvalidCredential;
            return result;
        }
        if (!role.HasAppAccess)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.UnauthorizedAppAccess;
            return result;
        }
        bool isPasswordValid = AuthenticationHandler.VerifyPassword(model.Password, user.Password);
        if (!isPasswordValid)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InvalidCredential;
            return result;
        }
        List<string> roles = [role.Titles];
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
        result.StatusCode = StatusCodes.Status200OK;
        return result;
    }

    public async Task<Result> GenerateTokenAndSendEmail(string email)
    {
        Result result = new();
        // get employee by email
        email = email.Trim();
        // Employees who have not created an initial password must still be able
        // to prove mailbox ownership and create one through this flow.
        var emp = await _employeeRepository.FirstOrDefault(x => x.Email == email && x.Status && !x.IsDeleted);
        // Do not reveal whether an address exists, but return the same accepted
        // response used for a real reset request.
        if (emp is null)
            return new Result { Success = true, StatusCode = CustomStatusCode.PasswordResetLinkSent };

        // Generate a fresh token without cancelling other unexpired links. Email
        // delivery and browser retries can overlap; all remaining links are
        // invalidated after the first successful password change.
        var token = TokenHelper.GenerateToken();
        var hashedToken = TokenHelper.ComputeSha256Hash(token);
        int tokenExpiryMinutes = 15;
        UserSecurityToken securityToken = new()
        {
            UserId = emp.UserId,
            TokenHash = hashedToken,
            IsUsed = false,
            Expiry = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes),
            Type = EnumsHelper.SecurityTokenType.PasswordReset,
        };
        var tokenResult = await _userSecurityTokenRepository.AddOne(securityToken);
        if (!tokenResult.Success)
            return new Result { Success=false, StatusCode=StatusCodes.Status500InternalServerError, Message="Unable to create a password reset request." };

        Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == emp.CompanyId);
        MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.ResetPassword);
        var resetLink = $"{ConfigManager.AppSettings.AppUrl.TrimEnd('/')}/auth/createpassword?token={Uri.EscapeDataString(token)}";
        var bodyTemplate = emailContent?.body ?? "<p>Hello [EmployeeName],</p><p>Reset your password using this link: <a href=\"[PasswordResetLink]\">Reset password</a>.</p><p>This link expires in [LinkExpiryTime].</p>";
        string replacedBody = HtmlTemplate.Render(bodyTemplate, new
        {
            EmployeeName = emp.FirstName + " " + emp.LastName,
            PasswordResetLink = resetLink,
            CompanyName = company != null ? company.CompanyName : "",
            LinkExpiryTime = $"{tokenExpiryMinutes} Minutes",
            CompanyLogo = company?.CompanyLogo != null ? Common.GetCompanyLogoUrl(company.CompanyLogo) : string.Empty,
            Year = DateTime.UtcNow.Year,
        });
        string replacedSubject = HtmlTemplate.Render(emailContent?.subject ?? "Reset your password", new
        {
            CompanyName = company != null ? company.CompanyName : ""
        });

        var delivery = await _middlewareService.EmailSendAndSaveWithResult(new EmpEmailLogs()
        {
            UserTo = emp.UserId,
            Subject = replacedSubject,
            Body = replacedBody,
            EmailLogType = EnumsHelper.MailType.ResetPassword,
            Email = emp.Email,
            UserFrom = "",
        });

        if (!delivery.IsSent)
        {
            return new Result
            {
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                Message = "The reset email could not be sent. Please try again later."
            };
        }

        result.Success = true;
        result.StatusCode = CustomStatusCode.PasswordResetLinkSent;
        return result;
    }

    public async Task<Result> CreateNewPassword(CreateNewPasswordRequest model)
    {
        Result result = new();
        if (model == null || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
            return new Result { Success=false, StatusCode=StatusCodes.Status400BadRequest, Message="A valid reset token and password are required." };
        var tokenHash = TokenHelper.ComputeSha256Hash(model.Token.Trim());
        // UserSecurityToken? userSecurityToken = await _userSecurityTokenRepository.FirstOrDefault(t => t.TokenHash == tokenHash && t.Type == EnumsHelper.SecurityTokenType.Invite && !t.IsUsed);
        UserSecurityToken? userSecurityToken = await _userSecurityTokenRepository.FirstOrDefault(t => t.TokenHash == tokenHash &&
            (t.Type == EnumsHelper.SecurityTokenType.PasswordReset || t.Type == EnumsHelper.SecurityTokenType.Invite) && !t.IsUsed);
        // PasswordResetTokens? token = await _passwordResetTokens.FirstOrDefault(x => x.TokenHash == tokenHash && !x.IsUsed);
        if (userSecurityToken is null)
        {
            result.StatusCode = CustomStatusCode.InvalidExpiredToken;
            return result;
        }
        if (userSecurityToken?.Expiry < DateTime.UtcNow)
        {
            result.StatusCode = CustomStatusCode.PasswordResetLinkExpired;
            return result;
        }

        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userSecurityToken.UserId && x.Status && !x.IsDeleted);
        if (user is null) return result;
        user.Password = AuthenticationHandler.HashedPassword(model.NewPassword);
        user.IsEmailVerified = true;
        Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == user.UserId;
        var passwordResult = await _employeeRepository.Update(whereCondition, user);
        if (!passwordResult.Success) return passwordResult;

        // mark current token used
        Expression<Func<UserSecurityToken, bool>> userTokenExpression = ut => ut.Id == userSecurityToken.Id;
        await _userSecurityTokenRepository.UpdateMany(userTokenExpression, Builders<UserSecurityToken>.Update.Set(t => t.IsUsed, true).Set(t => t.UsedAt, DateTime.UtcNow));

        // Invalidate any other active reset/invite links after a successful password change.
        Expression<Func<UserSecurityToken, bool>> expression = x => x.UserId == user.UserId &&
            (x.Type == EnumsHelper.SecurityTokenType.PasswordReset || x.Type == EnumsHelper.SecurityTokenType.Invite) && !x.IsUsed;
        await _userSecurityTokenRepository.UpdateMany(expression,
            Builders<UserSecurityToken>.Update.Set(x => x.IsUsed, true).Set(x => x.UsedAt, DateTime.UtcNow));
        result.Success = true;
        result.StatusCode = CustomStatusCode.PasswordResetSuccess;
        return result;
    }

    public async Task<Result> VerifyEmail(string token)
    {
        Result result = new();
        if (string.IsNullOrWhiteSpace(token))
        {
            result.StatusCode = CustomStatusCode.InvalidExpiredToken;
            return result;
        }

        string tokenHash = TokenHelper.ComputeSha256Hash(token);
        UserSecurityToken? userSecurityToken = await _userSecurityTokenRepository.FirstOrDefault(
            t => t.TokenHash == tokenHash
                 && t.Type == EnumsHelper.SecurityTokenType.EmailVerification
                 && !t.IsUsed);

        if (userSecurityToken is null)
        {
            result.StatusCode = CustomStatusCode.InvalidExpiredToken;
            return result;
        }

        if (userSecurityToken.Expiry < DateTime.UtcNow)
        {
            result.StatusCode = CustomStatusCode.PasswordResetLinkExpired;
            return result;
        }

        EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userSecurityToken.UserId && x.Status);
        if (user is null)
        {
            result.StatusCode = CustomStatusCode.EmployeeNotExist;
            return result;
        }

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == user.UserId;
            var verifyResult = await _employeeRepository.Update(whereCondition, user);
            if (!verifyResult.Success) return verifyResult;
        }

        Expression<Func<UserSecurityToken, bool>> userTokenExpression = ut => ut.Id == userSecurityToken.Id;
        var consumeResult = await _userSecurityTokenRepository.UpdateMany(
            userTokenExpression,
            Builders<UserSecurityToken>.Update.Set(t => t.IsUsed, true).Set(t => t.UsedAt, DateTime.UtcNow));
        if (!consumeResult.Success) return consumeResult;

        result.Success = true;
        return result;
    }

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
            result.StatusCode = CustomStatusCode.InvalidRefreshToken;
            return result;
        }
        if (storedRefreshToken.IsRevoked)
        {
            result.StatusCode = CustomStatusCode.RefreshTokenRevoked;
            return result;
        }
        if (storedRefreshToken.ExpireAt < DateTime.UtcNow)
        {
            result.StatusCode = CustomStatusCode.RefreshTokenExpired;
            return result;
        }
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(x => x.UserId == storedRefreshToken.UserId);
        if (empUser == null)
        {
            result.StatusCode = CustomStatusCode.InvalidRefreshToken;
            return result;
        }
        Roles? role = await _rolesRepository.FirstOrDefault(r => r.CompanyId == empUser.CompanyId && r.RolesId == empUser.RoleId);
        string newRefreshToken = TokenHelper.GenerateToken();
        string newRefreshTokenHashed = TokenHelper.ComputeSha256Hash(newRefreshToken);
        List<string> roles = [role.Titles];
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
            result.StatusCode = CustomStatusCode.InvalidRefreshToken;
            return result;
        }
        if (storedRefreshToken.IsRevoked)
        {
            // detected the use of revoked refresh token.Remove all the active refresh token 
            result.StatusCode = CustomStatusCode.RefreshTokenRevoked;
            Expression<Func<RefreshToken, bool>> expression = rt => !rt.IsRevoked && rt.UserId == userid;
            return await _refreshTokenRepository.UpdateMany(expression, Builders<RefreshToken>.Update.Set(rt => rt.IsRevoked, true).Set(rt => rt.RevokedAt, DateTime.UtcNow));
        }
        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        result = await _refreshTokenRepository.Update(whereCondition, storedRefreshToken);
        return result;
    }

}
