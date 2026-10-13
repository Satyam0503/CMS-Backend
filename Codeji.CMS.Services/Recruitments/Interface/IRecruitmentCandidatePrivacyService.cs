using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentCandidatePrivacyService
{
    Task<Result<string>> Apply(RecruitmentCandidatePrivacyCommand command, string actorUserId);
}
