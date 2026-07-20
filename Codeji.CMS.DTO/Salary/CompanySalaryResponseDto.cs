namespace Codeji.CMS.DTO.Salary;

public class CompanySalaryResponseDto
{
    public required string UserId { get; set; }
    public required string EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public Dictionary<string, string>? JobTitle { get; set; }
    public SalaryResponseDto? ActiveSalary { get; set; }
}
