using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using LinqKit;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.LeaveManagement;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<Roles> _roleRepository;
    private readonly IMongoDbRepository<EmpUser> _employeeRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveRequest> _leave;
    private readonly INotificationService _notificationService;
    private readonly IMongoDbRepository<Notifications> _notificationsRepo;
    private readonly IMongoDbRepository<UserNotifications> _userNotificationsRepo;
    private readonly IMongoDbRepository<JobTitles> _jobTitleRepo;
    private readonly IMapper _mapper;
    private readonly IEmployeeService _employeeService;
    private readonly IMiddlewareService _middlewareService;
    private readonly IMongoDbRepository<LeavePolicy> _leavePolicyRepo;
    private readonly IMongoDbRepository<EmployeeLeaveBalance> _employeeLeaveBalanceRepo;
    readonly IMongoDbRepository<CalendarEntity> _calendarRepository;
    public LeaveManagementService(
    IMongoDbRepository<EmpUser> employeeRepository,
    IMongoDbRepository<LeaveRequest> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper,
    INotificationService notificationService,
    IMongoDbRepository<Notifications> notificationsRepo,
    IMongoDbRepository<UserNotifications> userNotificationsRepo,
    IMongoDbRepository<Roles> roleRepository,
    IMongoDbRepository<JobTitles> jobTitleRepo,
    IEmployeeService employeeService,
    IMiddlewareService middlewareService,
    IMongoDbRepository<LeavePolicy> leavePolicyRepo,
    IMongoDbRepository<EmployeeLeaveBalance> employeeLeaveBalanceRepo,
    IMongoDbRepository<CalendarEntity> calendarRepository
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _employeeRepository = employeeRepository;
        _leave = leave;
        _mapper = mapper;
        _notificationService = notificationService;
        _notificationsRepo = notificationsRepo;
        _userNotificationsRepo = userNotificationsRepo;
        _roleRepository = roleRepository;
        _employeeService = employeeService;
        _jobTitleRepo = jobTitleRepo;
        _middlewareService = middlewareService;
        _leavePolicyRepo = leavePolicyRepo;
        _employeeLeaveBalanceRepo = employeeLeaveBalanceRepo;
        _calendarRepository = calendarRepository;
    }

    // updated services
    public async Task<Result> CreateNewLeavePolicy(LeavePolicyRequest leavePolicyDto, string company_id)
    {
        Result result = new();
        // check if policy already exist;
        var exist = await _leavePolicyRepo.Exist(lp => lp.Name.Equals(leavePolicyDto.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code.Equals(leavePolicyDto.Code, StringComparison.CurrentCultureIgnoreCase));
        if (exist)
        {
            result.StatusCode = CustomStatusCode.LeavePolicyAlreadyExist;
            return result;
        }
        LeavePolicy leavePolicy = _mapper.Map<LeavePolicy>(leavePolicyDto);
        leavePolicy.Id = Guid.NewGuid().ToString();

        // insert new leave policy
        result = await _leavePolicyRepo.AddOne(leavePolicy);

        // add leave policy to applicable employees 
        if (leavePolicyDto.ApplicableTo != null && result.Success)
        {
            //  apply for all employee if empty array
            List<string> EmpIdList = [];
            if (leavePolicyDto.ApplicableTo.Length == 0)
            {
                EmpIdList = (await _employeeRepository.GetAll(emp => emp.Status && emp.CompanyId == company_id)).Select(emp => emp.UserId).ToList();
            }
            else
            {
                foreach (string empId in leavePolicyDto.ApplicableTo)
                {
                    bool employeeExist = await _employeeRepository.Exist(emp => emp.UserId == empId && emp.CompanyId == company_id && emp.Status);
                    if (employeeExist) EmpIdList.Add(empId);
                }
            }

            // add balance for employees
            List<EmployeeLeaveBalance> employeeLeaveBalancesList = [];
            foreach (string empId in EmpIdList)
            {
                EmployeeLeaveBalance employeeLeaveBalance = new()
                {
                    UserId = empId,
                    LeavePolicyId = leavePolicy.Id,
                    Balance = leavePolicyDto.AccrualAmount,
                    UsedBalance = 0,
                    LastAccrual = DateTime.UtcNow,
                };
                employeeLeaveBalancesList.Add(employeeLeaveBalance);
            }
            await _employeeLeaveBalanceRepo.AddMany(employeeLeaveBalancesList);
        }
        return result;
    }

    public async Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy(UpdateLeavePolicyRequest model)
    {
        Result<UpdateLeavePolicyRequest> result = new() { Success = false };
        // check if leave policy exits or not;
        Expression<Func<LeavePolicy, bool>> expression = lp => lp.Id == model.Id;
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(expression);
        if (leavePolicy == null) return result;

        bool duplicateLeavePolicy = await _leavePolicyRepo.Exist(lp => (lp.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code.Equals(model.Code, StringComparison.CurrentCultureIgnoreCase)) && lp.Id != model.Id);
        if (duplicateLeavePolicy)
        {
            result.StatusCode = CustomStatusCode.DuplicationLeavePolicy;
            return result;
        }
        LeavePolicy updatedPolicyModel = _mapper.Map<LeavePolicy>(model);
        var updateResult = await _leavePolicyRepo.Update(expression, updatedPolicyModel);
        result.Success = updateResult.Success;
        if (result.Success)
        {
            result.MethodResult = model;
        }
        return result;
    }

    public async Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies(string companyId, bool? status)
    {
        Expression<Func<LeavePolicy, bool>> expression = status.HasValue ? lp => lp.CompanyId == companyId && lp.Status == status.Value : lp => lp.CompanyId == companyId;
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(expression);
        var list = _mapper.Map<List<UpdateLeavePolicyRequest>>(leavePolicies);
        return new Result<UpdateLeavePolicyRequest>()
        {
            Success = true,
            MethodResults = list
        };
    }

    public async Task<Result> CreateLeaveRequest(LeaveRequestDto leaveRequest)
    {
        Result result = new();
        // check if employee has balance for requested leave type
        var employeeLeaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.UserId == leaveRequest.UserId && elb.LeavePolicyId == leaveRequest.LeavePolicyId);
        if (employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }
        // check if requested leave type active or not
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.Id == leaveRequest.LeavePolicyId && lp.Status);
        if (leavePolicy is null) return result;

        // check if employee has pending leave
        int pendingLeaves = await _leave.Count(lr => lr.EmployeeId == leaveRequest.UserId && lr.Status == EnumsHelper.LeaveRequestStatus.Pending);
        if (pendingLeaves > 0)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.PendingLeaveExist;
            return result;
        }

        // check if emp already has apporoved leave on request date
        var upcomingApprovedLeaves = await _leave.GetAll(lr => lr.EmployeeId == leaveRequest.UserId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.EndDate >= DateTime.UtcNow.Date);
        bool isOverlapping = upcomingApprovedLeaves.Any(lr => leaveRequest.StartDate <= lr.EndDate && leaveRequest.EndDate >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            return result;
        }

        // validate min advance notice day
        var totalAdvanceNoticeDays = (leaveRequest.StartDate.Date - DateTime.UtcNow.Date).TotalDays + 1;

        if (leavePolicy.MinNoticeDays > 0 && leavePolicy.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        // get total requested days
        decimal totalRequestedDays = 0m;
        IEnumerable<CalendarEntity> calendarEntities = await _calendarRepository.GetAll(cal => cal.Type == EnumsHelper.CalendarItem.Holiday && cal.Date >= leaveRequest.StartDate || cal.Recurring);
        for (DateTime date = leaveRequest.StartDate; date <= leaveRequest.EndDate; date = date.AddDays(1))
        {
            bool isWeekend = IsWeekEnd(date);
            bool isHoliday = IsHoliday(date, calendarEntities);

            if ((!leavePolicy.WeekendInclusive && isWeekend) || (!leavePolicy.HolidayInclusive && isHoliday))
                continue;

            totalRequestedDays += 1;
        }

        if (leaveRequest.IsHalfDay && leavePolicy.HalfDayAllowed)
        {
            totalRequestedDays = 0.5m;
        }

        // validate requested days
        if (totalRequestedDays > employeeLeaveBalance.Balance)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            return result;
        }

        var request = new LeaveRequest
        {
            EmployeeId = leaveRequest.UserId,
            LeavePolicyId = leaveRequest.LeavePolicyId,
            StartDate = leaveRequest.StartDate,
            EndDate = (leavePolicy.HalfDayAllowed && leaveRequest.IsHalfDay) ? leaveRequest.StartDate : leaveRequest.EndDate,
            TotalDays = totalRequestedDays,
            IsHalfDay = leavePolicy.HalfDayAllowed && leaveRequest.IsHalfDay,
            Reason = leaveRequest.Reason,
            ReviewedBy = "",
            Status = EnumsHelper.LeaveRequestStatus.Pending
        };

        result = await _leave.AddOne(request);
        if (result.Success)
        {
            LeaveNotification(request, request.Status);
        }
        return result;
    }

    public async Task<Result> UpdateLeaveRequest(UpdateLeaveRequestDto leaveRequestDto)
    {
        Result result = new();
        var existingLeaveRequest = await _leave.FirstOrDefault(lr => lr.LeaveRequestId == leaveRequestDto.LeaveRequestId && leaveRequestDto.UserId == lr.EmployeeId);
        if (existingLeaveRequest is null) return result;
        if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Pending) return result;


        // check if employee has balance for requested leave type
        var employeeLeaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.UserId == leaveRequestDto.UserId && elb.LeavePolicyId == leaveRequestDto.LeavePolicyId);
        if (employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }

        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.Id == leaveRequestDto.LeavePolicyId && lp.Status);
        if (leavePolicy is null) return result;

        // check if emp already has apporoved leave on request date
        var upcomingApprovedLeaves = await _leave.GetAll(lr => lr.EmployeeId == leaveRequestDto.UserId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.EndDate >= DateTime.UtcNow.Date);
        bool isOverlapping = upcomingApprovedLeaves.Any(lr => leaveRequestDto.StartDate <= lr.EndDate && leaveRequestDto.EndDate >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            return result;
        }

        // validate min advance notice day
        var totalAdvanceNoticeDays = (leaveRequestDto.StartDate.Date - DateTime.UtcNow.Date).TotalDays + 1;

        if (leavePolicy.MinNoticeDays > 0 && leavePolicy.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        // get total requested days
        decimal totalRequestedDays = 0m;
        IEnumerable<CalendarEntity> calendarEntities = await _calendarRepository.GetAll(cal => cal.Type == EnumsHelper.CalendarItem.Holiday && cal.Date >= leaveRequestDto.StartDate || cal.Recurring);
        for (DateTime date = leaveRequestDto.StartDate; date <= leaveRequestDto.EndDate; date = date.AddDays(1))
        {
            bool isWeekend = IsWeekEnd(date);
            bool isHoliday = IsHoliday(date, calendarEntities);

            if ((!leavePolicy.WeekendInclusive && isWeekend) || (!leavePolicy.HolidayInclusive && isHoliday))
                continue;

            totalRequestedDays += 1;
        }

        if (leaveRequestDto.IsHalfDay && leavePolicy.HalfDayAllowed)
        {
            totalRequestedDays = 0.5m;
        }

        // validate requested days
        if (totalRequestedDays > employeeLeaveBalance.Balance)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            return result;
        }


        existingLeaveRequest.LeavePolicyId = leaveRequestDto.LeavePolicyId;
        existingLeaveRequest.StartDate = leaveRequestDto.StartDate;
        existingLeaveRequest.EndDate = (leavePolicy.HalfDayAllowed && leaveRequestDto.IsHalfDay) ? leaveRequestDto.StartDate : leaveRequestDto.EndDate;
        existingLeaveRequest.TotalDays = totalRequestedDays;
        existingLeaveRequest.IsHalfDay = leavePolicy.HalfDayAllowed && leaveRequestDto.IsHalfDay;
        existingLeaveRequest.Reason = leaveRequestDto.Reason;
        existingLeaveRequest.UpdatedDate = DateTime.UtcNow;

        Expression<Func<LeaveRequest, bool>> expression = lr => lr.LeaveRequestId == leaveRequestDto.LeaveRequestId;
        result = await _leave.Update(expression, existingLeaveRequest);
        return result;
    }

    private static bool IsHoliday(DateTime date, IEnumerable<CalendarEntity> calendarEntities)
    {
        if (!calendarEntities.Any()) return false;
        foreach (var entity in calendarEntities)
        {
            if (entity.Recurring)
            {
                if (entity.Date.Day == date.Day && entity.Date.Month == date.Month)
                    return true;
            }
            else
            {
                if (entity.Date.Date == date.Date) return true;
            }
        }
        return false;
    }

    private static bool IsWeekEnd(DateTime date)
    {
        var dayofWeek = date.DayOfWeek;
        return (dayofWeek == DayOfWeek.Sunday) || (dayofWeek == DayOfWeek.Saturday);
    }

    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter? leaveFilter)
    {
        int count;
        IEnumerable<LeaveRequest> leaveRequestList = [];
        if (leaveFilter == null)
        {
            leaveRequestList = await _leave.GetAll();
            count = await _leave.Count();
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = lr => (leaveFilter.Status == null || (lr.Status == leaveFilter.Status))
            && ((leaveFilter.StartDate == null || (lr.StartDate >= leaveFilter.StartDate)) && (leaveFilter.EndDate == null || (lr.StartDate <= leaveFilter.EndDate)));

            count = await _leave.Count(whereCondition);
            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        }
        var users = await _employeeRepository.GetAll();
        var jobTitles = await _jobTitleRepo.GetAll();
        var leavePolicies = await _leavePolicyRepo.GetAll();

        List<LeaveResponseDto> FinalLeaveRequestList = (from leave in leaveRequestList
                                                        join user in users on leave.EmployeeId equals user.UserId
                                                        join reviewerGroup in users on leave.ReviewedBy equals reviewerGroup.UserId into approvedUsers
                                                        from approvedUser in approvedUsers.DefaultIfEmpty()
                                                        join job in jobTitles on user.JobRole equals job.JobTitleId into jobRolesTitles
                                                        from jobTitle in jobRolesTitles.DefaultIfEmpty()
                                                        join policy in leavePolicies on leave.LeavePolicyId equals policy.Id
                                                        select new LeaveResponseDto
                                                        {
                                                            LeaveRequestId = leave.LeaveRequestId,
                                                            EmployeeName = user.FirstName + " " + user.LastName,
                                                            EmployeeId = user.EmployeeId,
                                                            ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                            JobRole = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                                                            IsHalfDay = leave.IsHalfDay,
                                                            StartDate = leave.StartDate,
                                                            EndDate = leave.EndDate,
                                                            TotalDays = leave.TotalDays,
                                                            Reason = leave.Reason,
                                                            ReviewedBy = string.IsNullOrEmpty(leave.ReviewedBy) ? "-" : approvedUser != null ?
                                                                     approvedUser.FirstName + " " + approvedUser.LastName : "-",
                                                            Status = leave.Status,
                                                            LeavePolicyName = policy.Name,
                                                            Code = policy.Code,
                                                            Comment = leave.Comment,
                                                        }
                                                        ).ToList();

        return new Result<LeaveResponseDto>
        {
            Success = true,
            TotalRecords = count,
            MethodResults = FinalLeaveRequestList
        };
    }

    public async Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(EmpLeaveRequestFilter filter)
    {
        Result<MyLeaveRequestResponse> result = new();
        Expression<Func<LeaveRequest, bool>> whereCondition = lr =>
                lr.EmployeeId == filter.EmployeeId
            && (filter.Status == null || (lr.Status == filter.Status))
            && ((filter.StartDate == null || (lr.StartDate >= filter.StartDate)) && (filter.EndDate == null || (lr.StartDate <= filter.EndDate)));

        int count = await _leave.Count(whereCondition);
        List<LeaveRequest> myLeaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: filter.PageNo, pageSize: filter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();

        // get reviewer details
        var userIds = myLeaveRequestList.Select(lr => lr.ReviewedBy).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await _employeeRepository.GetAll(u => userIds.Contains(u.UserId));

        // get leave policy details.
        List<string> leavePolicyIds = myLeaveRequestList.Select(lr => lr.LeavePolicyId).ToList();
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp => leavePolicyIds.Contains(lp.Id));

        var userDict = users.ToDictionary(u => u.UserId, u => u);
        var responseList = myLeaveRequestList.Select(lr =>
        {
            var reviewer = !string.IsNullOrEmpty(lr.ReviewedBy) && userDict.ContainsKey(lr.ReviewedBy)
                ? userDict[lr.ReviewedBy]
                : null;
            var existingLeavePolicy = leavePolicies.FirstOrDefault(lp => lp.Id == lr.LeavePolicyId);
            return new MyLeaveRequestResponse
            {
                LeaveRequestId = lr.LeaveRequestId,
                IsHalfDay = lr.IsHalfDay,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                ReviewedBy = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "-",
                Comment = lr.Comment ?? "",
                LeavePolicyName = existingLeavePolicy?.Name,
                LeavePolicyId = existingLeavePolicy?.Id,
                Code = existingLeavePolicy?.Code,
            };
        }).ToList();

        result.MethodResults = responseList;
        result.TotalRecords = count;
        return result;
    }

    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        Result result = new();
        Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.LeaveRequestId == leaveRequestId;
        LeaveRequest? existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);
        if (existingLeaveRequest is null) return result;
        // if existing leave status is not pending 
        if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Pending) return result;

        result = await _leave.UpdateMany(whereCondition, Builders<LeaveRequest>.Update.Set(lr => lr.Status, EnumsHelper.LeaveRequestStatus.WithDrawn));
        return result;
    }

    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, LeaveRequestUpdateDto model)
    {
        Result result = new();
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.LeaveRequestId == leaveRequestId;
        var existingLeaveRequest = await _leave.FirstOrDefault(expression);

        if (existingLeaveRequest == null || model.Status == EnumsHelper.LeaveRequestStatus.Pending || model.Status == EnumsHelper.LeaveRequestStatus.WithDrawn || model.Status == existingLeaveRequest.Status) return result;

        // get employee current leave balance for requested leave type
        Expression<Func<EmployeeLeaveBalance, bool>> expression2 = lb => lb.UserId == existingLeaveRequest.EmployeeId && lb.LeavePolicyId == existingLeaveRequest.LeavePolicyId;
        var leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(expression2);
        if (leaveBalance == null) return result;

        var reviewedBy = CurrentContext.UserId(_httpContextAccessor);

        if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Pending && model.Status != EnumsHelper.LeaveRequestStatus.WithDrawn)
        {
            if (existingLeaveRequest.TotalDays > leaveBalance.Balance)
            {
                result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
                return result;
            }
            if (model.Status == EnumsHelper.LeaveRequestStatus.Accepted)
            {
                leaveBalance.Balance -= existingLeaveRequest.TotalDays;
                leaveBalance.UsedBalance += existingLeaveRequest.TotalDays;
            }
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.Status = model.Status;
        }
        else if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted && model.Status == EnumsHelper.LeaveRequestStatus.Rejected)
        {
            leaveBalance.Balance += existingLeaveRequest.TotalDays;
            leaveBalance.UsedBalance -= existingLeaveRequest.TotalDays;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.Status = model.Status;
        }
        else if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Rejected && model.Status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            leaveBalance.Balance -= existingLeaveRequest.TotalDays;
            leaveBalance.UsedBalance += existingLeaveRequest.TotalDays;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.Status = model.Status;
        }
        else
        {
            result.Success = false;
            return result;
        }

        await _leave.Update(expression, existingLeaveRequest);
        result = await _employeeLeaveBalanceRepo.Update(expression2, leaveBalance);
        if (result.Success)
        {
            LeaveNotification(existingLeaveRequest, model.Status);
        }
        return result;
    }

    public async Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary()
    {
        Result<LeaveRequestSummaryResponseDto> result = new();
        // get last 30 days leave request
        var fromDate = DateTime.UtcNow.AddDays(-30);
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.CreatedDate.HasValue && lr.CreatedDate.Value.Date >= fromDate.Date;
        var currentMonthLeaveRequest = await _leave.GetAll(expression);
        Dictionary<int, int> leaveStatusSummary = new();
        currentMonthLeaveRequest.GroupBy(lr => lr.Status).ForEach(lr =>
        {
            leaveStatusSummary.Add((int)lr.Key, lr.Count());
        });

        var leavePolicyIds = currentMonthLeaveRequest.Select(lr => lr.LeavePolicyId).Distinct().ToList();
        var leavePolicyMap = (await _leavePolicyRepo.GetAll(lp => leavePolicyIds.Contains(lp.Id)))
            .ToDictionary(lp => lp.Id, lp => lp);
        var leaveTypeSummary = currentMonthLeaveRequest.GroupBy(lr => lr.LeavePolicyId).Select(group =>
        {
            leavePolicyMap.TryGetValue(group.Key, out var leavePolicy);
            return new LeaveTypeSummary()
            {
                LeaveTypeName = leavePolicy?.Name ?? string.Empty,
                StatusValues = group.GroupBy(lr => lr.Status).ToDictionary(ls => (int)ls.Key, ls => ls.Count()),
                LeaveTypeCode = leavePolicy?.Code ?? string.Empty,
            };
        }).ToList();

        result.MethodResult = new()
        {
            TotalRequests = currentMonthLeaveRequest.Count(),
            LeaveStatusSummary = leaveStatusSummary,
            LeaveTypeSummary = leaveTypeSummary,
        };
        return result;
    }

    public async Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year)
    {
        Result<MonthlyTakenLeaveSummaryResponseDto> result = new();
        var leaveRequests = (await _leave.GetAll(lr => lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.StartDate.Year == year))
            .GroupBy(lr => lr.StartDate.Month)
            .Select(g => new MonthlyTakenLeaveSummaryResponseDto
            {
                Month = g.Key,
                TotalLeavesTaken = g.Count()
            }).ToList();

        // Fill missing months with 0
        result.MethodResults = Enumerable.Range(1, 12).Select(m => new MonthlyTakenLeaveSummaryResponseDto
        {
            Month = m,
            TotalLeavesTaken = leaveRequests.FirstOrDefault(x => x.Month == m)?.TotalLeavesTaken ?? 0
        }).ToList();

        return result;
    }


    // leave balance services
    public async Task<Result> UpdateEmployeeLeaveBalance(List<LeaveBalanceRequestDto> leaveBalanceRequestDto)
    {
        Result result = new();
        if (leaveBalanceRequestDto.Count == 0) return result;
        foreach (var balanceItem in leaveBalanceRequestDto)
        {
            if (string.IsNullOrEmpty(balanceItem.LeaveBalanceId))
            {
                // add balance 
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.Id == balanceItem.LeavePolicyId);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    return result;
                }
                if (balanceItem.Balance > leavePolicy.MaxBalance)
                {
                    result.Success = false;
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }
                var employeeBalance = new EmployeeLeaveBalance()
                {
                    UserId = balanceItem.EmployeeId,
                    LeavePolicyId = balanceItem.LeavePolicyId,
                    Balance = balanceItem.Balance,
                    UsedBalance = 0,
                };
                result = await _employeeLeaveBalanceRepo.AddOne(employeeBalance);
            }
            else
            {
                // update balnce
                var leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.Id == balanceItem.LeaveBalanceId && elb.UserId == balanceItem.EmployeeId);
                if (leaveBalance == null)
                {
                    result.Success = false;
                    return result;
                }
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.Id == leaveBalance.LeavePolicyId);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    return result;
                }
                if (balanceItem.Balance > leavePolicy.MaxBalance)
                {
                    result.Success = false;
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }

                leaveBalance.Balance = balanceItem.Balance;
                Expression<Func<EmployeeLeaveBalance, bool>> expression = lb => lb.Id == balanceItem.LeaveBalanceId;
                result = await _employeeLeaveBalanceRepo.Update(expression, leaveBalance);
            }
        }
        return result;
    }

    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        Result<EmployeeLeaveBalanceResponseDto> result = new() { Success = false };
        bool isEmpExist = await _employeeRepository.Exist(x => x.UserId == employeeId && x.Status);
        if (!isEmpExist) return result;

        // get employee balance

        IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb => elb.UserId == employeeId);
        if (!employeeLeaveBalances.Any()) return result;
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp => lp.Status);

        List<EmployeeLeaveBalanceResponseDto> empLeaveBalanceResult = (
            from empLeaveBalance in employeeLeaveBalances
            join leavePolicy in leavePolicies on empLeaveBalance.LeavePolicyId equals leavePolicy.Id
            select new EmployeeLeaveBalanceResponseDto
            {
                LeaveBalanceId = empLeaveBalance.Id,
                LeavePolicyId = leavePolicy.Id,
                Balance = empLeaveBalance.Balance,
                UsedBalance = empLeaveBalance.UsedBalance,
                Name = leavePolicy.Name,
                Code = leavePolicy.Code,
                Description = leavePolicy.Description,
                Paid = leavePolicy.Paid,
                MinNoticeDays = leavePolicy.MinNoticeDays,
                HalfDayAllowed = leavePolicy.HalfDayAllowed,
                HolidayInclusive = leavePolicy.HolidayInclusive,
                WeekendInclusive = leavePolicy.WeekendInclusive,
            }
        ).ToList();

        result.MethodResults = empLeaveBalanceResult;
        result.Success = true;
        result.TotalRecords = empLeaveBalanceResult.Count;
        return result;
    }
    public async Task<Result<AllEmployeeLeaveBalance>> GetAllEmployeeLeaveBalances(LeaveBalanceFilter filter, string companyId)
    {
        Result<AllEmployeeLeaveBalance> result = new() { Success = false };

        // get employee list based on filter
        Expression<Func<EmpUser, bool>> empExpression = string.IsNullOrEmpty(filter.EmployeeName) ? u => u.Status : u => u.Status && (u.FirstName.Contains(filter.EmployeeName, StringComparison.CurrentCultureIgnoreCase) || u.LastName.Contains(filter.EmployeeName, StringComparison.CurrentCultureIgnoreCase));

        List<EmpUser> empUsers = (await _employeeRepository.GetAggregateDataAsync<EmpUser>(empExpression, pageNo: filter.PageNo, pageSize: filter.PageSize)).ToList();

        if (!empUsers.Any()) return result;
        List<string> empIds = empUsers.Select(e => e.UserId).ToList();

        // get employee job roles id
        List<string> empJobRoleId = empUsers.Where(emp => emp.JobRole != null).Select(emp => emp.JobRole).Distinct().ToList();
        IEnumerable<JobTitles> jobTitles = await _jobTitleRepo.GetAll(jr => empJobRoleId.Contains(jr.JobTitleId));

        // get employee leave balances
        Expression<Func<EmployeeLeaveBalance, bool>> leaveBalanceExpression;
        if (filter.LeavePolicies != null && filter.LeavePolicies.Count > 0)
        {
            leaveBalanceExpression = lb => empIds.Contains(lb.UserId) && filter.LeavePolicies.Contains(lb.LeavePolicyId);
        }
        else
        {
            leaveBalanceExpression = lb => empIds.Contains(lb.UserId);
        }
        IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(leaveBalanceExpression);

        // filter employees by leave policy if filter.LeavePolicies has any element
        if (filter.LeavePolicies != null && filter.LeavePolicies.Count > 0)
        {
            var filteredEmpIds = employeeLeaveBalances.Select(lb => lb.UserId).Distinct().ToList();
            empUsers = empUsers.Where(e => filteredEmpIds.Contains(e.UserId)).ToList();
        }

        // get leave policies details
        List<string> leavePolicyIds = employeeLeaveBalances.Select(elb => elb.LeavePolicyId).Distinct().ToList();
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp => leavePolicyIds.Contains(lp.Id) && lp.CompanyId == companyId);

        var empLeaveBalanceResult = empUsers.Select(emp =>
            {
                var balances = employeeLeaveBalances.Where(lb => lb.UserId == emp.UserId).Select(lb =>
                {
                    LeavePolicy? policy = leavePolicies.FirstOrDefault(lp => lp.Id == lb.LeavePolicyId);
                    return new LeaveBalanceDetail
                    {
                        LeaveBalanceId = lb.Id,
                        LeavePolicyId = lb.LeavePolicyId,
                        LeavePolicyName = policy != null ? policy.Name : string.Empty,
                        LeavePolicyCode = policy != null ? policy.Code : string.Empty,
                        Remaining = lb.Balance,
                        UsedLeave = lb.UsedBalance
                    };
                }).ToList();

                JobTitles? jobTitle = emp.JobRole != null ? jobTitles.FirstOrDefault(jt => jt.JobTitleId == emp.JobRole) : null;

                return new AllEmployeeLeaveBalance
                {
                    EmployeeId = emp.UserId,
                    EmployeeName = emp.FirstName + " " + emp.LastName,
                    EmployeeCode = emp.EmployeeId,
                    Designation = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                    ProfilePicture = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                    LeaveBalances = balances
                };
            }).ToList();
        result.MethodResults = empLeaveBalanceResult;
        result.TotalRecords = empUsers.Count;
        result.Success = true;
        return result;
    }

    private async void LeaveNotification(LeaveRequest leaveDomain, EnumsHelper.LeaveRequestStatus status)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        Notifications notification = new()
        {
            NotificationId = Guid.NewGuid().ToString(),
            TargetId = leaveDomain.LeaveRequestId,
            CreatedDateTime = DateTime.UtcNow,
        };
        List<string> targetUserIds = [];
        if (status == EnumsHelper.LeaveRequestStatus.Pending)
        {
            string company_id = CurrentContext.CompanyId(_httpContextAccessor);
            List<string> roleIds = (await _roleRepository.GetAll(x =>
                x.CompanyId == company_id &&
                (x.RoleType == Convert.ToInt32(EnumsHelper.Roles.Administrator) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HR))
            )).Select(x => x.RolesId).ToList();
            targetUserIds = (await _employeeRepository.GetAll(x => roleIds.Contains(x.RoleId))).Select(x => x.UserId).ToList();
            if (targetUserIds.Contains(leaveDomain.EmployeeId))
            {
                targetUserIds.Remove(leaveDomain.EmployeeId);
            }
            notification.Body = await _employeeService.GetEmployeeNameById(leaveDomain.EmployeeId);
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequest;
        }
        else if (status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            if (!await _middlewareService.IsUserNotificationPreferenceEnabled(leaveDomain.EmployeeId, EnumsHelper.NotificationPreferenceType.LeaveStatusUpdate)) return;
            targetUserIds.Add(leaveDomain.EmployeeId);
            notification.Body = await _employeeService.GetEmployeeNameById(leaveDomain.ReviewedBy);
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequestApproved;
        }
        else
        {
            if (!await _middlewareService.IsUserNotificationPreferenceEnabled(leaveDomain.EmployeeId, EnumsHelper.NotificationPreferenceType.LeaveStatusUpdate)) return;
            targetUserIds.Add(leaveDomain.EmployeeId);
            notification.Body = await _employeeService.GetEmployeeNameById(leaveDomain.ReviewedBy);
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequestReject;
        }
        if (targetUserIds.Count == 0) return;
        Result result1 = await _notificationsRepo.AddOne(notification);
        if (!result1.Success) return;
        List<UserNotifications> userNotifications = [];
        var notificationTasks = new List<Task>();
        foreach (var user in targetUserIds)
        {
            UserNotifications userNotification = new()
            {
                UserNotificationId = Guid.NewGuid().ToString(),
                UserId = user,
                NotificationId = notification.NotificationId,
                IsRead = false,
                CreatedDateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            userNotifications.Add(userNotification);
            notificationTasks.Add(_notificationService.SendNotificationToUser(user, new NotificationViewModel()
            {
                Title = notification.Title,
                Body = notification.Body,
                IsRead = false,
                SentDateTime = DateTime.UtcNow,
                TargetId = notification.TargetId,
                NotificationTypes = notification.NotificationType,
                UserNotificationId = userNotification.UserNotificationId
            }));
        }
        await _userNotificationsRepo.AddMany(userNotifications);
        // process all notificatios task
        await Task.WhenAll(notificationTasks);
    }

    public async Task EmployeeLeaveBalanceAccrual()
    {
        // get all active leavePolicy with accrual monthly or yearly
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            lp.Status && (lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly),
            withDefaultFilter: false
        );
        if (!leavePolicies.Any()) return;
        foreach (var policy in leavePolicies)
        {
            // get all active employees with leave policy
            IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb => elb.LeavePolicyId == policy.Id, withDefaultFilter: false);
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly && employeeLeaveBalances.Any())
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    // check if leave is already credited for this month 
                    if (empLeave.LastAccrual?.Date != DateTime.UtcNow.Date)
                    {
                        var updatedBalance = empLeave.Balance + policy.AccrualAmount;
                        empLeave.Balance = policy.MaxBalance.HasValue ? Math.Min(updatedBalance, policy.MaxBalance.Value) : updatedBalance;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.Id == empLeave.Id;
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Set(b => b.Balance, empLeave.Balance)
                        .Set(b => b.UsedBalance, 0)
                        .Set(b => b.LastAccrual, DateTime.UtcNow)
                        .Set(b => b.UpdatedDate, DateTime.UtcNow));
                    }
                }
            }
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly && DateTime.UtcNow.Month == 1)
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    if (empLeave.LastAccrual?.Date != DateTime.UtcNow.Date)
                    {
                        var updatedBalance = policy.CarryOverAllowed ? (Math.Min(empLeave.Balance, policy.CarryOverLimit.Value) + policy.AccrualAmount) : policy.AccrualAmount;
                        empLeave.Balance = policy.MaxBalance.HasValue ? Math.Min(updatedBalance, policy.MaxBalance.Value) : updatedBalance;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.Id == empLeave.Id;
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Set(b => b.Balance, empLeave.Balance)
                        .Set(b => b.UsedBalance, 0)
                        .Set(b => b.LastAccrual, DateTime.UtcNow)
                        .Set(b => b.UpdatedDate, DateTime.UtcNow));
                    }
                }
            }
        }
    }
}

