using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard;

public class NoticeViewModel
{
    public string NoticeId { get; set; }
    public string UserName { get; set; }
    public string? UserProfile { get; set; }
    public Dictionary<string, string>? UserDesignation { get; set; } = null;
    public string NoticeMessage { get; set; }
    public string NoticeTitle { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public EnumsHelper.NoticeType NoticeType { get; set; }
    public bool ViewedStatus { get; set; }
}