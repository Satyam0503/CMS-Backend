using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.CareerPortal.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.CareerPortal;

public sealed class CareerEngagementService : ICareerEngagementService
{
    private static readonly HashSet<string> Frequencies = new(StringComparer.OrdinalIgnoreCase)
        { "Instant", "DailyDigest", "WeeklyDigest", "None" };
    private static readonly HashSet<string> AnalyticsEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            "JobViewed", "CompanyViewed", "JobSaved", "JobUnsaved", "CompanyFollowed",
            "CompanyUnfollowed", "SearchPerformed", "FilterApplied", "ApplyClicked",
            "ExternalApplyClicked", "SubscriptionStarted", "SubscriptionVerified"
        };
    private readonly IMongoCollection<SavedJob> _saved;
    private readonly IMongoCollection<JobVacancy> _jobs;
    private readonly IMongoCollection<Company> _companies;
    private readonly IMongoCollection<CompanyFollower> _followers;
    private readonly IMongoCollection<JobAlertSubscription> _alerts;
    private readonly IMongoCollection<CareerAnalyticsEvent> _analytics;
    private readonly IMongoCollection<CareerVisitorPreference> _visitors;
    private readonly ICareerSubscriptionService _subscriptions;

    public CareerEngagementService(
        IMongoDbRepository<SavedJob> savedRepository,
        IMongoDbRepository<JobVacancy> jobRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<CompanyFollower> followerRepository,
        IMongoDbRepository<JobAlertSubscription> alertRepository,
        IMongoDbRepository<CareerAnalyticsEvent> analyticsRepository,
        IMongoDbRepository<CareerVisitorPreference> visitorRepository,
        ICareerSubscriptionService subscriptions)
    {
        _saved = savedRepository.GetCollection();
        _jobs = jobRepository.GetCollection();
        _companies = companyRepository.GetCollection();
        _followers = followerRepository.GetCollection();
        _alerts = alertRepository.GetCollection();
        _analytics = analyticsRepository.GetCollection();
        _visitors = visitorRepository.GetCollection();
        _subscriptions = subscriptions;
    }

    public async Task<Result> SaveJob(string publicJobId, string sessionToken, string visitorToken)
    {
        var job = await EligibleJob(publicJobId);
        if (job is null) return NotFound();
        var subscriber = await _subscriptions.Authorize(sessionToken);
        var visitorHash = subscriber is null ? VisitorHash(visitorToken) : null;
        if (subscriber is null && visitorHash is null) return Unauthorized();

        var identityFilter = subscriber is not null
            ? Builders<SavedJob>.Filter.Eq(x => x.SubscriberId, subscriber.SubscriberId)
            : Builders<SavedJob>.Filter.Eq(x => x.AnonymousVisitorIdHash, visitorHash);
        var filter = identityFilter & Builders<SavedJob>.Filter.Eq(x => x.JobId, job.JobId);
        var update = Builders<SavedJob>.Update
            .SetOnInsert(x => x.SubscriberId, subscriber?.SubscriberId)
            .SetOnInsert(x => x.AnonymousVisitorIdHash, visitorHash)
            .SetOnInsert(x => x.JobId, job.JobId)
            .SetOnInsert(x => x.PublicJobId, job.PublicJobId)
            .SetOnInsert(x => x.CompanyId, job.CompanyId)
            .Set(x => x.SavedAt, DateTime.UtcNow)
            .Set(x => x.IsActive, true);
        await _saved.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        await TryTrack("JobSaved", visitorHash, subscriber?.SubscriberId, job.PublicJobId, null);
        return Ok("Job saved.");
    }

    public async Task<Result> RemoveSavedJob(string publicJobId, string sessionToken, string visitorToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        var visitorHash = subscriber is null ? VisitorHash(visitorToken) : null;
        if (subscriber is null && visitorHash is null) return Unauthorized();
        var identity = subscriber is not null
            ? Builders<SavedJob>.Filter.Eq(x => x.SubscriberId, subscriber.SubscriberId)
            : Builders<SavedJob>.Filter.Eq(x => x.AnonymousVisitorIdHash, visitorHash);
        await _saved.UpdateManyAsync(
            identity & Builders<SavedJob>.Filter.Eq(x => x.PublicJobId, publicJobId),
            Builders<SavedJob>.Update.Set(x => x.IsActive, false));
        await TryTrack("JobUnsaved", visitorHash, subscriber?.SubscriberId, publicJobId, null);
        return Ok("Job removed from saved jobs.");
    }

    public async Task<Result<SavedCareerJobDto>> GetSavedJobs(string sessionToken, string visitorToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        var visitorHash = subscriber is null ? VisitorHash(visitorToken) : null;
        if (subscriber is null && visitorHash is null) return Unauthorized<SavedCareerJobDto>();
        var identity = subscriber is not null
            ? Builders<SavedJob>.Filter.Eq(x => x.SubscriberId, subscriber.SubscriberId)
            : Builders<SavedJob>.Filter.Eq(x => x.AnonymousVisitorIdHash, visitorHash);
        var saved = await _saved.Find(identity & Builders<SavedJob>.Filter.Eq(x => x.IsActive, true))
            .SortByDescending(x => x.SavedAt).ToListAsync();
        var jobs = await _jobs.Find(x => saved.Select(s => s.JobId).Contains(x.JobId)).ToListAsync();
        var companies = await _companies.Find(x => jobs.Select(j => j.CompanyId).Contains(x.CompanyId)).ToListAsync();
        var jobMap = jobs.ToDictionary(x => x.JobId);
        var companyMap = companies.ToDictionary(x => x.CompanyId);
        var result = saved.Where(s => jobMap.ContainsKey(s.JobId) && companyMap.ContainsKey(s.CompanyId))
            .Select(s =>
            {
                var j = jobMap[s.JobId];
                var c = companyMap[s.CompanyId];
                return new SavedCareerJobDto
                {
                    PublicJobId = j.PublicJobId, Slug = j.Slug, Title = j.Title,
                    CompanyCode = c.PublicCompanyCode, CompanyName = c.CompanyName,
                    Locations = string.IsNullOrWhiteSpace(j.Location) ? [] : [j.Location],
                    SavedAt = s.SavedAt, ApplicationDeadline = j.ApplicationDeadline
                };
            }).ToList();
        return new Result<SavedCareerJobDto> { MethodResults = result, TotalRecords = result.Count };
    }

    public async Task<Result> MergeAnonymousSaves(string sessionToken, string visitorToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        var visitorHash = VisitorHash(visitorToken);
        if (subscriber is null || visitorHash is null) return Unauthorized();
        var anonymous = await _saved.Find(x => x.AnonymousVisitorIdHash == visitorHash && x.IsActive).ToListAsync();
        foreach (var item in anonymous)
        {
            var filter = Builders<SavedJob>.Filter.Eq(x => x.SubscriberId, subscriber.SubscriberId) &
                         Builders<SavedJob>.Filter.Eq(x => x.JobId, item.JobId);
            var update = Builders<SavedJob>.Update
                .SetOnInsert(x => x.SubscriberId, subscriber.SubscriberId)
                .SetOnInsert(x => x.JobId, item.JobId).SetOnInsert(x => x.PublicJobId, item.PublicJobId)
                .SetOnInsert(x => x.CompanyId, item.CompanyId).Set(x => x.SavedAt, item.SavedAt)
                .Set(x => x.IsActive, true);
            await _saved.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
        await _saved.UpdateManyAsync(x => x.AnonymousVisitorIdHash == visitorHash,
            Builders<SavedJob>.Update.Set(x => x.IsActive, false));
        return Ok("Anonymous saved jobs merged.");
    }

    public async Task<Result> FollowCompany(string companyCode, string sessionToken, FollowCompanyRequest request)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized();
        var company = await EligibleCompany(companyCode);
        if (company is null) return NotFound();
        var filter = Builders<CompanyFollower>.Filter.Eq(x => x.SubscriberId, subscriber.SubscriberId) &
                     Builders<CompanyFollower>.Filter.Eq(x => x.CompanyId, company.CompanyId);
        var update = Builders<CompanyFollower>.Update
            .SetOnInsert(x => x.SubscriberId, subscriber.SubscriberId)
            .SetOnInsert(x => x.CompanyId, company.CompanyId)
            .SetOnInsert(x => x.PublicCompanyCode, company.PublicCompanyCode)
            .Set(x => x.NotifyForNewJobs, request.NotifyForNewJobs)
            .Set(x => x.NotifyForJobUpdates, request.NotifyForJobUpdates)
            .Set(x => x.IsActive, true).Set(x => x.FollowedAt, DateTime.UtcNow)
            .Set(x => x.UnfollowedAt, null);
        await _followers.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        await TryTrack("CompanyFollowed", null, subscriber.SubscriberId, null, company.PublicCompanyCode);
        return Ok("Company followed.");
    }

    public async Task<Result> UnfollowCompany(string companyCode, string sessionToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized();
        await _followers.UpdateManyAsync(x =>
            x.SubscriberId == subscriber.SubscriberId && x.PublicCompanyCode == Digits(companyCode),
            Builders<CompanyFollower>.Update.Set(x => x.IsActive, false).Set(x => x.UnfollowedAt, DateTime.UtcNow));
        await TryTrack("CompanyUnfollowed", null, subscriber.SubscriberId, null, Digits(companyCode));
        return Ok("Company unfollowed.");
    }

    public async Task<Result<CompanyFollowStatusDto>> GetFollowStatus(string companyCode, string sessionToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized<CompanyFollowStatusDto>();
        var follower = await _followers.Find(x =>
            x.SubscriberId == subscriber.SubscriberId && x.PublicCompanyCode == Digits(companyCode)).FirstOrDefaultAsync();
        return new Result<CompanyFollowStatusDto>
        {
            MethodResult = new CompanyFollowStatusDto
            {
                IsFollowing = follower?.IsActive == true,
                NotifyForNewJobs = follower?.NotifyForNewJobs == true,
                NotifyForJobUpdates = follower?.NotifyForJobUpdates == true
            },
            TotalRecords = 1
        };
    }

    public async Task<Result<JobAlertDto>> CreateAlert(string sessionToken, JobAlertRequest request)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized<JobAlertDto>();
        var error = ValidateAlert(request);
        if (error is not null) return BadRequest<JobAlertDto>(error);
        var alert = NewAlert(subscriber.SubscriberId, request);
        await _alerts.InsertOneAsync(alert);
        return new Result<JobAlertDto> { MethodResult = Map(alert), TotalRecords = 1, StatusCode = StatusCodes.Status201Created };
    }

    public async Task<Result<JobAlertDto>> GetAlerts(string sessionToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized<JobAlertDto>();
        var alerts = await _alerts.Find(x => x.SubscriberId == subscriber.SubscriberId && x.IsActive).ToListAsync();
        return new Result<JobAlertDto> { MethodResults = alerts.Select(Map).ToList(), TotalRecords = alerts.Count };
    }

    public async Task<Result<JobAlertDto>> UpdateAlert(string alertId, string sessionToken, JobAlertRequest request)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized<JobAlertDto>();
        var error = ValidateAlert(request);
        if (error is not null) return BadRequest<JobAlertDto>(error);
        var alert = await _alerts.Find(x => x.JobAlertSubscriptionId == alertId &&
            x.SubscriberId == subscriber.SubscriberId && x.IsActive).FirstOrDefaultAsync();
        if (alert is null) return new Result<JobAlertDto> { Success = false, StatusCode = 404, Message = "Job alert not found." };
        ApplyAlert(alert, request);
        alert.UpdatedAt = DateTime.UtcNow;
        await _alerts.ReplaceOneAsync(x => x.JobAlertSubscriptionId == alert.JobAlertSubscriptionId, alert);
        return new Result<JobAlertDto> { MethodResult = Map(alert), TotalRecords = 1 };
    }

    public async Task<Result> DeleteAlert(string alertId, string sessionToken)
    {
        var subscriber = await _subscriptions.Authorize(sessionToken);
        if (subscriber is null) return Unauthorized();
        await _alerts.UpdateOneAsync(x => x.JobAlertSubscriptionId == alertId && x.SubscriberId == subscriber.SubscriberId,
            Builders<JobAlertSubscription>.Update.Set(x => x.IsActive, false).Set(x => x.UpdatedAt, DateTime.UtcNow));
        return Ok("Job alert removed.");
    }

    public async Task<Result> Track(CareerAnalyticsRequest request)
    {
        if (!AnalyticsEvents.Contains(request.EventType)) return BadRequest("Unsupported analytics event.");
        await _analytics.InsertOneAsync(new CareerAnalyticsEvent
        {
            EventType = request.EventType,
            VisitorIdHash = VisitorHash(request.VisitorToken ?? ""),
            PublicJobId = request.PublicJobId,
            PublicCompanyCode = string.IsNullOrWhiteSpace(request.PublicCompanyCode) ? null : Digits(request.PublicCompanyCode),
            CreatedAt = DateTime.UtcNow
        });
        return Ok();
    }

    private async Task TryTrack(string eventType, string? visitorHash, string? subscriberId,
        string? publicJobId, string? companyCode)
    {
        try
        {
            await _analytics.InsertOneAsync(new CareerAnalyticsEvent
            {
                EventType = eventType, VisitorIdHash = visitorHash, SubscriberId = subscriberId,
                PublicJobId = publicJobId, PublicCompanyCode = companyCode, CreatedAt = DateTime.UtcNow
            });
        }
        catch { }
    }

    public async Task<Result> UpdateVisitorPreference(VisitorPreferenceRequest request)
    {
        var hash = VisitorHash(request.VisitorToken);
        if (hash is null) return BadRequest("A valid visitor token is required.");
        var now = DateTime.UtcNow;
        var update = Builders<CareerVisitorPreference>.Update
            .SetOnInsert(x => x.VisitorIdHash, hash).SetOnInsert(x => x.CreatedAt, now)
            .Set(x => x.UpdatedAt, now).Set(x => x.LastVisitedAt, now)
            .Set(x => x.LastVisitedCompanyCode,
                string.IsNullOrWhiteSpace(request.LastVisitedCompanyCode) ? null : Digits(request.LastVisitedCompanyCode));
        if (request.DismissSubscribePopup)
            update = update.Set(x => x.SubscribePopupDismissedAt, now)
                .Set(x => x.DoNotShowUntil, now.AddDays(Math.Clamp(request.SuppressDays ?? 7, 1, 180)))
                .Inc(x => x.PopupDisplayCount, 1);
        await _visitors.UpdateOneAsync(x => x.VisitorIdHash == hash, update, new UpdateOptions { IsUpsert = true });
        return Ok();
    }

    public async Task<Result<VisitorPreferenceDto>> GetVisitorPreference(string visitorToken)
    {
        var hash = VisitorHash(visitorToken);
        if (hash is null) return BadRequest<VisitorPreferenceDto>("A valid visitor token is required.");
        var value = await _visitors.Find(x => x.VisitorIdHash == hash).FirstOrDefaultAsync();
        return new Result<VisitorPreferenceDto>
        {
            MethodResult = new VisitorPreferenceDto
            {
                SuppressSubscribePopup = value?.DoNotShowUntil > DateTime.UtcNow,
                DoNotShowUntil = value?.DoNotShowUntil,
                PopupDisplayCount = value?.PopupDisplayCount ?? 0
            },
            TotalRecords = 1
        };
    }

    private async Task<JobVacancy?> EligibleJob(string publicJobId)
    {
        var now = DateTime.UtcNow;
        var job = await _jobs.Find(x => x.PublicJobId == publicJobId && x.Status && !x.IsDeleted &&
            x.PublishToCareerPortal && (x.ExpiresAt == null || x.ExpiresAt >= now) &&
            (x.ApplicationDeadline == null || x.ApplicationDeadline >= now)).FirstOrDefaultAsync();
        return job is not null && await _companies.Find(x => x.CompanyId == job.CompanyId && x.Status &&
            !x.IsDeleted && x.CareerPortalEnabled).AnyAsync() ? job : null;
    }

    private Task<Company?> EligibleCompany(string code) => _companies.Find(x =>
        x.PublicCompanyCode == Digits(code) && x.Status && !x.IsDeleted && x.CareerPortalEnabled).FirstOrDefaultAsync();
    private static string? ValidateAlert(JobAlertRequest r) =>
        string.IsNullOrWhiteSpace(r.Name) ? "Alert name is required." :
        !Frequencies.Contains(r.NotificationFrequency) ? "Invalid notification frequency." : null;
    private static JobAlertSubscription NewAlert(string subscriberId, JobAlertRequest r)
    {
        var alert = new JobAlertSubscription { SubscriberId = subscriberId, CreatedAt = DateTime.UtcNow };
        ApplyAlert(alert, r);
        return alert;
    }
    private static void ApplyAlert(JobAlertSubscription a, JobAlertRequest r)
    {
        a.Name = r.Name.Trim()[..Math.Min(100, r.Name.Trim().Length)];
        a.SearchText = r.SearchText?.Trim();
        a.Locations = Clean(r.Locations); a.EmploymentTypes = Clean(r.EmploymentTypes);
        a.WorkplaceTypes = Clean(r.WorkplaceTypes); a.JobTypes = r.JobTypes.Distinct().ToList();
        a.Industries = Clean(r.Industries); a.Departments = Clean(r.Departments); a.Skills = Clean(r.Skills);
        a.ExperienceMin = r.ExperienceMin; a.ExperienceMax = r.ExperienceMax;
        a.SalaryMin = r.SalaryMin; a.Currency = r.Currency?.Trim().ToUpperInvariant();
        a.NotificationFrequency = Frequencies.First(x => x.Equals(r.NotificationFrequency, StringComparison.OrdinalIgnoreCase));
        a.IsActive = true;
    }
    private static JobAlertDto Map(JobAlertSubscription a) => new()
    {
        JobAlertSubscriptionId = a.JobAlertSubscriptionId, Name = a.Name, SearchText = a.SearchText,
        Locations = a.Locations, EmploymentTypes = a.EmploymentTypes, WorkplaceTypes = a.WorkplaceTypes,
        JobTypes = a.JobTypes, Industries = a.Industries, Departments = a.Departments, Skills = a.Skills,
        ExperienceMin = a.ExperienceMin, ExperienceMax = a.ExperienceMax, SalaryMin = a.SalaryMin,
        Currency = a.Currency, NotificationFrequency = a.NotificationFrequency, IsActive = a.IsActive
    };
    private static List<string> Clean(IEnumerable<string> values) =>
        values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToList();
    private static string Digits(string value) => Regex.Replace(value ?? "", @"\D", "");
    private static string? VisitorHash(string token) =>
        string.IsNullOrWhiteSpace(token) || token.Length < 16 || token.Length > 200 ? null : Hash(token);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Result Ok(string message = "") => new() { Success = true, Message = message };
    private static Result NotFound() => new() { Success = false, StatusCode = 404, Message = "Public job or company not found." };
    private static Result Unauthorized() => new() { Success = false, StatusCode = 401, Message = "A valid career session or visitor token is required." };
    private static Result<T> Unauthorized<T>() => new() { Success = false, StatusCode = 401, Message = "A valid career session or visitor token is required." };
    private static Result BadRequest(string message) => new() { Success = false, StatusCode = 400, Message = message };
    private static Result<T> BadRequest<T>(string message) => new() { Success = false, StatusCode = 400, Message = message };
}
