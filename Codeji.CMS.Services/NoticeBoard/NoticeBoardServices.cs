using System.Linq.Expressions;
using AngleSharp.Text;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.NoticeBoard;

public class NoticeBoardServices : INoticeBoardService
{
    readonly IMapper _mapper;
    private readonly IMongoDbRepository<Notice> _noticeRepository;
    private readonly IMongoDbRepository<EmpUser> _empUserRepository;
    private readonly IMongoDbRepository<Notifications> _notificationRepository;
    private readonly IMongoDbRepository<UserNotifications> _userNotificationsRepository;
    private readonly INotificationService _notificationService;
    public NoticeBoardServices(IMapper mapper, IMongoDbRepository<Notice> noticeRepository, IMongoDbRepository<EmpUser> empUserRepository, IMongoDbRepository<Notifications> notificationRepository, IMongoDbRepository<UserNotifications> userNotificationsRepository, INotificationService notificationService)
    {
        _mapper = mapper;
        _noticeRepository = noticeRepository;
        _empUserRepository = empUserRepository;
        _notificationRepository = notificationRepository;
        _userNotificationsRepository = userNotificationsRepository;
        _notificationService = notificationService;
    }

    public async Task<Result> PostNotice(AddNoticeRequestModel model, string userId)
    {
        Notice notice = new()
        {
            Title = model.Title,
            Message = model.Message,
            Target = model.Target,
            NoticeType = model.NoticeType,
            Departments = model.Departments,
        };
        Result result = await _noticeRepository.AddOne(notice);
        if (!result.Success)
        {
            return result;
        }
        Notifications notification = new()
        {
            NotificationId = Guid.NewGuid().ToString(),
            Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.Notice, model.Title),
            CreatedDateTime = DateTime.UtcNow,
            NotificationType = EnumsHelper.NotificationTypes.Notice,
        };
        Result result1 = await _notificationRepository.AddOne(notification);
        if (result1.Success)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => (model.Departments.Equals("all") || x.Department.Equals(model.Departments))
            && (model.Target.Equals("all") || x.RoleId.Equals(model.Target));
            IEnumerable<EmpUser> empUsers = await _empUserRepository.GetAll(whereCondition);
            if (empUsers.Any())
            {
                List<UserNotifications> userNotifications = [];

                foreach (EmpUser user in empUsers)
                {
                    UserNotifications userNotification = new()
                    {
                        UserNotificationId = Guid.NewGuid().ToString(),
                        UserId = user.UserId,
                        NotificationId = notification.NotificationId,
                        IsRead = false,
                    };
                    userNotifications.Add(userNotification);
                    // await _notificationService.SendNoticeNotificationToUser(user.UserId, new NotificationViewModel()
                    // {
                    //     Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.Notice, model.Title),
                    //     IsRead = false,
                    //     SentDateTime = DateTime.UtcNow,
                    //     SentBy = userId,
                    //     NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                    //     UserNotificationId = userNotification.UserNotificationId,
                    // });
                }
                await _userNotificationsRepository.AddMany(userNotifications);
                await _notificationService.SendNoticeNotificationToUser(userId, new NotificationViewModel()
                {
                    Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.Notice, model.Title),
                    IsRead = false,
                    SentDateTime = DateTime.UtcNow,
                    SentBy = userId,
                    NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                    UserNotificationId = "",
                });
            }
        }
        return result;
    }

    public async Task<Result<NoticeViewModel>> GetAllNotices(string userId)
    {
        EmpUser? employee = await _empUserRepository.FirstOrDefault(x => x.UserId == userId);
        Expression<Func<Notice, bool>> whereCondition = x => (x.Departments.Equals("all") || x.Departments.Equals(employee.Department))
        && (x.Target.Equals("all") || x.Target.Equals(employee.RoleId));
        IEnumerable<Notice> noticeList = await _noticeRepository.GetAll(whereCondition);
        List<string> empIdList = noticeList.Select(x => x.CreatedBy).Distinct().ToList();
        IEnumerable<EmpUser> empUsers = await _empUserRepository.GetAll(x => empIdList.Contains(x.UserId));
        var data = (from notice in noticeList
                    join emp in empUsers on notice.CreatedBy equals emp.UserId
                    select new NoticeViewModel
                    {
                        UserName = $"{emp.FirstName} {emp.LastName}",
                        UserDesignation = emp.JobRole,
                        NoticeMessage = notice.Message,
                        NoticeTitle = notice.Title,
                        CreatedDateTime = notice.CreatedDate,
                        NoticeType = notice.NoticeType,
                        UserProfile = string.IsNullOrEmpty(emp.ProfileUrl) ? Common.GetEmployeeImageUrl(null) : Common.GetEmployeeImageUrl(emp.ProfileUrl),
                    }).OrderByDescending(x => x.CreatedDateTime).ToList();

        return new Result<NoticeViewModel>()
        {
            Success = true,
            MethodResults = data,
            TotalRecords = data.Count
        };
    }
    public async Task<Result<MyNoticeDTO>> GetMyNotices(string userId)
    {
        IEnumerable<Notice> noticeList = await _noticeRepository.GetAll(x => x.CreatedBy.Equals(userId));
        var data = _mapper.Map<List<MyNoticeDTO>>(noticeList).OrderByDescending(x => x.CreatedDate).ToList();
        return new Result<MyNoticeDTO>()
        {
            Success = true,
            MethodResults = data,
            TotalRecords = data.Count
        };
    }
}