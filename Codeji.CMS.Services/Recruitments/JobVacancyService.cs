using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Driver;
using System.Text.RegularExpressions;

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
                PublicJobId = string.IsNullOrWhiteSpace(jobVacancy.PublicJobId) ? $"JOB-{Guid.NewGuid():N}" : jobVacancy.PublicJobId.Trim(),
                Slug = await BuildUniqueSlug(string.IsNullOrWhiteSpace(jobVacancy.Slug) ? jobVacancy.Title : jobVacancy.Slug),
                ReferenceCode = jobVacancy.ReferenceCode,
                PublishToCareerPortal = jobVacancy.PublishToCareerPortal ?? true,
                PublishToMasterPortal = jobVacancy.PublishToMasterPortal ?? false,
                ApplicationMode = NormalizeApplicationMode(jobVacancy.ApplicationMode),
                ExternalApplicationUrl = NormalizeExternalUrl(jobVacancy.ExternalApplicationUrl, jobVacancy.ApplicationMode),
                Location = jobVacancy.Location,
                WorkplaceType = jobVacancy.WorkplaceType,
                EmploymentType = jobVacancy.EmploymentType,
                ExperienceMin = jobVacancy.ExperienceMin,
                ExperienceMax = jobVacancy.ExperienceMax,
                SalaryMin = jobVacancy.SalaryMin,
                SalaryMax = jobVacancy.SalaryMax,
                Currency = jobVacancy.Currency,
                Skills = jobVacancy.Skills,
                PublishedAt = jobVacancy.PublishedAt,
                ExpiresAt = jobVacancy.ExpiresAt,
                ApplicationDeadline = jobVacancy.ApplicationDeadline,
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
            JobVacancy? existing = await _jobVacancyRepo.FirstOrDefault(x => x.JobId == jobId, WithDeletedObjects: true);
            JobVacancy editedJob = new JobVacancy()
            {
                JobId = jobId,
                Title = jobVacancy.Title,
                Vacancies = jobVacancy.Vacancies,
                JobType = jobVacancy.JobType,
                Status = jobVacancy.Status,
                Description = jobVacancy.Description,
                PublicJobId = string.IsNullOrWhiteSpace(existing?.PublicJobId) ? $"JOB-{Guid.NewGuid():N}" : existing.PublicJobId,
                Slug = string.IsNullOrWhiteSpace(existing?.Slug) ? await BuildUniqueSlug(jobVacancy.Title, jobId) : existing.Slug,
                ReferenceCode = jobVacancy.ReferenceCode ?? existing?.ReferenceCode,
                PublishToCareerPortal = jobVacancy.PublishToCareerPortal ?? existing?.PublishToCareerPortal ?? true,
                PublishToMasterPortal = jobVacancy.PublishToMasterPortal ?? existing?.PublishToMasterPortal ?? false,
                ApplicationMode = NormalizeApplicationMode(jobVacancy.ApplicationMode ?? existing?.ApplicationMode),
                ExternalApplicationUrl = NormalizeExternalUrl(jobVacancy.ExternalApplicationUrl ?? existing?.ExternalApplicationUrl, jobVacancy.ApplicationMode ?? existing?.ApplicationMode),
                Location = jobVacancy.Location ?? existing?.Location,
                WorkplaceType = jobVacancy.WorkplaceType ?? existing?.WorkplaceType,
                EmploymentType = jobVacancy.EmploymentType ?? existing?.EmploymentType,
                ExperienceMin = jobVacancy.ExperienceMin ?? existing?.ExperienceMin,
                ExperienceMax = jobVacancy.ExperienceMax ?? existing?.ExperienceMax,
                SalaryMin = jobVacancy.SalaryMin ?? existing?.SalaryMin,
                SalaryMax = jobVacancy.SalaryMax ?? existing?.SalaryMax,
                Currency = jobVacancy.Currency ?? existing?.Currency,
                Skills = jobVacancy.Skills.Count > 0 ? jobVacancy.Skills : existing?.Skills ?? [],
                PublishedAt = jobVacancy.PublishedAt ?? existing?.PublishedAt,
                ExpiresAt = jobVacancy.ExpiresAt ?? existing?.ExpiresAt,
                ApplicationDeadline = jobVacancy.ApplicationDeadline ?? existing?.ApplicationDeadline,
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
                PublicJobId = job.PublicJobId,
                Slug = job.Slug,
                ReferenceCode = job.ReferenceCode,
                Title = job.Title,
                Vacancies = job.Vacancies,
                JobType = job.JobType,
                Status = job.Status,
                Description = job.Description,
                TotalApplication = applicationCounts.GetValueOrDefault(job.JobId, 0),
                PublishToCareerPortal = job.PublishToCareerPortal,
                PublishToMasterPortal = job.PublishToMasterPortal,
                ApplicationMode = job.ApplicationMode,
                ExternalApplicationUrl = job.ExternalApplicationUrl,
                Location = job.Location,
                WorkplaceType = job.WorkplaceType,
                EmploymentType = job.EmploymentType,
                ExperienceMin = job.ExperienceMin,
                ExperienceMax = job.ExperienceMax,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Currency = job.Currency,
                Skills = job.Skills,
                PublishedAt = job.PublishedAt,
                ExpiresAt = job.ExpiresAt,
                ApplicationDeadline = job.ApplicationDeadline,
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

        private async Task<string> BuildUniqueSlug(string value, string? currentJobId = null)
        {
            string baseSlug = Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "job";
            string slug = baseSlug;
            int counter = 2;
            while (await _jobVacancyRepo.GetCollection().Find(j => j.Slug == slug && j.JobId != currentJobId).AnyAsync())
            {
                slug = $"{baseSlug}-{counter++}";
            }
            return slug;
        }

        private static string NormalizeApplicationMode(string? mode)
        {
            return string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase) ? "External" : "Internal";
        }

        private static string? NormalizeExternalUrl(string? url, string? mode)
        {
            if (!string.Equals(NormalizeApplicationMode(mode), "External", StringComparison.OrdinalIgnoreCase)) return null;
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback ? url : null;
        }
    }
}
