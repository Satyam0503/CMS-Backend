using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentDuplicateDetectionService
{
    Task<Result<RecruitmentDuplicateCandidate>> Find(RecruitmentDuplicateLookupRequest request);
}
