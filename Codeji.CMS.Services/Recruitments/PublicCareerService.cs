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
    private readonly IMongoCollection<RecruitmentJobApplication> _jobApplications;
    private readonly IMongoCollection<RecruitmentEnterpriseConfig> _recruitmentConfigs;
    private readonly IMongoCollection<RecruitmentEmailOutbox> _recruitmentEmailOutbox;
    private readonly IRecruitmentResumeScanner? _resumeScanner;

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
        IMongoDbRepository<RecruitmentJobApplication> jobApplicationRepository,
        IMongoDbRepository<RecruitmentEnterpriseConfig> recruitmentConfigRepository,
        IMongoDbRepository<RecruitmentEmailOutbox> recruitmentEmailOutboxRepository,
        IRecruitmentResumeScanner? resumeScanner = null)
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
        _jobApplications = jobApplicationRepository.GetCollection();
        _recruitmentConfigs = recruitmentConfigRepository.GetCollection();
        _recruitmentEmailOutbox = recruitmentEmailOutboxRepository.GetCollection();
        _resumeScanner = resumeScanner;
    }

    public async Task<Result<PublicJobSummaryDto>> GetMasterJobs(PublicJobSearchRequest request)
    {
        var companies = await _companies.Find(c => c.Status && !c.IsDeleted && c.CareerPortalEnabled && c.PublishJobsToMasterPortal).ToListAsync();
        return await SearchJobs(request, companies, masterOnly: true);
    }

    public async Task<Result<PublicJobSummaryDto>> GetCompanyJobs(string companyCode, PublicJobSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            return new Result<PublicJobSummaryDto> { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Career portal not found." };
        }

        string trimmedCode = companyCode.Trim();
        string numericCode = Regex.Replace(trimmedCode, @"\D", string.Empty);
        var cf = Builders<Company>.Filter;
        FilterDefinition<Company> companyFilter;
        if (!string.IsNullOrEmpty(numericCode))
        {
            companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
                cf.Or(
                    cf.Eq(c => c.PublicCompanyCode, numericCode),
                    cf.Eq(c => c.PublicCompanyCode, trimmedCode)
                ));
        }
        else
        {
            companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
                cf.Eq(c => c.PublicCompanyCode, trimmedCode));
        }
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
        var cf = Builders<Company>.Filter;
        FilterDefinition<Company> companyFilter;
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false), cf.Eq(c => c.PublishJobsToMasterPortal, true));
        }
        else
        {
            string trimmed = companyCode.Trim();
            var normalizedCompanyCode = Regex.Replace(trimmed, @"\D", string.Empty);
            if (!string.IsNullOrEmpty(normalizedCompanyCode))
            {
                companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
                    cf.Or(
                        cf.Eq(c => c.PublicCompanyCode, normalizedCompanyCode),
                        cf.Eq(c => c.PublicCompanyCode, trimmed)
                    ));
            }
            else
            {
                companyFilter = cf.And(cf.Eq(c => c.Status, true), cf.Eq(c => c.IsDeleted, false),
                    cf.Eq(c => c.PublicCompanyCode, trimmed));
            }
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
        var requestValidation = ValidateApplicationRequest(request);
        if (requestValidation is not null) return BadRequest<PublicJobApplicationResponse>(requestValidation);
        var job = await _jobs.Find(j => j.PublicJobId == publicJobId && !j.IsDeleted && j.Status && j.PublishToCareerPortal).FirstOrDefaultAsync();
        if (job is null) return NotFound<PublicJobApplicationResponse>("Job not found.");

        var company = await _companies.Find(c => c.CompanyId == job.CompanyId && c.Status && !c.IsDeleted && c.CareerPortalEnabled).FirstOrDefaultAsync();
        if (company is null) return NotFound<PublicJobApplicationResponse>("Career portal not found.");

        var recruitmentConfig = await _recruitmentConfigs.Find(x => x.CompanyId == job.CompanyId && !x.IsDeleted).FirstOrDefaultAsync();
        var candidateSourceOptions = PublicCareerApplicationRules.EffectiveCandidateSourceOptions(recruitmentConfig?.CandidateSourceOptions);
        if (!PublicCareerApplicationRules.IsValidSourceAttribution(request, candidateSourceOptions))
            return BadRequest<PublicJobApplicationResponse>("Candidate source or campaign attribution is invalid or exceeds the allowed length.");

        var submissionKey = request.IdempotencyKey.Trim();
        string submissionPayloadHash = PublicCareerApplicationRules.HashApplicationSubmission(request, candidateSourceOptions);
        if (!string.IsNullOrWhiteSpace(submissionKey))
        {
            var priorSubmission = await _applicants.Find(a => a.CompanyId == job.CompanyId && a.ApplicationSubmissionIdempotencyKey == submissionKey && !a.IsDeleted).FirstOrDefaultAsync();
            if (priorSubmission is not null)
            {
                if (priorSubmission.VacancyId != job.JobId || !PublicCareerApplicationRules.IsSameApplicationSubmission(priorSubmission, request, candidateSourceOptions))
                    return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status409Conflict, ErrorCode = RecruitmentErrorCodes.IdempotencyConflict, Message = "This application submission key is already in use." };
                await EnsureApplicationRecords(priorSubmission, job);
                var replayResponse = await CreateApplicationResponse(priorSubmission, job, company);
                await QueueApplicationNotifications(priorSubmission, job, company);
                return new Result<PublicJobApplicationResponse> { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = replayResponse, Message = "Application submission already received." };
            }
        }

        if (IsClosed(job, out string? closedReason))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = closedReason ?? "This job is closed." };
        }

        if (IsExternal(job))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "This job uses an external application link." };
        }

        string email = request.Email.Trim().ToLowerInvariant();
        string privacyNotice = recruitmentConfig?.CandidatePrivacyNoticeText?.Trim() ?? string.Empty;
        string privacyVersion = recruitmentConfig?.CandidatePrivacyNoticeVersion?.Trim() ?? string.Empty;
        if (!PublicCareerApplicationRules.IsPrivacyNoticeConfigured(privacyNotice, privacyVersion))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "This company has not finished setting up its candidate privacy notice. Please contact the company for help." };
        }
        if (!request.PrivacyAcknowledged)
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Please acknowledge the candidate privacy notice before applying." };
        }
        if (!PublicCareerApplicationRules.AcknowledgesCurrentPrivacyNotice(true, request.PrivacyNoticeVersion, privacyVersion))
        {
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "The candidate privacy notice changed. Refresh this job page to review the current notice before applying." };
        }
        var nowUtc = DateTime.UtcNow;
        var retentionDays = recruitmentConfig?.DefaultCandidateRetentionDays is >= 30 and <= 3650
            ? recruitmentConfig.DefaultCandidateRetentionDays
            : 730;
        var allowMultiVacancyApplications = recruitmentConfig?.AllowMultiVacancyApplications ?? true;
        var existingFilter = Builders<Applicant>.Filter.Eq(a => a.CompanyId, job.CompanyId)
            & Builders<Applicant>.Filter.Eq(a => a.IsDeleted, false)
            & Builders<Applicant>.Filter.Regex(a => a.Email, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i"));
        if (allowMultiVacancyApplications)
        {
            // A candidate may be considered for another role at the same company.
            // When this company setting is disabled, the cooldown is company-wide.
            existingFilter &= Builders<Applicant>.Filter.Eq(a => a.VacancyId, job.JobId);
        }
        var existing = await _applicants.Find(existingFilter).SortByDescending(a => a.CreatedDate).FirstOrDefaultAsync();
        if (PublicCareerApplicationRules.IsWithinReapplicationCooldown(
            existing?.CreatedDate,
            nowUtc,
            allowMultiVacancyApplications,
            sameVacancy: existing?.VacancyId == job.JobId))
        {
            var message = existing?.VacancyId == job.JobId
                ? "You can apply to this role again after the six-month waiting period."
                : "This company accepts one application per candidate every six months. Please contact the hiring team if you need help.";
            return new Result<PublicJobApplicationResponse> { Success = false, StatusCode = CustomStatusCode.ApplyAfterWaitingPeriod, Message = message };
        }

        Applicant applicant = new()
        {
            ApplicantId = Guid.NewGuid().ToString(),
            CompanyId = job.CompanyId,
            CreatedBy = "public-career",
            CreatedDate = nowUtc,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            State = request.State?.Trim() ?? string.Empty,
            Experience = request.Experience * 12,
            CurrentCompany = request.CurrentCompany?.Trim(),
            CurrentJobTitle = request.CurrentJobTitle?.Trim(),
            NoticePeriodDays = request.NoticePeriodDays,
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            PortfolioUrl = request.PortfolioUrl?.Trim(),
            CoverLetter = request.CoverLetter?.Trim(),
            CandidateReportedSource = PublicCareerApplicationRules.NormalizeCandidateReportedSource(request.CandidateReportedSource, candidateSourceOptions),
            CandidateReportedSourceDetail = request.CandidateReportedSourceDetail?.Trim(),
            AttributionSource = request.AttributionSource?.Trim(),
            AttributionMedium = request.AttributionMedium?.Trim(),
            AttributionCampaign = request.AttributionCampaign?.Trim(),
            AttributionContent = request.AttributionContent?.Trim(),
            AttributionTerm = request.AttributionTerm?.Trim(),
            ReferrerHost = request.ReferrerHost?.Trim().ToLowerInvariant(),
            AttributionCapturedAtUtc = new[] { request.AttributionSource, request.AttributionMedium, request.AttributionCampaign, request.AttributionContent, request.AttributionTerm, request.ReferrerHost }.Any(x => !string.IsNullOrWhiteSpace(x)) ? nowUtc : null,
            VacancyId = job.JobId,
            Status = EnumsHelper.ActivityStatus.Active,
            ActivityType = existing is null ? EnumsHelper.ActivityType.New : EnumsHelper.ActivityType.ReApply,
            RetentionUntilUtc = nowUtc.AddDays(retentionDays),
            ApplicationSubmissionIdempotencyKey = submissionKey,
            ApplicationSubmissionPayloadHash = submissionPayloadHash,
            ResumeUrl = string.Empty
        };
        PublicCareerApplicationRules.RecordPrivacyNoticeAcknowledgement(applicant, privacyNotice, privacyVersion, nowUtc);
        try
        {
            await _applicants.InsertOneAsync(applicant);
        }
        catch (MongoWriteException exception) when (
            submissionKey is not null &&
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // The unique tenant/key index is the concurrency boundary. If a
            // simultaneous retry won the insert, return that application safely.
            var winningSubmission = await _applicants.Find(a =>
                    a.CompanyId == job.CompanyId &&
                    a.ApplicationSubmissionIdempotencyKey == submissionKey &&
                    !a.IsDeleted)
                .FirstOrDefaultAsync();
            if (winningSubmission is null) throw;
            if (winningSubmission.VacancyId != job.JobId ||
                !PublicCareerApplicationRules.IsSameApplicationSubmission(winningSubmission, request))
            {
                return new Result<PublicJobApplicationResponse>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ErrorCode = RecruitmentErrorCodes.IdempotencyConflict,
                    Message = "This application submission key is already in use."
                };
            }

            await EnsureApplicationRecords(winningSubmission, job);
            var replayResponse = await CreateApplicationResponse(winningSubmission, job, company);
            await QueueApplicationNotifications(winningSubmission, job, company);
            return new Result<PublicJobApplicationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                MethodResult = replayResponse,
                Message = "Application submission already received."
            };
        }
        await EnsureApplicationRecords(applicant, job);
        var applicationResponse = await CreateApplicationResponse(applicant, job, company);

        await QueueApplicationNotifications(applicant, job, company);

        return new Result<PublicJobApplicationResponse>
        {
            Success = true,
            MethodResult = applicationResponse
        };
    }

    public async Task<Result<PublicApplicationStatusDto>> GetApplicationStatus(PublicApplicationStatusRequest request)
    {
        // A random application reference plus the applicant's email is required. Keep
        // the response deliberately minimal and return the same 404 for every miss.
        if (request is null || !Guid.TryParse(request.ApplicationReference?.Trim(), out var reference) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email?.Trim()))
            return NotFound<PublicApplicationStatusDto>("We couldn't find an application with those details. Check them and try again.");

        string applicantId = reference.ToString();
        string email = request.Email.Trim();
        var applicant = await _applicants.Find(PublicApplicationStatusRules.BuildApplicantMatchFilter(applicantId, email))
            .FirstOrDefaultAsync();
        if (applicant is null)
            return NotFound<PublicApplicationStatusDto>("We couldn't find an application with those details. Check them and try again.");

        string publicSubmissionKey = $"public-application:{applicant.ApplicantId}:initial";
        var application = await _jobApplications.Find(a => a.CompanyId == applicant.CompanyId &&
                a.ApplicantId == applicant.ApplicantId && a.VacancyId == applicant.VacancyId &&
                a.PublicSubmissionIdempotencyKey == publicSubmissionKey && !a.IsDeleted)
            .FirstOrDefaultAsync();
        if (application is null)
            return NotFound<PublicApplicationStatusDto>("We couldn't find an application with those details. Check them and try again.");

        var job = await _jobs.Find(j => j.CompanyId == applicant.CompanyId && j.JobId == applicant.VacancyId).FirstOrDefaultAsync();
        var company = await _companies.Find(c => c.CompanyId == applicant.CompanyId).FirstOrDefaultAsync();
        string stage = application.CurrentStage?.Trim() ?? string.Empty;
        string candidateStatus = PublicApplicationStatusRules.MapStage(stage);
        return new Result<PublicApplicationStatusDto>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            MethodResult = new PublicApplicationStatusDto
            {
                Status = candidateStatus,
                JobTitle = job?.Title ?? "Applied role",
                CompanyName = company?.CompanyName ?? "Company",
                LastUpdatedAtUtc = application.UpdatedDate ?? application.CreatedDate
            }
        };
    }

    private async Task EnsureApplicationRecords(Applicant applicant, JobVacancy job)
    {
        string applicationKey = $"public-application:{applicant.ApplicantId}:initial";
        var applicationFilter = Builders<RecruitmentJobApplication>.Filter.Eq(x => x.CompanyId, job.CompanyId)
            & Builders<RecruitmentJobApplication>.Filter.Eq(x => x.PublicSubmissionIdempotencyKey, applicationKey);
        var applicationUpdate = Builders<RecruitmentJobApplication>.Update
            .SetOnInsert(x => x.ApplicationId, Guid.NewGuid().ToString())
            .SetOnInsert(x => x.CompanyId, job.CompanyId)
            .SetOnInsert(x => x.ApplicantId, applicant.ApplicantId)
            .SetOnInsert(x => x.VacancyId, job.JobId)
            .SetOnInsert(x => x.CurrentStage, applicant.ActivityType.ToString())
            .SetOnInsert(x => x.PipelineVersion, 1)
            .SetOnInsert(x => x.StageHistory, new List<RecruitmentApplicationStageEvent>
            {
                new()
                {
                    FromStage = "None",
                    ToStage = applicant.ActivityType.ToString(),
                    ActorUserId = applicant.ApplicantId,
                    ChangedAtUtc = applicant.CreatedDate ?? DateTime.UtcNow,
                    Reason = "Initial application submitted from public career portal"
                }
            })
            .SetOnInsert(x => x.PublicSubmissionIdempotencyKey, applicationKey)
            .SetOnInsert(x => x.CreatedBy, applicant.ApplicantId)
            .SetOnInsert(x => x.CreatedDate, applicant.CreatedDate ?? DateTime.UtcNow)
            .SetOnInsert(x => x.UpdatedBy, applicant.ApplicantId)
            .SetOnInsert(x => x.UpdatedDate, applicant.CreatedDate ?? DateTime.UtcNow);
        try
        {
            await _jobApplications.UpdateOneAsync(applicationFilter, applicationUpdate, new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            if (!await _jobApplications.Find(applicationFilter).AnyAsync()) throw;
        }

        string logKey = $"{applicationKey}:activity";
        var logFilter = Builders<ApplicantLogs>.Filter.Eq(x => x.CompanyId, job.CompanyId)
            & Builders<ApplicantLogs>.Filter.Eq(x => x.IdempotencyKey, logKey);
        var logUpdate = Builders<ApplicantLogs>.Update
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
            .SetOnInsert(x => x.CompanyId, job.CompanyId)
            .SetOnInsert(x => x.CreatedBy, applicant.ApplicantId)
            .SetOnInsert(x => x.CreatedDate, applicant.CreatedDate ?? DateTime.UtcNow)
            .SetOnInsert(x => x.ApplicantId, applicant.ApplicantId)
            .SetOnInsert(x => x.UserId, applicant.ApplicantId)
            .SetOnInsert(x => x.ActivityCategory, 0)
            .SetOnInsert(x => x.Description, "New application submitted from public career portal")
            .SetOnInsert(x => x.JobRole, job.Title)
            .SetOnInsert(x => x.ApplicantName, $"{applicant.FirstName} {applicant.LastName}".Trim())
            .SetOnInsert(x => x.EventType, "ApplicationSubmitted")
            .SetOnInsert(x => x.CurrentStage, applicant.ActivityType.ToString())
            .SetOnInsert(x => x.IdempotencyKey, logKey);
        try
        {
            await _applicantLogs.UpdateOneAsync(logFilter, logUpdate, new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            if (!await _applicantLogs.Find(logFilter).AnyAsync()) throw;
        }
    }

    private async Task<PublicJobApplicationResponse> CreateApplicationResponse(Applicant applicant, JobVacancy job, Company company)
    {
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
        return new PublicJobApplicationResponse
        {
            ApplicationReference = applicant.ApplicantId,
            ResumeUploadToken = rawToken,
            TokenExpiresAt = tokenExpiry,
            JobTitle = job.Title,
            CompanyName = company.CompanyName
        };
    }

    public async Task<Result<PublicResumeUploadResult>> UploadResume(string applicationReference, string token, IFormFile resume)
    {
        if (string.IsNullOrWhiteSpace(applicationReference) || string.IsNullOrWhiteSpace(token)) return BadRequest<PublicResumeUploadResult>("Application reference and upload token are required.");
        if (resume is null || resume.Length == 0) return BadRequest<PublicResumeUploadResult>("Resume file is required.");
        if (resume.Length > 5 * 1024 * 1024) return BadRequest<PublicResumeUploadResult>("Resume file must be 5MB or smaller.");
        if (!string.Equals(Path.GetExtension(resume.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)) return BadRequest<PublicResumeUploadResult>("Only PDF resumes are allowed.");

        // Claim the one-use token before writing the file so parallel requests cannot
        // both consume the same credential. The server retains the applicant/file digest
        // so a lost response can be retried safely without attaching a second resume.
        using MemoryStream memory = new();
        await resume.CopyToAsync(memory);
        byte[] bytes = memory.ToArray();
        if (bytes.Length < 4 || Encoding.ASCII.GetString(bytes, 0, 4) != "%PDF") return BadRequest<PublicResumeUploadResult>("Only valid PDF resumes are allowed.");
        var fileSha256 = Convert.ToHexString(SHA256.HashData(bytes));
        var tokenHash = HashToken(token);
        var uploadToken = await _tokens.Find(t => t.ApplicationReference == applicationReference && t.TokenHash == tokenHash && t.Purpose == "ResumeUpload" && !t.IsDeleted).FirstOrDefaultAsync();
        if (uploadToken is null) return BadRequest<PublicResumeUploadResult>("Resume upload token is invalid or expired.");

        var activeApplicant = await _applicants.Find(a =>
                a.CompanyId == uploadToken.CompanyId && a.ApplicantId == uploadToken.ApplicantId &&
                !a.IsDeleted && a.ConsentStatus != "Anonymized")
            .FirstOrDefaultAsync();
        if (!PublicCareerApplicationRules.CanAcceptPublicResumeUpload(activeApplicant))
            return new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status410Gone, Message = "This application can no longer accept resume uploads." };

        if (PublicCareerApplicationRules.IsCompletedResumeUploadReplay(uploadToken, fileSha256))
            return ResumeUploadSucceeded(applicationReference);

        if (uploadToken.IsUsed)
        {
            if (await TryRecoverCompletedResumeUpload(uploadToken, fileSha256))
                return ResumeUploadSucceeded(applicationReference);
            return Conflict<PublicResumeUploadResult>("This resume upload was already completed with a different file.");
        }

        var nowUtc = DateTime.UtcNow;
        if (await TryRecoverCompletedResumeUpload(uploadToken, fileSha256))
            return ResumeUploadSucceeded(applicationReference);
        if (uploadToken.ExpiresAt < nowUtc) return BadRequest<PublicResumeUploadResult>("Resume upload token is invalid or expired.");
        if (PublicCareerApplicationRules.IsResumeUploadClaimActive(uploadToken, nowUtc))
            return Conflict<PublicResumeUploadResult>("This resume upload is still processing. Retry in a few moments.");

        string uploadFolder;
        try
        {
            uploadFolder = RecruitmentResumeStorage.PrivateDirectory;
            Directory.CreateDirectory(uploadFolder);
        }
        catch
        {
            return new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "Private resume storage is not available. Please retry later." };
        }

        string attemptId = Guid.NewGuid().ToString("N");
        string fileName = $"{Guid.NewGuid():N}.pdf";
        string filePath = Path.Combine(uploadFolder, fileName);
        var tokenFilter = Builders<PublicApplicationToken>.Filter;
        var isNotProcessing = tokenFilter.Or(
            tokenFilter.Eq(t => t.IsProcessing, false),
            tokenFilter.Exists(t => t.IsProcessing, false));
        var claimIsAvailable = tokenFilter.Or(
            isNotProcessing,
            tokenFilter.Lte(t => t.ProcessingStartedAtUtc, PublicCareerApplicationRules.ResumeUploadClaimExpiry(nowUtc)));

        var claimed = await _tokens.UpdateOneAsync(
            tokenFilter.And(
                tokenFilter.Eq(t => t.TokenId, uploadToken.TokenId),
                tokenFilter.Eq(t => t.IsUsed, false),
                tokenFilter.Gte(t => t.ExpiresAt, nowUtc),
                claimIsAvailable),
            Builders<PublicApplicationToken>.Update
                .Set(t => t.IsProcessing, true)
                .Set(t => t.ProcessingAttemptId, attemptId)
                .Set(t => t.ProcessingStartedAtUtc, nowUtc)
                .Set(t => t.ProcessingFileName, fileName)
                .Set(t => t.ExpiresAt, uploadToken.ExpiresAt > nowUtc.AddMinutes(30) ? uploadToken.ExpiresAt : nowUtc.AddMinutes(30)));
        if (claimed.ModifiedCount != 1)
        {
            var latestToken = await _tokens.Find(t => t.TokenId == uploadToken.TokenId && !t.IsDeleted).FirstOrDefaultAsync();
            if (latestToken is not null && PublicCareerApplicationRules.IsCompletedResumeUploadReplay(latestToken, fileSha256))
                return ResumeUploadSucceeded(applicationReference);
            if (latestToken is not null && await TryRecoverCompletedResumeUpload(latestToken, fileSha256))
                return ResumeUploadSucceeded(applicationReference);
            return Conflict<PublicResumeUploadResult>("This resume upload is already being handled or was used with a different file.");
        }

        bool resumeLinkedToApplicant = false;
        try
        {
            ResumeScanStatus scanStatus;
            if (_resumeScanner is null) scanStatus = ResumeScanStatus.Unavailable;
            else
            {
                try
                {
                    await using var scanStream = new MemoryStream(bytes, writable: false);
                    var scanResult = await _resumeScanner.ScanAsync(scanStream);
                    scanStatus = scanResult.Status == ResumeScanStatus.Clean &&
                        string.Equals(scanResult.Sha256, fileSha256, StringComparison.OrdinalIgnoreCase)
                            ? ResumeScanStatus.Clean
                            : scanResult.Status == ResumeScanStatus.Infected ? ResumeScanStatus.Infected : ResumeScanStatus.Unavailable;
                }
                catch { scanStatus = ResumeScanStatus.Unavailable; }
            }

            try
            {
                await _applicantLogs.InsertOneAsync(RecruitmentResumeScanAudit.Create(uploadToken.CompanyId, uploadToken.ApplicantId, uploadToken.JobId, "public-career", attemptId, scanStatus));
            }
            catch
            {
                await ReleaseResumeUploadToken(uploadToken.TokenId, attemptId);
                return new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, ErrorCode = RecruitmentErrorCodes.WriteFailed, Message = "The resume scan could not be recorded safely. Please retry later." };
            }
            if (scanStatus != ResumeScanStatus.Clean)
            {
                await ReleaseResumeUploadToken(uploadToken.TokenId, attemptId);
                return scanStatus == ResumeScanStatus.Infected
                    ? new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status422UnprocessableEntity, ErrorCode = RecruitmentErrorCodes.InvalidResume, Message = "This resume could not be accepted. Upload a clean PDF or contact the hiring team." }
                    : new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, ErrorCode = RecruitmentErrorCodes.WriteFailed, Message = "Secure resume scanning is unavailable. Please retry later." };
            }

            await File.WriteAllBytesAsync(filePath, bytes);

            var applicantFilter = Builders<Applicant>.Filter.Eq(a => a.ApplicantId, uploadToken.ApplicantId)
                & Builders<Applicant>.Filter.Eq(a => a.CompanyId, uploadToken.CompanyId)
                & Builders<Applicant>.Filter.Eq(a => a.IsDeleted, false)
                & Builders<Applicant>.Filter.Ne(a => a.ConsentStatus, "Anonymized")
                & (Builders<Applicant>.Filter.Eq(a => a.ResumeUrl, string.Empty)
                    | Builders<Applicant>.Filter.Eq(a => a.ResumeUrl, null));
            var linked = await _applicants.UpdateOneAsync(applicantFilter, Builders<Applicant>.Update
                .Set(a => a.ResumeUrl, $"{RecruitmentResumeStorage.PrivatePrefix}{fileName}")
                .Set(a => a.ResumeScanSha256, fileSha256)
                .Set(a => a.ResumeScanVerdict, nameof(ResumeScanStatus.Clean))
                .Set(a => a.ResumeScannedAtUtc, DateTime.UtcNow)
                .Set(a => a.ResumeQuarantineFileName, null)
                .Set(a => a.ResumeQuarantinedAtUtc, null)
                .Set(a => a.PublicResumeUploadTokenId, uploadToken.TokenId)
                .Set(a => a.PublicResumeUploadSha256, fileSha256)
                .Inc(a => a.Version, 1)
                .Set(a => a.UpdatedDate, DateTime.UtcNow)
                .Set(a => a.UpdatedBy, "public-career"));
            if (linked.ModifiedCount != 1)
            {
                File.Delete(filePath);
                if (await TryRecoverCompletedResumeUpload(uploadToken, fileSha256))
                    return ResumeUploadSucceeded(applicationReference);
                await ReleaseResumeUploadToken(uploadToken.TokenId, attemptId);
                return Conflict<PublicResumeUploadResult>("A resume is already attached to this application. Contact the hiring team if it needs to be replaced.");
            }
            resumeLinkedToApplicant = true;

            var completed = await CompleteResumeUploadToken(uploadToken.TokenId, attemptId, fileSha256, fileName);
            if (completed.ModifiedCount != 1 && !await TryRecoverCompletedResumeUpload(uploadToken, fileSha256))
                return new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "The resume is attached, but upload confirmation is still processing. Retry the same file shortly." };
            return ResumeUploadSucceeded(applicationReference);
        }
        catch
        {
            try
            {
                if (await TryRecoverCompletedResumeUpload(uploadToken, fileSha256))
                    return ResumeUploadSucceeded(applicationReference);
            }
            catch
            {
                // Recovery is best effort here. A still-processing claim expires
                // after its lease so the candidate can retry if Mongo is unavailable.
            }
            try
            {
                if (!resumeLinkedToApplicant && File.Exists(filePath)) File.Delete(filePath);
            }
            catch
            {
                // The file is private and unlinked; cleanup can be handled by the
                // scheduled orphan-file sweep without consuming the candidate token.
            }
            if (!resumeLinkedToApplicant)
            {
                try { await ReleaseResumeUploadToken(uploadToken.TokenId, attemptId); }
                catch
                {
                    // The processing lease provides a bounded recovery path if token
                    // release cannot reach Mongo during a transient failure.
                }
            }
            return new Result<PublicResumeUploadResult> { Success = false, StatusCode = StatusCodes.Status500InternalServerError, Message = "The resume could not be stored. Please retry the upload." };
        }
    }

    private async Task<bool> TryRecoverCompletedResumeUpload(PublicApplicationToken uploadToken, string fileSha256)
    {
        var applicant = await _applicants.Find(a =>
                a.CompanyId == uploadToken.CompanyId &&
                a.ApplicantId == uploadToken.ApplicantId &&
                a.PublicResumeUploadTokenId == uploadToken.TokenId &&
                a.PublicResumeUploadSha256 == fileSha256 &&
                !a.IsDeleted &&
                a.ConsentStatus != "Anonymized" &&
                a.ResumeUrl != null &&
                a.ResumeUrl != string.Empty)
            .FirstOrDefaultAsync();
        if (applicant is null) return false;

        string path;
        try { path = RecruitmentResumeStorage.Resolve(applicant.ResumeUrl); }
        catch { return false; }
        if (!File.Exists(path)) return false;

        var update = await CompleteResumeUploadToken(uploadToken.TokenId, null, fileSha256, Path.GetFileName(path));
        if (update.ModifiedCount == 1) return true;

        var current = await _tokens.Find(t => t.TokenId == uploadToken.TokenId && !t.IsDeleted).FirstOrDefaultAsync();
        return current is not null && PublicCareerApplicationRules.IsCompletedResumeUploadReplay(current, fileSha256);
    }

    private Task<UpdateResult> CompleteResumeUploadToken(string tokenId, string? attemptId, string fileSha256, string fileName)
    {
        var filter = Builders<PublicApplicationToken>.Filter.Eq(t => t.TokenId, tokenId)
            & Builders<PublicApplicationToken>.Filter.Eq(t => t.IsUsed, false);
        if (!string.IsNullOrWhiteSpace(attemptId))
            filter &= Builders<PublicApplicationToken>.Filter.Eq(t => t.ProcessingAttemptId, attemptId);

        return _tokens.UpdateOneAsync(filter, Builders<PublicApplicationToken>.Update
            .Set(t => t.IsUsed, true)
            .Set(t => t.IsProcessing, false)
            .Set(t => t.UsedAt, DateTime.UtcNow)
            .Set(t => t.UploadCompletedAtUtc, DateTime.UtcNow)
            .Set(t => t.UploadSha256, fileSha256)
            .Set(t => t.UploadedFileName, fileName)
            .Set(t => t.ProcessingAttemptId, null)
            .Set(t => t.ProcessingStartedAtUtc, null)
            .Set(t => t.ProcessingFileName, null));
    }

    private Task<UpdateResult> ReleaseResumeUploadToken(string tokenId, string attemptId) => _tokens.UpdateOneAsync(
        t => t.TokenId == tokenId && !t.IsUsed && t.IsProcessing && t.ProcessingAttemptId == attemptId,
        Builders<PublicApplicationToken>.Update
            .Set(t => t.IsProcessing, false)
            .Set(t => t.ProcessingAttemptId, null)
            .Set(t => t.ProcessingStartedAtUtc, null)
            .Set(t => t.ProcessingFileName, null));

    private static Result<PublicResumeUploadResult> ResumeUploadSucceeded(string applicationReference) => new()
    {
        Success = true,
        MethodResult = new PublicResumeUploadResult { ApplicationReference = applicationReference }
    };

    private static Result<T> Conflict<T>(string message) => new()
    {
        Success = false,
        StatusCode = StatusCodes.Status409Conflict,
        ErrorCode = RecruitmentErrorCodes.IdempotencyConflict,
        Message = message
    };

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
            "salary-high" => Builders<JobVacancy>.Sort.Descending(j => j.SalaryMax),
            _ => Builders<JobVacancy>.Sort.Descending(j => j.PublishedAt).Descending(j => j.CreatedDate)
        };
        List<JobVacancy> jobs;
        if (sortName == "relevance" && !string.IsNullOrWhiteSpace(request.Search))
        {
            var query = request.Search.Trim()[..Math.Min(200, request.Search.Trim().Length)];
            var matchingCompanyIds = companies
                .Where(c => c.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CompanyId)
                .ToHashSet(StringComparer.Ordinal);
            jobs = await GetRelevantSortedPage(filter, query, matchingCompanyIds, pageNo, pageSize);
        }
        else if (sortName is "most-saved" or "most-viewed")
            jobs = await GetEngagementSortedPage(filter, sortName, pageNo, pageSize);
        else if (sortName == "deadline")
            jobs = await GetNullLastSortedPage(filter, pageNo, pageSize, j => j.ApplicationDeadline,
                Builders<JobVacancy>.Sort.Ascending(j => j.ApplicationDeadline));
        else if (sortName == "salary-low")
            jobs = await GetNullLastSortedPage(filter, pageNo, pageSize, j => j.SalaryMin,
                Builders<JobVacancy>.Sort.Ascending(j => j.SalaryMin));
        else
            jobs = await _jobs.Find(filter).Sort(sort).Skip((pageNo - 1) * pageSize).Limit(pageSize).ToListAsync();
        var configs = await _recruitmentConfigs.Find(x => companyIds.Contains(x.CompanyId) && !x.IsDeleted).ToListAsync();
        var configByCompany = configs
            .GroupBy(x => x.CompanyId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate).First(),
                StringComparer.Ordinal);
        var page = jobs.Select(j => ToSummary(
            j,
            companyMap[j.CompanyId],
            configByCompany.GetValueOrDefault(j.CompanyId))).ToList();

        return new Result<PublicJobSummaryDto> { Success = true, MethodResults = page, TotalRecords = (int)Math.Min(count, int.MaxValue) };
    }

    private async Task<List<JobVacancy>> GetRelevantSortedPage(
        FilterDefinition<JobVacancy> filter,
        string query,
        HashSet<string> matchingCompanyIds,
        int pageNo,
        int pageSize)
    {
        var pipeline = _jobs.Aggregate()
            .Match(filter)
            .AppendStage<JobVacancy>(BuildRelevanceScoreStage(query, matchingCompanyIds))
            .Sort(new BsonDocumentSortDefinition<JobVacancy>(new BsonDocument
            {
                { "__careerRelevanceScore", -1 },
                { nameof(JobVacancy.PublishedAt), -1 },
                { nameof(JobVacancy.CreatedDate), -1 },
                { "_id", 1 }
            }))
            .Skip((pageNo - 1) * pageSize)
            .Limit(pageSize)
            // The score is an aggregation-only field, not part of the persisted
            // JobVacancy model. Remove it before the typed Mongo deserializer runs.
            .AppendStage<JobVacancy>(BuildRelevanceScoreProjectionStage());

        return await pipeline.ToListAsync();
    }

    internal static BsonDocument BuildRelevanceScoreStage(string query, IEnumerable<string> matchingCompanyIds)
    {
        // JobVacancy uses the default MongoDB convention for its property names;
        // JobId is stored as the BSON _id field via its BsonId attribute.
        string Field(string propertyName) => "$" + propertyName;
        BsonDocument RegexMatch(string input, string term) => new("$regexMatch", new BsonDocument
        {
            { "input", new BsonDocument("$ifNull", new BsonArray { input, "" }) },
            { "regex", Regex.Escape(term) },
            { "options", "i" }
        });
        BsonDocument HasArrayMatch(string propertyName, string term) => new("$gt", new BsonArray
        {
            new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
            {
                { "input", new BsonDocument("$ifNull", new BsonArray { Field(propertyName), new BsonArray() }) },
                { "as", "item" },
                { "cond", RegexMatch("$$item", term) }
            })),
            0
        });

        string firstTerm = Regex.Matches(query, @"[\p{L}\p{N}+#.]+")
            .Select(match => match.Value).FirstOrDefault() ?? query;
        var terms = Regex.Matches(query, @"[\p{L}\p{N}+#.]+")
            .Select(match => match.Value)
            .Where(term => term.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        BsonArray scores = [
            new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Title)), query), 100, 0 }),
            new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Title)), firstTerm), 30, 0 }),
            new BsonDocument("$cond", new BsonArray { new BsonDocument("$in", new BsonArray { Field(nameof(JobVacancy.CompanyId)), new BsonArray(matchingCompanyIds.Select(id => (BsonValue)id)) }), 50, 0 })
        ];
        foreach (var term in terms)
        {
            scores.Add(new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Title)), term), 12, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { HasArrayMatch(nameof(JobVacancy.RequiredSkills), term), 14, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { HasArrayMatch(nameof(JobVacancy.Skills), term), 10, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { HasArrayMatch(nameof(JobVacancy.Keywords), term), 8, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Department)), term), 4, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Summary)), term), 3, 0 }));
            scores.Add(new BsonDocument("$cond", new BsonArray { RegexMatch(Field(nameof(JobVacancy.Description)), term), 2, 0 }));
        }
        return new BsonDocument("$addFields", new BsonDocument("__careerRelevanceScore", new BsonDocument("$add", scores)));
    }

    internal static BsonDocument BuildRelevanceScoreProjectionStage() =>
        new("$project", new BsonDocument("__careerRelevanceScore", 0));

    private async Task<List<JobVacancy>> GetNullLastSortedPage<TValue>(
        FilterDefinition<JobVacancy> filter,
        int pageNo,
        int pageSize,
        System.Linq.Expressions.Expression<Func<JobVacancy, TValue?>> valueSelector,
        SortDefinition<JobVacancy> valueSort)
    {
        // MongoDB sorts null and missing values first for ascending sorts. Put
        // jobs with a known deadline/salary first so the labels match the list.
        var f = Builders<JobVacancy>.Filter;
        var withValue = filter & f.Ne(valueSelector, default(TValue));
        var withoutValue = filter & f.Eq(valueSelector, default(TValue));
        long withValueCount = await _jobs.CountDocumentsAsync(withValue);
        int offset = (pageNo - 1) * pageSize;
        if (offset >= withValueCount)
        {
            return await _jobs.Find(withoutValue)
                .Sort(Builders<JobVacancy>.Sort.Descending(j => j.PublishedAt).Descending(j => j.CreatedDate))
                .Skip((int)Math.Min(offset - withValueCount, int.MaxValue)).Limit(pageSize).ToListAsync();
        }

        int firstPageCount = (int)Math.Min(pageSize, withValueCount - offset);
        var page = await _jobs.Find(withValue).Sort(valueSort).Skip(offset).Limit(firstPageCount).ToListAsync();
        int remaining = pageSize - page.Count;
        if (remaining > 0)
        {
            var noValueJobs = await _jobs.Find(withoutValue)
                .Sort(Builders<JobVacancy>.Sort.Descending(j => j.PublishedAt).Descending(j => j.CreatedDate))
                .Limit(remaining).ToListAsync();
            page.AddRange(noValueJobs);
        }
        return page;
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
        var recruitmentConfig = await _recruitmentConfigs.Find(x => x.CompanyId == job.CompanyId && !x.IsDeleted).FirstOrDefaultAsync();
        return new Result<PublicJobDetailsDto> { Success = true, MethodResult = ToDetails(job, company, recruitmentConfig) };
    }

    private static PublicJobSummaryDto ToSummary(JobVacancy job, Company company, RecruitmentEnterpriseConfig? recruitmentConfig = null)
    {
        bool closed = IsClosed(job, out string? reason);
        string applicationMode = NormalizeApplicationMode(job.ApplicationMode);
        string? privacyNotice = string.IsNullOrWhiteSpace(recruitmentConfig?.CandidatePrivacyNoticeText) ? null : recruitmentConfig.CandidatePrivacyNoticeText.Trim();
        string? privacyVersion = string.IsNullOrWhiteSpace(recruitmentConfig?.CandidatePrivacyNoticeVersion) ? null : recruitmentConfig.CandidatePrivacyNoticeVersion.Trim();
        bool canApply = PublicCareerApplicationRules.CanAcceptApplications(!closed, applicationMode, privacyNotice, privacyVersion);
        if (!closed && !canApply) reason = "This company is still setting up its candidate privacy notice.";
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
            ApplicationMode = applicationMode,
            ExternalApplicationUrl = IsSafeExternalUrl(job.ExternalApplicationUrl) ? job.ExternalApplicationUrl : null,
            CanApply = canApply,
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

    private static PublicJobDetailsDto ToDetails(JobVacancy job, Company company, RecruitmentEnterpriseConfig? recruitmentConfig)
    {
        var summary = ToSummary(job, company, recruitmentConfig);
        string? privacyNotice = string.IsNullOrWhiteSpace(recruitmentConfig?.CandidatePrivacyNoticeText) ? null : recruitmentConfig.CandidatePrivacyNoticeText.Trim();
        string? privacyVersion = string.IsNullOrWhiteSpace(recruitmentConfig?.CandidatePrivacyNoticeVersion) ? null : recruitmentConfig.CandidatePrivacyNoticeVersion.Trim();
        bool canApply = summary.CanApply;
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
            CanApply = canApply,
            ApplicationClosedReason = summary.ApplicationClosedReason,
            CompanyPublicCode = summary.CompanyPublicCode,
            CompanyName = summary.CompanyName,
            CompanySlug = summary.CompanySlug,
            CompanyLogo = summary.CompanyLogo,
            Description = job.Description,
            CandidatePrivacyNoticeText = privacyNotice,
            CandidatePrivacyNoticeVersion = privacyVersion,
            CandidateSourceOptions = PublicCareerApplicationRules.EffectiveCandidateSourceOptions(recruitmentConfig?.CandidateSourceOptions).ToList(),
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
        return PublicCareerApplicationRules.IsSafeExternalUrl(value);
    }

    private static string? ValidateApplicationRequest(PublicJobApplicationRequest? request)
    {
        return PublicCareerApplicationRules.ValidateApplicationRequest(request);
    }

    private async Task QueueApplicationNotifications(Applicant applicant, JobVacancy job, Company company)
    {
        string candidateName = $"{applicant.FirstName} {applicant.LastName}".Trim();
        RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.ApplyNowMailToApplicant, out var applicantTemplateSubject, out var applicantTemplateBody);
        string applicantSubject = string.IsNullOrWhiteSpace(applicantTemplateSubject) ? $"We received your application for {job.Title}" : applicantTemplateSubject;
        string applicantBody = string.IsNullOrWhiteSpace(applicantTemplateBody) ?
            $"<p>Hi {System.Net.WebUtility.HtmlEncode(candidateName)},</p><p>Thank you for applying for the <strong>{System.Net.WebUtility.HtmlEncode(job.Title)}</strong> position at {System.Net.WebUtility.HtmlEncode(company.CompanyName)}. Our hiring team will review your application and contact you with an update.</p>" : applicantTemplateBody;

        applicantSubject = HtmlTemplate.Render(applicantSubject, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName });
        applicantBody = HtmlTemplate.Render(applicantBody, new { CandidateName = candidateName, JobTitle = job.Title, CompanyName = company.CompanyName, ApplicationReference = applicant.ApplicantId });
        applicantBody += $"<p>Your application reference is <strong>{System.Net.WebUtility.HtmlEncode(applicant.ApplicantId)}</strong>. Keep it safe; you will need it with your email to check your application status.</p>";

        RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.ApplyNowMailToHR, out var hrTemplateSubject, out var hrTemplateBody);
        string hrSubject = HtmlTemplate.Render(
            string.IsNullOrWhiteSpace(hrTemplateSubject) ? $"New application received for {job.Title}" : hrTemplateSubject,
            new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = job.Title, CompanyName = company.CompanyName });
        string hrBody = HtmlTemplate.Render(
            string.IsNullOrWhiteSpace(hrTemplateBody) ?
            "<p>A new application has been submitted.</p><ul><li><strong>Name:</strong> [CandidateName]</li><li><strong>Email:</strong> [CandidateEmail]</li><li><strong>Phone:</strong> [CandidatePhone]</li><li><strong>Job Title:</strong> [JobTitle]</li><li><strong>Company:</strong> [CompanyName]</li></ul>" : hrTemplateBody,
            new { CandidateName = candidateName, CandidateEmail = applicant.Email, CandidatePhone = applicant.Phone, JobTitle = job.Title, CompanyName = company.CompanyName });

        var internalRecipients = await GetRecruitmentRecipients(job.CompanyId, job.RecruiterContactEmail);

        await QueueApplicationEmail(
            job.CompanyId,
            $"public-application:{applicant.ApplicantId}:candidate",
            "ApplicationReceivedCandidate",
            applicant.Email,
            applicant.ApplicantId,
            null,
            applicantSubject,
            applicantBody);
        foreach (var recipient in internalRecipients)
        {
            await QueueApplicationEmail(
                job.CompanyId,
                $"public-application:{applicant.ApplicantId}:hr:{HashToken(recipient.Email.Trim().ToLowerInvariant())}",
                "ApplicationReceivedRecruiter",
                recipient.Email,
                applicant.ApplicantId,
                recipient.UserId,
                hrSubject,
                hrBody);
        }
    }

    private async Task QueueApplicationEmail(
        string companyId,
        string dedupeKey,
        string eventType,
        string recipientEmail,
        string applicantId,
        string? recipientUserId,
        string subject,
        string htmlBody)
    {
        try
        {
            await _recruitmentEmailOutbox.InsertOneAsync(new RecruitmentEmailOutbox
            {
                CompanyId = companyId,
                RelatedApplicantId = applicantId,
                DedupeKey = dedupeKey,
                EventType = eventType,
                RecipientEmail = recipientEmail,
                RecipientUserId = recipientUserId,
                Subject = subject,
                HtmlBody = htmlBody,
                AvailableAtUtc = DateTime.UtcNow,
                CreatedBy = "public-career",
                UpdatedBy = "public-career"
            });
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            bool alreadyQueued = await _recruitmentEmailOutbox
                .Find(x => x.CompanyId == companyId && x.DedupeKey == dedupeKey)
                .AnyAsync();
            if (!alreadyQueued) throw;
        }
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
