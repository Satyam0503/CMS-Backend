using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentOfferNegotiationService
{
    Task<Result<string>> CreateVersion(RecruitmentOfferVersionCommand command, string actor);
    Task<Result<List<RecruitmentOfferVersion>>> GetVersions(string offerId);
    Task<Result<List<RecruitmentOfferCounterRequest>>> GetCounters(string offerId);
    Task<Result<string>> RequestCounter(RecruitmentOfferCounterCommand command, string actor);
    Task<Result<string>> DecideCounter(RecruitmentOfferCounterDecisionCommand command, string actor);
}
