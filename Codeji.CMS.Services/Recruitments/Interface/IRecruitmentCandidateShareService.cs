using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentCandidateShareService
{
    Task<Result<string>> Grant(RecruitmentCandidateShareCommand command, string actorUserId);
    Task<Result<string>> Revoke(string shareId, string actorUserId);
    Task<Result<RecruitmentCandidateShareItem>> GetForApplicant(string applicantId);
}
