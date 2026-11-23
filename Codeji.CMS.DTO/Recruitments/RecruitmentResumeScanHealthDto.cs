namespace Codeji.CMS.DTO.Recruitments;

/// <summary>Tenant-scoped operational counts for recruitment resume scanning.</summary>
public sealed class RecruitmentResumeScanHealthDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public bool ScannerConfigured { get; set; }
    public bool? ScannerReachable { get; set; }
    public int ApplicantsWithResume { get; set; }
    public int PendingInitialScans { get; set; }
    public int ScannerUnavailable { get; set; }
    public int InfectedAwaitingQuarantine { get; set; }
    public int StaleCleanScans { get; set; }
}
