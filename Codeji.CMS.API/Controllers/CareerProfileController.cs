using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CareerProfileController(
    IPublicCompanyProfileService service,
    IHttpContextAccessor context) : ControllerBase
{
    [HttpGet]
    [ModulePermission(AppModule.CareerProfile, Permission.View)]
    public async Task<IActionResult> Get()
    {
        var result = await service.GetCompanyProfile(CurrentContext.CompanyId(context));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut]
    [ModulePermission(AppModule.CareerProfile, Permission.Edit)]
    public async Task<IActionResult> Save(UpsertPublicCompanyProfileRequest request)
    {
        var result = await service.SaveCompanyProfile(
            CurrentContext.CompanyId(context), CurrentContext.UserId(context), request);
        return StatusCode(result.StatusCode, result);
    }
}
