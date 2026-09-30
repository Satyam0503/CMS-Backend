using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;
namespace Codeji.CMS.Services.PayRoll;
public sealed class PayrollRunReviewDto
{
 public string RunId {get;set;}=string.Empty; public string Status {get;set;}=string.Empty; public int EmployeeCount{get;set;} public int CalculatedCount{get;set;} public int BlockedCount{get;set;} public int SkippedCount{get;set;} public decimal GrossTotal{get;set;} public decimal DeductionTotal{get;set;} public decimal NetTotal{get;set;} public bool CanApprove{get;set;} public List<string> BlockingEmployeeIds{get;set;}=[];
}
public sealed class PayrollRunReviewRequestDto { public string? Comment{get;set;} }
public interface IPayrollRunReviewService
{
 Task<Result<PayrollRunReviewDto>> Get(string companyId,string runId);
 Task<Result> Record(string companyId,string runId,string actorId,PayrollRunReviewRequestDto request);
}
public sealed class PayrollRunReviewService(IMongoDbRepository<PayrollRun> runs,IMongoDbRepository<PayrollRunEmployee> employees):IPayrollRunReviewService
{
 public async Task<Result<PayrollRunReviewDto>> Get(string c,string id)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id); if(run is null)return Fail<PayrollRunReviewDto>(404,"Payroll run was not found.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList(); var blocked=rows.Where(x=>x.Status=="BLOCKED").Select(x=>x.EmployeeId).ToList();
  return new Result<PayrollRunReviewDto>{Success=true,MethodResult=new PayrollRunReviewDto{RunId=id,Status=run.Status,EmployeeCount=rows.Count,CalculatedCount=rows.Count(x=>x.Status=="CALCULATED"),BlockedCount=blocked.Count,SkippedCount=rows.Count(x=>x.Status=="SKIPPED"),GrossTotal=run.GrossTotal,DeductionTotal=run.DeductionTotal,NetTotal=run.NetTotal,BlockingEmployeeIds=blocked,CanApprove=run.Status=="CALCULATED"&&blocked.Count==0&&rows.Count==run.EmployeeCount}};
 }
 public async Task<Result> Record(string c,string id,string actor,PayrollRunReviewRequestDto request)
 {
  var run=await runs.FirstOrDefault(x=>x.CompanyId==c&&x.Id==id);if(run is null)return Fail(404,"Payroll run was not found.");
  if(run.Status is not ("CALCULATED" or "BLOCKED"))return Fail(409,"Review can only be recorded for a calculated or blocked run.");
  var rows=(await employees.GetAll(x=>x.CompanyId==c&&x.PayrollRunId==id)).ToList();if(rows.Any(x=>x.Status=="BLOCKED"))return Fail(409,"Resolve blocked employees before review approval.");
  run.ReviewedBy=actor;run.ReviewedAtUtc=DateTime.UtcNow;run.ReviewComment=request.Comment?.Trim();run.AuditHistory.Add(new PayrollRunAuditEvent{Action="RunReviewed",ActorId=actor,OccurredAtUtc=DateTime.UtcNow,Notes=run.ReviewComment});return await runs.Update(Builders<PayrollRun>.Filter.Eq(x=>x.CompanyId,c)&Builders<PayrollRun>.Filter.Eq(x=>x.Id,id),run);
 }
 static Result Fail(int code,string message)=>new(){Success=false,StatusCode=code,Message=message};
 static Result<T> Fail<T>(int code,string message)=>new(){Success=false,StatusCode=code,Message=message};
}
