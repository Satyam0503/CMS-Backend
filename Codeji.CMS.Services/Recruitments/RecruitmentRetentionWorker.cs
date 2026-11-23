using System.Diagnostics;
using System.Diagnostics.Metrics;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

internal readonly record struct RecruitmentRetentionRunResult(int Due, int Anonymized, int TransferredToEmployee, int Failed, TimeSpan Duration);

public sealed class RecruitmentRetentionWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentRetentionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);
    private static readonly Meter Meter = new("Codeji.CMS.Recruitment.Retention", "1.0.0");
    private static readonly Counter<long> DueCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.due");
    private static readonly Counter<long> AnonymizedCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.anonymized");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.failed");
    private static readonly Counter<long> EmployeeTransferCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.transferred_to_employee");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>("recruitment.retention.run.duration", "s");
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var applicants = scope.ServiceProvider.GetRequiredService<IMongoDbRepository<Applicant>>();
                var logs = scope.ServiceProvider.GetRequiredService<IMongoDbRepository<ApplicantLogs>>();
                var handoffs = scope.ServiceProvider.GetRequiredService<IMongoDbRepository<RecruitmentHandoff>>();
                var redactor = scope.ServiceProvider.GetRequiredService<IRecruitmentCandidateDataRedactor>();
                var result = await ProcessOnce(applicants, DateTime.UtcNow, redactor, handoffs, logs);
                DueCounter.Add(result.Due);
                AnonymizedCounter.Add(result.Anonymized);
                EmployeeTransferCounter.Add(result.TransferredToEmployee);
                FailedCounter.Add(result.Failed);
                DurationHistogram.Record(result.Duration.TotalSeconds);

                if (result.Failed > 0)
                    logger.LogWarning("Recruitment retention cycle completed with write failures. Due: {Due}, anonymized: {Anonymized}, transferredToEmployee: {TransferredToEmployee}, failed: {Failed}, durationMs: {DurationMs}.", result.Due, result.Anonymized, result.TransferredToEmployee, result.Failed, result.Duration.TotalMilliseconds);
                else if (result.Due > 0)
                    logger.LogInformation("Recruitment retention cycle completed. Due: {Due}, anonymized: {Anonymized}, transferredToEmployee: {TransferredToEmployee}, durationMs: {DurationMs}.", result.Due, result.Anonymized, result.TransferredToEmployee, result.Duration.TotalMilliseconds);

                if (result.Due == BatchSize)
                    logger.LogWarning("Recruitment retention cycle reached the batch limit of {BatchSize}; additional expired candidates may remain for the next cycle.", BatchSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                FailedCounter.Add(1);
                logger.LogError(ex, "Recruitment retention processing failed before the cycle completed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal static async Task<RecruitmentRetentionRunResult> ProcessOnce(
        IMongoDbRepository<Applicant> applicants,
        DateTime nowUtc,
        IRecruitmentCandidateDataRedactor? redactor = null,
        IMongoDbRepository<RecruitmentHandoff>? handoffs = null,
        IMongoDbRepository<ApplicantLogs>? logs = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var due = (await applicants.GetAll(
                x => !x.IsDeleted &&
                    ((x.ConsentStatus != "Anonymized" && x.RetentionDisposition != "TransferredToEmployee" && x.RetentionUntilUtc.HasValue && x.RetentionUntilUtc <= nowUtc) ||
                     (x.ConsentStatus == "Anonymized" && x.RetentionDisposition != "TransferredToEmployee" &&
                      (x.AnonymizationCleanupPending || x.AnonymizationCleanupCompletedAtUtc == null))),
                withDefaultFilter: false))
            .Take(BatchSize)
            .ToList();
        var anonymized = 0;
        var transferredToEmployee = 0;
        var failed = 0;

        foreach (var applicant in due)
        {
            if (handoffs is not null &&
                await handoffs.FirstOrDefault(x => x.CompanyId == applicant.CompanyId &&
                    x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) is not null)
            {
                int expectedVersion = Math.Max(1, applicant.Version);
                applicant.RetentionDisposition = "TransferredToEmployee";
                applicant.RetentionDispositionAtUtc = nowUtc;
                applicant.RetentionUntilUtc = null;
                applicant.Version = expectedVersion + 1;
                applicant.UpdatedBy = "retention-worker";
                applicant.UpdatedDate = nowUtc;
                var transferSaved = await applicants.Update(
                    Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId &&
                        x.ApplicantId == applicant.ApplicantId && !x.IsDeleted &&
                        x.ConsentStatus == applicant.ConsentStatus &&
                        (x.Version == expectedVersion || x.Version == 0)),
                    applicant);
                if (!transferSaved.Success)
                {
                    failed++;
                    continue;
                }
                transferredToEmployee++;
                continue;
            }

            string? resumePath = null;
            if (applicant.ConsentStatus != "Anonymized")
            {
                var expectedVersion = Math.Max(1, applicant.Version);
                if (!string.IsNullOrWhiteSpace(applicant.ResumeUrl))
                {
                    try { resumePath = RecruitmentResumeStorage.Resolve(applicant.ResumeUrl); }
                    catch { }
                }
                PublicCareerApplicationRules.AnonymizeApplicant(applicant, nowUtc);
                applicant.Version = expectedVersion + 1;
                applicant.UpdatedBy = "retention-worker";
                applicant.UpdatedDate = nowUtc;
                var update = await applicants.Update(
                    Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted && x.ConsentStatus != "Anonymized" && (x.Version == expectedVersion || x.Version == 0)),
                    applicant);
                if (!update.Success)
                {
                    failed++;
                    continue;
                }

                anonymized++;
            }

            if (resumePath is not null && File.Exists(resumePath))
            {
                try { File.Delete(resumePath); }
                catch { failed++; }
            }

            if (!RecruitmentResumeStorage.DeleteQuarantined(applicant.AnonymizationCleanupResumeQuarantineFileName))
            {
                failed++;
                continue;
            }

            if (redactor is null) continue;
            try
            {
                await redactor.RedactAsync(applicant.CompanyId, applicant.ApplicantId,
                    applicant.AnonymizationCleanupEmail, "retention-worker", nowUtc);
            }
            catch
            {
                failed++;
                continue;
            }

            if (logs is not null && !await EnsureRetentionAuditLog(logs, applicant, nowUtc))
            {
                failed++;
                continue;
            }

            int cleanupVersion = Math.Max(1, applicant.Version);
            applicant.AnonymizationCleanupPending = false;
            applicant.AnonymizationCleanupEmail = null;
            applicant.AnonymizationCleanupResumeQuarantineFileName = null;
            applicant.AnonymizationCleanupCompletedAtUtc = nowUtc;
            applicant.Version = cleanupVersion + 1;
            applicant.UpdatedBy = "retention-worker";
            applicant.UpdatedDate = nowUtc;
            var cleanupSaved = await applicants.Update(
                Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId &&
                    x.ApplicantId == applicant.ApplicantId && x.ConsentStatus == "Anonymized" &&
                    x.AnonymizationCleanupPending && x.Version == cleanupVersion),
                applicant);
            if (!cleanupSaved.Success) failed++;
        }

        stopwatch.Stop();
        return new RecruitmentRetentionRunResult(due.Count, anonymized, transferredToEmployee, failed, stopwatch.Elapsed);
    }

    private static async Task<bool> EnsureRetentionAuditLog(IMongoDbRepository<ApplicantLogs> logs, Applicant applicant, DateTime nowUtc)
    {
        try
        {
            var existing = await logs.FirstOrDefault(x => x.CompanyId == applicant.CompanyId &&
                x.ApplicantId == applicant.ApplicantId && x.EventType == "CandidateRetentionAnonymized" && !x.IsDeleted);
            if (existing is not null) return true;
            var key = $"retention-anonymization:{applicant.ApplicantId}";
            var added = await logs.AddOne(new ApplicantLogs
            {
                CompanyId = applicant.CompanyId,
                ApplicantId = applicant.ApplicantId,
                ApplicantName = "Privacy-controlled candidate",
                JobRole = applicant.VacancyId,
                UserId = "retention-worker",
                ActivityCategory = 0,
                Description = "Candidate anonymized after the configured retention period.",
                EventType = "CandidateRetentionAnonymized",
                PreviousStage = null,
                CurrentStage = "Anonymized",
                IdempotencyKey = key,
                CreatedBy = "retention-worker",
                UpdatedBy = "retention-worker",
                CreatedDate = nowUtc,
                UpdatedDate = nowUtc
            });
            return added?.Success == true || await logs.FirstOrDefault(x => x.CompanyId == applicant.CompanyId &&
                x.ApplicantId == applicant.ApplicantId && x.EventType == "CandidateRetentionAnonymized" && !x.IsDeleted) is not null;
        }
        catch
        {
            return false;
        }
    }
}
