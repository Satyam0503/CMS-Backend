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

        public async Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy)
        {
            JobVacancy vacancy = new JobVacancy()
            {
                Title = jobVacancy.Title,
                Vacancies = jobVacancy.Vacancies,
                JobType = jobVacancy.JobType,
                Status = jobVacancy.Status,
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
        public async Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId)
        {
            JobVacancy editedJob = new JobVacancy()
            {
                JobId = jobId,
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

        public async Task<Result<JobVacancyModel>> GetAllVacancy(JobSearchModel search, int pageNo, int records)
        {
            pageNo = pageNo == 0 ? 1 : pageNo;
            records = records == 0 ? 10 : records;
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.Title.Contains(search.name, StringComparison.CurrentCultureIgnoreCase);

            var totalRecord = await _jobVacancyRepo.Count(whereCondition);

            IEnumerable<JobVacancy> list = await _jobVacancyRepo.GetAggregateDataAsync<JobVacancy>(whereCondition, pageSize: records, pageNo: pageNo);

            List<JobVacancyModel> data = _mapper.Map<List<JobVacancyModel>>(list);
            Result<JobVacancyModel> result = new Result<JobVacancyModel>()
            {
                Success = true,
                TotalRecords = totalRecord,
                MethodResults = data,
            };
            return result;

        }

        public async Task<JobVacancyModel?> GetVacancyById(string vacancyId)
        {
            JobVacancy? data = await _jobVacancyRepo.FirstOrDefault(x => x.JobId == vacancyId);
            if (data == null)
            {
                return null;
            }
            return _mapper.Map<JobVacancyModel>(data);
        }

        public async Task<Result> DeleteJobVacancy(string vacancyId)
        {
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.JobId == vacancyId;
            Result data = await _jobVacancyRepo.Delete(whereCondition);
            return data;
        }
    }
}
