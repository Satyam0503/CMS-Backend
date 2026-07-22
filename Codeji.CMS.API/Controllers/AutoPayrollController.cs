using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AutoPayrollController : BaseApiController
{
    readonly IAutoPayRollServices _autoPayRollServices;
    readonly IHttpContextAccessor _httpContextAccessor;

    public AutoPayrollController(IHttpContextAccessor httpContextAccessor, IAutoPayRollServices autoPayRollServices)
    {
        _httpContextAccessor = httpContextAccessor;
        _autoPayRollServices = autoPayRollServices;
    }

    [HttpPost]
    [Route("GeneratePayRollMonthly")]
    [ModulePermission(AppModule.PayrollSettings, [Permission.Create, Permission.Edit])]
    public async Task<Result> GeneratePayRollForMonthly([FromBody] ProcessPayrollRequestDto model)
    {
        var result = new Result();

        var currentPeriod = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var requestPeriod = new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1);
        if (!PayrollPeriodRules.IsClosedPeriod(requestPeriod, DateTime.UtcNow))
        {
            result.Success = false;
            result.Message = "Payroll can be processed only for a completed month.";
            return result;
        }

        string companyId = CurrentContext.CompanyId(_httpContextAccessor);

        try
        {
            result = await _autoPayRollServices.GeneratePayrollForMonthAsync(companyId, model.PayMonth);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error processing payroll: {ex.Message}";
        }

        return result;
    }
}
