using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Authorize, Route("api/attendance/weekly-offs")]
public class WeeklyOffSettingsController(IWeeklyOffService service, IHttpContextAccessor context) : ControllerBase
{
    [HttpGet, ModulePermission(AppModule.Attendance, Permission.View)]
    public Task<WeeklyOffSettingDto> Get() => service.Get(CurrentContext.CompanyId(context));

    [HttpPost, ModulePermission(AppModule.Attendance, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> Save(WeeklyOffSettingDto dto) =>
        service.Save(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);
}
