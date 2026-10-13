namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollAuditHistoryRequestDto
{
    public required string PayrollId { get; set; }
}

public sealed class PayrollAuditEventDto
{
    public required string Action { get; set; }
    public required string ActorId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
}