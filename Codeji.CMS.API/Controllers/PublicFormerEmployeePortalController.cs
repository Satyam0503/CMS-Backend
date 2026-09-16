using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/former-employee")]
public sealed class PublicFormerEmployeePortalController(IFormerEmployeePortalService portal, IEmployeeExitEvidenceStorage storage) : ControllerBase
{
    [HttpPost("redeem")]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> Redeem([FromBody] FormerEmployeePortalTokenRequest request)
    {
        var result = await portal.RedeemInvite(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("otp/request")]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> RequestOtp([FromBody] FormerEmployeePortalOtpRequest request) => Ok(await portal.RequestOtp(request));

    [HttpPost("otp/verify")]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> VerifyOtp([FromBody] FormerEmployeePortalOtpVerifyRequest request)
    {
        var result = await portal.VerifyOtp(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() => Ok(await portal.Logout(SessionToken()));

    [HttpPost("verify-contact")]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> VerifyContact([FromBody] FormerEmployeePortalTokenRequest request)
    {
        var result = await portal.VerifyPostExitContact(request?.Token ?? string.Empty);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("case")]
    public async Task<IActionResult> GetCase()
    {
        var result = await portal.GetCase(SessionToken());
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("requests")]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> CreateRequest([FromBody] FormerEmployeePortalRequest request)
    {
        var result = await portal.CreateRequest(SessionToken(), request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("documents/{evidenceId}")]
    public async Task<IActionResult> DownloadDocument(string evidenceId)
    {
        var result = await portal.GetDocument(SessionToken(), evidenceId);
        if (!result.Success || result.MethodResult is null || string.IsNullOrWhiteSpace(result.MethodResult.StorageKey)) return Unauthorized(result);
        var file = await storage.OpenReadAsync(result.MethodResult.StorageKey);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet("documents/{evidenceId}/preview")]
    public async Task<IActionResult> PreviewDocument(string evidenceId)
    {
        var result = await portal.GetDocument(SessionToken(), evidenceId);
        if (!result.Success || result.MethodResult is null || string.IsNullOrWhiteSpace(result.MethodResult.StorageKey)) return Unauthorized(result);
        var file = await storage.OpenReadAsync(result.MethodResult.StorageKey);
        // No download filename intentionally: the browser renders supported formats inline.
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType);
    }

    private string SessionToken() => Request.Headers.TryGetValue("X-Former-Employee-Session", out var token) ? token.ToString() : string.Empty;
}
