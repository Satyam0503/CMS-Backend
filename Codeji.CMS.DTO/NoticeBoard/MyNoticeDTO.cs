using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard;

public class MyNoticeDTO
{
    public string NoticeId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Target { get; set; }
    public string Departments { get; set; }
    public EnumsHelper.NoticeType NoticeType { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<EmpNoticeView> Views { get; set; }
}

public class EmpNoticeView
{
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string? ProfileUrl { get; set; }
    public DateTime ViewedAt { get; set; }
}