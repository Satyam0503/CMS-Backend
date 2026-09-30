using System.Reflection;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Tests;

public sealed class PayrollControllerContractTests
{
    [Theory]
    [InlineData(nameof(PayRollController.RecordPayrollPayment))]
    [InlineData(nameof(PayRollController.ClosePayrollMonth))]
    [InlineData(nameof(PayRollController.ValidatePayrollData))]
    [InlineData(nameof(PayRollController.ApprovePayrollMonth))]
    public void Payroll_mutation_endpoints_require_payroll_create_or_edit(string methodName)
    {
        var controller = typeof(PayRollController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.PayRoll, permission.Module);
        Assert.Contains(Permission.Create, permission.Permissions);
        Assert.Contains(Permission.Edit, permission.Permissions);
        Assert.NotEmpty(method.GetCustomAttributes<HttpPostAttribute>());
    }
    [Fact]
    public void Payroll_audit_history_requires_payroll_view_permission_and_post()
    {
        var method = typeof(PayRollController).GetMethod(nameof(PayRollController.GetPayrollAuditHistory), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.PayRoll, permission.Module);
        Assert.Contains(Permission.View, permission.Permissions);
        Assert.NotEmpty(method.GetCustomAttributes<HttpPostAttribute>());
    }}


