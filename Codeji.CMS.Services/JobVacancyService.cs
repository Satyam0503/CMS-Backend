using System.Linq.Expressions;
using AutoMapper;
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
        readonly IMapper _mapper;

        public JobVacancyService(IMongoDbRepository<JobVacancy> jobVacancyRepo, IMapper mapper)
        {
            _jobVacancyRepo = jobVacancyRepo;
            _mapper = mapper;
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
        public async Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId, string companyId)
        {
            JobVacancy editedJob = new JobVacancy()
            {
                JobId = jobId,
                CompanyId = companyId,
                Title = jobVacancy.Title,
                Vacancies = jobVacancy.Vacancies,
                JobType = jobVacancy.JobType,
                Status = true,
                Description = jobVacancy.Description,
            };
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.JobId == jobId;
            await _jobVacancyRepo.Update(whereCondition, editedJob);
            return new Result<JobVacancyModel>
            {
                MethodResult = jobVacancy,
                Success = true,
                Message = "Job Updated"
            };
        }

        public async Task<List<JobVacancyModel>> GetAllVacancy()
        {
            IEnumerable<JobVacancy> list = await _jobVacancyRepo.GetAll();
            return _mapper.Map<List<JobVacancyModel>>(list);
        }
    }
}
