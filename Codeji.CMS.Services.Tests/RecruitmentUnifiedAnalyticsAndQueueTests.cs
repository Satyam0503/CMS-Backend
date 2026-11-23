using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentUnifiedAnalyticsAndQueueTests
{
    private static IHttpContextAccessor TenantContext(string companyId) =>
        new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId), new Claim("user_id", "test-user")], "TestAuth"))
            }
        };

    [Fact]
    public async Task Analytics_returns_all_active_applicants_without_writing_when_canonical_applications_are_desynchronized()
    {
        const string companyId = "tenant-codeji";

        // 5 active candidates with ActivityType = Selected (matching the exact user screenshot)
        var activeApplicants = new List<Applicant>
        {
            new() { ApplicantId = "app-1", CompanyId = companyId, FirstName = "gaurav", LastName = "negi", Email = "gaurav1@yopmail.com", Phone = "111", CandidateReportedSource = "Employee referral", ActivityType = ActivityType.Selected, CreatedDate = new DateTime(2026, 10, 1) },
            new() { ApplicantId = "app-2", CompanyId = companyId, FirstName = "Satyam", LastName = "Dubey", Email = "satyam1@yopmail.com", Phone = "222", CandidateReportedSource = "Career site", ActivityType = ActivityType.Selected, CreatedDate = new DateTime(2026, 10, 2) },
            new() { ApplicantId = "app-3", CompanyId = companyId, FirstName = "Vivek", LastName = "singh", Email = "vivek1@yopmail.com", Phone = "333", CandidateReportedSource = "untrusted-value@example.com", ActivityType = ActivityType.Selected },
            new() { ApplicantId = "app-4", CompanyId = companyId, FirstName = "sahil", LastName = "Negi", Email = "sahilnegi@yopmail.com", Phone = "444", ActivityType = ActivityType.Selected },
            new() { ApplicantId = "app-5", CompanyId = companyId, FirstName = "rohan", LastName = "Singh", Email = "rohan@yopmail.com", Phone = "555", ActivityType = ActivityType.Selected },
            new() { ApplicantId = "merged-app", CompanyId = companyId, Phone = "000", ActivityType = ActivityType.Selected, MergeStatus = "Merged", IsDeleted = true },
            new() { ApplicantId = "foreign-app", CompanyId = "tenant-other", Phone = "999", ActivityType = ActivityType.Selected },
        };

        // Only 1 canonical job application exists in the database
        var canonicalApplications = new List<RecruitmentJobApplication>
        {
            new() { ApplicationId = "can-1", ApplicantId = "app-1", CompanyId = companyId, CurrentStage = "Selected", VacancyId = "v-1" }
        };

        var applicantRepo = AnalyticsRepo(activeApplicants);
        var canonicalRepo = AnalyticsRepo(canonicalApplications);
        canonicalRepo.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>()))
            .ReturnsAsync(new Result { Success = true });

        var offerRepo = AnalyticsRepo(new List<RecruitmentOffer>
        {
            new() { OfferId = "offer-1", CompanyId = companyId, ApplicantId = "app-1", Status = RecruitmentOfferStatus.Accepted, RespondedAtUtc = new DateTime(2026, 10, 11), Salary = 90000m, TotalCtc = 1400000m },
            new() { OfferId = "offer-2", CompanyId = companyId, ApplicantId = "app-2", Status = RecruitmentOfferStatus.Accepted, RespondedAtUtc = new DateTime(2026, 10, 17), Salary = 100000m, TotalCtc = 1600000m },
            new() { OfferId = "foreign-offer", CompanyId = "tenant-other", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Accepted, RespondedAtUtc = new DateTime(2026, 10, 20), Salary = 400000m, TotalCtc = 5000000m },
        });

        var service = new RecruitmentEnterpriseService(
            applicantRepo.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            offerRepo.Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId),
            canonicalRepo.Object
        );

        var response = await service.Analytics(null);

        Assert.True(response.Success);
        Assert.NotNull(response.MethodResult);
        var analytics = response.MethodResult!;

        // Total tracked applicants must be 5 (all active candidates), not 1!
        Assert.Equal(5, analytics.TotalApplicants);
        Assert.Equal(2, analytics.TimeToHireSampleSize);
        Assert.Equal(12.5m, analytics.AverageTimeToHireDays);
        Assert.Equal(5, analytics.ActiveApplicants);
        Assert.Equal(0, analytics.RejectedApplicants);
        Assert.Equal(1, analytics.MergedApplicants);
        applicantRepo.Verify(x => x.Count(It.IsAny<Expression<Func<Applicant, bool>>>(), true), Times.Once);

        // Stage breakdown for Selected must be 5
        Assert.True(analytics.ByStage.ContainsKey("Selected"));
        Assert.Equal(5, analytics.ByStage["Selected"]);
        Assert.Equal(1, analytics.ByCandidateReportedSource["Employee referral"]);
        Assert.Equal(1, analytics.ByCandidateReportedSource["Career site"]);
        Assert.Equal(1, analytics.ByCandidateReportedSource["Unclassified"]);
        Assert.Equal(2, analytics.ByCandidateReportedSource["Not reported"]);

        // GET analytics uses the applicant profile as a read fallback; it never repairs canonical data.
        canonicalRepo.Verify(x => x.AddOne(It.IsAny<RecruitmentJobApplication>()), Times.Never());
        applicantRepo.Verify(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        canonicalRepo.Verify(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        offerRepo.Verify(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Analytics_maps_preboarding_and_hired_stages_into_funnel_selected_count()
    {
        const string companyId = "tenant-codeji";

        var activeApplicants = new List<Applicant>
        {
            new() { ApplicantId = "app-1", CompanyId = companyId, FirstName = "Alice", LastName = "Smith", Phone = "111", ActivityType = ActivityType.Selected },
            new() { ApplicantId = "app-2", CompanyId = companyId, FirstName = "Bob", LastName = "Jones", Phone = "222", ActivityType = ActivityType.Selected },
        };

        var canonicalApplications = new List<RecruitmentJobApplication>
        {
            new() { ApplicationId = "can-1", ApplicantId = "app-1", CompanyId = companyId, CurrentStage = "Preboarding", VacancyId = "v-1" },
            new() { ApplicationId = "can-2", ApplicantId = "app-2", CompanyId = companyId, CurrentStage = "Hired", VacancyId = "v-1" },
        };

        var applicantRepo = AnalyticsRepo(activeApplicants);
        var canonicalRepo = AnalyticsRepo(canonicalApplications);
        var offerRepo = AnalyticsRepo(new List<RecruitmentOffer>());

        var service = new RecruitmentEnterpriseService(
            applicantRepo.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            offerRepo.Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId),
            canonicalRepo.Object
        );

        var response = await service.Analytics(null);

        Assert.True(response.Success);
        var analytics = response.MethodResult!;

        Assert.Equal(2, analytics.TotalApplicants);
        Assert.Equal(2, analytics.ActiveApplicants);

        // Preboarding & Hired are preserved in ByStage AND mapped to Selected for the Funnel cards
        Assert.Equal(1, analytics.ByStage["Preboarding"]);
        Assert.Equal(1, analytics.ByStage["Hired"]);
        Assert.Equal(2, analytics.ByStage["Selected"]);
    }

    private static Mock<IMongoDbRepository<T>> AnalyticsRepo<T>(List<T> rows) where T : class
    {
        var repository = new Mock<IMongoDbRepository<T>>();
        repository.Setup(x => x.Count(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _) => rows.Count(filter.Compile()));
        repository.Setup(x => x.GetAggregateDataAsync<T>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<MongoDB.Driver.ProjectionDefinition<T, T>>(),
                It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter,
                MongoDB.Driver.ProjectionDefinition<T, T>? _, bool _, bool? ascending, string? orderedKey, int? pageNo, int? pageSize, string? _) =>
            {
                var result = rows.Where(filter.Compile()).ToList();
                if (!string.IsNullOrWhiteSpace(orderedKey))
                {
                    var property = typeof(T).GetProperty(orderedKey);
                    if (property is not null) result = result.OrderBy(item => property.GetValue(item)).ToList();
                    if (ascending == false) result.Reverse();
                }
                if (pageNo.HasValue && pageSize.HasValue)
                    result = result.Skip(Math.Max(0, (pageNo.Value - 1) * pageSize.Value)).Take(pageSize.Value).ToList();
                return result;
            });
        return repository;
    }

    [Fact]
    public async Task Analytics_reads_applicants_in_bounded_ordered_batches()
    {
        const string companyId = "company-large";
        var records = Enumerable.Range(1, 1001)
            .Select(index => new Applicant
            {
                ApplicantId = $"candidate-{index:D4}",
                CompanyId = companyId,
                Phone = "redacted",
                ActivityType = ActivityType.Selected,
                CreatedDate = new DateTime(2026, 1, 1)
            }).ToList();
        var applicantRepo = AnalyticsRepo(records);
        var service = new RecruitmentEnterpriseService(
            applicantRepo.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            AnalyticsRepo(new List<RecruitmentOffer>()).Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId),
            AnalyticsRepo(new List<RecruitmentJobApplication>()).Object);

        var response = await service.Analytics(null);

        Assert.True(response.Success);
        Assert.Equal(1001, response.MethodResult!.TotalApplicants);
        Assert.Equal(1001, response.MethodResult.ByStage["Selected"]);
        applicantRepo.Verify(x => x.GetAggregateDataAsync<Applicant>(
            It.IsAny<Expression<Func<Applicant, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<Applicant, Applicant>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(Applicant.ApplicantId), 1, 500, It.IsAny<string>()), Times.Once);
        applicantRepo.Verify(x => x.GetAggregateDataAsync<Applicant>(
            It.IsAny<Expression<Func<Applicant, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<Applicant, Applicant>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(Applicant.ApplicantId), 2, 500, It.IsAny<string>()), Times.Once);
        applicantRepo.Verify(x => x.GetAggregateDataAsync<Applicant>(
            It.IsAny<Expression<Func<Applicant, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<Applicant, Applicant>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(Applicant.ApplicantId), 3, 500, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Analytics_pages_large_canonical_and_accepted_offer_batches()
    {
        const string companyId = "company-large";
        var applicants = AnalyticsRepo(new List<Applicant>
        {
            new() { ApplicantId = "candidate-1", CompanyId = companyId, Phone = "redacted", ActivityType = ActivityType.Selected, CreatedDate = new DateTime(2026, 1, 1) }
        });
        var canonicalApplications = Enumerable.Range(1, 501)
            .Select(index => new RecruitmentJobApplication
            {
                ApplicationId = $"application-{index:D4}",
                ApplicantId = "candidate-1",
                CompanyId = companyId,
                CurrentStage = index == 501 ? "Hired" : "Interview",
                UpdatedDate = new DateTime(2026, 1, 1).AddMinutes(index)
            }).ToList();
        var canonical = AnalyticsRepo(canonicalApplications);
        var acceptedOfferRecords = Enumerable.Range(1, 501)
            .Select(index => new RecruitmentOffer
            {
                OfferId = $"offer-{index:D4}",
                ApplicantId = "candidate-1",
                CompanyId = companyId,
                Status = RecruitmentOfferStatus.Accepted,
                RespondedAtUtc = new DateTime(2026, 1, 2)
            }).ToList();
        var offerRepo = AnalyticsRepo(acceptedOfferRecords);
        var service = new RecruitmentEnterpriseService(
            applicants.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            offerRepo.Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId),
            canonical.Object);

        var response = await service.Analytics(null);

        Assert.True(response.Success);
        Assert.Equal(1, response.MethodResult!.ByStage["Hired"]);
        Assert.Equal(1, response.MethodResult.ByStage["Selected"]);
        Assert.Equal(501, response.MethodResult.TimeToHireSampleSize);
        Assert.Equal(1m, response.MethodResult.AverageTimeToHireDays);
        canonical.Verify(x => x.GetAggregateDataAsync<RecruitmentJobApplication>(
            It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<RecruitmentJobApplication, RecruitmentJobApplication>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(RecruitmentJobApplication.UpdatedDate), 1, 500, It.IsAny<string>()), Times.Once);
        canonical.Verify(x => x.GetAggregateDataAsync<RecruitmentJobApplication>(
            It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<RecruitmentJobApplication, RecruitmentJobApplication>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(RecruitmentJobApplication.UpdatedDate), 2, 500, It.IsAny<string>()), Times.Once);
        offerRepo.Verify(x => x.GetAggregateDataAsync<RecruitmentOffer>(
            It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<RecruitmentOffer, RecruitmentOffer>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(RecruitmentOffer.OfferId), 1, 500, It.IsAny<string>()), Times.Once);
        offerRepo.Verify(x => x.GetAggregateDataAsync<RecruitmentOffer>(
            It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<RecruitmentOffer, RecruitmentOffer>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), nameof(RecruitmentOffer.OfferId), 2, 500, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task PreboardingQueue_populates_candidate_name_email_and_vacancy_title()
    {
        const string companyId = "tenant-codeji";

        var cases = new List<RecruitmentPreboardingCase>
        {
            new()
            {
                CaseId = "case-1",
                ApplicantId = "app-1",
                CompanyId = companyId,
                Status = "InProgress",
                Tasks = [new() { Code = "candidate-details", Status = "Pending" }]
            }
        };

        var applicants = new List<Applicant>
        {
            new() { ApplicantId = "app-1", CompanyId = companyId, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com", VacancyId = "vac-1", Phone = "123" }
        };

        var vacancies = new List<JobVacancy>
        {
            new() { JobId = "vac-1", CompanyId = companyId, Title = "Senior Backend Engineer" }
        };

        var caseRepo = new Mock<IMongoDbRepository<RecruitmentPreboardingCase>>();
        caseRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentPreboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentPreboardingCase, bool>> filter, bool _, bool __) => cases.AsQueryable().Where(filter).ToList());

        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => applicants.AsQueryable().Where(filter).ToList());

        var vacancyRepo = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancyRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<JobVacancy, bool>> filter, bool _, bool __) => vacancies.AsQueryable().Where(filter).ToList());

        var service = new RecruitmentEnterpriseService(
            applicantRepo.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOffer>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId),
            preboardingCases: caseRepo.Object,
            vacancies: vacancyRepo.Object
        );

        var result = await service.PreboardingQueue();

        Assert.True(result.Success);
        Assert.Single(result.MethodResult!);
        var item = result.MethodResult![0];

        Assert.Equal("Jane Doe", item.ApplicantName);
        Assert.Equal("jane@example.com", item.Email);
        Assert.Equal("Senior Backend Engineer", item.VacancyName);
        Assert.Equal("vac-1", item.VacancyId);
    }

    [Fact]
    public async Task OnboardingQueue_populates_candidate_name_email_and_vacancy_title()
    {
        const string companyId = "tenant-codeji";

        var cases = new List<RecruitmentOnboardingCase>
        {
            new()
            {
                CaseId = "onb-1",
                ApplicantId = "app-10",
                CompanyId = companyId,
                Status = "InProgress",
                Tasks = [new() { Code = "asset-handover", Title = "Handover Laptop", Status = "Pending" }]
            }
        };

        var applicants = new List<Applicant>
        {
            new() { ApplicantId = "app-10", CompanyId = companyId, FirstName = "Carlos", LastName = "Santana", Email = "carlos@example.com", VacancyId = "vac-2", Phone = "456" }
        };

        var vacancies = new List<JobVacancy>
        {
            new() { JobId = "vac-2", CompanyId = companyId, Title = "Lead QA Engineer" }
        };

        var caseRepo = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        caseRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentOnboardingCase, bool>> filter, bool _, bool __) => cases.AsQueryable().Where(filter).ToList());

        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => applicants.AsQueryable().Where(filter).ToList());

        var vacancyRepo = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancyRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<JobVacancy, bool>> filter, bool _, bool __) => vacancies.AsQueryable().Where(filter).ToList());

        var service = new RecruitmentOnboardingService(
            caseRepo.Object,
            TenantContext(companyId),
            applicants: applicantRepo.Object,
            vacancies: vacancyRepo.Object
        );

        var result = await service.GetQueue(null, 1, 10);

        Assert.True(result.Success);
        Assert.Single(result.MethodResults!);
        var item = result.MethodResults![0];

        Assert.Equal("Carlos Santana", item.ApplicantName);
        Assert.Equal("carlos@example.com", item.Email);
        Assert.Equal("Lead QA Engineer", item.VacancyName);
        Assert.Equal("vac-2", item.VacancyId);
    }
}
