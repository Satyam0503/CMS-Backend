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
    public string? DateOfBirth { get; set; }
    public string? Department { get; set; }
    public List<MultilingualModel> DepartmentName { get; set; } = [];
    public string? ReportingManager { get; set; }
    public string? ReportingManagerName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BloodGroup { get; set; }
    public string? PersonalEmail { get; set; }
    public string? EmergencyContact { get; set; }
    public string? DateOfJoining { get; set; }
    public bool? Status { get; set; }
    public bool? IsEmailVerified { get; set; }
    public string? Address { get; set; }
    public string? ProfileUrl { get; set; }
    public string? FullProfileUrl { get; set; }
}
