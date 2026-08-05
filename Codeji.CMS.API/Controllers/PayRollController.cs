using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class PayRollController : BaseApiController
{
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly IPayRollServices _payRollServices;
    readonly IRoleService _roleService;
    public PayRollController(IHttpContextAccessor httpContextAccessor, IPayRollServices payRollServices, IRoleService roleService)
    {
        _httpContextAccessor = httpContextAccessor;
        _payRollServices = payRollServices;
        _roleService = roleService;
    }

    [HttpPost]
    [Route("GetEmpPayRollData")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public async Task<ActionResult<Result<GetEmpPayRollResponseDto>>> GetEmpPayRollData([FromBody] GetEmpPayRollRequestDto payload)
    {
        var requestPeriod = new DateTime(payload.PayMonth.Year, payload.PayMonth.Month, 1);
        if (!PayrollPeriodRules.IsClosedPeriod(requestPeriod, IndiaTime.Now))
        {
            return BadRequest("Payroll cannot be generated for current or future months");
        }
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var userId = CurrentContext.UserId(_httpContextAccessor);
        var canManagePayroll = await _roleService.VerifyUserAccess(
            AppModule.PayRoll, [Permission.Create, Permission.Edit], userId, companyId);
        var result = await _payRollServices.GetEmployeePayRoll(
            payload, companyId, canManagePayroll ? null : userId, processedOnly: !canManagePayroll);
        return Ok(result);
    }

    [HttpPost]
    [Route("GeneratePaySlip")]
    public async Task<ActionResult> GetSalarySlip(PayslipRequestDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        // prevent user form generating future salary slip  
        DateTime currentDate = DateTime.UtcNow;
        if (!PayrollPeriodRules.IsClosedPeriod(new DateTime(model.Year, model.Month, 1), currentDate)) return BadRequest(ModelState);
        try
        {
            var userId = CurrentContext.UserId(_httpContextAccessor);
            var (pdfByte, pdfName) = await _payRollServices.GenerateEmpSalarySlip(model, userId);
            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
            return File(pdfByte, "application/pdf", pdfName);
        }
        catch (InvalidOperationException exp)
        {
            return Conflict(new { message = exp.Message });
        }
        catch (Exception exp)
        {
            return StatusCode(500, new { message = "Error generating payslip", status = exp.Message });
        }
    }

    // used by HR/admin to preview or download another employee's payslip (e.g. from the payroll
    // table); gated by PayRoll.View since, unlike GeneratePaySlip above, the caller isn't limited
    // to their own payslip here
    [HttpPost]
    [Route("GenerateEmployeePaySlip")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public async Task<ActionResult> GetEmployeeSalarySlip(EmployeePayslipRequestDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        DateTime currentDate = DateTime.UtcNow;
        if (!PayrollPeriodRules.IsClosedPeriod(new DateTime(model.Year, model.Month, 1), currentDate)) return BadRequest(ModelState);
        try
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var (pdfByte, pdfName) = await _payRollServices.GenerateEmployeeSalarySlip(model, companyId);
            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
            return File(pdfByte, "application/pdf", pdfName);
        }
        catch (InvalidOperationException exp)
        {
            return Conflict(new { message = exp.Message });
        }
        catch (Exception exp)
        {
            return StatusCode(500, new { message = "Error generating payslip", status = exp.Message });
        }
    }

    [HttpPost]
    [Route("UploadPayloadData")]
    [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit])]
    public async Task<ActionResult<Result<string>>> UploadPayloadData([FromBody] EmplyeePayRollRequestDto model)
    {
        var currentDate = DateTime.UtcNow;
        if (!PayrollPeriodRules.IsClosedPeriod(model.PayMonth, currentDate)) return BadRequest();
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var result = await _payRollServices.UploadPayrollData(model, companyId);
        return Ok(result);
    }

    [HttpPost]
    [Route("AddUpdatePayRoll")]
    [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit])]
    public async Task<Result> AddUpdatePayRoll([FromBody] AddUpdatePayRollRequestDto model)
    {
        Result result = new Result();
        var currentDate = DateTime.UtcNow;
        if (!PayrollPeriodRules.IsClosedPeriod(model.PayMonth, currentDate)) return result;
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        result = await _payRollServices.AddUpdatePayRoll(model, companyId);
        if (result.Success)
        {
            return result;
        }
        return result;
    }

    [HttpPost]
    [Route("ProcessPayrollMonth")]
    [ModulePermission(AppModule.PayRoll, [Permission.Create, Permission.Edit])]
    public async Task<Result> ProcessPayrollMonth([FromBody] ProcessPayrollRequestDto model)
    {
        if (!PayrollPeriodRules.IsClosedPeriod(model.PayMonth, DateTime.UtcNow))
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Only a completed payroll month can be processed." };

        return await _payRollServices.ProcessPayrollMonth(
            new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1),
            CurrentContext.CompanyId(_httpContextAccessor),
            CurrentContext.UserId(_httpContextAccessor),
            model.EmployeeIds);
    }
}
