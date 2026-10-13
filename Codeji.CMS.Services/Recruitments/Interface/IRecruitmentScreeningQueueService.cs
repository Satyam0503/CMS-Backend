using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentScreeningQueueService
{
    Task<Result<RecruitmentScreeningQueueReport>> Get(string? vacancyId, int overdueAfterHours = 48, string? reviewerUserId = null, int? minScore = null, int? maxScore = null, DateTime? submittedAfterUtc = null, DateTime? submittedBeforeUtc = null);
}
