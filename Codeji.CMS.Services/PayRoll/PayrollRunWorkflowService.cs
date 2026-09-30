using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;
namespace Codeji.CMS.Services.PayRoll;
public interface IPayrollRunWorkflowService
{
 Task<Result> Validate(string companyId,string runId,string actorId);
 Task<Result> Approve(string companyId,string runId,string actorId);
 Task<Result> Process(string companyId,string runId,string actorId);
 Task<Result> Close(string companyId,string runId,string actorId);
}
public sealed class PayrollRunWorkflowService(IMongoDbRepository<PayrollRun> runs,IMongoDbRepository<PayrollRunEmployee> employees):IPayrollRunWorkflowService
{
 public Task<Result> Validate(string c,string id,string actor)=>Transition(c,id,actor,"DRAFT","CALCULATED","Payroll run validated.");
 public async Task<Result> Approve(string c,string id,string actor)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);
  if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status!="CALCULATED")return Fail(409,"Only a calculated payroll run can be approved."); if(string.IsNullOrWhiteSpace(run.ReviewedBy)||run.ReviewedAtUtc is null)return Fail(409,"Payroll run must be reviewed before approval.");
  if(!string.IsNullOrWhiteSpace(run.CreatedBy)&&string.Equals(run.CreatedBy,actor,StringComparison.OrdinalIgnoreCase))return Fail(403,"The payroll run creator cannot approve their own run.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList();
  if(rows.Count!=run.EmployeeCount||rows.Any(x=>x.Status=="BLOCKED"))return Fail(409,"Payroll run has unresolved employee validation errors.");
  return await Transition(c,id,actor,"CALCULATED","APPROVED","Payroll run approved and inputs frozen.");
 }
 public Task<Result> Process(string c,string id,string actor)=>Transition(c,id,actor,"APPROVED","PROCESSED","Payroll run processed.");
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
 static Result Fail(int code,string message)=>new(){Success=false,StatusCode=code,Message=message};
}
