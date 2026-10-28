using System.Threading.Tasks;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Authorize, Route("api/payroll/tax-declarations")]
public sealed class TaxDeclarationController(
    ITaxDeclarationService service,
    IHttpContextAccessor context) : ControllerBase
{
    [HttpGet("employee/{employeeId}/year/{financialYear}")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Result<EmployeeTaxDeclarationDto>> GetDeclaration(string employeeId, string financialYear) =>
        service.GetDeclaration(CurrentContext.CompanyId(context), employeeId, financialYear);

    [HttpPost("employee/{employeeId}/submit")]
    [ModulePermission(AppModule.PayRoll, Permission.Edit)]
    public Task<Result<EmployeeTaxDeclarationDto>> SubmitDeclaration(
        string employeeId,
        [FromBody] SubmitTaxDeclarationRequestDto request) =>
        service.SubmitDeclaration(CurrentContext.CompanyId(context), employeeId, request, CurrentContext.UserId(context));

    [HttpPost("{declarationId}/review")]
    [ModulePermission(AppModule.PayRoll, Permission.Approve)]
    public Task<Result<EmployeeTaxDeclarationDto>> ReviewDeclaration(
        string declarationId,
        [FromBody] ReviewTaxDeclarationRequestDto request) =>
        service.ReviewDeclaration(CurrentContext.CompanyId(context), declarationId, request, CurrentContext.UserId(context));

    [HttpGet("company/{financialYear}")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Result<System.Collections.Generic.List<EmployeeTaxDeclarationDto>>> GetCompanyDeclarations(
        string financialYear,
        [FromQuery] string? status = null) =>
        service.GetCompanyDeclarations(CurrentContext.CompanyId(context), financialYear, status);

    [HttpPost("employee/{employeeId}/projection")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Result<TaxProjectionResultDto>> CalculateTaxProjection(
        string employeeId,
        [FromBody] SubmitTaxDeclarationRequestDto request) =>
        service.CalculateTaxProjection(CurrentContext.CompanyId(context), employeeId, request.FinancialYear, request.PreviousEmployerGrossSalary);
}
