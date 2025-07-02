using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.Services.LeaveManagement.LeaveTypes;
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
    public async Task<Result> CreateUpdateLeaveType(LeaveTypeResponseDto leaveTypeResponseDto)
    {
        return await _leaveManagementService.CreateUpdateLeaveType(leaveTypeResponseDto);
    }

    [Route("GetLeaveType")]
    [HttpGet]
    public async Task<Result<LeaveTypeRequestDto>> GetLeaveType()
    {
        return await _leaveManagementService.GetLeaveType();
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
    public async Task<Result> CreateUpdateLeaveBalance(LeaveBalanceResponseDto leaveBalanceResponseDto)
    {
        return await _leaveManagementService.CreateUpdateLeaveBalance(leaveBalanceResponseDto);
    }

    [Route("GetLeaveBalance")]
    [HttpPost]
    public async Task<Result<LeaveBalanceRequestDto>> GetLeaveBalance(LeaveBalanceFilter? leaveBalanceFilter)
    {
        return await _leaveManagementService.GetLeaveBalance(leaveBalanceFilter);
    }


    // leave request
    [Route("CreateUpdateLeave")]
    [HttpPost]
    public async Task<Result> CreateUpdateLeave(LeaveResponseDto leaveResponseDto)
    {
        return await _leaveManagementService.CreateUpdateLeave(leaveResponseDto);
    }
}
