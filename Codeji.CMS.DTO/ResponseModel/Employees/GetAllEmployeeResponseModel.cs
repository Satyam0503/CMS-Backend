using Codeji.CMS.Utility;

namespace Codeji.CMS.DTO.ResponseModel;

public class GetAllEmployeeResponseModel
{
    public string UserId { get; set; }

    public string FullName { get; set; }
    public string Email { get; set; }
    public string? EmployeeId { get; set; }
    public string? JobRole { get; set; }
    public Dictionary<string, string>? Department { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DateOfBirth { get; set; }
    public string? FullProfileUrl { get; set; }
    public string Gender { get; set; }
}