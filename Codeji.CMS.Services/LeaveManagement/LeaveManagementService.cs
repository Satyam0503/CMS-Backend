using System.Linq.Expressions;
using System.Net;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
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
    private readonly ICompanyWorkingCalendarService _workingCalendar;
    private readonly IMongoDbRepository<AttendanceStatusSetting> _attendanceStatusRepo;
    private readonly IMongoDbRepository<WorkFromHomePolicy> _workFromHomePolicies;
    private readonly ILeaveAttendanceReconciliationService _leaveAttendanceReconciliation;
    private readonly IMongoClient _mongoClient;
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
    ICompanyWorkingCalendarService workingCalendar,
    IMongoDbRepository<AttendanceStatusSetting> attendanceStatusRepo,
    ILeaveAttendanceReconciliationService leaveAttendanceReconciliation,
    IMongoDbRepository<WorkFromHomePolicy> workFromHomePolicies,
    IMongoClient mongoClient
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
        _workingCalendar = workingCalendar;
        _attendanceStatusRepo = attendanceStatusRepo;
        _leaveAttendanceReconciliation = leaveAttendanceReconciliation;
        _workFromHomePolicies = workFromHomePolicies;
        _mongoClient = mongoClient;
    }

    // updated services
    public async Task<Result> CreateNewLeavePolicy(LeavePolicyRequest leavePolicyDto, string company_id)
    {
        Result result = new();
        if (!await ValidatePolicyShape(company_id, leavePolicyDto.PolicyType, leavePolicyDto.AttendanceStatusCode, leavePolicyDto.WorkFromHome))
            return new Result { Success = false, Message = "LEAVE_POLICY_CONFIGURATION_INVALID: The policy type and attendance configuration are not valid for this company." };
        if (leavePolicyDto.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome &&
            await _workFromHomePolicies.Exist(x => x.CompanyId == company_id && !string.IsNullOrEmpty(x.LeavePolicyId)))
            return new Result { Success = false, Message = "WFH_POLICY_ALREADY_CONFIGURED: This company already has a typed WFH policy. Update that policy instead." };
        // check if policy already exist;
        leavePolicyDto.Name = leavePolicyDto.Name.Trim();
        leavePolicyDto.Code = leavePolicyDto.Code.Trim().ToUpperInvariant();
        // WFH is attendance work, not unpaid leave. Keep the policy flag aligned
        // with the WFH attendance status so payroll and the policy UI agree.
        if (leavePolicyDto.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            leavePolicyDto.Paid = true;
        // A leave policy may be created before its attendance status has been configured.
        // The mapping is optional and is checked again when a leave request is approved,
        // which prevents an invalid mapping from creating attendance while allowing HR to
        // set up the policy independently of Attendance Settings.
        leavePolicyDto.AttendanceStatusCode = string.IsNullOrWhiteSpace(leavePolicyDto.AttendanceStatusCode)
            ? null
            : leavePolicyDto.AttendanceStatusCode.Trim().ToUpperInvariant();
        var exist = await _leavePolicyRepo.Exist(lp => lp.CompanyId == company_id &&
            (lp.Name.Equals(leavePolicyDto.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code == leavePolicyDto.Code));
        if (exist)
        {
            result.StatusCode = CustomStatusCode.LeavePolicyAlreadyExist;
            return result;
        }
        LeavePolicy leavePolicy = _mapper.Map<LeavePolicy>(leavePolicyDto);
        leavePolicy.Id = Guid.NewGuid().ToString();
        leavePolicy.CompanyId = company_id;
        leavePolicy.NormalizedName = leavePolicyDto.Name.ToUpperInvariant();
        leavePolicy.NormalizedCode = leavePolicyDto.Code;
        leavePolicy.AttendanceStatusCode = leavePolicyDto.AttendanceStatusCode;

        // insert new leave policy
        result = await _leavePolicyRepo.AddOne(leavePolicy);
        if (result.Success && leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
        {
            var sync = await SyncWorkFromHomePolicy(company_id, leavePolicy, leavePolicyDto.WorkFromHome!);
            if (!sync.Success) return sync;
        }

        // WFH is operational and does not own a leave balance. Normal leave policies
        // must synchronise employee eligibility immediately, including "All Employees".
        if (leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.Leave && result.Success)
            return await SynchronizeLeavePolicyEligibility(company_id, leavePolicy, leavePolicyDto.ApplicableTo);
        return result;
    }

    public async Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy(UpdateLeavePolicyRequest model)
    {
        Result<UpdateLeavePolicyRequest> result = new() { Success = false };
        // check if leave policy exits or not;
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeavePolicy, bool>> expression = lp => lp.CompanyId == companyId && lp.Id == model.Id;
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(expression);
        if (leavePolicy == null) return result;

        if (leavePolicy.PolicyType != model.PolicyType)
        {
            var hasBalances = await _employeeLeaveBalanceRepo.Exist(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
            var hasRequests = await _leave.Exist(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
            if (hasBalances || hasRequests)
                return new Result<UpdateLeavePolicyRequest> { Success = false, Message = "LEAVE_POLICY_TYPE_IMMUTABLE: Create a new policy instead of changing the type of a policy that already has balances or requests." };
        }

        model.Name = model.Name.Trim();
        model.Code = model.Code.Trim().ToUpperInvariant();
        if (model.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            model.Paid = true;
        if (!await ValidatePolicyShape(companyId, model.PolicyType, model.AttendanceStatusCode, model.WorkFromHome))
        {
            result.Message = "LEAVE_POLICY_CONFIGURATION_INVALID: The policy type and attendance configuration are not valid for this company.";
            return result;
        }
        bool duplicateLeavePolicy = await _leavePolicyRepo.Exist(lp => lp.CompanyId == companyId && (lp.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code == model.Code) && lp.Id != model.Id);
        if (duplicateLeavePolicy)
        {
            result.StatusCode = CustomStatusCode.DuplicationLeavePolicy;
            return result;
        }
        LeavePolicy updatedPolicyModel = _mapper.Map<LeavePolicy>(model);
        updatedPolicyModel.CompanyId = companyId;
        updatedPolicyModel.NormalizedName = model.Name.ToUpperInvariant();
        updatedPolicyModel.NormalizedCode = model.Code;
        updatedPolicyModel.AttendanceStatusCode = model.AttendanceStatusCode?.Trim().ToUpperInvariant();
        updatedPolicyModel.CreatedBy = leavePolicy.CreatedBy;
        updatedPolicyModel.CreatedDate = leavePolicy.CreatedDate;
        updatedPolicyModel.ApplicableTo = model.ApplicableTo?.ToList();
        var updateResult = await _leavePolicyRepo.Update(expression, updatedPolicyModel);
        result.Success = updateResult.Success;
        if (result.Success)
        {
            if (updatedPolicyModel.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            {
                var sync = await SyncWorkFromHomePolicy(companyId, updatedPolicyModel, model.WorkFromHome!);
                if (!sync.Success) return new Result<UpdateLeavePolicyRequest> { Success = false, Message = sync.Message };
            }
            else
            {
                var sync = await SynchronizeLeavePolicyEligibility(companyId, updatedPolicyModel, model.ApplicableTo);
                if (!sync.Success) return new Result<UpdateLeavePolicyRequest> { Success = false, Message = sync.Message };
            }
            result.MethodResult = model;
        }
        return result;
    }

    private async Task<Result> SynchronizeLeavePolicyEligibility(string companyId, LeavePolicy policy, string[]? applicableTo)
    {
        // null explicitly means no employees; an empty list means every active employee.
        var activeUserIds = (await _employeeRepository.GetAll(emp => emp.Status && emp.CompanyId == companyId))
            .Select(emp => emp.UserId)
            .Distinct()
            .ToHashSet();
        var eligibleUserIds = applicableTo == null
            ? new HashSet<string>()
            : applicableTo.Length == 0
                ? activeUserIds
                : applicableTo.Where(activeUserIds.Contains).ToHashSet();
        var existing = (await _employeeLeaveBalanceRepo.GetAll(lb => lb.CompanyId == companyId && lb.LeavePolicyId == policy.Id, withDefaultFilter: false)).ToList();
        var existingUserIds = existing.Select(lb => lb.UserId).ToHashSet();
        var now = DateTime.UtcNow;

        var additions = eligibleUserIds.Except(existingUserIds).Select(userId => new EmployeeLeaveBalance
        {
            CompanyId = companyId,
            UserId = userId,
            LeavePolicyId = policy.Id,
            TotalAllocated = policy.AccrualAmount,
            Taken = 0,
            Remaining = policy.AccrualAmount,
            IsManualAllocation = false,
            LastAccrual = now
        }).ToList();
        if (additions.Count > 0)
        {
            var addResult = await _employeeLeaveBalanceRepo.AddMany(additions);
            if (!addResult.Success) return addResult;
        }

        // Deallocation must not erase an approved-leave audit trail. Existing consumption is
        // retained, while remaining entitlement is removed. Untouched balances can be deleted.
        foreach (var balance in existing.Where(lb => !eligibleUserIds.Contains(lb.UserId)))
        {
            var filter = Builders<EmployeeLeaveBalance>.Filter.Where(lb => lb.CompanyId == companyId && lb.Id == balance.Id);
            if (balance.Taken > 0)
            {
                var taken = balance.Taken;
                var updateResult = await _employeeLeaveBalanceRepo.UpdateMany(filter, Builders<EmployeeLeaveBalance>.Update
                    .Set(lb => lb.TotalAllocated, taken).Set(lb => lb.Taken, taken).Set(lb => lb.Remaining, 0).Inc(lb => lb.Version, 1));
                if (!updateResult.Success) return updateResult;
            }
            else
            {
                var deleteResult = await _employeeLeaveBalanceRepo.Delete(filter);
                if (!deleteResult.Success) return deleteResult;
            }
        }

        return new Result { Success = true };
    }

    public async Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies(string companyId, bool? status)
    {
        Expression<Func<LeavePolicy, bool>> expression = status.HasValue ? lp => lp.CompanyId == companyId && lp.Status == status.Value : lp => lp.CompanyId == companyId;
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(expression);
        var list = _mapper.Map<List<UpdateLeavePolicyRequest>>(leavePolicies);
        foreach (var policy in list.Where(x => x.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome))
            policy.Paid = true;
        return new Result<UpdateLeavePolicyRequest>()
        {
            Success = true,
            MethodResults = list
        };
    }

    public async Task<Result> CreateLeaveRequest(LeaveRequestDto leaveRequest)
    {
        Result result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveRequest.LeavePolicyId && lp.Status);
        if (leavePolicy is null) return result;
        if (leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            return new Result { Success = false, Message = "WFH_POLICY_REQUIRES_WFH_REQUEST: Submit Work From Home through the WFH request workflow so its attendance and clocking rules are enforced." };
        if (!TryNormalizeLeaveRequestDates(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay, leavePolicy.HalfDayAllowed,
            out var requestStartDate, out var requestEndDate, out var validationError))
        {
            return new Result { Success = false, Message = validationError };
        }
        var requestStartDateTime = UtcMidnight(requestStartDate);
        var requestEndDateTime = UtcMidnight(requestEndDate);
        var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == leaveRequest.UserId && x.Status && !x.IsDeleted);
        if (employee == null) return new Result { Success = false, Message = "Employee does not belong to the authenticated company." };

        // check if employee has balance for requested leave type
        var employeeLeaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.CompanyId == companyId && elb.UserId == leaveRequest.UserId && elb.LeavePolicyId == leaveRequest.LeavePolicyId);
        if (employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }

        var activeRequests = await _leave.GetAll(lr => lr.CompanyId == companyId && lr.EmployeeId == leaveRequest.UserId &&
            (lr.Status == EnumsHelper.LeaveRequestStatus.Pending || lr.Status == EnumsHelper.LeaveRequestStatus.Accepted));
        if (activeRequests.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate))
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        // check if employee already has approved leave on request date
        var nowDate = DateTime.UtcNow.Date;
        var upcomingApprovedLeaves = await _leave.GetAll(lr => lr.EmployeeId == leaveRequest.UserId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.EndDate >= nowDate);
        bool isOverlapping = upcomingApprovedLeaves.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        // validate min advance notice day
        var totalAdvanceNoticeDays = (requestStartDateTime - DateTime.UtcNow.Date).TotalDays + 1;
        if (leavePolicy.MinNoticeDays > 0 && leavePolicy.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        var leaveDates = await _workingCalendar.GetLeaveDatesAsync(companyId,
            requestStartDate, requestEndDate,
            leavePolicy.WeekendInclusive, leavePolicy.HolidayInclusive);
        decimal totalRequestedDays = leaveDates.Count;
        if (leaveRequest.IsHalfDay)
        {
            if (leaveDates.Count != 1)
                return new Result { Success = false, Message = "Half-day leave must fall on an included business day according to the selected policy." };
            totalRequestedDays = 0.5m;
        }

        if (totalRequestedDays <= 0)
            return new Result { Success = false, Message = "Leave duration must be greater than zero." };
        if (!IsValidBalance(employeeLeaveBalance) || totalRequestedDays > employeeLeaveBalance.Remaining)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            return result;
        }

        var request = new LeaveRequest
        {
            EmployeeId = leaveRequest.UserId,
            LeavePolicyId = leaveRequest.LeavePolicyId,
            StartDate = requestStartDateTime,
            EndDate = requestEndDateTime,
            TotalDays = totalRequestedDays,
            IsHalfDay = leavePolicy.HalfDayAllowed && leaveRequest.IsHalfDay,
            HalfDayPeriod = leaveRequest.IsHalfDay ? NormalizeHalfDayPeriod(leaveRequest.HalfDayPeriod) : null,
            Reason = leaveRequest.Reason,
            ReviewedBy = "",
            Status = EnumsHelper.LeaveRequestStatus.Pending
        };

        result = await _leave.AddOne(request);
        if (result.Success)
        {
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            // An HR/Admin-created request is an administrative entry, not an employee
            // submission. Do not email the same review group about a request it just made.
            await LeaveNotification(request, request.Status, companyId, actorUserId,
                sendEmail: actorUserId == request.EmployeeId);
        }
        return result;
    }

    public async Task<Result> UpdateLeaveRequest(UpdateLeaveRequestDto leaveRequestDto)
    {
        Result result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leavePolicyEntity = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveRequestDto.LeavePolicyId && lp.Status);
        if (leavePolicyEntity is null) return result;
        if (leavePolicyEntity.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            return new Result { Success = false, Message = "WFH_POLICY_REQUIRES_WFH_REQUEST: WFH requests must use the WFH workflow." };
        if (!TryNormalizeLeaveRequestDates(leaveRequestDto.StartDate, leaveRequestDto.EndDate, leaveRequestDto.IsHalfDay, leavePolicyEntity.HalfDayAllowed,
            out var requestStartDate, out var requestEndDate, out var validationError))
        {
            return new Result { Success = false, Message = validationError };
        }
        var requestStartDateTime = UtcMidnight(requestStartDate);
        var requestEndDateTime = UtcMidnight(requestEndDate);

        var existingLeaveRequest = await _leave.FirstOrDefault(lr => lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestDto.LeaveRequestId && leaveRequestDto.UserId == lr.EmployeeId);
        if (existingLeaveRequest is null) return result;
        if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Pending) return result;

        var employeeLeaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.CompanyId == companyId && elb.UserId == leaveRequestDto.UserId && elb.LeavePolicyId == leaveRequestDto.LeavePolicyId);
        if (employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }

        var activeRequests = await _leave.GetAll(lr => lr.CompanyId == companyId && lr.EmployeeId == leaveRequestDto.UserId &&
            lr.LeaveRequestId != leaveRequestDto.LeaveRequestId &&
            (lr.Status == EnumsHelper.LeaveRequestStatus.Pending || lr.Status == EnumsHelper.LeaveRequestStatus.Accepted));
        bool isOverlapping = activeRequests.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        var totalAdvanceNoticeDays = (requestStartDateTime - DateTime.UtcNow.Date).TotalDays + 1;
        if (leavePolicyEntity.MinNoticeDays > 0 && leavePolicyEntity.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        var leaveDates = await _workingCalendar.GetLeaveDatesAsync(companyId,
            requestStartDate, requestEndDate,
            leavePolicyEntity.WeekendInclusive, leavePolicyEntity.HolidayInclusive);
        decimal totalRequestedDays = leaveDates.Count;
        if (leaveRequestDto.IsHalfDay)
        {
            if (leaveDates.Count != 1)
                return new Result { Success = false, Message = "Half-day leave must fall on an included business day according to the selected policy." };
            totalRequestedDays = 0.5m;
        }

        if (totalRequestedDays <= 0)
            return new Result { Success = false, Message = "Leave duration must be greater than zero." };
        if (!IsValidBalance(employeeLeaveBalance) || totalRequestedDays > employeeLeaveBalance.Remaining)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            return result;
        }

        existingLeaveRequest.LeavePolicyId = leaveRequestDto.LeavePolicyId;
        existingLeaveRequest.StartDate = requestStartDateTime;
        existingLeaveRequest.EndDate = requestEndDateTime;
        existingLeaveRequest.TotalDays = totalRequestedDays;
        existingLeaveRequest.IsHalfDay = leavePolicyEntity.HalfDayAllowed && leaveRequestDto.IsHalfDay;
        existingLeaveRequest.HalfDayPeriod = leaveRequestDto.IsHalfDay ? NormalizeHalfDayPeriod(leaveRequestDto.HalfDayPeriod) : null;
        existingLeaveRequest.Reason = leaveRequestDto.Reason;
        existingLeaveRequest.UpdatedDate = DateTime.UtcNow;

        Expression<Func<LeaveRequest, bool>> expression = lr => lr.LeaveRequestId == leaveRequestDto.LeaveRequestId;
        result = await _leave.Update(expression, existingLeaveRequest);
        return result;
    }

    private static string NormalizeHalfDayPeriod(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (normalized is "FIRST_HALF" or "SECOND_HALF") return normalized;
        // Existing half-day leave records predate segment ownership.  Preserve a
        // stable backwards-compatible interpretation instead of guessing later.
        return "FIRST_HALF";
    }

    internal static bool TryNormalizeLeaveRequestDates(DateTime start, DateTime end, bool isHalfDay, bool halfDayAllowed,
        out DateOnly requestStartDate, out DateOnly requestEndDate, out string? validationError)
    {
        requestStartDate = DateOnly.FromDateTime(start.Date);
        requestEndDate = DateOnly.FromDateTime(end.Date);
        validationError = null;

        if (isHalfDay)
        {
            if (!halfDayAllowed)
            {
                validationError = "Half-day leave is not allowed for the selected policy.";
                return false;
            }
            if (requestStartDate != requestEndDate)
            {
                validationError = "Half-day leave requires a single date.";
                return false;
            }
        }
        else if (requestEndDate < requestStartDate)
        {
            validationError = "End date cannot be before start date.";
            return false;
        }

        return true;
    }

    internal static DateTime UtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter? leaveFilter)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        int count;
        IEnumerable<LeaveRequest> leaveRequestList = [];
        if (leaveFilter == null)
        {
            leaveRequestList = await _leave.GetAll(lr => lr.CompanyId == companyId);
            count = await _leave.Count(lr => lr.CompanyId == companyId);
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.CompanyId == companyId && (leaveFilter.Status == null || (lr.Status == leaveFilter.Status))
            && ((leaveFilter.StartDate == null || (lr.StartDate >= leaveFilter.StartDate)) && (leaveFilter.EndDate == null || (lr.StartDate <= leaveFilter.EndDate)));

            count = await _leave.Count(whereCondition);
            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        }
        var users = await _employeeRepository.GetAll(u => u.CompanyId == companyId);
        var jobTitles = await _jobTitleRepo.GetAll(j => j.CompanyId == companyId);
        var leavePolicies = await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId);

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
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> whereCondition = lr =>
                lr.CompanyId == companyId && lr.EmployeeId == filter.EmployeeId
            && (filter.Status == null || (lr.Status == filter.Status))
            && ((filter.StartDate == null || (lr.StartDate >= filter.StartDate)) && (filter.EndDate == null || (lr.StartDate <= filter.EndDate)));

        int count = await _leave.Count(whereCondition);
        List<LeaveRequest> myLeaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: filter.PageNo, pageSize: filter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();

        // get reviewer details
        var userIds = myLeaveRequestList.Select(lr => lr.ReviewedBy).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await _employeeRepository.GetAll(u => u.CompanyId == companyId && userIds.Contains(u.UserId));

        // get leave policy details.
        List<string> leavePolicyIds = myLeaveRequestList.Select(lr => lr.LeavePolicyId).ToList();
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId && leavePolicyIds.Contains(lp.Id));

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
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var userId = CurrentContext.UserId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.CompanyId == companyId && lr.EmployeeId == userId && lr.LeaveRequestId == leaveRequestId;
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
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestId;
        var existingLeaveRequest = await _leave.FirstOrDefault(expression);

        if (existingLeaveRequest == null)
            return new Result { Success = false, Message = "Leave request was not found in the authenticated company." };
        var reviewedBy = CurrentContext.UserId(_httpContextAccessor);
        if (existingLeaveRequest.EmployeeId == reviewedBy)
            return new Result { Success = false, Message = "LEAVE_SELF_APPROVAL_NOT_ALLOWED: A user cannot approve their own leave request." };
        if (model.ExpectedVersion.HasValue && model.ExpectedVersion.Value != existingLeaveRequest.Version)
            return new Result { Success = false, Message = "LEAVE_REQUEST_VERSION_CONFLICT: The leave request was changed by another reviewer. Refresh and try again." };
        if (model.Status == EnumsHelper.LeaveRequestStatus.Pending || model.Status == EnumsHelper.LeaveRequestStatus.WithDrawn || model.Status == existingLeaveRequest.Status)
            return new Result { Success = false, Message = "The selected leave status transition is not allowed." };

        var decisionCommentError = ValidateLeaveDecisionComment(model);
        if (decisionCommentError != null)
            return new Result { Success = false, Message = decisionCommentError };

        bool debitBalance = model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            existingLeaveRequest.Status is EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Rejected;
        bool creditBalance = existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            model.Status == EnumsHelper.LeaveRequestStatus.Rejected;
        bool statusOnly = existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Pending &&
            model.Status == EnumsHelper.LeaveRequestStatus.Rejected;
        if (!debitBalance && !creditBalance && !statusOnly)
            return new Result { Success = false, Message = "The selected leave status transition is not allowed." };

        EmployeeLeaveBalance? leaveBalance = null;
        if (debitBalance || creditBalance)
        {
            leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(lb =>
                lb.CompanyId == companyId && lb.UserId == existingLeaveRequest.EmployeeId && lb.LeavePolicyId == existingLeaveRequest.LeavePolicyId);
            if (leaveBalance == null)
                return new Result { Success = false, Message = "No leave balance was found for this employee and leave policy." };
        }

        // Validate before changing the request. Otherwise approval could be committed while
        // leave-to-attendance reconciliation later fails because the policy is incomplete.
        if (debitBalance)
        {
            if (IsPastDatedLeaveRequest(existingLeaveRequest, DateTime.UtcNow))
                return new Result { Success = false, Message = "Leave requests for dates that have already passed cannot be approved." };

            var policy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == existingLeaveRequest.LeavePolicyId && lp.Status);
            if (policy == null)
                return new Result { Success = false, Message = "The leave policy for this request is not active." };
            if (string.IsNullOrWhiteSpace(policy.AttendanceStatusCode) ||
                !await ValidateAttendanceStatusMapping(companyId, policy.AttendanceStatusCode))
                return new Result { Success = false, Message = "The leave policy must have an active attendance status code that does not require check-in or check-out times before this request can be approved." };
        }

        var now = DateTime.UtcNow;
        var leaveFilter = Builders<LeaveRequest>.Filter.Where(lr =>
            lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestId &&
            lr.Status == existingLeaveRequest.Status && lr.Version == existingLeaveRequest.Version);
        var leaveUpdate = Builders<LeaveRequest>.Update
            .Set(lr => lr.ReviewedBy, reviewedBy)
            .Set(lr => lr.Comment, model.Comment)
            .Set(lr => lr.Status, model.Status)
            .Set(lr => lr.ReviewedAt, now)
            .Set(lr => lr.UpdatedDate, now)
            .Inc(lr => lr.Version, 1);

        if (debitBalance)
            leaveUpdate = leaveUpdate
                .Set(lr => lr.ReconciliationStatus, "Pending")
                .Set(lr => lr.ReconciliationErrorCode, null)
                .Set(lr => lr.ReconciliationErrorMessage, null)
                .Set(lr => lr.NextReconciliationAttemptAtUtc, now);
        else if (creditBalance)
            leaveUpdate = leaveUpdate
                .Set(lr => lr.ReconciliationStatus, "ReversalPending")
                .Set(lr => lr.ReconciliationErrorCode, null)
                .Set(lr => lr.ReconciliationErrorMessage, null)
                .Set(lr => lr.NextReconciliationAttemptAtUtc, now);

        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();

            if (debitBalance || creditBalance)
            {
                var balanceFilter = Builders<EmployeeLeaveBalance>.Filter.Where(lb =>
                    lb.CompanyId == companyId && lb.Id == leaveBalance!.Id &&
                    lb.Version == leaveBalance.Version &&
                    (!debitBalance || lb.Remaining >= existingLeaveRequest.TotalDays) &&
                    (!creditBalance || lb.Taken >= existingLeaveRequest.TotalDays));
                decimal remainingChange = debitBalance ? -existingLeaveRequest.TotalDays : existingLeaveRequest.TotalDays;
                decimal takenChange = -remainingChange;
                var balanceUpdate = Builders<EmployeeLeaveBalance>.Update
                    .Inc(lb => lb.Remaining, remainingChange)
                    .Inc(lb => lb.Taken, takenChange)
                    .Inc(lb => lb.Version, 1)
                    .Set(lb => lb.UpdatedDate, now);
                var balanceResult = await _employeeLeaveBalanceRepo.GetCollection()
                    .UpdateOneAsync(session, balanceFilter, balanceUpdate);
                if (balanceResult.ModifiedCount != 1)
                {
                    await session.AbortTransactionAsync();
                    result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
                    return result;
                }
            }

            var leaveResult = await _leave.GetCollection().UpdateOneAsync(session, leaveFilter, leaveUpdate);
            if (leaveResult.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync();
                result.Message = "The leave request was changed by another reviewer. Refresh and try again.";
                return result;
            }

            await session.CommitTransactionAsync();
            existingLeaveRequest.Status = model.Status;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.ReviewedAt = now;
            existingLeaveRequest.Version++;
            existingLeaveRequest.ReconciliationStatus = debitBalance ? "Pending" : creditBalance ? "ReversalPending" : existingLeaveRequest.ReconciliationStatus;
            result.Success = true;
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            // Local standalone MongoDB does not support transactions. This fallback keeps
            // compare-and-set protection and compensates the balance when the leave write
            // loses its optimistic-concurrency race. Production must use a replica set.
            result = await ApplyLeaveStatusWithoutTransactionAsync(
                companyId, existingLeaveRequest, leaveBalance, leaveFilter, leaveUpdate,
                debitBalance, creditBalance, now);
            if (!result.Success) return result;
            existingLeaveRequest.Status = model.Status;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.ReviewedAt = now;
            existingLeaveRequest.Version++;
        }
        catch (MongoException)
        {
            return new Result { Success = false, Message = "Leave update could not be completed. Please retry or contact an administrator." };
        }

        if (result.Success && model.Status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            var reconciliation = await _leaveAttendanceReconciliation.ReconcileAcceptedLeaveAsync(companyId, leaveRequestId, reviewedBy);
            // The decision and the reconciliation have separate durable states.
            // A retryable attendance failure must never turn a committed approval
            // into a false rejection or silently look like a completed sync.
            if (!reconciliation.Success)
            {
                await MarkReconciliationRetryAsync(companyId, leaveRequestId, reconciliation.Message);
                result = new Result { Success = true, Message = "Leave approved. Attendance synchronization is pending retry." };
            }
        }
        else if (result.Success && model.Status == EnumsHelper.LeaveRequestStatus.Rejected)
        {
            result = await _leaveAttendanceReconciliation.ReverseLeaveAttendanceAsync(companyId, leaveRequestId, existingLeaveRequest.Version, reviewedBy);
        }
        if (result.Success)
        {
            await LeaveNotification(existingLeaveRequest, model.Status, companyId, reviewedBy);
        }
        return result;
    }

    internal static bool IsValidBalance(EmployeeLeaveBalance balance) =>
        balance.TotalAllocated >= 0 && balance.Taken >= 0 && balance.Remaining >= 0 &&
        balance.TotalAllocated == balance.Taken + balance.Remaining;

    internal static bool IsPastDatedLeaveRequest(LeaveRequest request, DateTime currentDate)
    {
        var currentDay = currentDate.Date;
        return request.StartDate.Date < currentDay || request.EndDate.Date < currentDay;
    }

    internal static string? ValidateLeaveDecisionComment(LeaveRequestUpdateDto model)
    {
        if (model.Status != EnumsHelper.LeaveRequestStatus.Rejected)
            return null;

        return string.IsNullOrWhiteSpace(model.Comment)
            ? "Rejection requires a reason."
            : null;
    }

    private Task MarkReconciliationRetryAsync(string companyId, string leaveRequestId, string? message)
    {
        var now = DateTime.UtcNow;
        return _leave.GetCollection().UpdateOneAsync(
            x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId,
            Builders<LeaveRequest>.Update
                .Set(x => x.ReconciliationStatus, "RetryScheduled")
                .Set(x => x.ReconciliationErrorCode, "LEAVE_RECONCILIATION_FAILED")
                .Set(x => x.ReconciliationErrorMessage, message)
                .Set(x => x.LastReconciliationAttemptAtUtc, now)
                .Set(x => x.NextReconciliationAttemptAtUtc, now.AddMinutes(5))
                .Inc(x => x.ReconciliationAttempts, 1));
    }

    private static bool IsTransactionUnsupported(Exception exception) =>
        exception is NotSupportedException ||
        exception.Message.Contains("Transaction numbers are only allowed", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("replica set", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("standalone servers do not support transactions", StringComparison.OrdinalIgnoreCase);

    private async Task<Result> ApplyLeaveStatusWithoutTransactionAsync(
        string companyId,
        LeaveRequest leaveRequest,
        EmployeeLeaveBalance? leaveBalance,
        FilterDefinition<LeaveRequest> leaveFilter,
        UpdateDefinition<LeaveRequest> leaveUpdate,
        bool debitBalance,
        bool creditBalance,
        DateTime now)
    {
        var result = new Result { Success = false };
        var balanceChanged = false;
        decimal balanceChange = debitBalance ? -leaveRequest.TotalDays : leaveRequest.TotalDays;

        if (debitBalance || creditBalance)
        {
            var balanceFilter = Builders<EmployeeLeaveBalance>.Filter.Where(lb =>
                lb.CompanyId == companyId && lb.Id == leaveBalance!.Id &&
                lb.Version == leaveBalance.Version &&
                (!debitBalance || lb.Remaining >= leaveRequest.TotalDays) &&
                (!creditBalance || lb.Taken >= leaveRequest.TotalDays));
            var takenChange = -balanceChange;
            var balanceUpdate = Builders<EmployeeLeaveBalance>.Update
                .Inc(lb => lb.Remaining, balanceChange)
                .Inc(lb => lb.Taken, takenChange)
                .Inc(lb => lb.Version, 1)
                .Set(lb => lb.UpdatedDate, now);
            var balanceResult = await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(balanceFilter, balanceUpdate);
            if (balanceResult.ModifiedCount != 1)
                return new Result { Success = false, Message = debitBalance ? "The employee no longer has sufficient leave balance. Refresh and try again." : "The leave balance could not be restored. Please contact an administrator." };
            balanceChanged = true;
        }

        var leaveResult = await _leave.GetCollection().UpdateOneAsync(leaveFilter, leaveUpdate);
        if (leaveResult.ModifiedCount == 1)
            return new Result { Success = true };

        if (balanceChanged)
        {
            var compensation = Builders<EmployeeLeaveBalance>.Update
                .Inc(lb => lb.Remaining, -balanceChange)
                .Inc(lb => lb.Taken, balanceChange)
                .Set(lb => lb.UpdatedDate, DateTime.UtcNow);
            await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(
                Builders<EmployeeLeaveBalance>.Filter.Where(lb => lb.CompanyId == companyId && lb.Id == leaveBalance!.Id), compensation);
        }

        return new Result { Success = false, Message = "The leave request was changed by another reviewer. Refresh and try again." };
    }

    private async Task<bool> ValidateAttendanceStatusMapping(string companyId, string? code)
    {
        // Existing policies without a mapping remain readable/editable. New approvals
        // cannot reconcile until HR assigns a valid no-time attendance status.
        if (string.IsNullOrWhiteSpace(code)) return true;
        var normalized = code.Trim().ToUpperInvariant();
        return await _attendanceStatusRepo.Exist(x => x.CompanyId == companyId && x.Code == normalized && x.IsActive && !x.RequiresTime && x.IsAvailableForLeaveManagement);
    }

    private async Task<Result> SyncWorkFromHomePolicy(string companyId, LeavePolicy leavePolicy, WorkFromHomePolicySettingsRequest settings)
    {
        var existing = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
        if (existing is null)
        {
            var otherTypedPolicy = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && !string.IsNullOrEmpty(x.LeavePolicyId) && x.LeavePolicyId != leavePolicy.Id);
            if (otherTypedPolicy is not null)
                return new Result { Success = false, Message = "WFH_POLICY_ALREADY_CONFIGURED: This company already has a typed WFH policy. Update that policy instead of creating a second active WFH workflow." };
            existing = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.LeavePolicyId)) ?? new WorkFromHomePolicy { CompanyId = companyId };
        }

        existing.LeavePolicyId = leavePolicy.Id;
        existing.IsEnabled = leavePolicy.Status;
        existing.ManagerApprovalRequired = settings.ApprovalRequired;
        existing.RequireReason = settings.ReasonRequired;
        existing.RequireAttachment = settings.AttachmentRequired;
        existing.MaxDaysPerWeek = settings.MaxDaysPerWeek ?? 1;
        existing.MaxDaysPerMonth = settings.MaxDaysPerMonth;
        existing.AllowHalfDay = settings.AllowFirstHalf || settings.AllowSecondHalf;
        existing.AllowMixedDay = settings.AllowMixedHalfDayLeave;
        existing.AllowOnWeeklyOff = settings.AllowOnWeeklyOff;
        existing.AllowOnHoliday = settings.AllowOnHoliday;
        existing.EffectiveFrom = null;
        existing.EffectiveTo = null;
        existing.ApplyToAllEmployees = true;
        var selectedUserIds = leavePolicy.ApplicableTo;
        if (selectedUserIds is { Count: > 0 })
        {
            existing.ApplyToAllEmployees = false;
            existing.ApplicableEmployeeIds = (await _employeeRepository.GetAll(
                employee => employee.CompanyId == companyId && employee.Status && selectedUserIds.Contains(employee.UserId)))
                .Select(employee => employee.EmployeeId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
        else
        {
            existing.ApplicableEmployeeIds = [];
        }
        existing.FullDayAttendanceStatusCode = settings.FullDayAttendanceStatusCode.Trim().ToUpperInvariant();
        existing.HalfDayAttendanceStatusCode = settings.HalfDayAttendanceStatusCode.Trim().ToUpperInvariant();
        existing.MixedAttendanceStatusCode = settings.MixedAttendanceStatusCode?.Trim().ToUpperInvariant() ?? string.Empty;
        existing.FullDayMinimumHours = (settings.FullDayRequiredWorkingMinutes ?? 480) / 60m;
        existing.HalfDayMinimumHours = (settings.HalfDayRequiredWorkingMinutes ?? 240) / 60m;
        existing.UpdatedDate = DateTime.UtcNow;
        return string.IsNullOrWhiteSpace(existing.PolicyId)
            ? await _workFromHomePolicies.AddOne(existing)
            : await _workFromHomePolicies.Update(Builders<WorkFromHomePolicy>.Filter.Where(x => x.CompanyId == companyId && x.PolicyId == existing.PolicyId), existing);
    }

    private async Task<bool> ValidatePolicyShape(string companyId, EnumsHelper.LeavePolicyType policyType, string? attendanceStatusCode, WorkFromHomePolicySettingsRequest? wfh)
    {
        if (policyType == EnumsHelper.LeavePolicyType.Leave)
            return wfh is null && await ValidateAttendanceStatusMapping(companyId, attendanceStatusCode);

        if (policyType != EnumsHelper.LeavePolicyType.WorkFromHome || wfh is null || !wfh.AllowFullDay ||
            wfh.MaxDaysPerWeek is < 1 || wfh.MaxDaysPerMonth is < 1 || wfh.FullDayRequiredWorkingMinutes is < 0 ||
            wfh.HalfDayRequiredWorkingMinutes is < 0 || wfh.BreakMinutes < 0 ||
            wfh.ApproverStrategy is not ("REPORTING_MANAGER" or "DEPARTMENT_HEAD" or "HR_ADMIN"))
            return false;

        var codes = new[] { wfh.FullDayAttendanceStatusCode, wfh.HalfDayAttendanceStatusCode, wfh.MixedAttendanceStatusCode }
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim().ToUpperInvariant()).Distinct().ToList();
        if (codes.Count == 0 || (wfh.AllowFirstHalf || wfh.AllowSecondHalf) && string.IsNullOrWhiteSpace(wfh.HalfDayAttendanceStatusCode) ||
            wfh.AllowMixedHalfDayLeave && string.IsNullOrWhiteSpace(wfh.MixedAttendanceStatusCode))
            return false;
        return (await _attendanceStatusRepo.GetAll(x => x.CompanyId == companyId && x.IsActive && codes.Contains(x.Code)))
            .Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == codes.Count;
    }

    public async Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary()
    {
        Result<LeaveRequestSummaryResponseDto> result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        // get last 30 days leave request
        var fromDate = DateTime.UtcNow.AddDays(-30);
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.CompanyId == companyId && lr.CreatedDate.HasValue && lr.CreatedDate.Value.Date >= fromDate.Date;
        var currentMonthLeaveRequest = await _leave.GetAll(expression);
        Dictionary<int, int> leaveStatusSummary = new();
        currentMonthLeaveRequest.GroupBy(lr => lr.Status).ForEach(lr =>
        {
            leaveStatusSummary.Add((int)lr.Key, lr.Count());
        });

        var leavePolicyIds = currentMonthLeaveRequest.Select(lr => lr.LeavePolicyId).Distinct().ToList();
        var leavePolicyMap = (await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId && leavePolicyIds.Contains(lp.Id)))
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
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leaveRequests = (await _leave.GetAll(lr => lr.CompanyId == companyId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.StartDate.Year == year))
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
        if (leaveBalanceRequestDto is null || leaveBalanceRequestDto.Count == 0) return new Result { Success = false, Message = "At least one leave allocation change is required." };
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var employeeIds = leaveBalanceRequestDto.Select(x => x.EmployeeId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (employeeIds.Count != 1 || !await _employeeRepository.Exist(x => x.CompanyId == companyId && x.UserId == employeeIds[0] && x.Status && !x.IsDeleted))
            return new Result { Success = false, Message = "The selected employee is not active in this company." };
        foreach (var balanceItem in leaveBalanceRequestDto)
        {
            if (string.IsNullOrWhiteSpace(balanceItem.LeavePolicyId) || balanceItem.TotalAllocated < 0)
                return new Result { Success = false, Message = "Leave policy and a non-negative total allocation are required." };
            if (balanceItem.Remove)
            {
                if (!string.IsNullOrWhiteSpace(balanceItem.LeaveBalanceId))
                {
                    var existing = await _employeeLeaveBalanceRepo.FirstOrDefault(x => x.CompanyId == companyId && x.Id == balanceItem.LeaveBalanceId && x.UserId == balanceItem.EmployeeId && x.LeavePolicyId == balanceItem.LeavePolicyId);
                    if (existing is null) return new Result { Success = false, Message = "The leave allocation was changed. Refresh and try again." };
                    if (existing.Taken > 0) return new Result { Success = false, Message = "Leave allocation cannot be removed after leave has been taken." };
                    var deleted = await _employeeLeaveBalanceRepo.Delete(Builders<EmployeeLeaveBalance>.Filter.Where(x => x.CompanyId == companyId && x.Id == balanceItem.LeaveBalanceId && x.UserId == balanceItem.EmployeeId));
                    if (!deleted.Success) return deleted;
                }
                continue;
            }
            if (string.IsNullOrEmpty(balanceItem.LeaveBalanceId))
            {
                // add balance 
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == balanceItem.LeavePolicyId && lp.Status && lp.PolicyType != EnumsHelper.LeavePolicyType.WorkFromHome);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    result.Message = "The selected leave policy is no longer available for allocation.";
                    return result;
                }
                var requestedTotal = balanceItem.TotalAllocated;
                if (leavePolicy.MaxBalance.HasValue && requestedTotal > leavePolicy.MaxBalance.Value)
                {
                    result.Success = false;
                    result.Message = "The requested remaining balance exceeds this policy's maximum allocation.";
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }
                var employeeBalance = new EmployeeLeaveBalance()
                {
                    CompanyId = companyId,
                    UserId = balanceItem.EmployeeId,
                    LeavePolicyId = balanceItem.LeavePolicyId,
                    TotalAllocated = requestedTotal,
                    Taken = 0,
                    Remaining = requestedTotal,
                    IsManualAllocation = true,
                };
                result = await _employeeLeaveBalanceRepo.AddOne(employeeBalance);
            }
            else
            {
                // update balnce
                var leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.CompanyId == companyId && elb.Id == balanceItem.LeaveBalanceId && elb.UserId == balanceItem.EmployeeId);
                if (leaveBalance == null)
                {
                    result.Success = false;
                    result.Message = "The leave allocation was changed. Refresh and try again.";
                    return result;
                }
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveBalance.LeavePolicyId && lp.Status && lp.PolicyType != EnumsHelper.LeavePolicyType.WorkFromHome);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    result.Message = "The selected leave policy is no longer available for allocation.";
                    return result;
                }
                var requestedTotal = balanceItem.TotalAllocated;
                if (requestedTotal < leaveBalance.Taken)
                    return new Result { Success = false, Message = "Total allocation cannot be less than leave already taken." };
                if (leavePolicy.MaxBalance.HasValue && requestedTotal > leavePolicy.MaxBalance.Value)
                {
                    result.Success = false;
                    result.Message = "The requested remaining balance exceeds this policy's maximum allocation.";
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }

                var remaining = requestedTotal - leaveBalance.Taken;
                var expectedVersion = balanceItem.ExpectedVersion ?? leaveBalance.Version;
                var update = Builders<EmployeeLeaveBalance>.Update
                    .Set(lb => lb.TotalAllocated, requestedTotal)
                    .Set(lb => lb.Remaining, remaining)
                    .Set(lb => lb.IsManualAllocation, true)
                    .Set(lb => lb.UpdatedDate, DateTime.UtcNow)
                    .Inc(lb => lb.Version, 1);
                var updateResult = await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(
                    lb => lb.CompanyId == companyId && lb.Id == balanceItem.LeaveBalanceId && lb.UserId == balanceItem.EmployeeId && lb.Version == expectedVersion,
                    update);
                result = new Result { Success = updateResult.ModifiedCount == 1, Message = updateResult.ModifiedCount == 1 ? null : "The leave allocation was changed. Refresh and try again." };
                if (!result.Success) return result;
            }
        }
        return result;
    }

    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        Result<EmployeeLeaveBalanceResponseDto> result = new() { Success = false };
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        bool isEmpExist = await _employeeRepository.Exist(x =>
            x.CompanyId == companyId && x.UserId == employeeId && x.Status);
        if (!isEmpExist) return result;

        // get employee balance

        IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb =>
            elb.CompanyId == companyId && elb.UserId == employeeId);
        if (!employeeLeaveBalances.Any()) return result;
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            lp.CompanyId == companyId && lp.Status);

        List<EmployeeLeaveBalanceResponseDto> empLeaveBalanceResult = (
            from empLeaveBalance in employeeLeaveBalances
            join leavePolicy in leavePolicies on empLeaveBalance.LeavePolicyId equals leavePolicy.Id
            select new EmployeeLeaveBalanceResponseDto
            {
                LeaveBalanceId = empLeaveBalance.Id,
                LeavePolicyId = leavePolicy.Id,
                TotalAllocated = empLeaveBalance.TotalAllocated,
                Taken = empLeaveBalance.Taken,
                Remaining = empLeaveBalance.Remaining,
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
                        TotalAllocated = lb.TotalAllocated,
                        Taken = lb.Taken,
                        Remaining = lb.Remaining,
                        UsedLeave = lb.Taken
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

    private async Task LeaveNotification(
        LeaveRequest leaveDomain,
        EnumsHelper.LeaveRequestStatus status,
        string companyId,
        string actorUserId,
        bool sendEmail = true)
    {
        try
        {
            var employee = await _employeeRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == leaveDomain.EmployeeId);
            if (employee is null) return;

            List<EmpUser> recipients;
            string title;
            string body;
            EnumsHelper.NotificationTypes notificationType;
            EnumsHelper.MailType mailType;

            if (status == EnumsHelper.LeaveRequestStatus.Pending)
            {
                var roleIds = (await _roleRepository.GetAll(x =>
                    x.CompanyId == companyId &&
                    (x.RoleType == Convert.ToInt32(EnumsHelper.Roles.Administrator) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HR) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HRExecutive))))
                    .Select(x => x.RolesId)
                    .ToHashSet();
                recipients = (await _employeeRepository.GetAll(x =>
                        x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId)))
                    .Where(x => x.UserId != leaveDomain.EmployeeId)
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .ToList();
                title = "New leave request submitted";
                body = $"{employee.FirstName} {employee.LastName} requested leave from {leaveDomain.StartDate:dd MMM yyyy} to {leaveDomain.EndDate:dd MMM yyyy}.";
                notificationType = EnumsHelper.NotificationTypes.LeaveRequest;
                mailType = EnumsHelper.MailType.LeaveMailToHR;
            }
            else
            {
                if (!await _middlewareService.IsUserNotificationPreferenceEnabled(
                        leaveDomain.EmployeeId, EnumsHelper.NotificationPreferenceType.LeaveStatusUpdate)) return;
                recipients = [employee];
                var approved = status == EnumsHelper.LeaveRequestStatus.Accepted;
                title = approved ? "Leave request approved" : "Leave request rejected";
                body = $"Your leave request from {leaveDomain.StartDate:dd MMM yyyy} to {leaveDomain.EndDate:dd MMM yyyy} has been {(approved ? "approved" : "rejected")}.";
                notificationType = approved
                    ? EnumsHelper.NotificationTypes.LeaveRequestApproved
                    : EnumsHelper.NotificationTypes.LeaveRequestReject;
                mailType = EnumsHelper.MailType.LeaveReplyMail;
            }

            if (recipients.Count == 0) return;
            var notification = new Notifications
            {
                NotificationId = Guid.NewGuid().ToString(),
                CompanyId = companyId,
                CreatedBy = actorUserId,
                TargetId = leaveDomain.LeaveRequestId,
                Title = title,
                Body = body,
                CreatedDateTime = DateTime.UtcNow,
                NotificationType = notificationType,
            };
            if (!(await _notificationsRepo.AddOne(notification)).Success) return;

            var userNotifications = recipients.Select(user => new UserNotifications
            {
                UserNotificationId = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                CreatedDateTime = DateTime.UtcNow,
                IsDeleted = false,
            }).ToList();
            // Persist the inbox record before attempting optional real-time delivery.
            // A disconnected SignalR client must not prevent email delivery.
            await _userNotificationsRepo.AddMany(userNotifications);
            try
            {
                await Task.WhenAll(userNotifications.Select(item => _notificationService.SendNotificationToUser(item.UserId,
                    new NotificationViewModel
                    {
                        Title = title,
                        Body = body,
                        IsRead = false,
                        SentBy = actorUserId,
                        SentDateTime = DateTime.UtcNow,
                        TargetId = leaveDomain.LeaveRequestId,
                        NotificationTypes = notificationType,
                        UserNotificationId = item.UserNotificationId,
                    })));
            }
            catch
            {
                // The notification is already persisted and will be visible on next profile load.
            }

            if (!sendEmail) return;

            var emailBody = $"<p><strong>{WebUtility.HtmlEncode(title)}</strong></p><p>{WebUtility.HtmlEncode(body)}</p>";
            await Task.WhenAll(recipients
                .Where(user => user.IsEmailVerified && !string.IsNullOrWhiteSpace(user.Email))
                .Select(user => _middlewareService.EmailSendAndSave(new EmpEmailLogs
                {
                    CompanyId = companyId,
                    UserTo = user.UserId,
                    UserFrom = actorUserId,
                    Email = user.Email,
                    Subject = title,
                    Body = emailBody,
                    EmailLogType = mailType,
                })));
        }
        catch
        {
            // Notification delivery must not roll back a leave request that is already saved.
        }
    }

    public async Task EmployeeLeaveBalanceAccrual()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // get all active leavePolicy with accrual monthly or yearly
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            lp.Status && lp.PolicyType == EnumsHelper.LeavePolicyType.Leave && (lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly),
            withDefaultFilter: false
        );
        if (!leavePolicies.Any()) return;
        foreach (var policy in leavePolicies)
        {
            // get all active employees with leave policy
            IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb => elb.CompanyId == policy.CompanyId && elb.LeavePolicyId == policy.Id, withDefaultFilter: false);
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly && employeeLeaveBalances.Any())
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    // check if leave is already credited for this month 
                    if (!empLeave.IsManualAllocation && (!empLeave.LastAccrual.HasValue || empLeave.LastAccrual.Value < monthStart))
                    {
                        var availableSpace = policy.MaxBalance.HasValue ? Math.Max(0, policy.MaxBalance.Value - empLeave.Remaining) : policy.AccrualAmount;
                        var actualCredit = policy.MaxBalance.HasValue ? Math.Min(policy.AccrualAmount, availableSpace) : policy.AccrualAmount;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.CompanyId == policy.CompanyId && elb.Id == empLeave.Id && elb.Version == empLeave.Version && !elb.IsManualAllocation && (!elb.LastAccrual.HasValue || elb.LastAccrual.Value < monthStart);
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Inc(b => b.TotalAllocated, actualCredit)
                        .Inc(b => b.Remaining, actualCredit)
                        .Inc(b => b.Version, 1)
                        .Set(b => b.LastAccrual, now)
                        .Set(b => b.UpdatedDate, now));
                    }
                }
            }
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly && now.Month == 1)
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    if (!empLeave.IsManualAllocation && (!empLeave.LastAccrual.HasValue || empLeave.LastAccrual.Value < yearStart))
                    {
                        var carryOverLimit = policy.CarryOverLimit ?? 0m;
                        var carryForward = policy.CarryOverAllowed ? Math.Min(empLeave.Remaining, carryOverLimit) : 0m;
                        var newRemaining = carryForward + policy.AccrualAmount;
                        newRemaining = policy.MaxBalance.HasValue ? Math.Min(newRemaining, policy.MaxBalance.Value) : newRemaining;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.CompanyId == policy.CompanyId && elb.Id == empLeave.Id && elb.Version == empLeave.Version && !elb.IsManualAllocation && (!elb.LastAccrual.HasValue || elb.LastAccrual.Value < yearStart);
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Set(b => b.TotalAllocated, newRemaining)
                        .Set(b => b.Taken, 0)
                        .Set(b => b.Remaining, newRemaining)
                        .Inc(b => b.Version, 1)
                        .Set(b => b.LastAccrual, now)
                        .Set(b => b.UpdatedDate, now));
                    }
                }
            }
        }
    }
}

