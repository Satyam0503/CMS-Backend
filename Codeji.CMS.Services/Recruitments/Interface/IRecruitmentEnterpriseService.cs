using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentEnterpriseService
{
 Task<Result<string>> Merge(RecruitmentApplicantMergeCommand command,string actor);
 Task<Result<RecruitmentAnalytics>> Analytics(string? vacancyId);
 Task<Result<RecruitmentFeedbackSlaReport>> FeedbackSla(string? vacancyId);
 Task<Result<int>> QueueFeedbackReminders(string? vacancyId, string actorUserId);
 Task<Result<string>> UpsertSla(RecruitmentStageSlaPolicyCommand command,string actor);
 Task<Result<string>> UpsertConfig(RecruitmentEnterpriseConfigCommand command,string actor);
 Task<Result<RecruitmentEnterpriseConfig>> GetConfig();
 Task<Result<string>> Link(RecruitmentRelationshipLinkCommand command,string actor);
 Task<Result<string>> Enqueue(RecruitmentAutomationEventCommand command,string actor);
}



