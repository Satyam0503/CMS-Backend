using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentResumeExtractionService
{
    Task<Result<string>> Upsert(RecruitmentResumeExtractionReviewCommand command, string actorUserId);
    Task<Result<RecruitmentResumeExtractionReview?>> Get(string applicantId, string vacancyId);
}
