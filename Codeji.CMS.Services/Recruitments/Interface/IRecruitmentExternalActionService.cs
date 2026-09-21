using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentExternalActionService
{
    Task<Result<RecruitmentExternalActionTokenDto>> Issue(RecruitmentExternalActionIssueCommand command, string actor);
    Task<Result<RecruitmentExternalActionTokenDto>> Validate(string token, bool consume);
    Task<Result<string>> Revoke(string actionTokenId, string actor);
    Task<Result<string>> QueueEmail(RecruitmentEmailQueueCommand command, string actor);
}
