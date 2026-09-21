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
public sealed class RecruitmentCandidatePrivacyController(IRecruitmentCandidatePrivacyService privacyService, IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("Apply")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> Apply([FromBody] RecruitmentCandidatePrivacyCommand command) => privacyService.Apply(command, CurrentContext.UserId(context));
}
