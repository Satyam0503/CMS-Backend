using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;

namespace Codeji.CMS.Services.Interface
{
    public interface IJobVacancy
    {
        Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy, string companyId);
        Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId, string companyId);
        Task<List<JobVacancyModel>> GetAllVacancy(string companyId);
    }
}
