using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services
{
    public class JobVacancyService : IJobVacancy
    {
        readonly IMongoDbRepository<JobVacancy> _jobVacancyRepo;

        public JobVacancyService(IMongoDbRepository<JobVacancy> jobVacancyRepo)
        {
            _jobVacancyRepo = jobVacancyRepo;
        }

        public async Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy, string companyId)
        {
            JobVacancy vacancy = new JobVacancy()
            {
                CompanyId = companyId,
                Title = jobVacancy.Title,
                Vacancies = jobVacancy.Vacancies,
                JobType = jobVacancy.JobType,
                Status = true,
                Description = jobVacancy.Description,
            };

            await _jobVacancyRepo.AddOne(vacancy);

            return new Result<JobVacancyModel>
            {
                MethodResult = jobVacancy,
                Message = "User added",
                Success = true

            };

        }
    }
}
