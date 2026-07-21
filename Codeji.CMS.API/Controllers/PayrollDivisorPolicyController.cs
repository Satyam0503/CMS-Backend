using Codeji.CMS.Domain.Models;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController,Authorize,Route("api/payroll/divisor-policy")]
public class PayrollDivisorPolicyController(IPayrollDivisorPolicyService service,IHttpContextAccessor context):ControllerBase
{
    [HttpGet,ModulePermission(AppModule.PayrollSettings,Permission.View)]
    public Task<PayrollDivisorPolicyDto> Get([FromQuery]DateTime month)=>service.GetEffective(CurrentContext.CompanyId(context),month);
    [HttpPost,ModulePermission(AppModule.PayrollSettings,Permission.Edit)]
    public Task<Result> Save(PayrollDivisorPolicyDto dto)=>service.Save(CurrentContext.CompanyId(context),CurrentContext.UserId(context),dto);
}
