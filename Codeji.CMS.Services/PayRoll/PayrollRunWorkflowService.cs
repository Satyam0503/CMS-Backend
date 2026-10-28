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
public sealed class PayrollRunWorkflowService(IMongoDbRepository<PayrollRun> runs,IMongoDbRepository<PayrollRunEmployee> employees,IMongoDbRepository<EmpPayRoll>? payrolls=null):IPayrollRunWorkflowService
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
  var previousStatus=run.Status;
  var previousVersion=run.Version;
  if(previousStatus is not ("DRAFT" or "BLOCKED"))
   return Fail(409,"Only a draft or blocked payroll run can be cancelled and recreated. Calculated or later runs must use the controlled correction workflow.");
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
  return saved.Success
   ?new Result{Success=true,Message="Draft payroll run cancelled; its snapshots and audit history were retained. Create a new run to recalculate.",ResourceId=id}
   :Fail(409,"Payroll run changed before cancellation; reload before retrying.");
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
   return new Result{Success=true,Message="Payroll run and payroll rows are already synchronized.",ResourceId=id};
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

   return await MarkLegacyRowsSynchronized(c,run,actor,$"Synchronized {targetEmployeeIds.Count} employee payroll row(s) from Payroll Run {id}.");
  }
  catch(Exception)
  {
   return Fail(503,"Payroll run is processed, but payroll-row synchronization could not be verified. Retry Process to resume safely.");
  }
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
   EmployeeCount = originalMembers.Count,
   ReadyCount = originalMembers.Count(m => m.Status != "SKIPPED"),
   SkippedCount = originalMembers.Count(m => m.Status == "SKIPPED"),
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

  await runs.AddOne(correction);

  foreach (var m in originalMembers)
  {
   var correctionMember = new PayrollRunEmployee
   {
    CompanyId = c,
    PayrollRunId = correction.Id,
    EmployeeId = m.EmployeeId,
    Status = m.Status == "SKIPPED" ? "SKIPPED" : "READY",
    ExclusionReason = m.ExclusionReason,
    InclusionReason = m.InclusionReason,
    InputSnapshot = m.InputSnapshot,
    StatutorySnapshot = m.StatutorySnapshot,
    AttendanceSnapshot = m.AttendanceSnapshot,
    GrossAmount = 0m,
    DeductionAmount = 0m,
    NetAmount = 0m
   };
   await employees.AddOne(correctionMember);
  }

  return new Result { Success = true, StatusCode = 201, Message = "Correction run created in draft with employee members.", ResourceId = correction.Id };
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
