namespace Codeji.CMS.Services.Recruitments;

/// <summary>Stable machine-readable codes for Recruitment validation and workflow failures.</summary>
public static class RecruitmentErrorCodes
{
    public const string InvalidRequest = "REC_INVALID_REQUEST";
    public const string JobNotFound = "REC_JOB_NOT_FOUND";
    public const string JobClosed = "REC_JOB_CLOSED";
    public const string ExternalApplication = "REC_EXTERNAL_APPLICATION";
    public const string DuplicateApplication = "REC_DUPLICATE_APPLICATION";
    public const string InvalidTransition = "REC_INVALID_TRANSITION";
    public const string ConcurrencyConflict = "REC_CONCURRENCY_CONFLICT";
    public const string IdempotencyConflict = "REC_IDEMPOTENCY_CONFLICT";
    public const string UploadTokenInvalid = "REC_UPLOAD_TOKEN_INVALID";
    public const string InvalidResume = "REC_INVALID_RESUME";
    public const string PermissionDenied = "REC_PERMISSION_DENIED";
    public const string CalendarSyncFailed = "REC_CALENDAR_SYNC_FAILED";
    public const string ApprovalRequired = "REC_APPROVAL_REQUIRED";
    public const string RequisitionNotFound = "REC_REQUISITION_NOT_FOUND";
    public const string HeadcountInvalid = "REC_HEADCOUNT_INVALID";
    public const string ScorecardInvalid = "REC_SCORECARD_INVALID";
    public const string TalentPoolInvalid = "REC_TALENT_POOL_INVALID";
}




