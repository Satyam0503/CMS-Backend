using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class RecruitmentOfferNegotiationController(IRecruitmentOfferNegotiationService service, IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("CreateVersion"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> CreateVersion([FromBody] RecruitmentOfferVersionCommand command) => service.CreateVersion(command, CurrentContext.UserId(context));
    [HttpPost("RequestCounter"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> RequestCounter([FromBody] RecruitmentOfferCounterCommand command) => service.RequestCounter(command, CurrentContext.UserId(context));
    [HttpPost("DecideCounter"), ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> DecideCounter([FromBody] RecruitmentOfferCounterDecisionCommand command) => service.DecideCounter(command, CurrentContext.UserId(context));
}
