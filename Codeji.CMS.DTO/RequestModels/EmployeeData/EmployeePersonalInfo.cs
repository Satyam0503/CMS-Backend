using System.ComponentModel.DataAnnotations;
using Codeji.CMS.DTO.Employee;

namespace Codeji.CMS.DTO.RequestModels.EmployeeData
{
    public class EmployeePersonalInfo
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Gender { get; set; }
        public required string RoleId { get; set; }
        public string? DateOfBirth { get; set; }
        public string? DateOfJoining { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Department { get; set; }
        public string? BloodGroup { get; set; }
        public string? JobRole { get; set; }
        public string? PersonalEmail { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Address { get; set; }
        public string? ReportingManager { get; set; }
        public int? EmploymentType { get; set; }
        public List<EmpUserCustomAttribute> CustomAttributeList { get; set; } = [];

        [RegularExpression(@"[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid Pan Number")]
        public string? PanNumber { get; set; }

        [RegularExpression(@"^[0-9]{9,18}$", ErrorMessage = "Invalid Account Number")]
        public ulong? BankAccountNumber { get; set; }

    }
}
