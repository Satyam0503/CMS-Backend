using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentOnboardingController(IRecruitmentOnboardingService onboardingService, IHttpContextAccessor context) : BaseApiController
{
    [HttpGet("ByApplicant/{applicantId}")]
    [ModulePermission(AppModule.Applications, Permission.View)]
    public Task<Result<RecruitmentOnboardingCase>> GetByApplicant(string applicantId) => onboardingService.Get(applicantId);

    [HttpPost("Update")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Update([FromBody] RecruitmentOnboardingUpdateCommand command) =>
        onboardingService.Update(command, CurrentContext.UserId(context));
}


