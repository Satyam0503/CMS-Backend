using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Codeji.CMS.API.Controllers;
[ApiController, Route("api/[controller]"), Authorize]
public sealed class RecruitmentEnterpriseController(IRecruitmentEnterpriseService service,IHttpContextAccessor context) : BaseApiController
{
 [HttpPost("MergeApplicants"), ModulePermission(AppModule.Applications, Permission.Edit)] public Task<Result<string>> MergeApplicants([FromBody] RecruitmentApplicantMergeCommand command)=>service.Merge(command,CurrentContext.UserId(context));
 [HttpGet("Analytics"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<RecruitmentAnalytics>> Analytics([FromQuery]string? vacancyId)=>service.Analytics(vacancyId);
 [HttpGet("SelectedCandidates"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<List<RecruitmentSelectedCandidateQueueItem>>> SelectedCandidates([FromQuery]string? vacancyId)=>service.SelectedCandidates(vacancyId);
 [HttpGet("PendingOffers"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<List<RecruitmentOfferPendingQueueItem>>> PendingOffers([FromQuery]string? vacancyId)=>service.PendingOffers(vacancyId);
 [HttpPost("UpdatePreboarding"), ModulePermission(AppModule.Applications, Permission.Edit)] public Task<Result<string>> UpdatePreboarding([FromBody]RecruitmentPreboardingUpdateCommand command)=>service.UpdatePreboarding(command,CurrentContext.UserId(context));
 [HttpGet("PreboardingQueue"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<List<RecruitmentPreboardingQueueItem>>> PreboardingQueue()=>service.PreboardingQueue();
 [HttpGet("FeedbackSla"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<RecruitmentFeedbackSlaReport>> FeedbackSla([FromQuery]string? vacancyId)=>service.FeedbackSla(vacancyId);
 [HttpGet("DebriefPending"), ModulePermission(AppModule.Applications, Permission.View)] public Task<Result<RecruitmentFeedbackSlaReport>> DebriefPending([FromQuery]string? vacancyId)=>service.FeedbackSla(vacancyId);
 [HttpPost("QueueFeedbackReminders"), ModulePermission(AppModule.Applications, Permission.Edit)] public Task<Result<int>> QueueFeedbackReminders([FromQuery]string? vacancyId)=>service.QueueFeedbackReminders(vacancyId,CurrentContext.UserId(context));
 [HttpPost("UpsertSlaPolicy"), ModulePermission(AppModule.Jobs, Permission.Edit)] public Task<Result<string>> UpsertSlaPolicy([FromBody]RecruitmentStageSlaPolicyCommand command)=>service.UpsertSla(command,CurrentContext.UserId(context));
 [HttpGet("Config"), ModulePermission(AppModule.Jobs, Permission.View)] public Task<Result<Codeji.CMS.Repository.Entities.Recruitments.RecruitmentEnterpriseConfig>> Config()=>service.GetConfig();
 [HttpGet("Readiness"), ModulePermission(AppModule.Jobs, Permission.View)] public Task<Result<RecruitmentReadinessDto>> Readiness()=>service.GetReadiness();
 [HttpPost("UpsertConfig"), ModulePermission(AppModule.Jobs, Permission.Edit)] public Task<Result<string>> UpsertConfig([FromBody]RecruitmentEnterpriseConfigCommand command)=>service.UpsertConfig(command,CurrentContext.UserId(context));
 [HttpPost("LinkRelationship"), ModulePermission(AppModule.Applications, Permission.Edit)] public Task<Result<string>> LinkRelationship([FromBody]RecruitmentRelationshipLinkCommand command)=>service.Link(command,CurrentContext.UserId(context));
 [HttpPost("QueueAutomation"), ModulePermission(AppModule.Applications, Permission.Edit)] public Task<Result<string>> QueueAutomation([FromBody]RecruitmentAutomationEventCommand command)=>service.Enqueue(command,CurrentContext.UserId(context));
}




