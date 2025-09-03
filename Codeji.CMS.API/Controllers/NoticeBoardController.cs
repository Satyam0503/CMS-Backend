using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoticeBoardController : BaseApiController
{
    private readonly INoticeBoardService _noticeBoardServices;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public NoticeBoardController(INoticeBoardService noticeBoardService, IHttpContextAccessor httpContextAccessor)
    {
        _noticeBoardServices = noticeBoardService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [Route("PostNotice")]
    public async Task<Result> PostNotice(AddNoticeRequestModel notice)
    {
        if (!ModelState.IsValid)
        {
            return new Result();
        }
        else
        {
            string userId = CurrentContext.UserId(_httpContextAccessor);
            Result result = await _noticeBoardServices.PostNotice(notice, userId);
            return result;
        }
    }

    [HttpPost]
    [Route("GetAllNotice")]
    public async Task<Result<NoticeViewModel>> GetAllNotices([FromBody] GetNoticeRequest filter)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _noticeBoardServices.GetAllNotices(currentUserId, filter);
    }

    [HttpGet]
    [Route("GetNoticeById/{noticeId}")]
    public async Task<Result<NoticeViewModel>> GetNoticeById(string noticeId)
    {
        Result<NoticeViewModel> result = new();
        var data = await _noticeBoardServices.GetNoticeById(noticeId);
        if (data is null)
        {
            result.Success = false;
        }
        else
        {
            result.MethodResult = data;
            result.Success = true;
        }
        return result;
    }

    [HttpGet]
    [Route("GetMyNotices")]
    public async Task<Result<MyNoticeDTO>> GetMyNotices([FromQuery] int pageNo, [FromQuery] int records)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _noticeBoardServices.GetMyNotices(currentUserId, pageNo, records);
    }

    [HttpPut]
    [Route("UpdateMyNotice")]
    public async Task<Result> UpdateMyNotice([FromBody] MyNoticeDTO notice)
    {
        if (!ModelState.IsValid)
        {
            return new Result();
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _noticeBoardServices.UpdateMyNotice(notice, currentUserId);
    }

    [HttpDelete]
    [Route("DeleteNotice/{noticeId}")]
    public async Task<Result> DeleteNotice(string noticeId)
    {
        Result result = new();
        if (string.IsNullOrEmpty(noticeId)) return result;
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        bool isDeleted = await _noticeBoardServices.DeleteNotice(currentUserId, noticeId);
        if (isDeleted) result.Success = true;
        return result;
    }

}