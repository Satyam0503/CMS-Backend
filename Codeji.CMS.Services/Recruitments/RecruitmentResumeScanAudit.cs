using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentResumeScanAudit
{
    public static ApplicantLogs Create(string companyId, string applicantId, string jobId, string actorUserId, string attemptId, ResumeScanStatus status)
    {
        var (eventType, description) = status switch
        {
            ResumeScanStatus.Clean => ("ResumeScanPassed", "Resume content passed malware scanning."),
            ResumeScanStatus.Infected => ("ResumeScanRejected", "Resume content was blocked by malware scanning."),
            _ => ("ResumeScanUnavailable", "Resume malware scanning did not complete; resume content was not served.")
        };

        return new ApplicantLogs
        {
            CompanyId = companyId,
            ApplicantId = applicantId,
            ApplicantName = string.Empty,
            JobRole = jobId,
            UserId = actorUserId,
            ActivityCategory = 0,
            EventType = eventType,
            Description = description,
            IdempotencyKey = $"resume-scan:{attemptId}",
            CreatedBy = actorUserId,
            CreatedDate = DateTime.UtcNow,
            UpdatedBy = actorUserId
        };
    }
}
