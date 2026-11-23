using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentCandidatePrivacyController(IRecruitmentCandidatePrivacyService privacyService, IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("Apply")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Apply([FromBody] RecruitmentCandidatePrivacyCommand command)
    {
        if (string.Equals(command.Action?.Trim(), "Anonymize", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Failure("Use the Anonymize endpoint; erasing a candidate requires Applications:Delete permission."));
        return privacyService.Apply(command, CurrentContext.UserId(context));
    }

    [HttpPost("Anonymize")]
    [ModulePermission(AppModule.Applications, Permission.Delete)]
    public Task<Result<string>> Anonymize([FromBody] RecruitmentCandidatePrivacyCommand command)
    {
        if (!string.Equals(command.Action?.Trim(), "Anonymize", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Failure("The Anonymize endpoint only accepts the Anonymize action."));
        return privacyService.Apply(command, CurrentContext.UserId(context));
    }

    private static Result<string> Failure(string message) => new()
    {
        Success = false,
        StatusCode = StatusCodes.Status400BadRequest,
        ErrorCode = RecruitmentErrorCodes.InvalidRequest,
        Message = message
    };
}
