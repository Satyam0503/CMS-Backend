namespace Codeji.CMS.DTO.Company.Department
{
    public class DepartmentResponseDto
    {
        public string? DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string> Titles { get; set; }
    }
}
