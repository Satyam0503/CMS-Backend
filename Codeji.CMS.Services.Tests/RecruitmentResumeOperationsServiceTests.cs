using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeOperationsServiceTests
{
    [Fact]
    public async Task Scan_health_counts_only_active_resume_records_for_the_current_company()
    {
        const string companyId = "tenant-resume-ops";
        DateTime now = DateTime.UtcNow;
        var seed = new[]
        {
            Candidate("pending", companyId, "/fs/Resume/pending.pdf"),
            Candidate("unavailable", companyId, "/fs/Resume/unavailable.pdf", "Unavailable"),
            Candidate("infected", companyId, "/fs/Resume/infected.pdf", "Infected"),
            Candidate("stale", companyId, "/fs/Resume/stale.pdf", "Clean", "digest", now.AddHours(-25)),
            Candidate("current", companyId, "/fs/Resume/current.pdf", "Clean", "digest", now.AddHours(-1)),
            Candidate("other-tenant", "other-tenant", "/fs/Resume/other.pdf"),
            Candidate("deleted", companyId, "/fs/Resume/deleted.pdf", deleted: true)
        };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(repository => repository.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<Applicant, bool>> filter, bool _) =>
                Task.FromResult(seed.Count(filter.Compile())));

        var service = new RecruitmentResumeOperationsService(
            applicants.Object,
            new ClamAvRecruitmentResumeScanner(null),
            TenantContext(companyId));

        var result = await service.GetScanHealth();

        Assert.True(result.Success);
        Assert.Equal(5, result.Data!.ApplicantsWithResume);
        Assert.Equal(1, result.Data.PendingInitialScans);
        Assert.Equal(1, result.Data.ScannerUnavailable);
        Assert.Equal(1, result.Data.InfectedAwaitingQuarantine);
        Assert.Equal(1, result.Data.StaleCleanScans);
        Assert.False(result.Data.ScannerConfigured);
        Assert.False(result.Data.ScannerReachable);
    }

    [Fact]
    public async Task Scan_health_reports_a_successful_live_scanner_probe()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(repository => repository.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()))
            .Returns(Task.FromResult(0));
        var service = new RecruitmentResumeOperationsService(
            applicants.Object,
            new ProbeResumeScanner(true),
            TenantContext("tenant-resume-ops"));

        var result = await service.GetScanHealth();

        Assert.True(result.Success);
        Assert.True(result.Data!.ScannerConfigured);
        Assert.True(result.Data.ScannerReachable);
    }

    [Fact]
    public async Task Scan_health_keeps_database_counts_available_when_probe_throws()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(repository => repository.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()))
            .Returns(Task.FromResult(0));
        var service = new RecruitmentResumeOperationsService(
            applicants.Object,
            new ProbeResumeScanner(null, throwOnProbe: true),
            TenantContext("tenant-resume-ops"));

        var result = await service.GetScanHealth();

        Assert.True(result.Success);
        Assert.Null(result.Data!.ScannerReachable);
        Assert.Equal(0, result.Data.ApplicantsWithResume);
    }

    [Fact]
    public async Task Scan_health_denies_missing_tenant_context_before_database_access()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var service = new RecruitmentResumeOperationsService(
            applicants.Object,
            new ClamAvRecruitmentResumeScanner("clamav.internal"),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var result = await service.GetScanHealth();

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        applicants.Verify(repository => repository.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Scan_health_propagates_database_cancellation_instead_of_reporting_an_outage()
    {
        using var requestCancellation = new CancellationTokenSource();
        requestCancellation.Cancel();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(repository => repository.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()))
            .Returns(Task.FromCanceled<int>(requestCancellation.Token));
        var httpContext = new DefaultHttpContext { RequestAborted = requestCancellation.Token };
        var service = new RecruitmentResumeOperationsService(
            applicants.Object,
            new ClamAvRecruitmentResumeScanner("clamav.internal"),
            new HttpContextAccessor { HttpContext = ContextWithTenant(httpContext, "tenant-resume-ops") });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetScanHealth());
    }

    private static Applicant Candidate(
        string id,
        string companyId,
        string resumeUrl,
        string? verdict = null,
        string? digest = null,
        DateTime? scannedAt = null,
        bool deleted = false) => new()
    {
        CompanyId = companyId,
        ApplicantId = id,
        Phone = string.Empty,
        ResumeUrl = resumeUrl,
        ResumeScanVerdict = verdict,
        ResumeScanSha256 = digest,
        ResumeScannedAtUtc = scannedAt,
        ResumeQuarantinedAtUtc = null,
        IsDeleted = deleted
    };

    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"))
        }
    };

    private static DefaultHttpContext ContextWithTenant(DefaultHttpContext context, string companyId)
    {
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"));
        return context;
    }

    private sealed class ProbeResumeScanner(bool? reachable, bool throwOnProbe = false) : IRecruitmentResumeScanner
    {
        public bool IsConfigured => true;
        public Task<bool?> ProbeAsync(CancellationToken cancellationToken = default) => throwOnProbe
            ? Task.FromException<bool?>(new IOException("probe failed"))
            : Task.FromResult(reachable);
        public Task<ResumeScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResumeScanResult(ResumeScanStatus.Unavailable));
    }
}
