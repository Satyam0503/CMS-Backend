using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.Utility.Constraints;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentAuditController(IRecruitmentAuditExportService exportService) : BaseApiController
{
    [HttpGet("Export")]
    [ModulePermission(AppModule.ProcessLog, Permission.View)]
    public async Task<IActionResult> Export([FromQuery] RecruitmentAuditExportQuery query)
    {
        var result = await exportService.Export(query);
        if (!result.Success) return StatusCode(result.StatusCode == 0 ? StatusCodes.Status400BadRequest : result.StatusCode, result);
        var export = result.MethodResult;
        return File(Convert.FromBase64String(export.ContentBase64), export.ContentType, export.FileName);
    }
}

