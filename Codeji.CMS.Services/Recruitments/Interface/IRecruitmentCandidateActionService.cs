using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentCandidateActionService
{
    Task<Result<RecruitmentCandidateOfferDetailsDto>> RecordOfferView(RecruitmentCandidateOfferViewCommand command);
    Task<Result<string>> RespondToOffer(RecruitmentCandidateOfferActionCommand command);
    Task<Result<string>> ConfirmInterview(RecruitmentCandidateInterviewActionCommand command);
}
