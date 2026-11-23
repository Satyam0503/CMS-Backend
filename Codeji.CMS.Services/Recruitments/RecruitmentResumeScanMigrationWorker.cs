using Codeji.CMS.Repository.Entities.Recruitments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>
/// Backfills scan verdicts for legacy resumes and moves infected bytes out of all active resume paths.
/// Work is durable in Applicant scan state, so hosts may stop between any two files and resume later.
/// </summary>
public sealed class RecruitmentResumeScanMigrationWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentResumeScanMigrationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UnavailableRetry = TimeSpan.FromHours(6);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                var scanner = scope.ServiceProvider.GetRequiredService<IRecruitmentResumeScanner>();
                var result = await ProcessBatch(database.GetCollection<Applicant>(nameof(Applicant)),
                    database.GetCollection<ApplicantLogs>(nameof(ApplicantLogs)), scanner, DateTime.UtcNow, BatchSize, stoppingToken);
                if (result.Failed > 0)
                    logger.LogWarning("Legacy resume scan migration batch completed with failures. Examined: {Examined}, clean: {Clean}, quarantined: {Quarantined}, unavailable: {Unavailable}, failed: {Failed}.",
                        result.Examined, result.Clean, result.Quarantined, result.Unavailable, result.Failed);
                else if (result.Examined > 0)
                    logger.LogInformation("Legacy resume scan migration batch completed. Examined: {Examined}, clean: {Clean}, quarantined: {Quarantined}, unavailable: {Unavailable}.",
                        result.Examined, result.Clean, result.Quarantined, result.Unavailable);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Legacy resume scan migration batch failed before completing.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    internal static async Task<RecruitmentResumeMigrationBatchResult> ProcessBatch(
        IMongoCollection<Applicant> applicants,
        IMongoCollection<ApplicantLogs> logs,
        IRecruitmentResumeScanner scanner,
        DateTime nowUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(batchSize));
        var a = Builders<Applicant>.Filter;
        var dueUnavailable = a.Eq(x => x.ResumeScanVerdict, nameof(ResumeScanStatus.Unavailable)) &
            (a.Eq(x => x.ResumeScannedAtUtc, null) | a.Lte(x => x.ResumeScannedAtUtc, nowUtc - UnavailableRetry));
        var infectedNeedsQuarantine = a.Eq(x => x.ResumeScanVerdict, nameof(ResumeScanStatus.Infected)) &
            a.Eq(x => x.ResumeQuarantinedAtUtc, null);
        var missingVerdict = a.Eq(x => x.ResumeScanVerdict, null) |
            (a.Eq(x => x.ResumeScanVerdict, nameof(ResumeScanStatus.Clean)) & a.Eq(x => x.ResumeScanSha256, null));
        var filter = a.Eq(x => x.IsDeleted, false) & a.Ne(x => x.ConsentStatus, "Anonymized") &
            a.Exists(x => x.ResumeUrl, true) & a.Ne(x => x.ResumeUrl, "") &
            (missingVerdict | dueUnavailable | infectedNeedsQuarantine);
        var pending = await applicants.Find(filter)
            .SortBy(x => x.ResumeScannedAtUtc)
            .ThenBy(x => x.ApplicantId)
            .Limit(batchSize)
            .ToListAsync(cancellationToken);

        var result = new RecruitmentResumeMigrationBatchResult();
        foreach (var applicant in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Examined++;
            try
            {
                var state = await ProcessApplicant(applicants, logs, scanner, applicant, nowUtc, cancellationToken);
                switch (state)
                {
                    case RecruitmentResumeMigrationItemState.Clean: result.Clean++; break;
                    case RecruitmentResumeMigrationItemState.Quarantined: result.Quarantined++; break;
                    case RecruitmentResumeMigrationItemState.Unavailable: result.Unavailable++; break;
                    default: result.Failed++; break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { result.Failed++; }
        }
        return result;
    }

    private static async Task<RecruitmentResumeMigrationItemState> ProcessApplicant(
        IMongoCollection<Applicant> applicants,
        IMongoCollection<ApplicantLogs> logs,
        IRecruitmentResumeScanner scanner,
        Applicant applicant,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        byte[] bytes;
        string sourcePath;
        try
        {
            sourcePath = RecruitmentResumeStorage.Resolve(applicant.ResumeUrl);
            var info = new FileInfo(sourcePath);
            if (!info.Exists || info.Length is < 4 or > 5 * 1024 * 1024 ||
                (File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
                return string.Equals(applicant.ResumeScanVerdict, nameof(ResumeScanStatus.Infected), StringComparison.Ordinal)
                    ? await QuarantinePersistedInfected(applicants, applicant, cancellationToken)
                    : await RecordUnavailable(applicants, logs, applicant, null, nowUtc, cancellationToken);
            bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            return string.Equals(applicant.ResumeScanVerdict, nameof(ResumeScanStatus.Infected), StringComparison.Ordinal)
                ? await QuarantinePersistedInfected(applicants, applicant, cancellationToken)
                : await RecordUnavailable(applicants, logs, applicant, null, nowUtc, cancellationToken);
        }

        string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        if (RecruitmentResumeVerdictRules.IsCachedInfected(applicant.ResumeScanVerdict, applicant.ResumeScanSha256, digest))
            return await QuarantinePersistedInfected(applicants, applicant, cancellationToken);

        ResumeScanResult scan;
        try
        {
            await using var stream = new MemoryStream(bytes, writable: false);
            scan = await scanner.ScanAsync(stream, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { scan = new ResumeScanResult(ResumeScanStatus.Unavailable); }

        if (scan.Status != ResumeScanStatus.Unavailable &&
            !RecruitmentResumeVerdictRules.Matches(scan.Sha256, digest))
            scan = new ResumeScanResult(ResumeScanStatus.Unavailable);

        try
        {
            await logs.InsertOneAsync(RecruitmentResumeScanAudit.Create(
                applicant.CompanyId, applicant.ApplicantId, applicant.VacancyId, "resume-migration-worker",
                Guid.NewGuid().ToString("N"), scan.Status), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return RecruitmentResumeMigrationItemState.Failed; }

        if (scan.Status == ResumeScanStatus.Unavailable)
            return await RecordUnavailable(applicants, null, applicant, digest, nowUtc, cancellationToken, auditAlreadyWritten: true);

        string? quarantineFileName = scan.Status == ResumeScanStatus.Infected ? $"{Guid.NewGuid():N}.quarantine" : null;
        var filter = ExpectedResumeVersion(applicant);
        var update = Builders<Applicant>.Update
            .Set(x => x.ResumeScanSha256, digest)
            .Set(x => x.ResumeScanVerdict, scan.Status.ToString())
            .Set(x => x.ResumeScannedAtUtc, nowUtc)
            .Set(x => x.ResumeQuarantineFileName, quarantineFileName)
            .Set(x => x.ResumeQuarantinedAtUtc, (DateTime?)null);
        var saved = await applicants.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (saved.MatchedCount != 1) return RecruitmentResumeMigrationItemState.Failed;
        if (scan.Status == ResumeScanStatus.Clean) return RecruitmentResumeMigrationItemState.Clean;

        applicant.ResumeScanSha256 = digest;
        applicant.ResumeQuarantineFileName = quarantineFileName;
        applicant.ResumeQuarantinedAtUtc = null;
        return await QuarantinePersistedInfected(applicants, applicant, cancellationToken);
    }

    private static async Task<RecruitmentResumeMigrationItemState> RecordUnavailable(
        IMongoCollection<Applicant> applicants,
        IMongoCollection<ApplicantLogs>? logs,
        Applicant applicant,
        string? digest,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        bool auditAlreadyWritten = false)
    {
        if (!auditAlreadyWritten && logs is not null)
        {
            try
            {
                await logs.InsertOneAsync(RecruitmentResumeScanAudit.Create(
                    applicant.CompanyId, applicant.ApplicantId, applicant.VacancyId, "resume-migration-worker",
                    Guid.NewGuid().ToString("N"), ResumeScanStatus.Unavailable), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return RecruitmentResumeMigrationItemState.Failed; }
        }

        var saved = await applicants.UpdateOneAsync(
            ExpectedResumeVersion(applicant),
            Builders<Applicant>.Update.Set(x => x.ResumeScanSha256, digest)
                .Set(x => x.ResumeScanVerdict, nameof(ResumeScanStatus.Unavailable))
                .Set(x => x.ResumeScannedAtUtc, nowUtc), cancellationToken: cancellationToken);
        return saved.MatchedCount == 1 ? RecruitmentResumeMigrationItemState.Unavailable : RecruitmentResumeMigrationItemState.Failed;
    }

    private static async Task<RecruitmentResumeMigrationItemState> QuarantinePersistedInfected(
        IMongoCollection<Applicant> applicants,
        Applicant applicant,
        CancellationToken cancellationToken)
    {
        string quarantineFileName = applicant.ResumeQuarantineFileName ?? $"{Guid.NewGuid():N}.quarantine";
        if (applicant.ResumeQuarantineFileName is null)
        {
            var assigned = await applicants.UpdateOneAsync(x => x.CompanyId == applicant.CompanyId &&
                x.ApplicantId == applicant.ApplicantId && !x.IsDeleted && x.ResumeUrl == applicant.ResumeUrl &&
                x.ResumeScanSha256 == applicant.ResumeScanSha256 && x.ResumeScanVerdict == nameof(ResumeScanStatus.Infected) &&
                x.ResumeQuarantineFileName == null,
                Builders<Applicant>.Update.Set(x => x.ResumeQuarantineFileName, quarantineFileName),
                cancellationToken: cancellationToken);
            if (assigned.MatchedCount != 1)
            {
                var current = await applicants.Find(x => x.CompanyId == applicant.CompanyId &&
                    x.ApplicantId == applicant.ApplicantId && !x.IsDeleted).FirstOrDefaultAsync(cancellationToken);
                if (current is null || current.ResumeUrl != applicant.ResumeUrl ||
                    current.ResumeScanSha256 != applicant.ResumeScanSha256 ||
                    current.ResumeScanVerdict != nameof(ResumeScanStatus.Infected) ||
                    string.IsNullOrWhiteSpace(current.ResumeQuarantineFileName))
                    return RecruitmentResumeMigrationItemState.Failed;
                quarantineFileName = current.ResumeQuarantineFileName;
                applicant.ResumeQuarantinedAtUtc = current.ResumeQuarantinedAtUtc;
            }
        }
        applicant.ResumeQuarantineFileName = quarantineFileName;
        if (applicant.ResumeQuarantinedAtUtc.HasValue) return RecruitmentResumeMigrationItemState.Quarantined;
        string sourcePath;
        try { sourcePath = RecruitmentResumeStorage.Resolve(applicant.ResumeUrl); }
        catch { return RecruitmentResumeMigrationItemState.Failed; }

        bool moved = await RecruitmentResumeStorage.MoveToQuarantine(
            sourcePath, quarantineFileName, applicant.ResumeScanSha256!, cancellationToken);
        if (!moved) return RecruitmentResumeMigrationItemState.Failed;

        var update = Builders<Applicant>.Update.Set(x => x.ResumeQuarantinedAtUtc, DateTime.UtcNow);
        var saved = await applicants.UpdateOneAsync(x => x.CompanyId == applicant.CompanyId &&
            x.ApplicantId == applicant.ApplicantId && !x.IsDeleted && x.ResumeUrl == applicant.ResumeUrl &&
            x.ResumeScanSha256 == applicant.ResumeScanSha256 && x.ResumeScanVerdict == nameof(ResumeScanStatus.Infected) &&
            x.ResumeQuarantineFileName == quarantineFileName && x.ResumeQuarantinedAtUtc == null,
            update, cancellationToken: cancellationToken);
        return saved.MatchedCount == 1 ? RecruitmentResumeMigrationItemState.Quarantined : RecruitmentResumeMigrationItemState.Failed;
    }

    private static FilterDefinition<Applicant> ExpectedResumeVersion(Applicant applicant) =>
        Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId &&
            x.ApplicantId == applicant.ApplicantId && !x.IsDeleted && x.ResumeUrl == applicant.ResumeUrl &&
            x.ResumeScanSha256 == applicant.ResumeScanSha256 && x.ResumeScanVerdict == applicant.ResumeScanVerdict &&
            x.ResumeScannedAtUtc == applicant.ResumeScannedAtUtc);
}

internal enum RecruitmentResumeMigrationItemState { Clean, Quarantined, Unavailable, Failed }

internal sealed class RecruitmentResumeMigrationBatchResult
{
    public int Examined { get; set; }
    public int Clean { get; set; }
    public int Quarantined { get; set; }
    public int Unavailable { get; set; }
    public int Failed { get; set; }
}
