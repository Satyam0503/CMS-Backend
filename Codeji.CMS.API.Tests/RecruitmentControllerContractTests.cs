using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
namespace Codeji.CMS.API.Tests;
public sealed class RecruitmentControllerContractTests
{
    [Theory]
    [InlineData(nameof(ApplicantsController.HandoffToPreboarding))]
    [InlineData(nameof(ApplicantsController.UpsertInterview))]
    [InlineData(nameof(ApplicantsController.UpsertOffer))]
    public void Lifecycle_endpoint_requires_authentication_and_applications_edit(string methodName)
    {
        var controller = typeof(ApplicantsController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
    [Fact]
    public void Application_backfill_endpoint_requires_authentication_and_applications_edit()
    {
        var controller = typeof(RecruitmentApplicationController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(nameof(RecruitmentApplicationController.BackfillLegacy), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
        [Fact]
    public void Requisition_endpoint_requires_authentication_and_jobs_edit()
    {
        var controller = typeof(RecruitmentPlanningController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(nameof(RecruitmentPlanningController.UpsertRequisition), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Jobs, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
    [Fact]
    public void Scorecard_endpoint_requires_applications_edit()
    {
        var method = typeof(RecruitmentPlanningController).GetMethod(nameof(RecruitmentPlanningController.UpsertScorecard), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
    [Fact]
    public void Talent_pool_endpoint_requires_applications_edit()
    {
        var method = typeof(RecruitmentPlanningController).GetMethod(nameof(RecruitmentPlanningController.UpsertTalentPoolMember), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
    [Theory]
    [InlineData(nameof(RecruitmentEnterpriseController.MergeApplicants), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentEnterpriseController.Analytics), AppModule.Applications, Permission.View)]
    [InlineData(nameof(RecruitmentEnterpriseController.UpsertSlaPolicy), AppModule.Jobs, Permission.Edit)]
    [InlineData(nameof(RecruitmentEnterpriseController.UpsertConfig), AppModule.Jobs, Permission.Edit)]
    [InlineData(nameof(RecruitmentEnterpriseController.Config), AppModule.Jobs, Permission.View)]
    [InlineData(nameof(RecruitmentEnterpriseController.LinkRelationship), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentEnterpriseController.QueueAutomation), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentExternalActionController.Issue), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentExternalActionController.QueueEmail), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentExternalActionController.Revoke), AppModule.Applications, Permission.Edit)]
    public void Enterprise_endpoints_require_permissions(string methodName, string module, string permissionValue)
    {
        var controller = methodName is nameof(RecruitmentExternalActionController.Issue) or nameof(RecruitmentExternalActionController.QueueEmail) or nameof(RecruitmentExternalActionController.Revoke) ? typeof(RecruitmentExternalActionController) : typeof(RecruitmentEnterpriseController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(module, attribute.Module);
        Assert.Contains(permissionValue, attribute.Permissions);
    }







    [Fact]
    public void Bulk_screening_endpoint_requires_applications_edit()
    {
        var controller = typeof(RecruitmentPlanningController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(nameof(RecruitmentPlanningController.BulkScreening), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
}
public sealed class RecruitmentResumeUploadContractTests
{
    [Fact]
    public void Legacy_email_upload_is_explicitly_retired()
    {
        var method = typeof(ApplicantsController).GetMethod(nameof(ApplicantsController.UploadResume), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ObsoleteAttribute>());
        Assert.Equal("UploadResume", method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>()?.Template);
    }

    [Fact]
    public void Applicant_bound_upload_requires_applications_edit()
    {
        var method = typeof(ApplicantsController).GetMethod(nameof(ApplicantsController.UploadResumeForApplicant), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
}
public sealed class RecruitmentCandidateActionContractTests
{
    [Theory]
    [InlineData(nameof(RecruitmentCandidateActionController.RespondToOffer))]
    [InlineData(nameof(RecruitmentCandidateActionController.ConfirmInterview))]
    public void Candidate_email_actions_are_anonymous_and_explicit(string methodName)
    {
        var controller = typeof(RecruitmentCandidateActionController);
        Assert.NotNull(controller.GetCustomAttribute<AllowAnonymousAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Single());
    }
}





public sealed class RecruitmentOnboardingContractTests
{
    [Fact]
    public void Retry_activation_requires_applications_edit()
    {
        var controller = typeof(RecruitmentOnboardingController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(nameof(RecruitmentOnboardingController.RetryActivation), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.Edit, permission.Permissions);
    }
}

public sealed class RecruitmentCandidateShareContractTests
{
    [Theory]
    [InlineData(nameof(RecruitmentCandidateShareController.Grant), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentCandidateShareController.Revoke), AppModule.Applications, Permission.Edit)]
    [InlineData(nameof(RecruitmentCandidateShareController.ByApplicant), AppModule.Applications, Permission.View)]
    public void Candidate_share_endpoints_are_authenticated_and_permissioned(string methodName, string module, string permissionValue)
    {
        var controller = typeof(RecruitmentCandidateShareController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(module, permission.Module);
        Assert.Contains(permissionValue, permission.Permissions);
    }
    [Theory]
    [InlineData(nameof(RecruitmentApplicationController.UpsertStage), Permission.Edit)]
    [InlineData(nameof(RecruitmentApplicationController.GetHistory), Permission.View)]
    [InlineData(nameof(RecruitmentApplicationController.BackfillLegacy), Permission.Edit)]
    public void Application_stage_endpoints_require_expected_permissions(string methodName, string permissionValue)
    {
        var controller = typeof(RecruitmentApplicationController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var permission = method!.GetCustomAttributes<ModulePermissionAttribute>().Single();
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(permissionValue, permission.Permissions);
    }
}
