using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Codeji.CMS.API.Tests;

public sealed class PayrollWorkflowSecurityTests
{
    [Theory]
    [InlineData(typeof(PayrollAdjustmentController), "GetAll", Permission.View)]
    [InlineData(typeof(PayrollAdjustmentController), "GetComponents", Permission.View)]
    [InlineData(typeof(PayrollAdjustmentController), "Create", Permission.Edit)]
    [InlineData(typeof(PayrollAdjustmentController), "Approve", Permission.Approve)]
    [InlineData(typeof(PayrollAdjustmentController), "Reject", Permission.Approve)]
    [InlineData(typeof(PayrollAdjustmentController), "Process", Permission.Edit)]
    [InlineData(typeof(PayrollRunReviewController), "Get", Permission.View)]
    [InlineData(typeof(PayrollRunReviewController), "Record", Permission.Edit)]
    [InlineData(typeof(PayrollRunWorkflowController), "Cancel", Permission.Edit)]
    [InlineData(typeof(PayrollRunWorkflowController), "Approve", Permission.Approve)]
    [InlineData(typeof(PayrollPaymentBatchController), "Get", Permission.View)]
    [InlineData(typeof(PayrollPaymentBatchController), "Create", Permission.Edit)]
    [InlineData(typeof(PayrollPaymentBatchController), "Submit", Permission.Edit)]
    [InlineData(typeof(PayrollPaymentBatchController), "Reconcile", Permission.Edit)]
    public void Payroll_workflow_action_has_the_expected_module_permission(Type controllerType, string methodName, string permissionName)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = Assert.Single(method!.GetCustomAttributes<ModulePermissionAttribute>());
        Assert.Equal(AppModule.PayRoll, permission.Module);
        Assert.Contains(permissionName, permission.Permissions);
        Assert.NotEmpty(method.GetCustomAttributes<HttpMethodAttribute>());
    }

    [Fact]
    public void Payroll_adjustment_approval_requires_company_admin_and_payroll_approve_permission()
    {
        var method = typeof(PayrollAdjustmentController).GetMethod("Approve", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttribute<ModulePermissionAttribute>();
        var authorization = method.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayRoll, permission!.Module);
        Assert.Contains(Permission.Approve, permission.Permissions);
        Assert.DoesNotContain(Permission.Edit, permission.Permissions);
        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
    }

    [Fact]
    public void Payroll_adjustment_rejection_requires_company_admin_and_payroll_approve_permission()
    {
        var method = typeof(PayrollAdjustmentController).GetMethod("Reject", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttribute<ModulePermissionAttribute>();
        var authorization = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayRoll, permission!.Module);
        Assert.Contains(Permission.Approve, permission.Permissions);
        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Submit")]
    [InlineData("Reconcile")]
    public void Payroll_payment_mutations_require_company_admin(string methodName)
    {
        var method = typeof(PayrollPaymentBatchController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var authorization = method!.GetCustomAttribute<AuthorizeAttribute>();
        var permission = method.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayRoll, permission!.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
}
