
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;

namespace Codeji.CMS.Services.NoticeBoard
{
    public interface INoticeBoardService
    {
        Task<Result> PostNotice(AddNoticeRequestModel notice, string userId);
        Task<Result<NoticeViewModel>> GetAllNotices(string userId, int pageNo, int records);
        Task<Result<MyNoticeDTO>> GetMyNotices(string userId, int pageNo, int records);
        Task<Result> UpdateMyNotice(MyNoticeDTO notice, string userId);
    }
}