namespace Codeji.CMS.DTO.PayRoll;

public class ProcessPayrollRequestDto
{
    public required DateTime PayMonth { get; set; }
    public List<string>? EmployeeIds { get; set; }
}
