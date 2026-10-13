using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentOnboardingService
{
    Task<Result<RecruitmentOnboardingCase>> Get(string applicantId);
    Task<Result<RecruitmentOnboardingQueueItem>> GetQueue(string? status, int pageNo, int pageSize);
    Task<Result<string>> Update(RecruitmentOnboardingUpdateCommand command, string actorUserId);
    Task<Result<string>> RetryActivation(string caseId, string actorUserId);
}



