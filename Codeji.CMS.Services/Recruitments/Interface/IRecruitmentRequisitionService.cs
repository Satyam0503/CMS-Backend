using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentRequisitionService
{
    Task<Result<string>> Upsert(RecruitmentRequisitionCommand command, string actorUserId);
}