using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentApplicationBackfillTests
{
    [Fact]
    public async Task Backfill_reads_bounded_pages_and_writes_initial_stage_history()
    {
        const string companyId = "tenant-backfill";
        const int totalCandidates = 201;
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var canonicalApplications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var source = Enumerable.Range(0, totalCandidates).Select(index => new Applicant
        {
            CompanyId = companyId,
            ApplicantId = $"app-{index:D3}",
            Phone = string.Empty,
            VacancyId = "vacancy-1",
            ActivityType = Codeji.CMS.Utility.Enums.EnumsHelper.ActivityType.New,
            PipelineVersion = 1,
            WorkflowSnapshot = RecruitmentWorkflowSnapshot.LegacyV1
        }).ToArray();
        var pagesRead = new List<int?>();
        var inserted = new List<RecruitmentJobApplication>();

        applicants.Setup(repository => repository.GetAggregateDataAsync<Applicant>(
                It.IsAny<Expression<Func<Applicant, bool>>>(),
                It.IsAny<ProjectionDefinition<Applicant, Applicant>?>(),
                It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .Returns((Expression<Func<Applicant, bool>> _, ProjectionDefinition<Applicant, Applicant>? _, bool _, bool? _, string? _, int? pageNo, int? pageSize, string? _) =>
            {
                pagesRead.Add(pageNo);
                var page = source.Skip(((pageNo ?? 1) - 1) * (pageSize ?? 200)).Take(pageSize ?? 200).ToArray();
                return Task.FromResult<IEnumerable<Applicant>>(page);
            });
        canonicalApplications.Setup(repository => repository.FirstOrDefault(
                It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentJobApplication?)null);
        canonicalApplications.Setup(repository => repository.AddOne(It.IsAny<RecruitmentJobApplication>()))
            .Callback<RecruitmentJobApplication>(inserted.Add)
            .ReturnsAsync(new Result { Success = true });

        var context = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"))
            }
        };
        var service = new RecruitmentApplicationService(canonicalApplications.Object, applicants.Object, context);

        var result = await service.BackfillLegacy("recruiter-1");

        Assert.True(result.Success);
        Assert.Equal(totalCandidates, result.MethodResult);
        Assert.Equal(new int?[] { 1, 2 }, pagesRead);
        Assert.Equal(totalCandidates, inserted.Count);
        Assert.All(inserted, application =>
        {
            var history = Assert.Single(application.StageHistory);
            Assert.Equal("New", history.ToStage);
            Assert.Equal("Legacy applicant backfill", history.Reason);
            Assert.Equal("recruiter-1", history.ActorUserId);
        });
    }

    [Fact]
    public async Task Backfill_reports_page_read_failures_without_claiming_completion()
    {
        const string companyId = "tenant-backfill";
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var canonicalApplications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var source = Enumerable.Range(0, 200).Select(index => new Applicant
        {
            CompanyId = companyId,
            ApplicantId = $"app-{index:D3}",
            Phone = string.Empty,
            VacancyId = "vacancy-1",
            ActivityType = Codeji.CMS.Utility.Enums.EnumsHelper.ActivityType.New
        }).ToArray();
        applicants.Setup(repository => repository.GetAggregateDataAsync<Applicant>(
                It.IsAny<Expression<Func<Applicant, bool>>>(),
                It.IsAny<ProjectionDefinition<Applicant, Applicant>?>(),
                It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .Returns((Expression<Func<Applicant, bool>> _, ProjectionDefinition<Applicant, Applicant>? _, bool _, bool? _, string? _, int? pageNo, int? pageSize, string? _) =>
            {
                if (pageNo == 2) return Task.FromResult<IEnumerable<Applicant>>(null!);
                return Task.FromResult<IEnumerable<Applicant>>(source);
            });
        canonicalApplications.Setup(repository => repository.FirstOrDefault(
                It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentJobApplication?)null);
        canonicalApplications.Setup(repository => repository.AddOne(It.IsAny<RecruitmentJobApplication>()))
            .ReturnsAsync(new Result { Success = true });
        var context = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"))
            }
        };

        var result = await new RecruitmentApplicationService(canonicalApplications.Object, applicants.Object, context)
            .BackfillLegacy("recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("REC_BACKFILL_READ_FAILED", result.ErrorCode);
        Assert.Equal(200, result.MethodResult);
        Assert.Equal(200, result.TotalRecords);
    }
}
