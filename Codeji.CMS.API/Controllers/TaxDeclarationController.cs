using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
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
    public async Task<ActionResult<Result<EmployeeTaxDeclarationDto>>> GetDeclaration(
        string employeeId,
        string financialYear,
        [FromServices] IRoleService roleService,
        [FromServices] IMongoDbRepository<EmpUser> employeeRepo)
    {
        var companyId = CurrentContext.CompanyId(context);
        var currentUserId = CurrentContext.UserId(context);

        var currentEmployee = await employeeRepo.FirstOrDefault(x =>
            x.CompanyId == companyId && x.UserId == currentUserId && !x.IsDeleted);

        bool isSelf = (currentEmployee != null && string.Equals(currentEmployee.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
            || string.Equals(currentUserId, employeeId, StringComparison.OrdinalIgnoreCase);

        if (!isSelf)
        {
            var hasEditAccess = await roleService.VerifyUserAccess(
                AppModule.PayRoll, [Permission.Edit], currentUserId, companyId);
            if (!hasEditAccess)
            {
                return Forbid();
            }
        }

        var result = await service.GetDeclaration(companyId, employeeId, financialYear);
        return Ok(result);
    }

    [HttpPost("employee/{employeeId}/submit")]
    [ModulePermission(AppModule.PayRoll, Permission.View, Permission.Edit)]
    public async Task<ActionResult<Result<EmployeeTaxDeclarationDto>>> SubmitDeclaration(
        string employeeId,
        [FromBody] SubmitTaxDeclarationRequestDto request,
        [FromServices] IRoleService roleService,
        [FromServices] IMongoDbRepository<EmpUser> employeeRepo)
    {
        var currentUserId = CurrentContext.UserId(context);
        var companyId = CurrentContext.CompanyId(context);

        var currentEmployee = await employeeRepo.FirstOrDefault(x =>
            x.CompanyId == companyId && x.UserId == currentUserId && !x.IsDeleted);

        bool isSelf = (currentEmployee != null && string.Equals(currentEmployee.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
            || string.Equals(currentUserId, employeeId, StringComparison.OrdinalIgnoreCase);

        if (!isSelf)
        {
            var hasEditAccess = await roleService.VerifyUserAccess(
                AppModule.PayRoll, [Permission.Edit], currentUserId, companyId);
            if (!hasEditAccess)
            {
                return Forbid();
            }
        }

        var result = await service.SubmitDeclaration(companyId, employeeId, request, currentUserId);
        return Ok(result);
    }

    [HttpPost("{declarationId}/review")]
    [ModulePermission(AppModule.PayRoll, Permission.Approve)]
    public Task<Result<EmployeeTaxDeclarationDto>> ReviewDeclaration(
        string declarationId,
        [FromBody] ReviewTaxDeclarationRequestDto request) =>
        service.ReviewDeclaration(CurrentContext.CompanyId(context), declarationId, request, CurrentContext.UserId(context));

    [HttpGet("company/{financialYear}")]
    [ModulePermission(AppModule.PayRoll, Permission.View)]
    public Task<Result<List<EmployeeTaxDeclarationDto>>> GetCompanyDeclarations(
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
