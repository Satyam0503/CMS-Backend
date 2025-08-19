namespace Codeji.CMS.DTO.Company.Department;

public class DepartmentRequestDto
{
    public string? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public List<MultilingualModel> Titles { get; set; }
}
