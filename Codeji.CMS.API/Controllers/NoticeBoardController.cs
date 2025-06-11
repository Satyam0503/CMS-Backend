using System.Net;
using Codeji.CMS.API.Notification;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
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
    private readonly INoticeBoardService _noticeBoardServices;
    private readonly INotificationService _notificationServices;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public NoticeBoardController(INoticeBoardService noticeBoardService, IHttpContextAccessor httpContextAccessor, INotificationService notificationService)
    {
        _noticeBoardServices = noticeBoardService;
        _httpContextAccessor = httpContextAccessor;
        _notificationServices = notificationService;
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
                string companyId = CurrentContext.CompanyId(_httpContextAccessor);
                NotificationViewModel newNotification = new()
                {
                    UserNotificationId = "",
                    NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                    SentBy = userId,
                    SentDateTime = DateTime.UtcNow,
                    IsRead = false,
                    Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.Notice, notice.Title)
                };
                await _notificationServices.SendNoticeNotification(companyId, newNotification);
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