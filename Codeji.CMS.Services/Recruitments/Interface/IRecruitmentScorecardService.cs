using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentScorecardService
{
    Task<Result<string>> Upsert(RecruitmentScorecardCommand command, string actorUserId);
    Task<Result<List<RecruitmentScorecard>>> GetByApplicant(string applicantId, string vacancyId);
    Task<Result<RecruitmentFinalScoreResult>> GetFinalInterviewScore(string applicantId);
}
