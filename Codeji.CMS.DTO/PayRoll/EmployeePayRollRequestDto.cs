namespace Codeji.CMS.DTO.PayRoll;

public class EmployeePayRollModel
{
    public string? PayRollId { get; set; }
    public required string EmployeeId { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal? BasicPay { get; set; }
    public float? PaidDays { get; set; }
    public decimal? Bonus { get; set; }
    public decimal? HRA { get; set; }
    public decimal? LTA { get; set; }
    public decimal? OtherAllowance { get; set; }
    public float? LossOfPayDays { get; set; }
    public decimal? LossOfPay { get; set; }
    public decimal? IncomeTax { get; set; }
    public decimal? HealthInsurance { get; set; }
    public decimal? EPF { get; set; }
    public decimal? ESIC { get; set; }
}
public class EmplyeePayRollRequestDto
{
    public DateTime PayMonth { get; set; }
    public List<EmployeePayRollModel> PayData { get; set; } = [];
}