using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;

namespace Codeji.CMS.Services.Recruitments
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
                Message = "Vacacny Added Successfully",
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
                Status = jobVacancy.Status,
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

        public async Task<Result<JobVacancyModel>> GetAllVacancy(string companyId, int pageNo, int records)
        {
            IEnumerable<JobVacancy> list = await _jobVacancyRepo.GetAll(x => x.CompanyId == companyId);
            IEnumerable<JobVacancy> pagedList = list.Skip((pageNo - 1) * records).Take(records);

            if (pageNo != 0 && records != 0)
            {
                List<JobVacancyModel> data = _mapper.Map<List<JobVacancyModel>>(pagedList);
                Result<JobVacancyModel> result = new Result<JobVacancyModel>()
                {
                    Success = true,
                    TotalRecords = list.Count(),
                    MethodResults = data,
                };
                return result;
            }
            else
            {
                List<JobVacancyModel> data = _mapper.Map<List<JobVacancyModel>>(list);
                Result<JobVacancyModel> result = new Result<JobVacancyModel>()
                {
                    Success = true,
                    TotalRecords = list.Count(),
                    MethodResults = data,
                };
                return result;
            }

        }

        public async Task<string> GetVacancyById(string companyId, string vacancyId)
        {
            JobVacancy? data = await _jobVacancyRepo.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == vacancyId);
            if (data == null)
            {
                return "[Job Title]";
            }
            return data.Title;
        }
    }
}
