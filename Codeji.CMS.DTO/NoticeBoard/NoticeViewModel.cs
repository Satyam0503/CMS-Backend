using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard;

public class NoticeViewModel
{
    public string UserName { get; set; }
    public string UserProfile { get; set; }
    public string UserDesignation { get; set; }
    public string NoticeMessage { get; set; }
    public string NoticeTitle { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public EnumsHelper.NoticeType NoticeType { get; set; }
}