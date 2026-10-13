using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentTalentPoolService(IMongoDbRepository<Applicant> applicants, IMongoDbRepository<RecruitmentTalentPoolMember> members, IMongoDbRepository<ApplicantLogs> applicantLogs, IHttpContextAccessor context) : IRecruitmentTalentPoolService
{
 public async Task<Result<string>> Upsert(RecruitmentTalentPoolCommand command,string actorUserId)
 {
  var result=new Result<string>{Success=false,StatusCode=StatusCodes.Status400BadRequest}; var companyId=CurrentContext.CompanyId(context); var key=command.IdempotencyKey?.Trim(); var poolName=command.PoolName?.Trim(); var status=string.IsNullOrWhiteSpace(command.Status)?RecruitmentTalentPoolStatus.Active:command.Status.Trim();
  if(string.IsNullOrWhiteSpace(companyId)||string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(command.ApplicantId)||string.IsNullOrWhiteSpace(poolName)) return Fail(result,RecruitmentErrorCodes.InvalidRequest,"Company, applicant, pool name, and idempotency key are required.");
  if(poolName.Length>120||status is not (RecruitmentTalentPoolStatus.Active or RecruitmentTalentPoolStatus.Archived)) return Fail(result,RecruitmentErrorCodes.TalentPoolInvalid,"Pool name or status is invalid.");
  var tags=(command.Tags??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
  var applicant=await applicants.FirstOrDefault(x=>x.CompanyId==companyId&&x.ApplicantId==command.ApplicantId.Trim()&&!x.IsDeleted); if(applicant is null) return Fail(result,RecruitmentErrorCodes.InvalidRequest,"Applicant was not found in the current company.");
  var existing=string.IsNullOrWhiteSpace(command.MemberId)?await members.FirstOrDefault(x=>x.CompanyId==companyId&&x.ApplicantId==applicant.ApplicantId&&x.PoolName==poolName&&!x.IsDeleted):await members.FirstOrDefault(x=>x.CompanyId==companyId&&x.MemberId==command.MemberId&&!x.IsDeleted);
  if(existing is not null&&string.Equals(existing.LastCommandIdempotencyKey,key,StringComparison.Ordinal)) return Ok(result,existing.MemberId,"Talent-pool command already applied.");
  if(existing is null){var entity=new RecruitmentTalentPoolMember{CompanyId=companyId,ApplicantId=applicant.ApplicantId,PoolName=poolName,Tags=tags,Notes=command.Notes?.Trim(),Status=status,LastCommandIdempotencyKey=key,CreatedBy=actorUserId,UpdatedBy=actorUserId}; var saved=await members.AddOne(entity); if(!saved.Success)return Fail(result,RecruitmentErrorCodes.ConcurrencyConflict,"Talent-pool member could not be saved.",StatusCodes.Status409Conflict); await WriteLog(applicant,companyId,actorUserId,$"Talent pool '{poolName}' member created.",null,status); return Ok(result,entity.MemberId,"Talent-pool member saved.");}
  if(!string.Equals(existing.PoolName,poolName,StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(command.MemberId)) return Fail(result,RecruitmentErrorCodes.TalentPoolInvalid,"A member cannot be moved between pools by update.");
  var version=existing.Version<=0?1:existing.Version; if(command.ExpectedVersion.HasValue&&command.ExpectedVersion.Value!=version)return Fail(result,RecruitmentErrorCodes.ConcurrencyConflict,"Talent-pool member was changed by another user.",StatusCodes.Status409Conflict); var previousStatus=existing.Status;
  existing.Tags=tags; existing.Notes=command.Notes?.Trim(); existing.Status=status; existing.LastCommandIdempotencyKey=key; existing.Version=version+1; existing.UpdatedBy=actorUserId; existing.UpdatedDate=DateTime.UtcNow; var updated=await members.Update(Builders<RecruitmentTalentPoolMember>.Filter.Where(x=>x.CompanyId==companyId&&x.MemberId==existing.MemberId&&(x.Version==version||x.Version==0)),existing); if(!updated.Success)return Fail(result,RecruitmentErrorCodes.ConcurrencyConflict,"Talent-pool member was changed by another user.",StatusCodes.Status409Conflict); await WriteLog(applicant,companyId,actorUserId,$"Talent pool '{existing.PoolName}' member updated.",previousStatus,status); return Ok(result,existing.MemberId,"Talent-pool member updated.");
 }
 private async Task WriteLog(Applicant a,string c,string actor,string d,string? p,string cur)=>await applicantLogs.AddOne(new ApplicantLogs{CompanyId=c,ApplicantId=a.ApplicantId,ApplicantName=$"{a.FirstName} {a.LastName}".Trim(),JobRole=a.VacancyId,UserId=actor,ActivityCategory=0,Description=d,EventType="TalentPoolUpdated",PreviousStage=p,CurrentStage=cur,CreatedBy=actor,UpdatedBy=actor});
 private static Result<string> Ok(Result<string> r,string id,string m){r.Success=true;r.StatusCode=StatusCodes.Status200OK;r.MethodResult=id;r.Message=m;return r;} private static Result<string> Fail(Result<string> r,string c,string m,int s=StatusCodes.Status400BadRequest){r.ErrorCode=c;r.StatusCode=s;r.Message=m;return r;}
}
