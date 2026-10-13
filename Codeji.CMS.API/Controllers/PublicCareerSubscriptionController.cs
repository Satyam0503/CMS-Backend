using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.Services.CareerPortal.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/career/subscribers")]
public sealed class PublicCareerSubscriptionController(ICareerSubscriptionService service) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Subscribe(CareerSubscribeRequest request)
    {
        var result = await service.Subscribe(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("verify")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Verify(CareerTokenRequest request)
    {
        var result = await service.Verify(request.Token);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Resend(CareerSubscribeRequest request)
    {
        var result = await service.Resend(request.Email);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(CareerTokenRequest request)
    {
        var result = await service.Unsubscribe(request.Token);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var result = await service.GetPreferences(SessionToken());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(UpdateCareerPreferencesRequest request)
    {
        var result = await service.UpdatePreferences(SessionToken(), request);
        return StatusCode(result.StatusCode, result);
    }

    private string SessionToken() =>
        Request.Headers.TryGetValue("X-Career-Session", out var value) ? value.ToString() : string.Empty;
}
