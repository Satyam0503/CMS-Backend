using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Codeji.CMS.Repository.Entities.Employees;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentHandoffService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentHandoff> handoffs,
    IMongoDbRepository<ApplicantLogs> applicantLogs,
    IEmployeeService employeeService,
    IHttpContextAccessor httpContextAccessor,
    IMongoDbRepository<RecruitmentPreboardingCase>? preboardingCases = null,
    IMongoDbRepository<RecruitmentOnboardingCase>? onboardingCases = null,
    IMongoDbRepository<RecruitmentOffer>? offers = null,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IEmployeeSalaryStructureService? salaries = null,
    IMongoDbRepository<EmpUser>? employees = null,
    IMongoDbRepository<RecruitmentScorecard>? scorecards = null,
    IMongoDbRepository<RecruitmentInterview>? interviews = null,
    IMongoDbRepository<JobVacancy>? vacancies = null) : IRecruitmentHandoffService
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

        var prior = await handoffs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == request.ApplicantId && !x.IsDeleted);
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

        if (applicant.ConsentStatus == "Anonymized")
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidTransition;
            result.StatusCode = StatusCodes.Status409Conflict;
            result.Message = "Anonymized candidate records cannot be used to create an employee. Reopen the hiring process with the candidate's current application and required privacy notice.";
            return result;
        }

        var canonicalApplication = applications is null
            ? null
            : await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
        var isSelected = canonicalApplication is not null
            ? string.Equals(canonicalApplication.CurrentStage, nameof(ActivityType.Selected), StringComparison.OrdinalIgnoreCase)
            : applicant.ActivityType == ActivityType.Selected;
        if (!isSelected && offers is not null)
        {
            var accepted = await offers.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.Status == RecruitmentOfferStatus.Accepted && !x.IsDeleted);
            if (accepted is not null)
            {
                isSelected = true;
                applicant.ActivityType = ActivityType.Selected;
                applicant.UpdatedBy = actorUserId;
                applicant.UpdatedDate = DateTime.UtcNow;
                await applicants.Update(MongoDB.Driver.Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);
                if (canonicalApplication is not null)
                {
                    canonicalApplication.CurrentStage = nameof(ActivityType.Selected);
                    await applications!.Update(MongoDB.Driver.Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonicalApplication.ApplicationId), canonicalApplication);
                }
            }
        }
        if (!isSelected)
        {
            result.ErrorCode = RecruitmentErrorCodes.InvalidTransition;
            result.StatusCode = StatusCodes.Status409Conflict;
            result.Message = "Only a selected applicant can be handed off to preboarding.";
            return result;
        }

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
        var vacancy = vacancies is null ? null : await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == applicant.VacancyId && !x.IsDeleted);
        var allowCustomWorkflows = config is null || config.AllowJobWorkflowCustomization;
        var isDirectVacancy = allowCustomWorkflows && string.Equals(vacancy?.WorkflowMode, "Direct", StringComparison.OrdinalIgnoreCase);
        var requirePreboarding = RecruitmentPreboardingPolicy.Requires(config, vacancy);
        RecruitmentOffer? acceptedOffer = null;

        var requireCandidateOfferAcceptance = offers is not null
            && !isDirectVacancy
            && !string.Equals(config?.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase);
        if (offers is not null)
        {
            acceptedOffer = await offers.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.Status == RecruitmentOfferStatus.Accepted && !x.IsDeleted);
            if (acceptedOffer is null && requireCandidateOfferAcceptance)
            {
                result.ErrorCode = RecruitmentErrorCodes.InvalidTransition;
                result.StatusCode = StatusCodes.Status409Conflict;
                result.Message = "An accepted offer is required before preboarding can start.";
                return result;
            }
        }

        RecruitmentPreboardingCase? existingPreboarding = null;
        if (preboardingCases is not null && requirePreboarding)
        {
            existingPreboarding = await preboardingCases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (existingPreboarding is null)
            {
                var checklist = config?.PreboardingChecklist is { Count: > 0 } ? config.PreboardingChecklist : ["candidate-details", "documents", "verification"];
                var created = new RecruitmentPreboardingCase
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    OfferId = acceptedOffer?.OfferId ?? string.Empty,
                    Status = RecruitmentPreboardingStatus.PendingCandidateDetails,
                    OwnerUserId = actorUserId,
                    LastCommandIdempotencyKey = $"handoff-checklist:{request.IdempotencyKey.Trim()}",
                    Tasks = checklist.Select(code => new RecruitmentPreboardingTask { Code = code, Title = code.Replace("-", " "), Status = "Pending" }).ToList(),
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                };
                var added = await preboardingCases.AddOne(created);
                result.ErrorCode = added.Success ? RecruitmentErrorCodes.InvalidTransition : RecruitmentErrorCodes.ConcurrencyConflict;
                result.StatusCode = StatusCodes.Status409Conflict;
                result.Message = added.Success
                    ? "A preboarding checklist was created. Verify each task, save the checklist, and retry the handoff."
                    : "The preboarding checklist could not be created. Please retry.";
                return result;
            }
            if (existingPreboarding.Tasks.Any(x => !RecruitmentPreboardingTaskRules.IsResolved(x)))
            {
                result.ErrorCode = RecruitmentErrorCodes.InvalidTransition;
                result.StatusCode = StatusCodes.Status409Conflict;
                result.Message = "Preboarding mandatory tasks must be complete before employee creation.";
                return result;
            }
        }

        string assignedEmployeeId = request.EmployeeId?.Trim() ?? string.Empty;
        string assignedUserId = string.Empty;

        var invite = await employeeService.InviteNewEmployee(new InviteEmployeeDto
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            EmployeeId = assignedEmployeeId,
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            JobRoleId = request.JobRoleId,
            Gender = request.Gender,
            ReportingManager = request.ReportingManager,
            ScheduleId = request.ScheduleId
        }, actorUserId);

        if (invite is { Success: true, MethodResult: not null } && !string.IsNullOrWhiteSpace(invite.MethodResult.UserId))
        {
            assignedUserId = invite.MethodResult.UserId;
            assignedEmployeeId = !string.IsNullOrWhiteSpace(invite.MethodResult.EmployeeId)
                ? invite.MethodResult.EmployeeId
                : assignedEmployeeId;
        }
        else
        {
            // Defensive recovery: If employee already exists with this email in the company (e.g. from an interrupted prior attempt),
            // recover their identity so the hiring workflow can complete cleanly instead of staying permanently blocked.
            EmpUser? existingEmp = null;
            if (employees is not null)
            {
                existingEmp = await employees.FirstOrDefault(x =>
                    x.CompanyId == companyId &&
                    x.Email.ToLower() == request.Email.Trim().ToLower() &&
                    !x.IsDeleted);
            }

            if (existingEmp is not null)
            {
                assignedUserId = existingEmp.UserId;
                assignedEmployeeId = existingEmp.EmployeeId;
            }
            else
            {
                result.ErrorCode = RecruitmentErrorCodes.InvalidRequest;
                result.StatusCode = invite.StatusCode == 0 ? StatusCodes.Status400BadRequest : invite.StatusCode;
                result.Message = invite.Message;
                return result;
            }
        }

        if (salaries is not null && acceptedOffer is not null)
        {
            var components = new List<EmployeeSalaryComponentValueDto>
            {
                new() { ComponentCode = "BASIC", ComponentName = "Basic Pay", Kind = "EARNING", Value = acceptedOffer.Salary, CalculationType = "FIXED", IsTaxable = true, IsRecurring = true }
            };
            components.AddRange(acceptedOffer.CompensationComponents.Select(item => new EmployeeSalaryComponentValueDto
            {
                ComponentCode = item.Key.Trim().ToUpperInvariant(), ComponentName = item.Key.Trim(), Kind = "EARNING", Value = item.Value, CalculationType = "FIXED", IsTaxable = true, IsRecurring = true
            }));
            var effectiveFrom = (acceptedOffer.StartDateUtc ?? DateTime.UtcNow).Date;
            var salaryDraft = await salaries.SaveDraft(companyId, actorUserId, new EmployeeSalaryStructureDto
            {
                EmployeeId = assignedEmployeeId, UserId = assignedUserId, OfferId = acceptedOffer.OfferId, OfferVersion = acceptedOffer.Version,
                Currency = acceptedOffer.Currency, PaymentFrequency = "MONTHLY", AnnualCtc = acceptedOffer.TotalCtc, MonthlyGross = acceptedOffer.TotalCtc / 12m,
                EffectiveFrom = effectiveFrom, Status = "DRAFT", ApprovalStatus = "DRAFT", Source = "OFFER_HANDOFF", Version = 1, Components = components
            });
            if (!salaryDraft.Success)
            {
                // Note: The employee has already been created. An unconfigured salary template or paygroup must not crash the entire handoff.
                // We record a warning audit log and proceed so the candidate can complete onboarding.
                await applicantLogs.AddOne(new ApplicantLogs
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
                    JobRole = applicant.VacancyId,
                    UserId = actorUserId,
                    ActivityCategory = 0,
                    Description = $"Employee {assignedEmployeeId} created. Payroll salary draft staging warning: {salaryDraft.Message}",
                    EventType = "SalaryStagingWarning",
                    PreviousStage = applicant.ActivityType.ToString(),
                    CurrentStage = "Preboarding",
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                });
            }
        }

        var allScorecards = scorecards is not null
            ? (await scorecards.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted)).ToList()
            : [];
        var allInterviews = interviews is not null
            ? (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted)).ToList()
            : null;
        var finalScoreResult = RecruitmentInterviewScoreCalculator.Compute(allScorecards, allInterviews);

        var hiringScore = finalScoreResult.FinalInterviewScore > 0 ? finalScoreResult.FinalInterviewScore : applicant.FinalInterviewScore;
        var hiringRating = finalScoreResult.FinalInterviewRating > 0 ? finalScoreResult.FinalInterviewRating : applicant.FinalInterviewRating;
        var hiringRec = !string.IsNullOrEmpty(finalScoreResult.FinalInterviewRecommendation) && finalScoreResult.FinalInterviewRecommendation != "Pending"
            ? finalScoreResult.FinalInterviewRecommendation
            : applicant.FinalInterviewRecommendation;
        var evaluatedCount = finalScoreResult.EvaluatedRoundsCount > 0 ? finalScoreResult.EvaluatedRoundsCount : applicant.EvaluatedRoundsCount;

        var handoff = new RecruitmentHandoff
        {
            CompanyId = companyId,
            ApplicantId = applicant.ApplicantId,
            EmployeeUserId = assignedUserId,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            HiringBenchmarkScore = hiringScore,
            HiringBenchmarkRating = hiringRating,
            HiringRecommendation = hiringRec,
            EvaluatedRoundsCount = evaluatedCount,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId
        };
        var saved = await handoffs.AddOne(handoff);
        if (saved.Success)
        {
            if (employees is not null && !string.IsNullOrWhiteSpace(assignedUserId))
            {
                var emp = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == assignedUserId && !x.IsDeleted);
                if (emp is not null)
                {
                    emp.HiringBenchmarkScore = hiringScore;
                    emp.HiringBenchmarkRating = hiringRating;
                    emp.HiringRecommendation = hiringRec;
                    emp.EvaluatedRoundsCount = evaluatedCount;
                    await employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == assignedUserId), emp);
                }
            }
            if (hiringScore.HasValue && hiringScore.Value > 0)
            {
                applicant.FinalInterviewScore = hiringScore;
                applicant.FinalInterviewRating = hiringRating;
                applicant.FinalInterviewRecommendation = hiringRec;
                applicant.EvaluatedRoundsCount = evaluatedCount;
                applicant.FinalScoreCalculatedAtUtc = DateTime.UtcNow;
                await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);
            }
        }
        if (!saved.Success)
        {
            var committed = await handoffs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == request.ApplicantId && !x.IsDeleted);
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
            if (onboarding is null)
            {
                await onboardingCases.AddOne(new RecruitmentOnboardingCase
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    EmployeeUserId = handoff.EmployeeUserId,
                    Status = RecruitmentOnboardingStatus.PendingActivation,
                    LastCommandIdempotencyKey = request.IdempotencyKey.Trim(),
                    Tasks =
                    [
                        new RecruitmentOnboardingTask { Code = "account-activation", Title = "Activate employee account" },
                        new RecruitmentOnboardingTask { Code = "access", Title = "Provision role and system access" },
                        new RecruitmentOnboardingTask { Code = "payroll-setup", Title = "Configure payroll and compensation profile" },
                        new RecruitmentOnboardingTask { Code = "attendance-setup", Title = "Configure attendance, schedule, and work policy" },
                        new RecruitmentOnboardingTask { Code = "leave-setup", Title = "Configure leave policy and balances" },
                        new RecruitmentOnboardingTask { Code = "orientation", Title = "Complete onboarding orientation" }
                    ],
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                });
            }
        }

        if (preboardingCases is not null)
        {
            var preboarding = existingPreboarding ?? await preboardingCases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (preboarding is not null)
            {
                preboarding.Status = RecruitmentPreboardingStatus.ReadyForOnboarding;
                preboarding.OwnerUserId = actorUserId;
                preboarding.UpdatedBy = actorUserId;
                preboarding.UpdatedDate = DateTime.UtcNow;
                await preboardingCases.Update(MongoDB.Driver.Builders<RecruitmentPreboardingCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == preboarding.CaseId), preboarding);
            }
        }

        if (applications is not null)
        {
            var canonical = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (canonical is not null)
            {
                canonical.StageHistory.Add(new RecruitmentApplicationStageEvent
                {
                    FromStage = canonical.CurrentStage,
                    ToStage = "Preboarding",
                    ActorUserId = actorUserId,
                    Reason = $"Candidate handed off to preboarding with Employee ID {assignedEmployeeId}."
                });
                canonical.CurrentStage = "Preboarding";
                canonical.Version = (canonical.Version <= 0 ? 1 : canonical.Version) + 1;
                canonical.UpdatedBy = actorUserId;
                canonical.UpdatedDate = DateTime.UtcNow;
                await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonical.ApplicationId), canonical);
            }
            else if (canonical is null)
            {
                var newCanonical = new RecruitmentJobApplication
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    VacancyId = applicant.VacancyId ?? string.Empty,
                    CurrentStage = "Preboarding",
                    CreatedBy = actorUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = actorUserId,
                    UpdatedDate = DateTime.UtcNow,
                    StageHistory =
                    [
                        new RecruitmentApplicationStageEvent
                        {
                            FromStage = applicant.ActivityType.ToString(),
                            ToStage = "Preboarding",
                            ActorUserId = actorUserId,
                            Reason = $"Candidate handed off to preboarding with Employee ID {assignedEmployeeId}."
                        }
                    ]
                };
                await applications.AddOne(newCanonical);
            }
        }

        await applicantLogs.AddOne(new ApplicantLogs
        {
            CompanyId = companyId,
            ApplicantId = applicant.ApplicantId,
            ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
            JobRole = applicant.VacancyId,
            UserId = actorUserId,
            ActivityCategory = 0,
            Description = $"Candidate handed off to preboarding. Employee ID: {assignedEmployeeId}",
            EventType = "HandoffToPreboarding",
            PreviousStage = applicant.ActivityType.ToString(),
            CurrentStage = "Preboarding",
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId
        });

        // Sibling Application Auto-Cascade: Auto-withdraw any other active applications for this candidate in the same company
        try
        {
            var candidateEmail = (applicant.Email ?? request.Email)?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(candidateEmail))
            {
                var siblings = await applicants.GetAll(x =>
                    x.CompanyId == companyId &&
                    x.ApplicantId != applicant.ApplicantId &&
                    !x.IsDeleted &&
                    x.ActivityType != ActivityType.Selected &&
                    x.ActivityType != ActivityType.Rejected &&
                    x.ActivityType != ActivityType.ReApply);

                foreach (var sibling in siblings.Where(s => string.Equals(s.Email?.Trim(), candidateEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    var prevStage = sibling.ActivityType.ToString();
                    sibling.ActivityType = ActivityType.Rejected;
                    sibling.UpdatedBy = actorUserId;
                    sibling.UpdatedDate = DateTime.UtcNow;
                    await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == sibling.ApplicantId), sibling);

                    if (applications is not null)
                    {
                        var siblingApp = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == sibling.ApplicantId && !x.IsDeleted);
                        if (siblingApp is not null)
                        {
                            siblingApp.StageHistory.Add(new RecruitmentApplicationStageEvent
                            {
                                FromStage = siblingApp.CurrentStage,
                                ToStage = "Withdrawn",
                                ActorUserId = actorUserId,
                                Reason = $"Auto-withdrawn: Candidate accepted offer and was hired for vacancy {applicant.VacancyId} with Employee ID {assignedEmployeeId}."
                            });
                            siblingApp.CurrentStage = "Withdrawn";
                            siblingApp.Version = (siblingApp.Version <= 0 ? 1 : siblingApp.Version) + 1;
                            siblingApp.UpdatedBy = actorUserId;
                            siblingApp.UpdatedDate = DateTime.UtcNow;
                            await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == siblingApp.ApplicationId), siblingApp);
                        }
                    }

                    await applicantLogs.AddOne(new ApplicantLogs
                    {
                        CompanyId = companyId,
                        ApplicantId = sibling.ApplicantId,
                        ApplicantName = $"{sibling.FirstName} {sibling.LastName}".Trim(),
                        JobRole = sibling.VacancyId,
                        UserId = actorUserId,
                        ActivityCategory = 0,
                        Description = $"Application auto-withdrawn: Candidate accepted offer and was hired for vacancy {applicant.VacancyId}.",
                        EventType = "SiblingApplicationAutoWithdrawn",
                        PreviousStage = prevStage,
                        CurrentStage = "Withdrawn",
                        CreatedBy = actorUserId,
                        UpdatedBy = actorUserId
                    });
                }
            }
        }
        catch
        {
            // Defensive isolation: Sibling cascade errors must not roll back or block the primary handoff.
        }

        result.Success = true;
        result.StatusCode = StatusCodes.Status200OK;
        result.MethodResult = handoff.EmployeeUserId;
        result.Message = $"Candidate handed off to preboarding. Employee created with ID: {assignedEmployeeId}";
        return result;
    }
}
