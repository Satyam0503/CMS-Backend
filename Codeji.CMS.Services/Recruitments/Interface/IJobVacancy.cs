using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IJobVacancy
    {
        Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy);
        Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId);
        Task<Result<JobVacancyModel>> GetAllVacancy(JobSearchModel name, int pageNo, int records);
        Task<JobVacancyModel?> GetVacancyById(string vacancyId);
        Task<Result> DeleteJobVacancy(string vacancyId);
    }
}
