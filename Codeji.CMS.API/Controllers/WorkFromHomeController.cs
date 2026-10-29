using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Route("api/wfh"), Authorize]
public class WorkFromHomeController(IWorkFromHomeService service) : ControllerBase
{
    [HttpGet("policy"), ModulePermission(AppModule.WorkFromHome, [Permission.View, Permission.ViewOwn, Permission.PolicyView])] public Task<Result<WorkFromHomePolicyDto>> Policy() => service.GetPolicy();
    [HttpGet("context"), ModulePermission(AppModule.WorkFromHome, [Permission.View, Permission.ViewOwn])] public Task<Result<EmployeeWfhContextDto>> Context() => service.GetEmployeeContext();
    [HttpPut("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result> SavePolicy(WorkFromHomePolicyDto dto) => service.SavePolicy(dto);
    // The React request helper uses POST for form mutations; retain PUT for REST clients.
    [HttpPost("policy"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)] public Task<Result> SavePolicyPost(WorkFromHomePolicyDto dto) => service.SavePolicy(dto);
    [HttpGet("allocations")]
    [ModulePermission(AppModule.WorkFromHome, [Permission.PolicyEdit, Permission.Edit, Permission.View, Permission.ViewAll])]
    [ModulePermission(AppModule.LeaveManagement, [Permission.View, Permission.Edit, Permission.Create])]
    public Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> Allocations() => service.GetEmployeeAllocations();

    [HttpPut("allocations")]
    [ModulePermission(AppModule.WorkFromHome, [Permission.PolicyEdit, Permission.Edit])]
    [ModulePermission(AppModule.LeaveManagement, [Permission.Edit, Permission.Create])]
    public Task<Result> SaveAllocation(WorkFromHomeEmployeeAllocationDto dto) => service.SaveEmployeeAllocation(dto);

    [HttpPost("allocations")]
    [ModulePermission(AppModule.WorkFromHome, [Permission.PolicyEdit, Permission.Edit])]
    [ModulePermission(AppModule.LeaveManagement, [Permission.Edit, Permission.Create])]
    public Task<Result> SaveAllocationPost(WorkFromHomeEmployeeAllocationDto dto) => service.SaveEmployeeAllocation(dto);
    [HttpPost("requests"), ModulePermission(AppModule.WorkFromHome, Permission.CreateOwn)] public Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey) => service.Create(dto, idempotencyKey);
    [HttpGet("requests/my"), ModulePermission(AppModule.WorkFromHome, [Permission.View, Permission.ViewOwn])] public Task<Result<WorkFromHomeResponseDto>> Mine([FromQuery] WorkFromHomeRequestFilterDto filter) => service.GetMy(filter);
    [HttpGet("requests/team"), ModulePermission(AppModule.WorkFromHome, Permission.ViewTeam)] public Task<Result<WorkFromHomeResponseDto>> Team([FromQuery] WorkFromHomeRequestFilterDto filter) => service.GetTeam(filter);
    [HttpGet("requests/all"), ModulePermission(AppModule.WorkFromHome, Permission.ViewAll)] public Task<Result<WorkFromHomeResponseDto>> All([FromQuery] WorkFromHomeRequestFilterDto filter) => service.GetAll(filter);
    [HttpPost("requests/{id}/approve"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Approve(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Approved", dto);
    [HttpPost("requests/{id}/reject"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Reject(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Rejected", dto);
    [HttpPost("requests/{id}/return"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Return(string id, WorkFromHomeDecisionDto dto) => service.Decide(id, "Returned", dto);
    [HttpPost("requests/{id}/cancel"), ModulePermission(AppModule.WorkFromHome, [Permission.CancelOwn, Permission.PolicyEdit])] public Task<Result> Cancel(string id, WorkFromHomeCancelDto dto) => service.Cancel(id, dto);
    [HttpPost("requests/{id}/check-in"), ModulePermission(AppModule.WorkFromHome, [Permission.CreateOwn, Permission.EditOwn])] public Task<Result> CheckIn(string id) => service.CheckIn(id);
    [HttpPost("requests/{id}/check-out"), ModulePermission(AppModule.WorkFromHome, [Permission.CreateOwn, Permission.EditOwn])] public Task<Result> CheckOut(string id, [FromBody] WorkFromHomeCheckOutDto? dto = null) => service.CheckOut(id, dto);
    [HttpPost("requests/{id}/regularize"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> Regularize(string id, [FromBody] WorkFromHomeRegularizeAttendanceDto dto) => service.RegularizeAttendance(id, dto);
    [HttpPost("requests/{id}/convert-to-office"), ModulePermission(AppModule.WorkFromHome, Permission.ApproveTeam)] public Task<Result> ConvertToOffice(string id, [FromBody] WorkFromHomeOfficeConversionDto dto) => service.ConvertApprovedWfhToOffice(id, dto);
    [HttpPost("admin-grant")]
    [ModulePermission(AppModule.WorkFromHome, [Permission.PolicyEdit, Permission.ApproveTeam])]
    [ModulePermission(AppModule.Attendance, [Permission.Edit, Permission.Override])]
    public async Task<IActionResult> AdminGrant([FromBody] WorkFromHomeAdminGrantDto dto)
    {
        var result = await service.AdminGrantAsync(dto);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }
    [HttpGet("requests/{id}/timing"), ModulePermission(AppModule.WorkFromHome, [Permission.View, Permission.ViewOwn])] public Task<Result<WorkFromHomeTimingDto>> Timing(string id) => service.GetTiming(id);
    [HttpPatch("attendance-exceptions/{id}/hours"), ModulePermission(AppModule.Attendance, Permission.Edit)]
    public async Task<IActionResult> CorrectHours(string id, WorkFromHomeHoursCorrectionDto dto)
    {
        var result = await service.CorrectInsufficientHours(id, dto);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }
    [HttpPatch("attendance-exceptions/{id}/resolve-conflict"), ModulePermission(AppModule.Attendance, Permission.Edit)]
    public async Task<IActionResult> ResolveConflict(string id, WorkFromHomeConflictResolutionDto dto)
    {
        var result = await service.ResolveAttendanceConflict(id, dto);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("outbox/dead-letter"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]
    public Task<Result<WorkFromHomeDeadLetterDto>> DeadLetterOutbox([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 25) =>
        service.GetDeadLetterOutbox(pageNo, pageSize);

    [HttpPost("outbox/dead-letter/{eventId}/replay"), ModulePermission(AppModule.WorkFromHome, Permission.PolicyEdit)]
    public async Task<IActionResult> ReplayDeadLetterOutbox(string eventId, [FromBody] WorkFromHomeDeadLetterReplayDto dto)
    {
        var result = await service.ReplayDeadLetterOutbox(eventId, dto);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.View)] public Task<Result<IEnumerable<AttendanceRemarkOptionDto>>> RemarkOptions() => service.GetRemarkOptions();
    [HttpPost("/api/attendance/remark-options"), ModulePermission(AppModule.Attendance, Permission.Edit)] public Task<Result> SaveRemarkOption(AttendanceRemarkOptionDto dto) => service.SaveRemarkOption(dto);
}
