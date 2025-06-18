
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.Repository.Entities.NoticeBoard;

namespace Codeji.CMS.Services.NoticeBoard
{
    public interface INoticeBoardService
    {
        Task<Result> PostNotice(AddNoticeRequestModel notice, string userId);
        Task<Result<NoticeViewModel>> GetAllNotices(string userId);
        Task<Result<MyNoticeDTO>> GetMyNotices(string userId);
    }
}