namespace Codeji.CMS.DTO.Company.JobTitle;

public class JobTitleResponseDto
{
    public string? JobTitleId { get; set; }
    public string? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Titles { get; set; }
}
