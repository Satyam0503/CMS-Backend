using Codeji.CMS.DTO.Employee;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class EmpUser : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string UserId { get; set; }
    public string RoleId { get; set; }
    public required string FirstName { get; set; }
    public required string? LastName { get; set; }
    public required string Email { get; set; }
    public string Password { get; set; }
    public string Gender { get; set; }
    public string EmployeeId { get; set; }
    public string JobRole { get; set; }
    public string? DateOfBirth { get; set; }
    public string Department { get; set; }
    public string ReportingManager { get; set; }
    public string PhoneNumber { get; set; }
    public string BloodGroup { get; set; }
    public string PersonalEmail { get; set; }
    public string EmergencyContact { get; set; }
    public string DateOfJoining { get; set; }
    public string? ExitDate { get; set; }
    public int? EmploymentType { get; set; } = null;
    public bool Status { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Address { get; set; }
    public string ProfileUrl { get; set; }
    public string? PanNumber { get; set; }
    public ulong? BankAccountNumber { get; set; } = null;
    public List<EmpUserCustomAttribute> CustomAttributeList { get; set; } = [];
}


