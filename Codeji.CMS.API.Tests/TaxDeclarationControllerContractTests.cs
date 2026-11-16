using System.Reflection;
using System.Security.Claims;
using System.Linq.Expressions;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class TaxDeclarationControllerContractTests
{
    [Fact]
    public void Controller_has_api_route_and_authorize_attribute()
    {
        var type = typeof(TaxDeclarationController);
        var auth = type.GetCustomAttribute<AuthorizeAttribute>();
        var route = type.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(auth);
        Assert.NotNull(route);
        Assert.Equal("api/payroll/tax-declarations", route!.Template);
    }

    [Theory]
    [InlineData(nameof(TaxDeclarationController.GetDeclaration), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.GetCompanyDeclarations), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.CalculateTaxProjection), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.SubmitDeclaration), Permission.Edit)]
    [InlineData(nameof(TaxDeclarationController.ReviewDeclaration), Permission.Approve)]
    public void Endpoints_enforce_correct_payroll_permissions(string methodName, string expectedPermission)
    {
        var method = typeof(TaxDeclarationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var perm = method?.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(perm);
        Assert.Equal(AppModule.PayRoll, perm!.Module);
        Assert.Contains(expectedPermission, perm.Permissions);
    }

    [Fact]
    public async Task Company_declarations_forbid_non_hr_and_do_not_read_company_records()
    {
        var (controller, roleService, declarationService) = CreateController();
        roleService.Setup(x => x.IsRoleTypeMatch("role-1", It.IsAny<Codeji.CMS.Utility.Enums.EnumsHelper.Roles>(), "company-1"))
            .ReturnsAsync(false);

        var result = await controller.GetCompanyDeclarations("2026-27", "SUBMITTED", roleService.Object);

        Assert.IsType<ForbidResult>(result.Result);
        declarationService.Verify(x => x.GetCompanyDeclarations(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Company_declarations_allow_hr_to_read_only_the_authenticated_tenant()
    {
        var (controller, roleService, declarationService) = CreateController();
        roleService.Setup(x => x.IsRoleTypeMatch("role-1", Codeji.CMS.Utility.Enums.EnumsHelper.Roles.Administrator, "company-1"))
            .ReturnsAsync(false);
        roleService.Setup(x => x.IsRoleTypeMatch("role-1", Codeji.CMS.Utility.Enums.EnumsHelper.Roles.HR, "company-1"))
            .ReturnsAsync(true);
        declarationService.Setup(x => x.GetCompanyDeclarations("company-1", "2026-27", "SUBMITTED"))
            .ReturnsAsync(new Result<List<EmployeeTaxDeclarationDto>> { Success = true, MethodResult = [] });

        var result = await controller.GetCompanyDeclarations("2026-27", "SUBMITTED", roleService.Object);

        Assert.IsType<OkObjectResult>(result.Result);
        declarationService.Verify(x => x.GetCompanyDeclarations("company-1", "2026-27", "SUBMITTED"), Times.Once);
    }

    [Fact]
    public async Task Employee_self_service_resolves_user_id_to_canonical_employee_id_before_saving()
    {
        var (controller, roleService, declarationService) = CreateController();
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> _, bool __, bool ___) => new EmpUser
            {
                CompanyId = "company-1", UserId = "user-1", EmployeeId = "EMP-10",
                FirstName = "Test", LastName = "Employee", Email = "test@example.invalid"
            });
        declarationService.Setup(x => x.SubmitDeclaration("company-1", "EMP-10", It.IsAny<SubmitTaxDeclarationRequestDto>(), "user-1"))
            .ReturnsAsync(new Result<EmployeeTaxDeclarationDto> { Success = true, MethodResult = new EmployeeTaxDeclarationDto { EmployeeId = "EMP-10" } });

        await controller.SubmitDeclaration("user-1", new SubmitTaxDeclarationRequestDto { FinancialYear = "2026-27" }, roleService.Object, employees.Object);

        declarationService.Verify(x => x.SubmitDeclaration("company-1", "EMP-10", It.IsAny<SubmitTaxDeclarationRequestDto>(), "user-1"), Times.Once);
    }

    private static (TaxDeclarationController Controller, Mock<IRoleService> RoleService, Mock<ITaxDeclarationService> DeclarationService) CreateController()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim("user_id", "user-1"),
            new Claim("company_id", "company-1"),
            new Claim("role_id", "role-1")
        ], "test"));
        var context = new HttpContextAccessor { HttpContext = http };
        var roles = new Mock<IRoleService>();
        var declarations = new Mock<ITaxDeclarationService>();
        return (new TaxDeclarationController(declarations.Object, context), roles, declarations);
    }
}
