using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentApplicationController(IRecruitmentApplicationService service, IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("UpsertStage")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> UpsertStage([FromBody] RecruitmentApplicationStageCommand command) => service.UpsertStage(command, CurrentContext.UserId(context));

    [HttpGet("{applicationId}/History")]
    [ModulePermission(AppModule.Applications, Permission.View)]
    public Task<Result<List<RecruitmentApplicationStageEvent>>> GetHistory(string applicationId) => service.GetHistory(applicationId);

    [HttpPost("BackfillLegacy")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<int>> BackfillLegacy() => service.BackfillLegacy(CurrentContext.UserId(context));
}

