using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using System.Security.Claims;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentSprint0SecurityTests
{
    [Fact]
    public async Task RequestOfferChange_updates_offer_then_consumes_matching_token()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("raw-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto {
                CompanyId = "company-a",
ActionType = "RequestOfferChange",
                ApplicantId = "app-1",
                OfferId = "offer-1"
            }
        });
        external.Setup(x => x.Validate("raw-token", true)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto()
        });

        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 3 });
        offers.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, outbox.Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "raw-token",
            Decision = "RequestOfferChange",
            Message = "Please review the start date.",
            IdempotencyKey = "candidate-command-001"
        });

        Assert.True(result.Success);
        Assert.Equal("offer-1", result.MethodResult);
        external.Verify(x => x.Validate("raw-token", true), Times.Once);
        offers.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Offer_link_rejects_decision_that_does_not_match_action_type()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("raw-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto {
                CompanyId = "company-a",
ActionType = "AcceptOffer",
                ApplicantId = "app-1",
                OfferId = "offer-1"
            }
        });

        var service = new RecruitmentCandidateActionService(
            external.Object,
            Repository<Applicant>().Object,
            Repository<RecruitmentOffer>().Object,
            Repository<RecruitmentInterview>().Object,
            Repository<RecruitmentEmailOutbox>().Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "raw-token",
            Decision = "Declined",
            IdempotencyKey = "candidate-command-002"
        });

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
        external.Verify(x => x.Validate("raw-token", true), Times.Never);
    }

    private static Mock<IMongoDbRepository<T>> Repository<T>() where T : class
        => new();
}






