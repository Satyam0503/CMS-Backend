using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayRollController : BaseApiController
{
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly IPayRollServices _payRollServices;
    public PayRollController(IHttpContextAccessor httpContextAccessor, IPayRollServices payRollServices)
    {
        _httpContextAccessor = httpContextAccessor;
        _payRollServices = payRollServices;
    }

    [HttpPost]
    [Route("GetEmpPayRollData")]
    public async Task<ActionResult<Result<GetEmpPayRollResponseDto>>> GetEmpPayRollData([FromBody] GetEmpPayRollRequestDto payload)
    {
        var requestPeriod = new DateTime(payload.PayMonth.Year, payload.PayMonth.Month, 1);
        var currentPeriod = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        if (requestPeriod >= currentPeriod)
        {
            return BadRequest("Payroll cannot be generated for current or future months");
        }
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var result = await _payRollServices.GetEmployeePayRoll(payload, companyId);
        return Ok(result);
    }

    // route to generate salary slip of employee
    [HttpPost]
    [Route("GeneratePaySlip")]
    public async Task<ActionResult> GetSalarySlip(PayslipRequestDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        // prevent user form generating future salary slip  
        DateTime currentDate = DateTime.UtcNow;
        if (model.Month >= currentDate.Month && model.Year >= currentDate.Year) return BadRequest(ModelState);
        try
        {
            var userId = CurrentContext.UserId(_httpContextAccessor);
            var (pdfByte, pdfName) = await _payRollServices.GenerateEmpSalarySlip(model, userId);
            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
            return File(pdfByte, "application/pdf", pdfName);
        }
        catch (Exception exp)
        {
            return StatusCode(500, new { message = "Error generating payslip", status = exp.Message });
        }
    }

    [HttpPost]
    [Route("UploadPayloadData")]
    public async Task<ActionResult> UploadPayloadData([FromBody] EmplyeePayRollRequestDto model)
    {
        var currentDate = DateTime.UtcNow;
        if (model.PayMonth.Month >= currentDate.Month && model.PayMonth.Year >= currentDate.Year) return BadRequest();
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var result = await _payRollServices.UploadPayrollData(model, companyId);
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest();
    }
}