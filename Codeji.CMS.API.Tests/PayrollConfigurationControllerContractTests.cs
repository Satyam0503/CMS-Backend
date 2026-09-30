using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
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
    [InlineData("SavePolicy")]
    [InlineData("SaveAssignment")]
    [InlineData("ApproveAssignment")]
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
}
