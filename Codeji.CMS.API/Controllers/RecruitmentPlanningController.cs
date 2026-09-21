using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentPlanningController(
    IRecruitmentRequisitionService requisitionService,
    IRecruitmentScorecardService scorecardService,
    IRecruitmentScreeningService screeningService,
    IRecruitmentInterviewDebriefService debriefService,
    IRecruitmentInterviewPlanService interviewPlanService,
    IRecruitmentOfferApprovalPolicyService offerApprovalPolicyService,
    IRecruitmentTalentPoolService talentPoolService,
    IHttpContextAccessor context) : BaseApiController
{
    [HttpPost("UpsertRequisition")]
    [ModulePermission(AppModule.Jobs, Permission.Edit)]
    public Task<Result<string>> UpsertRequisition([FromBody] RecruitmentRequisitionCommand command) =>
        requisitionService.Upsert(command, CurrentContext.UserId(context));

    [HttpPost("UpsertScreening")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> UpsertScreening([FromBody] RecruitmentScreeningReviewCommand command) =>
        screeningService.Upsert(command, CurrentContext.UserId(context));

    [HttpGet("OfferApprovalPolicy")]
    [ModulePermission(AppModule.Jobs, Permission.View)]
    public Task<Result<RecruitmentOfferApprovalPolicy>> GetOfferApprovalPolicy([FromQuery] string? vacancyId) => offerApprovalPolicyService.Get(vacancyId);

    [HttpPost("UpsertOfferApprovalPolicy")]
    [ModulePermission(AppModule.Jobs, Permission.Edit)]
    public Task<Result<string>> UpsertOfferApprovalPolicy([FromBody] RecruitmentOfferApprovalPolicyCommand command) => offerApprovalPolicyService.Upsert(command, CurrentContext.UserId(context));

    [HttpGet("InterviewPlan")]
    [ModulePermission(AppModule.Jobs, Permission.View)]
    public Task<Result<RecruitmentInterviewPlan>> GetInterviewPlan([FromQuery] string? vacancyId) => interviewPlanService.Get(vacancyId);

    [HttpPost("UpsertInterviewPlan")]
    [ModulePermission(AppModule.Jobs, Permission.Edit)]
    public Task<Result<string>> UpsertInterviewPlan([FromBody] RecruitmentInterviewPlanCommand command) =>
        interviewPlanService.Upsert(command, CurrentContext.UserId(context));

    [HttpPost("CompleteInterviewDebrief")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> CompleteInterviewDebrief([FromBody] RecruitmentInterviewDebriefCommand command) =>
        debriefService.Complete(command, CurrentContext.UserId(context));

    [HttpPost("UpsertScorecard")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> UpsertScorecard([FromBody] RecruitmentScorecardCommand command) =>
        scorecardService.Upsert(command, CurrentContext.UserId(context));

    [HttpPost("UpsertTalentPoolMember")]
    [ModulePermission(AppModule.Applications, Permission.Edit)]
    public Task<Result<string>> UpsertTalentPoolMember([FromBody] RecruitmentTalentPoolCommand command) =>
        talentPoolService.Upsert(command, CurrentContext.UserId(context));
}
// Scorecard commands share the Applications.Edit permission and existing audit stream.









