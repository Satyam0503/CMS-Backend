using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentResumeExtractionService
{
    Task<Result<string>> Upsert(RecruitmentResumeExtractionReviewCommand command, string actorUserId);
}
