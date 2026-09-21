using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentInterviewService
{
    Task<Result<string>> Upsert(RecruitmentInterviewCommand command, string actorUserId);
}

public interface IRecruitmentOfferService
{
    Task<Result<string>> Upsert(RecruitmentOfferCommand command, string actorUserId);
}
