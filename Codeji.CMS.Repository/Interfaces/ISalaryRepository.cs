using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.Repository.Interfaces
{
    public interface ISalaryRepository
    {
        Task<SalaryModel?> GetActiveSalaryAsync(Guid userId);
        Task<List<SalaryModel>> GetActiveSalariesAsync(List<Guid> userIds);
        Task<List<SalaryModel>> GetSalaryHistoryAsync(Guid userId);
        Task AddAsync(SalaryModel salary);
        Task UpdateAsync(SalaryModel salary);
    }
}
