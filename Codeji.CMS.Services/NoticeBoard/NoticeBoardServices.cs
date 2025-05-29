using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.NoticeBoard;

namespace Codeji.CMS.Services.NoticeBoard;

public class NoticeBoardServices : INoticeBoardService
{
    private readonly IMongoDbRepository<Notice> _notice;
    public NoticeBoardServices(IMongoDbRepository<Notice> notice)
    {
        _notice = notice;
    }

    public async Task<Result> PostNotice(AddNoticeRequestModel model)
    {
        Notice notice = new Notice()
        {
            Title = model.Title,
            Message = model.Message,
            Target = model.Target,
            NoticeType = model.NoticeType,
        };
        return await _notice.AddOne(notice);
    }

    public async Task<Result<Notice>> GetAllNotices()
    {
        IEnumerable<Notice> notices = await _notice.GetAll();
        return new Result<Notice>
        {
            MethodResults = notices.ToList(),
            Success = true
        };
    }
}