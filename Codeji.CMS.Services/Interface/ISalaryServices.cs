using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.Services.Interfaces
{
    public interface ISalaryService
    {
        Task<SalaryModel> CreateSalaryAsync(CreateSalaryDto dto);
        Task<SalaryResponseDto?> GetActiveSalaryAsync(Guid userId);
        Task<List<SalaryResponseDto>> GetSalaryHistoryAsync(Guid userId);
    }
}
