using System.IO;
using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class AttendanceImportControllerContractTests
{
    [Fact]
    public void AdminAttendanceController_RequiresAuthorization()
    {
        var controller = typeof(AdminAttendanceController);
        var authAttr = controller.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
    }

    [Theory]
    [InlineData("DownloadImportTemplate")]
    [InlineData("ValidateImport")]
    [InlineData("CommitImport")]
    public void Attendance_import_endpoints_require_create_for_employee_permission(string methodName)
    {
        var controller = typeof(AdminAttendanceController);
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Attendance, permission.Module);
        Assert.Contains(Permission.CreateForEmployee, permission.Permissions);
    }

    [Fact]
    public void ValidateImport_RequiresHttpPostAttribute()
    {
        var method = typeof(AdminAttendanceController).GetMethod("ValidateImport", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes<HttpPostAttribute>());
    }

    [Fact]
    public void CommitImport_RequiresHttpPostAttribute()
    {
        var method = typeof(AdminAttendanceController).GetMethod("CommitImport", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes<HttpPostAttribute>());
    }

    [Fact]
    public void DownloadImportTemplate_RequiresHttpGetAttribute()
    {
        var method = typeof(AdminAttendanceController).GetMethod("DownloadImportTemplate", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes<HttpGetAttribute>());
    }

    [Fact]
    public async Task ValidateImport_ReturnsBadRequest_WhenFileIsNull()
    {
        var controller = new AdminAttendanceController(null!, null!);
        var result = await controller.ValidateImport(null!, 2026, 10, true, null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task ValidateImport_ReturnsBadRequest_WhenFileIsNotXlsx()
    {
        var controller = new AdminAttendanceController(null!, null!);
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, 3, "file", "test.csv");

        var result = await controller.ValidateImport(file, 2026, 10, true, null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CommitImport_ReturnsBadRequest_WhenFileIsNull()
    {
        var controller = new AdminAttendanceController(null!, null!);
        var result = await controller.CommitImport(null!, 2026, 10, true, null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task CommitImport_ReturnsBadRequest_WhenFileIsNotXlsx()
    {
        var controller = new AdminAttendanceController(null!, null!);
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, 3, "file", "test.txt");

        var result = await controller.CommitImport(file, 2026, 10, true, null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
