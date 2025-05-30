using System.Net;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoticeBoardController : BaseApiController
{
    private readonly IHubContext<NoticeBoardHub> _noticeHub;
    private readonly INoticeBoardService _noticeBoardServices;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public NoticeBoardController(INoticeBoardService noticeBoardService, IHttpContextAccessor httpContextAccessor, IHubContext<NoticeBoardHub> noticeHub)
    {
        _noticeBoardServices = noticeBoardService;
        _httpContextAccessor = httpContextAccessor;
        _noticeHub = noticeHub;
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
            Result result = await _noticeBoardServices.PostNotice(notice);
            if (result.Success)
            {
                await _noticeHub.Clients.All.SendAsync("noticeNotify", notice);
                result.Message = "Notice posted successfully";
                result.StatusCode = StatusCodes.Status201Created;
            }
            return result;
        }
    }

    [HttpGet]
    [Route("GetAllNotice")]
    public async Task<Result<NoticeViewModel>> GetAllNotices()
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _noticeBoardServices.GetAllNotices(currentUserId);
    }
}