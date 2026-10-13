using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.Repository.Interfaces
{
    public interface ISalaryRepository
    {
        Task<SalaryModel?> GetActiveSalaryAsync(string companyId, Guid userId);
        Task<List<SalaryModel>> GetActiveSalariesAsync(string companyId, List<Guid> userIds);
        Task<List<SalaryModel>> GetSalaryHistoryAsync(string companyId, Guid userId);
        Task AddAsync(SalaryModel salary);
        Task UpdateAsync(SalaryModel salary);
    }
}
