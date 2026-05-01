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

public AutoPayrollController(IHttpContextAccessor httpContextAccessor,IAutoPayRollServices autoPayRollServices)
    {
                _httpContextAccessor = httpContextAccessor;

        _autoPayRollServices = autoPayRollServices;
}

[HttpPost]
[Route("GeneratePayRollMonthly")]
[ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit])]
public async Task<Result> GeneratePayRollForMonthly([FromBody] AddUpdatePayRollRequestDto model)
{
    var result = new Result();
    
    string companyId = CurrentContext.CompanyId(_httpContextAccessor);
    
    try
    {
        await _autoPayRollServices.GeneratePayrollForMonthAsync(companyId, model.PayMonth);
        
        result.Success = true;
        result.Message = $"Payroll generated successfully for {model.PayMonth:MMMM yyyy}.";
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Message = $"Error generating payroll: {ex.Message}";
    }

    return result;
}

    }