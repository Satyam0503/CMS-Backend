using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.Attendance;
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
    readonly IAttendancePenaltyService _attendancePenaltyService;
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly ILogger<AutoPayrollController> _logger;

    public AutoPayrollController(
        IHttpContextAccessor httpContextAccessor,
        IAutoPayRollServices autoPayRollServices,
        IAttendancePenaltyService attendancePenaltyService,
        ILogger<AutoPayrollController> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _autoPayRollServices = autoPayRollServices;
        _attendancePenaltyService = attendancePenaltyService;
        _logger = logger;
    }

    [HttpPost]
    [Route("GeneratePayRollMonthly")]
    [ModulePermission(AppModule.PayrollSettings, [Permission.Create, Permission.Edit])]
    public async Task<ActionResult> GeneratePayRollForMonthly([FromBody] ProcessPayrollRequestDto model)
    {
        var result = new Result();

        var currentPeriod = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var requestPeriod = new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1);
        if (!PayrollPeriodRules.IsClosedPeriod(requestPeriod, DateTime.UtcNow))
        {
            result.Success = false;
            result.Message = "Payroll can be processed only for a completed month.";
            return Ok(result);
        }

        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        string userId = CurrentContext.UserId(_httpContextAccessor);

        try
        {
            // Payroll generation is a finalisation boundary. Validate and lock the exact
            // selected month on the server so a browser cannot generate payroll from
            // unreviewed or mutable attendance data.
            var attendanceLock = await _attendancePenaltyService.ValidateAndLock(companyId, userId, requestPeriod);
            if (!attendanceLock.Success)
                return Ok(attendanceLock);

            result = await _autoPayRollServices.GeneratePayrollForMonthAsync(companyId, requestPeriod);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Payroll generation failed. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "PAYROLL_GENERATION_FAILED",
                message = "Unable to generate payroll. Please try again or contact support with the correlation ID.",
                correlationId
            });
        }

        return Ok(result);
    }
}
