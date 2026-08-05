using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Route("api/wfh"), Authorize]
public class WorkFromHomeController(IWorkFromHomeService service) : ControllerBase
{
    [HttpGet("policy")] public Task<Result<WorkFromHomePolicyDto>> Policy() => service.GetPolicy();
    [HttpGet("context")] public Task<Result<EmployeeWfhContextDto>> Context() => service.GetEmployeeContext();
    [HttpPut("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result> SavePolicy(WorkFromHomePolicyDto dto) => service.SavePolicy(dto);
    // The React request helper uses POST for form mutations; retain PUT for REST clients.
    [HttpPost("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result> SavePolicyPost(WorkFromHomePolicyDto dto) => service.SavePolicy(dto);
    [HttpGet("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> Allocations() => service.GetEmployeeAllocations();
    [HttpPut("allocations"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result> SaveAllocation(WorkFromHomeEmployeeAllocationDto dto) => service.SaveEmployeeAllocation(dto);
    [HttpPost("requests")] public Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto) => service.Create(dto);
    [HttpGet("requests/my")] public Task<Result<WorkFromHomeResponseDto>> Mine() => service.GetMy();
    [HttpGet("requests/team"), ModulePermission(AppModule.WorkFromHome, Permission.ViewTeam)] public Task<Result<WorkFromHomeResponseDto>> Team() => service.GetTeam();
    [HttpGet("requests/all"), ModulePermission(AppModule.WorkFromHome, Permission.ViewAll)] public Task<Result<WorkFromHomeResponseDto>> All() => service.GetAll();
    [HttpPost("requests/{id}/approve"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Approve(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Approved", dto);
    [HttpPost("requests/{id}/reject"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Reject(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Rejected", dto);
    [HttpPost("requests/{id}/return"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Return(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Returned", dto);
    [HttpPost("requests/{id}/cancel")] public Task<Result> Cancel(string id, WorkFromHomeCancelDto dto) => service.Cancel(id, dto);
    [HttpPost("requests/{id}/check-in")] public Task<Result> CheckIn(string id) => service.CheckIn(id);
    [HttpPost("requests/{id}/check-out")] public Task<Result> CheckOut(string id) => service.CheckOut(id);
    [HttpGet("requests/{id}/timing")] public Task<Result<WorkFromHomeTimingDto>> Timing(string id) => service.GetTiming(id);
    [HttpPatch("attendance-exceptions/{id}/hours"), ModulePermission(AppModule.Attendance, Permission.Edit)] public Task<Result> CorrectHours(string id, WorkFromHomeHoursCorrectionDto dto) => service.CorrectInsufficientHours(id, dto);

    [HttpGet("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.View)] public Task<Result<IEnumerable<AttendanceRemarkOptionDto>>> RemarkOptions() => service.GetRemarkOptions();
    [HttpPost("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.Edit)] public Task<Result> SaveRemarkOption(AttendanceRemarkOptionDto dto) => service.SaveRemarkOption(dto);
}
