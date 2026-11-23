using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentResumeOperationsService(
    IMongoDbRepository<Applicant> applicants,
    IRecruitmentResumeScanner scanner,
    IHttpContextAccessor context) : IRecruitmentResumeOperationsService
{
    private static readonly TimeSpan CleanVerdictMaxAge = TimeSpan.FromHours(24);

    public async Task<Result<RecruitmentResumeScanHealthDto>> GetScanHealth()
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId))
            return new Result<RecruitmentResumeScanHealthDto>
            {
                Success = false,
                StatusCode = StatusCodes.Status403Forbidden,
                ErrorCode = RecruitmentErrorCodes.InvalidRequest,
                Message = "A valid company context is required."
            };

        var nowUtc = DateTime.UtcNow;
        var staleBefore = nowUtc - CleanVerdictMaxAge;
        var totalTask = applicants.Count(x => x.CompanyId == companyId && !x.IsDeleted && x.ResumeUrl != null && x.ResumeUrl != "");
        var pendingTask = applicants.Count(x => x.CompanyId == companyId && !x.IsDeleted && x.ResumeUrl != null && x.ResumeUrl != "" &&
            (x.ResumeScanVerdict == null || x.ResumeScanVerdict == "" ||
             (x.ResumeScanVerdict == nameof(ResumeScanStatus.Clean) && x.ResumeScanSha256 == null)));
        var unavailableTask = applicants.Count(x => x.CompanyId == companyId && !x.IsDeleted && x.ResumeUrl != null && x.ResumeUrl != "" && x.ResumeScanVerdict == nameof(ResumeScanStatus.Unavailable));
        var quarantineTask = applicants.Count(x => x.CompanyId == companyId && !x.IsDeleted && x.ResumeUrl != null && x.ResumeUrl != "" &&
            x.ResumeScanVerdict == nameof(ResumeScanStatus.Infected) && x.ResumeQuarantinedAtUtc == null);
        var staleTask = applicants.Count(x => x.CompanyId == companyId && !x.IsDeleted && x.ResumeUrl != null && x.ResumeUrl != "" &&
            x.ResumeScanVerdict == nameof(ResumeScanStatus.Clean) && x.ResumeScanSha256 != null &&
            (x.ResumeScannedAtUtc == null || x.ResumeScannedAtUtc <= staleBefore));
        var scannerReachabilityTask = ProbeScanner(context.HttpContext?.RequestAborted ?? default);

        try
        {
            await Task.WhenAll(totalTask, pendingTask, unavailableTask, quarantineTask, staleTask, scannerReachabilityTask);
        }
        catch (OperationCanceledException) when (context.HttpContext?.RequestAborted.IsCancellationRequested == true)
        {
            throw;
        }
        catch
        {
            return new Result<RecruitmentResumeScanHealthDto>
            {
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorCode = "REC_RESUME_SCAN_STATUS_UNAVAILABLE",
                Message = "Resume scanning status is temporarily unavailable. Retry shortly."
            };
        }

        var status = new RecruitmentResumeScanHealthDto
        {
            GeneratedAtUtc = nowUtc,
            ScannerConfigured = scanner.IsConfigured,
            ScannerReachable = scannerReachabilityTask.Result,
            ApplicantsWithResume = totalTask.Result,
            PendingInitialScans = pendingTask.Result,
            ScannerUnavailable = unavailableTask.Result,
            InfectedAwaitingQuarantine = quarantineTask.Result,
            StaleCleanScans = staleTask.Result
        };
        return new Result<RecruitmentResumeScanHealthDto>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            MethodResult = status,
            Data = status,
            TotalRecords = status.ApplicantsWithResume,
            Message = "Resume scan operations status loaded."
        };
    }

    private async Task<bool?> ProbeScanner(CancellationToken cancellationToken)
    {
        try
        {
            return await scanner.ProbeAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (context.HttpContext?.RequestAborted.IsCancellationRequested == true)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
