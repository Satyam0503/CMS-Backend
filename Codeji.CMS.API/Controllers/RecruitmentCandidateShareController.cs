using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentCandidateShareController(IRecruitmentCandidateShareService shares, IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("Grant")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Grant([FromBody] RecruitmentCandidateShareCommand command) => shares.Grant(command, CurrentContext.UserId(context));
    [HttpPost("Revoke/{shareId}")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Revoke(string shareId) => shares.Revoke(shareId, CurrentContext.UserId(context));
    [HttpGet("ByApplicant/{applicantId}")]
    [ModulePermission(AppModule.Applications, Permission.View)]
    public Task<Result<RecruitmentCandidateShareItem>> ByApplicant(string applicantId) => shares.GetForApplicant(applicantId);
}
