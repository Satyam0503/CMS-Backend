using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;

namespace Codeji.CMS.Services.LeaveManagement;

public interface ILeaveManagementService
{
    // leave policy
    Task<Result> CreateNewLeavePolicy(LeavePolicyRequest leavePolicyDto, string company_id);
    Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy(UpdateLeavePolicyRequest model);
    Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies(string companyId, bool? status);

    // leave request services 
    Task<Result> CreateLeaveRequest(LeaveRequestDto leaveRequest);
    Task<Result> UpdateLeaveRequest(UpdateLeaveRequestDto leaveRequestDto);

    // leave
    Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter filter);
    Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(LeaveRequestFilter filter, string userId);
    Task<Result> DeleteLeaveRequest(string leaveRequestId);
    Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, LeaveRequestUpdateDto model);
    Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary();
    Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year);
    // leave balance 
    Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId);
    Task<Result> UpdateEmployeeLeaveBalance(List<LeaveBalanceRequestDto> leaveBalanceRequestDto);
    Task<Result<AllEmployeeLeaveBalance>> GetAllEmployeeLeaveBalances(LeaveBalanceFilter filter, string companyId);

    // service for leave accrual based on leave policy
    Task EmployeeLeaveBalanceAccrual();
}
