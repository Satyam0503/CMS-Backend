using System.Reflection;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class RecruitmentSecurityMetadataTests
{
    [Fact]
    public void Internal_recruitment_controllers_require_authorization_and_module_permissions()
    {
        var controllers = new[]
        {
            typeof(ApplicantsController),
            typeof(JobVacancyController),
            typeof(RecruitmentApplicationController),
            typeof(RecruitmentAuditController),
            typeof(RecruitmentCandidatePrivacyController),
            typeof(RecruitmentCandidateShareController),
            typeof(RecruitmentDuplicateController),
            typeof(RecruitmentEnterpriseController),
            typeof(RecruitmentHiringDecisionController),
            typeof(RecruitmentOnboardingController),
            typeof(RecruitmentOfferNegotiationController),
            typeof(RecruitmentPlanningController),
            typeof(RecruitmentResumeExtractionController),
            typeof(RecruitmentScreeningController),
            typeof(RecruitmentExternalActionController)
        };

        foreach (var controller in controllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.DeclaringType == controller && method.GetCustomAttributes<HttpMethodAttribute>().Any())
                .ToArray();
            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                if (action.GetCustomAttribute<AllowAnonymousAttribute>() is not null) continue;
                Assert.NotEmpty(action.GetCustomAttributes<ModulePermissionAttribute>());
            }
        }
    }

    [Fact]
    public void Candidate_action_controller_is_explicitly_anonymous()
    {
        var controller = typeof(RecruitmentCandidateActionController);
        Assert.NotNull(controller.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.All(controller.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.DeclaringType == controller), method => Assert.NotEmpty(method.GetCustomAttributes<HttpPostAttribute>()));
    }
}

