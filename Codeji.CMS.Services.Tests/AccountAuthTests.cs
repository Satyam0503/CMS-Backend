using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Account;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.Enums;
using Moq;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public class AccountAuthTests
{
    public AccountAuthTests()
    {
        ConfigManager.AppSettings = new AppSettings
        {
            APIUrl = "https://api.test.local",
            AppUrl = "https://app.test.local",
            AppVersion = "test"
        };
        ConfigManager.Jwt = new JwtSettings
        {
            SecretKey = "test-only-secret-key-with-at-least-32-characters",
            Expiry = 30
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtClaimsAndStoresHashedRefreshToken()
    {
        var fixture = new AccountFixture();
        var user = CreateUser();
        var role = CreateRole(hasAppAccess: true);
        RefreshToken? storedToken = null;

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(user);
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false))
            .ReturnsAsync(role);
        fixture.RefreshTokenRepository
            .Setup(x => x.AddOne(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => storedToken = token)
            .ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.VerifyAndGenerateToken(new LoginModel
        {
            Email = user.Email,
            Password = "CorrectPassword1!"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.NotNull(storedToken);
        Assert.NotEqual(result.MethodResult.RefreshToken, storedToken.Token);
        Assert.Equal(TokenHelper.ComputeSha256Hash(result.MethodResult.RefreshToken), storedToken.Token);
        Assert.Equal(user.UserId, storedToken.UserId);
        Assert.InRange(storedToken.ExpireAt, DateTime.UtcNow.AddDays(7).AddSeconds(-5), DateTime.UtcNow.AddDays(7).AddSeconds(5));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.MethodResult.Token);
        Assert.Equal(ConfigManager.AppSettings.APIUrl, jwt.Issuer);
        Assert.Contains(ConfigManager.AppSettings.AppUrl, jwt.Audiences);
        Assert.Equal(user.UserId, jwt.Claims.Single(x => x.Type == "user_id").Value);
        Assert.Equal(user.CompanyId, jwt.Claims.Single(x => x.Type == "company_id").Value);
        Assert.Equal(user.RoleId, jwt.Claims.Single(x => x.Type == "role_id").Value);
        Assert.Equal(role.Titles, jwt.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task Login_InvalidPassword_IsRejectedWithoutCreatingRefreshToken()
    {
        var fixture = new AccountFixture();
        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(CreateUser());
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false))
            .ReturnsAsync(CreateRole(hasAppAccess: true));

        var result = await fixture.Service.VerifyAndGenerateToken(new LoginModel
        {
            Email = "user@example.com",
            Password = "WrongPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.InvalidCredential, result.StatusCode);
        fixture.RefreshTokenRepository.Verify(x => x.AddOne(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_RoleWithoutAppAccess_IsRejectedBeforePasswordValidation()
    {
        var fixture = new AccountFixture();
        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(CreateUser());
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false))
            .ReturnsAsync(CreateRole(hasAppAccess: false));

        var result = await fixture.Service.VerifyAndGenerateToken(new LoginModel
        {
            Email = "user@example.com",
            Password = "CorrectPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.UnauthorizedAppAccess, result.StatusCode);
        fixture.RefreshTokenRepository.Verify(x => x.AddOne(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnverifiedEmail_IsRejected()
    {
        var fixture = new AccountFixture();
        var user = CreateUser();
        user.IsEmailVerified = false;
        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(user);

        var result = await fixture.Service.VerifyAndGenerateToken(new LoginModel
        {
            Email = user.Email,
            Password = "CorrectPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.UnVerifiedMail, result.StatusCode);
        fixture.RoleRepository.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_RotatesTokenAndRevokesPreviousToken()
    {
        var fixture = new AccountFixture();
        const string currentRefreshToken = "current-refresh-token";
        var storedToken = new RefreshToken
        {
            Id = "refresh-1",
            UserId = "user-1",
            Token = TokenHelper.ComputeSha256Hash(currentRefreshToken),
            ExpireAt = DateTime.UtcNow.AddDays(1)
        };
        RefreshToken? replacement = null;

        fixture.RefreshTokenRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RefreshToken, bool>>>(), false))
            .ReturnsAsync(storedToken);
        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(CreateUser());
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false))
            .ReturnsAsync(CreateRole(hasAppAccess: true));
        fixture.RefreshTokenRepository
            .Setup(x => x.AddOne(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => replacement = token)
            .ReturnsAsync(new Result { Success = true });
        fixture.RefreshTokenRepository
            .Setup(x => x.Update(It.IsAny<FilterDefinition<RefreshToken>>(), storedToken))
            .ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.RefreshToken(new RefreshTokenRequestDto
        {
            RefreshToken = currentRefreshToken
        });

        Assert.True(result.Success);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
        Assert.NotNull(replacement);
        Assert.Equal(TokenHelper.ComputeSha256Hash(result.MethodResult.RefreshToken), replacement.Token);
        Assert.NotEqual(currentRefreshToken, result.MethodResult.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(result.MethodResult.Token));
        fixture.RefreshTokenRepository.Verify(
            x => x.Update(It.IsAny<FilterDefinition<RefreshToken>>(), storedToken), Times.Once);
    }

    [Fact]
    public async Task Logout_ValidRefreshToken_RevokesStoredTokenForCurrentUser()
    {
        var fixture = new AccountFixture();
        var storedToken = new RefreshToken
        {
            Id = "refresh-1",
            UserId = "user-1",
            Token = TokenHelper.ComputeSha256Hash("refresh-token"),
            ExpireAt = DateTime.UtcNow.AddDays(1)
        };
        fixture.RefreshTokenRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RefreshToken, bool>>>(), false))
            .ReturnsAsync(storedToken);
        fixture.RefreshTokenRepository
            .Setup(x => x.Update(It.IsAny<FilterDefinition<RefreshToken>>(), storedToken))
            .ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.LogOut("refresh-token", "user-1");

        Assert.True(result.Success);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task CreateNewPassword_ValidResetToken_UpdatesPasswordAndConsumesToken()
    {
        var fixture = new AccountFixture();
        var user = CreateUser();
        var token = "one-time-reset-token";
        var securityToken = new UserSecurityToken
        {
            Id = "security-1", UserId = user.UserId,
            TokenHash = TokenHelper.ComputeSha256Hash(token),
            Expiry = DateTime.UtcNow.AddMinutes(10), IsUsed = false,
            Type = EnumsHelper.SecurityTokenType.PasswordReset
        };
        fixture.SecurityTokenRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<UserSecurityToken, bool>>>(), false)).ReturnsAsync(securityToken);
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false)).ReturnsAsync(user);
        fixture.EmployeeRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), user)).ReturnsAsync(new Result { Success=true });
        fixture.SecurityTokenRepository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<UserSecurityToken>>(), It.IsAny<UpdateDefinition<UserSecurityToken>>())).ReturnsAsync(new Result { Success=true });

        var result = await fixture.Service.CreateNewPassword(new CreateNewPasswordRequest { Token=token, NewPassword="NewPassword1!" });

        Assert.True(result.Success);
        Assert.True(AuthenticationHandler.VerifyPassword("NewPassword1!", user.Password));
        fixture.SecurityTokenRepository.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<UserSecurityToken>>(), It.IsAny<UpdateDefinition<UserSecurityToken>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateNewPassword_EmailVerificationToken_CannotResetPassword()
    {
        var fixture = new AccountFixture();
        fixture.SecurityTokenRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<UserSecurityToken, bool>>>(), false)).ReturnsAsync((UserSecurityToken?)null);

        var result = await fixture.Service.CreateNewPassword(new CreateNewPasswordRequest { Token="verification-token", NewPassword="NewPassword1!" });

        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.InvalidExpiredToken, result.StatusCode);
        fixture.EmployeeRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<EmpUser>()), Times.Never);
    }

    private static EmpUser CreateUser() => new()
    {
        UserId = "user-1",
        CompanyId = "company-1",
        RoleId = "role-1",
        FirstName = "Test",
        LastName = "User",
        Email = "user@example.com",
        Password = AuthenticationHandler.HashedPassword("CorrectPassword1!"),
        Status = true,
        IsEmailVerified = true
    };

    private static Roles CreateRole(bool hasAppAccess) => new()
    {
        RolesId = "role-1",
        CompanyId = "company-1",
        Titles = "Administrator",
        RoleType = 1,
        HasAppAccess = hasAppAccess,
        UserRoles = []
    };

    private sealed class AccountFixture
    {
        public Mock<IMongoDbRepository<EmpUser>> EmployeeRepository { get; } = new();
        public Mock<IMongoDbRepository<RefreshToken>> RefreshTokenRepository { get; } = new();
        public Mock<IMongoDbRepository<Roles>> RoleRepository { get; } = new();
        public Mock<IMongoDbRepository<UserSecurityToken>> SecurityTokenRepository { get; } = new();
        public AccountServices Service { get; }

        public AccountFixture()
        {
            var middlewareService = new Mock<IMiddlewareService>();
            middlewareService
                .Setup(x => x.EmailSendAndSaveWithResult(It.IsAny<EmpEmailLogs>()))
                .ReturnsAsync((true, string.Empty));
            Service = new AccountServices(
                Mock.Of<IEmployeeService>(),
                EmployeeRepository.Object,
                RefreshTokenRepository.Object,
                RoleRepository.Object,
                Mock.Of<IMongoDbRepository<Company>>(),
                Mock.Of<IMongoDbRepository<MailTemplate>>(),
                Mock.Of<IPriorityTaskQueue>(),
                middlewareService.Object,
                SecurityTokenRepository.Object);
        }
    }
}
