using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Codeji.CMS.API.Tests;

public sealed class PayrollConfigurationControllerContractTests
{
    [Theory]
    [InlineData("GetComponents")]
    [InlineData("GetPayGroups")]
    [InlineData("GetPolicies")]
    [InlineData("GetTemplates")]
    [InlineData("GetAssignments")]
    [InlineData("GetEmployeeSalaryImportBatch")]
    [InlineData("GetPendingEmployeeSalaries")]
    public void Configuration_reads_require_payroll_settings_view(string methodName)
    {
        var method = typeof(PayrollConfigurationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.View, permission.Permissions);
    }

    [Theory]
    [InlineData("SaveComponent")]
    [InlineData("SavePayGroup")]
    [InlineData("SaveTemplate")]
    [InlineData("RetireTemplate")]
    [InlineData("SavePolicy")]
    [InlineData("SaveAssignment")]
    [InlineData("EndAssignment")]
    [InlineData("PreviewBulkAssignments")]
    [InlineData("SubmitBulkAssignments")]
    [InlineData("PreviewEmployeeSalaryPdf")]
    public void Configuration_writes_require_payroll_settings_edit(string methodName)
    {
        var method = typeof(PayrollConfigurationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }

    [Fact]
    public void Assignment_approval_requires_payroll_settings_approve_not_edit()
    {
        var method = typeof(PayrollConfigurationController).GetMethod("ApproveAssignment", BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        var authorization = method?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.Approve, permission.Permissions);
        Assert.DoesNotContain(Permission.Edit, permission.Permissions);
        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
    }

    [Fact]
    public void Statutory_policy_approval_requires_company_administrator_and_payroll_approve_permission()
    {
        var method = typeof(PayrollConfigurationController).GetMethod("ApprovePolicy", BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        var authorization = method?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.Approve, permission.Permissions);
        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
    }

    [Theory]
    [InlineData("DeletePolicyDraft")]
    [InlineData("DeletePayGroup")]
    [InlineData("DeleteTemplate")]
    [InlineData("DeleteAssignment")]
    public void Configuration_deletes_require_payroll_settings_delete(string methodName)
    {
        var method = typeof(PayrollConfigurationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        var route = method?.GetCustomAttribute<HttpDeleteAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.Delete, permission.Permissions);
        Assert.NotNull(route);
    }

    [Fact]
    public void Pending_salary_queue_is_tenant_permission_gated_for_view()
    {
        var method = typeof(PayrollConfigurationController).GetMethod("GetPendingEmployeeSalaries", BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.View, permission.Permissions);
    }

    [Fact]
    public void Pdf_salary_preview_accepts_optional_onboarding_employee_and_effective_date_context()
    {
        var parameters = typeof(PayrollConfigurationController)
            .GetMethod("PreviewEmployeeSalaryPdf", BindingFlags.Instance | BindingFlags.Public)!
            .GetParameters();

        var employeeId = Assert.Single(parameters, parameter => parameter.Name == "employeeId");
        var effectiveFrom = Assert.Single(parameters, parameter => parameter.Name == "effectiveFrom");
        Assert.NotNull(employeeId.GetCustomAttribute<FromFormAttribute>());
        Assert.NotNull(effectiveFrom.GetCustomAttribute<FromFormAttribute>());
        Assert.True(Nullable.GetUnderlyingType(effectiveFrom.ParameterType) == typeof(DateTime));
    }

    [Fact]
    public void Employee_salary_approval_requires_admin_and_payroll_settings_approve()
    {
        var method = typeof(PayrollConfigurationController).GetMethod("ApproveEmployeeSalary", BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();
        var authorization = method?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayrollSettings, permission!.Module);
        Assert.Contains(Permission.Approve, permission.Permissions);
        Assert.DoesNotContain(Permission.Edit, permission.Permissions);
        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization!.Policy);
    }
}
