namespace Codeji.CMS.DTO.PayRoll;

public sealed class RecordPayrollPaymentRequestDto
{
    public required string PayrollId { get; set; }
    public required string PaymentMethod { get; set; }
    public required string PaymentReference { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
}
