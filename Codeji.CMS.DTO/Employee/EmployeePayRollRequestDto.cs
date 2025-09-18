namespace Codeji.CMS.DTO.Employee;

public class EmployeePayRollModel
{
    public required string EmployeeId { get; set; }
    public required decimal BasicPay { get; set; }
    public required float PaidDays { get; set; }
    public decimal Bonus { get; set; }
    public decimal HRA { get; set; }
    public decimal LTA { get; set; }
    public decimal OtherAllowance { get; set; }
    public float LossOfPayDays { get; set; }
    public decimal LossOfPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal HealthInsurance { get; set; }
}
public class EmplyeePayRollRequestDto
{
    public DateTime PayMonth { get; set; }
    public List<EmployeePayRollModel> PayData { get; set; } = [];
}