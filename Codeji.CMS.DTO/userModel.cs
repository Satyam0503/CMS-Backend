using Codeji.CMS.DTO.Employee;

namespace Codeji.CMS.DTO;

public class UserModel
{
    public string UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string? CompanyId { get; set; }
    public string RoleId { get; set; }
    public string Gender { get; set; }
    public string? EmployeeId { get; set; }
    public string? JobRole { get; set; }
    public int? EmploymentType { get; set; }
    public Dictionary<string, string>? JobRoleTitle { get; set; } = null;
    public DateTime? DateOfBirth { get; set; }
    public string? Department { get; set; }
    public Dictionary<string, string>? DepartmentTitle { get; set; }
    public string? ReportingManager { get; set; }
    public string? ReportingManagerName { get; set; } = null;
    public string? PhoneNumber { get; set; }
    public string? BloodGroup { get; set; }
    public string? PersonalEmail { get; set; }
    public string? EmergencyContact { get; set; }
    public string? DateOfJoining { get; set; }
    public bool? Status { get; set; }
    public bool? IsEmailVerified { get; set; }
    public string? Address { get; set; }
    public string? ProfileUrl { get; set; }
    public List<UserCustomAttribute> CustomAttributes { get; set; } = [];
}


public class UserCustomAttribute
{
    public required string CustomAttributeId { get; set; }
    public required string CustomAttributeValueId { get; set; }
    public Dictionary<string, string> CustomAttributeTitle { get; set; }
    public Dictionary<string, string> CustomAttributeValueTitle { get; set; }
}