using Codeji.CMS.Repository.Entities.Recruitments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>
/// Removes aged private resume files that are no longer referenced by an applicant.
/// This cleans up files left behind by interrupted uploads and candidate retention.
/// </summary>
public sealed class RecruitmentResumeOrphanCleanupWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentResumeOrphanCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan MinimumFileAge = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                string privateDirectory = RecruitmentResumeStorage.PrivateDirectory;
                var result = await CleanupOnce(
                    database.GetCollection<Applicant>(nameof(Applicant)),
                    privateDirectory,
                    DateTime.UtcNow,
                    MinimumFileAge,
                    stoppingToken);

                if (result.Failed > 0)
                    logger.LogWarning(
                        "Recruitment resume orphan cleanup finished with file errors. Examined: {Examined}, deleted: {Deleted}, failed: {Failed}.",
                        result.Examined,
                        result.Deleted,
                        result.Failed);
                else if (result.Deleted > 0)
                    logger.LogInformation(
                        "Recruitment resume orphan cleanup finished. Examined: {Examined}, deleted: {Deleted}.",
                        result.Examined,
                        result.Deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Recruitment resume orphan cleanup failed before completing its scan.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal static async Task<RecruitmentResumeCleanupResult> CleanupOnce(
        IMongoCollection<Applicant> applicants,
        string privateDirectory,
        DateTime nowUtc,
        TimeSpan minimumFileAge,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(privateDirectory))
            return new RecruitmentResumeCleanupResult(0, 0, 0);

        var examined = 0;
        var deleted = 0;
        var failed = 0;
        foreach (string filePath in Directory.EnumerateFiles(privateDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileInfo file = new(filePath);
            if (!file.Exists || (File.GetAttributes(filePath) & FileAttributes.ReparsePoint) != 0 ||
                !RecruitmentResumeOrphanCleanupRules.ShouldClean(filePath, file.LastWriteTimeUtc, nowUtc, minimumFileAge))
                continue;

            examined++;
            string reference = $"{RecruitmentResumeStorage.PrivatePrefix}{file.Name}";
            try
            {
                var referenced = await applicants.Find(applicant => applicant.ResumeUrl == reference)
                    .Limit(1)
                    .FirstOrDefaultAsync(cancellationToken);
                if (referenced is not null) continue;

                File.Delete(filePath);
                deleted++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                failed++;
            }
        }

        return new RecruitmentResumeCleanupResult(examined, deleted, failed);
    }
}

internal readonly record struct RecruitmentResumeCleanupResult(int Examined, int Deleted, int Failed);

public static class RecruitmentResumeOrphanCleanupRules
{
    public static bool ShouldClean(string filePath, DateTime lastWriteTimeUtc, DateTime nowUtc, TimeSpan minimumFileAge) =>
        string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase) &&
        minimumFileAge > TimeSpan.Zero &&
        lastWriteTimeUtc <= nowUtc - minimumFileAge;
}
