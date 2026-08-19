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
    // Payroll penalty limits are employee-specific. The route selects the target
    // employee; CompanyId and actor are always taken from the authenticated token.
    [HttpGet("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]
    public Task<Result<AttendancePenaltyPolicyDto>> GetEmployeePolicy(string employeeUserId,[FromQuery]DateTime? effectiveOn=null)=>
        _service.GetEmployeePolicy(CurrentContext.CompanyId(_context),employeeUserId,effectiveOn);
    [HttpPost("employees/{employeeUserId}/policy"),ModulePermission(AppModule.Employees,Permission.Edit)]
    public Task<Result> SaveEmployeePolicy(string employeeUserId,AttendancePenaltyPolicyDto dto)=>
        _service.SaveEmployeePolicy(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),employeeUserId,dto);
    [HttpPost("exceptions/recalculate"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result<AttendancePayrollException>> Recalculate(AttendanceMonthRequestDto dto)=>_service.Recalculate(CurrentContext.CompanyId(_context),dto.PayrollMonth);
    [HttpPost("exceptions/search"),ModulePermission(AppModule.Attendance,Permission.View)]
    public Task<Result<AttendancePayrollException>> Search(AttendanceExceptionFilterDto dto)=>_service.GetExceptions(CurrentContext.CompanyId(_context),dto);
    [HttpPatch("exceptions/{id}/review"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result> Review(string id,AttendanceExceptionReviewDto dto)=>_service.Review(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),id,dto);
    [HttpPost("exceptions/{id}/resolve-attendance"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result<AttendanceExceptionResolutionResultDto>> ResolveAttendance(string id, AttendanceExceptionResolutionDto dto) =>
        _service.ResolveAttendanceException(CurrentContext.CompanyId(_context), CurrentContext.UserId(_context), id, dto);
    [HttpGet("month/lock-status"),ModulePermission(AppModule.Attendance,Permission.View)]
    public Task<AttendanceMonthLockStatusDto> GetMonthLockStatus([FromQuery]DateTime month)=>_service.GetMonthLockStatus(CurrentContext.CompanyId(_context),month);
    [HttpPost("month/validate-lock"),ModulePermission(AppModule.Attendance,Permission.Edit)]
    public Task<Result<AttendanceMonthLockValidationResultDto>> ValidateAndLock(AttendanceMonthRequestDto dto)=>_service.ValidateAndLock(CurrentContext.CompanyId(_context),CurrentContext.UserId(_context),dto.PayrollMonth);
}
