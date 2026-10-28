using System.Collections.Generic;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;

namespace Codeji.CMS.Services.PayRoll;

public interface ITaxDeclarationService
{
    Task<Result<EmployeeTaxDeclarationDto>> GetDeclaration(string companyId, string employeeId, string financialYear);
    Task<Result<EmployeeTaxDeclarationDto>> SubmitDeclaration(string companyId, string employeeId, SubmitTaxDeclarationRequestDto request, string actorId);
    Task<Result<EmployeeTaxDeclarationDto>> ReviewDeclaration(string companyId, string declarationId, ReviewTaxDeclarationRequestDto request, string reviewerId);
    Task<Result<List<EmployeeTaxDeclarationDto>>> GetCompanyDeclarations(string companyId, string financialYear, string? status = null);
    Task<Result<TaxProjectionResultDto>> CalculateTaxProjection(string companyId, string employeeId, string financialYear, decimal annualGrossSalary);
}
