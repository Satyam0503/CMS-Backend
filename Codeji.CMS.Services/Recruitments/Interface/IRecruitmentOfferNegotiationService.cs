using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentOfferNegotiationService
{
    Task<Result<string>> CreateVersion(RecruitmentOfferVersionCommand command, string actor);
    Task<Result<string>> RequestCounter(RecruitmentOfferCounterCommand command, string actor);
    Task<Result<string>> DecideCounter(RecruitmentOfferCounterDecisionCommand command, string actor);
}
