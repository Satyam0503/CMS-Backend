using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentOfferApprovalPolicyService
{
    Task<Result<string>> Upsert(RecruitmentOfferApprovalPolicyCommand command, string actorUserId);
    Task<Result<RecruitmentOfferApprovalPolicy>> Get(string? vacancyId);
}
