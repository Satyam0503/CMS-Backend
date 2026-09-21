using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentHandoffService
{
    Task<Result<string>> HandoffToPreboarding(CandidateHandoffRequest request, string actorUserId);
}
