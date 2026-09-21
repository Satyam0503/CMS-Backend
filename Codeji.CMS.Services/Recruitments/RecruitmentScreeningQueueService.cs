using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentScreeningQueueService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentScreeningReview> reviews,
    IHttpContextAccessor context) : IRecruitmentScreeningQueueService
{
    public async Task<Result<RecruitmentScreeningQueueReport>> Get(string? vacancyId, int overdueAfterHours = 48)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return Fail("Company context is required.");
        if (overdueAfterHours is < 1 or > 720) return Fail("overdueAfterHours must be between 1 and 720.");
        var applicantsInScope = (await applicants.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (string.IsNullOrWhiteSpace(vacancyId) || x.VacancyId == vacancyId) && x.ActivityType != ActivityType.Rejected && x.ActivityType != ActivityType.Selected)).ToList();
        var reviewsInScope = (await reviews.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (string.IsNullOrWhiteSpace(vacancyId) || x.VacancyId == vacancyId))).ToList();
        var now = DateTime.UtcNow;
        var items = applicantsInScope.Select(applicant =>
        {
            var review = reviewsInScope.Where(x => x.ApplicantId == applicant.ApplicantId).OrderByDescending(x => x.UpdatedDate).ThenByDescending(x => x.CreatedDate).FirstOrDefault();
            var submitted = review?.UpdatedDate ?? applicant.CreatedDate ?? now;
            var age = Math.Max(0, (now - submitted).TotalHours);
            return new RecruitmentScreeningQueueItem { ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), Email = applicant.Email, ReviewId = review?.ReviewId ?? string.Empty, Decision = review?.Decision ?? RecruitmentScreeningDecision.Pending, Score = review?.Score ?? 0, ReviewerUserId = review?.ReviewerUserId, RejectionReason = review?.RejectionReason, SubmittedAtUtc = submitted, AgeHours = age, Overdue = age >= overdueAfterHours && (review is null || review.Decision is RecruitmentScreeningDecision.Pending or RecruitmentScreeningDecision.Hold) };
        }).Where(x => x.Decision is RecruitmentScreeningDecision.Pending or RecruitmentScreeningDecision.Hold).OrderByDescending(x => x.Overdue).ThenByDescending(x => x.AgeHours).ToList();
        var report = new RecruitmentScreeningQueueReport { TotalPending = items.Count, OverdueCount = items.Count(x => x.Overdue), Items = items };
        return new Result<RecruitmentScreeningQueueReport> { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = report, Data = report, TotalRecords = report.TotalPending, Message = "Screening queue loaded." };
    }
    private static Result<RecruitmentScreeningQueueReport> Fail(string message) => new() { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = message };
}

