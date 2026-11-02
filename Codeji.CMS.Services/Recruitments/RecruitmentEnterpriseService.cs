using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
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
 IMongoDbRepository<JobVacancy>? vacancies = null) : IRecruitmentEnterpriseService
{
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
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company))return Fail<RecruitmentAnalytics>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var canonical = applications is null ? null : (await applications.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId))).GroupBy(x=>x.ApplicantId).Select(g=>g.OrderByDescending(y=>y.UpdatedDate).First()).ToList();
  var rows=(await applicants.GetAll(x=>x.CompanyId==company&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId))).ToList();
  var activeRows=rows.Where(x=>!x.IsDeleted&&x.MergeStatus!="Merged").ToList();
  var mergedCount=rows.Count(x=>x.MergeStatus=="Merged");
  var canonicalMap = canonical?.ToDictionary(x => x.ApplicantId, x => x) ?? new Dictionary<string, RecruitmentJobApplication>();
  var stageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
  foreach (var row in activeRows)
  {
   var hasCanonical = canonicalMap.TryGetValue(row.ApplicantId, out var canApp);
   var stage = (hasCanonical && !string.IsNullOrWhiteSpace(canApp!.CurrentStage)) ? canApp.CurrentStage : row.ActivityType.ToString();
   if (!hasCanonical && applications is not null)
   {
    var newCan = new RecruitmentJobApplication { CompanyId = company, ApplicantId = row.ApplicantId, VacancyId = row.VacancyId ?? string.Empty, CurrentStage = stage, CreatedBy = row.ApplicantId, CreatedDate = row.CreatedDate ?? DateTime.UtcNow, UpdatedBy = row.ApplicantId, UpdatedDate = row.UpdatedDate ?? DateTime.UtcNow, StageHistory = [new RecruitmentApplicationStageEvent { FromStage = "None", ToStage = stage, ActorUserId = row.ApplicantId, Reason = "Auto-synchronized from applicant profile" }] };
    _ = applications.AddOne(newCan);
    canonicalMap[row.ApplicantId] = newCan;
   }
   stageCounts[stage] = stageCounts.GetValueOrDefault(stage, 0) + 1;
   if (stage is "Preboarding" or "ReadyForHire" or "Hired")
   {
    stageCounts["Selected"] = stageCounts.GetValueOrDefault("Selected", 0) + 1;
   }
  }
  var a = new RecruitmentAnalytics
  {
   TotalApplicants = activeRows.Count,
   ActiveApplicants = activeRows.Count(x => { var st = (canonicalMap.TryGetValue(x.ApplicantId, out var c) && !string.IsNullOrWhiteSpace(c.CurrentStage)) ? c.CurrentStage : x.ActivityType.ToString(); return !string.Equals(st, "Rejected", StringComparison.OrdinalIgnoreCase); }),
   RejectedApplicants = activeRows.Count(x => { var st = (canonicalMap.TryGetValue(x.ApplicantId, out var c) && !string.IsNullOrWhiteSpace(c.CurrentStage)) ? c.CurrentStage : x.ActivityType.ToString(); return string.Equals(st, "Rejected", StringComparison.OrdinalIgnoreCase); }),
   MergedApplicants = mergedCount,
   ByStage = stageCounts
  };



  var acceptedOffers=(await offers.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&x.Status==RecruitmentOfferStatus.Accepted&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId))).ToList(); var applicantCreated=activeRows.Where(x=>x.CreatedDate.HasValue).ToDictionary(x=>x.ApplicantId,x=>x.CreatedDate!.Value); var hireDays=acceptedOffers.Where(x=>x.RespondedAtUtc.HasValue&&applicantCreated.TryGetValue(x.ApplicantId,out _)).Select(x=>(decimal)(x.RespondedAtUtc!.Value-applicantCreated[x.ApplicantId]).TotalDays).Where(x=>x>=0).ToList(); a.TimeToHireSampleSize=hireDays.Count; a.AverageTimeToHireDays=hireDays.Count==0?null:Math.Round(hireDays.Average(),2); var r=new Result<RecruitmentAnalytics>{Success=true,StatusCode=200,Message="Recruitment analytics loaded.",MethodResult=a,Data=a,TotalRecords=a.TotalApplicants}; return r;
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
  var rows=(await preboardingCases.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&x.Status!=RecruitmentPreboardingStatus.Completed)).ToList();
  var applicantIds = rows.Select(x => x.ApplicantId).Distinct().ToHashSet();
  var appList = (await applicants.GetAll(x => x.CompanyId == company && !x.IsDeleted && applicantIds.Contains(x.ApplicantId))).ToList();
  var appMap = appList.ToDictionary(x => x.ApplicantId, x => x);
  var vacIds = appList.Select(x => x.VacancyId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToHashSet();
  var vacMap = vacancies is not null && vacIds.Count > 0 ? (await vacancies.GetAll(x => x.CompanyId == company && !x.IsDeleted && vacIds.Contains(x.JobId))).ToDictionary(x => x.JobId, x => x.Title) : new Dictionary<string, string>();
  var items=rows.Select(x=>{ appMap.TryGetValue(x.ApplicantId, out var app); var vId = app?.VacancyId ?? string.Empty; vacMap.TryGetValue(vId, out var vTitle); return new RecruitmentPreboardingQueueItem{CaseId=x.CaseId,ApplicantId=x.ApplicantId,ApplicantName=app is null ? null : $"{app.FirstName} {app.LastName}".Trim(),Email=app?.Email,VacancyId=vId,VacancyName=vTitle,OfferId=x.OfferId,Status=x.Status,OwnerUserId=x.OwnerUserId,PendingTaskCount=x.Tasks.Count(t=>t.Status is not ("Completed" or "Skipped")),ReadyForHire=x.Tasks.Count>0&&x.Tasks.All(t=>t.Status is "Completed" or "Skipped"),FirstBlockerCode=x.Tasks.FirstOrDefault(t=>t.Status is not ("Completed" or "Skipped"))?.Code}; }).OrderByDescending(x=>x.PendingTaskCount).ToList();
  return new Result<List<RecruitmentPreboardingQueueItem>>{Success=true,StatusCode=200,MethodResult=items,Data=items,TotalRecords=items.Count,Message="Preboarding queue loaded."};
 }
 public async Task<Result<string>> UpdatePreboarding(RecruitmentPreboardingUpdateCommand command, string actor)
 {
  var company=CurrentContext.CompanyId(context); var key=command.IdempotencyKey?.Trim(); if(string.IsNullOrWhiteSpace(company)||string.IsNullOrWhiteSpace(command.CaseId)||string.IsNullOrWhiteSpace(key)||preboardingCases is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company, case, idempotency key, and preboarding configuration are required.");
  var item=await preboardingCases.FirstOrDefault(x=>x.CompanyId==company&&x.CaseId==command.CaseId.Trim()&&!x.IsDeleted); if(item is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Preboarding case was not found.",404); if(item.LastCommandIdempotencyKey==key) return Ok(item.CaseId,"Preboarding command already applied.");
  var version=item.Version<=0?1:item.Version; if(command.ExpectedVersion.HasValue&&command.ExpectedVersion.Value!=version) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Preboarding case was changed by another user.",409);
  var requested=(command.Tasks??[]).Where(x=>!string.IsNullOrWhiteSpace(x.Code)).ToDictionary(x=>x.Code.Trim(),StringComparer.OrdinalIgnoreCase); if(requested.Count==0) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"At least one task update is required.");
  foreach(var task in item.Tasks) if(requested.TryGetValue(task.Code,out var update)){ if(update.Status is not ("Pending" or "InProgress" or "Completed" or "Skipped")) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Invalid preboarding task status."); task.Status=update.Status; task.AssigneeUserId=update.AssigneeUserId?.Trim()??task.AssigneeUserId; if(task.Status=="Completed") task.CompletedAtUtc=DateTime.UtcNow; }
  var status=command.Status.Trim(); if(status=="ReadyForHire"&&item.Tasks.Any(x=>x.Status is not ("Completed" or "Skipped"))) return Fail<string>(RecruitmentErrorCodes.InvalidTransition,"Mandatory preboarding tasks are incomplete.",409); if(status is not ("PendingCandidateDetails" or "InProgress" or "ReadyForHire" or "Completed")) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Invalid preboarding status.");
  item.Status=status; item.LastCommandIdempotencyKey=key; item.Version=version+1; item.UpdatedBy=actor; item.UpdatedDate=DateTime.UtcNow; var updated=await preboardingCases.Update(Builders<RecruitmentPreboardingCase>.Filter.Where(x=>x.CompanyId==company&&x.CaseId==item.CaseId&&(x.Version==version||x.Version==0)),item); return updated.Success?Ok(item.CaseId,"Preboarding updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Preboarding case was changed by another user.",409);
 }

 public async Task<Result<RecruitmentFeedbackSlaReport>> FeedbackSla(string? vacancyId)
 {
  var company=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(company)) return Fail<RecruitmentFeedbackSlaReport>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");
  var interviewRows=(await interviews.GetAll(x=>x.CompanyId==company&&!x.IsDeleted&&(string.IsNullOrWhiteSpace(vacancyId)||x.VacancyId==vacancyId)&&(x.Status==RecruitmentInterviewStatus.Scheduled||x.Status==RecruitmentInterviewStatus.Confirmed))).ToList();
  var items=new List<RecruitmentFeedbackSlaItem>(); var now=DateTime.UtcNow;
  foreach(var interview in interviewRows) { var cards=(await scorecards.GetAll(x=>x.CompanyId==company&&x.InterviewId==interview.InterviewId&&!x.IsDeleted&&(x.Status==RecruitmentScorecardStatus.Submitted||x.Status==RecruitmentScorecardStatus.Locked))).ToList(); var due=cards.Where(x=>x.FeedbackDueAtUtc.HasValue).Select(x=>x.FeedbackDueAtUtc!.Value).DefaultIfEmpty(interview.EndsAtUtc.AddHours(24)).Min(); items.Add(new RecruitmentFeedbackSlaItem { InterviewId=interview.InterviewId, ApplicantId=interview.ApplicantId, VacancyId=interview.VacancyId, RoundNumber=interview.RoundNumber, DueAtUtc=due, AssignedInterviewers=interview.PanelistUserIds.Length, SubmittedScorecards=cards.Select(x=>x.ReviewerUserId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Overdue=due<now && cards.Count < Math.Max(1, interview.QuorumRequired) }); }
  var report=new RecruitmentFeedbackSlaReport { TotalOpenRounds=items.Count, OverdueRounds=items.Count(x=>x.Overdue), Items=items.OrderByDescending(x=>x.Overdue).ThenBy(x=>x.DueAtUtc).ToList() }; return new Result<RecruitmentFeedbackSlaReport> { Success=true, StatusCode=200, MethodResult=report, Data=report, TotalRecords=report.TotalOpenRounds, Message="Feedback SLA report loaded." };
 }
 public async Task<Result<int>> QueueFeedbackReminders(string? vacancyId, string actorUserId)
 {
  var report=await FeedbackSla(vacancyId); if(!report.Success || report.MethodResult is null) return new Result<int> { Success=false, StatusCode=report.StatusCode, ErrorCode=report.ErrorCode, Message=report.Message };
  var company=CurrentContext.CompanyId(context); var queued=0;
  foreach(var item in report.MethodResult.Items.Where(x=>x.Overdue)) { var key=$"feedback-reminder:{item.InterviewId}:{item.DueAtUtc:O}"; var existing=await outbox.FirstOrDefault(x=>x.CompanyId==company&&x.IdempotencyKey==key&&!x.IsDeleted); if(existing is not null) continue; var add=await outbox.AddOne(new RecruitmentAutomationOutboxEvent { CompanyId=company, EventType="InterviewerFeedbackReminder", ApplicantId=item.ApplicantId, IdempotencyKey=key, Payload=new Dictionary<string,string> { ["interviewId"]=item.InterviewId, ["vacancyId"]=item.VacancyId, ["roundNumber"]=item.RoundNumber.ToString(), ["submittedScorecards"]=item.SubmittedScorecards.ToString(), ["assignedInterviewers"]=item.AssignedInterviewers.ToString() }, CreatedBy=actorUserId, UpdatedBy=actorUserId }); if(add.Success) queued++; }
  return new Result<int> { Success=true, StatusCode=200, MethodResult=queued, Data=queued, Message=$"Queued {queued} interviewer feedback reminder(s)." };
 }
 public async Task<Result<string>> UpsertSla(RecruitmentStageSlaPolicyCommand command,string actor){var c=CurrentContext.CompanyId(context); if(string.IsNullOrWhiteSpace(c)||string.IsNullOrWhiteSpace(command.Stage)||command.TargetHours<=0)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Stage and positive target hours are required."); var normalizedLocation=string.IsNullOrWhiteSpace(command.Location)?null:command.Location.Trim(); var p=await slaPolicies.FirstOrDefault(x=>x.CompanyId==c&&x.Stage==command.Stage.Trim()&&x.Location==normalizedLocation&&!x.IsDeleted); if(p is null){p=new RecruitmentStageSlaPolicy{CompanyId=c,Stage=command.Stage.Trim(),Location=normalizedLocation,TargetHours=command.TargetHours,Enabled=command.Enabled,LastCommandIdempotencyKey=command.IdempotencyKey,CreatedBy=actor,UpdatedBy=actor}; var add=await slaPolicies.AddOne(p); return add.Success?Ok(p.PolicyId,"SLA policy saved."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"SLA policy could not be saved.",409);} p.TargetHours=command.TargetHours;p.Enabled=command.Enabled;p.LastCommandIdempotencyKey=command.IdempotencyKey;p.UpdatedBy=actor;p.UpdatedDate=DateTime.UtcNow;var u=await slaPolicies.Update(Builders<RecruitmentStageSlaPolicy>.Filter.Where(x=>x.CompanyId==c&&x.PolicyId==p.PolicyId),p);return u.Success?Ok(p.PolicyId,"SLA policy updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"SLA policy could not be updated.",409);}
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
   new(){Code="scorecards",Label="Configure interview scorecards",Complete=c.RecruitmentMode=="Simple"||c.Scorecards}
  };
  var all=required.Concat(recommended).ToList();
  return new RecruitmentReadinessDto{Mode=c.RecruitmentMode??"Simple",Timezone=c.DefaultTimezone??"UTC",ReadyToPublishJobs=required.All(x=>x.Complete),CompletionPercent=all.Count==0?0:(int)Math.Round(all.Count(x=>x.Complete)*100d/all.Count),Required=required,Recommended=recommended};
 }
 public async Task<Result<string>> UpsertConfig(RecruitmentEnterpriseConfigCommand command,string actor){var c=CurrentContext.CompanyId(context);if(string.IsNullOrWhiteSpace(c))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");var e=await configs.FirstOrDefault(x=>x.CompanyId==c&&!x.IsDeleted);var mode=(command.RecruitmentMode??e?.RecruitmentMode??"Simple").Trim();if(mode is not ("Simple" or "Standard" or "Enterprise"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Recruitment mode must be Simple, Standard, or Enterprise.");var schemeName=(command.CompensationSchemeName??e?.CompensationSchemeName??"Standard").Trim();var currency=(command.CompensationCurrency??e?.CompensationCurrency??"INR").Trim().ToUpperInvariant();var components=(command.CompensationComponents??e?.CompensationComponents??[]).Where(x=>!string.IsNullOrWhiteSpace(x.Key)&&x.Value>=0).ToDictionary(x=>x.Key.Trim(),x=>x.Value,StringComparer.OrdinalIgnoreCase);if(schemeName.Length>80||currency.Length is < 3 or > 3||components.Count>30)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Compensation scheme, currency, or component count is invalid.");var locations=(command.Locations??e?.Locations??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();var enableSla=command.EnableSla??e?.EnableSla??false;var enableAutomation=command.EnableAutomation??e?.EnableAutomation??false;var retentionDays=command.DefaultCandidateRetentionDays??e?.DefaultCandidateRetentionDays??730;var offerSubject=(command.OfferLetterSubjectTemplate??e?.OfferLetterSubjectTemplate??"Formal Offer of Employment: [JobTitle] - [CandidateName]").Trim();var offerBody=(command.OfferLetterBodyTemplate??e?.OfferLetterBodyTemplate??"<p>Dear [CandidateName],</p><p>We are pleased to extend this formal offer of employment for the position of <strong>[JobTitle]</strong>.</p><p><strong>Compensation (Total CTC):</strong> [Currency] [TotalCtc]</p><p>Please review the terms of this offer. To confirm your acceptance, kindly reply directly to this email with your written confirmation and signed offer letter.</p><p>Sincerely,<br/><strong>Human Resources & Talent Acquisition</strong></p>").Trim();if(offerSubject.Length is < 3 or > 160||offerBody.Length is < 3 or > 20000)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Offer letter subject or body length is invalid.");if(retentionDays is < 30 or > 3650)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Candidate retention must be between 30 and 3650 days.");var timezone=(command.DefaultTimezone??e?.DefaultTimezone??"UTC").Trim(); if(string.IsNullOrWhiteSpace(timezone)||!TimeZoneInfo.TryFindSystemTimeZoneById(timezone,out _)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A valid IANA or system timezone is required.");if(!command.ResetToDefaults && enableSla && locations.Count == 0) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Add at least one hiring location before enabling SLA tracking.");if(!command.ResetToDefaults && enableAutomation && !(command.PreboardingChecklist ?? e?.PreboardingChecklist ?? []).Any(x => !string.IsNullOrWhiteSpace(x))) return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Configure at least one preboarding checklist item before enabling automation.");if(e is null)e=new RecruitmentEnterpriseConfig{CompanyId=c,CreatedBy=actor,DefaultVersion=1}; else e.DefaultVersion = Math.Max(1, e.DefaultVersion) + 1;if(command.ResetToDefaults){locations=[];enableSla=false;enableAutomation=false;retentionDays=730;timezone="UTC";mode="Simple";}e.Locations=locations;e.RecruitmentMode=mode;e.EnableSla=enableSla;e.EnableAutomation=enableAutomation;e.ApplicationReview=command.ApplicationReview ?? e!.ApplicationReview;e.HiringManagerReview=command.HiringManagerReview ?? e!.HiringManagerReview;e.InterviewRounds=command.InterviewRounds ?? e!.InterviewRounds;e.ParallelPanels=command.ParallelPanels ?? e!.ParallelPanels;e.Scorecards=command.Scorecards ?? e!.Scorecards;e.OfferApproval=command.OfferApproval ?? e!.OfferApproval;e.OfferNegotiation=command.OfferNegotiation ?? e!.OfferNegotiation;e.CandidateEmailActions=command.CandidateEmailActions ?? e!.CandidateEmailActions;e.Requisitions=command.Requisitions ?? e!.Requisitions;e.SlaTracking=command.SlaTracking ?? e!.SlaTracking;e.Automation=command.Automation ?? e!.Automation;e.InternalMobility=command.InternalMobility ?? e!.InternalMobility;e.RehireLinks=command.RehireLinks ?? e!.RehireLinks;e.AutoAdvanceOnScreening=command.ResetToDefaults?true:(command.AutoAdvanceOnScreening ?? e!.AutoAdvanceOnScreening);e.RequireInterviewBeforeDecision=command.ResetToDefaults?false:(command.RequireInterviewBeforeDecision ?? e!.RequireInterviewBeforeDecision);e.RequirePreboardingBeforeHandoff=command.ResetToDefaults?false:(command.RequirePreboardingBeforeHandoff ?? e!.RequirePreboardingBeforeHandoff);e.AllowMultiVacancyApplications=command.ResetToDefaults?true:(command.AllowMultiVacancyApplications ?? e!.AllowMultiVacancyApplications);e.AllowJobWorkflowCustomization=command.ResetToDefaults?false:(command.AllowJobWorkflowCustomization ?? e!.AllowJobWorkflowCustomization);e.ScreeningCriteria=(command.ScreeningCriteria??e.ScreeningCriteria??["Relevant experience","Required skills","Communication"]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();e.ScreeningPassScore=Math.Clamp(command.ScreeningPassScore??e.ScreeningPassScore,0,100);e.PreboardingChecklist=(command.PreboardingChecklist??e.PreboardingChecklist??["candidate-details","documents","verification"]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();e.CompensationSchemeName=command.ResetToDefaults?"Standard":schemeName;e.CompensationCurrency=command.ResetToDefaults?"INR":currency;e.CompensationComponents=command.ResetToDefaults?new Dictionary<string,decimal>():components;e.DefaultCandidateRetentionDays=retentionDays;e.OfferLetterSubjectTemplate=command.ResetToDefaults?"Formal Offer of Employment: [JobTitle] - [CandidateName]":offerSubject;e.OfferLetterBodyTemplate=command.ResetToDefaults?"<p>Dear [CandidateName],</p><p>We are pleased to extend this formal offer of employment for the position of <strong>[JobTitle]</strong>.</p><p><strong>Compensation (Total CTC):</strong> [Currency] [TotalCtc]</p><p>Please review the terms of this offer. To confirm your acceptance, kindly reply directly to this email with your written confirmation and signed offer letter.</p><p>Sincerely,<br/><strong>Human Resources & Talent Acquisition</strong></p>":offerBody;e.DefaultTimezone=timezone;e.ConfigSource=command.ResetToDefaults?"SystemDefault":"CompanyOverride";e.OverriddenFields=command.ResetToDefaults?new List<string>():new[] { "Locations", "RecruitmentMode", "EnableSla", "EnableAutomation", "DefaultTimezone" }.Where(field=>(field=="Locations"&&locations.Count>0)||(field=="RecruitmentMode"&&mode!="Simple")||(field=="EnableSla"&&!enableSla)||(field=="EnableAutomation"&&!enableAutomation)||false).ToList();e.LastCommandIdempotencyKey=command.IdempotencyKey;e.UpdatedBy=actor;e.UpdatedDate=DateTime.UtcNow;if(string.IsNullOrWhiteSpace(e.ConfigId)){var add=await configs.AddOne(e);return add.Success?Ok(e.ConfigId,"Recruitment configuration saved."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Recruitment configuration could not be saved.",409);}var u=await configs.Update(Builders<RecruitmentEnterpriseConfig>.Filter.Where(x=>x.CompanyId==c&&x.ConfigId==e.ConfigId),e);return u.Success?Ok(e.ConfigId,command.ResetToDefaults?"Recruitment configuration reset to defaults.":"Recruitment configuration updated."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Recruitment configuration could not be updated.",409);} public async Task<Result<RecruitmentEnterpriseConfig>> GetConfig(){var c=CurrentContext.CompanyId(context);if(string.IsNullOrWhiteSpace(c))return Fail<RecruitmentEnterpriseConfig>(RecruitmentErrorCodes.InvalidRequest,"Company context is required.");var e=await configs.FirstOrDefault(x=>x.CompanyId==c&&!x.IsDeleted);if(e is null)e=new RecruitmentEnterpriseConfig{CompanyId=c,ConfigSource="SystemDefault",DefaultVersion=1,Locations=[],RecruitmentMode="Simple",EnableSla=false,EnableAutomation=false,AutoAdvanceOnScreening=true,RequireInterviewBeforeDecision=false,RequirePreboardingBeforeHandoff=false,AllowMultiVacancyApplications=true,AllowJobWorkflowCustomization=false,DefaultTimezone="UTC",DefaultCandidateRetentionDays=730,OverriddenFields=[]};return new Result<RecruitmentEnterpriseConfig>{Success=true,StatusCode=200,MethodResult=e,Data=e,Message=e.ConfigSource=="SystemDefault"?"Recruitment defaults loaded.":"Recruitment configuration loaded."};} public async Task<Result<string>> Link(RecruitmentRelationshipLinkCommand command,string actor){var c=CurrentContext.CompanyId(context);var key=command.IdempotencyKey?.Trim();if(string.IsNullOrWhiteSpace(c)||string.IsNullOrWhiteSpace(key)||key.Length<12)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Company and an idempotency key of at least 12 characters are required.");var existing=await links.FirstOrDefault(x=>x.CompanyId==c&&x.LastCommandIdempotencyKey==key&&!x.IsDeleted);if(existing is not null)return Ok(existing.LinkId,"Relationship link already exists.");var a=await applicants.FirstOrDefault(x=>x.CompanyId==c&&x.ApplicantId==command.ApplicantId&&!x.IsDeleted);if(a is null)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Applicant was not found in the current company.");if(string.IsNullOrWhiteSpace(command.RelatedPersonId)||command.RelatedPersonId.Trim()==a.ApplicantId)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"A valid related person is required and cannot be the applicant.");if(command.RelationshipType is not ("InternalMobility" or "Rehire"))return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Relationship type must be InternalMobility or Rehire.");var e=new RecruitmentRelationshipLink{CompanyId=c,ApplicantId=a.ApplicantId,RelationshipType=command.RelationshipType,RelatedPersonId=command.RelatedPersonId.Trim(),Notes=command.Notes?.Trim(),LastCommandIdempotencyKey=key,CreatedBy=actor,UpdatedBy=actor};var add=await links.AddOne(e);return add.Success?Ok(e.LinkId,"Recruitment relationship linked."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Relationship could not be linked.",409);}
 public async Task<Result<string>> Enqueue(RecruitmentAutomationEventCommand command,string actor){var c=CurrentContext.CompanyId(context);var key=command.IdempotencyKey?.Trim();if(string.IsNullOrWhiteSpace(c)||string.IsNullOrWhiteSpace(command.EventType)||string.IsNullOrWhiteSpace(key)||key.Length<12)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Event type and an idempotency key of at least 12 characters are required.");var existing=await outbox.FirstOrDefault(x=>x.CompanyId==c&&x.IdempotencyKey==key&&!x.IsDeleted);if(existing is not null)return Ok(existing.EventId,"Automation event already queued.");if(!string.IsNullOrWhiteSpace(command.ApplicantId)){var applicant=await applicants.FirstOrDefault(x=>x.CompanyId==c&&x.ApplicantId==command.ApplicantId.Trim()&&!x.IsDeleted);if(applicant is null)return Fail<string>(RecruitmentErrorCodes.InvalidRequest,"Applicant was not found in the current company.");}var e=new RecruitmentAutomationOutboxEvent{CompanyId=c,EventType=command.EventType.Trim(),ApplicantId=command.ApplicantId?.Trim(),Payload=command.Payload??[],IdempotencyKey=key,CreatedBy=actor,UpdatedBy=actor};var add=await outbox.AddOne(e);return add.Success?Ok(e.EventId,"Automation event queued."):Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict,"Automation event could not be queued.",409);}
 private static Result<T> Fail<T>(string code,string msg,int status=400)=>new(){Success=false,StatusCode=status,ErrorCode=code,Message=msg}; private static Result<string> Ok(string id,string msg)=>new(){Success=true,StatusCode=200,MethodResult=id,Message=msg};
}



