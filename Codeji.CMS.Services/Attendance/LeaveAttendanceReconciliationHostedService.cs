using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Utility.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

/// <summary>
/// Completes leave-to-attendance synchronization that could not be completed in
/// the approval request. The leave decision is already durable; this worker makes
/// its attendance projection eventually consistent without treating it as a new
/// approval or allowing cross-tenant processing.
/// </summary>
public sealed class LeaveAttendanceReconciliationHostedService(
    IServiceProvider services,
    ILogger<LeaveAttendanceReconciliationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const string SystemActor = "SYSTEM_LEAVE_RECONCILIATION";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessDueRetriesAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Leave attendance reconciliation retry cycle failed."); }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcessDueRetriesAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var leaveRepository = scope.ServiceProvider.GetRequiredService<IMongoDbRepository<LeaveRequest>>();
        var reconciliation = scope.ServiceProvider.GetRequiredService<ILeaveAttendanceReconciliationService>();
        var now = DateTime.UtcNow;
        var due = (await leaveRepository.GetAll(x =>
                x.ReconciliationStatus == "RetryScheduled" &&
                x.NextReconciliationAttemptAtUtc.HasValue &&
                x.NextReconciliationAttemptAtUtc.Value <= now,
                withDefaultFilter: false))
            .Take(100)
            .ToList();

        foreach (var leave in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result outcome;
            if (leave.Status == EnumsHelper.LeaveRequestStatus.Accepted)
                outcome = await reconciliation.ReconcileAcceptedLeaveAsync(leave.CompanyId, leave.LeaveRequestId, SystemActor, cancellationToken);
            else
                outcome = await reconciliation.ReverseLeaveAttendanceAsync(leave.CompanyId, leave.LeaveRequestId, leave.Version, SystemActor, cancellationToken);

            if (outcome.Success) continue;
            var nextAttempt = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Max(5, (leave.ReconciliationAttempts + 1) * 5)));
            await leaveRepository.GetCollection().UpdateOneAsync(
                x => x.CompanyId == leave.CompanyId && x.LeaveRequestId == leave.LeaveRequestId && x.ReconciliationStatus == "RetryScheduled",
                Builders<LeaveRequest>.Update
                    .Set(x => x.ReconciliationErrorCode, "LEAVE_RECONCILIATION_FAILED")
                    .Set(x => x.ReconciliationErrorMessage, outcome.Message)
                    .Set(x => x.LastReconciliationAttemptAtUtc, DateTime.UtcNow)
                    .Set(x => x.NextReconciliationAttemptAtUtc, nextAttempt)
                    .Inc(x => x.ReconciliationAttempts, 1),
                cancellationToken: cancellationToken);
            logger.LogWarning("Leave attendance reconciliation retry failed for {CompanyId}/{LeaveRequestId}: {Message}", leave.CompanyId, leave.LeaveRequestId, outcome.Message);
        }
    }
}
