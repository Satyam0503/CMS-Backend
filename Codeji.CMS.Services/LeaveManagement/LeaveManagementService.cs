using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.LeaveManagement;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<LeaveTypes> _leaveTypeRepo;
    private readonly IMongoDbRepository<EmpUser> _empUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveBalance> _leaveBalance;
    private readonly IMongoDbRepository<LeaveRequest> _leave;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    public LeaveManagementService(IMongoDbRepository<LeaveTypes> leaveTypeRepo,
    IMongoDbRepository<EmpUser> empUser,
    IMongoDbRepository<LeaveBalance> leaveBalance,
    IMongoDbRepository<LeaveRequest> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper,
    INotificationService notificationService)
    {
        _leaveTypeRepo = leaveTypeRepo;
        _httpContextAccessor = httpContextAccessor;
        _empUser = empUser;
        _leaveBalance = leaveBalance;
        _leave = leave;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<Result> CreateUpdateLeaveType(LeaveTypeRequestDto leaveTypeRequestDto)
    {
        Result result = new();
        // Expression<Func<LeaveType, bool>> leaveTypeNameCondition = l => l.LeaveTypeName.Equals(leaveTypeResponseDto.LeaveTypeName, StringComparison.CurrentCultureIgnoreCase);
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
                    // LeaveTypeName = leaveTypeResponseDto.LeaveTypeName,
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
            // existingLeaveType.LeaveTypeName = leaveTypeResponseDto.LeaveTypeName;
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
                                                        //    LeaveTypeName = Enum.GetName(typeof(EnumsHelper.LeaveTypes),leaveType.LeaveTypes)?? "Not Defined",
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
            Expression<Func<LeaveBalance, bool>> whereCondition = l => l.EmployeeId == leaveBalanceRequestDto.EmployeeId && l.Year.Year == leaveBalanceRequestDto.Year.Year;

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
        if (leaveBalanceFilter == null)
        {
            leaveBalances = await _leaveBalance.GetAll();
        }
        else
        {
            List<string> empId = !string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) ? (await _empUser.GetAll(e => e.FirstName.ToLower().Contains(leaveBalanceFilter.EmployeeName) || e.LastName.ToLower().Contains(leaveBalanceFilter.EmployeeName))).Select(e => e.UserId).ToList() : null;
            Expression<Func<LeaveBalance, bool>> whereCondition = l =>
            (string.IsNullOrEmpty(leaveBalanceFilter.EmployeeId) || l.EmployeeId == leaveBalanceFilter.EmployeeId)
            && (string.IsNullOrEmpty(leaveBalanceFilter.EmployeeName) || empId.Contains(l.EmployeeId))
            && (leaveBalanceFilter.Year == null || (l.Year.Year == leaveBalanceFilter.Year))
            ;

            leaveBalances = (await _leaveBalance.GetAggregateDataAsync<LeaveBalance>(whereCondition, pageNo: leaveBalanceFilter.PageNo, pageSize: leaveBalanceFilter.PageSize)).ToList();
        }

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

        var currentMonth = DateTime.UtcNow.Month;
        var earnedAnnualDayTillNow = currentMonth;

        if (string.IsNullOrEmpty(leaveRequestDto.LeaveRequestId))
        {

            var requestedDay = (leaveRequestDto.EndDate.Day - leaveRequestDto.StartDate.Day) + 1;
            bool IsValid = await LeaveRequestValidation(selectedLeaveType, selectedEmpLeaveBal, _leave, leaveRequestDto, requestedDay, result);
            if (IsValid == false)
            {
                return result;
            }
            var leaveDomain = new LeaveRequest
            {
                EmployeeId = leaveRequestDto.EmployeeId,
                LeaveType = leaveRequestDto.LeaveType,
                StartDate = leaveRequestDto.StartDate,
                EndDate = leaveRequestDto.IsHalfDay ? leaveRequestDto.StartDate : leaveRequestDto.EndDate,
                TotalDays = leaveRequestDto.IsHalfDay ? 0.5m : requestedDay,
                IsHalfDay = leaveRequestDto.IsHalfDay,
                Reason = leaveRequestDto.Reason,
                ReviewedBy = "",
                Status = EnumsHelper.LeaveRequestStatus.Pending
            };

            result = await _leave.AddOne(leaveDomain);
            Expression<Func<EmpUser, bool>> whereCondition = x => (x.UserId.Contains(leaveRequestDto.EmployeeId));
            // IEnumerable<EmpUser> empUsers = (await _empUser.GetAll(whereCondition)).Select(x => x.ReportingManager).ToList();
            var empUser = await _empUser.FirstOrDefault(whereCondition);
            if (!string.IsNullOrEmpty(empUser.ReportingManager))
            {
                await _notificationService.SendNoticeNotificationToUser(empUser.UserId, new NotificationViewModel()
                {
                    Title = Enum.GetName(typeof(EnumsHelper.LeaveTypes), leaveRequestDto.LeaveType),
                    Body = leaveRequestDto.Reason,
                    IsRead = false,
                    SentDateTime = DateTime.UtcNow,
                    SentBy = CurrentContext.UserId(_httpContextAccessor),
                    TargetId = leaveRequestDto.LeaveRequestId,
                    NotificationTypes = EnumsHelper.NotificationTypes.Notice,
                    UserNotificationId = empUser.UserId
                });
            }
            return result;
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.LeaveRequestId == leaveRequestDto.LeaveRequestId;
            var existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);
            if (leaveRequestDto.Status != EnumsHelper.LeaveRequestStatus.Pending)
            {
                var reviewedBy = CurrentContext.UserId(_httpContextAccessor);
                var balance = selectedEmpLeaveBal.LeaveTypeBalances.FirstOrDefault(lb => lb.LeaveType == leaveRequestDto.LeaveType);
                var requestedDay = (leaveRequestDto.EndDate.Day - leaveRequestDto.StartDate.Day) + 1;
                bool isValid = await LeaveRequestValidation(selectedLeaveType, selectedEmpLeaveBal, _leave, leaveRequestDto, requestedDay, result);
                if (leaveRequestDto.Status == EnumsHelper.LeaveRequestStatus.Rejected)
                {
                    if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted && existingLeaveRequest.IsHalfDay == true)
                    {
                        balance.RemainingLeave += 0.5m;
                    }
                    else if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted)
                    {
                        balance.RemainingLeave += requestedDay;
                    }
                    existingLeaveRequest.ReviewedBy = reviewedBy;
                    existingLeaveRequest.Status = EnumsHelper.LeaveRequestStatus.Rejected;
                }
                else
                {
                    if (existingLeaveRequest.IsHalfDay == true && existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Accepted)
                    {
                        balance.RemainingLeave -= 0.5m;
                    }
                    else if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Accepted)
                    {
                        balance.RemainingLeave -= requestedDay;
                    }
                    existingLeaveRequest.Status = EnumsHelper.LeaveRequestStatus.Accepted;
                    existingLeaveRequest.ReviewedBy = reviewedBy;
                }
                await _leave.Update(whereCondition, existingLeaveRequest);
                result = await _leaveBalance.Update(leaveBalanceCondition, selectedEmpLeaveBal);
                return result;
            }
            else
            {
                var requestedDay = (leaveRequestDto.EndDate.Day - leaveRequestDto.StartDate.Day) + 1;
                bool IsValid = await LeaveRequestValidation(selectedLeaveType, selectedEmpLeaveBal, _leave, leaveRequestDto, requestedDay, result);
                if (IsValid == false)
                {
                    return result;
                }
                existingLeaveRequest.StartDate = leaveRequestDto.StartDate;
                existingLeaveRequest.EndDate = leaveRequestDto.EndDate;
                existingLeaveRequest.IsHalfDay = leaveRequestDto.IsHalfDay;
                existingLeaveRequest.TotalDays = leaveRequestDto.IsHalfDay ? 0.5m : requestedDay;
                existingLeaveRequest.LeaveType = leaveRequestDto.LeaveType;

                result = await _leave.Update(whereCondition, existingLeaveRequest);
                return result;
            }
        }
    }

    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveFilter? leaveFilter)
    {
        IEnumerable<LeaveRequest> leaveRequestList = [];
        var count = 0;
        if (leaveFilter == null)
        {
            leaveRequestList = await _leave.GetAll();
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = l =>
            (leaveFilter.EmployeeId == null || l.EmployeeId == leaveFilter.EmployeeId)
            && (leaveFilter.LeaveType == null || !leaveFilter.LeaveType.Any() || leaveFilter.LeaveType.Contains(l.LeaveType))
            && (leaveFilter.StartDate == null || !leaveFilter.StartDate.HasValue || (l.StartDate >= leaveFilter.StartDate))
            && (leaveFilter.EndDate == null || !leaveFilter.EndDate.HasValue || (l.EndDate <= leaveFilter.EndDate))
            && (leaveFilter.Status == null || !leaveFilter.Status.Any() || leaveFilter.Status.Contains(l.Status));

            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize)).ToList();
        }
        count = leaveRequestList.Count();
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

    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.LeaveRequestId == leaveRequestId;
        var existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);

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


    private async Task<bool> LeaveRequestValidation(LeaveTypes selectedLeaveType, LeaveBalance selectedEmpLeaveBal, IMongoDbRepository<LeaveRequest> _leave, LeaveRequestDto leaveRequestDto, int requestedDay, Result result)
    {
        var currentMonth = DateTime.UtcNow.Month;
        var earnedAnnualDayTillNow = currentMonth;
        var daysInLeave = (leaveRequestDto.StartDate.Date - DateTime.UtcNow.Date).TotalDays;

        if (selectedLeaveType.MinAdvanceNoticeDate > 0 && selectedLeaveType.MinAdvanceNoticeDate > daysInLeave)
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


        var annualLeaveTaken = await _leave.Count(l =>
            (l.EmployeeId == leaveRequestDto.EmployeeId)
            && (l.LeaveType == leaveRequestDto.LeaveType)
            && (l.StartDate.Year == DateTime.UtcNow.Year)
            && (l.Status != EnumsHelper.LeaveRequestStatus.Rejected)
        );
        if (selectedLeaveType.LeaveType.Equals(3)
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


}
