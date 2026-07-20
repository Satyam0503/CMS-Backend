namespace Codeji.CMS.DTO.RequestModels.EmployeeData
{
    // Fields an employee may update on their own profile without the Employees module's
    // Edit permission (which governs managing *other* employees). Deliberately excludes
    // RoleId, JobRole, EmploymentType, ReportingManager, Department, DateOfJoining,
    // BankAccountNumber, PanNumber and CustomAttributeList - those stay HR/Admin-controlled.
    public class EmployeeSelfEditDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? PersonalEmail { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Address { get; set; }
    }
}
