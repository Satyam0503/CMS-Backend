using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Codeji.CMS.Repository.Entities.Leave;

namespace Codeji.CMS.Services.LeaveManagement;

public interface ILeaveManagementService
{
    // leave policy
    Task<Result> CreateNewLeavePolicy(LeavePolicyRequest leavePolicyDto, string company_id);
    Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy(UpdateLeavePolicyRequest model);
    Task<Result> DeleteLeavePolicy(string policyId, string companyId);
    Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies(string companyId, bool? status);
    Task<Result<UpdateLeavePolicyRequest>> GetMySelfServicePolicies();

    // leave request services
    Task<Result> CreateLeaveRequest(LeaveRequestDto leaveRequest);
    Task<Result> CreateLeaveRequestWithEvidence(LeaveRequestDto leaveRequest, IFormFile evidence);
    Task<Result> UpdateLeaveRequest(UpdateLeaveRequestDto leaveRequestDto);

    // leave
    Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter filter);
    Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(EmpLeaveRequestFilter filter);
    Task<Result> DeleteLeaveRequest(string leaveRequestId);
    Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, LeaveRequestUpdateDto model);
    Task<Result> ExecuteAcceptedLeaveWfhConflictTransactionAsync(string leaveRequestId, string reason,
        Func<IClientSessionHandle, LeaveRequest, Task<Result>> applyLinkedWrites);
    Task<Result> WithdrawAcceptedLeaveForWfhConflictAsync(string leaveRequestId, string reason,
        Func<IClientSessionHandle, LeaveRequest, Task<Result>> applyLinkedWrites);
    Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary();
    Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year);
    // leave balance
    Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId);
    Task<Result> UpdateEmployeeLeaveBalance(List<LeaveBalanceRequestDto> leaveBalanceRequestDto);
    Task<Result<AllEmployeeLeaveBalance>> GetAllEmployeeLeaveBalances(LeaveBalanceFilter filter, string companyId);

    // service for leave accrual based on leave policy
    Task EmployeeLeaveBalanceAccrual();
}
