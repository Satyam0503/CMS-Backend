using System.Net;
using System.Text.Json;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.CareerPortal;

public sealed class CareerNotificationService : ICareerNotificationService
{
    private const int MaxAttempts = 5;
    private static long _lastDeadlineScanTicks;
    private readonly IMongoCollection<CareerNotificationOutbox> _outbox;
    private readonly IMongoCollection<CareerNotificationDelivery> _deliveries;
    private readonly IMongoCollection<CareerSubscriber> _subscribers;
    private readonly IMongoCollection<CompanyFollower> _followers;
    private readonly IMongoCollection<JobAlertSubscription> _alerts;
    private readonly IMongoCollection<Company> _companies;
    private readonly IMongoCollection<JobVacancy> _jobs;
    private readonly IMongoCollection<SavedJob> _savedJobs;
    private readonly IMiddlewareService _mail;

    public CareerNotificationService(
        IMongoDbRepository<CareerNotificationOutbox> outboxRepository,
        IMongoDbRepository<CareerNotificationDelivery> deliveryRepository,
        IMongoDbRepository<CareerSubscriber> subscriberRepository,
        IMongoDbRepository<CompanyFollower> followerRepository,
        IMongoDbRepository<JobAlertSubscription> alertRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<JobVacancy> jobRepository,
        IMongoDbRepository<SavedJob> savedJobRepository,
        IMiddlewareService mail)
    {
        _outbox = outboxRepository.GetCollection();
        _deliveries = deliveryRepository.GetCollection();
        _subscribers = subscriberRepository.GetCollection();
        _followers = followerRepository.GetCollection();
        _alerts = alertRepository.GetCollection();
        _companies = companyRepository.GetCollection();
        _jobs = jobRepository.GetCollection();
        _savedJobs = savedJobRepository.GetCollection();
        _mail = mail;
    }

    public async Task HandleJobSaved(JobVacancy? previous, JobVacancy current)
    {
        var company = await _companies.Find(c => c.CompanyId == current.CompanyId).FirstOrDefaultAsync();
        if (company is null) return;
        var now = DateTime.UtcNow;
        var companyVisible = Eligible(current, company, now, false);
        var masterVisible = Eligible(current, company, now, true);
        var previousCompanyVisible = previous is not null && Eligible(previous, company, now, false);
        var previousMasterVisible = previous is not null && Eligible(previous, company, now, true);

        if (!previousCompanyVisible && companyVisible)
            await QueueNewJob(current, company, masterVisible);
        else if (previousCompanyVisible && companyVisible && PublicFieldsChanged(previous!, current))
            await QueueJobUpdate(current, company, masterVisible && previousMasterVisible);
    }

    public async Task ProcessNext(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await QueueSavedDeadlineReminders(now, cancellationToken);
        var claimable = Builders<CareerNotificationOutbox>.Filter.And(
            Builders<CareerNotificationOutbox>.Filter.Lte(x => x.AvailableAt, now),
            Builders<CareerNotificationOutbox>.Filter.Or(
                Builders<CareerNotificationOutbox>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }),
                Builders<CareerNotificationOutbox>.Filter.And(
                    Builders<CareerNotificationOutbox>.Filter.Eq(x => x.Status, "Processing"),
                    Builders<CareerNotificationOutbox>.Filter.Lt(x => x.ProcessingStartedAt, now.AddMinutes(-10)))));
        var item = await _outbox.FindOneAndUpdateAsync(
            claimable,
            Builders<CareerNotificationOutbox>.Update
                .Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAt, now).Set(x => x.UpdatedAt, now)
                .Inc(x => x.AttemptCount, 1),
            new FindOneAndUpdateOptions<CareerNotificationOutbox>
            {
                Sort = Builders<CareerNotificationOutbox>.Sort.Ascending(x => x.AvailableAt),
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
        if (item is null) return;

        var processingItems = new List<CareerNotificationOutbox> { item };
        try
        {
            var subscriber = await _subscribers.Find(x => x.SubscriberId == item.SubscriberId &&
                x.IsActive && x.IsEmailVerified && x.NotificationFrequency != "None").FirstOrDefaultAsync(cancellationToken);
            if (subscriber is null)
            {
                await _outbox.UpdateOneAsync(x => x.NotificationId == item.NotificationId,
                    Builders<CareerNotificationOutbox>.Update.Set(x => x.Status, "Cancelled").Set(x => x.UpdatedAt, now),
                    cancellationToken: cancellationToken);
                return;
            }

            if (subscriber.NotificationFrequency.EndsWith("Digest", StringComparison.OrdinalIgnoreCase))
                processingItems.AddRange(await ClaimDigestItems(item.SubscriberId, now, cancellationToken));

            var pending = new List<(CareerNotificationOutbox Item, JobNotificationPayload Payload)>();
            foreach (var candidate in processingItems)
            {
                if (await _deliveries.Find(x => x.SubscriberId == candidate.SubscriberId &&
                    x.JobId == candidate.JobId && x.NotificationReason == candidate.NotificationType).AnyAsync(cancellationToken))
                {
                    await MarkSent(candidate.NotificationId, now, cancellationToken);
                    continue;
                }
                var payload = JsonSerializer.Deserialize<JobNotificationPayload>(candidate.PayloadJson)
                              ?? throw new InvalidOperationException("Notification payload is invalid.");
                pending.Add((candidate, payload));
            }
            if (pending.Count == 0) return;

            var digest = subscriber.NotificationFrequency.EndsWith("Digest", StringComparison.OrdinalIgnoreCase);
            var result = await _mail.EmailSendAndSaveWithResult(new EmpEmailLogs
            {
                CompanyId = item.CompanyId ?? string.Empty,
                UserTo = subscriber.SubscriberId,
                UserFrom = "career-portal",
                Email = subscriber.Email,
                Subject = digest
                    ? $"{(subscriber.NotificationFrequency.StartsWith("Weekly", StringComparison.OrdinalIgnoreCase) ? "Weekly" : "Daily")} job digest: {pending.Count} opportunities"
                    : item.NotificationType == "JobUpdated"
                    ? $"Updated opportunity: {pending[0].Payload.JobTitle}"
                    : item.NotificationType == "SavedJobDeadlineReminder"
                    ? $"Application deadline approaching: {pending[0].Payload.JobTitle}"
                    : $"New opportunity at {pending[0].Payload.CompanyName}: {pending[0].Payload.JobTitle}",
                Body = digest
                    ? BuildDigestBody(pending.Select(x => x.Payload), subscriber.SubscriberId)
                    : BuildBody(pending[0].Payload, subscriber.SubscriberId),
                EmailLogType = MailType.CareerPortal
            });
            if (!result.IsSent) throw new InvalidOperationException(result.Error);

            foreach (var delivered in pending)
            {
                try
                {
                    await _deliveries.InsertOneAsync(new CareerNotificationDelivery
                    {
                        SubscriberId = delivered.Item.SubscriberId,
                        JobId = delivered.Item.JobId ?? "",
                        CompanyId = delivered.Item.CompanyId ?? "",
                        NotificationReason = delivered.Item.NotificationType,
                        Channel = "Email", SentAt = now, Status = "Sent"
                    }, cancellationToken: cancellationToken);
                }
                catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey) { }
                await MarkSent(delivered.Item.NotificationId, now, cancellationToken);
            }
            await _subscribers.UpdateOneAsync(x => x.SubscriberId == subscriber.SubscriberId,
                Builders<CareerSubscriber>.Update.Set(x => x.LastNotificationAt, now), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            foreach (var failed in processingItems)
            {
                var dead = failed.AttemptCount >= MaxAttempts;
                var delay = TimeSpan.FromMinutes(Math.Pow(2, Math.Min(failed.AttemptCount, 6)));
                await _outbox.UpdateOneAsync(x => x.NotificationId == failed.NotificationId && x.Status == "Processing",
                    Builders<CareerNotificationOutbox>.Update
                        .Set(x => x.Status, dead ? "DeadLetter" : "Failed")
                        .Set(x => x.AvailableAt, now.Add(delay))
                        .Set(x => x.LastError, ex.Message[..Math.Min(1000, ex.Message.Length)])
                        .Set(x => x.UpdatedAt, now),
                    cancellationToken: cancellationToken);
            }
        }
    }

    private async Task QueueSavedDeadlineReminders(DateTime now, CancellationToken cancellationToken)
    {
        var previousTicks = Interlocked.Read(ref _lastDeadlineScanTicks);
        if (previousTicks != 0 && new DateTime(previousTicks, DateTimeKind.Utc) > now.AddHours(-1)) return;
        if (Interlocked.CompareExchange(ref _lastDeadlineScanTicks, now.Ticks, previousTicks) != previousTicks) return;

        var jobs = await _jobs.Find(x => x.Status && !x.IsDeleted && x.PublishToCareerPortal &&
                x.ApplicationDeadline > now && x.ApplicationDeadline <= now.AddDays(2))
            .ToListAsync(cancellationToken);
        if (jobs.Count == 0) return;
        var jobMap = jobs.ToDictionary(x => x.JobId);
        var jobIds = jobMap.Keys.ToList();
        var companyIds = jobs.Select(x => x.CompanyId).Distinct().ToList();
        var saves = await _savedJobs.Find(x => x.IsActive && x.SubscriberId != null &&
            jobIds.Contains(x.JobId)).ToListAsync(cancellationToken);
        if (saves.Count == 0) return;
        var subscriberIds = saves.Select(x => x.SubscriberId!).Distinct().ToList();
        var subscribers = (await _subscribers.Find(x => subscriberIds.Contains(x.SubscriberId) &&
                x.IsActive && x.IsEmailVerified && x.NotificationFrequency != "None")
            .ToListAsync(cancellationToken)).ToDictionary(x => x.SubscriberId);
        var companies = (await _companies.Find(x => companyIds.Contains(x.CompanyId) &&
                x.Status && !x.IsDeleted && x.CareerPortalEnabled).ToListAsync(cancellationToken))
            .ToDictionary(x => x.CompanyId);

        foreach (var save in saves.Where(x => subscribers.ContainsKey(x.SubscriberId!) &&
                                              jobMap.ContainsKey(x.JobId)))
        {
            var job = jobMap[save.JobId];
            if (!companies.TryGetValue(job.CompanyId, out var company)) continue;
            const string type = "SavedJobDeadlineReminder";
            if (await _outbox.Find(x => x.SubscriberId == save.SubscriberId && x.JobId == job.JobId &&
                    x.NotificationType == type && x.Status != "Cancelled").AnyAsync(cancellationToken) ||
                await _deliveries.Find(x => x.SubscriberId == save.SubscriberId && x.JobId == job.JobId &&
                    x.NotificationReason == type).AnyAsync(cancellationToken)) continue;
            await _outbox.InsertOneAsync(new CareerNotificationOutbox
            {
                SubscriberId = save.SubscriberId!, NotificationType = type,
                CompanyId = company.CompanyId, JobId = job.JobId,
                PayloadJson = JsonSerializer.Serialize(new JobNotificationPayload(
                    job.Title, company.CompanyName, company.PublicCompanyCode, job.PublicJobId,
                    job.Slug, job.Location, job.ApplicationDeadline)),
                Status = "Pending", AvailableAt = now, CreatedAt = now
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task<List<CareerNotificationOutbox>> ClaimDigestItems(
        string subscriberId, DateTime now, CancellationToken cancellationToken)
    {
        var candidates = await _outbox.Find(x => x.SubscriberId == subscriberId &&
                (x.Status == "Pending" || x.Status == "Failed") && x.AvailableAt <= now)
            .SortBy(x => x.AvailableAt).Limit(49).ToListAsync(cancellationToken);
        var claimed = new List<CareerNotificationOutbox>();
        foreach (var candidate in candidates)
        {
            var value = await _outbox.FindOneAndUpdateAsync(
                x => x.NotificationId == candidate.NotificationId &&
                     (x.Status == "Pending" || x.Status == "Failed") && x.AvailableAt <= now,
                Builders<CareerNotificationOutbox>.Update
                    .Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAt, now)
                    .Set(x => x.UpdatedAt, now).Inc(x => x.AttemptCount, 1),
                new FindOneAndUpdateOptions<CareerNotificationOutbox> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
            if (value is not null) claimed.Add(value);
        }
        return claimed;
    }

    private async Task QueueNewJob(JobVacancy job, Company company, bool masterVisible)
    {
        var subscriberIds = (await _followers.Find(x =>
            x.CompanyId == company.CompanyId && x.IsActive && x.NotifyForNewJobs).ToListAsync())
            .Select(x => x.SubscriberId).ToHashSet();
        if (masterVisible)
        {
            var general = await _subscribers.Find(x => x.IsActive && x.IsEmailVerified &&
                x.NotificationFrequency != "None").ToListAsync();
            subscriberIds.UnionWith(general.Select(x => x.SubscriberId));
            var alerts = (await _alerts.Find(x => x.IsActive).ToListAsync()).Where(x => Matches(x, job));
            subscriberIds.UnionWith(alerts.Select(x => x.SubscriberId));
        }
        await Queue(job, company, subscriberIds, "NewJob");
    }

    private async Task QueueJobUpdate(JobVacancy job, Company company, bool masterVisible)
    {
        var subscriberIds = (await _followers.Find(x =>
            x.CompanyId == company.CompanyId && x.IsActive && x.NotifyForJobUpdates).ToListAsync())
            .Select(x => x.SubscriberId).ToHashSet();
        if (masterVisible)
        {
            var alerts = (await _alerts.Find(x => x.IsActive).ToListAsync()).Where(x => Matches(x, job));
            subscriberIds.UnionWith(alerts.Select(x => x.SubscriberId));
        }
        await Queue(job, company, subscriberIds, "JobUpdated");
    }

    private async Task Queue(JobVacancy job, Company company, HashSet<string> subscriberIds, string type)
    {
        if (subscriberIds.Count == 0) return;
        var subscribers = await _subscribers.Find(x => subscriberIds.Contains(x.SubscriberId) &&
            x.IsActive && x.IsEmailVerified && x.NotificationFrequency != "None").ToListAsync();
        var existing = (await _outbox.Find(x => subscriberIds.Contains(x.SubscriberId) &&
            x.JobId == job.JobId && x.NotificationType == type && x.Status != "Cancelled").ToListAsync())
            .Select(x => x.SubscriberId).ToHashSet();
        var payload = JsonSerializer.Serialize(new JobNotificationPayload(
            job.Title, company.CompanyName, company.PublicCompanyCode, job.PublicJobId, job.Slug,
            job.Location, job.ApplicationDeadline));
        var now = DateTime.UtcNow;
        var items = subscribers.Where(x => !existing.Contains(x.SubscriberId)).Select(s => new CareerNotificationOutbox
        {
            SubscriberId = s.SubscriberId, NotificationType = type, CompanyId = company.CompanyId,
            JobId = job.JobId, PayloadJson = payload, Status = "Pending", AttemptCount = 0,
            AvailableAt = NextDelivery(now, s.NotificationFrequency), CreatedAt = now
        }).ToList();
        if (items.Count > 0) await _outbox.InsertManyAsync(items);
    }

    private static DateTime NextDelivery(DateTime now, string frequency)
    {
        if (frequency.Equals("DailyDigest", StringComparison.OrdinalIgnoreCase))
            return now.Date.AddDays(1).AddHours(8);
        if (!frequency.Equals("WeeklyDigest", StringComparison.OrdinalIgnoreCase)) return now;
        var days = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        return now.Date.AddDays(days == 0 ? 7 : days).AddHours(8);
    }
    internal static bool Eligible(JobVacancy j, Company c, DateTime now, bool master) =>
        c.Status && !c.IsDeleted && c.CareerPortalEnabled && j.Status && !j.IsDeleted &&
        j.PublishToCareerPortal && (j.ExpiresAt is null || j.ExpiresAt >= now) &&
        (j.ApplicationDeadline is null || j.ApplicationDeadline >= now) &&
        (!master || c.PublishJobsToMasterPortal && j.PublishToMasterPortal);
    internal static bool PublicFieldsChanged(JobVacancy a, JobVacancy b) =>
        a.Title != b.Title || a.Summary != b.Summary || a.Description != b.Description ||
        a.Location != b.Location || a.WorkplaceType != b.WorkplaceType ||
        a.EmploymentType != b.EmploymentType || a.SalaryMin != b.SalaryMin ||
        a.SalaryMax != b.SalaryMax || a.ApplicationDeadline != b.ApplicationDeadline ||
        !a.Skills.SequenceEqual(b.Skills);
    internal static bool Matches(JobAlertSubscription a, JobVacancy j) =>
        (string.IsNullOrWhiteSpace(a.SearchText) ||
         new[] { j.Title, j.Summary, j.Description }.Any(x => x?.Contains(a.SearchText, StringComparison.OrdinalIgnoreCase) == true)) &&
        (a.Locations.Count == 0 || a.Locations.Any(x => j.Location?.Contains(x, StringComparison.OrdinalIgnoreCase) == true)) &&
        (a.EmploymentTypes.Count == 0 || a.EmploymentTypes.Contains(j.EmploymentType ?? "", StringComparer.OrdinalIgnoreCase)) &&
        (a.WorkplaceTypes.Count == 0 || a.WorkplaceTypes.Contains(j.WorkplaceType ?? "", StringComparer.OrdinalIgnoreCase)) &&
        (a.JobTypes.Count == 0 || a.JobTypes.Contains(j.JobType)) &&
        (a.Industries.Count == 0 || a.Industries.Contains(j.Industry ?? "", StringComparer.OrdinalIgnoreCase)) &&
        (a.Departments.Count == 0 || a.Departments.Contains(j.Department ?? "", StringComparer.OrdinalIgnoreCase)) &&
        (a.Skills.Count == 0 || a.Skills.Any(s => j.Skills.Contains(s, StringComparer.OrdinalIgnoreCase))) &&
        (a.ExperienceMin is null || j.ExperienceMax >= a.ExperienceMin) &&
        (a.ExperienceMax is null || j.ExperienceMin <= a.ExperienceMax) &&
        (a.SalaryMin is null || j.SalaryMax >= a.SalaryMin) &&
        (string.IsNullOrWhiteSpace(a.Currency) || a.Currency.Equals(j.Currency, StringComparison.OrdinalIgnoreCase));
    private static string BuildBody(JobNotificationPayload p, string subscriberId)
    {
        var root = (ConfigManager.AppSettings.AppUrl ?? "").TrimEnd('/');
        var jobUrl = $"{root}/careers/{Uri.EscapeDataString(p.CompanyCode)}/jobs/{Uri.EscapeDataString(p.JobSlug)}";
        var token = CareerTokenProtector.CreateUnsubscribeToken(subscriberId);
        var unsubscribe = $"{root}/careers/preferences?unsubscribe={Uri.EscapeDataString(token)}";
        return $"<p>A role matching your career preferences is available.</p><h2>{WebUtility.HtmlEncode(p.JobTitle)}</h2>" +
               $"<p>{WebUtility.HtmlEncode(p.CompanyName)} · {WebUtility.HtmlEncode(p.Location ?? "Location not specified")}</p>" +
               $"<p><a href=\"{WebUtility.HtmlEncode(jobUrl)}\">View job</a></p>" +
               $"<p><a href=\"{WebUtility.HtmlEncode(unsubscribe)}\">Unsubscribe or manage preferences</a></p>";
    }
    private static string BuildDigestBody(IEnumerable<JobNotificationPayload> values, string subscriberId)
    {
        var root = (ConfigManager.AppSettings.AppUrl ?? "").TrimEnd('/');
        var cards = string.Join("", values.Select(p =>
        {
            var url = $"{root}/careers/{Uri.EscapeDataString(p.CompanyCode)}/jobs/{Uri.EscapeDataString(p.JobSlug)}";
            return $"<li><strong>{WebUtility.HtmlEncode(p.JobTitle)}</strong> — {WebUtility.HtmlEncode(p.CompanyName)}" +
                   $" · {WebUtility.HtmlEncode(p.Location ?? "Location not specified")} " +
                   $"<a href=\"{WebUtility.HtmlEncode(url)}\">View job</a></li>";
        }));
        var token = CareerTokenProtector.CreateUnsubscribeToken(subscriberId);
        var unsubscribe = $"{root}/careers/preferences?unsubscribe={Uri.EscapeDataString(token)}";
        return $"<p>Here are the latest opportunities matching your career preferences.</p><ul>{cards}</ul>" +
               $"<p><a href=\"{WebUtility.HtmlEncode(unsubscribe)}\">Unsubscribe or manage preferences</a></p>";
    }
    private Task MarkSent(string id, DateTime now, CancellationToken token) =>
        _outbox.UpdateOneAsync(x => x.NotificationId == id,
            Builders<CareerNotificationOutbox>.Update.Set(x => x.Status, "Sent").Set(x => x.SentAt, now).Set(x => x.UpdatedAt, now),
            cancellationToken: token);
    private sealed record JobNotificationPayload(string JobTitle, string CompanyName, string CompanyCode,
        string PublicJobId, string JobSlug, string? Location, DateTime? ApplicationDeadline);
}
