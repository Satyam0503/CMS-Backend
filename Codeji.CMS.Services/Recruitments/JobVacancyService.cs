using System.Linq.Expressions;
using AutoMapper;
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

        public async Task<Result<JobVacancyModel>> GetAllVacancy(JobRequestModel model)
        {
            IEnumerable<JobVacancy> jobList = [];
            int count = 0;
            Expression<Func<JobVacancy, bool>> whereCondition = x =>
           (model.JobTypes.Count == 0 || model.JobTypes.Contains(x.JobType)) &&
            (model.Status == null || x.Status == model.Status) &&
            (string.IsNullOrEmpty(model.Search) || x.Title.Contains(model.Search, StringComparison.CurrentCultureIgnoreCase));

            jobList = await _jobVacancyRepo.GetAggregateDataAsync<JobVacancy>(whereCondition, isAscending: false, orderedKey: "CreatedDate", pageSize: model.Records, pageNo: model.PageNo);
            count = await _jobVacancyRepo.Count(whereCondition);
            List<JobVacancyModel> data = _mapper.Map<List<JobVacancyModel>>(jobList);
            Result<JobVacancyModel> result = new Result<JobVacancyModel>()
            {
                Success = true,
                TotalRecords = count,
                MethodResults = data,
            };
            return result;
        }

        public async Task<JobVacancyModel?> GetVacancyById(string vacancyId)
        {
            JobVacancy? data = await _jobVacancyRepo.FirstOrDefault(x => x.JobId == vacancyId);
            if (data == null) return null;
            var result = _mapper.Map<JobVacancyModel>(data);
            int TotalSubmissions = await _applicantRepository.Count(x => x.VacancyId == data.JobId);
            result.TotalSubmissions = TotalSubmissions;
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
