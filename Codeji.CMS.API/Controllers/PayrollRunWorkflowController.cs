using Codeji.CMS.API.App_Start; using Codeji.CMS.DTO.PayRoll; using Codeji.CMS.Services.PayRoll; using Codeji.CMS.Utility.Constraints; using Codeji.CMS.Utility.middlewares; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Authorize,Route("api/payroll/runs")]
public sealed class PayrollRunWorkflowController(IPayrollRunWorkflowService service,IHttpContextAccessor context):ControllerBase
{
 [HttpPost("{id}/validate"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Validate(string id)=>service.Validate(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/cancel"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Cancel(string id,[FromBody]CancelPayrollRunRequestDto request)=>service.Cancel(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context),request.Reason);
 [HttpPost("{id}/employees/{employeeId}/exclude"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> ExcludeEmployee(string id,string employeeId,[FromBody]ExcludePayrollRunEmployeeRequestDto request)=>service.ExcludeEmployee(CurrentContext.CompanyId(context),id,employeeId,CurrentContext.UserId(context),request.Reason);
 [HttpPost("{id}/employees/{employeeId}/include"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> IncludeEmployee(string id,string employeeId)=>service.IncludeEmployee(CurrentContext.CompanyId(context),id,employeeId,CurrentContext.UserId(context));
 [HttpPost("{id}/approve"),Authorize(Policy = "AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Approve)] public Task<Codeji.CMS.Domain.Models.Result> Approve(string id)=>service.Approve(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/process"),ModulePermission(AppModule.PayRoll,Permission.Approve)] public Task<Codeji.CMS.Domain.Models.Result> Process(string id)=>service.Process(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/close"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Close(string id)=>service.Close(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/reopen"),Authorize(Policy = "AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Approve)] public Task<Codeji.CMS.Domain.Models.Result> Reopen(string id,[FromBody]ReopenPayrollRunRequestDto request)=>service.Reopen(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context),request.Reason);
 [HttpPost("{id}/correction"),Authorize(Policy = "AdminOnly"),ModulePermission(AppModule.PayRoll,Permission.Approve)] public Task<Codeji.CMS.Domain.Models.Result> CreateCorrectionRun(string id,[FromBody]CreateCorrectionRunRequestDto request)=>service.CreateCorrectionRun(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context),request.Reason,request.PayDate);

}
