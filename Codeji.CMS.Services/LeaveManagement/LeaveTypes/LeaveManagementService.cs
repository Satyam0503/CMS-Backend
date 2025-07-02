using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.LeaveManagement.LeaveTypes;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<LeaveType> _leaveTypeRepo;
    private readonly IMongoDbRepository<EmpUser> _empUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveBalance> _leaveBalance;
    private readonly IMongoDbRepository<Leave> _leave;
    private readonly IMapper _mapper;
    public LeaveManagementService(IMongoDbRepository<LeaveType> leaveTypeRepo,
    IMongoDbRepository<EmpUser> empUser,
    IMongoDbRepository<LeaveBalance> leaveBalance,
    IMongoDbRepository<Leave> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper)
    {
        _leaveTypeRepo = leaveTypeRepo;
        _httpContextAccessor = httpContextAccessor;
        _empUser = empUser;
        _leaveBalance = leaveBalance;
        _leave = leave;
        _mapper = mapper;
    }

    public async Task<Result> CreateUpdateLeaveType(LeaveTypeResponseDto leaveTypeResponseDto)
    {
        Result result = new();
        Expression<Func<LeaveType, bool>> leaveTypeNameCondition = l => l.LeaveTypeName.Equals(leaveTypeResponseDto.LeaveTypeName, StringComparison.CurrentCultureIgnoreCase);
        if (string.IsNullOrEmpty(leaveTypeResponseDto.LeaveTypeId))
        {
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(leaveTypeNameCondition, true);
            if (existingLeaveType == null)
            {
                var leaveTypeDomain = new LeaveType
                {
                    LeaveTypeName = leaveTypeResponseDto.LeaveTypeName,
                    MinAdvanceNoticeDate = leaveTypeResponseDto.MinAdvanceNoticeDate,
                    IsHalfDay = leaveTypeResponseDto.IsHalfDay,
                    LeaveTypes = leaveTypeResponseDto.LeaveTypes,
                    CreatedDate = DateTime.UtcNow
                };
                result = await _leaveTypeRepo.AddOne(leaveTypeDomain);

            }
            else if (existingLeaveType != null && existingLeaveType.IsDeleted == true)
            {
                existingLeaveType.IsDeleted = false;
                existingLeaveType.MinAdvanceNoticeDate = leaveTypeResponseDto.MinAdvanceNoticeDate;
                existingLeaveType.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
                await _leaveTypeRepo.Update(leaveTypeNameCondition, existingLeaveType);
                result.Success = true;
            }
            return result;
        }
        else
        {

            var existingLeaveTypeName = await _leaveTypeRepo.FirstOrDefault(leaveTypeNameCondition, true);
            Expression<Func<LeaveType, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeResponseDto.LeaveTypeId;
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
            if (existingLeaveType == null)
            {
                result.Success = false;
                return result;
            }
            if (existingLeaveTypeName != null && existingLeaveTypeName.LeaveTypeName == leaveTypeResponseDto.LeaveTypeName && existingLeaveTypeName.LeaveTypeId != leaveTypeResponseDto.LeaveTypeId)
            {
                if (existingLeaveTypeName.IsDeleted == true)
                {
                    // state change of already exist leave type
                    existingLeaveTypeName.IsDeleted = false;
                    existingLeaveTypeName.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
                    existingLeaveType.MinAdvanceNoticeDate = leaveTypeResponseDto.MinAdvanceNoticeDate;
                    existingLeaveType.IsHalfDay = leaveTypeResponseDto.IsHalfDay;
                    await _leaveTypeRepo.Update(leaveTypeNameCondition, existingLeaveTypeName);
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
            existingLeaveType.LeaveTypeName = leaveTypeResponseDto.LeaveTypeName;
            existingLeaveType.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
            existingLeaveType.IsHalfDay = leaveTypeResponseDto.IsHalfDay;
            existingLeaveType.MinAdvanceNoticeDate = leaveTypeResponseDto.MinAdvanceNoticeDate;
            result = await _leaveTypeRepo.Update(whereCondition, existingLeaveType);

        }
        return result;
    }

    public async Task<Result<LeaveTypeRequestDto>> GetLeaveType()
    {
        Expression<Func<LeaveType, bool>> whereCondition = l => !l.IsDeleted;
        IEnumerable<LeaveType> leaveTypeList = [];
        leaveTypeList = await _leaveTypeRepo.GetAll(whereCondition);

        List<string> usersId = leaveTypeList.Select(l => l.CreatedBy).ToList();
        List<EmpUser> users = (await _empUser.GetAll(u => usersId.Contains(u.UserId))).ToList();

        List<LeaveTypeRequestDto> leaveTypeData = (from leaveType in leaveTypeList
                                                   join user in users on leaveType.CreatedBy equals user.UserId
                                                   select new LeaveTypeRequestDto
                                                   {
                                                       LeaveTypeId = leaveType.LeaveTypeId,
                                                       LeaveTypeName = leaveType.LeaveTypeName,
                                                       LeaveTypes = leaveType.LeaveTypes,
                                                       MinAdvanceNoticeDate = leaveType.MinAdvanceNoticeDate,
                                                       IsHalfDay = leaveType.IsHalfDay,
                                                       CreatedBy = user.FirstName + " " + user.LastName,
                                                       CreatedOn = leaveType.CreatedDate
                                                   }
                                                    ).OrderByDescending(d => d.CreatedOn).ToList();
        return new Result<LeaveTypeRequestDto>
        {
            Success = true,
            MethodResults = leaveTypeData
        };
    }

    public async Task<Result> DeleteLeaveType(string leaveTypeId)
    {
        Result result = new();
        Expression<Func<LeaveType, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeId;
        var deletedLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
        deletedLeaveType.IsDeleted = true;
        result = await _leaveTypeRepo.Update(whereCondition, deletedLeaveType);
        return result;
    }



    // leave balance 

    public async Task<Result> CreateUpdateLeaveBalance(LeaveBalanceResponseDto leaveBalanceResponseDto)
    {
        Result result = new();

        if (string.IsNullOrEmpty(leaveBalanceResponseDto.Id))
        {
            Expression<Func<LeaveBalance, bool>> whereCondition = l => l.EmployeeId == leaveBalanceResponseDto.EmployeeId && l.Year.Year == leaveBalanceResponseDto.Year.Year;

            var existingEmployeeLeaveBalance = await _leaveBalance.FirstOrDefault(whereCondition);
            if (existingEmployeeLeaveBalance == null)
            {
                InitializeRemainingBalance(leaveBalanceResponseDto);
                var leaveBalanceDomain = _mapper.Map<LeaveBalance>(leaveBalanceResponseDto);
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
            Expression<Func<LeaveBalance, bool>> whereCondition = l => l.Id == leaveBalanceResponseDto.Id;
            var existingLeaveBalance = await _leaveBalance.FirstOrDefault(whereCondition);
            existingLeaveBalance.LeaveTypeBalances = leaveBalanceResponseDto.LeaveTypeBalances;
            result = await _leaveBalance.Update(whereCondition, existingLeaveBalance);
        }

        return result;
    }


    public async Task<Result<LeaveBalanceRequestDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter)
    {
        IEnumerable<LeaveBalance> leaveBalances = [];
        var CompanyId = CurrentContext.CompanyId(_httpContextAccessor);
        if (leaveBalanceFilter == null)
        {
            leaveBalances = await _leaveBalance.GetAll();
        }
        else
        {
            Expression<Func<LeaveBalance, bool>> whereCondition = l =>
            (string.IsNullOrEmpty(leaveBalanceFilter.EmployeeId) || l.EmployeeId == leaveBalanceFilter.EmployeeId)
            && (leaveBalanceFilter.Year == null || (l.Year.Year >= leaveBalanceFilter.Year))
            ;

            leaveBalances = (await _leaveBalance.GetAggregateDataAsync<LeaveBalance>(whereCondition,pageNo:leaveBalanceFilter.PageNo,pageSize:leaveBalanceFilter.PageSize)).ToList();
        }

        List<EmpUser> users = (await _empUser.GetAll(e => CompanyId.Contains(e.CompanyId))).ToList();
        List<LeaveType> leaveTypes = (await _leaveTypeRepo.GetAll()).ToList();
        List<LeaveBalanceRequestDto> LeaveBalanceList = (from leaveBalance in leaveBalances
                                                         join user in users on leaveBalance.EmployeeId equals user.UserId
                                                         join createdByUser in users on leaveBalance.CreatedBy equals createdByUser.UserId
                                                         select new LeaveBalanceRequestDto
                                                         {
                                                             Id = leaveBalance.Id,
                                                             EmployeeId = leaveBalance.EmployeeId,
                                                             EmployeeName = user.FirstName + " " + user.LastName,
                                                             CreatedBy = createdByUser.FirstName + " " + createdByUser.LastName,
                                                             Year = leaveBalance.Year,
                                                             LeaveTypeBalances = leaveBalance.LeaveTypeBalances.Select(lb=> new LeaveTypeBalance
                                                             {
                                                                 LeaveTypeId = lb.LeaveTypeId,
                                                                 LeaveTypeName = leaveTypes.FirstOrDefault(lt => lt.LeaveTypeId == lb.LeaveTypeId)?.LeaveTypeName,
                                                                 MaximumLeave = lb.MaximumLeave,
                                                                 RemainingLeave = lb.RemainingLeave
                                                             }).ToList()
                                                         }
                                                         ).ToList();
        return new Result<LeaveBalanceRequestDto>
        {
            Success = true,
            MethodResults = LeaveBalanceList
        };
    }

    
    // Leave 
    public async Task<Result> CreateUpdateLeave(LeaveResponseDto leaveResponseDto)
{
    Result result = new();
    
    var selectedLeaveType = await _leaveTypeRepo.FirstOrDefault(lt => lt.LeaveTypeId == leaveResponseDto.LeaveTypeId);
    if (selectedLeaveType == null)
    {
        result.Success = false;
        result.Message = "Invalid Leave Type";
        return result;
    }
    
    var selectedEmpLeaveBal = await _leaveBalance.FirstOrDefault(lb => lb.EmployeeId == leaveResponseDto.EmployeeId);
    if (selectedEmpLeaveBal == null)
    {
        result.Success = false;
        result.Message = "Leave balance not found for the employee";
        return result;
    }

    var currentMonth = DateTime.UtcNow.Month;
    var earnedAnnualDayTillNow = currentMonth;

    if (string.IsNullOrEmpty(leaveResponseDto.LeaveId))
    {

        var daysInLeave = (leaveResponseDto.StartDate.Date - DateTime.UtcNow.Date).TotalDays;

        if (selectedLeaveType.MinAdvanceNoticeDate > 0 && selectedLeaveType.MinAdvanceNoticeDate > daysInLeave)
        {
            result.Success = false;
            result.Message = $"Leave must be applied at least {selectedLeaveType.MinAdvanceNoticeDate} days in advance";
            return result;
        }

        var balance = selectedEmpLeaveBal.LeaveTypeBalances.FirstOrDefault(lb => lb.LeaveTypeId == leaveResponseDto.LeaveTypeId);
        if (balance == null || balance.RemainingLeave == null || balance.RemainingLeave <= 0)
        {
            result.Success = false;
            result.Message = $"Insufficient leave balance for {selectedLeaveType.LeaveTypeName}";
            return result; 
        }

       
        var annualLeaveTaken = await _leave.Count(l =>
            (l.EmployeeId == leaveResponseDto.EmployeeId)
            && (l.LeaveTypeId == leaveResponseDto.LeaveTypeId)
            && (l.StartDate.Year == DateTime.UtcNow.Year)
            && (l.Status != EnumsHelper.LeaveRequestStatus.Rejected)
        );

        
        var requestedDay = (leaveResponseDto.EndDate.Day - leaveResponseDto.StartDate.Day) + 1;

        if (selectedLeaveType.LeaveTypeName.Equals("earned leave", StringComparison.OrdinalIgnoreCase) 
            && (annualLeaveTaken + requestedDay) >= earnedAnnualDayTillNow)
        {
            result.Success = false;
            result.Message = "Insufficient leave balance for current month";
            return result;
        }

        if (balance.RemainingLeave < requestedDay || (balance.RemainingLeave - requestedDay) < annualLeaveTaken - earnedAnnualDayTillNow)
        {
            result.Success = false;
            result.Message = $"Only {balance.RemainingLeave} days available";
            return result;
        }

        var leaveDomain = new Leave
        {
            EmployeeId = leaveResponseDto.EmployeeId,
            LeaveTypeId = leaveResponseDto.LeaveTypeId,
            StartDate = leaveResponseDto.StartDate,
            EndDate = leaveResponseDto.EndDate,
            TotalDays = requestedDay,
            Reason = leaveResponseDto.Reason,
            ApprovedBy = "",
            Status = EnumsHelper.LeaveRequestStatus.Pending
        };

        result = await _leave.AddOne(leaveDomain);
        return result;
    }

    return result;
}



    private void InitializeRemainingBalance(LeaveBalanceResponseDto leaveBalanceResponseDto)
    {
        foreach (var leaveType in leaveBalanceResponseDto.LeaveTypeBalances)
        {
            if (leaveType.RemainingLeave.HasValue || leaveType.RemainingLeave == 0)
            {
                leaveType.RemainingLeave = leaveType.MaximumLeave;
            }

        }
    }
}
