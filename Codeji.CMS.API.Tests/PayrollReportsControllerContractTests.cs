using Codeji.CMS.API.Controllers;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using System.Reflection;

namespace Codeji.CMS.API.Tests;

public sealed class PayrollReportsControllerContractTests
{
    [Theory]
    [InlineData(nameof(PayrollReportsController.Summary))]
    [InlineData(nameof(PayrollReportsController.Register))]
    [InlineData(nameof(PayrollReportsController.RegisterCsv))]
    [InlineData(nameof(PayrollReportsController.StatutoryRegister))]
    [InlineData(nameof(PayrollReportsController.StatutoryRegisterCsv))]
    [InlineData(nameof(PayrollReportsController.EpfoEcrText))]
    [InlineData(nameof(PayrollReportsController.EsicReturnCsv))]
    public void Reports_require_payroll_view_permission(string methodName)
    {
        var method = typeof(PayrollReportsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.PayRoll, permission!.Module);
        Assert.Contains(Permission.View, permission.Permissions);
    }
}
