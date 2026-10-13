using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentInterviewPlanService
{
    Task<Result<string>> Upsert(RecruitmentInterviewPlanCommand command, string actorUserId);
    Task<Result<RecruitmentInterviewPlan>> Get(string? vacancyId);
}



