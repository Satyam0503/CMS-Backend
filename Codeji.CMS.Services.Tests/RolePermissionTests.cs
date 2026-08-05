using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Interface;
using MapsterMapper;
using Moq;
using AuthRole = Codeji.CMS.Utility.Enums.EnumsHelper.Roles;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;

namespace Codeji.CMS.Services.Tests;

public class RolePermissionTests
{
    [Theory]
    [InlineData(AuthRole.Administrator, true)]
    [InlineData(AuthRole.HR, false)]
    [InlineData(AuthRole.Employee, false)]
    public async Task AdminRoleCheck_OnlyMatchesAdministratorRole(AuthRole storedRoleType, bool expected)
    {
        var fixture = new RoleFixture();
        var storedRole = new Codeji.CMS.Repository.Entities.RolePermissions.Roles
        {
            RolesId = "role-1",
            CompanyId = "company-1",
            RoleType = (int)storedRoleType,
            Titles = storedRoleType.ToString(),
            UserRoles = []
        };
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>>>(), false))
            .ReturnsAsync((Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>> predicate, bool _) =>
                predicate.Compile()(storedRole) ? storedRole : null);

        var allowed = await fixture.Service.IsRoleTypeMatch("role-1", AuthRole.Administrator, "company-1");

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public async Task AdminRoleCheck_WrongCompany_IsDenied()
    {
        var fixture = new RoleFixture();
        var storedRole = new Codeji.CMS.Repository.Entities.RolePermissions.Roles
        {
            RolesId = "role-1",
            CompanyId = "other-company",
            RoleType = (int)AuthRole.Administrator,
            Titles = "Administrator",
            UserRoles = []
        };
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>>>(), false))
            .ReturnsAsync((Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>> predicate, bool _) =>
                predicate.Compile()(storedRole) ? storedRole : null);

        Assert.False(await fixture.Service.IsRoleTypeMatch("role-1", AuthRole.Administrator, "company-1"));
    }

    [Theory]
    [InlineData("Edit", true)]
    [InlineData("Delete", false)]
    public async Task ModulePermission_RequiresAssignedModuleAndAction(string requestedPermission, bool expected)
    {
        var fixture = new RoleFixture();
        fixture.Middleware.Setup(x => x.GetUserById("user-1"))
            .ReturnsAsync(new UserModel { UserId = "user-1", RoleId = "role-1", CompanyId = "company-1" });
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>>>(), false))
            .ReturnsAsync(new Codeji.CMS.Repository.Entities.RolePermissions.Roles
            {
                RolesId = "role-1",
                CompanyId = "company-1",
                Titles = "HR",
                UserRoles = []
            });
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 1, PermissionConstant = "Edit", PermissionName = "Edit" }]);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 1, ModuleConstant = "Employee", ModuleName = "Employee" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 10, ModuleId = 1, PermissionId = 1 }]);
        fixture.RolePermissionRepository
            .Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns(new[] { new RolePermission { RoleId = "role-1", ModulePermissionId = 10, IsAccessible = true, HasAccess = true } }.AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            "Employee", [requestedPermission], "user-1", "company-1");

        Assert.Equal(expected, allowed);
    }

    [Theory]
    [InlineData((int)AuthRole.Employee)]
    [InlineData(4)]
    public async Task ModulePermission_AssignedPermissionIsAuthoritativeRegardlessOfRoleType(int roleType)
    {
        var fixture = new RoleFixture();
        fixture.Middleware.Setup(x => x.GetUserById("user-1"))
            .ReturnsAsync(new UserModel { UserId = "user-1", RoleId = "role-1", CompanyId = "company-1" });
        fixture.RoleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.RolePermissions.Roles, bool>>>(), false))
            .ReturnsAsync(new Codeji.CMS.Repository.Entities.RolePermissions.Roles
            {
                RolesId = "role-1", CompanyId = "company-1", RoleType = roleType,
                Titles = "Non-HR role", UserRoles = []
            });
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 1, PermissionConstant = "View", PermissionName = "View" }]);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 1, ModuleConstant = "PayRoll", ModuleName = "PayRoll" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 10, ModuleId = 1, PermissionId = 1 }]);
        fixture.RolePermissionRepository
            .Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns(new[] { new RolePermission { RoleId = "role-1", ModulePermissionId = 10, IsAccessible = true, HasAccess = true } }.AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            Codeji.CMS.Utility.Constraints.AppModule.PayRoll,
            [Codeji.CMS.Utility.Constraints.Permission.View],
            "user-1", "company-1");

        Assert.True(allowed);
    }

    [Fact]
    public async Task AddDefaultRole_CopiesOnlyOneActiveTemplatePerTypeAndWaitsForDistinctPermissions()
    {
        var fixture = new RoleFixture();
        var templates = new[]
        {
            new Roles { RolesId = "admin-old", RoleType = 1, IsDefault = true, Titles = "Admin", CreatedDate = DateTime.UtcNow, UserRoles = [] },
            new Roles { RolesId = "admin-deleted", RoleType = 1, IsDefault = true, IsDeleted = true, Titles = "Deleted admin", UserRoles = [] },
            new Roles { RolesId = "hr-old", RoleType = 2, IsDefault = true, Titles = "HR", CreatedDate = DateTime.UtcNow, UserRoles = [] },
            new Roles { RolesId = "hr-new", RoleType = 2, IsDefault = true, Titles = "HR copy", CreatedDate = DateTime.UtcNow.AddMinutes(1), UserRoles = [] },
        };
        fixture.RoleRepository
            .Setup(x => x.Get(It.IsAny<Expression<Func<Roles, bool>>>(), null, false))
            .Returns((Expression<Func<Roles, bool>> predicate, object? _, bool _) => templates.Where(predicate.Compile()).AsQueryable());
        fixture.RolePermissionRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RolePermission, bool>>>(), false, true))
            .ReturnsAsync([
                new RolePermission { RoleId = "admin-old", ModulePermissionId = 10, HasAccess = true, IsAccessible = true },
                new RolePermission { RoleId = "admin-old", ModulePermissionId = 10, HasAccess = false, IsAccessible = false },
                new RolePermission { RoleId = "hr-new", ModulePermissionId = 20, HasAccess = true, IsAccessible = true },
            ]);
        fixture.RoleRepository.Setup(x => x.AddMany(It.IsAny<IEnumerable<Roles>>())).ReturnsAsync(new Result { Success = true });
        fixture.RolePermissionRepository.Setup(x => x.AddMany(It.IsAny<IEnumerable<RolePermission>>())).ReturnsAsync(new Result { Success = true });

        var roles = await fixture.Service.AddDefaultRole("company-1");

        Assert.Equal(new[] { 1, 2 }, roles.Select(x => x.RoleType).Order());
        fixture.RoleRepository.Verify(x => x.AddMany(It.Is<IEnumerable<Roles>>(saved => saved.Count() == 2)), Times.Once);
        fixture.RolePermissionRepository.Verify(x => x.AddMany(It.Is<IEnumerable<RolePermission>>(saved =>
            saved.Count() == 2 && saved.All(x => x.CompanyId == "company-1"))), Times.Once);
    }

    private sealed class RoleFixture
    {
        public Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.RolePermissions.Roles>> RoleRepository { get; } = new();
        public Mock<IMongoDbRepository<RolePermission>> RolePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<ModulePermission>> ModulePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<Module>> ModuleRepository { get; } = new();
        public Mock<IMongoDbRepository<Permission>> PermissionRepository { get; } = new();
        public Mock<IMiddlewareService> Middleware { get; } = new();
        public RoleServices Service { get; }

        public RoleFixture()
        {
            Service = new RoleServices(
                RoleRepository.Object,
                RolePermissionRepository.Object,
                ModulePermissionRepository.Object,
                ModuleRepository.Object,
                PermissionRepository.Object,
                Mock.Of<IMongoDbRepository<EmpUser>>(),
                Mock.Of<IMapper>(),
                Middleware.Object);
        }
    }
}
