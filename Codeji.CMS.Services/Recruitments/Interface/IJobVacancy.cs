using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IJobVacancy
    {
        Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy, string companyId);
        Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId, string companyId);
        Task<Result<JobVacancyModel>> GetAllVacancy(string companyId, int pageNo, int records);
        Task<string> GetVacancyById(string companyId, string vacancyId);
    }
}
