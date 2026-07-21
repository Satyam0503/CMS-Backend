using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Route("api/attendance-status-settings"), Authorize]
public class AttendanceStatusSettingsController : ControllerBase
{
    private readonly IAttendanceStatusService _service;
    private readonly IHttpContextAccessor _context;
    public AttendanceStatusSettingsController(IAttendanceStatusService service, IHttpContextAccessor context) { _service=service; _context=context; }
    [HttpGet]
    public async Task<Result<AttendanceStatusSettingDto>> Get([FromQuery] bool activeOnly=true) => new() { MethodResults=await _service.Get(CurrentContext.CompanyId(_context), activeOnly) };
    [HttpPost, Authorize(Policy="AdminOnly")]
    public Task<Result> Save(AttendanceStatusSettingDto dto) => _service.Save(CurrentContext.CompanyId(_context), dto);
}
