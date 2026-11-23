using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentEnterpriseService(
 IMongoDbRepository<Applicant> applicants,
 IMongoDbRepository<ApplicantLogs> logs,
 IMongoDbRepository<RecruitmentInterview> interviews,
 IMongoDbRepository<RecruitmentOffer> offers,
 IMongoDbRepository<RecruitmentOfferVersion> offerVersions,
 IMongoDbRepository<RecruitmentOfferCounterRequest> offerCounters,
 IMongoDbRepository<RecruitmentScorecard> scorecards,
 IMongoDbRepository<RecruitmentTalentPoolMember> talentPoolMembers,
 IMongoDbRepository<RecruitmentHandoff> handoffs,
 IMongoDbRepository<RecruitmentExternalActionToken> externalTokens,
 IMongoDbRepository<RecruitmentStageSlaPolicy> slaPolicies,
 IMongoDbRepository<RecruitmentEnterpriseConfig> configs,
 IMongoDbRepository<RecruitmentAutomationOutboxEvent> outbox,
 IMongoDbRepository<RecruitmentRelationshipLink> links,
 IHttpContextAccessor context,
 IMongoDbRepository<RecruitmentJobApplication>? applications = null,
 IMongoDbRepository<RecruitmentPreboardingCase>? preboardingCases = null,
 IMongoDbRepository<RecruitmentScreeningReview>? screeningReviews = null,
 IMongoDbRepository<RecruitmentHiringDecision>? hiringDecisions = null,
 IMongoDbRepository<RecruitmentOnboardingCase>? onboardingCases = null,
 IMongoDbRepository<JobVacancy>? vacancies = null,
 IMongoDbRepository<EmpUser>? employees = null,
 IRecruitmentExternalActionService? externalActions = null) : IRecruitmentEnterpriseService
{
 private const int AnalyticsPageSize = 500;

 internal static readonly ProjectionDefinition<Applicant, Applicant> AnalyticsApplicantProjection =
  Builders<Applicant>.Projection.Expression(applicant => new Applicant
  {
   ApplicantId = applicant.ApplicantId,
   Phone = string.Empty,
   ActivityType = applicant.ActivityType,
   CreatedDate = applicant.CreatedDate,
   IsDeleted = applicant.IsDeleted,
   MergeStatus = applicant.MergeStatus,
   CandidateReportedSource = applicant.CandidateReportedSource,
  });

 internal static readonly ProjectionDefinition<RecruitmentJobApplication, RecruitmentJobApplication> AnalyticsApplicationProjection =
  Builders<RecruitmentJobApplication>.Projection.Expression(application => new RecruitmentJobApplication
  {
   ApplicantId = application.ApplicantId,
   CurrentStage = application.CurrentStage,
   UpdatedDate = application.UpdatedDate,
  });

 internal static readonly ProjectionDefinition<RecruitmentJobApplication, RecruitmentJobApplication> StageSlaApplicationProjection =
  Builders<RecruitmentJobApplication>.Projection.Expression(application => new RecruitmentJobApplication
  {
   ApplicationId = application.ApplicationId,
   ApplicantId = application.ApplicantId,
   VacancyId = application.VacancyId,
   CurrentStage = application.CurrentStage,
   StageHistory = application.StageHistory,
   CreatedDate = application.CreatedDate,
  });

 internal static readonly ProjectionDefinition<RecruitmentOffer, RecruitmentOffer> AnalyticsOfferProjection =
  Builders<RecruitmentOffer>.Projection.Expression(offer => new RecruitmentOffer
  {
   ApplicantId = offer.ApplicantId,
   RespondedAtUtc = offer.RespondedAtUtc,
  });

 public async Task<Result<string>> Merge(RecruitmentApplicantMergeCommand command,string actor)
 {
  var company=CurrentContext.CompanyId(context); var key=command.IdempotencyKey?.Trim();
  if(string.IsNullOrWhiteSpace(company)||string.IsNullOrWhiteSpace(key)||command.SourceApplicantId==command.TargetApplicantId)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Invalid merge request.");
  var source=await applicants.FirstOrDefault(x=>x.CompanyId==company&&x.ApplicantId==command.SourceApplicantId&&!x.IsDeleted); var target=await applicants.FirstOrDefault(x=>x.CompanyId==company&&x.ApplicantId==command.TargetApplicantId&&!x.IsDeleted);
  if(source is null||target is null)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Both applicants must belong to the current company.");
  if(source.MergeStatus=="Merged")return Ok(source.ApplicantId,"Merge already applied.");
  var version=source.Version<=0?1:source.Version; if(command.ExpectedSourceVersion.HasValue&&command.ExpectedSourceVersion.Value!=version)return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Source applicant was changed by another user.",409);
  if(!await MigrateApplicantReferences(company,source.ApplicantId,target.ApplicantId,actor))return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Applicant related history could not be preserved; merge was not completed.",409);
  source.MergedIntoApplicantId=target.ApplicantId; source.MergeStatus="Merged"; source.Version=version+1; source.UpdatedBy=actor; source.UpdatedDate=DateTime.UtcNow; source.IsDeleted=true;
  var updated=await applicants.Update(Builders<Applicant>.Filter.Where(x=>x.CompanyId==company&&x.ApplicantId==source.ApplicantId&&(x.Version==version||x.Version==0)),source); if(!updated.Success)return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Merge could not be completed.",409);
  await logs.AddOne(new ApplicantLogs{CompanyId=company,ApplicantId=target.ApplicantId,ApplicantName=(target.FirstName+" "+target.LastName).Trim(),JobRole=target.VacancyId,UserId=actor,ActivityCategory=0,Description="Applicant "+source.ApplicantId+" merged into "+target.ApplicantId+". "+command.Reason?.Trim(),EventType="ApplicantMerged",PreviousStage=source.ActivityType.ToString(),CurrentStage=target.ActivityType.ToString(),CreatedBy=actor,UpdatedBy=actor});
  return Ok(target.ApplicantId,"Applicant merge completed; related recruitment history was reassigned and the source remains as a soft-deleted audit record.");
 }
 private async Task<bool> MigrateApplicantReferences(string company,string source,string target,string actor)
 {
  var now=DateTime.UtcNow;
  var migrationTasks = new List<Task<Codeji.CMS.Domain.Models.Result>> { Migrate(interviews,company,source,target,actor,now), Migrate(offers,company,source,target,actor,now), Migrate(offerVersions,company,source,target,actor,now), Migrate(offerCounters,company,source,target,actor,now), Migrate(scorecards,company,source,target,actor,now), Migrate(talentPoolMembers,company,source,target,actor,now), Migrate(handoffs,company,source,target,actor,now), Migrate(logs,company,source,target,actor,now), Migrate(externalTokens,company,source,target,actor,now,true) };
  if (applications is not null) migrationTasks.Add(Migrate(applications,company,source,target,actor,now));
  if (preboardingCases is not null) migrationTasks.Add(Migrate(preboardingCases,company,source,target,actor,now));
  if (screeningReviews is not null) migrationTasks.Add(Migrate(screeningReviews,company,source,target,actor,now));
  if (hiringDecisions is not null) migrationTasks.Add(Migrate(hiringDecisions,company,source,target,actor,now));
  if (onboardingCases is not null) migrationTasks.Add(Migrate(onboardingCases,company,source,target,actor,now));
  var results=await Task.WhenAll(migrationTasks);
  return results.All(x=>x.Success);
 }
 private static Task<Codeji.CMS.Domain.Models.Result> Migrate<T>(IMongoDbRepository<T> repository,string company,string source,string target,string actor,DateTime now,bool revoke=false) where T:Codeji.CMS.Repository.Entities.BaseClass
 {
  var filter=Builders<T>.Filter.And(Builders<T>.Filter.Eq("CompanyId",company),Builders<T>.Filter.Eq("ApplicantId",source));
  var update=Builders<T>.Update.Set("UpdatedBy",actor).Set("UpdatedDate",now);
  update=revoke?update.Set("RevokedAtUtc",now):update.Set("ApplicantId",target);
  return repository.UpdateMany(filter,update);
 }
 public async Task<Result<RecruitmentAnalytics>> Analytics(string? vacancyId)
 {
  var company = CurrentContext.CompanyId(context);
  if (string.IsNullOrWhiteSpace(company))
   return Fail<RecruitmentAnalytics>(RecruitmentErrorCodes.InvalidRequest, "Company context is required.");

  var hasVacancyFilter = !string.IsNullOrWhiteSpace(vacancyId);
  Expression<Func<Applicant, bool>> activeFilter = hasVacancyFilter
   ? x => x.CompanyId == company && !x.IsDeleted && x.MergeStatus != "Merged" && x.VacancyId == vacancyId
   : x => x.CompanyId == company && !x.IsDeleted && x.MergeStatus != "Merged";
  Expression<Func<Applicant, bool>> mergedFilter = hasVacancyFilter
   ? x => x.CompanyId == company && x.MergeStatus == "Merged" && x.VacancyId == vacancyId
   : x => x.CompanyId == company && x.MergeStatus == "Merged";

  // The count query includes soft-deleted records so merged candidates remain visible in this metric.
  var totalApplicants = await applicants.Count(activeFilter);
  var mergedApplicants = await applicants.Count(mergedFilter, WithDeletedObjects: true);
  var config = await configs.FirstOrDefault(x => x.CompanyId == company && !x.IsDeleted);
  var configuredSourceOptions = PublicCareerApplicationRules.EffectiveCandidateSourceOptions(config?.CandidateSourceOptions);
  var sourceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
  var stageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
  var rejectedApplicants = 0;
  decimal totalTimeToHireDays = 0;
  var timeToHireSampleSize = 0;

  for (var pageNo = 1; ; pageNo++)
  {
   var batch = (await applicants.GetAggregateDataAsync<Applicant>(
    activeFilter,
    AnalyticsApplicantProjection,
    isAscending: true,
    orderedKey: nameof(Applicant.ApplicantId),
    pageNo: pageNo,
    pageSize: AnalyticsPageSize)).ToList();
   if (batch.Count == 0) break;

   var applicantIds = batch.Select(x => x.ApplicantId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
   var canonicalMap = new Dictionary<string, RecruitmentJobApplication>();
   if (applications is not null && applicantIds.Length > 0)
   {
    for (var canonicalPageNo = 1; ; canonicalPageNo++)
    {
     var canonicalBatch = (await applications.GetAggregateDataAsync<RecruitmentJobApplication>(
      x => x.CompanyId == company && !x.IsDeleted && applicantIds.Contains(x.ApplicantId) && (!hasVacancyFilter || x.VacancyId == vacancyId),
      AnalyticsApplicationProjection,
      isAscending: false,
      orderedKey: nameof(RecruitmentJobApplication.UpdatedDate),
      pageNo: canonicalPageNo,
      pageSize: AnalyticsPageSize)).ToList();
     foreach (var application in canonicalBatch)
     {
      if (!canonicalMap.TryGetValue(application.ApplicantId, out var previous) || application.UpdatedDate > previous.UpdatedDate)
       canonicalMap[application.ApplicantId] = application;
     }
     if (canonicalBatch.Count < AnalyticsPageSize) break;
    }
   }

   foreach (var row in batch)
   {
    var stage = canonicalMap.TryGetValue(row.ApplicantId, out var application) && !string.IsNullOrWhiteSpace(application.CurrentStage)
     ? application.CurrentStage
     : row.ActivityType.ToString();
    stageCounts[stage] = stageCounts.GetValueOrDefault(stage) + 1;
    if (string.Equals(stage, "Rejected", StringComparison.OrdinalIgnoreCase)) rejectedApplicants++;
    if (stage is "Preboarding" or "ReadyForHire" or "Hired")
     stageCounts["Selected"] = stageCounts.GetValueOrDefault("Selected") + 1;

    var source = PublicCareerApplicationRules.NormalizeCandidateReportedSource(row.CandidateReportedSource, configuredSourceOptions)
     ?? (string.IsNullOrWhiteSpace(row.CandidateReportedSource) ? "Not reported" : "Unclassified");
    sourceCounts[source] = sourceCounts.GetValueOrDefault(source) + 1;
   }

   if (applicantIds.Length > 0)
   {
    var createdByApplicant = batch.Where(x => x.CreatedDate.HasValue)
     .GroupBy(x => x.ApplicantId)
     .ToDictionary(group => group.Key, group => group.First().CreatedDate!.Value);
    for (var offerPageNo = 1; ; offerPageNo++)
    {
     var acceptedOffers = (await offers.GetAggregateDataAsync<RecruitmentOffer>(
      x => x.CompanyId == company && !x.IsDeleted && x.Status == RecruitmentOfferStatus.Accepted && applicantIds.Contains(x.ApplicantId) && (!hasVacancyFilter || x.VacancyId == vacancyId),
      AnalyticsOfferProjection,
      isAscending: true,
      orderedKey: nameof(RecruitmentOffer.OfferId),
      pageNo: offerPageNo,
      pageSize: AnalyticsPageSize)).ToList();
     foreach (var offer in acceptedOffers)
     {
      if (!offer.RespondedAtUtc.HasValue || !createdByApplicant.TryGetValue(offer.ApplicantId, out var createdAt)) continue;
      var days = (decimal)(offer.RespondedAtUtc.Value - createdAt).TotalDays;
      if (days < 0) continue;
      totalTimeToHireDays += days;
      timeToHireSampleSize++;
     }
     if (acceptedOffers.Count < AnalyticsPageSize) break;
    }
   }

   if (batch.Count < AnalyticsPageSize) break;
  }

  var analytics = new RecruitmentAnalytics
  {
   TotalApplicants = totalApplicants,
   ActiveApplicants = totalApplicants - rejectedApplicants,
   RejectedApplicants = rejectedApplicants,
   MergedApplicants = mergedApplicants,
   ByStage = stageCounts,
   ByCandidateReportedSource = sourceCounts,
   TimeToHireSampleSize = timeToHireSampleSize,
   AverageTimeToHireDays = timeToHireSampleSize == 0 ? null : Math.Round(totalTimeToHireDays / timeToHireSampleSize, 2)
  };

  return new Result<RecruitmentAnalytics>
  {
   Success = true,
   StatusCode = 200,
   Message = "Recruitment analytics loaded.",
   MethodResult = analytics,
   Data = analytics,
   TotalRecords = analytics.TotalApplicants
  };
 }
 public async Task<Result<List<RecruitmentSelectedCandidateQueueItem>>> SelectedCandidates(string? vacancyId)
 {
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company)) return new Result<List<RecruitmentSelectedCandidateQueueItem>>{Success=false,StatusCode=400,ErrorCode=RecruitmentErrorCodes.InvalidRequest,Message="Company context is required."};
  var rows=(await applicants.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&x.ActivityType==ActivityType.Selected&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId))).OrderByDescending(x=>x.UpdatedDate).ToList();
  var items=rows.Select(x=>new RecruitmentSelectedCandidateQueueItem{ApplicantId=x.ApplicantId,VacancyId=x.VacancyId,ApplicantName=$"{x.FirstName} {x.LastName}".Trim(),Email=x.Email,Stage=x.ActivityType.ToString(),IsEmployee=false,SelectedAtUtc=x.UpdatedDate??x.CreatedDate??DateTime.UtcNow}).ToList();
  return new Result<List<RecruitmentSelectedCandidateQueueItem>>{Success=true,StatusCode=200,MethodResult=items,Data=items,TotalRecords=items.Count,Message="Selected candidate queue loaded."};
 }
 public async Task<Result<List<RecruitmentOfferPendingQueueItem>>> PendingOffers(string? vacancyId)
 {
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company)) return new Result<List<RecruitmentOfferPendingQueueItem>>{Success=false,StatusCode=400,ErrorCode=RecruitmentErrorCodes.InvalidRequest,Message="Company context is required."};
  var pending=(await offers.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&(x.Status!=RecruitmentOfferStatus.Withdrawn&&x.Status!=RecruitmentOfferStatus.Expired)&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId))).ToList();
  var ids=pending.Select(x=>x.ApplicantId).Distinct().ToHashSet(); var names=(await applicants.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&ids.Contains(x.ApplicantId))).ToDictionary(x=>x.ApplicantId,x=>$"{x.FirstName} {x.LastName}".Trim());
  var items=pending.OrderByDescending(x=>x.UpdatedDate).Select(x=>new RecruitmentOfferPendingQueueItem{OfferId=x.OfferId,ApplicantId=x.ApplicantId,VacancyId=x.VacancyId,ApplicantName=names.GetValueOrDefault(x.ApplicantId,string.Empty),Status=x.Status,Salary=x.Salary,Currency=x.Currency,ApprovalRequired=x.Status==RecruitmentOfferStatus.PendingApproval}).ToList();
  return new Result<List<RecruitmentOfferPendingQueueItem>>{Success=true,StatusCode=200,MethodResult=items,Data=items,TotalRecords=items.Count,Message="Offer-pending queue loaded."};
 }
 public async Task<Result<List<RecruitmentPreboardingQueueItem>>> PreboardingQueue()
 {
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company)) return new Result<List<RecruitmentPreboardingQueueItem>>{Success=false,StatusCode=400,ErrorCode=RecruitmentErrorCodes.InvalidRequest,Message="Company context is required."};
  if(preboardingCases is null) return new Result<List<RecruitmentPreboardingQueueItem>>{Success=true,StatusCode=200,MethodResult=[],Data=[],TotalRecords=0,Message="Preboarding queue is not configured."};
  var rows=(await preboardingCases.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&x.Status!=RecruitmentPreboardingStatus.Completed&&x.Status!=RecruitmentPreboardingStatus.Cancelled)).ToList();
  var applicantIds = rows.Select(x => x.ApplicantId).Distinct().ToHashSet();
  var appList = (await applicants.GetAll(x => x.CompanyId == company && !x.IsDeleted && applicantIds.Contains(x.ApplicantId))).ToList();
  var appMap = appList.ToDictionary(x => x.ApplicantId, x => x);
  var vacIds = appList.Select(x => x.VacancyId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToHashSet();
  var vacMap = vacancies is not null && vacIds.Count > 0 ? (await vacancies.GetAll(x => x.CompanyId == company && !x.IsDeleted && vacIds.Contains(x.JobId))).ToDictionary(x => x.JobId, x => x.Title) : new Dictionary<string, string>();
  var items=rows.Select(x=>{ appMap.TryGetValue(x.ApplicantId, out var app); var vId = app?.VacancyId ?? string.Empty; vacMap.TryGetValue(vId, out var vTitle); return new RecruitmentPreboardingQueueItem{CaseId=x.CaseId,ApplicantId=x.ApplicantId,ApplicantName=app is null ? null : $"{app.FirstName} {app.LastName}".Trim(),Email=app?.Email,VacancyId=vId,VacancyName=vTitle,OfferId=x.OfferId,Status=x.Status,OwnerUserId=x.OwnerUserId,PendingTaskCount=x.Tasks.Count(t=>!RecruitmentPreboardingTaskRules.IsResolved(t)),ReadyForHire=x.Tasks.Count>0&&x.Tasks.All(RecruitmentPreboardingTaskRules.IsResolved),FirstBlockerCode=x.Tasks.FirstOrDefault(t=>!RecruitmentPreboardingTaskRules.IsResolved(t))?.Code,Tasks=x.Tasks.Select(t=>new RecruitmentPreboardingTaskQueueItem{Code=t.Code,Title=t.Title,Status=t.Status,SkipReason=t.SkipReason,AssigneeUserId=t.AssigneeUserId,CompletedAtUtc=t.CompletedAtUtc}).ToList()}; }).OrderByDescending(x=>x.PendingTaskCount).ToList();
  return new Result<List<RecruitmentPreboardingQueueItem>>{Success=true,StatusCode=200,MethodResult=items,Data=items,TotalRecords=items.Count,Message="Preboarding queue loaded."};
 }
 public async Task<Result<string>> UpdatePreboarding(RecruitmentPreboardingUpdateCommand command, string actor)
 {
  var company=CurrentContext.CompanyId(context); var key=command.IdempotencyKey?.Trim(); if(string.IsNullOrWhiteSpace(company)||string.IsNullOrWhiteSpace(command.CaseId)||string.IsNullOrWhiteSpace(key)||preboardingCases is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company, case, idempotency key, and preboarding configuration are required.");
  var item=await preboardingCases.FirstOrDefault(x=>x.CompanyId==company&&x.CaseId==command.CaseId.Trim()&&!x.IsDeleted); if(item is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Preboarding case was not found.",404); if(item.LastCommandIdempotencyKey==key) return Ok(item.CaseId,"Preboarding command already applied.");
  var version=item.Version<=0?1:item.Version; if(command.ExpectedVersion.HasValue&&command.ExpectedVersion.Value!=version) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Preboarding case was changed by another user.",409);
  var requested=(command.Tasks??[]).Where(x=>!string.IsNullOrWhiteSpace(x.Code)).ToDictionary(x=>x.Code.Trim(),StringComparer.OrdinalIgnoreCase); if(requested.Count==0) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"At least one task update is required.");
  foreach(var task in item.Tasks) if(requested.TryGetValue(task.Code,out var update)){ if(update.Status is not ("Pending" or "InProgress" or "Completed" or "Skipped")) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Invalid preboarding task status."); if(update.Status=="Skipped"&&string.IsNullOrWhiteSpace(update.SkipReason)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A reason is required when a preboarding task is skipped."); task.Status=update.Status; task.SkipReason=update.Status=="Skipped"?update.SkipReason!.Trim():null; task.AssigneeUserId=update.AssigneeUserId?.Trim()??task.AssigneeUserId; task.CompletedAtUtc=task.Status=="Completed"?DateTime.UtcNow:null; }
  var status=command.Status.Trim(); if(status=="ReadyForHire"&&item.Tasks.Any(x=>!RecruitmentPreboardingTaskRules.IsResolved(x))) return Fail<string>(RecruitmentErrorCodes.InvalidTransition,"Mandatory preboarding tasks are incomplete.",409); if(status is not ("PendingCandidateDetails" or "InProgress" or "ReadyForHire" or "Completed")) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Invalid preboarding status.");
  item.Status=status; item.LastCommandIdempotencyKey=key; item.Version=version+1; item.UpdatedBy=actor; item.UpdatedDate=DateTime.UtcNow; var updated=await preboardingCases.Update(Builders<RecruitmentPreboardingCase>.Filter.Where(x=>x.CompanyId==company&&x.CaseId==item.CaseId&&(x.Version==version||x.Version==0)),item); return updated.Success?Ok(item.CaseId,"Preboarding updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Preboarding case was changed by another user.",409);
 }

 public async Task<Result<RecruitmentFeedbackSlaReport>> FeedbackSla(string? vacancyId)
 {
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company)) return Fail<RecruitmentFeedbackSlaReport>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var featureGate=await RecruitmentFeatureGate.Require<RecruitmentFeedbackSlaReport>(configs,company,c=>c.SlaTracking&&c.EnableSla,"Feedback SLA tracking"); if(featureGate is not null) return featureGate;
  var interviewRows=(await interviews.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId)&&(x.Status==RecruitmentInterviewStatus.Scheduled||x.Status==RecruitmentInterviewStatus.Confirmed))).ToList();
  var items=new List<RecruitmentFeedbackSlaItem>(); var now=DateTime.UtcNow;
  foreach(var interview in interviewRows) { var cards=(await scorecards.GetAll(x=>x.CompanyId==company&&x.InterviewId==interview.InterviewId&&!x.IsDeleted&&(x.Status==RecruitmentScorecardStatus.Submitted||x.Status==RecruitmentScorecardStatus.Locked))).ToList(); var due=cards.Where(x=>x.FeedbackDueAtUtc.HasValue).Select(x=>x.FeedbackDueAtUtc!.Value).DefaultIfEmpty(interview.EndsAtUtc.AddHours(24)).Min(); items.Add(new RecruitmentFeedbackSlaItem { InterviewId=interview.InterviewId, ApplicantId=interview.ApplicantId, VacancyId=interview.VacancyId, RoundNumber=interview.RoundNumber, DueAtUtc=due, AssignedInterviewers=interview.PanelistUserIds.Length, SubmittedScorecards=cards.Select(x=>x.ReviewerUserId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Overdue=due<now && cards.Count < Math.Max(1, interview.QuorumRequired) }); }
  var report=new RecruitmentFeedbackSlaReport { TotalOpenRounds=items.Count, OverdueRounds=items.Count(x=>x.Overdue), Items=items.OrderByDescending(x=>x.Overdue).ThenBy(x=>x.DueAtUtc).ToList() }; return new Result<RecruitmentFeedbackSlaReport> { Success=true, StatusCode=200, MethodResult=report, Data=report, TotalRecords=report.TotalOpenRounds, Message="Feedback SLA report loaded." };
 }

 public async Task<Result<List<RecruitmentStageSlaPolicyDto>>> GetSlaPolicies()
 {
  var company=CurrentContext.CompanyId(context);
  if(string.IsNullOrWhiteSpace(company)) return Fail<List<RecruitmentStageSlaPolicyDto>>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var gate=await RecruitmentFeatureGate.Require<List<RecruitmentStageSlaPolicyDto>>(configs,company,c=>c.SlaTracking&&c.EnableSla,"Stage SLA tracking");
  if(gate is not null) return gate;
  var rows=(await slaPolicies.GetAll(x=>x.CompanyId==company&&!x.IsDeleted)).ToList();
  var items=rows.OrderBy(x=>x.Stage,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Location,StringComparer.OrdinalIgnoreCase)
   .Select(x=>new RecruitmentStageSlaPolicyDto{PolicyId=x.PolicyId,Stage=x.Stage,TargetHours=x.TargetHours,Enabled=x.Enabled,Location=x.Location}).ToList();
  return new Result<List<RecruitmentStageSlaPolicyDto>>{Success=true,StatusCode=200,MethodResult=items,Data=items,TotalRecords=items.Count,Message="Stage SLA policies loaded."};
 }

 public async Task<Result<RecruitmentStageSlaReport>> StageSla(string? vacancyId)
 {
  var company=CurrentContext.CompanyId(context);
  if(string.IsNullOrWhiteSpace(company)) return Fail<RecruitmentStageSlaReport>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var gate=await RecruitmentFeatureGate.Require<RecruitmentStageSlaReport>(configs,company,c=>c.SlaTracking&&c.EnableSla,"Stage SLA tracking");
  if(gate is not null) return gate;
  if(applications is null||vacancies is null) return Fail<RecruitmentStageSlaReport>(RecruitmentErrorCodes.WriteFailed,"Stage SLA reporting is not configured.",503);

  var policies=(await slaPolicies.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&x.Enabled&&x.TargetHours>0)).ToList();
  Expression<Func<RecruitmentJobApplication,bool>> applicationFilter=x=>x.CompanyId==company&&!x.IsDeleted&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId);
  var policyMap=policies.GroupBy(x=>x.Stage.Trim(),StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.ToList(),StringComparer.OrdinalIgnoreCase);
  var now=DateTime.UtcNow;
  var items=new List<RecruitmentStageSlaItem>();
  var totalTracked=0;
  var overdueCount=0;
  for(var pageNo=1;;pageNo++)
  {
   var applicationBatch=(await applications.GetAggregateDataAsync<RecruitmentJobApplication>(applicationFilter,StageSlaApplicationProjection,isAscending:true,orderedKey:nameof(RecruitmentJobApplication.ApplicationId),pageNo:pageNo,pageSize:AnalyticsPageSize)).ToList();
   if(applicationBatch.Count==0) break;
   var vacancyIds=applicationBatch.Select(x=>x.VacancyId).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
   var vacancyRows=new List<JobVacancy>();
   foreach(var batch in vacancyIds.Chunk(500)) vacancyRows.AddRange((await vacancies.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&batch.Contains(x.JobId))).ToList());
   var vacancyMap=vacancyRows.GroupBy(x=>x.JobId,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.OrdinalIgnoreCase);
   foreach(var application in applicationBatch)
   {
    var stage=application.CurrentStage?.Trim();
    if(string.IsNullOrWhiteSpace(stage)||stage.Equals("Rejected",StringComparison.OrdinalIgnoreCase)||stage.Equals("Selected",StringComparison.OrdinalIgnoreCase)||stage.Equals("Hired",StringComparison.OrdinalIgnoreCase)||stage.Equals("Withdrawn",StringComparison.OrdinalIgnoreCase)||stage.Equals("Cancelled",StringComparison.OrdinalIgnoreCase)||stage.Equals("Canceled",StringComparison.OrdinalIgnoreCase)||stage.Equals("Archived",StringComparison.OrdinalIgnoreCase)||!policyMap.TryGetValue(stage,out var stagePolicies)) continue;
    vacancyMap.TryGetValue(application.VacancyId,out var vacancy);
    var location=vacancy?.Location?.Trim();
    var policy=stagePolicies.Where(x=>!string.IsNullOrWhiteSpace(location)&&string.Equals(x.Location?.Trim(),location,StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.UpdatedDate??x.CreatedDate).FirstOrDefault()
     ?? stagePolicies.FirstOrDefault(x=>string.IsNullOrWhiteSpace(x.Location));
    if(policy is null) continue;
    var latestEntry=application.StageHistory?.Where(x=>string.Equals(x.ToStage?.Trim(),stage,StringComparison.OrdinalIgnoreCase)&&x.ChangedAtUtc>DateTime.UnixEpoch&&x.ChangedAtUtc<=now.AddMinutes(5)).OrderByDescending(x=>x.ChangedAtUtc).FirstOrDefault();
    var enteredAt=latestEntry?.ChangedAtUtc;
    if(!enteredAt.HasValue&&application.StageHistory is {Count:0}&&application.CreatedDate.HasValue&&application.CreatedDate.Value>DateTime.UnixEpoch&&application.CreatedDate.Value<=now.AddMinutes(5)) enteredAt=application.CreatedDate.Value;
    if(!enteredAt.HasValue) continue;
    var due=enteredAt.Value.AddHours(policy.TargetHours);
    var isOverdue=due<now;
    totalTracked++;
    if(isOverdue) overdueCount++;
    items.Add(new RecruitmentStageSlaItem{ApplicantId=application.ApplicantId,VacancyId=application.VacancyId,VacancyTitle=vacancy?.Title,Stage=stage,Location=location,EnteredStageAtUtc=enteredAt.Value,DueAtUtc=due,HoursInStage=Math.Max(0,(now-enteredAt.Value).TotalHours),TargetHours=policy.TargetHours,Overdue=isOverdue});
   }
   items=items.OrderByDescending(x=>x.Overdue).ThenBy(x=>x.DueAtUtc).Take(1000).ToList();
   if(applicationBatch.Count<AnalyticsPageSize) break;
  }
  var report=new RecruitmentStageSlaReport{TotalTrackedApplications=totalTracked,OverdueApplications=overdueCount,Items=items};
  return new Result<RecruitmentStageSlaReport>{Success=true,StatusCode=200,MethodResult=report,Data=report,TotalRecords=report.TotalTrackedApplications,Message="Stage aging SLA report loaded."};
 }
 public async Task<Result<int>> QueueFeedbackReminders(string? vacancyId, string actorUserId)
 {
  var company=CurrentContext.CompanyId(context);
  if(string.IsNullOrWhiteSpace(company)) return new Result<int> { Success=false, StatusCode=StatusCodes.Status400BadRequest, ErrorCode=RecruitmentErrorCodes.InvalidRequest, Message="Company context is required." };
  var automationGate=await RecruitmentFeatureGate.Require<int>(configs,company,c=>c.Automation&&c.EnableAutomation,"Recruitment automation");
  if(automationGate is not null) return automationGate;
  var report=await FeedbackSla(vacancyId); if(!report.Success || report.MethodResult is null) return new Result<int> { Success=false, StatusCode=report.StatusCode, ErrorCode=report.ErrorCode, Message=report.Message };
  var queued=0; var failed=0; var skipped=0;
  var overdueItems=report.MethodResult.Items.Where(x=>x.Overdue).ToList();
  if(overdueItems.Count==0) return new Result<int> { Success=true, StatusCode=200, MethodResult=0, Data=0, Message="No overdue interviewer feedback reminders need delivery." };
  if(employees is null || externalActions is null)
   return new Result<int> { Success=false, StatusCode=StatusCodes.Status503ServiceUnavailable, ErrorCode=RecruitmentErrorCodes.WriteFailed, Message="Interviewer reminder delivery is not configured. No reminders were queued." };

  var overdueInterviewIds=overdueItems.Select(x=>x.InterviewId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  var interviewRows=(await interviews.GetAll(x=>x.CompanyId==company&&overdueInterviewIds.Contains(x.InterviewId)&&!x.IsDeleted&&(x.Status==RecruitmentInterviewStatus.Scheduled||x.Status==RecruitmentInterviewStatus.Confirmed))).ToList();
  var interviewMap=interviewRows.ToDictionary(x=>x.InterviewId,StringComparer.OrdinalIgnoreCase);
  var allPanelistIds=interviewRows.SelectMany(x=>x.PanelistUserIds??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  var employeeRows=new List<EmpUser>();
  foreach(var userIdBatch in allPanelistIds.Chunk(500))
   employeeRows.AddRange((await employees.GetAll(x=>x.CompanyId==company&&x.Status&&userIdBatch.Contains(x.UserId))).ToList());
  var employeeMap=employeeRows.GroupBy(x=>x.UserId,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.OrdinalIgnoreCase);
  var submittedReviewerMap=new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);
  foreach(var interviewIdBatch in overdueInterviewIds.Chunk(500))
  {
   var submittedCards=(await scorecards.GetAll(x=>x.CompanyId==company&&x.InterviewId!=null&&interviewIdBatch.Contains(x.InterviewId)&&!x.IsDeleted&&(x.Status==RecruitmentScorecardStatus.Submitted||x.Status==RecruitmentScorecardStatus.Locked))).ToList();
   foreach(var group in submittedCards.GroupBy(x=>x.InterviewId!,StringComparer.OrdinalIgnoreCase))
    submittedReviewerMap[group.Key]=group.Select(x=>x.ReviewerUserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
  }

  foreach(var item in overdueItems)
  {
   if(!interviewMap.TryGetValue(item.InterviewId,out var interview)||interview.VacancyId!=item.VacancyId) { skipped++; continue; }
   var interviewPanelistIds=(interview.PanelistUserIds??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
   if(interviewPanelistIds.Length==0) { skipped++; continue; }
   submittedReviewerMap.TryGetValue(item.InterviewId,out var submittedReviewerIds);
   var pendingPanelists=interviewPanelistIds.Where(userId=>employeeMap.TryGetValue(userId,out var panelist)&&!string.IsNullOrWhiteSpace(panelist.Email)&&(submittedReviewerIds is null||!submittedReviewerIds.Contains(userId)))
    .Select(userId=>employeeMap[userId]).ToList();
   foreach(var panelist in pendingPanelists)
   {
    var due=item.DueAtUtc.ToUniversalTime();
    var dedupeKey=$"feedback-reminder:{item.InterviewId}:{item.RoundNumber}:{due:yyyyMMddHHmmss}:{panelist.UserId}";
    var title=System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(interview.Title)?"Interview":interview.Title.Trim());
    var body=$"<p>Please complete your interview scorecard in Codeji CMS.</p><ul><li>Interview: {title}</li><li>Round: {item.RoundNumber}</li><li>Feedback due: {due:yyyy-MM-dd HH:mm} UTC</li></ul><p>Sign in to review the candidate and submit your feedback.</p>";
    var emailResult=await externalActions.QueueEmail(new RecruitmentEmailQueueCommand
    {
     RelatedApplicantId=item.ApplicantId,
     DedupeKey=dedupeKey,
     EventType="InterviewerFeedbackReminder",
     RecipientEmail=panelist.Email.Trim(),
     RecipientUserId=panelist.UserId,
     Subject=$"Interview feedback due: {interview.Title} (Round {item.RoundNumber})",
     HtmlBody=body
    },actorUserId);
    if(emailResult.Success) queued++; else failed++;
   }
   if(pendingPanelists.Count==0) skipped++;
  }
  if(failed>0) return new Result<int> { Success=false, StatusCode=StatusCodes.Status503ServiceUnavailable, ErrorCode=RecruitmentErrorCodes.WriteFailed, MethodResult=queued, Data=queued, Message=$"Queued {queued} interviewer reminder(s); {failed} could not be queued. Retry to complete delivery." };
  return new Result<int> { Success=true, StatusCode=200, MethodResult=queued, Data=queued, Message=$"Queued {queued} interviewer feedback reminder(s) for email delivery; {skipped} rounds had no active, deliverable panelists." };
 }
 public async Task<Result<string>> UpsertSla(RecruitmentStageSlaPolicyCommand command,string actor)
 {
  var company=CurrentContext.CompanyId(context);
  var stage=command.Stage?.Trim();
  var location=string.IsNullOrWhiteSpace(command.Location)?null:command.Location.Trim();
  var key=command.IdempotencyKey?.Trim();
  if(string.IsNullOrWhiteSpace(company)||string.IsNullOrWhiteSpace(stage)||stage.Length>100||command.TargetHours is <1 or >8760||location?.Length>120||string.IsNullOrWhiteSpace(key)||key.Length>128)
   return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Stage, idempotency key, and a target between 1 hour and 1 year are required. Stage and location must be at most 100 and 120 characters.");
  var featureGate=await RecruitmentFeatureGate.Require<string>(configs,company,x=>x.SlaTracking&&x.EnableSla,"Stage SLA tracking");
  if(featureGate is not null)return featureGate;
  if(Enum.TryParse<ActivityType>(stage,true,out var builtInStage)) stage=builtInStage.ToString();
  var existing=(await slaPolicies.GetAll(x=>x.CompanyId==company&&!x.IsDeleted)).FirstOrDefault(x=>string.Equals(x.Stage?.Trim(),stage,StringComparison.OrdinalIgnoreCase)&&string.Equals(string.IsNullOrWhiteSpace(x.Location)?null:x.Location.Trim(),location,StringComparison.OrdinalIgnoreCase));
  if(existing is not null&&string.Equals(existing.LastCommandIdempotencyKey,key,StringComparison.Ordinal)) return Ok(existing.PolicyId,"Stage SLA command already applied.");
  if(existing is null)
  {
   var policy=new RecruitmentStageSlaPolicy{CompanyId=company,Stage=stage,Location=location,TargetHours=command.TargetHours,Enabled=command.Enabled,LastCommandIdempotencyKey=key,CreatedBy=actor,UpdatedBy=actor};
   var added=await slaPolicies.AddOne(policy);
   return added.Success?Ok(policy.PolicyId,"Stage SLA policy saved."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Stage SLA policy could not be saved.",409);
  }
  existing.TargetHours=command.TargetHours;existing.Enabled=command.Enabled;existing.LastCommandIdempotencyKey=key;existing.UpdatedBy=actor;existing.UpdatedDate=DateTime.UtcNow;
  var updated=await slaPolicies.Update(Builders<RecruitmentStageSlaPolicy>.Filter.Where(x=>x.CompanyId==company&&x.PolicyId==existing.PolicyId),existing);
  return updated.Success?Ok(existing.PolicyId,"Stage SLA policy updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Stage SLA policy could not be updated.",409);
 }
 public async Task<Result<RecruitmentReadinessDto>> GetReadiness()
 {
  var company=CurrentContext.CompanyId(context);
  if(string.IsNullOrWhiteSpace(company)) return Fail<RecruitmentReadinessDto>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var config=await configs.FirstOrDefault(x=>x.CompanyId==company&&!x.IsDeleted);
  var readiness=BuildReadiness(config);
  return new Result<RecruitmentReadinessDto>{Success=true,StatusCode=200,MethodResult=readiness,Data=readiness,Message=readiness.ReadyToPublishJobs?"Recruitment is ready for job publishing.":"Recruitment setup is incomplete."};
 }
 internal static RecruitmentReadinessDto BuildReadiness(RecruitmentEnterpriseConfig? config)
 {
  var c=config??new RecruitmentEnterpriseConfig{RecruitmentMode="Simple",DefaultTimezone="UTC",Locations=[],ScreeningCriteria=[],PreboardingChecklist=[],EnableSla=false,EnableAutomation=false};
  var required=new List<RecruitmentReadinessItemDto>
  {
   new(){Code="timezone",Label="Choose a company recruitment timezone",Complete=config is not null&&!string.IsNullOrWhiteSpace(c.DefaultTimezone)},
   new(){Code="location",Label="Add at least one hiring location",Complete=c.Locations?.Count>0},
   new(){Code="screening",Label="Configure screening criteria",Complete=c.ScreeningCriteria?.Count>0},
   new(){Code="preboarding",Label="Configure the preboarding checklist",Complete=c.PreboardingChecklist?.Count>0}
  };
  var recommended=new List<RecruitmentReadinessItemDto>
  {
   new(){Code="sla",Label="Review SLA settings before enabling reminders",Complete=!c.EnableSla},
   new(){Code="automation",Label="Review automation before enabling it",Complete=!c.EnableAutomation},
   new(){Code="approvals",Label="Configure offer approval for the company size",Complete=c.RecruitmentMode=="Simple"||c.OfferApproval},
   new(){Code="scorecards",Label="Configure interview scorecards",Complete=c.RecruitmentMode=="Simple"||c.Scorecards},
   new(){Code="candidate-privacy",Label="Set a candidate privacy notice before accepting public applications",Complete=PublicCareerApplicationRules.IsPrivacyNoticeConfigured(c.CandidatePrivacyNoticeText,c.CandidatePrivacyNoticeVersion)}
  };
  var all=required.Concat(recommended).ToList();
  return new RecruitmentReadinessDto{Mode=c.RecruitmentMode??"Simple",Timezone=c.DefaultTimezone??"UTC",ReadyToPublishJobs=required.All(x=>x.Complete),CompletionPercent=all.Count==0?0:(int)Math.Round(all.Count(x=>x.Complete)*100d/all.Count),Required=required,Recommended=recommended};
 }
 public async Task<Result<string>> UpsertConfig(RecruitmentEnterpriseConfigCommand command,string actor){var c=CurrentContext.CompanyId(context);if(string.IsNullOrWhiteSpace(c))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");var commandKey=command.IdempotencyKey?.Trim();if(string.IsNullOrWhiteSpace(commandKey)||!System.Text.RegularExpressions.Regex.IsMatch(commandKey,"^[A-Za-z0-9._:-]{12,100}$"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A valid recruitment configuration idempotency key is required.");command.IdempotencyKey=commandKey;var commandHash=RecruitmentEnterpriseConfigRules.HashCommand(command);var e=await configs.FirstOrDefault(x=>x.CompanyId==c&&!x.IsDeleted);if(e is not null){var priorReceipt=RecruitmentEnterpriseConfigRules.FindCommandReceipt(e,commandKey);if(priorReceipt is not null){if(!string.Equals(priorReceipt.PayloadHash,commandHash,StringComparison.Ordinal))return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"This idempotency key was already used for a different recruitment configuration command. Submit it with a new key.",409);return Ok(e.ConfigId,"Recruitment configuration command already applied.");}if(string.Equals(e.LastCommandIdempotencyKey,commandKey,StringComparison.Ordinal)){if(!string.Equals(e.LastCommandPayloadHash,commandHash,StringComparison.Ordinal))return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"This idempotency key was already used for a different recruitment configuration command. Submit it with a new key.",409);return Ok(e.ConfigId,"Recruitment configuration command already applied.");}}var mode=(command.RecruitmentMode??e?.RecruitmentMode??"Simple").Trim();if(mode is not ("Simple" or "Standard" or "Enterprise"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Recruitment mode must be Simple, Standard, or Enterprise.");var schemeName=(command.CompensationSchemeName??e?.CompensationSchemeName??"Standard").Trim();var currency=(command.CompensationCurrency??e?.CompensationCurrency??"INR").Trim().ToUpperInvariant();var components=(command.CompensationComponents??e?.CompensationComponents??[]).Where(x=>!string.IsNullOrWhiteSpace(x.Key)&&x.Value>=0).ToDictionary(x=>x.Key.Trim(),x=>x.Value,StringComparer.OrdinalIgnoreCase);if(schemeName.Length>80||currency.Length is < 3 or > 3||components.Count>30)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Compensation scheme, currency, or component count is invalid.");var locations=(command.Locations??e?.Locations??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();var enableSla=RecruitmentEnterpriseConfigRules.ResolveSlaEnabled(command,e);var enableAutomation=RecruitmentEnterpriseConfigRules.ResolveAutomationEnabled(command,e);var retentionDays=command.DefaultCandidateRetentionDays??e?.DefaultCandidateRetentionDays??730;var offerSubject=(command.OfferLetterSubjectTemplate??e?.OfferLetterSubjectTemplate??"Formal Offer of Employment: [JobTitle] - [CandidateName]").Trim();var offerBody=(command.OfferLetterBodyTemplate??e?.OfferLetterBodyTemplate??"<p>Dear [CandidateName],</p><p>We are pleased to extend this formal offer of employment for the position of <strong>[JobTitle]</strong>.</p><p><strong>Compensation (Total CTC):</strong> [Currency] [TotalCtc]</p><p>Please review the terms and submit your response using the secure candidate link in this email.</p><p>Sincerely,<br/><strong>Human Resources & Talent Acquisition</strong></p>").Trim();if(offerSubject.Length is < 3 or > 160||offerBody.Length is < 3 or > 20000)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Offer letter subject or body length is invalid.");if(retentionDays is < 30 or > 3650)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Candidate retention must be between 30 and 3650 days.");var sourceOptions=(command.CandidateSourceOptions??e?.CandidateSourceOptions??PublicCareerApplicationRules.CandidateSourceOptions.ToList()).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();if(sourceOptions.Count is < 1 or > 20||sourceOptions.Any(x=>x.Length>60||x.Any(char.IsControl)))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Candidate source options must contain 1 to 20 labels, each no longer than 60 characters.");var privacyNotice=(command.CandidatePrivacyNoticeText??e?.CandidatePrivacyNoticeText??string.Empty).Trim();var privacyVersion=(command.CandidatePrivacyNoticeVersion??e?.CandidatePrivacyNoticeVersion??string.Empty).Trim();if(privacyNotice.Length>8000||privacyVersion.Length>40||(!string.IsNullOrEmpty(privacyNotice)&&string.IsNullOrEmpty(privacyVersion))||(!string.IsNullOrEmpty(privacyVersion)&&string.IsNullOrEmpty(privacyNotice))||(!string.IsNullOrEmpty(privacyVersion)&&!System.Text.RegularExpressions.Regex.IsMatch(privacyVersion,"^[A-Za-z0-9._-]{1,40}$")))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Candidate privacy notice and version must both be supplied; notice text is limited to 8000 characters and version to 40 letters, numbers, dots, underscores, or hyphens.");if(e is not null&&!command.ResetToDefaults&&!string.Equals(e.CandidatePrivacyNoticeText?.Trim(),privacyNotice,StringComparison.Ordinal)&&string.Equals(e.CandidatePrivacyNoticeVersion?.Trim(),privacyVersion,StringComparison.Ordinal))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Increment the privacy notice version whenever its text changes.");if(command.ResetToDefaults){privacyNotice=string.Empty;privacyVersion=string.Empty;}var timezone=(command.DefaultTimezone??e?.DefaultTimezone??"UTC").Trim(); if(string.IsNullOrWhiteSpace(timezone)||!TimeZoneInfo.TryFindSystemTimeZoneById(timezone,out _)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A valid IANA or system timezone is required.");if(!command.ResetToDefaults && enableSla && locations.Count == 0) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Add at least one hiring location before enabling SLA tracking.");if(!command.ResetToDefaults && enableAutomation && !(command.PreboardingChecklist ?? e?.PreboardingChecklist ?? []).Any(x => !string.IsNullOrWhiteSpace(x))) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Configure at least one preboarding checklist item before enabling automation.");var expectedVersion=e?.DefaultVersion??0;if(e is null)e=new RecruitmentEnterpriseConfig{CompanyId=c,CreatedBy=actor,DefaultVersion=1}; else e.DefaultVersion = Math.Max(1, e.DefaultVersion) + 1;if(command.ResetToDefaults){locations=[];enableSla=false;enableAutomation=false;retentionDays=730;timezone="UTC";mode="Simple";}e.Locations=locations;e.RecruitmentMode=mode;e.EnableSla=enableSla;e.EnableAutomation=enableAutomation;e.ApplicationReview=command.ResetToDefaults?true:(command.ApplicationReview ?? e!.ApplicationReview);e.HiringManagerReview=command.ResetToDefaults?false:(command.HiringManagerReview ?? e!.HiringManagerReview);e.InterviewRounds=command.ResetToDefaults?false:(command.InterviewRounds ?? e!.InterviewRounds);e.ParallelPanels=command.ResetToDefaults?false:(command.ParallelPanels ?? e!.ParallelPanels);e.Scorecards=command.ResetToDefaults?false:(command.Scorecards ?? e!.Scorecards);e.OfferApproval=command.ResetToDefaults?false:(command.OfferApproval ?? e!.OfferApproval);e.OfferNegotiation=command.ResetToDefaults?false:(command.OfferNegotiation ?? e!.OfferNegotiation);e.CandidateEmailActions=command.ResetToDefaults?true:(command.CandidateEmailActions ?? e!.CandidateEmailActions);e.Requisitions=command.ResetToDefaults?false:(command.Requisitions ?? e!.Requisitions);e.SlaTracking=enableSla;e.Automation=enableAutomation;e.InternalMobility=command.ResetToDefaults?false:(command.InternalMobility ?? e!.InternalMobility);e.RehireLinks=command.ResetToDefaults?false:(command.RehireLinks ?? e!.RehireLinks);e.AutoAdvanceOnScreening=command.ResetToDefaults?true:(command.AutoAdvanceOnScreening ?? e!.AutoAdvanceOnScreening);e.RequireInterviewBeforeDecision=command.ResetToDefaults?false:(command.RequireInterviewBeforeDecision ?? e!.RequireInterviewBeforeDecision);e.RequirePreboardingBeforeHandoff=command.ResetToDefaults?false:(command.RequirePreboardingBeforeHandoff ?? e!.RequirePreboardingBeforeHandoff);e.AllowMultiVacancyApplications=command.ResetToDefaults?true:(command.AllowMultiVacancyApplications ?? e!.AllowMultiVacancyApplications);e.AllowJobWorkflowCustomization=command.ResetToDefaults?false:(command.AllowJobWorkflowCustomization ?? e!.AllowJobWorkflowCustomization);e.ScreeningCriteria=(command.ResetToDefaults?new List<string>{"Relevant experience","Required skills","Communication"}:(command.ScreeningCriteria??e.ScreeningCriteria??["Relevant experience","Required skills","Communication"])).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();e.ScreeningPassScore=command.ResetToDefaults?60:Math.Clamp(command.ScreeningPassScore??e.ScreeningPassScore,0,100);e.PreboardingChecklist=(command.ResetToDefaults?new List<string>{"candidate-details","documents","verification"}:(command.PreboardingChecklist??e.PreboardingChecklist??["candidate-details","documents","verification"])).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();e.CompensationSchemeName=command.ResetToDefaults?"Standard":schemeName;e.CompensationCurrency=command.ResetToDefaults?"INR":currency;e.CompensationComponents=command.ResetToDefaults?new Dictionary<string,decimal>():components;e.DefaultCandidateRetentionDays=retentionDays;e.CandidatePrivacyNoticeText=string.IsNullOrWhiteSpace(privacyNotice)?null:privacyNotice;e.CandidatePrivacyNoticeVersion=string.IsNullOrWhiteSpace(privacyVersion)?null:privacyVersion;e.CandidateSourceOptions=command.ResetToDefaults?PublicCareerApplicationRules.CandidateSourceOptions.ToList():sourceOptions;e.OfferLetterSubjectTemplate=command.ResetToDefaults?"Formal Offer of Employment: [JobTitle] - [CandidateName]":offerSubject;e.OfferLetterBodyTemplate=command.ResetToDefaults?"<p>Dear [CandidateName],</p><p>We are pleased to extend this formal offer of employment for the position of <strong>[JobTitle]</strong>.</p><p><strong>Compensation (Total CTC):</strong> [Currency] [TotalCtc]</p><p>Please review the terms and submit your response using the secure candidate link in this email.</p><p>Sincerely,<br/><strong>Human Resources & Talent Acquisition</strong></p>":offerBody;e.DefaultTimezone=timezone;if(command.ResetToDefaults)RecruitmentEnterpriseConfigRules.ApplyDefaults(e);e.OverriddenFields=command.ResetToDefaults?new List<string>():RecruitmentEnterpriseConfigRules.GetOverriddenFields(e);e.ConfigSource=e.OverriddenFields.Count==0?"SystemDefault":"CompanyOverride";e.LastCommandIdempotencyKey=commandKey;e.LastCommandPayloadHash=commandHash;RecruitmentEnterpriseConfigRules.RecordCommandReceipt(e,commandKey,commandHash,DateTime.UtcNow);e.UpdatedBy=actor;e.UpdatedDate=DateTime.UtcNow;if(string.IsNullOrWhiteSpace(e.ConfigId)){var add=await configs.AddOne(e);return add.Success?Ok(e.ConfigId,"Recruitment configuration saved."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Recruitment configuration could not be saved.",409);}var u=await configs.Update(RecruitmentEnterpriseConfigRules.BuildVersionFilter(c,e.ConfigId,expectedVersion),e);return u.Success?Ok(e.ConfigId,command.ResetToDefaults?"Recruitment configuration reset to defaults.":"Recruitment configuration updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Recruitment configuration could not be updated.",409);} public async Task<Result<RecruitmentEnterpriseConfig>> GetConfig(){var c=CurrentContext.CompanyId(context);if(string.IsNullOrWhiteSpace(c))return Fail<RecruitmentEnterpriseConfig>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");var e=await configs.FirstOrDefault(x=>x.CompanyId==c&&!x.IsDeleted);if(e is null){e=RecruitmentEnterpriseConfigRules.CreateDefaults();e.CompanyId=c;e.ConfigSource="SystemDefault";e.DefaultVersion=1;e.OverriddenFields=[];}RecruitmentEnterpriseConfigRules.NormalizeHistoricalSwitches(e);return new Result<RecruitmentEnterpriseConfig>{Success=true,StatusCode=200,MethodResult=e,Data=e,Message=e.ConfigSource=="SystemDefault"?"Recruitment defaults loaded.":"Recruitment configuration loaded."};} public async Task<Result<string>> Link(RecruitmentRelationshipLinkCommand command,string actor){var c=CurrentContext.CompanyId(context);var key=command.IdempotencyKey?.Trim();if(string.IsNullOrWhiteSpace(c)||string.IsNullOrWhiteSpace(key)||key.Length<12)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company and an idempotency key of at least 12 characters are required.");if(command.RelationshipType is not ("InternalMobility" or "Rehire"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Relationship type must be InternalMobility or Rehire.");var gate=await RecruitmentFeatureGate.Require<string>(configs,c,x=>command.RelationshipType=="InternalMobility"?x.InternalMobility:x.RehireLinks,command.RelationshipType=="InternalMobility"?"Internal mobility":"Rehire links");if(gate is not null)return gate;var existing=await links.FirstOrDefault(x=>x.CompanyId==c&&x.LastCommandIdempotencyKey==key&&!x.IsDeleted);if(existing is not null)return Ok(existing.LinkId,"Relationship link already exists.");var a=await applicants.FirstOrDefault(x=>x.CompanyId==c&&x.ApplicantId==command.ApplicantId&&!x.IsDeleted);if(a is null)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Applicant was not found in the current company.");if(string.IsNullOrWhiteSpace(command.RelatedPersonId)||command.RelatedPersonId.Trim()==a.ApplicantId)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A valid related person is required and cannot be the applicant.");if(command.RelationshipType is not ("InternalMobility" or "Rehire"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Relationship type must be InternalMobility or Rehire.");var e=new RecruitmentRelationshipLink{CompanyId=c,ApplicantId=a.ApplicantId,RelationshipType=command.RelationshipType,RelatedPersonId=command.RelatedPersonId.Trim(),Notes=command.Notes?.Trim(),LastCommandIdempotencyKey=key,CreatedBy=actor,UpdatedBy=actor};var add=await links.AddOne(e);return add.Success?Ok(e.LinkId,"Recruitment relationship linked."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Relationship could not be linked.",409);}
 public async Task<Result<string>> Enqueue(RecruitmentAutomationEventCommand command,string actor){var c=CurrentContext.CompanyId(context);var key=command.IdempotencyKey?.Trim();if(string.IsNullOrWhiteSpace(c)||string.IsNullOrWhiteSpace(command.EventType)||string.IsNullOrWhiteSpace(key)||key.Length<12)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Event type and an idempotency key of at least 12 characters are required.");var gate=await RecruitmentFeatureGate.Require<string>(configs,c,x=>x.Automation&&x.EnableAutomation,"Recruitment automation");if(gate is not null)return gate;var existing=await outbox.FirstOrDefault(x=>x.CompanyId==c&&x.IdempotencyKey==key&&!x.IsDeleted);if(existing is not null)return Ok(existing.EventId,"Automation event already queued.");if(!string.IsNullOrWhiteSpace(command.ApplicantId)){var applicant=await applicants.FirstOrDefault(x=>x.CompanyId==c&&x.ApplicantId==command.ApplicantId.Trim()&&!x.IsDeleted);if(applicant is null)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Applicant was not found in the current company.");}var e=new RecruitmentAutomationOutboxEvent{CompanyId=c,EventType=command.EventType.Trim(),ApplicantId=command.ApplicantId?.Trim(),Payload=command.Payload??[],IdempotencyKey=key,CreatedBy=actor,UpdatedBy=actor};var add=await outbox.AddOne(e);return add.Success?Ok(e.EventId,"Automation event queued."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Automation event could not be queued.",409);}
 private static Result<T> Fail<T>(string code,string msg,int status=400)=>new(){Success=false,StatusCode=status,ErrorCode=code,Message=msg}; private static Result<string> Ok(string id,string msg)=>new(){Success=true,StatusCode=200,MethodResult=id,Message=msg};
}



