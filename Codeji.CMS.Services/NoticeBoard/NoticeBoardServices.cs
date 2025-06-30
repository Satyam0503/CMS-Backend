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
using MongoDB.Driver;

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
            Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.Notice),
            Body = model.Title,
            CreatedDateTime = DateTime.UtcNow,
            NotificationType = EnumsHelper.NotificationTypes.Notice,
        };
        Result result1 = await _notificationRepository.AddOne(notification);
        if (result1.Success)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => (model.Departments.Equals("all") || x.Department.Equals(model.Departments))
            && (model.Target.Equals("all") || x.RoleId.Equals(model.Target)) && x.UserId != userId;
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
                        CreatedDateTime = DateTime.UtcNow,
                        IsDeleted = false,
                    };
                    userNotifications.Add(userNotification);
                    await _notificationService.SendNoticeNotificationToUser(user.UserId, new NotificationViewModel()
                    {
                        Title = notification.Title,
                        Body = notification.Body,
                        IsRead = false,
                        SentDateTime = DateTime.UtcNow,
                        SentBy = userId,
                        NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                        UserNotificationId = userNotification.UserNotificationId,
                    });
                }
                await _userNotificationsRepository.AddMany(userNotifications);
            }
        }
        return result;
    }

    public async Task<Result> UpdateMyNotice(MyNoticeDTO model, string userId)
    {
        Notice? existingNotice = await _noticeRepository.FirstOrDefault(x => x.NoticeId == model.NoticeId);
        if (existingNotice is null)
        {
            return new Result();
        }
        Notice notice = _mapper.Map<Notice>(model);
        notice.UpdatedBy = userId;
        notice.UpdatedDate = DateTime.UtcNow;
        notice.CreatedBy = existingNotice.CreatedBy;
        Expression<Func<Notice, bool>> wherecondition = x => x.NoticeId == model.NoticeId;
        return await _noticeRepository.Update(wherecondition, notice);
    }
    public async Task<Result<NoticeViewModel>> GetAllNotices(string userId, GetNoticeRequest filter)
    {
        List<string> empIdsList = [];
        if (filter.PostedBy.Length > 0)
        {
            empIdsList = (await _empUserRepository.GetAll(x => (x.FirstName + " " + x.LastName).Contains(filter.PostedBy.Trim(), StringComparison.CurrentCultureIgnoreCase))).Select(x => x.UserId).ToList();
        }
        EmpUser? employee = await _empUserRepository.FirstOrDefault(x => x.UserId == userId);
        Expression<Func<Notice, bool>> whereCondition = x => (x.Departments.Equals("all") || x.Departments.Equals(employee.Department))
        && (x.Target.Equals("all") || x.Target.Equals(employee.RoleId))
        && (filter.NoticeType.Length == 0 || filter.NoticeType.Contains(x.NoticeType))
        && ((!filter.FilterFrom.HasValue || filter.FilterFrom.Value <= x.CreatedDate) && (!filter.FilterTo.HasValue || filter.FilterTo >= x.CreatedDate))
        && (filter.PostedBy.Trim().Length == 0 || empIdsList.Contains(x.CreatedBy));
        int totalRecords = await _noticeRepository.Count(whereCondition);
        List<Notice> noticeList = (await _noticeRepository.GetAggregateDataAsync<Notice>(whereCondition, isAscending: false, orderedKey: "CreatedDate", pageNo: filter.PageNo, pageSize: filter.Records)).ToList();
        List<string> empIdList = noticeList.Select(x => x.CreatedBy).Distinct().ToList();
        IEnumerable<EmpUser> empUsers = await _empUserRepository.GetAll(x => empIdList.Contains(x.UserId));
        var data = (from notice in noticeList
                    join emp in empUsers on notice.CreatedBy equals emp.UserId
                    select new NoticeViewModel
                    {
                        NoticeId = notice.NoticeId,
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
            TotalRecords = totalRecords
        };
    }
    public async Task<Result<MyNoticeDTO>> GetMyNotices(string userId, int pageNo, int records)
    {
        Expression<Func<Notice, bool>> whereCondition = x => x.CreatedBy.Equals(userId);
        int totalRecords = await _noticeRepository.Count(whereCondition);
        List<Notice> noticeList = (await _noticeRepository.GetAggregateDataAsync<Notice>(whereCondition, isAscending: false, orderedKey: "CreatedDate", pageNo: pageNo, pageSize: records)).ToList();
        var data = _mapper.Map<List<MyNoticeDTO>>(noticeList);
        return new Result<MyNoticeDTO>()
        {
            Success = true,
            MethodResults = data,
            TotalRecords = totalRecords
        };
    }
    public async Task<bool> DeleteNotice(string userId, string noticeId)
    {
        Expression<Func<Notice, bool>> whereCondition = x => x.CreatedBy == userId && x.NoticeId == noticeId;
        bool isExist = await _noticeRepository.Exist(whereCondition);
        if (!isExist) return false;
        Result result = await _noticeRepository.UpdateMany(whereCondition, Builders<Notice>.Update.Set(x => x.IsDeleted, true));
        return result.Success;
    }
}