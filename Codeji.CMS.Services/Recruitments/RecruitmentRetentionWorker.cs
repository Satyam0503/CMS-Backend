using System.Diagnostics;
using System.Diagnostics.Metrics;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

internal readonly record struct RecruitmentRetentionRunResult(int Due, int Anonymized, int Failed, TimeSpan Duration);

public sealed class RecruitmentRetentionWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentRetentionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);
    private static readonly Meter Meter = new("Codeji.CMS.Recruitment.Retention", "1.0.0");
    private static readonly Counter<long> DueCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.due");
    private static readonly Counter<long> AnonymizedCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.anonymized");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("recruitment.retention.candidates.failed");
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
                var result = await ProcessOnce(applicants, DateTime.UtcNow);
                DueCounter.Add(result.Due);
                AnonymizedCounter.Add(result.Anonymized);
                FailedCounter.Add(result.Failed);
                DurationHistogram.Record(result.Duration.TotalSeconds);

                if (result.Failed > 0)
                    logger.LogWarning("Recruitment retention cycle completed with write failures. Due: {Due}, anonymized: {Anonymized}, failed: {Failed}, durationMs: {DurationMs}.", result.Due, result.Anonymized, result.Failed, result.Duration.TotalMilliseconds);
                else if (result.Due > 0)
                    logger.LogInformation("Recruitment retention cycle completed. Due: {Due}, anonymized: {Anonymized}, durationMs: {DurationMs}.", result.Due, result.Anonymized, result.Duration.TotalMilliseconds);

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

    internal static async Task<RecruitmentRetentionRunResult> ProcessOnce(IMongoDbRepository<Applicant> applicants, DateTime nowUtc)
    {
        var stopwatch = Stopwatch.StartNew();
        var due = (await applicants.GetAll(
                x => !x.IsDeleted && x.ConsentStatus != "Anonymized" && x.RetentionUntilUtc.HasValue && x.RetentionUntilUtc <= nowUtc,
                withDefaultFilter: false))
            .Take(BatchSize)
            .ToList();
        var anonymized = 0;
        var failed = 0;

        foreach (var applicant in due)
        {
            var expectedVersion = Math.Max(1, applicant.Version);
            applicant.ConsentStatus = "Anonymized";
            applicant.AnonymizedAtUtc = nowUtc;
            applicant.Email = $"anonymized-{applicant.ApplicantId}@invalid.local";
            applicant.Phone = "0000000000";
            applicant.FirstName = "Anonymized";
            applicant.LastName = "Candidate";
            applicant.ResumeUrl = string.Empty;
            applicant.CoverLetter = null;
            applicant.LinkedInUrl = null;
            applicant.PortfolioUrl = null;
            applicant.Version = expectedVersion + 1;
            applicant.UpdatedBy = "retention-worker";
            applicant.UpdatedDate = nowUtc;
            var update = await applicants.Update(
                Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted && x.ConsentStatus != "Anonymized" && (x.Version == expectedVersion || x.Version == 0)),
                applicant);
            if (update.Success) anonymized++;
            else failed++;
        }

        stopwatch.Stop();
        return new RecruitmentRetentionRunResult(due.Count, anonymized, failed, stopwatch.Elapsed);
    }
}