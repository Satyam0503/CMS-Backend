namespace Codeji.CMS.DTO.RequestModels.EmployeeData;

public class EmployeeWorkHistoryModel
{
    public string? WorkHistoryId { get; set; }
    public string? UserId { get; set; }
    public string OrganisationName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required string JobRole { get; set; }
    public string? JobLocation { get; set; }
}