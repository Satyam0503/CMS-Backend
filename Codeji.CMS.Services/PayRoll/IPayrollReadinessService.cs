using Codeji.CMS.DTO.PayRoll;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollReadinessService
{
    Task<PayrollReadinessReportDto> ValidateCompanyPayrollReadiness(string companyId, DateTime targetMonth);
}
