using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments
{
    public class JobVacancyService : IJobVacancy
    {
        readonly IMongoDbRepository<JobVacancy> _jobVacancyRepo;
        readonly IMongoDbRepository<Applicant> _applicantRepository;
        readonly IMapper _mapper;
        readonly ICareerNotificationService _careerNotifications;
        readonly IHttpContextAccessor _httpContextAccessor;

        public JobVacancyService(IMongoDbRepository<JobVacancy> jobVacancyRepo, IMongoDbRepository<Applicant> applicantRepo, IMapper mapper,
            ICareerNotificationService careerNotifications, IHttpContextAccessor httpContextAccessor)
        {
            _jobVacancyRepo = jobVacancyRepo;
            _mapper = mapper;
            _applicantRepository = applicantRepo;
            _careerNotifications = careerNotifications;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy)
        {
            var validation = ValidateJob(jobVacancy);
            if (!validation.Success) return new Result<JobVacancyModel> { Success = false, Message = validation.Message, ErrorCode = RecruitmentErrorCodes.InvalidRequest };
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId)) return new Result<JobVacancyModel> { Success = false, Message = "A valid company context is required.", ErrorCode = RecruitmentErrorCodes.PermissionDenied };
            JobVacancy vacancy = new JobVacancy()
            {
                CompanyId = companyId,
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
                Summary = jobVacancy.Summary, Department = jobVacancy.Department, FunctionalArea = jobVacancy.FunctionalArea,
                Industry = jobVacancy.Industry, RoleCategory = jobVacancy.RoleCategory, EducationRequirement = jobVacancy.EducationRequirement,
                Responsibilities = jobVacancy.Responsibilities, RequiredSkills = jobVacancy.RequiredSkills,
                PreferredSkills = jobVacancy.PreferredSkills, Benefits = jobVacancy.Benefits, Keywords = jobVacancy.Keywords,
                ShiftType = jobVacancy.ShiftType, WorkingDays = jobVacancy.WorkingDays, TravelRequirement = jobVacancy.TravelRequirement,
                IsFeatured = jobVacancy.IsFeatured, IsUrgentHiring = jobVacancy.IsUrgentHiring, IsWalkIn = jobVacancy.IsWalkIn,
                WalkInStartAt = jobVacancy.WalkInStartAt, WalkInEndAt = jobVacancy.WalkInEndAt, WalkInAddress = jobVacancy.WalkInAddress,
                RecruiterContactEmail = jobVacancy.RecruiterContactEmail, NoticePeriodMaxDays = jobVacancy.NoticePeriodMaxDays,
                ShowSalary = jobVacancy.ShowSalary,
                ScreeningPassScore = jobVacancy.ScreeningPassScore,
                ScreeningCriteria = jobVacancy.ScreeningCriteria ?? [],
                WorkflowMode = string.IsNullOrWhiteSpace(jobVacancy.WorkflowMode) ? "Inherit" : jobVacancy.WorkflowMode.Trim(),
                RequireInterview = jobVacancy.RequireInterview,
                RequirePreboarding = jobVacancy.RequirePreboarding,
            };

            Result saved = await _jobVacancyRepo.AddOne(vacancy);
            if (saved.Success)
                await _careerNotifications.HandleJobSaved(null, vacancy);

            return new Result<JobVacancyModel>
            {
                MethodResult = jobVacancy,
                Success = true
            };
        }
        public async Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId)
        {
            var validation = ValidateJob(jobVacancy);
            if (!validation.Success) return new Result<JobVacancyModel> { Success = false, Message = validation.Message, ErrorCode = RecruitmentErrorCodes.InvalidRequest };
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            JobVacancy? existing = await _jobVacancyRepo.FirstOrDefault(x => x.JobId == jobId && x.CompanyId == companyId, WithDeletedObjects: true);
            if (existing is null) return new Result<JobVacancyModel> { Success = false, Message = "Job vacancy was not found.", ErrorCode = RecruitmentErrorCodes.JobNotFound };
            JobVacancy editedJob = new JobVacancy()
            {
                JobId = jobId,
                CompanyId = companyId,
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
                Summary = jobVacancy.Summary ?? existing?.Summary, Department = jobVacancy.Department ?? existing?.Department,
                FunctionalArea = jobVacancy.FunctionalArea ?? existing?.FunctionalArea, Industry = jobVacancy.Industry ?? existing?.Industry,
                RoleCategory = jobVacancy.RoleCategory ?? existing?.RoleCategory,
                EducationRequirement = jobVacancy.EducationRequirement ?? existing?.EducationRequirement,
                Responsibilities = jobVacancy.Responsibilities.Count > 0 ? jobVacancy.Responsibilities : existing?.Responsibilities ?? [],
                RequiredSkills = jobVacancy.RequiredSkills.Count > 0 ? jobVacancy.RequiredSkills : existing?.RequiredSkills ?? [],
                PreferredSkills = jobVacancy.PreferredSkills.Count > 0 ? jobVacancy.PreferredSkills : existing?.PreferredSkills ?? [],
                Benefits = jobVacancy.Benefits.Count > 0 ? jobVacancy.Benefits : existing?.Benefits ?? [],
                Keywords = jobVacancy.Keywords.Count > 0 ? jobVacancy.Keywords : existing?.Keywords ?? [],
                ShiftType = jobVacancy.ShiftType ?? existing?.ShiftType, WorkingDays = jobVacancy.WorkingDays ?? existing?.WorkingDays,
                TravelRequirement = jobVacancy.TravelRequirement ?? existing?.TravelRequirement,
                IsFeatured = jobVacancy.IsFeatured ?? existing?.IsFeatured, IsUrgentHiring = jobVacancy.IsUrgentHiring ?? existing?.IsUrgentHiring,
                IsWalkIn = jobVacancy.IsWalkIn ?? existing?.IsWalkIn, WalkInStartAt = jobVacancy.WalkInStartAt ?? existing?.WalkInStartAt,
                WalkInEndAt = jobVacancy.WalkInEndAt ?? existing?.WalkInEndAt, WalkInAddress = jobVacancy.WalkInAddress ?? existing?.WalkInAddress,
                RecruiterContactEmail = jobVacancy.RecruiterContactEmail ?? existing?.RecruiterContactEmail,
                NoticePeriodMaxDays = jobVacancy.NoticePeriodMaxDays ?? existing?.NoticePeriodMaxDays,
                ShowSalary = jobVacancy.ShowSalary ?? existing?.ShowSalary,
                ScreeningPassScore = jobVacancy.ScreeningPassScore ?? existing?.ScreeningPassScore,
                ScreeningCriteria = (jobVacancy.ScreeningCriteria is { Count: > 0 } sc ? sc : existing?.ScreeningCriteria) ?? [],
                WorkflowMode = jobVacancy.WorkflowMode ?? existing?.WorkflowMode ?? "Inherit",
                RequireInterview = jobVacancy.RequireInterview ?? existing?.RequireInterview,
                RequirePreboarding = jobVacancy.RequirePreboarding ?? existing?.RequirePreboarding,
            };
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.JobId == jobId && x.CompanyId == companyId;
            Result saved = await _jobVacancyRepo.Update(whereCondition, editedJob);
            if (saved.Success)
                await _careerNotifications.HandleJobSaved(existing, editedJob);
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
                Summary = job.Summary, Department = job.Department, FunctionalArea = job.FunctionalArea, Industry = job.Industry,
                RoleCategory = job.RoleCategory, EducationRequirement = job.EducationRequirement,
                Responsibilities = job.Responsibilities, RequiredSkills = job.RequiredSkills, PreferredSkills = job.PreferredSkills,
                Benefits = job.Benefits, Keywords = job.Keywords, ShiftType = job.ShiftType, WorkingDays = job.WorkingDays,
                TravelRequirement = job.TravelRequirement, IsFeatured = job.IsFeatured, IsUrgentHiring = job.IsUrgentHiring,
                IsWalkIn = job.IsWalkIn, WalkInStartAt = job.WalkInStartAt, WalkInEndAt = job.WalkInEndAt,
                WalkInAddress = job.WalkInAddress, RecruiterContactEmail = job.RecruiterContactEmail,
                NoticePeriodMaxDays = job.NoticePeriodMaxDays, ShowSalary = job.ShowSalary,
                ScreeningPassScore = job.ScreeningPassScore,
                ScreeningCriteria = job.ScreeningCriteria,
                WorkflowMode = job.WorkflowMode ?? "Inherit",
                RequireInterview = job.RequireInterview,
                RequirePreboarding = job.RequirePreboarding,
            });
            Result<GetJobVacancyModel> result = new Result<GetJobVacancyModel>()
            {
                Success = true,
                TotalRecords = count,
                MethodResults = [.. data],
            };
            return result;
        }

        public async Task<JobVacancyModel?> GetVacancyById(string vacancyId, string? companyId = null)
        {
            var owningCompanyId = string.IsNullOrWhiteSpace(companyId)
                ? CurrentContext.CompanyId(_httpContextAccessor)
                : companyId.Trim();
            if (string.IsNullOrWhiteSpace(vacancyId) || string.IsNullOrWhiteSpace(owningCompanyId))
                return null;

            JobVacancy? data = await _jobVacancyRepo.FirstOrDefault(x =>
                x.JobId == vacancyId.Trim() &&
                x.CompanyId == owningCompanyId &&
                !x.IsDeleted);
            return data is null ? null : _mapper.Map<JobVacancyModel>(data);
        }
        public async Task<Result> DeleteJobVacancy(string vacancyId)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            Expression<Func<JobVacancy, bool>> whereCondition = x => x.JobId == vacancyId && x.CompanyId == companyId;
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

        private static Result ValidateJob(JobVacancyModel model)
        {
            var result = new Result { Success = true };
            if (model is null) return new Result { Message = "Job details are required." };
            if (string.IsNullOrWhiteSpace(model.Title)) result.Message = "Job title is required.";
            else if (model.Vacancies < 1) result.Message = "At least one vacancy is required.";
            else if (string.IsNullOrWhiteSpace(model.Description)) result.Message = "Job description is required.";
            else if (model.ExperienceMin.HasValue && model.ExperienceMax.HasValue && model.ExperienceMin > model.ExperienceMax) result.Message = "Minimum experience cannot exceed maximum experience.";
            else if (model.SalaryMin.HasValue && model.SalaryMax.HasValue && model.SalaryMin > model.SalaryMax) result.Message = "Minimum salary cannot exceed maximum salary.";
            else if (model.PublishedAt.HasValue && model.ExpiresAt.HasValue && model.PublishedAt > model.ExpiresAt) result.Message = "Publication date cannot exceed expiry date.";
            else if (model.PublishedAt.HasValue && model.ApplicationDeadline.HasValue && model.PublishedAt > model.ApplicationDeadline) result.Message = "Publication date cannot exceed application deadline.";
            else if (model.ExpiresAt.HasValue && model.ApplicationDeadline.HasValue && model.ApplicationDeadline > model.ExpiresAt) result.Message = "Application deadline cannot exceed expiry date.";
            else if (model.NoticePeriodMaxDays < 0) result.Message = "Notice period cannot be negative.";
            else if (!string.IsNullOrWhiteSpace(model.RecruiterContactEmail) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.RecruiterContactEmail)) result.Message = "Recruiter contact email is invalid.";
            if (!string.IsNullOrEmpty(result.Message)) result.Success = false;
            return result;
        }
    }
}
