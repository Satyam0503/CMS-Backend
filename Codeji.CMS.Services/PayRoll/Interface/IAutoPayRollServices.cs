using Codeji.CMS.Domain.Models;

namespace Codeji.CMS.Services.PayRoll.Interface
{
    public interface IAutoPayRollServices
{
    Task<Result> GeneratePayrollForMonthAsync(string companyId, DateTime payMonth);
}

}
