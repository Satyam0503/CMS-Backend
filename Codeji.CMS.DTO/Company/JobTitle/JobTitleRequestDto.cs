namespace Codeji.CMS.DTO.Company.JobTitle;

public class JobTitleRequestDto
{
    public string? JobTitleId { get; set; }
    public string? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public List<MultilingualModel> Titles { get; set; }
}
