using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments
{
    public class JobVacancyService : IJobVacancy
    {
        readonly IMongoDbRepository<JobVacancy> _jobVacancyRepo;
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        readonly IMapper _mapper;

        public JobVacancyService(IMongoDbRepository<JobVacancy> jobVacancyRepo, IMongoDbRepository<Applicant> applicantRepo, IMapper mapper)
        {
            _jobVacancyRepo = jobVacancyRepo;
            _mapper = mapper;
            _applicantRepository = applicantRepo;
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
            };
        }

        public async Task<Result<GetJobVacancyModel>> GetAllVacancy(JobRequestModel model)
        {
            IEnumerable<JobVacancy> jobList = [];
            int count = 0;
            Expression<Func<JobVacancy, bool>> whereCondition = x =>
           (model.JobTypes.Count == 0 || model.JobTypes.Contains(x.JobType)) &&
            (model.Status == null || x.Status == model.Status) &&
            (string.IsNullOrEmpty(model.Search) || x.Title.ToLower().Contains(model.Search.ToLower()));

            jobList = (await _jobVacancyRepo.GetAggregateDataAsync<JobVacancy>(whereCondition, isAscending: false, orderedKey: "CreatedDate", pageSize: model.Records, pageNo: model.PageNo)).ToList();
            count = await _jobVacancyRepo.Count(whereCondition);

            List<string> jobIds = jobList.Select(j => j.JobId).ToList();

            Expression<Func<Applicant, bool>> expression = a => jobIds.Contains(a.VacancyId);
            Dictionary<string, int> applicationCounts = _applicantRepository.Get(expression)
                                    .GroupBy(a => a.VacancyId)
                                    .Select(g => new
                                    {
                                        JobId = g.Key,
                                        Count = g.Count()
                                    })
                                    .ToDictionary(x => x.JobId, x => x.Count);

            var data = jobList.Select(job => new GetJobVacancyModel()
            {
                JobId = job.JobId,
                Title = job.Title,
                Vacancies = job.Vacancies,
                JobType = job.JobType,
                Status = job.Status,
                Description = job.Description,
                TotalApplication = applicationCounts.GetValueOrDefault(job.JobId, 0),
            });
            Result<GetJobVacancyModel> result = new Result<GetJobVacancyModel>()
            {
                Success = true,
                TotalRecords = count,
                MethodResults = [.. data],
            };
            return result;
        }

        public async Task<JobVacancyModel?> GetVacancyById(string vacancyId)
        {
            JobVacancy? data = await _jobVacancyRepo.FirstOrDefault(x => x.JobId == vacancyId);
            if (data == null) return null;
            var result = _mapper.Map<JobVacancyModel>(data);
            return result;
        }

        public async Task<Result> DeleteJobVacancy(string vacancyId)
        {
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.JobId == vacancyId;
            Result result = await _jobVacancyRepo.UpdateMany(whereCondition, Builders<JobVacancy>.Update.Set(x => x.IsDeleted, true));
            Result data = await _jobVacancyRepo.Delete(whereCondition);
            return data;
        }
    }
}
