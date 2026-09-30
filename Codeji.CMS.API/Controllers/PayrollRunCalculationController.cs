using Codeji.CMS.API.App_Start; using Codeji.CMS.Services.PayRoll; using Codeji.CMS.Utility.Constraints; using Codeji.CMS.Utility.middlewares; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Authorize,Route("api/payroll/runs")]
public sealed class PayrollRunCalculationController(IPayrollRunCalculationService service,IHttpContextAccessor context):ControllerBase
{
 [HttpPost("{id}/calculate"),ModulePermission(AppModule.PayRoll,Permission.Edit)] public Task<Codeji.CMS.Domain.Models.Result> Calculate(string id)=>service.SyncCalculatedRows(CurrentContext.CompanyId(context),id,CurrentContext.UserId(context));
}
