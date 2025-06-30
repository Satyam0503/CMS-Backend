using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave;

namespace Codeji.CMS.Services.LeaveManagement.LeaveTypes;

public interface ILeaveManagementService
{
    Task<Result> CreateUpdateLeaveType(LeaveTypeResponseDto leaveTypeResponseDto);
    Task<Result<LeaveTypeRequestDto>> GetLeaveType();
    Task<Result> DeleteLeaveType(string leaveTypeId);
}
