using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;
namespace Codeji.CMS.Services.PayRoll;
public interface IPayrollRunWorkflowService
{
 Task<Result> Validate(string companyId,string runId,string actorId);
 Task<Result> Cancel(string companyId,string runId,string actorId,string reason);
 Task<Result> Approve(string companyId,string runId,string actorId);
 Task<Result> Process(string companyId,string runId,string actorId);
 Task<Result> Close(string companyId,string runId,string actorId);
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
 public async Task<Result> Approve(string c,string id,string actor)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!="CALCULATED")return Fail(409,"Only a calculated payroll run can be approved."); if(string.IsNullOrWhiteSpace(run.ReviewedBy)||run.ReviewedAtUtc is null)return Fail(409,"Payroll run must be reviewed before approval.");
  if(!string.IsNullOrWhiteSpace(run.CreatedBy)&&string.Equals(run.CreatedBy,actor,StringComparison.OrdinalIgnoreCase))return Fail(403,"The payroll run creator cannot approve their own run.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList();
  var rowError=CalculatedRowsError(run,rows);
  if(rowError is not null)return Fail(409,rowError);
  return await Transition(c,id,actor,"CALCULATED","APPROVED","Payroll run approved and inputs frozen.");
 }
 public async Task<Result> Process(string c,string id,string actor)
 {
  var transition=await Transition(c,id,actor,"APPROVED","PROCESSED","Payroll run processed.");
  if(!transition.Success) return transition;
  if(payrolls is not null)
  {
   try
   {
    var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
    if(run is not null)
    {
     var runRows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id&&x.Status=="CALCULATED")).ToList();
     var targetEmployeeIds=runRows.Select(x=>x.EmployeeId).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().ToList();
     var filter=Builders<EmpPayRoll>.Filter.Eq(p=>p.CompanyId,c)&Builders<EmpPayRoll>.Filter.Eq(p=>p.PayMonth,run.PeriodStart)&Builders<EmpPayRoll>.Filter.Ne(p=>p.IsProcessed,true);
     if(targetEmployeeIds.Count>0)
     {
      filter&=Builders<EmpPayRoll>.Filter.In(p=>p.EmployeeId,targetEmployeeIds);
     }
     var update=Builders<EmpPayRoll>.Update.Set(p=>p.IsProcessed,true).Set(p=>p.WorkflowStatus,"PROCESSED").Set(p=>p.ProcessedAt,DateTime.UtcNow).Set(p=>p.ProcessedBy,actor).Push(p=>p.AuditHistory,new PayrollAuditEvent{Action="PayrollProcessedViaRun",ActorId=actor,OccurredAtUtc=DateTime.UtcNow,Notes=$"Synchronized from Payroll Run {id}"});
     await payrolls.UpdateMany(filter,update);
    }
   }
   catch{}
  }
  return transition;
 }
 public Task<Result> Close(string c,string id,string actor)=>Transition(c,id,actor,"PAID","CLOSED","Payroll run closed.");
 private async Task<Result> Transition(string c,string id,string actor,string from,string to,string message)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!=from)return Fail(409,$"Payroll run must be {from} before it can become {to}.");
  run.Status=to; run.Version++; run.AuditHistory.Add(new PayrollRunAuditEvent{Action=$"Run{to}",ActorId=actor,OccurredAtUtc=DateTime.UtcNow});
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
