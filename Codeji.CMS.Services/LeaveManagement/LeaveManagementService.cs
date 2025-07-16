using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
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
    private readonly IMongoDbRepository<EmpUser> _empUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveBalance> _leaveBalance;
    private readonly IMongoDbRepository<LeaveRequest> _leave;
    private readonly INotificationService _notificationService;
    private readonly IMongoDbRepository<Notifications> _notificationsRepo;
    private readonly IMongoDbRepository<UserNotifications> _userNotificationsRepo;
    private readonly IMapper _mapper;
    public LeaveManagementService(IMongoDbRepository<LeaveTypes> leaveTypeRepo,
    IMongoDbRepository<EmpUser> empUser,
    IMongoDbRepository<LeaveBalance> leaveBalance,
    IMongoDbRepository<LeaveRequest> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper,
    INotificationService notificationService,
    IMongoDbRepository<Notifications> notificationsRepo,
    IMongoDbRepository<UserNotifications> userNotificationsRepo,
    IMongoDbRepository<Roles> roleRepository)
    {
        _leaveTypeRepo = leaveTypeRepo;
        _httpContextAccessor = httpContextAccessor;
        _empUser = empUser;
        _leaveBalance = leaveBalance;
        _leave = leave;
        _mapper = mapper;
        _notificationService = notificationService;
        _notificationsRepo = notificationsRepo;
        _userNotificationsRepo = userNotificationsRepo;
        _roleRepository = roleRepository;
    }

    public async Task<Result> CreateUpdateLeaveType(LeaveTypeRequestDto leaveTypeRequestDto)
    {
        Result result = new();
        bool isValidLeaveType = Enum.IsDefined(typeof(EnumsHelper.LeaveTypes), leaveTypeRequestDto.LeaveType);
        if (!isValidLeaveType)
        {
            result.Message = "Leave type doesn't exists";
            return result;
        }
        Expression<Func<LeaveTypes, bool>> leaveTypeCondition = l => l.LeaveType == leaveTypeRequestDto.LeaveType;

        if (string.IsNullOrEmpty(leaveTypeRequestDto.LeaveTypeId))
        {
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(leaveTypeCondition, true);
            if (existingLeaveType == null)
            {
                var leaveTypeDomain = new LeaveTypes
                {
                    MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate,
                    IsHalfDay = leaveTypeRequestDto.IsHalfDay,
                    LeaveType = leaveTypeRequestDto.LeaveType,
                    IsActive = leaveTypeRequestDto.IsActive,
                    MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays,
                    CreatedDate = DateTime.UtcNow
                };
                result = await _leaveTypeRepo.AddOne(leaveTypeDomain);

            }
            else if (existingLeaveType != null && existingLeaveType.IsDeleted == true)
            {
                existingLeaveType.IsDeleted = false;
                existingLeaveType.MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate;
                existingLeaveType.LeaveType = leaveTypeRequestDto.LeaveType;
                existingLeaveType.IsActive = leaveTypeRequestDto.IsActive;
                existingLeaveType.MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays;
                await _leaveTypeRepo.Update(leaveTypeCondition, existingLeaveType);
                result.Success = true;
            }
            result.Message = "Leave Type Already Exists";
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
                return result;
            }
            if (existingLeaveTypeName != null && existingLeaveTypeName.LeaveType == leaveTypeRequestDto.LeaveType && existingLeaveTypeName.LeaveTypeId != leaveTypeRequestDto.LeaveTypeId)
            {
                if (existingLeaveTypeName.IsDeleted == true)
                {
                    // state change of already exist leave type
                    existingLeaveTypeName.IsDeleted = false;
                    existingLeaveTypeName.LeaveType = leaveTypeRequestDto.LeaveType;
                    existingLeaveTypeName.MaxLeaveDays = leaveTypeRequestDto.MaxLeaveDays;
                    existingLeaveType.MinAdvanceNoticeDate = leaveTypeRequestDto.MinAdvanceNoticeDate;
                    existingLeaveType.IsHalfDay = leaveTypeRequestDto.IsHalfDay;
                    existingLeaveType.IsActive = leaveTypeRequestDto.IsActive;
                    await _leaveTypeRepo.Update(leaveTypeCondition, existingLeaveTypeName);
                    //state change of selected leave type
                    existingLeaveType.IsDeleted = true;
                    result.Success = true;
                    await _leaveTypeRepo.Update(whereCondition, existingLeaveType);
                    return result;
                }
                result.Success = false;
                result.Message = "Leave Type Already Exists";
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


        List<string> usersId = leaveTypeList.Select(l => l.CreatedBy).ToList();
        List<EmpUser> users = (await _empUser.GetAll(u => usersId.Contains(u.UserId))).ToList();

        List<LeaveTypeResponseDto> leaveTypeData = (from leaveType in leaveTypeList
                                                    join user in users on leaveType.CreatedBy equals user.UserId
                                                    select new LeaveTypeResponseDto
                                                    {
                                                        LeaveTypeId = leaveType.LeaveTypeId,
                                                        LeaveType = leaveType.LeaveType,
                                                        MaxLeaveDays = leaveType.MaxLeaveDays,
                                                        MinAdvanceNoticeDate = leaveType.MinAdvanceNoticeDate,
                                                        IsHalfDay = leaveType.IsHalfDay,
                                                        IsActive = leaveType.IsActive,
                                                        CreatedBy = user.FirstName + " " + user.LastName,
                                                        CreatedOn = leaveType.CreatedDate
                                                    }
                                                    ).OrderByDescending(d => d.CreatedOn).ToList();
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



    // leave balance 

    public async Task<Result> CreateUpdateLeaveBalance(LeaveBalanceRequestDto leaveBalanceRequestDto)
    {
        Result result = new();
        var employeeExist = await _empUser.FirstOrDefault(e => e.UserId == leaveBalanceRequestDto.EmployeeId);
        if (employeeExist == null)
        {
            result.Message = "Employee Doesn't Exists";
            return result;
        }
        if (InitializeRemainingBalance(leaveBalanceRequestDto) == false)
        {
            result.Message = "Remaining Leave Should Be Less Than Maximum Leave";
            return result;
        }
        IEnumerable<LeaveTypes> leaveTypes = await _leaveTypeRepo.GetAll();
        if (LeaveBalanceValidation(leaveBalanceRequestDto, result, leaveTypes) == false)
        {
            return result;
        }
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
                result.Message = "Leave Balance For Current Employee Already Exists";
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

        List<string> empId = !string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) ? (await _empUser.GetAll(e => e.FirstName.ToLower().Contains(leaveBalanceFilter.EmployeeName) || e.LastName.ToLower().Contains(leaveBalanceFilter.EmployeeName))).Select(e => e.UserId).ToList() : null;
        Expression<Func<LeaveBalance, bool>> whereCondition = l =>
        (string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) || empId.Contains(l.EmployeeId))
        && (leaveBalanceFilter.Year == null || (l.Year.Year == leaveBalanceFilter.Year));
        int count = await _leaveBalance.Count(whereCondition);

        leaveBalances = (await _leaveBalance.GetAggregateDataAsync<LeaveBalance>(whereCondition, pageNo: leaveBalanceFilter.PageNo, pageSize: leaveBalanceFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        List<EmpUser> users = (await _empUser.GetAll(e => CompanyId.Contains(e.CompanyId))).ToList();
        List<LeaveTypes> leaveTypes = (await _leaveTypeRepo.GetAll()).ToList();
        List<LeaveBalanceResponseDto> LeaveBalanceList = (from leaveBalance in leaveBalances
                                                          join user in users on leaveBalance.EmployeeId equals user.UserId
                                                          join createdByUser in users on leaveBalance.CreatedBy equals createdByUser.UserId
                                                          select new LeaveBalanceResponseDto
                                                          {
                                                              Id = leaveBalance.Id,
                                                              EmployeeId = leaveBalance.EmployeeId,
                                                              EmployeeName = user.FirstName + " " + user.LastName,
                                                              ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                              JobRole = user.JobRole,
                                                              CreatedBy = createdByUser.FirstName + " " + createdByUser.LastName,
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
        bool isEmpExist = await _empUser.Exist(x => x.UserId == employeeId);
        if (!isEmpExist)
        {
            result.Message = "Employee doesn't exist";
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
                MinAdvanceNoticeDate = leaveType.MinAdvanceNoticeDate ?? 0,
                IsHalfDay = leaveType.IsHalfDay,
            }
        ).ToList();

        result.MethodResults = employeeLeaveBalanceList;
        result.Success = true;
        result.TotalRecords = employeeLeaveBalanceList.Count;
        return result;
    }

    // Leave 
    public async Task<Result> CreateUpdateLeave(LeaveRequestDto leaveRequestDto)
    {
        Result result = new();
        var selectedLeaveType = await _leaveTypeRepo.FirstOrDefault(lt => lt.LeaveType == leaveRequestDto.LeaveType);
        if (selectedLeaveType == null)
        {
            result.Success = false;
            result.Message = "Invalid Leave Type";
            return result;
        }
        Expression<Func<LeaveBalance, bool>> leaveBalanceCondition = lb => lb.EmployeeId == leaveRequestDto.EmployeeId;
        var selectedEmpLeaveBal = await _leaveBalance.FirstOrDefault(leaveBalanceCondition);
        if (selectedEmpLeaveBal == null)
        {
            result.Success = false;
            result.Message = "Leave balance not found for the employee";
            return result;
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
                EmployeeId = leaveRequestDto.EmployeeId,
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

    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveFilter? leaveFilter)
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
            Expression<Func<LeaveRequest, bool>> whereCondition = l =>
            (leaveFilter.EmployeeId == null || l.EmployeeId == leaveFilter.EmployeeId)
            && (leaveFilter.LeaveType == null || !leaveFilter.LeaveType.Any() || leaveFilter.LeaveType.Contains(l.LeaveType))
            && (leaveFilter.StartDate == null || !leaveFilter.StartDate.HasValue || (l.StartDate >= leaveFilter.StartDate))
            && (leaveFilter.EndDate == null || !leaveFilter.EndDate.HasValue || (l.EndDate <= leaveFilter.EndDate))
            && (leaveFilter.Status == null || !leaveFilter.Status.Any() || leaveFilter.Status.Contains(l.Status));
            count = await _leave.Count(whereCondition);
            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        }
        var users = await _empUser.GetAll();

        List<LeaveResponseDto> FinalLeaveRequestList = (from leave in leaveRequestList
                                                        join user in users on leave.EmployeeId equals user.UserId
                                                        join reviewerGroup in users on leave.ReviewedBy equals reviewerGroup.UserId into approvedUsers
                                                        from approvedUser in approvedUsers.DefaultIfEmpty()
                                                        select new LeaveResponseDto
                                                        {
                                                            LeaveRequestId = leave.LeaveRequestId,
                                                            EmployeeId = leave.EmployeeId,
                                                            EmployeeName = user.FirstName + " " + user.LastName,
                                                            ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                            JobRole = user.JobRole,
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

        var requestedDay = (existingLeaveRequest.EndDate.Day - existingLeaveRequest.StartDate.Day) + 1;
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

    private bool InitializeRemainingBalance(LeaveBalanceRequestDto leaveBalanceRequestDto)
    {
        foreach (var leaveType in leaveBalanceRequestDto.LeaveTypeBalances)
        {
            if (leaveType.MaximumLeave < leaveType.RemainingLeave)
            {
                return false;
            }
            else if (leaveType.RemainingLeave == null)
            {
                leaveType.RemainingLeave = leaveType.MaximumLeave;
            }
        }
        return true;
    }

    private bool LeaveBalanceValidation(LeaveBalanceRequestDto leaveBalanceRequestDto, Result result, IEnumerable<LeaveTypes> leaveTypes)
    {
        foreach (var leavetype in leaveBalanceRequestDto.LeaveTypeBalances)
        {
            var leave = leaveTypes.FirstOrDefault(lt => lt.LeaveType == leavetype.LeaveType);
            if (leave == null)
            {
                result.Success = false;
                result.Message = "Leave Type Not Exists";
                return false;
            }
            if (leavetype.MaximumLeave > leave.MaxLeaveDays)
            {
                result.Success = false;
                result.Message = $"Maximum Leave For {leavetype.LeaveType} is greater than maximum allowed leave days ";
                return false;
            }
        }
        return true;
    }


    private async Task<bool> LeaveRequestValidation(LeaveTypes selectedLeaveType, LeaveBalance selectedEmpLeaveBal, LeaveRequestDto leaveRequestDto, int requestedDay, Result result)
    {
        var earnedAnnualDayTillNow = DateTime.UtcNow.Month;
        var totalAdvanceNoticeDays = (leaveRequestDto.StartDate.Date - DateTime.UtcNow.Date).TotalDays;

        if (selectedLeaveType.MinAdvanceNoticeDate > 0 && selectedLeaveType.MinAdvanceNoticeDate > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.Message = $"Leave must be applied at least {selectedLeaveType.MinAdvanceNoticeDate} days in advance";
            return false;
        }

        var balance = selectedEmpLeaveBal.LeaveTypeBalances.FirstOrDefault(lb => lb.LeaveType == leaveRequestDto.LeaveType);
        if (balance == null || balance.RemainingLeave == null || balance.RemainingLeave <= 0)
        {
            result.Success = false;
            result.Message = $"Insufficient leave balance for ";
            return false;
        }

        // redefine logic
        var annualLeaveTaken = await _leave.Count(l =>
            (l.EmployeeId == leaveRequestDto.EmployeeId)
            && (l.LeaveType == leaveRequestDto.LeaveType)
            && (l.StartDate.Year == DateTime.UtcNow.Year)
            && (l.Status != EnumsHelper.LeaveRequestStatus.Rejected)
        );
        if (selectedLeaveType.LeaveType.Equals(EnumsHelper.LeaveTypes.Earned)
            && (annualLeaveTaken + requestedDay) >= earnedAnnualDayTillNow)
        {
            result.Success = false;
            result.Message = "Insufficient leave balance for current month";
            return false;
        }

        if (balance.RemainingLeave < requestedDay || (balance.RemainingLeave - requestedDay) < annualLeaveTaken - earnedAnnualDayTillNow)
        {
            result.Success = false;
            result.Message = $"Only {balance.RemainingLeave} days available";
            return false;
        }
        return true;
    }


    private async void LeaveNotification(LeaveRequest leaveDomain, EnumsHelper.LeaveRequestStatus status)
    {
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
            targetUserIds = (await _empUser.GetAll(x => roleIds.Contains(x.RoleId))).Select(x => x.UserId).ToList();
            notification.Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.LeaveRequest);
            notification.Body = leaveDomain.Reason;
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequest;
        }
        else if (status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            targetUserIds.Add(leaveDomain.EmployeeId);
            notification.Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.LeaveRequestApproved);
            notification.Body = leaveDomain.Reason;
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequestApproved;
        }
        else
        {
            targetUserIds.Add(leaveDomain.EmployeeId);
            notification.Title = NotificationMessageTemplate.Create(EnumsHelper.NotificationTypes.LeaveRequestReject);
            notification.Body = leaveDomain.Reason;
            notification.NotificationType = EnumsHelper.NotificationTypes.LeaveRequestReject;
        }
        if (targetUserIds.Count == 0) return;
        Result result1 = await _notificationsRepo.AddOne(notification);
        if (!result1.Success) return;
        List<UserNotifications> userNotifications = [];
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
            await _notificationService.SendNoticeNotificationToUser(user, new NotificationViewModel()
            {
                Title = notification.Title,
                Body = notification.Body,
                IsRead = false,
                SentDateTime = DateTime.UtcNow,
                TargetId = notification.TargetId,
                NotificationTypes = notification.NotificationType,
                UserNotificationId = userNotification.UserNotificationId
            });
        }
        await _userNotificationsRepo.AddMany(userNotifications);
    }
}

