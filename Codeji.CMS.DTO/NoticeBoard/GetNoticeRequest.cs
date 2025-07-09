using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard;

public class GetNoticeRequest
{
    public int PageNo { get; set; }
    public int Records { get; set; }
    public EnumsHelper.NoticeType[] NoticeType { get; set; }
    public string PostedBy { get; set; }
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
}
