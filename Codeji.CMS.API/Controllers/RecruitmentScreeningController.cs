using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentScreeningController(IRecruitmentScreeningQueueService queueService) : BaseApiController
{
    [HttpGet("Queue")]
    [ModulePermission(AppModule.Applications, Permission.View)]
    public Task<Result<RecruitmentScreeningQueueReport>> Queue([FromQuery] string? vacancyId, [FromQuery] int overdueAfterHours = 48, [FromQuery] string? reviewerUserId = null, [FromQuery] int? minScore = null, [FromQuery] int? maxScore = null, [FromQuery] DateTime? submittedAfterUtc = null, [FromQuery] DateTime? submittedBeforeUtc = null) => queueService.Get(vacancyId, overdueAfterHours, reviewerUserId, minScore, maxScore, submittedAfterUtc, submittedBeforeUtc);
}
