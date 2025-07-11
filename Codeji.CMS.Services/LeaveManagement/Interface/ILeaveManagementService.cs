using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Services.LeaveManagement;

public interface ILeaveManagementService
{
    Task<Result> CreateUpdateLeaveType(LeaveTypeRequestDto leaveTypeRequestDto);
    Task<Result<LeaveTypeResponseDto>> GetLeaveType(bool? IsActive);
    Task<Result> DeleteLeaveType(string leaveTypeId);

    // leave balance 
    Task<Result> CreateUpdateLeaveBalance(LeaveBalanceRequestDto leaveBalanceRequestDto);
    Task<Result<LeaveBalanceResponseDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter);

    // leave
    Task<Result> CreateUpdateLeave(LeaveRequestDto leaveRequestDto);
    Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveFilter filter);
    Task<Result> DeleteLeaveRequest(string leaveRequestId);
    Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, EnumsHelper.LeaveRequestStatus status);
}
