namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentAuditExportQuery
{
    public string? ApplicantId { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Limit { get; set; } = 10000;
}

public sealed class RecruitmentAuditExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public string ContentBase64 { get; set; } = string.Empty;
    public int Count { get; set; }
}
