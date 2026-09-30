using Codeji.CMS.Services.PayRoll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Route("api/payroll/adjustments"),Authorize]
public sealed class PayrollAdjustmentController(IPayrollAdjustmentService service,IHttpContextAccessor context):ControllerBase
{
 string? Company=>context.HttpContext?.User.FindFirst("CompanyId")?.Value; string? Actor=>context.HttpContext?.User.FindFirst("UserId")?.Value??context.HttpContext?.User.Identity?.Name;
 [HttpPost][Authorize(Policy="PayRollEdit")]
 public async Task<IActionResult> Create(CreatePayrollAdjustmentRequestDto request)=>Company is null||Actor is null?Unauthorized():Ok(await service.Create(Company,Actor,request));
 [HttpPost("{id}/approve")][Authorize(Policy="PayRollEdit")]
 public async Task<IActionResult> Approve(string id)=>Company is null||Actor is null?Unauthorized():Ok(await service.Approve(Company,Actor,id));
 [HttpPost("{id}/process")][Authorize(Policy="PayRollEdit")]
 public async Task<IActionResult> Process(string id,PayrollAdjustmentActionDto request)=>Company is null||Actor is null?Unauthorized():Ok(await service.Process(Company,Actor,id,request));
}