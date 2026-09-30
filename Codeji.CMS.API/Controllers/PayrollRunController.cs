using Codeji.CMS.API.App_Start; using Codeji.CMS.DTO.PayRoll; using Codeji.CMS.Services.PayRoll; using Codeji.CMS.Utility.Constraints; using Codeji.CMS.Utility.middlewares; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController,Authorize,Route("api/payroll/runs")]
public sealed class PayrollRunController(IPayrollRunService service,IHttpContextAccessor context):ControllerBase
{
 [HttpPost,ModulePermission(AppModule.PayrollSettings,Permission.Create)] public Task<Codeji.CMS.Domain.Models.Result<PayrollRunDto>> Create(CreatePayrollRunRequestDto dto)=>service.Create(CurrentContext.CompanyId(context),CurrentContext.UserId(context),dto);
 [HttpGet,ModulePermission(AppModule.PayRoll,Permission.View)] public Task<List<PayrollRunDto>> List()=>service.List(CurrentContext.CompanyId(context));
 [HttpGet("{id}"),ModulePermission(AppModule.PayRoll,Permission.View)] public Task<Codeji.CMS.Domain.Models.Result<PayrollRunDto>> Get(string id)=>service.Get(CurrentContext.CompanyId(context),id);
 [HttpGet("{id}/employees"),ModulePermission(AppModule.PayRoll,Permission.View)] public Task<Codeji.CMS.Domain.Models.Result<List<PayrollRunEmployeeDto>>> Employees(string id)=>service.GetEmployees(CurrentContext.CompanyId(context),id);
}
