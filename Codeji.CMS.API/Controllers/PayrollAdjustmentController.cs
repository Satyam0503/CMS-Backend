using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Route("api/payroll/adjustments"),Authorize]
public sealed class PayrollAdjustmentController(IPayrollAdjustmentService service,IHttpContextAccessor context):ControllerBase
{
 string? Company=>context.HttpContext?.User.FindFirst("CompanyId")?.Value; string? Actor=>context.HttpContext?.User.FindFirst("UserId")?.Value??context.HttpContext?.User.Identity?.Name;
 [HttpGet("components"),ModulePermission(AppModule.PayRoll,Permission.View)]
 public async Task<IActionResult> GetComponents([FromQuery] DateTime effectiveDate)
 {
  if (Company is null) return Unauthorized();
  var result = await service.GetComponents(Company, effectiveDate);
  return StatusCode(result.StatusCode, result);
 }
 [HttpGet,ModulePermission(AppModule.PayRoll,Permission.View)]
 public async Task<IActionResult> GetAll()
 {
  if (Company is null) return Unauthorized();
  var result = await service.GetAll(Company);
  return StatusCode(result.StatusCode, result);
 }
 [HttpPost,ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Create(CreatePayrollAdjustmentRequestDto request)
 {
  if (Company is null || Actor is null) return Unauthorized();
  var result = await service.Create(Company, Actor, request);
  return StatusCode(result.StatusCode, result);
 }
 [HttpPost("{id}/approve"),Authorize(Policy="AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Approve)]
 public async Task<IActionResult> Approve(string id)
 {
  if (Company is null || Actor is null) return Unauthorized();
  var result = await service.Approve(Company, Actor, id);
  return StatusCode(result.StatusCode, result);
 }
 [HttpPost("{id}/reject"),Authorize(Policy="AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Approve)]
 public async Task<IActionResult> Reject(string id,RejectPayrollAdjustmentRequestDto request)
 {
  if (Company is null || Actor is null) return Unauthorized();
  var result = await service.Reject(Company, Actor, id, request);
  return StatusCode(result.StatusCode, result);
 }
 [HttpPost("{id}/process"),ModulePermission(AppModule.PayRoll,Permission.Edit)]
 public async Task<IActionResult> Process(string id,PayrollAdjustmentActionDto request)
 {
  if (Company is null || Actor is null) return Unauthorized();
  var result = await service.Process(Company, Actor, id, request);
  return StatusCode(result.StatusCode, result);
 }
}
