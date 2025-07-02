using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

namespace Codeji.CMS.Services.LeaveManagement.LeaveTypes;

public interface ILeaveManagementService
{
    Task<Result> CreateUpdateLeaveType(LeaveTypeResponseDto leaveTypeResponseDto);
    Task<Result<LeaveTypeRequestDto>> GetLeaveType();
    Task<Result> DeleteLeaveType(string leaveTypeId);

    // leave balance 
    Task<Result> CreateUpdateLeaveBalance(LeaveBalanceResponseDto leaveBalanceResponseDto);
    Task<Result<LeaveBalanceRequestDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter);

    // leave
    Task<Result> CreateUpdateLeave(LeaveResponseDto leaveResponseDto);
    // Task<Result<LeaveRequestDto>> GetLeave(LeaveFilter filter);
}
