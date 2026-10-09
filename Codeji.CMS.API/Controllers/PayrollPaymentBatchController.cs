using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Route("api/payroll/payment-batches"),Authorize]
public sealed class PayrollPaymentBatchController(IPayrollPaymentBatchService service,IHttpContextAccessor context):ControllerBase
{
 string? Company=>context.HttpContext?.User.FindFirst("CompanyId")?.Value; string? Actor=>context.HttpContext?.User.FindFirst("UserId")?.Value??context.HttpContext?.User.Identity?.Name;
 [HttpGet("from-run/{runId}"),ModulePermission(AppModule.PayRoll,Permission.View)]
 public async Task<IActionResult> Get(string runId){return Company is null?Unauthorized():Ok(await service.GetByRun(Company,runId));}
 [HttpPost("from-run/{runId}"),Authorize(Policy="AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Create(string runId){return Company is null||Actor is null?Unauthorized():Ok(await service.Create(Company,runId,Actor));}
 [HttpPost("{id}/submit"),Authorize(Policy="AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Submit(string id,[FromBody]PaymentBatchRequestDto request){return Company is null||Actor is null?Unauthorized():Ok(await service.Submit(Company,id,Actor,request));}
 [HttpPost("{id}/reconcile"),Authorize(Policy="AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Reconcile(string id,[FromBody]PaymentBatchRequestDto request){return Company is null||Actor is null?Unauthorized():Ok(await service.Reconcile(Company,id,Actor,request));}
}
