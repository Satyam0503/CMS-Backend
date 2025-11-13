using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveManagementController : ControllerBase
{

    private readonly ILeaveManagementService _leaveManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LeaveManagementController(ILeaveManagementService leaveManagementService, IHttpContextAccessor httpContextAccessor)
    {
        _leaveManagementService = leaveManagementService;
        _httpContextAccessor = httpContextAccessor;
    }


    [Route("CreateLeavePolicy")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Create)]
    public async Task<Result> CreateNewLeavePolicy([FromBody] LeavePolicyRequest model)
    {
        Result result = new();
        if (!ModelState.IsValid) return result;
        if ((model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly) && model.AccrualAmount <= 0)
        {
            result.StatusCode = CustomStatusCode.AccrualAmountRequired;
            return result;
        }
        if (model.CarryOverAllowed && model.CarryOverLimit == null)
        {
            result.StatusCode = CustomStatusCode.CarryOverLimitRequired;
            return result;
        }
        if (!model.CarryOverAllowed)
        {
            model.CarryOverLimit = null;
        }
        // if (model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.None)
        // {
        //     model.AccrualAmount = null;
        // }
        string company_id = CurrentContext.CompanyId(_httpContextAccessor);
        result = await _leaveManagementService.CreateNewLeavePolicy(model, company_id);
        return result;
    }

    [Route("UpdateLeavePolicy")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]
    public async Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy([FromBody] UpdateLeavePolicyRequest model)
    {
        Result<UpdateLeavePolicyRequest> result = new() { Success = false };
        if (!ModelState.IsValid) return result;
        if ((model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly) && model.AccrualAmount <= 0)
        {
            result.StatusCode = CustomStatusCode.AccrualAmountRequired;
            return result;
        }
        if (model.CarryOverAllowed && model.CarryOverLimit == null)
        {
            result.StatusCode = CustomStatusCode.CarryOverLimitRequired;
            return result;
        }
        if (!model.CarryOverAllowed)
        {
            model.CarryOverLimit = null;
        }
        // if (model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.None)
        // {
        //     model.AccrualAmount = null;
        // }
        return await _leaveManagementService.UpdateLeavePolicy(model);
    }

    [Route("GetAllLeavePolicies")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]
    public async Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies()
    {
        string company_id = CurrentContext.CompanyId(_httpContextAccessor);
        return await _leaveManagementService.GetAllLeavePolicies(company_id);
    }

    [Route("GetEmployeeLeaveBalance/{employeeId}")]
    [HttpGet]
    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        var result = await _leaveManagementService.GetEmployeeLeaveBalance(employeeId);
        return result;
    }

    // leave request services 
    [Route("CreateLeaveRequest")]
    [HttpPost]
    public async Task<Result> CreateLeaveRequest([FromBody] LeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId ??= CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.CreateLeaveRequest(leaveRequest);
    }

    [Route("UpdateLeaveRequest")]
    [HttpPost]
    public async Task<Result> UpdateLeaveRequest([FromBody] UpdateLeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId ??= CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.UpdateLeaveRequest(leaveRequest);
    }


    [Route("GetLeaveRequests")]
    [HttpPost]
    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter? leaveFilter)
    {
        return await _leaveManagementService.GetLeaveRequest(leaveFilter);
    }

    [Route("GetMyLeaveRequests")]
    [HttpPost]
    public async Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(LeaveRequestFilter leaveFilter)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.GetMyLeaveRequests(leaveFilter, userId);
    }

    [Route("DeleteLeaveRequest/{leaveRequestId}")]
    [HttpDelete]
    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        return await _leaveManagementService.DeleteLeaveRequest(leaveRequestId);
    }

    [Route("LeaveRequest/{leaveRequestId}/Status")]
    [HttpPatch]
    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, [FromBody] LeaveRequestUpdateDto model)
    {
        return await _leaveManagementService.UpdateLeaveRequestStatus(leaveRequestId, model);
    }

    [Route("GetLeaveRequestSummary")]
    [HttpGet]
    public async Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary()
    {
        return await _leaveManagementService.GetLeaveRequestSummary();
    }

    [Route("GetMonthlyTakenLeaveSummary/{year}")]
    [HttpGet]
    public async Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year)
    {
        if (year == null || year <= 0)
        {
            year = DateTime.UtcNow.Year;
        }
        return await _leaveManagementService.GetMonthlyTakenLeaveSummary(year);
    }

    // leave Balance
    [Route("UpdateEmployeeLeaveBalance")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, [Permission.Create, Permission.Edit])]
    public async Task<Result> UpdateEmpLeaveBalance(List<LeaveBalanceRequestDto> LeaveBalanceRequestDto)
    {
        return await _leaveManagementService.UpdateEmployeeLeaveBalance(LeaveBalanceRequestDto);
    }
}
