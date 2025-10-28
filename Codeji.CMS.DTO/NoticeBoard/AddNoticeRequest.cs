using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.NoticeBoard
{
    public class AddNoticeRequestModel
    {
        [MinLength(3, ErrorMessage = "Title length should be atleast 3 characters")]
        public string Title { get; set; }
        [MinLength(3, ErrorMessage = "Message length should be atleast 3 characters")]
        public string Message { get; set; }
        public string Target { get; set; }
        [Range(1, 3)]
        public EnumsHelper.NoticeType NoticeType { get; set; }
        public string Departments { get; set; }
    }
}