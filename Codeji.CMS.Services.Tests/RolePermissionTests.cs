using System.Linq.Expressions;
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
