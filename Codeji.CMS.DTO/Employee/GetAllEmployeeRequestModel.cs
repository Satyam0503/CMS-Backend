namespace Codeji.CMS.DTO.Employee
{
    public class GetAllEmployeeRequestModel
    {
        public string Name { get; set; }
        public List<string> DepartmentId { get; set; }
        public List<string> Gender { get; set; }

        public int PageNo { get; set; }
        public int Records { get; set; }
    }
}
