using System.Diagnostics;
using System.Linq.Expressions;
using AngleSharp.Common;
using AngleSharp.Text;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
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
    private readonly IEmployeeService _employeeService;
    private readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
    readonly IMiddlewareService _middlewareService;
    public NoticeBoardServices(
        IMapper mapper,
        IMongoDbRepository<Notice> noticeRepository,
        IMongoDbRepository<EmpUser> empUserRepository,
        IMongoDbRepository<Notifications> notificationRepository,
        IMongoDbRepository<UserNotifications> userNotificationsRepository,
        INotificationService notificationService,
        IMongoDbRepository<JobTitles> jobTitlesRepository,
        IEmployeeService employeeService,
        IMiddlewareService middlewareService
        )
    {
        _mapper = mapper;
        _noticeRepository = noticeRepository;
        _empUserRepository = empUserRepository;
        _notificationRepository = notificationRepository;
        _userNotificationsRepository = userNotificationsRepository;
        _notificationService = notificationService;
        _jobTitlesRepository = jobTitlesRepository;
        _employeeService = employeeService;
        _middlewareService = middlewareService;
    }

    public async Task<Result> PostNotice(AddNoticeRequestModel model, string userId)
    {
        Notice notice = new()
        {
            NoticeId = Guid.NewGuid().ToString(),
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
            TargetId = notice.NoticeId,
            CreatedDateTime = DateTime.UtcNow,
            NotificationType = EnumsHelper.NotificationTypes.Notice,
        };
        Result result1 = await _notificationRepository.AddOne(notification);
        if (result1.Success)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => (model.Departments.Equals("all") || x.Department.Equals(model.Departments))
            && (model.Target.Equals("all") || x.RoleId.Equals(model.Target)) && x.UserId != userId;
            IEnumerable<EmpUser> empUsers = await _empUserRepository.GetAll(whereCondition);
            empUsers = empUsers.Where(emp => _middlewareService.IsUserNotificationPreferenceEnabled(emp.UserId, EnumsHelper.NotificationPreferenceType.Notice));

            if (empUsers.Any())
            {
                List<UserNotifications> userNotifications = [];
                var notificationsTasks = new List<Task>();
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

                    // add user notification entry
                    userNotifications.Add(userNotification);

                    // queue notification task 
                    notificationsTasks.Add(_notificationService.SendNotificationToUser(user.UserId, new NotificationViewModel()
                    {
                        Title = notification.Title,
                        Body = notification.Body,
                        IsRead = false,
                        SentDateTime = DateTime.UtcNow,
                        SentBy = userId,
                        TargetId = notification.TargetId,
                        NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                        UserNotificationId = userNotification.UserNotificationId,
                    }));
                }
                // run all the notification task parallel
                await Task.WhenAll(notificationsTasks);
                await _userNotificationsRepository.AddMany(userNotifications);
            }
        }
        return result;
    }

    public async Task<Result> UpdateMyNotice(UpdateNoticeDto model, string userId)
    {
        Notice? existingNotice = await _noticeRepository.FirstOrDefault(x => x.NoticeId == model.NoticeId);
        if (existingNotice is null)
        {
            return new Result();
        }
        Expression<Func<Notice, bool>> wherecondition = x => x.NoticeId == model.NoticeId;
        UpdateDefinition<Notice> updateDefinition = Builders<Notice>.Update.Set(n => n.Title, model.Title).
                                                    Set(n => n.Message, model.Message).
                                                    Set(n => n.Target, model.Target).
                                                    Set(n => n.Departments, model.Departments).
                                                    Set(n => n.NoticeType, model.NoticeType).
                                                    Set(n => n.UpdatedBy, userId).
                                                    Set(n => n.UpdatedDate, DateTime.UtcNow);
        return await _noticeRepository.UpdateMany(wherecondition, updateDefinition);
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
        IEnumerable<JobTitles> jobTitles = await _jobTitlesRepository.GetAll();
        var data = (from notice in noticeList
                    join emp in empUsers on notice.CreatedBy equals emp.UserId
                    join job in jobTitles on emp.JobRole equals job.JobTitleId into jobGroup
                    from jobTitle in jobGroup.DefaultIfEmpty()
                    select new NoticeViewModel
                    {
                        NoticeId = notice.NoticeId,
                        UserName = $"{emp.FirstName} {emp.LastName}",
                        UserDesignation = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                        NoticeMessage = notice.Message,
                        NoticeTitle = notice.Title,
                        CreatedDateTime = notice.CreatedDate,
                        NoticeType = notice.NoticeType,
                        UserProfile = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                        ViewedStatus = notice.Views.Any(v => v.EmployeeId == userId)
                    }).OrderByDescending(x => x.CreatedDateTime).ToList();

        return new Result<NoticeViewModel>()
        {
            Success = true,
            MethodResults = data,
            TotalRecords = totalRecords
        };
    }

    public async Task<NoticeViewModel?> GetNoticeById(string noticeId, string userId)
    {
        Notice? notice = await _noticeRepository.FirstOrDefault(x => x.NoticeId == noticeId);
        if (notice is null) return null;

        // if notice exist mark the notice as read for current user if user not present in views list
        var hasViewed = notice.Views.Any(v => v.EmployeeId == userId);
        if (!hasViewed)
        {
            notice.Views.Add(new NoticeView()
            {
                EmployeeId = userId,
                ViewedAt = DateTime.UtcNow
            });

            Expression<Func<Notice, bool>> expression = n => n.NoticeId == noticeId;
            await _noticeRepository.UpdateMany(expression, Builders<Notice>.Update.Set(n => n.Views, notice.Views));
        }

        EmpUser? user = await _empUserRepository.FirstOrDefault(x => x.UserId == notice.CreatedBy);
        if (user is null) return null;
        JobTitles? jobTitle = await _jobTitlesRepository.FirstOrDefault(jt => jt.JobTitleId == user.JobRole);
        var data = new NoticeViewModel()
        {
            NoticeId = notice.NoticeId,
            UserName = $"{user.FirstName} {user.LastName}",
            UserProfile = Common.GetEmployeeImageUrl(user.ProfileUrl),
            UserDesignation = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
            NoticeMessage = notice.Message,
            NoticeTitle = notice.Title,
            CreatedDateTime = notice.CreatedDate,
            NoticeType = notice.NoticeType,
            ViewedStatus = notice.Views.Any(n => n.EmployeeId == userId)
        };
        return data;
    }

    public async Task<Result<MyNoticeDTO>> GetMyNotices(string userId, int pageNo, int records)
    {
        Expression<Func<Notice, bool>> whereCondition = x => x.CreatedBy.Equals(userId);
        int totalRecords = await _noticeRepository.Count(whereCondition);
        List<Notice> noticeList = (await _noticeRepository.GetAggregateDataAsync<Notice>(whereCondition, isAscending: false, orderedKey: "CreatedDate", pageNo: pageNo, pageSize: records)).ToList();
        // Collect all unique employee IDs from the notice views
        var employeeIds = noticeList
            .SelectMany(notice => notice.Views)
            .Select(view => view.EmployeeId)
            .Distinct()
            .ToList();

        var employeeDetails = await _empUserRepository.GetAll(emp => employeeIds.Contains(emp.UserId));
        List<MyNoticeDTO> myNoticeList = [];
        foreach (var notice in noticeList)
        {
            var myNotice = new MyNoticeDTO()
            {
                NoticeId = notice.NoticeId,
                Title = notice.Title,
                Message = notice.Message,
                Target = notice.Target,
                Departments = notice.Departments,
                NoticeType = notice.NoticeType,
                CreatedDate = (DateTime)notice.CreatedDate
            };

            var viewsList = new List<EmpNoticeView>();

            foreach (var v in notice.Views)
            {
                var employeeDetail = employeeDetails.FirstOrDefault(emp => emp.UserId == v.EmployeeId);

                // If employee details exist, map them to the EmpNoticeView
                if (employeeDetail != null)
                {
                    viewsList.Add(new EmpNoticeView
                    {
                        EmployeeId = v.EmployeeId,
                        ViewedAt = v.ViewedAt,
                        EmployeeName = $"{employeeDetail.FirstName} {employeeDetail.LastName}",
                        ProfileUrl = Common.GetEmployeeImageUrl(employeeDetail.ProfileUrl)
                    });
                }
            }

            // Assign the populated views list to the notice DTO
            myNotice.Views = viewsList;
            myNoticeList.Add(myNotice);
        }
        return new Result<MyNoticeDTO>()
        {
            Success = true,
            MethodResults = myNoticeList,
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