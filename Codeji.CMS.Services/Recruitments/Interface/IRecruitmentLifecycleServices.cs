using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentInterviewService
{
    Task<Result<string>> Upsert(RecruitmentInterviewCommand command, string actorUserId);
    Task<Result<string>> RetryCalendar(string interviewId, string actorUserId);
    Task<Result<List<RecruitmentInterview>>> GetRounds(string applicantId, string vacancyId);
}

public interface IRecruitmentOfferService
{
    Task<Result<string>> Upsert(RecruitmentOfferCommand command, string actorUserId);
}
