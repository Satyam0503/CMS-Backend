using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Route("api/payroll/runs/{runId}/review"),Authorize]
public sealed class PayrollRunReviewController(IPayrollRunReviewService service,IHttpContextAccessor context):ControllerBase
{
 [HttpGet,ModulePermission(AppModule.PayRoll,Permission.View)]
 public async Task<IActionResult> Get(string runId){var company=context.HttpContext?.User.FindFirst("CompanyId")?.Value;return company is null?Unauthorized():Ok(await service.Get(company,runId));}
 [HttpPost,ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Record(string runId,[FromBody]PayrollRunReviewRequestDto request){var company=context.HttpContext?.User.FindFirst("CompanyId")?.Value;var actor=context.HttpContext?.User.FindFirst("UserId")?.Value??context.HttpContext?.User.Identity?.Name;return company is null||string.IsNullOrWhiteSpace(actor)?Unauthorized():Ok(await service.Record(company,runId,actor,request));}
}
