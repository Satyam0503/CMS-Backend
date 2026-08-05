using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public class PublicCareerService : IPublicCareerService
{
    private const string InternalApplicationMode = "Internal";
    private const string ExternalApplicationMode = "External";
    private readonly IMongoCollection<JobVacancy> _jobs;
    private readonly IMongoCollection<Company> _companies;
    private readonly IMongoCollection<Applicant> _applicants;
    private readonly IMongoCollection<ApplicantLogs> _applicantLogs;
    private readonly IMongoCollection<PublicApplicationToken> _tokens;
    private readonly IMongoCollection<EmpUser> _employees;
    private readonly IMongoCollection<Roles> _roles;
    private readonly IMongoCollection<PublicCompanyProfile> _companyProfiles;
    private readonly IMongoCollection<SavedJob> _savedJobs;
    private readonly IMongoCollection<CareerAnalyticsEvent> _analytics;
    private readonly IPriorityTaskQueue _priorityTaskQueue;
    private readonly IMiddlewareService _middlewareService;

    public PublicCareerService(
        IMongoDbRepository<JobVacancy> jobVacancyRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<Applicant> applicantRepository,
        IMongoDbRepository<ApplicantLogs> applicantLogsRepository,
        IMongoDbRepository<PublicApplicationToken> tokenRepository,
        IMongoDbRepository<EmpUser> employeeRepository,
        IMongoDbRepository<Roles> roleRepository,
        IMongoDbRepository<PublicCompanyProfile> companyProfileRepository,
        IMongoDbRepository<SavedJob> savedJobRepository,
        IMongoDbRepository<CareerAnalyticsEvent> analyticsRepository,
        IPriorityTaskQueue priorityTaskQueue,
        IMiddlewareService middlewareService)
    {
        _jobs = jobVacancyRepository.GetCollection();
        _companies = companyRepository.GetCollection();
        _applicants = applicantRepository.GetCollection();
        _applicantLogs = applicantLogsRepository.GetCollection();
        _tokens = tokenRepository.GetCollection();
        _employees = employeeRepository.GetCollection();
        _roles = roleRepository.GetCollection();
        _companyProfiles = companyProfileRepository.GetCollection();
        _savedJobs = savedJobRepository.GetCollection();
        _analytics = analyticsRepository.GetCollection();
        _priorityTaskQueue = priorityTaskQueue;
        _middlewareService = middlewareService;
    }

    public async Task<Result<PublicJobSummaryDto>> GetMasterJobs(PublicJobSearchRequest request)
    {
        var companies = await _companies.Find(c => c.Status && !c.IsDeleted && c.CareerPortalEnabled && c.PublishJobsToMasterPortal).ToListAsync();
        return await SearchJobs(request, companies, masterOnly: true);
    }

    public async Task<Result<PublicJobSummaryDto>> GetCompanyJobs(string companyCode, PublicJobSearchRequest request)
    {
        string code = Regex.Replace(companyCode ?? string.Empty, @"\D", string.Empty);
        var cf = Builders<Company>.Filter;
        var companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
            cf.Or(
                cf.Eq(c => c.PublicCompanyCode, code),
                cf.Eq(c => c.PublicCompanyCode, companyCode),
                cf.Regex(c => c.PublicCompanyCode, new MongoDB.Bson.BsonRegularExpression(code))
            ));
        var company = await _companies.Find(companyFilter).FirstOrDefaultAsync();
        if (company is null)
        {
            return new Result<PublicJobSummaryDto> { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Career portal not found." };
        }

        if (!Regex.IsMatch(company.PublicCompanyCode ?? string.Empty, @"^\d{6}$") || !company.CareerPortalEnabled)
        {
            company.PublicCompanyCode = await GenerateUniquePublicCompanyCodeAsync(company.CompanyId);
            company.CareerPortalEnabled = true;
            await _companies.UpdateOneAsync(
                c => c.CompanyId == company.CompanyId,
                Builders<Company>.Update
                    .Set(x => x.PublicCompanyCode, company.PublicCompanyCode)
                    .Set(x => x.CareerPortalEnabled, true)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
        }

        return await SearchJobs(request, [company], masterOnly: false);
    }

    public async Task<Result<string>> GetLocations(string? companyCode, string? search)
    {
        var normalizedCompanyCode = Regex.Replace(companyCode ?? string.Empty, @"\D", string.Empty);
        var cf = Builders<Company>.Filter;
        FilterDefinition<Company> companyFilter;
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false), cf.Eq(c => c.PublishJobsToMasterPortal, true));
        }
        else
        {
            companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
                cf.Or(
                    cf.Eq(c => c.PublicCompanyCode, normalizedCompanyCode),
                    cf.Eq(c => c.PublicCompanyCode, companyCode),
                    cf.Regex(c => c.PublicCompanyCode, new BsonRegularExpression(normalizedCompanyCode))
                ));
        }
        var companies = await _companies.Find(companyFilter).Project(c => c.CompanyId).ToListAsync();
        if (companies.Count == 0)
            return new Result<string> { Success = true, MethodResults = [] };

        var f = Builders<JobVacancy>.Filter;
        var filter = f.In(j => j.CompanyId, companies) & f.Eq(j => j.Status, true)
            & f.Eq(j => j.IsDeleted, false) & f.Eq(j => j.PublishToCareerPortal, true)
            & f.Ne(j => j.Location, null) & f.Ne(j => j.Location, string.Empty);
        if (string.IsNullOrWhiteSpace(companyCode)) filter &= f.Eq(j => j.PublishToMasterPortal, true);
        if (!string.IsNullOrWhiteSpace(search))
            filter &= f.Regex(j => j.Location,
                new BsonRegularExpression(Regex.Escape(search.Trim()[..Math.Min(80, search.Trim().Length)]), "i"));

        var locations = await _jobs.Find(filter).Project(j => j.Location).ToListAsync();
        return new Result<string>
        {
            Success = true,
            MethodResults = locations.Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Take(30).ToList()
        };
    }

    public async Task<Result<PublicJobDetailsDto>> GetJobByPublicId(string publicJobId)
    {
        return await GetJobDetails(j => j.PublicJobId == publicJobId);
    }

    public async Task<Result<PublicJobDetailsDto>> GetJobBySlug(string jobSlug)
    {
        string slug = NormalizeSlug(jobSlug);
        return await GetJobDetails(j => j.Slug == slug);
    }

    public async Task<Result<PublicJobApplicationResponse>> Apply(string publicJobId, PublicJobApplicationRequest request)
    {
        var job = await _jobs.Find(j => j.PublicJobId == publicJobId && !j.IsDeleted && j.Status && j.PublishToCareerPortal).FirstOrDefaultAsync();
        if (job is null) return NotFound<PublicJobApplicationResponse>("Job not found.");

        var company = await _companies.Find(c => c.CompanyId == job.CompanyId && c.Status && !c.IsDeleted && c.CareerPortalEnabled).FirstOrDefaultAsync();
        if (company is null) return NotFound<PublicJobApplicationResponse>("Career portal not found.");

        if (IsClosed(job, out string? closedReason))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = closedReason ?? "This job is closed." };
        }

        if (IsExternal(job))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "This job uses an external application link." };
        }

        string email = request.Email.Trim().ToLowerInvariant();
        var existingFilter = Builders<Applicant>.Filter.Eq(a => a.CompanyId, job.CompanyId)
            & Builders<Applicant>.Filter.Eq(a => a.IsDeleted, false)
            & Builders<Applicant>.Filter.Regex(a => a.Email, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i"));
        var existing = await _applicants.Find(existingFilter).SortByDescending(a => a.CreatedDate).FirstOrDefaultAsync();
        if (existing?.CreatedDate is DateTime createdDate && createdDate > DateTime.UtcNow.AddMonths(-6))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = CustomStatusCode.ApplyAfterWaitingPeriod, Message = "Applicant can apply again after the waiting period." };
        }

        Applicant applicant = new()
        {
            ApplicantId = Guid.NewGuid().ToString(),
            CompanyId = job.CompanyId,
            CreatedBy = "public-career",
            CreatedDate = DateTime.UtcNow,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            State = request.State?.Trim() ?? string.Empty,
            Experience = request.Experience,
            VacancyId = job.JobId,
            Status = EnumsHelper.ActivityStatus.Active,
            ActivityType = existing is null ? EnumsHelper.ActivityType.New : EnumsHelper.ActivityType.ReApply,
            ResumeUrl = string.Empty
        };
        await _applicants.InsertOneAsync(applicant);

        await _applicantLogs.InsertOneAsync(new ApplicantLogs
        {
            CompanyId = job.CompanyId,
            CreatedBy = applicant.ApplicantId,
            CreatedDate = DateTime.UtcNow,
            ApplicantId = applicant.ApplicantId,
            UserId = applicant.ApplicantId,
            ActivityCategory = 0,
            Description = "New application submitted from public career portal",
            JobRole = job.Title,
            ApplicantName = $"{applicant.FirstName} {applicant.LastName}"
        });

        string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        DateTime tokenExpiry = DateTime.UtcNow.AddHours(2);
        await _tokens.InsertOneAsync(new PublicApplicationToken
        {
            CompanyId = job.CompanyId,
            CreatedBy = applicant.ApplicantId,
            CreatedDate = DateTime.UtcNow,
            TokenHash = HashToken(rawToken),
            ApplicationReference = applicant.ApplicantId,
            ApplicantId = applicant.ApplicantId,
            JobId = job.JobId,
            ExpiresAt = tokenExpiry
        });

        await QueueApplicationNotifications(applicant, job, company);

        return new Result<PublicJobApplicationResponse>
        {
            Success = true,
            MethodResult = new PublicJobApplicationResponse
            {
                ApplicationReference = applicant.ApplicantId,
                ResumeUploadToken = rawToken,
                TokenExpiresAt = tokenExpiry,
                JobTitle = job.Title,
                CompanyName = company.CompanyName
            }
        };
    }

    public async Task<Result<PublicResumeUploadResult>> UploadResume(string applicationReference, string token, IFormFile resume)
    {
        if (resume is null || resume.Length == 0) return BadRequest<PublicResumeUploadResult>("Resume file is required.");
        if (resume.Length > 5 * 1024 * 1024) return BadRequest<PublicResumeUploadResult>("Resume file must be 5MB or smaller.");
        if (!string.Equals(Path.GetExtension(resume.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)) return BadRequest<PublicResumeUploadResult>("Only PDF resumes are allowed.");

        var tokenHash = HashToken(token);
        var uploadToken = await _tokens.Find(t => t.ApplicationReference == applicationReference && t.TokenHash == tokenHash && !t.IsUsed).FirstOrDefaultAsync();
        if (uploadToken is null || uploadToken.ExpiresAt < DateTime.UtcNow) return BadRequest<PublicResumeUploadResult>("Resume upload token is invalid or expired.");

        using MemoryStream memory = new();
        await resume.CopyToAsync(memory);
        byte[] bytes = memory.ToArray();
        if (bytes.Length < 4 || Encoding.ASCII.GetString(bytes, 0, 4) != "%PDF") return BadRequest<PublicResumeUploadResult>("Only valid PDF resumes are allowed.");

        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resume");
        Directory.CreateDirectory(uploadFolder);
        string fileName = $"{Guid.NewGuid()}.pdf";
        string filePath = Path.Combine(uploadFolder, fileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        var applicantFilter = Builders<Applicant>.Filter.Eq(a => a.ApplicantId, uploadToken.ApplicantId) & Builders<Applicant>.Filter.Eq(a => a.CompanyId, uploadToken.CompanyId);
        await _applicants.UpdateOneAsync(applicantFilter, Builders<Applicant>.Update.Set(a => a.ResumeUrl, fileName).Set(a => a.UpdatedDate, DateTime.UtcNow).Set(a => a.UpdatedBy, "public-career"));
        await _tokens.UpdateOneAsync(t => t.TokenId == uploadToken.TokenId, Builders<PublicApplicationToken>.Update.Set(t => t.IsUsed, true).Set(t => t.UsedAt, DateTime.UtcNow));

        return new Result<PublicResumeUploadResult> { Success = true, MethodResult = new PublicResumeUploadResult { ApplicationReference = applicationReference } };
    }

    private async Task<Result<PublicJobSummaryDto>> SearchJobs(PublicJobSearchRequest request, List<Company> companies, bool masterOnly)
    {
        if (!string.IsNullOrWhiteSpace(request.Company))
            companies = companies.Where(c => c.CompanyName.Contains(request.Company.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(request.CompanySize) && companies.Count > 0)
        {
            var companyIdsBySize = await _companyProfiles.Find(p =>
                    !p.IsDeleted && p.IsPublished && p.CompanySize == request.CompanySize)
                .Project(p => p.CompanyId).ToListAsync();
            companies = companies.Where(c => companyIdsBySize.Contains(c.CompanyId)).ToList();
        }
        Dictionary<string, Company> companyMap = companies.ToDictionary(c => c.CompanyId, c => c);
        if (companyMap.Count == 0) return new Result<PublicJobSummaryDto> { Success = true, MethodResults = [], TotalRecords = 0 };

        var companyIds = companyMap.Keys.ToList();
        var f = Builders<JobVacancy>.Filter;
        var filter = Builders<JobVacancy>.Filter.In(j => j.CompanyId, companyIds)
            & f.Eq(j => j.Status, true) & f.Eq(j => j.IsDeleted, false) & f.Eq(j => j.PublishToCareerPortal, true)
            & (f.Eq(j => j.ExpiresAt, null) | f.Gte(j => j.ExpiresAt, DateTime.UtcNow))
            & (f.Eq(j => j.ApplicationDeadline, null) | f.Gte(j => j.ApplicationDeadline, DateTime.UtcNow));
        if (masterOnly) filter &= f.Eq(j => j.PublishToMasterPortal, true);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var safeSearch = request.Search.Trim()[..Math.Min(200, request.Search.Trim().Length)];
            var rx = new BsonRegularExpression(Regex.Escape(safeSearch), "i");
            var matchingCompanyIds = companies
                .Where(c => c.CompanyName.Contains(safeSearch, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CompanyId).ToList();
            filter &= f.Or(f.In(j => j.CompanyId, matchingCompanyIds),
                f.Regex(j => j.Title, rx), f.Regex(j => j.Summary, rx), f.Regex(j => j.Description, rx),
                f.Regex(j => j.Department, rx), f.Regex(j => j.Industry, rx), f.AnyIn(j => j.Skills, [safeSearch]),
                f.AnyIn(j => j.RequiredSkills, [safeSearch]), f.AnyIn(j => j.Keywords, [safeSearch]));
        }
        if (!string.IsNullOrWhiteSpace(request.Location)) filter &= f.Regex(j => j.Location, new BsonRegularExpression(Regex.Escape(request.Location.Trim()), "i"));
        if (!string.IsNullOrWhiteSpace(request.EmploymentType)) filter &= f.Eq(j => j.EmploymentType, request.EmploymentType);
        if (!string.IsNullOrWhiteSpace(request.WorkplaceType)) filter &= f.Eq(j => j.WorkplaceType, request.WorkplaceType);
        if (request.JobType.HasValue) filter &= f.Eq(j => j.JobType, request.JobType.Value);
        if (request.ExperienceMin.HasValue) filter &= (f.Eq(j => j.ExperienceMax, null) | f.Gte(j => j.ExperienceMax, request.ExperienceMin));
        if (request.ExperienceMax.HasValue) filter &= (f.Eq(j => j.ExperienceMin, null) | f.Lte(j => j.ExperienceMin, request.ExperienceMax));
        if (request.SalaryMin.HasValue) filter &= f.Gte(j => j.SalaryMax, request.SalaryMin);
        if (request.SalaryMax.HasValue) filter &= f.Lte(j => j.SalaryMin, request.SalaryMax);
        if (!string.IsNullOrWhiteSpace(request.Currency)) filter &= f.Eq(j => j.Currency, request.Currency.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(request.Department)) filter &= f.Eq(j => j.Department, request.Department);
        if (!string.IsNullOrWhiteSpace(request.Industry)) filter &= f.Eq(j => j.Industry, request.Industry);
        if (!string.IsNullOrWhiteSpace(request.RoleCategory)) filter &= f.Eq(j => j.RoleCategory, request.RoleCategory);
        if (!string.IsNullOrWhiteSpace(request.Education)) filter &= f.Regex(j => j.EducationRequirement, new BsonRegularExpression(Regex.Escape(request.Education.Trim()), "i"));
        if (!string.IsNullOrWhiteSpace(request.Skills))
        {
            var skills = request.Skills.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Take(30);
            filter &= f.Or(f.AnyIn(j => j.Skills, skills), f.AnyIn(j => j.RequiredSkills, skills),
                f.AnyIn(j => j.PreferredSkills, skills));
        }
        if (request.DatePostedDays.HasValue) filter &= f.Gte(j => j.PublishedAt, DateTime.UtcNow.AddDays(-Math.Clamp(request.DatePostedDays.Value, 1, 365)));
        if (request.Featured.HasValue) filter &= f.Eq(j => j.IsFeatured, request.Featured);
        if (request.UrgentHiring.HasValue) filter &= f.Eq(j => j.IsUrgentHiring, request.UrgentHiring);
        if (request.WalkIn.HasValue) filter &= f.Eq(j => j.IsWalkIn, request.WalkIn);
        if (!string.IsNullOrWhiteSpace(request.ApplicationMode)) filter &= f.Eq(j => j.ApplicationMode, request.ApplicationMode);

        int pageNo = Math.Max(1, request.PageNo);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);
        long count = await _jobs.CountDocumentsAsync(filter);
        var sortName = (request.Sort ?? "newest").ToLowerInvariant();
        var sort = sortName switch
        {
            "oldest" => Builders<JobVacancy>.Sort.Ascending(j => j.PublishedAt),
            "deadline" => Builders<JobVacancy>.Sort.Ascending(j => j.ApplicationDeadline),
            "salary-high" => Builders<JobVacancy>.Sort.Descending(j => j.SalaryMax),
            "salary-low" => Builders<JobVacancy>.Sort.Ascending(j => j.SalaryMin),
            _ => Builders<JobVacancy>.Sort.Descending(j => j.PublishedAt).Descending(j => j.CreatedDate)
        };
        List<JobVacancy> jobs;
        if (sortName is "most-saved" or "most-viewed")
            jobs = await GetEngagementSortedPage(filter, sortName, pageNo, pageSize);
        else
            jobs = await _jobs.Find(filter).Sort(sort).Skip((pageNo - 1) * pageSize).Limit(pageSize).ToListAsync();
        var page = jobs.Select(j => ToSummary(j, companyMap[j.CompanyId])).ToList();

        return new Result<PublicJobSummaryDto> { Success = true, MethodResults = page, TotalRecords = (int)Math.Min(count, int.MaxValue) };
    }

    private async Task<List<JobVacancy>> GetEngagementSortedPage(
        FilterDefinition<JobVacancy> filter, string sort, int pageNo, int pageSize)
    {
        var eligible = await _jobs.Find(filter).Project(j => new { j.JobId, j.PublicJobId, j.PublishedAt }).ToListAsync();
        if (eligible.Count == 0) return [];
        var eligibleJobIds = eligible.Select(e => e.JobId).ToList();
        var eligiblePublicJobIds = eligible.Select(e => e.PublicJobId).ToList();
        Dictionary<string, long> counts;
        if (sort == "most-saved")
        {
            counts = (await _savedJobs.Aggregate()
                    .Match(x => x.IsActive && eligibleJobIds.Contains(x.JobId))
                    .Group(x => x.JobId, group => new { Id = group.Key, Count = group.LongCount() })
                    .ToListAsync())
                .ToDictionary(x => x.Id, x => x.Count);
        }
        else
        {
            counts = (await _analytics.Aggregate()
                    .Match(x => x.EventType == "JobViewed" && x.PublicJobId != null &&
                                eligiblePublicJobIds.Contains(x.PublicJobId))
                    .Group(x => x.PublicJobId!, group => new { Id = group.Key, Count = group.LongCount() })
                    .ToListAsync())
                .ToDictionary(x => x.Id, x => x.Count);
        }

        var pageIds = eligible
            .OrderByDescending(x => counts.GetValueOrDefault(sort == "most-saved" ? x.JobId : x.PublicJobId))
            .ThenByDescending(x => x.PublishedAt)
            .Skip((pageNo - 1) * pageSize).Take(pageSize).Select(x => x.JobId).ToList();
        var page = await _jobs.Find(x => pageIds.Contains(x.JobId)).ToListAsync();
        var positions = pageIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return page.OrderBy(x => positions[x.JobId]).ToList();
    }

    private async Task<Result<PublicJobDetailsDto>> GetJobDetails(System.Linq.Expressions.Expression<Func<JobVacancy, bool>> predicate)
    {
        var job = await _jobs.Find(predicate).FirstOrDefaultAsync();
        if (job is null || job.IsDeleted || !job.Status || !job.PublishToCareerPortal) return NotFound<PublicJobDetailsDto>("Job not found.");
        var company = await _companies.Find(c => c.CompanyId == job.CompanyId && c.Status && !c.IsDeleted && c.CareerPortalEnabled).FirstOrDefaultAsync();
        if (company is null) return NotFound<PublicJobDetailsDto>("Career portal not found.");
        return new Result<PublicJobDetailsDto> { Success = true, MethodResult = ToDetails(job, company) };
    }

    private static PublicJobSummaryDto ToSummary(JobVacancy job, Company company)
    {
        bool closed = IsClosed(job, out string? reason);
        return new PublicJobSummaryDto
        {
            PublicJobId = job.PublicJobId,
            Slug = job.Slug,
            Title = job.Title,
            ShortDescription = ShortText(job.Description),
            Vacancies = job.Vacancies,
            JobType = job.JobType,
            Location = job.Location,
            WorkplaceType = job.WorkplaceType,
            EmploymentType = job.EmploymentType,
            ExperienceMin = job.ExperienceMin,
            ExperienceMax = job.ExperienceMax,
            Currency = job.Currency,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            Skills = job.Skills ?? [],
            PublishedAt = job.PublishedAt ?? job.CreatedDate,
            ApplicationDeadline = job.ApplicationDeadline,
            ApplicationMode = NormalizeApplicationMode(job.ApplicationMode),
            ExternalApplicationUrl = IsSafeExternalUrl(job.ExternalApplicationUrl) ? job.ExternalApplicationUrl : null,
            CanApply = !closed,
            ApplicationClosedReason = reason,
            CompanyPublicCode = company.PublicCompanyCode,
            CompanyName = company.CompanyName,
            CompanySlug = company.CareerSlug,
            CompanyLogo = Common.GetCompanyLogoUrl(company.CompanyLogo)
            ,Summary = job.Summary, Department = job.Department, Industry = job.Industry, RoleCategory = job.RoleCategory,
            EducationRequirement = job.EducationRequirement, IsFeatured = job.IsFeatured == true,
            IsUrgentHiring = job.IsUrgentHiring == true, IsWalkIn = job.IsWalkIn == true, ShowSalary = job.ShowSalary != false
        };
    }

    private static PublicJobDetailsDto ToDetails(JobVacancy job, Company company)
    {
        var summary = ToSummary(job, company);
        return new PublicJobDetailsDto
        {
            PublicJobId = summary.PublicJobId,
            Slug = summary.Slug,
            Title = summary.Title,
            ShortDescription = summary.ShortDescription,
            Vacancies = summary.Vacancies,
            JobType = summary.JobType,
            Location = summary.Location,
            WorkplaceType = summary.WorkplaceType,
            EmploymentType = summary.EmploymentType,
            ExperienceMin = summary.ExperienceMin,
            ExperienceMax = summary.ExperienceMax,
            Currency = summary.Currency,
            SalaryMin = summary.SalaryMin,
            SalaryMax = summary.SalaryMax,
            Skills = summary.Skills,
            PublishedAt = summary.PublishedAt,
            ApplicationDeadline = summary.ApplicationDeadline,
            ApplicationMode = summary.ApplicationMode,
            ExternalApplicationUrl = summary.ExternalApplicationUrl,
            CanApply = summary.CanApply,
            ApplicationClosedReason = summary.ApplicationClosedReason,
            CompanyPublicCode = summary.CompanyPublicCode,
            CompanyName = summary.CompanyName,
            CompanySlug = summary.CompanySlug,
            CompanyLogo = summary.CompanyLogo,
            Description = job.Description,
            ReferenceCode = job.ReferenceCode,
            ExpiresAt = job.ExpiresAt
            ,FunctionalArea = job.FunctionalArea, Responsibilities = job.Responsibilities, RequiredSkills = job.RequiredSkills,
            PreferredSkills = job.PreferredSkills, Benefits = job.Benefits, ShiftType = job.ShiftType,
            WorkingDays = job.WorkingDays, TravelRequirement = job.TravelRequirement, WalkInStartAt = job.WalkInStartAt,
            WalkInEndAt = job.WalkInEndAt, WalkInAddress = job.WalkInAddress, NoticePeriodMaxDays = job.NoticePeriodMaxDays
        };
    }

    private static bool IsClosed(JobVacancy job, out string? reason)
    {
        DateTime now = DateTime.UtcNow;
        if (job.ExpiresAt.HasValue && job.ExpiresAt.Value < now) { reason = "This job posting has expired."; return true; }
        if (job.ApplicationDeadline.HasValue && job.ApplicationDeadline.Value < now) { reason = "Application deadline has passed."; return true; }
        reason = null;
        return false;
    }

    private static bool IsExternal(JobVacancy job) => NormalizeApplicationMode(job.ApplicationMode) == ExternalApplicationMode;
    private static string NormalizeApplicationMode(string? mode) => string.Equals(mode, ExternalApplicationMode, StringComparison.OrdinalIgnoreCase) ? ExternalApplicationMode : InternalApplicationMode;
    private static string NormalizeSlug(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private async Task<string> GenerateUniquePublicCompanyCodeAsync(string companyId)
    {
        while (true)
        {
            var code = Random.Shared.Next(100000, 1000000).ToString();
            var exists = await _companies.Find(c => c.CompanyId != companyId && c.PublicCompanyCode == code).AnyAsync();
            if (!exists) return code;
        }
    }

    private static string ShortText(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, "<.*?>", string.Empty).Trim() is var clean && clean.Length > 180 ? clean[..180] + "..." : clean;
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static Result<T> NotFound<T>(string message) => new() { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = message };
    private static Result<T> BadRequest<T>(string message) => new() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = message };
    private static bool IsSafeExternalUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback;
    }

    private async Task QueueApplicationNotifications(Applicant applicant, JobVacancy job, Company company)
    {
        string candidateName = $"{applicant.FirstName} {applicant.LastName}".Trim();
        RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.ApplyNowMailToApplicant, out var applicantTemplateSubject, out var applicantTemplateBody);
        string applicantSubject = string.IsNullOrWhiteSpace(applicantTemplateSubject) ? $"We received your application for {job.Title}" : applicantTemplateSubject;
        string applicantBody = string.IsNullOrWhiteSpace(applicantTemplateBody) ?
            $"<p>Hi {System.Net.WebUtility.HtmlEncode(candidateName)},</p><p>Thank you for applying for the <strong>{System.Net.WebUtility.HtmlEncode(job.Title)}</strong> position at {System.Net.WebUtility.HtmlEncode(company.CompanyName)}. Our hiring team will review your application and contact you with an update.</p>" : applicantTemplateBody;

        applicantSubject = HtmlTemplate.Render(applicantSubject, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName });
        applicantBody = HtmlTemplate.Render(applicantBody, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName });

        RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.ApplyNowMailToHR, out var hrTemplateSubject, out var hrTemplateBody);
        string hrSubject = HtmlTemplate.Render(
            string.IsNullOrWhiteSpace(hrTemplateSubject) ? $"New application received for {job.Title}" : hrTemplateSubject,
            new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = job.Title, CompanyName = company.CompanyName });
        string hrBody = HtmlTemplate.Render(
            string.IsNullOrWhiteSpace(hrTemplateBody) ?
            "<p>A new application has been submitted.</p><ul><li><strong>Name:</strong> [CandidateName]</li><li><strong>Email:</strong> [CandidateEmail]</li><li><strong>Phone:</strong> [CandidatePhone]</li><li><strong>Job Title:</strong> [JobTitle]</li><li><strong>Company:</strong> [CompanyName]</li></ul>" : hrTemplateBody,
            new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = job.Title, CompanyName = company.CompanyName });

        var internalRecipients = await GetRecruitmentRecipients(job.CompanyId, job.RecruiterContactEmail);

        _priorityTaskQueue.QueueBackgroundWorkItem(async _ =>
        {
            await _middlewareService.EmailSendAndSave(new EmpEmailLogs
            {
                CompanyId = job.CompanyId,
                UserTo = applicant.ApplicantId,
                UserFrom = "public-career",
                Email = applicant.Email,
                Subject = applicantSubject,
                Body = applicantBody,
                EmailLogType = EnumsHelper.MailType.ApplyNowMailToApplicant
            });

            foreach (var recipient in internalRecipients)
            {
                await _middlewareService.EmailSendAndSave(new EmpEmailLogs
                {
                    CompanyId = job.CompanyId,
                    UserTo = recipient.UserId,
                    UserFrom = "public-career",
                    Email = recipient.Email,
                    Subject = hrSubject,
                    Body = hrBody,
                    EmailLogType = EnumsHelper.MailType.ApplyNowMailToHR
                });
            }
        }, priority: 1);
    }

    private async Task<List<(string UserId, string Email)>> GetRecruitmentRecipients(string companyId, string? recruiterContactEmail)
    {
        var recipientMap = new Dictionary<string, (string UserId, string Email)>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(recruiterContactEmail))
        {
            string email = recruiterContactEmail.Trim();
            recipientMap[email] = ("recruiter-contact", email);
        }

        var roleIds = await _roles.Find(r => r.CompanyId == companyId && !r.IsDeleted &&
            (r.RoleType == (int)EnumsHelper.Roles.Administrator || r.RoleType == (int)EnumsHelper.Roles.HR || r.RoleType == (int)EnumsHelper.Roles.HRExecutive))
            .Project(r => r.RolesId)
            .ToListAsync();
        if (roleIds.Count == 0) return recipientMap.Values.ToList();

        var users = await _employees.Find(u => u.CompanyId == companyId && u.Status && u.IsEmailVerified &&
            roleIds.Contains(u.RoleId) && !string.IsNullOrWhiteSpace(u.Email))
            .ToListAsync();
        foreach (var user in users)
        {
            string email = user.Email.Trim();
            recipientMap[email] = (user.UserId, email);
        }

        return recipientMap.Values.ToList();
    }
}
