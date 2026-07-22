using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.Services.Interfaces
{
    public interface ISalaryService
    {
        Task<SalaryModel> CreateSalaryAsync(string companyId, CreateSalaryDto dto);
        Task<SalaryResponseDto?> GetActiveSalaryAsync(string companyId, Guid userId);
        Task<List<SalaryResponseDto>> GetSalaryHistoryAsync(string companyId, Guid userId);
        Task<List<CompanySalaryResponseDto>> GetCompanySalariesAsync(string companyId, string? employeeName);
    }
}
