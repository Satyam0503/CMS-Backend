using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentTalentPoolService
{
    Task<Result<string>> Upsert(RecruitmentTalentPoolCommand command, string actorUserId);
    Task<Result<List<RecruitmentTalentPoolMember>>> GetMembers(string? poolName, string? status = null);
}
