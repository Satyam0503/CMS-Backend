using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveManagementController : ControllerBase
{

    private readonly ILeaveManagementService _leaveManagementService;

    public LeaveManagementController(ILeaveManagementService leaveManagementService)
    {
        _leaveManagementService = leaveManagementService;
    }

    [Route("CreateUpdateLeaveType")]
    [HttpPost]
    public async Task<Result> CreateUpdateLeaveType(LeaveTypeRequestDto leaveTypeRequestDto)
    {
        return await _leaveManagementService.CreateUpdateLeaveType(leaveTypeRequestDto);
    }

    [Route("GetLeaveType")]
    [HttpGet]
    public async Task<Result<LeaveTypeResponseDto>> GetLeaveType(bool? IsActive)
    {
        return await _leaveManagementService.GetLeaveType(IsActive);
    }

    [Route("DeleteLeaveType/{leaveTypeId}")]
    [HttpDelete]
    public async Task<Result> DeleteLeaveType(string leaveTypeId)
    {
        return await _leaveManagementService.DeleteLeaveType(leaveTypeId);
    }


    // leave Balance
    [Route("CreateUpdateLeaveBalance")]
    [HttpPost]
    public async Task<Result> CreateUpdateLeaveBalance(LeaveBalanceRequestDto LeaveBalanceRequestDto)
    {
        return await _leaveManagementService.CreateUpdateLeaveBalance(LeaveBalanceRequestDto);
    }
    [Route("GetLeaveBalance")]
    [HttpPost]
    public async Task<Result<LeaveBalanceResponseDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter)
    {
        return await _leaveManagementService.GetLeaveBalance(leaveBalanceFilter);
    }


    // leave request
    [Route("CreateUpdateLeave")]
    [HttpPost]
    public async Task<Result> CreateUpdateLeave(LeaveRequestDto leaveRequestDto)
    {
        return await _leaveManagementService.CreateUpdateLeave(leaveRequestDto);
    }

    [Route("GetLeaveRequest")]
    [HttpPost]
    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveFilter? leaveFilter)
    {
        return await _leaveManagementService.GetLeaveRequest(leaveFilter);
    }

    [Route("DeleteLeaveRequest")]
    [HttpDelete]
    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        return await _leaveManagementService.DeleteLeaveRequest(leaveRequestId);
    }

    [Route("LeaveRequest/{leaveRequestId}/Status")]
    [HttpPatch]
    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, [FromBody] EnumsHelper.LeaveRequestStatus status)
    {
        return await _leaveManagementService.UpdateLeaveRequestStatus(leaveRequestId, status);
    }

    [Route("GetEmployeeLeaveBalance/{employeeId}")]
    [HttpGet]
    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        var result = await _leaveManagementService.GetEmployeeLeaveBalance(employeeId);
        return result;
    }

}
