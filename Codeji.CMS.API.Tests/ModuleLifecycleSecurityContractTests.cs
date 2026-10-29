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

    [Theory]
    [InlineData(nameof(CareerProfileController.Get))]
    [InlineData(nameof(CareerProfileController.Save))]
    [InlineData(nameof(CareerProfileController.UpdateTheme))]
    public void CareerProfile_endpoints_have_career_profile_module_permission(string methodName)
    {
        var method = typeof(CareerProfileController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.CareerProfile, attr.Module);
    }

    [Theory]
    [InlineData(nameof(UserController.SubmitOwnResignation))]
    [InlineData(nameof(UserController.GetEmployeeExitCase))]
    [InlineData(nameof(UserController.GetOwnEmployeeExitHistory))]
    [InlineData(nameof(UserController.GetEmployeeExitCaseForManagement))]
    [InlineData(nameof(UserController.GetEmployeeExitQueue))]
    [InlineData(nameof(UserController.CompleteEmployeeExit))]
    [InlineData(nameof(UserController.ReviewEmployeeExit))]
    [InlineData(nameof(UserController.RespondToEmployeeExitDiscussion))]
    [InlineData(nameof(UserController.RespondToEmployeeExitJointDiscussion))]
    [InlineData(nameof(UserController.WithdrawOwnEmployeeExit))]
    [InlineData(nameof(UserController.DecideEmployeeExitWithdrawal))]
    [InlineData(nameof(UserController.CancelEmployeeExit))]
    [InlineData(nameof(UserController.UpdateEmployeeExitDate))]
    [InlineData(nameof(UserController.RequestEmployeeExitNoticeAdjustment))]
    [InlineData(nameof(UserController.DecideEmployeeExitNoticeAdjustment))]
    [InlineData(nameof(UserController.DecideOwnEmployeeExitNoticeOffer))]
    [InlineData(nameof(UserController.UpsertEmployeeExitPolicy))]
    [InlineData(nameof(UserController.UpdateEmployeeExitPolicy))]
    [InlineData(nameof(UserController.GetEmployeeExitPolicy))]
    [InlineData(nameof(UserController.GetEmployeeExitClearanceTemplate))]
    [InlineData(nameof(UserController.GetEmployeeExitPolicies))]
    [InlineData(nameof(UserController.GetEmployeeExitClearanceTemplates))]
    [InlineData(nameof(UserController.UpsertEmployeeExitClearanceTemplate))]
    [InlineData(nameof(UserController.DeleteEmployeeExitClearanceTemplate))]
    [InlineData(nameof(UserController.ApproveEmployeeFinalSettlement))]
    [InlineData(nameof(UserController.UpdateEmployeeFinalSettlementPayment))]
    [InlineData(nameof(UserController.CalculateEmployeeFinalSettlement))]
    [InlineData(nameof(UserController.MarkEmployeeFinalSettlementPaid))]
    [InlineData(nameof(UserController.RecordEmployeeExitInterview))]
    [InlineData(nameof(UserController.UpsertEmployeeExitReassignment))]
    [InlineData(nameof(UserController.GenerateEmployeeExitDocument))]
    [InlineData(nameof(UserController.IssueEmployeeExitDocument))]
    [InlineData(nameof(UserController.CompleteEmployeeExitClearance))]
    [InlineData(nameof(UserController.UpdateEmployeeExitClearanceStatus))]
    [InlineData(nameof(UserController.WaiveEmployeeExitClearance))]
    [InlineData(nameof(UserController.UpsertEmployeeExitHandoverTask))]
    [InlineData(nameof(UserController.ReviewEmployeeExitHandoverTask))]
    [InlineData(nameof(UserController.UpsertEmployeeExitAsset))]
    [InlineData(nameof(UserController.UpsertEmployeeExitAccessItem))]
    [InlineData(nameof(UserController.UpsertEmployeeExitFinanceItem))]
    [InlineData(nameof(UserController.UpsertEmployeeExitBenefitItem))]
    [InlineData(nameof(UserController.AddEmployeeExitEvidence))]
    [InlineData(nameof(UserController.UploadEmployeeExitEvidence))]
    [InlineData(nameof(UserController.DownloadEmployeeExitEvidence))]
    [InlineData(nameof(UserController.VerifyEmployeeExitEvidence))]
    [InlineData(nameof(UserController.SupersedeEmployeeExitEvidence))]
    [InlineData(nameof(UserController.UpdateEmployeeExitPostExitContact))]
    [InlineData(nameof(UserController.AcknowledgeEmployeeExit))]
    [InlineData(nameof(UserController.CreateEmployeeExitFormerEmployeeRequest))]
    [InlineData(nameof(UserController.ResolveEmployeeExitFormerEmployeeRequest))]
    [InlineData(nameof(UserController.CreateFormerEmployeePortalInvite))]
    [InlineData(nameof(UserController.RevokeFormerEmployeePortalAccess))]
    [InlineData(nameof(UserController.SetFormerEmployeePortalAccess))]
    [InlineData(nameof(UserController.RejoinEmployee))]
    [InlineData(nameof(UserController.ExceptionalCompleteEmployeeExit))]
    public void UserController_offboarding_endpoints_have_offboarding_module_permission(string methodName)
    {
        var method = typeof(UserController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AppModule.Offboarding, attr.Module);
    }
}