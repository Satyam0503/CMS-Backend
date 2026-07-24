using Codeji.CMS.Services.CareerPortal.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/career/companies")]
public sealed class PublicCompanyProfileController(IPublicCompanyProfileService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var result = await service.GetPublishedProfiles();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{companyCode}/profile")]
    public async Task<IActionResult> GetProfile(string companyCode)
    {
        var result = await service.GetPublishedProfile(companyCode);
        return StatusCode(result.StatusCode, result);
    }
}
