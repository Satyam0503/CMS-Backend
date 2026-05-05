namespace Codeji.CMS.DTO.Employee
{
    public class LoginUserViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }
        public string? ProfileImage { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string DefaultLanguage { get; set; }
        public List<string> ApplicationLanguage { get; set; }
        public string[] ModulePermission { get; set; }
        public string? CompanyLogo { get; set; }
        public int RoleType { get; set; }
        // Localized job title — chosen by the server using Accept-Language,
        // falling back to the company's DefaultLanguage. Empty when the
        // employee has no job role assigned.
        public string? JobTitle { get; set; }
    }
}

