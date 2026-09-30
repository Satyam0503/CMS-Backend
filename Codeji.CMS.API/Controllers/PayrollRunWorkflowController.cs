using Codeji.CMS.API.App_Start; using Codeji.CMS.Services.PayRoll; using Codeji.CMS.Utility.Constraints; using Codeji.CMS.Utility.middlewares; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Authorize,Route("api/payroll/runs")]
public sealed class PayrollRunWorkflowController(IPayrollRunWorkflowService service,IHttpContextAccessor context):ControllerBase
{
 [HttpPost("{id}/validate"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Validate(string id)=>service.Validate(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/approve"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Approve(string id)=>service.Approve(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/process"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Process(string id)=>service.Process(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
 [HttpPost("{id}/close"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Close(string id)=>service.Close(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
}
