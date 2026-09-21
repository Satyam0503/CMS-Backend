using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentScorecardService
{
    Task<Result<string>> Upsert(RecruitmentScorecardCommand command, string actorUserId);
}