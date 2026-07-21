using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Route("api/attendance/penalty"), Authorize]
public class AttendancePenaltyController : ControllerBase
{
    private readonly IAttendancePenaltyService _service; private readonly IHttpContextAccessor _context;
    public AttendancePenaltyController(IAttendancePenaltyService service,IHttpContextAccessor context){_service=service;_context=context;}
    [HttpGet("policy"),ModulePermission(AppModule.Attendance,Permission.View)]
    public Task<AttendancePenaltyPolicyDto> GetPolicy([FromQuery]DateTime? effectiveOn=null)=>_service.GetPolicy(CurrentContext.CompanyId(_context),effectiveOn);
    [HttpPost("policy"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result> SavePolicy(AttendancePenaltyPolicyDto dto)=>_service.SavePolicy(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),dto);
    [HttpPost("exceptions/recalculate"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result<AttendancePayrollException>> Recalculate(AttendanceMonthRequestDto dto)=>_service.Recalculate(CurrentContext.CompanyId(_context),dto.PayrollMonth);
    [HttpPost("exceptions/search"),ModulePermission(AppModule.Attendance,Permission.View)]
    public Task<Result<AttendancePayrollException>> Search(AttendanceExceptionFilterDto dto)=>_service.GetExceptions(CurrentContext.CompanyId(_context),dto);
    [HttpPatch("exceptions/{id}/review"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result> Review(string id,AttendanceExceptionReviewDto dto)=>_service.Review(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),id,dto);
    [HttpGet("month/lock-status"),ModulePermission(AppModule.Attendance,Permission.View)]
    public Task<AttendanceMonthLockStatusDto> GetMonthLockStatus([FromQuery]DateTime month)=>_service.GetMonthLockStatus(CurrentContext.CompanyId(_context),month);
    [HttpPost("month/validate-lock"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result<MonthlyAttendanceSummary>> ValidateAndLock(AttendanceMonthRequestDto dto)=>_service.ValidateAndLock(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),dto.PayrollMonth);
}
