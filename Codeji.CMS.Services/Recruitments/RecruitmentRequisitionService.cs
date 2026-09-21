using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentRequisitionService(
    IMongoDbRepository<JobVacancy> vacancies,
    IMongoDbRepository<RecruitmentRequisition> requisitions,
    IHttpContextAccessor context) : IRecruitmentRequisitionService
{
    public async Task<Result<string>> Upsert(RecruitmentRequisitionCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.VacancyId) || string.IsNullOrWhiteSpace(command.PositionTitle))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company, vacancy, title, and idempotency key are required.");
        if (command.Headcount <= 0 || (command.ApprovedHeadcount.HasValue && (command.ApprovedHeadcount.Value < 0 || command.ApprovedHeadcount.Value > command.Headcount)))
            return Fail(result, RecruitmentErrorCodes.HeadcountInvalid, "Headcount must be positive and approved headcount cannot exceed requested headcount.");
        var vacancy = await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId && !x.IsDeleted);
        if (vacancy is null) return Fail(result, RecruitmentErrorCodes.JobNotFound, "The vacancy was not found in the current company.");
        var existing = string.IsNullOrWhiteSpace(command.RequisitionId)
            ? await requisitions.FirstOrDefault(x => x.CompanyId == companyId && x.LastCommandIdempotencyKey == key && !x.IsDeleted)
            : await requisitions.FirstOrDefault(x => x.CompanyId == companyId && x.RequisitionId == command.RequisitionId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, key, StringComparison.Ordinal))
            return Ok(result, existing.RequisitionId, "Requisition command already applied.");
        var requestedStatus = string.IsNullOrWhiteSpace(command.Status) ? RecruitmentRequisitionStatus.Draft : command.Status.Trim();
        if (existing is null)
        {
            if (requestedStatus is not (RecruitmentRequisitionStatus.Draft or RecruitmentRequisitionStatus.PendingApproval))
                return Fail(result, RecruitmentErrorCodes.InvalidTransition, "A new requisition must start in Draft or PendingApproval.", StatusCodes.Status409Conflict);
            var entity = new RecruitmentRequisition { CompanyId = companyId, VacancyId = vacancy.JobId, RequestedByUserId = actorUserId, PositionTitle = command.PositionTitle.Trim(), DepartmentId = command.DepartmentId?.Trim(), Location = command.Location?.Trim(), Headcount = command.Headcount, ApprovedHeadcount = 0, Status = requestedStatus, LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var saved = await requisitions.AddOne(entity);
            return saved.Success ? Ok(result, entity.RequisitionId, "Requisition created.") : Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Requisition could not be created.", StatusCodes.Status409Conflict);
        }
        if (!IsAllowedTransition(existing.Status, requestedStatus)) return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Requisition status transition is not allowed.", StatusCodes.Status409Conflict);
        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Requisition was changed by another user.", StatusCodes.Status409Conflict);
        if (requestedStatus == RecruitmentRequisitionStatus.Approved && string.IsNullOrWhiteSpace(command.ApprovalReference) && string.IsNullOrWhiteSpace(existing.ApprovalReference)) return Fail(result, RecruitmentErrorCodes.ApprovalRequired, "A shared approval reference is required before headcount can be approved.", StatusCodes.Status409Conflict);
        existing.PositionTitle = command.PositionTitle.Trim(); existing.DepartmentId = command.DepartmentId?.Trim(); existing.Location = command.Location?.Trim(); existing.Headcount = command.Headcount; existing.ApprovedHeadcount = requestedStatus == RecruitmentRequisitionStatus.Approved ? command.ApprovedHeadcount ?? command.Headcount : existing.ApprovedHeadcount; existing.Status = requestedStatus; existing.ApprovalReference = string.IsNullOrWhiteSpace(command.ApprovalReference) ? existing.ApprovalReference : command.ApprovalReference.Trim(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var updated = await requisitions.Update(Builders<RecruitmentRequisition>.Filter.Where(x => x.CompanyId == companyId && x.RequisitionId == existing.RequisitionId && (x.Version == version || x.Version == 0)), existing);
        return updated.Success ? Ok(result, existing.RequisitionId, "Requisition updated.") : Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Requisition was changed by another user.", StatusCodes.Status409Conflict);
    }

    private static bool IsAllowedTransition(string current, string next) => current == next || (current, next) switch { (RecruitmentRequisitionStatus.Draft, RecruitmentRequisitionStatus.PendingApproval) => true, (RecruitmentRequisitionStatus.PendingApproval, RecruitmentRequisitionStatus.Approved or RecruitmentRequisitionStatus.Rejected) => true, (RecruitmentRequisitionStatus.Approved, RecruitmentRequisitionStatus.Closed) => true, _ => false };
    private static Result<string> Ok(Result<string> result, string id, string message) { result.Success = true; result.StatusCode = StatusCodes.Status200OK; result.MethodResult = id; result.Message = message; return result; }
    private static Result<string> Fail(Result<string> result, string code, string message, int status = StatusCodes.Status400BadRequest) { result.ErrorCode = code; result.StatusCode = status; result.Message = message; return result; }
}