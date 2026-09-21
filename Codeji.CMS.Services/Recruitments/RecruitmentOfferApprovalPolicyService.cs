using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentOfferApprovalPolicyService(IMongoDbRepository<RecruitmentOfferApprovalPolicy> policies, IHttpContextAccessor context) : IRecruitmentOfferApprovalPolicyService
{
    public async Task<Result<RecruitmentOfferApprovalPolicy>> Get(string? vacancyId)
    {
        var company = CurrentContext.CompanyId(context); if (string.IsNullOrWhiteSpace(company)) return Fail<RecruitmentOfferApprovalPolicy>(400, "REC_INVALID_REQUEST", "Company context is required.");
        RecruitmentOfferApprovalPolicy? policy = null;
        if (!string.IsNullOrWhiteSpace(vacancyId)) policy = await policies.FirstOrDefault(x => x.CompanyId == company && x.VacancyId == vacancyId.Trim() && !x.IsDeleted);
        policy ??= await policies.FirstOrDefault(x => x.CompanyId == company && string.IsNullOrEmpty(x.VacancyId) && !x.IsDeleted);
        if (policy is null) return new Result<RecruitmentOfferApprovalPolicy> { Success = true, StatusCode = 200, Message = "No custom approval policy configured." };
        return new Result<RecruitmentOfferApprovalPolicy> { Success = true, StatusCode = 200, MethodResult = policy, Data = policy, Message = "Offer approval policy loaded." };
    }
    public async Task<Result<string>> Upsert(RecruitmentOfferApprovalPolicyCommand command, string actorUserId)
    {
        var company = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim(); if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.Currency)) return Fail<string>(400, "REC_INVALID_REQUEST", "Company, currency, and idempotency key are required.");
        var currency = command.Currency.Trim().ToUpperInvariant(); if (currency.Length != 3 || command.ApprovalThreshold < 0) return Fail<string>(400, "REC_INVALID_REQUEST", "Currency and threshold are invalid.");
        var existing = string.IsNullOrWhiteSpace(command.PolicyId) ? await policies.FirstOrDefault(x => x.CompanyId == company && x.VacancyId == command.VacancyId && !x.IsDeleted) : await policies.FirstOrDefault(x => x.CompanyId == company && x.PolicyId == command.PolicyId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key) return Ok(existing.PolicyId, "Approval policy command already applied.");
        if (existing is null)
        {
            var policy = new RecruitmentOfferApprovalPolicy { CompanyId = company, VacancyId = command.VacancyId?.Trim(), LegalEntityId = command.LegalEntityId?.Trim(), DepartmentId = command.DepartmentId?.Trim(), Grade = command.Grade?.Trim(), Currency = currency, ApprovalThreshold = command.ApprovalThreshold, RequireApprovalReference = command.RequireApprovalReference, ApproverRoleId = command.ApproverRoleId?.Trim(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var add = await policies.AddOne(policy); return add.Success ? Ok(policy.PolicyId, "Offer approval policy saved.") : Fail<string>(409, "REC_CONCURRENCY_CONFLICT", "Offer approval policy could not be saved.");
        }
        var version = existing.Version <= 0 ? 1 : existing.Version; if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail<string>(409, "REC_CONCURRENCY_CONFLICT", "Approval policy was changed by another user.");
        existing.VacancyId = command.VacancyId?.Trim(); existing.LegalEntityId = command.LegalEntityId?.Trim(); existing.DepartmentId = command.DepartmentId?.Trim(); existing.Grade = command.Grade?.Trim(); existing.Currency = currency; existing.ApprovalThreshold = command.ApprovalThreshold; existing.RequireApprovalReference = command.RequireApprovalReference; existing.ApproverRoleId = command.ApproverRoleId?.Trim(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var update = await policies.Update(Builders<RecruitmentOfferApprovalPolicy>.Filter.Where(x => x.CompanyId == company && x.PolicyId == existing.PolicyId && (x.Version == version || x.Version == 0)), existing); return update.Success ? Ok(existing.PolicyId, "Offer approval policy updated.") : Fail<string>(409, "REC_CONCURRENCY_CONFLICT", "Approval policy was changed by another user.");
    }
    private static Result<T> Fail<T>(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
}

