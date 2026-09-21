using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentHandoffService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentHandoff> handoffs,
    IMongoDbRepository<ApplicantLogs> applicantLogs,
    IEmployeeService employeeService,
    IHttpContextAccessor httpContextAccessor,
    IMongoDbRepository<RecruitmentPreboardingCase>? preboardingCases = null,
    IMongoDbRepository<RecruitmentOnboardingCase>? onboardingCases = null) : IRecruitmentHandoffService
{
    public async Task<Result<string>> HandoffToPreboarding(CandidateHandoffRequest request, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidRequest;
            result.Message = "Company and idempotency key are required.";
            return result;
        }

        var prior = await handoffs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == request.ApplicantId && x.IdempotencyKey == request.IdempotencyKey && !x.IsDeleted);
        if (prior is not null)
        {
            result.Success = true;
            result.StatusCode = StatusCodes.Status200OK;
            result.MethodResult = prior.EmployeeUserId;
            result.Message = "Candidate handoff already completed.";
            return result;
        }

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == request.ApplicantId && !x.IsDeleted);
        if (applicant is null)
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidRequest;
            result.Message = "Applicant was not found in the current company.";
            return result;
        }
        if (applicant.ActivityType != ActivityType.Selected)
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidTransition;
            result.StatusCode = StatusCodes.Status409Conflict;
            result.Message = "Only a selected applicant can be handed off to preboarding.";
            return result;
        }

        var invite = await employeeService.InviteNewEmployee(new InviteEmployeeDto
        {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = request.Email.Trim(),
            EmployeeId = request.EmployeeId.Trim(), RoleId = request.RoleId, DepartmentId = request.DepartmentId,
            JobRoleId = request.JobRoleId, Gender = request.Gender, ReportingManager = request.ReportingManager,
            ScheduleId = request.ScheduleId
        }, actorUserId);
        if (!invite.Success || string.IsNullOrWhiteSpace(invite.MethodResult?.UserId))
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidRequest;
            result.StatusCode = invite.StatusCode == 0 ? StatusCodes.Status400BadRequest : invite.StatusCode;
            result.Message = invite.Message;
            return result;
        }

        var handoff = new RecruitmentHandoff
        {
            CompanyId = companyId, ApplicantId = applicant.ApplicantId, EmployeeUserId = invite.MethodResult.UserId!,
            IdempotencyKey = request.IdempotencyKey.Trim(), CreatedBy = actorUserId, UpdatedBy = actorUserId
        };
        var saved = await handoffs.AddOne(handoff);
        if (!saved.Success)
        {
            var committed = await handoffs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == request.ApplicantId && x.IdempotencyKey == request.IdempotencyKey.Trim() && !x.IsDeleted);
            if (committed is not null)
            {
                result.Success = true;
                result.StatusCode = StatusCodes.Status200OK;
                result.MethodResult = committed.EmployeeUserId;
                result.Message = "Candidate handoff already completed.";
                return result;
            }
            result.ErrorCode = RecruitmentErrorCodes.ConcurrencyConflict;
            result.StatusCode = StatusCodes.Status409Conflict;
            result.Message = "Handoff could not be committed safely; retry with the same idempotency key.";
            return result;
        }
        if (onboardingCases is not null)
        {
            var onboarding = await onboardingCases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (onboarding is null) await onboardingCases.AddOne(new RecruitmentOnboardingCase { CompanyId = companyId, ApplicantId = applicant.ApplicantId, EmployeeUserId = handoff.EmployeeUserId, Status = RecruitmentOnboardingStatus.PendingActivation, LastCommandIdempotencyKey = request.IdempotencyKey.Trim(), Tasks = [new RecruitmentOnboardingTask { Code = "account-activation", Title = "Activate employee account" }, new RecruitmentOnboardingTask { Code = "access", Title = "Provision role and system access" }, new RecruitmentOnboardingTask { Code = "payroll-setup", Title = "Configure payroll and compensation profile" }, new RecruitmentOnboardingTask { Code = "attendance-setup", Title = "Configure attendance, schedule, and work policy" }, new RecruitmentOnboardingTask { Code = "leave-setup", Title = "Configure leave policy and balances" }, new RecruitmentOnboardingTask { Code = "orientation", Title = "Complete onboarding orientation" }], CreatedBy = actorUserId, UpdatedBy = actorUserId });
        }
        if (preboardingCases is not null)
        {
            var preboarding = await preboardingCases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (preboarding is not null) { preboarding.Status = RecruitmentPreboardingStatus.ReadyForOnboarding; preboarding.OwnerUserId = actorUserId; preboarding.UpdatedBy = actorUserId; preboarding.UpdatedDate = DateTime.UtcNow; await preboardingCases.Update(MongoDB.Driver.Builders<RecruitmentPreboardingCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == preboarding.CaseId), preboarding); }
        }
        await applicantLogs.AddOne(new ApplicantLogs
        {
            CompanyId = companyId,
            ApplicantId = applicant.ApplicantId,
            ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
            JobRole = applicant.VacancyId,
            UserId = actorUserId,
            ActivityCategory = 0,
            Description = "Candidate handed off to preboarding.",
            EventType = "HandoffToPreboarding",
            PreviousStage = applicant.ActivityType.ToString(),
            CurrentStage = "Preboarding",
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId
        });
        result.Success = true;
        result.StatusCode = StatusCodes.Status200OK;
        result.MethodResult = handoff.EmployeeUserId;
        result.Message = "Candidate handed off to preboarding.";
        return result;
    }
}







