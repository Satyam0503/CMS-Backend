using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentScreeningService
{
    Task<Result<string>> Upsert(RecruitmentScreeningReviewCommand command, string actorUserId);
    Task<Result<RecruitmentScreeningBulkResult>> Bulk(RecruitmentScreeningBulkCommand command, string actorUserId);
}