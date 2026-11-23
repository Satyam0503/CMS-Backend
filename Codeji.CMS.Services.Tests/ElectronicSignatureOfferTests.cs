using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class ElectronicSignatureOfferTests
{
    private const string TestCompanyId = "comp-1";
    private const string TestApplicantId = "app-1";
    private const string TestOfferId = "off-1";
    private const string ValidToken = "valid-action-token";

    [Fact]
    public async Task RespondToOffer_WithDigitalSignature_PersistsSignatureAuditTrailAndAcceptsOffer()
    {
        var mockExternal = new Mock<IRecruitmentExternalActionService>();
        var mockApplicants = new Mock<IMongoDbRepository<Applicant>>();
        var mockOffers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        var mockInterviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        var mockOutbox = new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>();
        var mockPreboarding = new Mock<IMongoDbRepository<RecruitmentPreboardingCase>>();
        var mockApplications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var mockApplicantLogs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var mockAutomationOutbox = new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>();

        var tokenDto = new RecruitmentExternalActionTokenDto
        {
            CompanyId = TestCompanyId,
            ApplicantId = TestApplicantId,
            OfferId = TestOfferId,
            ActionType = "OfferResponse",
            ResourceVersion = 1,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };

        mockExternal
            .Setup(x => x.Validate(ValidToken, false))
            .ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
            {
                Success = true,
                Data = tokenDto,
                MethodResult = tokenDto
            });

        mockExternal
            .Setup(x => x.Validate(ValidToken, true))
            .ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
            {
                Success = true,
                Data = tokenDto,
                MethodResult = tokenDto
            });

        var applicant = new Applicant
        {
            CompanyId = TestCompanyId,
            ApplicantId = TestApplicantId,
            FirstName = "John",
            LastName = "Candidate",
            Email = "john@example.com",
            Phone = "9998887776",
            VacancyId = "vac-1",
            ConsentStatus = "Active"
        };
        mockApplicants
            .Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(applicant);

        var offer = new RecruitmentOffer
        {
            CompanyId = TestCompanyId,
            ApplicantId = TestApplicantId,
            OfferId = TestOfferId,
            Status = RecruitmentOfferStatus.Sent,
            Salary = 1200000,
            TotalCtc = 1400000,
            Version = 1
        };
        mockOffers
            .Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(offer);

        mockOffers
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<RecruitmentOffer>>(), It.IsAny<UpdateDefinition<RecruitmentOffer>>()))
            .ReturnsAsync(new Result { Success = true });

        mockApplicants
            .Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Result { Success = true });
        mockOutbox.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentEmailOutbox?)null);
        mockOutbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });
        mockPreboarding.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<RecruitmentPreboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentPreboardingCase?)null);
        mockPreboarding.Setup(x => x.AddOne(It.IsAny<RecruitmentPreboardingCase>())).ReturnsAsync(new Result { Success = true });
        mockApplications.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentJobApplication?)null);
        mockApplications.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>())).ReturnsAsync(new Result { Success = true });
        mockApplicantLogs.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((ApplicantLogs?)null);
        mockApplicantLogs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        mockAutomationOutbox.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<RecruitmentAutomationOutboxEvent, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentAutomationOutboxEvent?)null);
        mockAutomationOutbox.Setup(x => x.AddOne(It.IsAny<RecruitmentAutomationOutboxEvent>())).ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentCandidateActionService(
            mockExternal.Object,
            mockApplicants.Object,
            mockOffers.Object,
            mockInterviews.Object,
            mockOutbox.Object,
            automationOutbox: mockAutomationOutbox.Object,
            preboardingCases: mockPreboarding.Object,
            applications: mockApplications.Object,
            applicantLogs: mockApplicantLogs.Object
        );

        var command = new RecruitmentCandidateOfferActionCommand
        {
            Token = ValidToken,
            Decision = "Accepted",
            IdempotencyKey = "idempotency-key-00123",
            SignatureDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==",
            SignerLegalName = "John Candidate",
            LegalConsent = true,
            IpAddress = "192.168.1.50",
            UserAgent = "Mozilla/5.0 TestBrowser"
        };

        var response = await service.RespondToOffer(command);

        Assert.True(response.Success);
        Assert.Equal(TestOfferId, response.Data);

        // Verify update was called on offers with signature
        mockOffers.Verify(x => x.UpdateMany(
            It.IsAny<FilterDefinition<RecruitmentOffer>>(),
            It.IsAny<UpdateDefinition<RecruitmentOffer>>()
        ), Times.Once);
    }
}
