using Codeji.CMS.Utility;

namespace Codeji.CMS.DTO.ResponseModel;

public class EmployeeSearchResponseDTO
{
    public string UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmpId { get; set; }
    public string? JobRole { get; set; }
    public string? ProfileUrl { get; set; }
    public string? FullProfileUrl
    {
        get
        {
            return Common.GetEmployeeImageUrl(ProfileUrl);
        }
    }
}