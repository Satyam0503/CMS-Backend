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
    readonly IPayrollDivisorPolicyService _divisorPolicyService;
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly ILogger<AutoPayrollController> _logger;

    public AutoPayrollController(
        IHttpContextAccessor httpContextAccessor,
        IAutoPayRollServices autoPayRollServices,
        IAttendancePenaltyService attendancePenaltyService,
        IPayrollDivisorPolicyService divisorPolicyService,
        ILogger<AutoPayrollController> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _autoPayRollServices = autoPayRollServices;
        _attendancePenaltyService = attendancePenaltyService;
        _divisorPolicyService = divisorPolicyService;
        _logger = logger;
    }

    [HttpPost]
    [Route("GeneratePayRollMonthly")]
    [ModulePermission(AppModule.PayrollSettings, [Permission.Create, Permission.Edit])]
    public async Task<ActionResult> GeneratePayRollForMonthly([FromBody] ProcessPayrollRequestDto model)
    {
        var result = new Result();

        var requestPeriod = new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1);
        if (PayrollPeriodRules.IsFuturePeriod(requestPeriod, DateTime.UtcNow))
        {
            result.Success = false;
            result.StatusCode = StatusCodes.Status422UnprocessableEntity;
            result.ErrorCode = "PAYROLL_PERIOD_FUTURE";
            result.Message = "Payroll cannot be processed for a future month.";
            return UnprocessableEntity(result);
        }

        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        string userId = CurrentContext.UserId(_httpContextAccessor);

        try
        {
            var divisorSetting = await _divisorPolicyService.GetEffective(companyId, requestPeriod);
            if (divisorSetting.RequireAttendanceLock)
            {
                // Payroll generation is a finalisation boundary when attendance lock is required.
                // Validate and lock the exact selected month on the server so unreviewed or
                // mutable attendance data cannot generate payroll.
                var attendanceLock = await _attendancePenaltyService.ValidateAndLock(companyId, userId, requestPeriod);
                if (!attendanceLock.Success)
                    return StatusCode(attendanceLock.StatusCode >= 400 ? attendanceLock.StatusCode : 409, attendanceLock);
            }

            result = await _autoPayRollServices.GeneratePayrollForMonthAsync(companyId, requestPeriod, model.EmployeeIds);
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

        return result.Success ? Ok(result) : StatusCode(result.StatusCode >= 400 ? result.StatusCode : 409, result);
    }
}
