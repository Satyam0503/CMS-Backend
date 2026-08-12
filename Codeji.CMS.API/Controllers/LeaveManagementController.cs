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
public class LeaveManagementController : BaseApiController
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
        // WFH is an attendance policy and never creates a leave balance.  Its
        // inherited accrual fields must not prevent HR from saving WFH rules.
        if (model.PolicyType == EnumsHelper.LeavePolicyType.Leave &&
            (model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly) &&
            model.AccrualAmount <= 0)
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
        // WFH is an attendance policy and does not accrue a leave balance.
        if (model.PolicyType == EnumsHelper.LeavePolicyType.Leave &&
            (model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || model.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly) &&
            model.AccrualAmount <= 0)
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
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies([FromQuery] bool? status)
    {
        string company_id = CurrentContext.CompanyId(_httpContextAccessor);
        return await _leaveManagementService.GetAllLeavePolicies(company_id, status);
    }

    // Employee self-service must not depend on the HR-only policy-view permission.
    // The service resolves both user and company from the authenticated context and
    // returns only active policies that are applicable to that employee.
    [Route("me/policies")]
    [HttpGet]
    public Task<Result<UpdateLeavePolicyRequest>> GetMySelfServicePolicies() =>
        _leaveManagementService.GetMySelfServicePolicies();

    [Route("GetEmployeeLeaveBalance/{employeeId}")]
    [HttpGet]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        var result = await _leaveManagementService.GetEmployeeLeaveBalance(employeeId);
        return result;
    }

    // Profile self-service is deliberately separate from the Leave Management module.
    // The employee identity always comes from the authenticated session.
    [Route("me/balances")]
    [HttpGet]
    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetMyLeaveBalance()
    {
        return await _leaveManagementService.GetEmployeeLeaveBalance(
            CurrentContext.UserId(_httpContextAccessor));
    }

    // leave request services 
    [Route("CreateLeaveRequest")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Create)]
    public async Task<Result> CreateLeaveRequest([FromBody] LeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.CreateLeaveRequest(leaveRequest);
    }

    [Route("me/requests")]
    [HttpPost]
    public async Task<Result> CreateMyLeaveRequest([FromBody] LeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.CreateLeaveRequest(leaveRequest);
    }

    [Route("CreateEmployeeLeaveRequest/{employeeId}")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]
    public async Task<Result> CreateEmployeeLeaveRequest(
        string employeeId,
        [FromBody] LeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(employeeId))
            return new Result();

        // The target comes from the protected route, never from the request body.
        // CreateLeaveRequest also verifies that the employee belongs to this company.
        leaveRequest.UserId = employeeId;
        return await _leaveManagementService.CreateLeaveRequest(leaveRequest);
    }

    [Route("UpdateLeaveRequest")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]
    public async Task<Result> UpdateLeaveRequest([FromBody] UpdateLeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.UpdateLeaveRequest(leaveRequest);
    }

    [Route("me/requests")]
    [HttpPatch]
    public async Task<Result> UpdateMyLeaveRequest([FromBody] UpdateLeaveRequestDto leaveRequest)
    {
        if (!ModelState.IsValid) return new Result();
        leaveRequest.UserId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.UpdateLeaveRequest(leaveRequest);
    }


    [Route("GetLeaveRequests")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter? leaveFilter)
    {
        return await _leaveManagementService.GetLeaveRequest(leaveFilter);
    }

    [Route("GetMyLeaveRequests")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests([FromBody] EmpLeaveRequestFilter leaveFilter)
    {
        leaveFilter.EmployeeId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.GetMyLeaveRequests(leaveFilter);
    }

    [Route("me/requests/search")]
    [HttpPost]
    public async Task<Result<MyLeaveRequestResponse>> GetMyProfileLeaveRequests([FromBody] EmpLeaveRequestFilter leaveFilter)
    {
        leaveFilter.EmployeeId = CurrentContext.UserId(_httpContextAccessor);
        return await _leaveManagementService.GetMyLeaveRequests(leaveFilter);
    }

    [Route("DeleteLeaveRequest/{leaveRequestId}")]
    [HttpDelete]
    [ModulePermission(AppModule.LeaveManagement, Permission.Delete)]
    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        return await _leaveManagementService.DeleteLeaveRequest(leaveRequestId);
    }

    [Route("me/requests/{leaveRequestId}")]
    [HttpDelete]
    public async Task<Result> DeleteMyLeaveRequest(string leaveRequestId)
    {
        return await _leaveManagementService.DeleteLeaveRequest(leaveRequestId);
    }

    [Route("LeaveRequest/{leaveRequestId}/Status")]
    [HttpPatch]
    [ModulePermission(AppModule.LeaveManagement, Permission.Edit)]
    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, [FromBody] LeaveRequestUpdateDto model)
    {
        return await _leaveManagementService.UpdateLeaveRequestStatus(leaveRequestId, model);
    }

    [Route("GetLeaveRequestSummary")]
    [HttpGet]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary()
    {
        return await _leaveManagementService.GetLeaveRequestSummary();
    }

    [Route("GetMonthlyTakenLeaveSummary/{year}")]
    [HttpGet]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year)
    {
        if (year == null || year <= 0)
        {
            year = DateTime.UtcNow.Year;
        }
        return await _leaveManagementService.GetMonthlyTakenLeaveSummary(year);
    }

    // leave allocation
    [Route("UpdateEmployeeLeaveBalance")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, [Permission.Create, Permission.Edit])]
    public async Task<Result> UpdateEmpLeaveBalance(List<LeaveBalanceRequestDto> LeaveBalanceRequestDto)
    {
        return await _leaveManagementService.UpdateEmployeeLeaveBalance(LeaveBalanceRequestDto);
    }

    [Route("GetAllEmployeeLeaveBalances")]
    [HttpPost]
    [ModulePermission(AppModule.LeaveManagement, Permission.View)]
    public async Task<Result<AllEmployeeLeaveBalance>> GetAllEmployeeLeaveBalances([FromBody] LeaveBalanceFilter filter)
    {
        string company_id = CurrentContext.CompanyId(_httpContextAccessor);
        return await _leaveManagementService.GetAllEmployeeLeaveBalances(filter, company_id);
    }
}
