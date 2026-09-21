using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentInterviewDebriefService
{
    Task<Result<string>> Complete(RecruitmentInterviewDebriefCommand command, string actorUserId);
}
