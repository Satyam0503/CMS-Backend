using Codeji.CMS.API.Notification;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
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

            if (result.Success)
            {
                result.Message = "Notice posted successfully";
                result.StatusCode = StatusCodes.Status201Created;
            }
            return result;
        }
    }

    [HttpGet]
    [Route("GetAllNotice")]
    public async Task<Result<NoticeViewModel>> GetAllNotices([FromQuery] int pageNo, [FromQuery] int records)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _noticeBoardServices.GetAllNotices(currentUserId, pageNo, records);
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
}