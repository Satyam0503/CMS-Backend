using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class RecruitmentCandidatePrivacyPermissionContractTests
{
    [Fact]
    public void ConsentChangesRequireApplicationsEdit()
    {
        var method = GetAction(nameof(RecruitmentCandidatePrivacyController.Apply));
        var route = method.GetCustomAttribute<HttpPostAttribute>();
        var permission = Assert.Single(method.GetCustomAttributes<ModulePermissionAttribute>());

        Assert.Equal("Apply", route?.Template);
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Equal(new[] { Permission.Edit }, permission.Permissions);
    }

    [Fact]
    public void CandidateErasureRequiresApplicationsDelete()
    {
        var method = GetAction(nameof(RecruitmentCandidatePrivacyController.Anonymize));
        var route = method.GetCustomAttribute<HttpPostAttribute>();
        var permission = Assert.Single(method.GetCustomAttributes<ModulePermissionAttribute>());

        Assert.Equal("Anonymize", route?.Template);
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Equal(new[] { Permission.Delete }, permission.Permissions);
    }

    [Fact]
    public async Task ApplyEndpointRejectsAnonymizationWithoutCallingService()
    {
        var service = new Mock<IRecruitmentCandidatePrivacyService>();
        var controller = new RecruitmentCandidatePrivacyController(service.Object, new HttpContextAccessor());

        var result = await controller.Apply(new RecruitmentCandidatePrivacyCommand { Action = "Anonymize" });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        service.Verify(x => x.Apply(It.IsAny<RecruitmentCandidatePrivacyCommand>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AnonymizeEndpointRejectsConsentChangesWithoutCallingService()
    {
        var service = new Mock<IRecruitmentCandidatePrivacyService>();
        var controller = new RecruitmentCandidatePrivacyController(service.Object, new HttpContextAccessor());

        var result = await controller.Anonymize(new RecruitmentCandidatePrivacyCommand { Action = "Revoke" });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        service.Verify(x => x.Apply(It.IsAny<RecruitmentCandidatePrivacyCommand>(), It.IsAny<string>()), Times.Never);
    }

    private static MethodInfo GetAction(string name)
    {
        var method = typeof(RecruitmentCandidatePrivacyController).GetMethod(name);
        Assert.NotNull(method);
        return method!;
    }
}
