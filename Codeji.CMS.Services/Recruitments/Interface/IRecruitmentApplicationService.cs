using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentApplicationService
{
    Task<Result<string>> UpsertStage(RecruitmentApplicationStageCommand command, string actorUserId);
    Task<Result<int>> BackfillLegacy(string actorUserId);
    Task<Result<List<RecruitmentApplicationStageEvent>>> GetHistory(string applicationId);
}

