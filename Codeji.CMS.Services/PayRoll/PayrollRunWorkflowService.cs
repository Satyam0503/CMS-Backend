using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;
namespace Codeji.CMS.Services.PayRoll;
public interface IPayrollRunWorkflowService
{
 Task<Result> Validate(string companyId,string runId,string actorId);
 Task<Result> Cancel(string companyId,string runId,string actorId,string reason);
 Task<Result> ExcludeEmployee(string companyId,string runId,string employeeId,string actorId,string reason);
 Task<Result> IncludeEmployee(string companyId,string runId,string employeeId,string actorId);
 Task<Result> Approve(string companyId,string runId,string actorId);
 Task<Result> Process(string companyId,string runId,string actorId);
 Task<Result> Close(string companyId,string runId,string actorId);
 Task<Result> Reopen(string companyId,string runId,string actorId,string reason);
 Task<Result> CreateCorrectionRun(string companyId,string runId,string actorId,string reason,DateTime? payDate=null);
}
public sealed class PayrollRunWorkflowService(IMongoDbRepository<PayrollRun> runs,IMongoDbRepository<PayrollRunEmployee> employees,IMongoDbRepository<EmpPayRoll>? payrolls=null,IMongoDbRepository<PayrollAdjustment>? adjustments=null):IPayrollRunWorkflowService
{
 public async Task<Result> Validate(string c,string id,string actor)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!="CALCULATED")return Fail(409,"Payroll run must be calculated before validation.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList();
  var error=CalculatedRowsError(run,rows);
  return error is null
   ?new Result{Success=true,Message="Payroll run calculation verified.",ResourceId=id}
   :Fail(409,error);
 }
 public async Task<Result> Cancel(string c,string id,string actor,string reason)
 {
  var normalizedReason=(reason??string.Empty).Trim();
  if(normalizedReason.Length<5)return Fail(400,"A cancellation reason of at least 5 characters is required.");
  if(normalizedReason.Length>500)return Fail(400,"Cancellation reason cannot exceed 500 characters.");
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status=="CANCELLED"&&string.Equals(run.RunType,"CORRECTION",StringComparison.OrdinalIgnoreCase))
   return await ReleaseRunAdjustments(c,run,actor,"Cancelled correction run reservation cleanup retry.");
  if(string.Equals(run.RunType,"CORRECTION",StringComparison.OrdinalIgnoreCase)&&adjustments is null)
   return Fail(503,"Adjustment storage is unavailable; correction run cannot be cancelled safely until its reserved adjustments can be released.");
  var previousStatus=run.Status;
  var previousVersion=run.Version;
  var isCorrection=string.Equals(run.RunType,"CORRECTION",StringComparison.OrdinalIgnoreCase);
  var cancellable=isCorrection
   ? previousStatus is "DRAFT" or "BLOCKED" or "CALCULATED" or "APPROVED"
   : previousStatus is "DRAFT" or "BLOCKED";
  if(!cancellable)
   return Fail(409,"This payroll run cannot be cancelled in its current state. Processed payroll must be corrected through a separate correction run.");
  run.Status="CANCELLED";
  run.IsActive=false;
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent
  {
   Action="RunCancelled",
   ActorId=actor,
   OccurredAtUtc=DateTime.UtcNow,
   Notes=normalizedReason
  });
  var saved=await runs.Update(
   Builders<PayrollRun>.Filter.Eq(x=>x.CompanyId,c)&
   Builders<PayrollRun>.Filter.Eq(x=>x.Id,id)&
   Builders<PayrollRun>.Filter.Eq(x=>x.Status,previousStatus)&
   Builders<PayrollRun>.Filter.Eq(x=>x.Version,previousVersion),run);
  if(!saved.Success)return Fail(409,"Payroll run changed before cancellation; reload before retrying.");
  if(string.Equals(run.RunType,"CORRECTION",StringComparison.OrdinalIgnoreCase))
  {
   var released=await ReleaseRunAdjustments(c,run,actor,normalizedReason);
   if(!released.Success)return released;
  }
  return new Result{Success=true,Message="Draft payroll run cancelled; its snapshots and audit history were retained. Create a new run to recalculate.",ResourceId=id};
 }
 public async Task<Result> ExcludeEmployee(string c, string id, string employeeId, string actor, string reason)
 {
  var normalizedReason = (reason ?? string.Empty).Trim();
  if(normalizedReason.Length < 5) return Fail(400, "An exclusion reason of at least 5 characters is required.");
  if(normalizedReason.Length > 500) return Fail(400, "Exclusion reason cannot exceed 500 characters.");
  var run = await runs.FirstOrDefault(x => x.CompanyId == c && x.Id == id);
  if(run is null) return Fail(404, "Payroll run was not found.");
  if(run.Status is not ("DRAFT" or "BLOCKED"))
   return Fail(409, "Employees can only be excluded from a draft or blocked payroll run.");
  var member = await employees.FirstOrDefault(x => x.CompanyId == c && x.PayrollRunId == id && x.EmployeeId == employeeId);
  if(member is null) return Fail(404, "Employee is not part of this payroll run.");
  if(member.Status == "SKIPPED" && string.Equals(member.ExclusionReason, normalizedReason, StringComparison.Ordinal))
   return new Result { Success = true, Message = "Employee is already excluded with this reason.", ResourceId = id };

  member.Status = "SKIPPED";
  member.ExclusionReason = normalizedReason;
  member.ErrorCode = null;
  member.ErrorMessage = null;
  member.GrossAmount = 0m;
  member.DeductionAmount = 0m;
  member.NetAmount = 0m;
  var memberSaved = await employees.Update(
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.CompanyId, c) &
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.PayrollRunId, id) &
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.Id, member.Id),
   member);
  if(!memberSaved.Success) return Fail(500, "Failed to update employee exclusion status.");

  var allMembers = (await employees.GetAll(x => x.CompanyId == c && x.PayrollRunId == id)).ToList();
  run.ReadyCount = allMembers.Count(x => x.Status == "CALCULATED");
  run.SkippedCount = allMembers.Count(x => x.Status == "SKIPPED");
  run.BlockedCount = allMembers.Count(x => x.Status == "BLOCKED");
  var previousStatus = run.Status;
  var previousVersion = run.Version;
  if(run.BlockedCount == 0 && run.Status == "BLOCKED") run.Status = "DRAFT";
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent
  {
   Action = "EmployeeExcluded",
   ActorId = actor,
   OccurredAtUtc = DateTime.UtcNow,
   Notes = $"Excluded employee {employeeId}: {normalizedReason}"
  });
  var runSaved = await runs.Update(
   Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, c) &
   Builders<PayrollRun>.Filter.Eq(x => x.Id, id) &
   Builders<PayrollRun>.Filter.Eq(x => x.Status, previousStatus) &
   Builders<PayrollRun>.Filter.Eq(x => x.Version, previousVersion),
   run);
  return runSaved.Success
   ? new Result { Success = true, Message = $"Employee {employeeId} excluded from payroll run.", ResourceId = id }
   : Fail(409, "Payroll run changed concurrently; reload and retry.");
 }
 public async Task<Result> IncludeEmployee(string c, string id, string employeeId, string actor)
 {
  var run = await runs.FirstOrDefault(x => x.CompanyId == c && x.Id == id);
  if(run is null) return Fail(404, "Payroll run was not found.");
  if(run.Status is not ("DRAFT" or "BLOCKED"))
   return Fail(409, "Employees can only be re-included in a draft or blocked payroll run.");
  var member = await employees.FirstOrDefault(x => x.CompanyId == c && x.PayrollRunId == id && x.EmployeeId == employeeId);
  if(member is null) return Fail(404, "Employee is not part of this payroll run.");
  if(member.Status != "SKIPPED")
   return new Result { Success = true, Message = "Employee is not excluded from this payroll run.", ResourceId = id };

  member.Status = "READY";
  member.ExclusionReason = null;
  member.ErrorCode = null;
  member.ErrorMessage = null;
  var memberSaved = await employees.Update(
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.CompanyId, c) &
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.PayrollRunId, id) &
   Builders<PayrollRunEmployee>.Filter.Eq(x => x.Id, member.Id),
   member);
  if(!memberSaved.Success) return Fail(500, "Failed to update employee inclusion status.");

  var allMembers = (await employees.GetAll(x => x.CompanyId == c && x.PayrollRunId == id)).ToList();
  run.ReadyCount = allMembers.Count(x => x.Status == "CALCULATED");
  run.SkippedCount = allMembers.Count(x => x.Status == "SKIPPED");
  run.BlockedCount = allMembers.Count(x => x.Status == "BLOCKED");
  var previousStatus = run.Status;
  var previousVersion = run.Version;
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent
  {
   Action = "EmployeeIncluded",
   ActorId = actor,
   OccurredAtUtc = DateTime.UtcNow,
   Notes = $"Re-included employee {employeeId} in payroll run"
  });
  var runSaved = await runs.Update(
   Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, c) &
   Builders<PayrollRun>.Filter.Eq(x => x.Id, id) &
   Builders<PayrollRun>.Filter.Eq(x => x.Status, previousStatus) &
   Builders<PayrollRun>.Filter.Eq(x => x.Version, previousVersion),
   run);
  return runSaved.Success
   ? new Result { Success = true, Message = $"Employee {employeeId} re-included in payroll run.", ResourceId = id }
   : Fail(409, "Payroll run changed concurrently; reload and retry.");
 }
 public async Task<Result> Approve(string c,string id,string actor)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!="CALCULATED")return Fail(409,"Only a calculated payroll run can be approved."); if(string.IsNullOrWhiteSpace(run.ReviewedBy)||run.ReviewedAtUtc is null)return Fail(409,"Payroll run must be reviewed before approval.");
  if(!string.IsNullOrWhiteSpace(run.CreatedBy)&&string.Equals(run.CreatedBy,actor,StringComparison.OrdinalIgnoreCase))return Fail(403,"The payroll run creator cannot approve their own run.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList();
  var rowError=CalculatedRowsError(run,rows);
  if(rowError is not null)return Fail(409,rowError);
  var approvalNote = run.SkippedCount > 0
   ? $"Payroll run approved and inputs frozen with {run.ReadyCount} calculated employee(s) and {run.SkippedCount} authorized excluded employee(s)."
   : "Payroll run approved and inputs frozen.";
  return await Transition(c,id,actor,"CALCULATED","APPROVED",approvalNote);
 }
 public async Task<Result> Process(string c,string id,string actor)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status=="APPROVED")
  {
   var transition=await Transition(c,id,actor,"APPROVED","PROCESSED","Payroll run processed; payroll-row synchronization is pending.");
   if(!transition.Success)return transition;
   run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
   if(run is null)return Fail(404,"Payroll run was processed, but could not be reloaded to finish payroll-row synchronization.");
  }
  else if(run.Status!="PROCESSED")
   return Fail(409,"Only an approved payroll run can be processed, or a processed run can be retried to finish payroll-row synchronization.");

  if(run.LegacyPayrollRowsSynchronizedAtUtc.HasValue)
  {
   var adjustmentSync = await MarkRunAdjustmentsProcessed(c, run, actor);
   return adjustmentSync ?? new Result{Success=true,Message="Payroll run and payroll rows are already synchronized.",ResourceId=id};
  }
  if(payrolls is null)
   return Fail(503,"Payroll run is processed, but payroll-row synchronization is unavailable. Configure the payroll repository, then retry Process.");

  try
  {
   var runRows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id&&x.Status=="CALCULATED")).ToList();
   var targetEmployeeIds=runRows.Select(x=>x.EmployeeId).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   if(targetEmployeeIds.Count!=runRows.Count)
    return Fail(409,"Payroll run contains a blank or duplicate employee identity; payroll-row synchronization was not completed.");

   if(targetEmployeeIds.Count>0)
   {
    var filter=Builders<EmpPayRoll>.Filter.Eq(p=>p.CompanyId,c)&
               Builders<EmpPayRoll>.Filter.Eq(p=>p.PayMonth,run.PeriodStart)&
               Builders<EmpPayRoll>.Filter.In(p=>p.EmployeeId,targetEmployeeIds)&
               Builders<EmpPayRoll>.Filter.Or(Builders<EmpPayRoll>.Filter.Ne(p=>p.IsProcessed,true),Builders<EmpPayRoll>.Filter.Ne(p=>p.WorkflowStatus,"PROCESSED"))&
               Builders<EmpPayRoll>.Filter.Ne(p=>p.PaymentStatus,"PAID")&
               Builders<EmpPayRoll>.Filter.Ne(p=>p.PaymentStatus,"CLOSED")&
               Builders<EmpPayRoll>.Filter.Ne(p=>p.WorkflowStatus,"PAID")&
               Builders<EmpPayRoll>.Filter.Ne(p=>p.WorkflowStatus,"CLOSED");
    var update=Builders<EmpPayRoll>.Update.Set(p=>p.IsProcessed,true)
     .Set(p=>p.WorkflowStatus,"PROCESSED").Set(p=>p.ProcessedAt,DateTime.UtcNow).Set(p=>p.ProcessedBy,actor)
     .Push(p=>p.AuditHistory,new PayrollAuditEvent{Action="PayrollProcessedViaRun",ActorId=actor,OccurredAtUtc=DateTime.UtcNow,Notes=$"Synchronized from Payroll Run {id}"});
    await payrolls.UpdateMany(filter,update);

    var persisted=(await payrolls.GetAll(p=>p.CompanyId==c&&p.PayMonth==run.PeriodStart&&targetEmployeeIds.Contains(p.EmployeeId))).ToList();
    var perEmployee=persisted.GroupBy(p=>p.EmployeeId,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.ToList(),StringComparer.OrdinalIgnoreCase);
    var incomplete=targetEmployeeIds.Where(employeeId=>!perEmployee.TryGetValue(employeeId,out var matches)||matches.Count!=1||!IsLegacyRowSynchronized(matches[0])).ToArray();
    if(incomplete.Length>0)
     return Fail(503,$"Payroll run is processed, but payroll rows are not synchronized for {incomplete.Length} employee(s). Resolve missing or duplicate monthly payroll rows, then retry Process.");
   }

   var syncResult = await MarkLegacyRowsSynchronized(c,run,actor,$"Synchronized {targetEmployeeIds.Count} employee payroll row(s) from Payroll Run {id}.");
   if(!syncResult.Success)return syncResult;
   return await MarkRunAdjustmentsProcessed(c, run, actor) ?? syncResult;
  }
  catch(Exception)
  {
   return Fail(503,"Payroll run is processed, but payroll-row synchronization could not be verified. Retry Process to resume safely.");
  }
 }
 private async Task<Result?> MarkRunAdjustmentsProcessed(string c,PayrollRun run,string actor)
 {
  if(!string.Equals(run.RunType,"CORRECTION",StringComparison.OrdinalIgnoreCase))return null;
  var lines=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==run.Id&&x.Status=="CALCULATED")).ToList();
  var adjustmentIds=lines.SelectMany(x=>x.AppliedAdjustmentIds??[]).Distinct(StringComparer.Ordinal).ToList();
  if(adjustmentIds.Count==0)return null;
  if(adjustments is null)return Fail(503,"Correction payroll was processed, but adjustment status storage is unavailable. Retry processing after payroll adjustment storage is configured.");
  foreach(var adjustmentId in adjustmentIds)
  {
   var adjustment=await adjustments.FirstOrDefault(x=>x.CompanyId==c&&x.Id==adjustmentId);
   if(adjustment is null)return Fail(409,$"Correction payroll was processed, but adjustment {adjustmentId} could not be verified.");
   if(adjustment.Status=="PROCESSED"&&adjustment.CorrectionPayrollRunId==run.Id)continue;
   if(adjustment.Status!="APPROVED"||adjustment.OriginalPayrollRunId!=run.ParentRunId||
      adjustment.EmployeeId!=lines.FirstOrDefault(x=>(x.AppliedAdjustmentIds??[]).Contains(adjustmentId,StringComparer.Ordinal))?.EmployeeId||
      adjustment.CorrectionPayrollRunId != run.Id)
    return Fail(409,$"Correction payroll was processed, but adjustment {adjustmentId} no longer matches its approved source. Resolve the audit state before retrying.");
   adjustment.Status="PROCESSED";
   adjustment.CorrectionPayrollRunId=run.Id;
   adjustment.ProcessedBy=actor;
   adjustment.ProcessedAtUtc=DateTime.UtcNow;
   adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent{Action="Processed",ActorId=actor,OccurredAtUtc=DateTime.UtcNow,Notes=$"Included in correction payroll run {run.Id}"});
   var saved=await adjustments.Update(
    Builders<PayrollAdjustment>.Filter.Eq(x=>x.CompanyId,c)&
    Builders<PayrollAdjustment>.Filter.Eq(x=>x.Id,adjustmentId)&
    Builders<PayrollAdjustment>.Filter.Eq(x=>x.Status,"APPROVED")&
    Builders<PayrollAdjustment>.Filter.Eq(x=>x.CorrectionPayrollRunId,run.Id),adjustment);
   if(!saved.Success)return Fail(503,$"Correction payroll was processed, but adjustment {adjustmentId} status could not be synchronized. Retry processing to finish the audit update.");
  }
  return null;
 }
 private static bool IsLegacyRowSynchronized(EmpPayRoll row)=>(row.IsProcessed&&string.Equals(row.WorkflowStatus,"PROCESSED",StringComparison.OrdinalIgnoreCase))||string.Equals(row.PaymentStatus,"PAID",StringComparison.OrdinalIgnoreCase)||string.Equals(row.PaymentStatus,"CLOSED",StringComparison.OrdinalIgnoreCase)||string.Equals(row.WorkflowStatus,"PAID",StringComparison.OrdinalIgnoreCase)||string.Equals(row.WorkflowStatus,"CLOSED",StringComparison.OrdinalIgnoreCase);
 private async Task<Result> MarkLegacyRowsSynchronized(string c,PayrollRun run,string actor,string note)
 {
  var expectedVersion=run.Version;
  run.LegacyPayrollRowsSynchronizedAtUtc=DateTime.UtcNow;
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent{Action="LegacyPayrollRowsSynchronized",ActorId=actor,OccurredAtUtc=run.LegacyPayrollRowsSynchronizedAtUtc.Value,Notes=note});
  var saved=await runs.Update(Builders<PayrollRun>.Filter.Eq(x=>x.CompanyId,c)&Builders<PayrollRun>.Filter.Eq(x=>x.Id,run.Id)&Builders<PayrollRun>.Filter.Eq(x=>x.Status,"PROCESSED")&Builders<PayrollRun>.Filter.Eq(x=>x.Version,expectedVersion),run);
  return saved.Success?new Result{Success=true,Message="Payroll run processed and payroll rows synchronized.",ResourceId=run.Id}:Fail(409,"Payroll rows are synchronized, but the run acknowledgement changed concurrently. Retry Process to finish safely.");
 }
 public Task<Result> Close(string c,string id,string actor)=>Transition(c,id,actor,"PAID","CLOSED","Payroll run closed.");
 public async Task<Result> Reopen(string c, string id, string actor, string reason)
 {
  var normalizedReason = (reason ?? string.Empty).Trim();
  if (normalizedReason.Length < 5) return Fail(400, "A reopening reason of at least 5 characters is required.");
  if (normalizedReason.Length > 500) return Fail(400, "Reopening reason cannot exceed 500 characters.");

  var run = await runs.FirstOrDefault(x => x.CompanyId == c && x.Id == id);
  if (run is null) return Fail(404, "Payroll run was not found.");

  var fromStatus = run.Status;
  string targetStatus;
  if (fromStatus == "CLOSED")
   targetStatus = "PAID";
  else if (fromStatus == "APPROVED")
   targetStatus = "DRAFT";
  else
   return Fail(409, "Only a closed or approved payroll run can be reopened.");

  var previousVersion = run.Version;
  run.Status = targetStatus;
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent
  {
   Action = "RunReopened",
   ActorId = actor,
   OccurredAtUtc = DateTime.UtcNow,
   Notes = $"Reopened run from {fromStatus} to {targetStatus}: {normalizedReason}"
  });

  var saved = await runs.Update(
   Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, c) &
   Builders<PayrollRun>.Filter.Eq(x => x.Id, id) &
   Builders<PayrollRun>.Filter.Eq(x => x.Status, fromStatus) &
   Builders<PayrollRun>.Filter.Eq(x => x.Version, previousVersion),
   run);

  if (!saved.Success)
   return Fail(409, "Payroll run changed concurrently; reload and retry.");

  if (payrolls is not null && fromStatus == "CLOSED" && targetStatus == "PAID")
  {
   var payrollFilter = Builders<EmpPayRoll>.Filter.Eq(p => p.CompanyId, c) &
                       Builders<EmpPayRoll>.Filter.Eq(p => p.PayMonth, run.PeriodStart) &
                       (Builders<EmpPayRoll>.Filter.Eq(p => p.PaymentStatus, "CLOSED") | Builders<EmpPayRoll>.Filter.Eq(p => p.WorkflowStatus, "CLOSED"));
   var payrollUpdate = Builders<EmpPayRoll>.Update
    .Set(p => p.PaymentStatus, "PAID")
    .Set(p => p.WorkflowStatus, "PAID")
    .Push(p => p.AuditHistory, new PayrollAuditEvent
    {
     Action = "PayrollReopenedViaRun",
     ActorId = actor,
     OccurredAtUtc = DateTime.UtcNow,
     Notes = $"Reopened from closed status: {normalizedReason}"
    });
   await payrolls.UpdateMany(payrollFilter, payrollUpdate);
  }

  return new Result { Success = true, Message = $"Payroll run reopened to {targetStatus}.", ResourceId = id };
 }

 public async Task<Result> CreateCorrectionRun(string c, string id, string actor, string reason, DateTime? payDate = null)
 {
  var normalizedReason = (reason ?? string.Empty).Trim();
  if (normalizedReason.Length < 5) return Fail(400, "A correction reason of at least 5 characters is required.");
  if (normalizedReason.Length > 500) return Fail(400, "Correction reason cannot exceed 500 characters.");

  var original = await runs.FirstOrDefault(x => x.CompanyId == c && x.Id == id);
  if (original is null) return Fail(404, "Payroll run was not found.");
  if (original.Status != "PAID" && original.Status != "CLOSED")
   return Fail(409, "A correction run can only be created for a paid or closed payroll run.");

  var existingCorrection = await runs.FirstOrDefault(x => x.CompanyId == c && x.ParentRunId == id && x.IsActive && x.Status != "CANCELLED");
  if (existingCorrection is not null)
   return Fail(409, $"An active correction run ({existingCorrection.Id}) already exists for this payroll run.");

  var originalMembers = (await employees.GetAll(x => x.CompanyId == c && x.PayrollRunId == id)).ToList();
  List<PayrollAdjustment> approvedAdjustments = adjustments is null
   ? []
   : (await adjustments.GetAll(x => x.CompanyId == c && x.OriginalPayrollRunId == id && x.Status == "APPROVED" && x.CorrectionPayrollRunId == null && x.TargetPeriod == original.PeriodStart)).ToList();
  if (approvedAdjustments.Count == 0)
   return Fail(409, "No approved, unassigned adjustments exist for this original payroll run and month.");
  var adjustmentEmployeeIds = approvedAdjustments.Select(x => x.EmployeeId).ToHashSet(StringComparer.Ordinal);
  if (approvedAdjustments.Any(x => !originalMembers.Any(m => m.EmployeeId == x.EmployeeId && m.Status != "SKIPPED")))
   return Fail(409, "An approved adjustment does not match an eligible employee in the original payroll run.");
  var correctionMembers = originalMembers.Where(m => adjustmentEmployeeIds.Contains(m.EmployeeId) && m.Status != "SKIPPED").ToList();

  var correction = new PayrollRun
  {
   CompanyId = c,
   PeriodStart = original.PeriodStart,
   PeriodEnd = original.PeriodEnd,
   PayDate = payDate?.Date ?? DateTime.UtcNow.Date,
   RunType = "CORRECTION",
   ParentRunId = id,
   Status = "DRAFT",
   PopulationType = original.PopulationType,
   DepartmentId = original.DepartmentId,
   PayGroupId = original.PayGroupId,
   ConfigurationSnapshot = original.ConfigurationSnapshot,
   EmployeeCount = correctionMembers.Count,
   ReadyCount = correctionMembers.Count,
   SkippedCount = 0,
   BlockedCount = 0,
   CreatedBy = actor,
   CreatedAtUtc = DateTime.UtcNow,
   AuditHistory =
   [
    new PayrollRunAuditEvent
    {
     Action = "CorrectionRunCreated",
     ActorId = actor,
     OccurredAtUtc = DateTime.UtcNow,
     Notes = $"Created correction run for {id}: {normalizedReason}"
    }
   ]
  };

  var runInsert = await runs.AddOne(correction);
  if (!runInsert.Success) return Fail(500, "Correction payroll run could not be created.");

  try
  {
   foreach (var adjustment in approvedAdjustments)
   {
    adjustment.CorrectionPayrollRunId = correction.Id;
    adjustment.AuditHistory.Add(new PayrollAdjustmentAuditEvent
    {
     Action = "CorrectionRunReserved", ActorId = actor, OccurredAtUtc = DateTime.UtcNow,
     Notes = $"Reserved by correction payroll run {correction.Id}; no payment has occurred."
    });
    var reservation = await adjustments!.Update(
     Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, c) &
     Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, adjustment.Id) &
     Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "APPROVED") &
     Builders<PayrollAdjustment>.Filter.Eq(x => x.CorrectionPayrollRunId, null), adjustment);
    if (!reservation.Success)
     return await AbortCorrectionRunCreation(c, correction, actor, 409,
      "An adjustment was reserved by another correction run. Refresh and create a new correction draft.");
   }

   foreach (var m in correctionMembers)
   {
    var employeeAdjustments = approvedAdjustments.Where(x => x.EmployeeId == m.EmployeeId).ToList();
    var correctionMember = new PayrollRunEmployee
    {
    CompanyId = c,
    PayrollRunId = correction.Id,
    EmployeeId = m.EmployeeId,
    Status = "READY",
    ExclusionReason = m.ExclusionReason,
    InclusionReason = m.InclusionReason,
    InputSnapshot = new PayrollEmployeeInputSnapshot
    {
     AssignmentId = m.InputSnapshot.AssignmentId,
     AssignmentVersion = m.InputSnapshot.AssignmentVersion,
     SalaryStructureId = m.InputSnapshot.SalaryStructureId,
     SalaryStructureVersion = m.InputSnapshot.SalaryStructureVersion,
     SalaryStructures = m.InputSnapshot.SalaryStructures,
     SalaryTemplateId = m.InputSnapshot.SalaryTemplateId,
     SalaryTemplateVersion = m.InputSnapshot.SalaryTemplateVersion,
     PayGroupId = m.InputSnapshot.PayGroupId,
     PayGroupVersion = m.InputSnapshot.PayGroupVersion,
     PolicyProfileId = m.InputSnapshot.PolicyProfileId,
     PolicyProfileVersion = m.InputSnapshot.PolicyProfileVersion,
     EpfMode = m.InputSnapshot.EpfMode,
     EpfStatutoryRuleSetId = m.InputSnapshot.EpfStatutoryRuleSetId,
     EpfWageCeiling = m.InputSnapshot.EpfWageCeiling,
     EpfEmployeeRatePercent = m.InputSnapshot.EpfEmployeeRatePercent,
     EpfEmployerRatePercent = m.InputSnapshot.EpfEmployerRatePercent,
     EsicMode = m.InputSnapshot.EsicMode,
     EsicWageCeiling = m.InputSnapshot.EsicWageCeiling,
     EsicEmployeeRatePercent = m.InputSnapshot.EsicEmployeeRatePercent,
     EsicEmployerRatePercent = m.InputSnapshot.EsicEmployerRatePercent,
     TdsMode = m.InputSnapshot.TdsMode,
     RequireAttendanceLock = m.InputSnapshot.RequireAttendanceLock,
     Components = m.InputSnapshot.Components,
     Adjustments = employeeAdjustments.Select(x => new PayrollAdjustmentSnapshot
     {
      Id = x.Id, ComponentCode = x.ComponentCode ?? string.Empty, ComponentName = x.ComponentName ?? string.Empty,
      ComponentKind = x.ComponentKind ?? string.Empty, Amount = x.Amount, IsTaxable = x.IsTaxable ?? true,
      IsStatutory = x.IsStatutory ?? true, IsEmployerContribution = x.IsEmployerContribution ?? true,
      IncludeInEsiWage = x.IncludeInEsiWage ?? false,
      IncludeInPfWage = x.IncludeInPfWage ?? false, IncludeInPtWage = x.IncludeInPtWage ?? false,
      ComponentVersion = x.ComponentVersion ?? 0
     }).ToList()
    },
    StatutorySnapshot = m.StatutorySnapshot,
    AttendanceSnapshot = m.AttendanceSnapshot,
    GrossAmount = 0m,
    DeductionAmount = 0m,
    NetAmount = 0m
    };
    var memberInsert = await employees.AddOne(correctionMember);
    if (!memberInsert.Success)
     return await AbortCorrectionRunCreation(c, correction, actor, 500,
      "Correction employee rows could not be created.");
   }
  }
  catch
  {
   return await AbortCorrectionRunCreation(c, correction, actor, 500,
    "Correction run creation failed while reserving adjustments or writing employee snapshots.");
  }

  return new Result { Success = true, StatusCode = 201, Message = "Correction run created in draft with only employees who have approved adjustments. The run contains adjustment deltas, not the original payroll amounts.", ResourceId = correction.Id };
 }

 private async Task<Result> AbortCorrectionRunCreation(string companyId, PayrollRun run, string actor, int failureCode, string message)
 {
  var released = false;
  var cancelled = false;
  try
  {
   released = (await ReleaseRunAdjustments(companyId, run, actor, message)).Success;
  }
  catch
  {
   // The persisted run remains available for the correction cancellation retry path.
  }
  try
  {
   cancelled = (await CancelCorrectionDraft(companyId, run, actor, message)).Success;
  }
  catch
  {
   // Report incomplete cleanup below; never claim that a failed draft was cancelled.
  }

  if (released && cancelled)
   return Fail(failureCode, $"{message} The correction draft was cancelled and its reservations were released.");
  return Fail(503, $"{message} Automatic cleanup is incomplete (reservations released: {released}; draft cancelled: {cancelled}). Retry cancellation for correction run {run.Id} before creating another correction.");
 }

 private async Task<Result> ReleaseRunAdjustments(string companyId, PayrollRun run, string actor, string reason)
 {
  if(adjustments is null)return Fail(503,"Adjustment storage is unavailable; correction reservations could not be released.");
  var items=(await adjustments.GetAll(x=>x.CompanyId==companyId&&x.CorrectionPayrollRunId==run.Id&&x.Status=="APPROVED")).ToList();
  foreach (var item in items)
  {
   item.CorrectionPayrollRunId = null;
   item.AuditHistory.Add(new PayrollAdjustmentAuditEvent
   {
    Action = "CorrectionRunReservationReleased", ActorId = actor, OccurredAtUtc = DateTime.UtcNow,
    Notes = $"Reservation for cancelled correction run {run.Id} was released: {reason}"
   });
   var released=await adjustments.Update(
    Builders<PayrollAdjustment>.Filter.Eq(x => x.CompanyId, companyId) &
    Builders<PayrollAdjustment>.Filter.Eq(x => x.Id, item.Id) &
    Builders<PayrollAdjustment>.Filter.Eq(x => x.Status, "APPROVED") &
    Builders<PayrollAdjustment>.Filter.Eq(x => x.CorrectionPayrollRunId, run.Id), item);
   if(!released.Success)return Fail(503,$"Correction run was cancelled, but reservation for adjustment {item.Id} could not be released. Retry cancellation to resume cleanup.");
  }
  return new Result{Success=true,Message="Correction adjustment reservations were released.",ResourceId=run.Id};
 }

 private async Task<Result> CancelCorrectionDraft(string companyId, PayrollRun run, string actor, string reason)
 {
  run.Status = "CANCELLED";
  run.IsActive = false;
  run.Version++;
  run.AuditHistory.Add(new PayrollRunAuditEvent
  {
   Action = "CorrectionRunCreationFailed", ActorId = actor, OccurredAtUtc = DateTime.UtcNow, Notes = reason
  });
  var saved = await runs.Update(
   Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId) &
   Builders<PayrollRun>.Filter.Eq(x => x.Id, run.Id) &
   Builders<PayrollRun>.Filter.Eq(x => x.Status, "DRAFT"), run);
  return saved.Success
   ? new Result { Success = true, Message = "Correction draft cancelled.", ResourceId = run.Id }
   : Fail(503, $"Correction run {run.Id} could not be marked cancelled; retry cleanup before proceeding.");
 }

 private async Task<Result> Transition(string c,string id,string actor,string from,string to,string message)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!=from)return Fail(409,$"Payroll run must be {from} before it can become {to}.");
  run.Status=to; run.Version++; run.AuditHistory.Add(new PayrollRunAuditEvent{Action=$"Run{to}",ActorId=actor,OccurredAtUtc=DateTime.UtcNow,Notes=message});
  if(to=="APPROVED"){run.ApprovedBy=actor;run.ApprovedAtUtc=DateTime.UtcNow;} if(to=="PROCESSED")run.ProcessedAtUtc=DateTime.UtcNow;if(to=="CLOSED")run.ClosedAtUtc=DateTime.UtcNow;
  var saved=await runs.Update(Builders<PayrollRun>.Filter.Eq(x=>x.CompanyId,c)&Builders<PayrollRun>.Filter.Eq(x=>x.Id,id)&Builders<PayrollRun>.Filter.Eq(x=>x.Status,from)&Builders<PayrollRun>.Filter.Eq(x=>x.Version,run.Version-1),run);
  return saved.Success?new Result{Success=true,Message=message,ResourceId=id}:Fail(409,"Payroll run changed by another request; reload and retry.");
 }
 private static string? CalculatedRowsError(PayrollRun run,IReadOnlyCollection<PayrollRunEmployee> rows)
 {
  if(rows.Count!=run.EmployeeCount)return "Payroll run employee rows do not match the captured employee count.";
  if(rows.Any(x=>x.Status is not ("CALCULATED" or "SKIPPED")))return "Payroll run has unresolved employee validation errors.";
  var ready=rows.Count(x=>x.Status=="CALCULATED");
  var skipped=rows.Count(x=>x.Status=="SKIPPED");
  if(ready!=run.ReadyCount||skipped!=run.SkippedCount||run.BlockedCount!=0)
   return "Payroll run readiness counts do not match its employee rows.";
  var unexplainedSkipped=rows.Where(x=>x.Status=="SKIPPED"&&string.IsNullOrWhiteSpace(x.ExclusionReason)).ToList();
  if(unexplainedSkipped.Count>0)
   return $"Payroll run contains {unexplainedSkipped.Count} excluded employee(s) without an explicit exclusion reason. Every excluded employee requires an authorized exclusion reason before approval.";
  var calculated=rows.Where(x=>x.Status=="CALCULATED").ToArray();
  var gross=PayrollCalculationRules.RoundMoney(calculated.Sum(x=>x.GrossAmount));
  var deductions=PayrollCalculationRules.RoundMoney(calculated.Sum(x=>x.DeductionAmount));
  var net=PayrollCalculationRules.RoundMoney(calculated.Sum(x=>x.NetAmount));
  if(gross!=run.GrossTotal||deductions!=run.DeductionTotal||net!=run.NetTotal)
   return "Payroll run totals do not reconcile with its calculated employee rows.";
  return null;
 }
 static Result Fail(int code,string message)=>new(){Success=false,StatusCode=code,Message=message};
}
