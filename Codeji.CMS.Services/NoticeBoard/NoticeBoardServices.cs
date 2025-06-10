using System.Linq.Expressions;
using AngleSharp.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Services.NoticeBoard;

public class NoticeBoardServices : INoticeBoardService
{
    private readonly IMongoDbRepository<Notice> _notice;
    private readonly IMongoDbRepository<EmpUser> _empUser;
    private readonly IMongoDbRepository<Notifications> _notification;
    private readonly IMongoDbRepository<UserNotifications> _userNotifications;
    public NoticeBoardServices(IMongoDbRepository<Notice> notice, IMongoDbRepository<EmpUser> empUser, IMongoDbRepository<Notifications> notification, IMongoDbRepository<UserNotifications> userNotification)
    {
        _notice = notice;
        _empUser = empUser;
        _notification = notification;
        _userNotifications = userNotification;
    }

    public async Task<Result> PostNotice(AddNoticeRequestModel model, string userId)
    {
        Notice notice = new Notice()
        {
            Title = model.Title,
            Message = model.Message,
            Target = model.Target,
            NoticeType = model.NoticeType,
            Departments = model.Departments,
        };
        Result result = await _notice.AddOne(notice);
        // if (result.Success)
        // {
        Expression<Func<EmpUser, bool>> whereCondition = x => (model.Departments.Equals("all") || x.Department.Equals(model.Departments))
        && (model.Target.Equals("all") || x.RoleId.Equals(x.RoleId));

        EmpUser currentUser = await _empUser.FirstOrDefault(x => x.UserId.Equals(userId));
        string notificationId = Guid.NewGuid().ToString();
        Notifications notification = new Notifications()
        {
            NotificationId = notificationId,
            Title = $"{currentUser.FirstName} {currentUser.LastName} posted a notice",
            CreatedDateTime = DateTime.Now,
            NotificationType = EnumsHelper.NotificationTypes.Notice,
            CreatedBy = $"{currentUser?.FirstName} {currentUser?.LastName}",
        };
        await _notification.AddOne(notification);
        IEnumerable<EmpUser> empUsers = await _empUser.GetAll(whereCondition);
        // if (empUsers.Any())
        // {
        List<UserNotifications> userNotifications = [];
        foreach (EmpUser user in empUsers)
        {
            UserNotifications userNotification = new()
            {
                UserId = user.UserId,
                NotificationId = notificationId,
                IsRead = false,
            };
            userNotifications.Add(userNotification);
        }
        await _userNotifications.AddMany(userNotifications);
        // }
        return result;
        // }
    }

    public async Task<Result<NoticeViewModel>> GetAllNotices(string userId)
    {
        EmpUser? employee = await _empUser.FirstOrDefault(x => x.UserId == userId);
        Expression<Func<Notice, bool>> whereCondition = x => (x.Departments.Equals("all") || x.Departments.Equals(employee.Department))
        && (x.Target.Equals("all") || x.Target.Equals(employee.RoleId));
        IEnumerable<Notice> noticeList = await _notice.GetAll(whereCondition);
        List<string> empIdList = noticeList.Select(x => x.CreatedBy).Distinct().ToList();
        IEnumerable<EmpUser> empUsers = await _empUser.GetAll(x => empIdList.Contains(x.UserId));
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
}