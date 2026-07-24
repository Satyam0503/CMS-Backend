using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
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
    private readonly IMongoCollection<MailTemplate> _mailTemplates;
    private readonly IPriorityTaskQueue _priorityTaskQueue;
    private readonly IMiddlewareService _middlewareService;

    public PublicCareerService(
        IMongoDbRepository<JobVacancy> jobVacancyRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<Applicant> applicantRepository,
        IMongoDbRepository<ApplicantLogs> applicantLogsRepository,
        IMongoDbRepository<PublicApplicationToken> tokenRepository,
        IMongoDbRepository<MailTemplate> mailTemplateRepository,
        IPriorityTaskQueue priorityTaskQueue,
        IMiddlewareService middlewareService)
    {
        _jobs = jobVacancyRepository.GetCollection();
        _companies = companyRepository.GetCollection();
        _applicants = applicantRepository.GetCollection();
        _applicantLogs = applicantLogsRepository.GetCollection();
        _tokens = tokenRepository.GetCollection();
        _mailTemplates = mailTemplateRepository.GetCollection();
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
        var company = await _companies.Find(c => c.Status && !c.IsDeleted && c.CareerPortalEnabled && c.PublicCompanyCode == code).FirstOrDefaultAsync();
        if (company is null)
        {
            return new Result<PublicJobSummaryDto> { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Career portal not found." };
        }

        return await SearchJobs(request, [company], masterOnly: false);
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

        await QueueApplicationReceipt(applicant, job, company);

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
        Dictionary<string, Company> companyMap = companies.ToDictionary(c => c.CompanyId, c => c);
        if (companyMap.Count == 0) return new Result<PublicJobSummaryDto> { Success = true, MethodResults = [], TotalRecords = 0 };

        var companyIds = companyMap.Keys.ToList();
        var filter = Builders<JobVacancy>.Filter.In(j => j.CompanyId, companyIds)
            & Builders<JobVacancy>.Filter.Eq(j => j.Status, true)
            & Builders<JobVacancy>.Filter.Eq(j => j.IsDeleted, false)
            & Builders<JobVacancy>.Filter.Eq(j => j.PublishToCareerPortal, true);
        if (masterOnly) filter &= Builders<JobVacancy>.Filter.Eq(j => j.PublishToMasterPortal, true);

        var jobs = await _jobs.Find(filter).ToListAsync();
        jobs = ApplyInMemoryFilters(jobs, request);
        jobs = SortJobs(jobs, request.Sort);

        int pageNo = Math.Max(1, request.PageNo);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);
        int count = jobs.Count;
        var page = jobs.Skip((pageNo - 1) * pageSize).Take(pageSize).Select(j => ToSummary(j, companyMap[j.CompanyId])).ToList();

        return new Result<PublicJobSummaryDto> { Success = true, MethodResults = page, TotalRecords = count };
    }

    private async Task<Result<PublicJobDetailsDto>> GetJobDetails(System.Linq.Expressions.Expression<Func<JobVacancy, bool>> predicate)
    {
        var job = await _jobs.Find(predicate).FirstOrDefaultAsync();
        if (job is null || job.IsDeleted || !job.Status || !job.PublishToCareerPortal) return NotFound<PublicJobDetailsDto>("Job not found.");
        var company = await _companies.Find(c => c.CompanyId == job.CompanyId && c.Status && !c.IsDeleted && c.CareerPortalEnabled).FirstOrDefaultAsync();
        if (company is null) return NotFound<PublicJobDetailsDto>("Career portal not found.");
        return new Result<PublicJobDetailsDto> { Success = true, MethodResult = ToDetails(job, company) };
    }

    private static List<JobVacancy> ApplyInMemoryFilters(List<JobVacancy> jobs, PublicJobSearchRequest request)
    {
        string search = request.Search?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(search)) jobs = jobs.Where(j => j.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || j.Description.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (request.JobType.HasValue) jobs = jobs.Where(j => j.JobType == request.JobType.Value).ToList();
        if (!string.IsNullOrWhiteSpace(request.Location)) jobs = jobs.Where(j => (j.Location ?? string.Empty).Contains(request.Location, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(request.EmploymentType)) jobs = jobs.Where(j => string.Equals(j.EmploymentType, request.EmploymentType, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(request.WorkplaceType)) jobs = jobs.Where(j => string.Equals(j.WorkplaceType, request.WorkplaceType, StringComparison.OrdinalIgnoreCase)).ToList();
        if (request.ExperienceMin.HasValue) jobs = jobs.Where(j => !j.ExperienceMax.HasValue || j.ExperienceMax >= request.ExperienceMin).ToList();
        if (request.ExperienceMax.HasValue) jobs = jobs.Where(j => !j.ExperienceMin.HasValue || j.ExperienceMin <= request.ExperienceMax).ToList();
        return jobs.Where(j => !IsClosed(j, out _)).ToList();
    }

    private static List<JobVacancy> SortJobs(List<JobVacancy> jobs, string? sort) => (sort ?? "newest").ToLowerInvariant() switch
    {
        "oldest" => jobs.OrderBy(j => j.PublishedAt ?? j.CreatedDate ?? DateTime.MinValue).ToList(),
        "deadline" => jobs.OrderBy(j => j.ApplicationDeadline ?? DateTime.MaxValue).ToList(),
        _ => jobs.OrderByDescending(j => j.PublishedAt ?? j.CreatedDate ?? DateTime.MinValue).ToList()
    };

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
            CompanyLogo = company.CompanyLogo
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
    private static string ShortText(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, "<.*?>", string.Empty).Trim() is var clean && clean.Length > 180 ? clean[..180] + "..." : clean;
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static Result<T> NotFound<T>(string message) => new() { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = message };
    private static Result<T> BadRequest<T>(string message) => new() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = message };
    private static bool IsSafeExternalUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback;
    }

    private async Task QueueApplicationReceipt(Applicant applicant, JobVacancy job, Company company)
    {
        var template = await _mailTemplates.Find(t => t.mailType == EnumsHelper.MailType.ApplyNowMailToApplicant).FirstOrDefaultAsync();
        string candidateName = $"{applicant.FirstName} {applicant.LastName}".Trim();
        string subject = template?.subject ?? $"We received your application for {job.Title}";
        string body = template?.body ??
            $"<p>Hi {System.Net.WebUtility.HtmlEncode(candidateName)},</p><p>Thank you for applying for the <strong>{System.Net.WebUtility.HtmlEncode(job.Title)}</strong> position at {System.Net.WebUtility.HtmlEncode(company.CompanyName)}. Our hiring team will review your application and contact you with an update.</p>";

        subject = HtmlTemplate.Render(subject, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName });
        body = HtmlTemplate.Render(body, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName });

        _priorityTaskQueue.QueueBackgroundWorkItem(async _ =>
        {
            await _middlewareService.EmailSendAndSave(new EmpEmailLogs
            {
                CompanyId = job.CompanyId,
                UserTo = applicant.ApplicantId,
                UserFrom = "public-career",
                Email = applicant.Email,
                Subject = subject,
                Body = body,
                EmailLogType = EnumsHelper.MailType.ApplyNowMailToApplicant
            });
        }, priority: 1);
    }
}
