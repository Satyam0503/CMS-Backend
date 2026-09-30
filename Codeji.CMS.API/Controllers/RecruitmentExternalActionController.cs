using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class RecruitmentExternalActionController(IRecruitmentExternalActionService service, IHttpContextAccessor context) : BaseApiController
{
    [Authorize, HttpPost("Issue"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<RecruitmentExternalActionTokenDto>> Issue([FromBody] RecruitmentExternalActionIssueCommand command) => service.Issue(command, CurrentContext.UserId(context));

    [Authorize, HttpPost("QueueEmail"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> QueueEmail([FromBody] RecruitmentEmailQueueCommand command) => service.QueueEmail(command, CurrentContext.UserId(context));

    [Authorize, HttpPost("{actionTokenId}/Revoke"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Revoke(string actionTokenId) => service.Revoke(actionTokenId, CurrentContext.UserId(context));

    [AllowAnonymous, EnableRateLimiting("career-sensitive"), HttpGet("Validate")]
    public Task<Result<RecruitmentExternalActionTokenDto>> Validate([FromQuery] string token) => service.Validate(token, false);

    [AllowAnonymous, EnableRateLimiting("career-sensitive"), HttpPost("Consume")]
    public Task<Result<RecruitmentExternalActionTokenDto>> Consume([FromBody] RecruitmentExternalActionConsumeCommand command) => service.Validate(command.Token, true);
}


