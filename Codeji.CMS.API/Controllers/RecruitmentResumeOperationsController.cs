using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Route("api/RecruitmentResumeOperations"), Authorize]
public sealed class RecruitmentResumeOperationsController(IRecruitmentResumeOperationsService service) : BaseApiController
{
    [HttpGet("ScanHealth")]
    [ModulePermission(AppModule.Applications, Permission.View)]
    public Task<Result<RecruitmentResumeScanHealthDto>> GetScanHealth() => service.GetScanHealth();
}
