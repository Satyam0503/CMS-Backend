using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Authorize, Route("api/payroll/reports")]
public sealed class PayrollReportsController(
    IPayrollReportsService service,
    IHttpContextAccessor context) : ControllerBase
{
    [HttpGet("summary"), ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Codeji.CMS.Domain.Models.Result<PayrollReportSummaryDto>> Summary([FromQuery] PayrollReportQueryDto query) =>
        service.GetSummary(CurrentContext.CompanyId(context), query);

    [HttpGet("register"), ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Codeji.CMS.Domain.Models.Result<List<PayrollRegisterRowDto>>> Register([FromQuery] PayrollReportQueryDto query) =>
        service.GetRegister(CurrentContext.CompanyId(context), query);

    [HttpGet("statutory/register"), ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Codeji.CMS.Domain.Models.Result<List<PayrollStatutoryRegisterRowDto>>> StatutoryRegister([FromQuery] PayrollReportQueryDto query) =>
        service.GetStatutoryRegister(CurrentContext.CompanyId(context), query);

    [HttpGet("statutory/register.csv"), ModulePermission(AppModule.PayRoll, Permission.View)]
    public async Task<IActionResult> StatutoryRegisterCsv([FromQuery] PayrollReportQueryDto query)
    {
        var result = await service.ExportStatutoryRegister(CurrentContext.CompanyId(context), query);
        if (!result.Success) return BadRequest(result);
        return File(result.MethodResult, "text/csv; charset=utf-8", "payroll-statutory-register.csv");
    }
    [HttpGet("register.csv"), ModulePermission(AppModule.PayRoll, Permission.View)]
    public async Task<IActionResult> RegisterCsv([FromQuery] PayrollReportQueryDto query)
    {
        var result = await service.ExportRegister(CurrentContext.CompanyId(context), query);
        if (!result.Success) return BadRequest(result);
        return File(result.MethodResult, "text/csv; charset=utf-8", "payroll-register.csv");
    }
}


