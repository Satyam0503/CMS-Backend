using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentHiringDecisionService
{
    Task<Result<string>> Upsert(RecruitmentHiringDecisionCommand command, string actorUserId);
}
