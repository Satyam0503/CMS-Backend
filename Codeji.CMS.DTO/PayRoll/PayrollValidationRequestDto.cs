namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollValidationRequestDto
{
    public DateTime PayMonth { get; set; }
    public List<EmployeePayRollModel> PayData { get; set; } = [];
}
