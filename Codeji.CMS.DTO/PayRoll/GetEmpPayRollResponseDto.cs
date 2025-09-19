namespace Codeji.CMS.DTO.PayRoll;

public class GetEmpPayRollResponseDto
{
    public required string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public Dictionary<string, string>? JobTitle { get; set; }
    public EmployeePayRollModel PayData { get; set; }
}
