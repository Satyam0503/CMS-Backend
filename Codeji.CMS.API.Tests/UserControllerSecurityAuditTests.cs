using System.IO;
using System.Security.Claims;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class UserControllerSecurityAuditTests
{
    private const string CurrentCompanyId = "company-tenant-123";
    private const string CurrentUserId = "user-alice";
    private const string ColleagueUserId = "user-bob";

    [Fact]
    public async Task GetEmployeeById_WhenCalledByPeer_MasksPanBankPersonalEmailAndAddress()
    {
        var fixture = CreateFixture();
        var colleagueProfile = new UserModel
        {
            UserId = ColleagueUserId,
            FirstName = "Bob",
            LastName = "Smith",
            CompanyId = CurrentCompanyId,
            Email = "bob@company.com",
            PersonalEmail = "bob.personal@gmail.com",
            PanNumber = "ABCDE1234F",
            BankAccountNumber = 987654321012,
            PhoneNumber = "9998887776",
            Address = "123 Private Street",
            EmergencyContact = "Jane Smith: 9991112223"
        };

        fixture.EmployeeService
            .Setup(s => s.GetEmployeeById(ColleagueUserId))
            .ReturnsAsync(colleagueProfile);

        // Standard employee has NO HR view/edit access
        fixture.RoleService
            .Setup(r => r.VerifyUserAccess(AppModule.Employees, It.IsAny<string[]>(), CurrentUserId, CurrentCompanyId, null))
            .ReturnsAsync(false);

        var result = await fixture.Controller.GetEmployeeById(ColleagueUserId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("Bob", result.MethodResult.FirstName);
        Assert.Equal("bob@company.com", result.MethodResult.Email); // Work email retained

        // Sensitive PII and financial credentials MUST be masked/nullified:
        Assert.Null(result.MethodResult.PanNumber);
        Assert.Null(result.MethodResult.BankAccountNumber);
        Assert.Null(result.MethodResult.PersonalEmail);
        Assert.Null(result.MethodResult.Address);
        Assert.Null(result.MethodResult.PhoneNumber);
        Assert.Null(result.MethodResult.EmergencyContact);
    }

    [Fact]
    public async Task GetEmployeeById_WhenCalledBySelf_RetainsFullFinancialAndPiiDetails()
    {
        var fixture = CreateFixture();
        var myProfile = new UserModel
        {
            UserId = CurrentUserId,
            FirstName = "Alice",
            LastName = "Doe",
            CompanyId = CurrentCompanyId,
            Email = "alice@company.com",
            PersonalEmail = "alice.personal@gmail.com",
            PanNumber = "ABCDE1234F",
            BankAccountNumber = 987654321012,
            PhoneNumber = "9998887776",
            Address = "123 Private Street",
            EmergencyContact = "John Doe: 9991112223"
        };

        fixture.EmployeeService
            .Setup(s => s.GetEmployeeById(CurrentUserId))
            .ReturnsAsync(myProfile);

        var result = await fixture.Controller.GetEmployeeById(CurrentUserId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("ABCDE1234F", result.MethodResult.PanNumber);
        Assert.Equal((ulong)987654321012, result.MethodResult.BankAccountNumber);
        Assert.Equal("alice.personal@gmail.com", result.MethodResult.PersonalEmail);
        Assert.Equal("123 Private Street", result.MethodResult.Address);
    }

    [Fact]
    public async Task AddEditEmployeeSummary_WhenStandardUserAttemptsToModifyColleague_ReturnsForbidden()
    {
        var fixture = CreateFixture();
        fixture.RoleService
            .Setup(r => r.VerifyUserAccess(AppModule.Employees, new[] { Permission.Edit }, CurrentUserId, CurrentCompanyId, null))
            .ReturnsAsync(false);

        var result = await fixture.Controller.AddEditEmployeeSummary(new EmployeeSummaryRequestModel
        {
            UserId = ColleagueUserId,
            Summary = "Tampered summary by peer"
        });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        fixture.EmployeeService.Verify(s => s.AddEditEmployeeSummary(It.IsAny<EmployeeSummaryRequestModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddEmployeeEducation_WhenStandardUserAttemptsToTargetColleague_ReturnsForbidden()
    {
        var fixture = CreateFixture();
        fixture.RoleService
            .Setup(r => r.VerifyUserAccess(AppModule.Employees, new[] { Permission.Edit }, CurrentUserId, CurrentCompanyId, null))
            .ReturnsAsync(false);

        var result = await fixture.Controller.AddEmployeeEducation(new EmployeeEducationRequestModel
        {
            UserId = ColleagueUserId,
            EducationTitle = "Fake Degree",
            CollegeName = "Attacker College"
        });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        fixture.EmployeeService.Verify(s => s.AddEmployeeEducation(It.IsAny<EmployeeEducationRequestModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEducationDetails_WhenStandardUserAttemptsToTargetColleague_ReturnsForbidden()
    {
        var fixture = CreateFixture();
        fixture.RoleService
            .Setup(r => r.VerifyUserAccess(AppModule.Employees, new[] { Permission.Edit }, CurrentUserId, CurrentCompanyId, null))
            .ReturnsAsync(false);

        var result = await fixture.Controller.DeleteEducationDetails("edu-999", ColleagueUserId);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        fixture.EmployeeService.Verify(s => s.DeleteEducationDetails(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadUserImage_WhenNullOrEmptyFile_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.UploadUserImage(null!);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("No image file provided.", result.Message);
    }

    [Theory]
    [InlineData("exploit.svg")]
    [InlineData("payload.html")]
    [InlineData("malicious.exe")]
    [InlineData("script.sh")]
    public async Task UploadUserImage_WithDisallowedExtension_ReturnsBadRequest(string fileName)
    {
        var fixture = CreateFixture();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(1024); // 1 KB

        var result = await fixture.Controller.UploadUserImage(mockFile.Object);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("Invalid file type", result.Message);
    }

    private static UserControllerFixture CreateFixture()
    {
        var fixture = new UserControllerFixture();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(EnumsHelper.ClaimTypesEnum.company_id.ToString(), CurrentCompanyId),
            new Claim(EnumsHelper.ClaimTypesEnum.user_id.ToString(), CurrentUserId),
            new Claim(ClaimTypes.Role, "Employee")
        ], "TestAuth"));

        fixture.HttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        fixture.Controller = new UserController(
            fixture.HttpContextAccessor.Object,
            fixture.EmployeeService.Object,
            fixture.AccountServices.Object,
            fixture.OfficeScheduleAssignmentService.Object,
            fixture.OfficeScheduleSettingsService.Object,
            fixture.EmployeeExitEvidenceStorage.Object,
            fixture.FormerEmployeePortalService.Object,
            fixture.RoleService.Object);

        return fixture;
    }

    private sealed class UserControllerFixture
    {
        public UserController Controller { get; set; } = null!;
        public Mock<IHttpContextAccessor> HttpContextAccessor { get; } = new();
        public Mock<IEmployeeService> EmployeeService { get; } = new();
        public Mock<IAccountServices> AccountServices { get; } = new();
        public Mock<IOfficeScheduleAssignmentService> OfficeScheduleAssignmentService { get; } = new();
        public Mock<IOfficeScheduleSettingsService> OfficeScheduleSettingsService { get; } = new();
        public Mock<IEmployeeExitEvidenceStorage> EmployeeExitEvidenceStorage { get; } = new();
        public Mock<IFormerEmployeePortalService> FormerEmployeePortalService { get; } = new();
        public Mock<IRoleService> RoleService { get; } = new();
    }
}
