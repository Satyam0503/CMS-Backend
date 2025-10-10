using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using EllipticCurve.Utils;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.LeaveManagement;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<LeaveTypes> _leaveTypeRepo;
    private readonly IMongoDbRepository<Roles> _roleRepository;
    private readonly IMongoDbRepository<EmpUser> _employeeRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveBalance> _leaveBalance;
    private readonly IMongoDbRepository<LeaveRequest> _leave;
    private readonly INotificationService _notificationService;
    private readonly IMongoDbRepository<Notifications> _notificationsRepo;
    private readonly IMongoDbRepository<UserNotifications> _userNotificationsRepo;
    private readonly IMongoDbRepository<JobTitles> _jobTitleRepo;
    private readonly IMapper _mapper;
    private readonly IEmployeeService _employeeService;
    public LeaveManagementService(IMongoDbRepository<LeaveTypes> leaveTypeRepo,
    IMongoDbRepository<EmpUser> employeeRepository,
    IMongoDbRepository<LeaveBalance> leaveBalance,
    IMongoDbRepository<LeaveRequest> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper,
    INotificationService notificationService,
    IMongoDbRepository<Notifications> notificationsRepo,
    IMongoDbRepository<UserNotifications> userNotificationsRepo,
    IMongoDbRepository<Roles> roleRepository,
    IMongoDbRepository<JobTitles> jobTitleRepo,
    IEmployeeService employeeService)
    {
        _leaveTypeRepo = leaveTypeRepo;
        _httpContextAccessor = httpContextAccessor;
        _employeeRepository = employeeRepository;
        _leaveBalance = leaveBalance;
        _leave = leave;
        _mapper = mapper;
        _notificationService = notificationService;
        _notificationsRepo = notificationsRepo;
        _userNotificationsRepo = userNotificationsRepo;
        _roleRepository = roleRepository;
        _employeeService = employeeService;
        _jobTitleRepo = jobTitleRepo;
    }

    public async Task<Result> CreateUpdateLeaveType(LeaveTypeRequestDto leaveTypeRequestDto)
    {
        Result result = new();
        // validate leave type enum value
        bool isValidLeaveType = Enum.IsDefined(typeof(EnumsHelper.LeaveTypes), leaveTypeRequestDto.LeaveType);
        if (!isValidLeaveType)
        {
            result.StatusCode = CustomStatusCode.LeaveTypeNotExist;
            return result;
        }
        Expression<Func<LeaveTypes, bool>> leaveTypeCondition = l => l.LeaveType == leaveTypeRequestDto.LeaveType;

        // create new leave type
        if (string.IsNullOrEmpty(leaveTypeRequestDto.LeaveTypeId))
        {
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(leaveTypeCondition, true);
            if (existingLeaveType == null)
            {
                var leaveType = new LeaveTypes
                {
                    MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate,
                    IsHalfDay = leaveTypeRequestDto.IsHalfDay,
                    LeaveType = leaveTypeRequestDto.LeaveType,
                    IsActive = leaveTypeRequestDto.IsActive,
                    MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays,
                    CreatedDate = DateTime.UtcNow
                };
                result = await _leaveTypeRepo.AddOne(leaveType);
            }
            else if (existingLeaveType != null && existingLeaveType.IsDeleted == true)
            {
                existingLeaveType.IsDeleted = false;
                existingLeaveType.MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate;
                existingLeaveType.LeaveType = leaveTypeRequestDto.LeaveType;
                existingLeaveType.IsActive = leaveTypeRequestDto.IsActive;
                existingLeaveType.MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays;
                result = await _leaveTypeRepo.Update(leaveTypeCondition, existingLeaveType);
            }
            result.StatusCode = CustomStatusCode.LeaveTypeAlreadyExist;
            return result;
        }
        else
        {
            var existingLeaveTypeName = await _leaveTypeRepo.FirstOrDefault(leaveTypeCondition, true);
            Expression<Func<LeaveTypes, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeRequestDto.LeaveTypeId;
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
            if (existingLeaveType == null)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.LeaveTypeNotExist;
                return result;
            }
            if (existingLeaveTypeName != null && existingLeaveTypeName.LeaveType == leaveTypeRequestDto.LeaveType && existingLeaveTypeName.LeaveTypeId != leaveTypeRequestDto.LeaveTypeId)
            {
                // if (existingLeaveTypeName.IsDeleted == true)
                // {
                //     // state change of already exist leave type
                //     existingLeaveTypeName.IsDeleted = false;
                //     existingLeaveTypeName.LeaveType = leaveTypeRequestDto.LeaveType;
                //     existingLeaveTypeName.MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays;
                //     existingLeaveType.MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate;
                //     existingLeaveType.IsHalfDay = leaveTypeRequestDto.IsHalfDay;
                //     existingLeaveType.IsActive = leaveTypeRequestDto.IsActive;
                //     await _leaveTypeRepo.Update(leaveTypeCondition, existingLeaveTypeName);
                //     //state change of selected leave type
                //     existingLeaveType.IsDeleted = true;
                //     result.Success = true;
                //     await _leaveTypeRepo.Update(whereCondition, existingLeaveType);
                //     return result;
                // }
                result.Success = false;
                result.StatusCode = CustomStatusCode.LeaveTypeAlreadyExist;
                return result;
            }
            existingLeaveType.LeaveType = leaveTypeRequestDto.LeaveType;
            existingLeaveType.MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays;
            existingLeaveType.IsHalfDay = leaveTypeRequestDto.IsHalfDay;
            existingLeaveType.IsActive = leaveTypeRequestDto.IsActive;
            existingLeaveType.MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate;
            result = await _leaveTypeRepo.Update(whereCondition, existingLeaveType);
        }
        return result;
    }

    public async Task<Result<LeaveTypeResponseDto>> GetLeaveType(bool? IsActive)
    {

        IEnumerable<LeaveTypes> leaveTypeList = [];
        if (!IsActive.HasValue || IsActive == false)
        {
            leaveTypeList = await _leaveTypeRepo.GetAll();
        }
        else
        {
            Expression<Func<LeaveTypes, bool>> isActiveCondition = l => !l.IsDeleted && l.IsActive;
            leaveTypeList = await _leaveTypeRepo.GetAll(isActiveCondition);
        }
       ;
        List<LeaveTypeResponseDto> leaveTypeData = _mapper.Map<List<LeaveTypeResponseDto>>(leaveTypeList);
        return new Result<LeaveTypeResponseDto>
        {
            Success = true,
            MethodResults = leaveTypeData
        };
    }

    public async Task<Result> DeleteLeaveType(string leaveTypeId)
    {
        Result result = new();
        Expression<Func<LeaveTypes, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeId;
        var deletedLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
        deletedLeaveType.IsDeleted = true;
        result = await _leaveTypeRepo.Update(whereCondition, deletedLeaveType);
        return result;
    }


    // leave balance services

    public async Task<Result> CreateUpdateLeaveBalance(LeaveBalanceRequestDto leaveBalanceRequestDto)
    {
        Result result = new();
        var employeeExist = await _employeeRepository.FirstOrDefault(e => e.UserId == leaveBalanceRequestDto.EmployeeId);
        if (employeeExist == null)
        {
            result.StatusCode = CustomStatusCode.EmployeeNotExist;
            return result;
        }
        IEnumerable<LeaveTypes> leaveTypes = await _leaveTypeRepo.GetAll();
        if (ValidateLeaveBalance(leaveBalanceRequestDto, result, leaveTypes) == false) return result;
        if (await InitializeLeaveBalance(leaveBalanceRequestDto, result) == false) return result;
        if (string.IsNullOrEmpty(leaveBalanceRequestDto.Id))
        {
            Expression<Func<LeaveBalance, bool>> whereCondition = l => l.EmployeeId == leaveBalanceRequestDto.EmployeeId && l.Year.Year == DateTime.UtcNow.Year;

            var existingEmployeeLeaveBalance = await _leaveBalance.FirstOrDefault(whereCondition);
            if (existingEmployeeLeaveBalance == null)
            {
                var leaveBalanceDomain = _mapper.Map<LeaveBalance>(leaveBalanceRequestDto);
                result = await _leaveBalance.AddOne(leaveBalanceDomain);
                return result;
            }
            else
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.LeaveBalanceAlreadyExists;
                return result;
            }
        }
        else
        {
            Expression<Func<LeaveBalance, bool>> whereCondition = l => l.Id == leaveBalanceRequestDto.Id;
            var existingLeaveBalance = await _leaveBalance.FirstOrDefault(whereCondition);
            existingLeaveBalance.LeaveTypeBalances = leaveBalanceRequestDto.LeaveTypeBalances;
            result = await _leaveBalance.Update(whereCondition, existingLeaveBalance);
        }
        return result;
    }

    public async Task<Result<LeaveBalanceResponseDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter)
    {
        IEnumerable<LeaveBalance> leaveBalances = [];
        var CompanyId = CurrentContext.CompanyId(_httpContextAccessor);

        List<string> empId = !string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) ? (await _employeeRepository.GetAll(e => e.FirstName.ToLower().Contains(leaveBalanceFilter.EmployeeName) || e.LastName.ToLower().Contains(leaveBalanceFilter.EmployeeName))).Select(e => e.UserId).ToList() : null;
        Expression<Func<LeaveBalance, bool>> whereCondition = l =>
        (string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) || empId.Contains(l.EmployeeId))
        && ((leaveBalanceFilter.Year == null && l.Year.Year == DateTime.UtcNow.Year) || (l.Year.Year == leaveBalanceFilter.Year));
        int count = await _leaveBalance.Count(whereCondition);

        leaveBalances = (await _leaveBalance.GetAggregateDataAsync<LeaveBalance>(whereCondition, pageNo: leaveBalanceFilter.PageNo, pageSize: leaveBalanceFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        List<string> userIds = leaveBalances.Select(lb => lb.EmployeeId).ToList();
        List<EmpUser> users = (await _employeeRepository.GetAll(e => userIds.Contains(e.UserId))).ToList();
        List<LeaveTypes> leaveTypes = (await _leaveTypeRepo.GetAll()).ToList();
        IEnumerable<JobTitles> jobTitles = await _jobTitleRepo.GetAll();

        List<LeaveBalanceResponseDto> LeaveBalanceList = (from leaveBalance in leaveBalances
                                                          join user in users on leaveBalance.EmployeeId equals user.UserId
                                                          join jobTitle in jobTitles on user.JobRole equals jobTitle.JobTitleId into jobTitlesGroup
                                                          from jobTitleItem in jobTitlesGroup.DefaultIfEmpty()
                                                          select new LeaveBalanceResponseDto
                                                          {
                                                              Id = leaveBalance.Id,
                                                              EmployeeId = leaveBalance.EmployeeId,
                                                              EmployeeName = user.FirstName + " " + user.LastName,
                                                              ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                              JobRole = jobTitleItem?.Titles.ToDictionary(jt => jt.Language, jt => jt.Label),
                                                              Year = leaveBalance.Year,
                                                              SickLeave = leaveBalance.LeaveTypeBalances.Find(x => x.LeaveType == EnumsHelper.LeaveTypes.Sick) ?? null,
                                                              EarnedLeave = leaveBalance.LeaveTypeBalances.Find(x => x.LeaveType == EnumsHelper.LeaveTypes.Earned) ?? null,
                                                              CasualLeave = leaveBalance.LeaveTypeBalances.Find(x => x.LeaveType == EnumsHelper.LeaveTypes.Casual) ?? null,
                                                              PaternityLeave = leaveBalance.LeaveTypeBalances.Find(x => x.LeaveType == EnumsHelper.LeaveTypes.Paternity) ?? null,
                                                              MaternityLeave = leaveBalance.LeaveTypeBalances.Find(x => x.LeaveType == EnumsHelper.LeaveTypes.Maternity) ?? null,
                                                          }
                                                         ).ToList();
        return new Result<LeaveBalanceResponseDto>
        {
            Success = true,
            TotalRecords = count,
            MethodResults = LeaveBalanceList
        };
    }

    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        Result<EmployeeLeaveBalanceResponseDto> result = new();
        bool isEmpExist = await _employeeRepository.Exist(x => x.UserId == employeeId);
        if (!isEmpExist)
        {
            result.Success = false;
            return result;
        }
        // add current year check here
        LeaveBalance? empLeaveBalances = await _leaveBalance.FirstOrDefault(x => x.EmployeeId == employeeId && x.Year.Year == DateTime.UtcNow.Year);
        if (empLeaveBalances is null)
        {
            return result;
        }
        // List<EnumsHelper.LeaveTypes> employeeLeaveTypes = leaveBalance.LeaveTypeBalances.Select(x => x.LeaveType).ToList();
        List<LeaveTypes> leaveTypes = (await _leaveTypeRepo.GetAll()).ToList();
        List<EmployeeLeaveBalanceResponseDto> employeeLeaveBalanceList = (
            from empLeaveTypeBalance in empLeaveBalances.LeaveTypeBalances
            join leaveType in leaveTypes on empLeaveTypeBalance.LeaveType equals leaveType.LeaveType
            select new EmployeeLeaveBalanceResponseDto
            {
                LeaveType = empLeaveTypeBalance.LeaveType,
                MaximumLeave = empLeaveTypeBalance.MaximumLeave,
                RemainingLeave = empLeaveTypeBalance.RemainingLeave ?? 0,
                MinAdvanceNoticeDate = leaveType.MinAdvanceNoticeDate,
                IsHalfDay = leaveType.IsHalfDay,
            }
        ).ToList();

        result.MethodResults = employeeLeaveBalanceList;
        result.Success = true;
        result.TotalRecords = employeeLeaveBalanceList.Count;
        return result;
    }

    public async Task<Result> CreateUpdateLeave(LeaveRequestDto leaveRequestDto, string userId)
    {
        Result result = new();

        var selectedLeaveType = await _leaveTypeRepo.FirstOrDefault(lt => lt.LeaveType == leaveRequestDto.LeaveType);
        if (selectedLeaveType == null)
        {
            result.StatusCode = CustomStatusCode.LeaveTypeNotExist;
            return result;
        }

        Expression<Func<LeaveBalance, bool>> leaveBalanceCondition = lb => lb.EmployeeId == userId && lb.Year.Year == DateTime.UtcNow.Year;
        var selectedEmpLeaveBal = await _leaveBalance.FirstOrDefault(leaveBalanceCondition);
        if (selectedEmpLeaveBal == null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }
        bool leaveTypeExist = selectedEmpLeaveBal.LeaveTypeBalances.Any(lb => lb.LeaveType == leaveRequestDto.LeaveType);
        if (!leaveTypeExist)
        {
            result.StatusCode = CustomStatusCode.LeaveTypeBalanceNotExists;
            return result;
        }

        // if user has pending leave request
        if (leaveRequestDto.LeaveRequestId == null)
        {
            int pendingLeaves = await _leave.Count(l => l.EmployeeId == userId && l.Status == EnumsHelper.LeaveRequestStatus.Pending);
            if (pendingLeaves > 0)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.PendingLeaveExist;
                return result;
            }
        }
        var totalRequestedLeaveDays = leaveRequestDto.EndDate.Day - leaveRequestDto.StartDate.Day + 1;
        bool IsValid = await LeaveRequestValidation(selectedLeaveType, selectedEmpLeaveBal, leaveRequestDto, totalRequestedLeaveDays, result);
        if (IsValid == false)
        {
            return result;
        }

        if (string.IsNullOrEmpty(leaveRequestDto.LeaveRequestId))
        {
            var leaveDomain = new LeaveRequest
            {
                EmployeeId = userId,
                LeaveType = leaveRequestDto.LeaveType,
                StartDate = leaveRequestDto.StartDate,
                EndDate = leaveRequestDto.IsHalfDay ? leaveRequestDto.StartDate : leaveRequestDto.EndDate,
                TotalDays = leaveRequestDto.IsHalfDay ? 0.5m : totalRequestedLeaveDays,
                IsHalfDay = leaveRequestDto.IsHalfDay,
                Reason = leaveRequestDto.Reason,
                ReviewedBy = "",
                Status = EnumsHelper.LeaveRequestStatus.Pending
            };

            result = await _leave.AddOne(leaveDomain);
            if (result.Success)
            {
                LeaveNotification(leaveDomain, leaveDomain.Status);
            }
            return result;
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.LeaveRequestId == leaveRequestDto.LeaveRequestId;
            var existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);
            if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Pending)
            {
                existingLeaveRequest.StartDate = leaveRequestDto.StartDate;
                existingLeaveRequest.EndDate = leaveRequestDto.EndDate;
                existingLeaveRequest.IsHalfDay = leaveRequestDto.IsHalfDay;
                existingLeaveRequest.TotalDays = leaveRequestDto.IsHalfDay ? 0.5m : totalRequestedLeaveDays;
                existingLeaveRequest.LeaveType = leaveRequestDto.LeaveType;
                existingLeaveRequest.Reason = leaveRequestDto.Reason;
                result = await _leave.Update(whereCondition, existingLeaveRequest);
            }
            return result;
        }
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
            Expression<Func<LeaveRequest, bool>> whereCondition = lr =>
               (leaveFilter.LeaveType == null || (lr.LeaveType == leaveFilter.LeaveType))
            && (leaveFilter.Status == null || (lr.Status == leaveFilter.Status))
            && ((leaveFilter.StartDate == null || (lr.StartDate >= leaveFilter.StartDate)) && (leaveFilter.EndDate == null || (lr.StartDate <= leaveFilter.EndDate)));

            count = await _leave.Count(whereCondition);
            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        }
        var users = await _employeeRepository.GetAll();
        var jobTitles = await _jobTitleRepo.GetAll();

        List<LeaveResponseDto> FinalLeaveRequestList = (from leave in leaveRequestList
                                                        join user in users on leave.EmployeeId equals user.UserId
                                                        join reviewerGroup in users on leave.ReviewedBy equals reviewerGroup.UserId into approvedUsers
                                                        from approvedUser in approvedUsers.DefaultIfEmpty()
                                                        join job in jobTitles on user.JobRole equals job.JobTitleId into jobRolesTitles
                                                        from jobTitle in jobRolesTitles.DefaultIfEmpty()
                                                        select new LeaveResponseDto
                                                        {
                                                            LeaveRequestId = leave.LeaveRequestId,
                                                            EmployeeName = user.FirstName + " " + user.LastName,
                                                            ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                            JobRole = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                                                            IsHalfDay = leave.IsHalfDay,
                                                            LeaveType = leave.LeaveType,
                                                            StartDate = leave.StartDate,
                                                            EndDate = leave.EndDate,
                                                            TotalDays = leave.TotalDays,
                                                            Reason = leave.Reason,
                                                            ReviewedBy = string.IsNullOrEmpty(leave.ReviewedBy) ? "-" : approvedUser != null ?
                                                                     approvedUser.FirstName + " " + approvedUser.LastName : "-",
                                                            Status = leave.Status
                                                        }
                                                        ).ToList();

        return new Result<LeaveResponseDto>
        {
            Success = true,
            TotalRecords = count,
            MethodResults = FinalLeaveRequestList
        };
    }

    public async Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(LeaveRequestFilter filter, string userId)
    {
        Result<MyLeaveRequestResponse> result = new();
        Expression<Func<LeaveRequest, bool>> whereCondition = lr =>
                lr.EmployeeId == userId
            && (filter.LeaveType == null || (lr.LeaveType == filter.LeaveType))
            && (filter.Status == null || (lr.Status == filter.Status))
            && ((filter.StartDate == null || (lr.StartDate >= filter.StartDate)) && (filter.EndDate == null || (lr.StartDate <= filter.EndDate)));

        int count = await _leave.Count(whereCondition);
        List<LeaveRequest> myLeaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: filter.PageNo, pageSize: filter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();

        // get reviewer details
        var userIds = myLeaveRequestList.Select(lr => lr.ReviewedBy).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await _employeeRepository.GetAll(u => userIds.Contains(u.UserId));

        var userDict = users.ToDictionary(u => u.UserId, u => u);

        var responseList = myLeaveRequestList.Select(lr =>
        {
            var reviewer = !string.IsNullOrEmpty(lr.ReviewedBy) && userDict.ContainsKey(lr.ReviewedBy)
                ? userDict[lr.ReviewedBy]
                : null;
            return new MyLeaveRequestResponse
            {
                LeaveRequestId = lr.LeaveRequestId,
                IsHalfDay = lr.IsHalfDay,
                LeaveType = lr.LeaveType,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                ReviewedBy = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "-",
            };
        }).ToList();

        result.MethodResults = responseList;
        result.TotalRecords = count;
        return result;
    }

    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, EnumsHelper.LeaveRequestStatus status)
    {
        Result result = new();
        Expression<Func<LeaveRequest, bool>> leaveRequestCond = lr => lr.LeaveRequestId == leaveRequestId;
        var existingLeaveRequest = await _leave.FirstOrDefault(leaveRequestCond);
        if (existingLeaveRequest == null || status == EnumsHelper.LeaveRequestStatus.Pending || status == existingLeaveRequest.Status) return result;

        Expression<Func<LeaveBalance, bool>> leaveBalanceCond = lb => lb.EmployeeId == existingLeaveRequest.EmployeeId;
        var selectedEmpLeaveBal = await _leaveBalance.FirstOrDefault(leaveBalanceCond);
        if (selectedEmpLeaveBal == null)
        {
            return result;
        }
        var balance = selectedEmpLeaveBal.LeaveTypeBalances.FirstOrDefault(lt => lt.LeaveType == existingLeaveRequest.LeaveType);

        var reviewedBy = CurrentContext.UserId(_httpContextAccessor);
        if (status == EnumsHelper.LeaveRequestStatus.Rejected)
        {
            if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted && existingLeaveRequest.IsHalfDay == true)
            {
                balance.RemainingLeave += 0.5m;
            }
            if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted && existingLeaveRequest.IsHalfDay == false)
            {
                balance.RemainingLeave += existingLeaveRequest.TotalDays;
            }
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Status = EnumsHelper.LeaveRequestStatus.Rejected;
        }
        if (status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            if (existingLeaveRequest.IsHalfDay == true && existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Accepted)
            {
                balance.RemainingLeave -= 0.5m;
            }
            if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Accepted && existingLeaveRequest.IsHalfDay == false)
            {
                balance.RemainingLeave -= existingLeaveRequest.TotalDays;
            }
            existingLeaveRequest.Status = EnumsHelper.LeaveRequestStatus.Accepted;
            existingLeaveRequest.ReviewedBy = reviewedBy;
        }
        await _leave.Update(leaveRequestCond, existingLeaveRequest);
        result = await _leaveBalance.Update(leaveBalanceCond, selectedEmpLeaveBal);
        if (result.Success)
        {
            LeaveNotification(existingLeaveRequest, status);
        }
        return result;
    }

    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.LeaveRequestId == leaveRequestId;
        LeaveRequest? existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);
        if (existingLeaveRequest is null) return new Result();
        existingLeaveRequest.IsDeleted = true;
        Result result = await _leave.Update(whereCondition, existingLeaveRequest);
        return result;
    }

    private async Task<bool> InitializeLeaveBalance(LeaveBalanceRequestDto leaveBalanceRequestDto, Result result)
    {
        if (string.IsNullOrEmpty(leaveBalanceRequestDto.Id))
        {
            // add logic
            foreach (var leaveTypeBalance in leaveBalanceRequestDto.LeaveTypeBalances)
            {
                leaveTypeBalance.RemainingLeave ??= leaveTypeBalance.MaximumLeave;
            }
        }
        else
        {
            // updateLogic
            LeaveBalance? empLeaveBalances = await _leaveBalance.FirstOrDefault(lb => lb.Id == leaveBalanceRequestDto.Id && lb.Year.Year == DateTime.UtcNow.Year);
            if (empLeaveBalances is null)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
                return false;
            }
            foreach (var leaveTypeBalance in leaveBalanceRequestDto.LeaveTypeBalances)
            {
                LeaveTypeBalance? existingLeaveBalance = empLeaveBalances.LeaveTypeBalances.Find(lb => lb.LeaveType == leaveTypeBalance.LeaveType);
                if (existingLeaveBalance is null)
                {
                    leaveTypeBalance.RemainingLeave = leaveTypeBalance.MaximumLeave;
                }
                else if (existingLeaveBalance.MaximumLeave == leaveTypeBalance.MaximumLeave)
                {
                    leaveTypeBalance.RemainingLeave = existingLeaveBalance.RemainingLeave;
                }
                else if (leaveTypeBalance.MaximumLeave > existingLeaveBalance.MaximumLeave)
                {
                    leaveTypeBalance.RemainingLeave = (existingLeaveBalance.RemainingLeave ?? 0) + leaveTypeBalance.MaximumLeave - existingLeaveBalance.MaximumLeave;
                }
                else
                {
                    decimal leaveTaken = existingLeaveBalance.MaximumLeave - (existingLeaveBalance.RemainingLeave ?? 0);
                    leaveTaken = Math.Max(leaveTaken, 0);
                    leaveTypeBalance.RemainingLeave = leaveTaken > 0 ? Math.Max(leaveTypeBalance.MaximumLeave - leaveTaken, 0) : leaveTypeBalance.MaximumLeave;
                }

                // Clamp RemainingLeave between 0 and MaximumLeave
                if (leaveTypeBalance.RemainingLeave < 0)
                    leaveTypeBalance.RemainingLeave = 0;
                if (leaveTypeBalance.RemainingLeave > leaveTypeBalance.MaximumLeave)
                    leaveTypeBalance.RemainingLeave = leaveTypeBalance.MaximumLeave;
            }
        }
        return true;
    }

    private static bool ValidateLeaveBalance(LeaveBalanceRequestDto leaveBalanceRequestDto, Result result, IEnumerable<LeaveTypes> leaveTypes)
    {
        foreach (var leaveTypeBalance in leaveBalanceRequestDto.LeaveTypeBalances)
        {
            LeaveTypes? leaveType = leaveTypes.FirstOrDefault(lt => lt.LeaveType == leaveTypeBalance.LeaveType && lt.IsActive);
            if (leaveType is null)
            {
                result.StatusCode = CustomStatusCode.LeaveTypeNotExist;
                result.Success = false;
                return false;
            }
            else if (leaveTypeBalance.MaximumLeave < 0)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.NegativeValueNotAllowed;
                return false;
            }
            else if (leaveType.MaxLeaveDays < leaveTypeBalance.MaximumLeave)
            {
                result.StatusCode = CustomStatusCode.MaxAllowedLeaveDaysExceed;
                result.Success = false;
                return false;
            }
        }
        return true;
    }

    private async Task<bool> LeaveRequestValidation(LeaveTypes selectedLeaveType, LeaveBalance selectedEmpLeaveBal, LeaveRequestDto leaveRequestDto, int requestedDay, Result result)
    {
        var totalAdvanceNoticeDays = (leaveRequestDto.StartDate.Date - DateTime.UtcNow.Date).TotalDays + 1;

        if (selectedLeaveType.MinAdvanceNoticeDate > 0 && selectedLeaveType.MinAdvanceNoticeDate > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return false;
        }

        var balance = selectedEmpLeaveBal.LeaveTypeBalances.FirstOrDefault(lb => lb.LeaveType == leaveRequestDto.LeaveType);
        if (balance == null || balance.RemainingLeave == null || balance.RemainingLeave <= 0 || balance.RemainingLeave < requestedDay)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalanc;
            return false;
        }
        if (leaveRequestDto.LeaveType == EnumsHelper.LeaveTypes.Earned)
        {
            var earnedAnnualDayTillNow = DateTime.UtcNow.Month;
            var remainingMonthLeave = balance.MaximumLeave - earnedAnnualDayTillNow;
            var availableEarnedLeave = balance.RemainingLeave - remainingMonthLeave;
            if (availableEarnedLeave <= 0 || availableEarnedLeave < requestedDay)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.InsufficientMonthLeaveBalance;
                return false;
            }
        }
        return true;
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
            List<string> roleIds = (await _roleRepository.GetAll(x => x.CompanyId == company_id && x.RoleType == Convert.ToInt32(EnumsHelper.Roles.Administrator) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HR))).Select(x => x.RolesId).ToList();
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
            targetUserIds.Add(leaveDomain.EmployeeId);
            notification.Body = await _employeeService.GetEmployeeNameById(leaveDomain.ReviewedBy);
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequestApproved;
        }
        else
        {
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
}

