using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard;

public class UpdateNoticeDto
{
    public string NoticeId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Target { get; set; }
    public string Departments { get; set; }
    public EnumsHelper.NoticeType NoticeType { get; set; }
    public DateTime CreatedDate { get; set; }
}
