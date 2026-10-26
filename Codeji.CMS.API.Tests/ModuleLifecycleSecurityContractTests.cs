using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class ModuleLifecycleSecurityContractTests
{
    [Fact]
    public void GeneratePaySlip_has_payroll_view_permission()
    {
        var method = typeof(PayRollController).GetMethod(nameof(PayRollController.GetSalarySlip), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.PayRoll, attr.Module);
        Assert.Contains(Permission.View, attr.Permissions);
    }

    [Fact]
    public void GetAllCalendarItems_has_calendar_view_permission()
    {
        var method = typeof(CalendarController).GetMethod(nameof(CalendarController.GetAllCalendarItems), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.Calendar, attr.Module);
        Assert.Contains(Permission.View, attr.Permissions);
    }

    [Fact]
    public void InitiateEmployeeExit_has_offboarding_edit_permission()
    {
        var method = typeof(UserController).GetMethod(nameof(UserController.InitiateEmployeeExit), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.Offboarding, attr.Module);
        Assert.Contains(Permission.Edit, attr.Permissions);
    }

    [Theory]
    [InlineData(nameof(WorkFromHomeController.Policy))]
    [InlineData(nameof(WorkFromHomeController.Context))]
    [InlineData(nameof(WorkFromHomeController.Mine))]
    [InlineData(nameof(WorkFromHomeController.Cancel))]
    [InlineData(nameof(WorkFromHomeController.CheckIn))]
    [InlineData(nameof(WorkFromHomeController.CheckOut))]
    [InlineData(nameof(WorkFromHomeController.Timing))]
    public void WorkFromHome_endpoints_have_wfh_module_permission(string methodName)
    {
        var method = typeof(WorkFromHomeController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.WorkFromHome, attr.Module);
    }

    [Theory]
    [InlineData(nameof(EmployeeLifecycleCaseController.Get))]
    [InlineData(nameof(EmployeeLifecycleCaseController.Summary))]
    [InlineData(nameof(EmployeeLifecycleCaseController.Create))]
    [InlineData(nameof(EmployeeLifecycleCaseController.Update))]
    public void EmployeeLifecycleCase_endpoints_have_offboarding_module_permission(string methodName)
    {
        var method = typeof(EmployeeLifecycleCaseController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.Offboarding, attr.Module);
    }
}

