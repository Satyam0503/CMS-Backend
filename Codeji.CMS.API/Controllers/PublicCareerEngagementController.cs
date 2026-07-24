using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.Services.CareerPortal.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/career")]
public sealed class PublicCareerEngagementController(ICareerEngagementService service) : ControllerBase
{
    [HttpPost("jobs/{publicJobId}/save")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Save(string publicJobId, SaveJobRequest request)
    {
        var result = await service.SaveJob(publicJobId, Session(request.SubscriberSessionToken), request.VisitorToken ?? "");
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("jobs/{publicJobId}/save")]
    public async Task<IActionResult> Unsave(string publicJobId, [FromQuery] string? visitorToken)
    {
        var result = await service.RemoveSavedJob(publicJobId, Session(), visitorToken ?? "");
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/saved")]
    public async Task<IActionResult> Saved([FromQuery] string? visitorToken)
    {
        var result = await service.GetSavedJobs(Session(), visitorToken ?? "");
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("jobs/merge-anonymous-saves")]
    public async Task<IActionResult> Merge(MergeAnonymousSavesRequest request)
    {
        var result = await service.MergeAnonymousSaves(Session(request.SubscriberSessionToken), request.VisitorToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("companies/{companyCode}/follow")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Follow(string companyCode, FollowCompanyRequest request)
    {
        var result = await service.FollowCompany(companyCode, Session(request.SubscriberSessionToken), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("companies/{companyCode}/follow")]
    public async Task<IActionResult> Unfollow(string companyCode)
    {
        var result = await service.UnfollowCompany(companyCode, Session());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("companies/{companyCode}/follow-status")]
    public async Task<IActionResult> FollowStatus(string companyCode)
    {
        var result = await service.GetFollowStatus(companyCode, Session());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("job-alerts")]
    public async Task<IActionResult> CreateAlert(JobAlertRequest request)
    {
        var result = await service.CreateAlert(Session(request.SubscriberSessionToken), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("job-alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        var result = await service.GetAlerts(Session());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("job-alerts/{alertId}")]
    public async Task<IActionResult> UpdateAlert(string alertId, JobAlertRequest request)
    {
        var result = await service.UpdateAlert(alertId, Session(request.SubscriberSessionToken), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("job-alerts/{alertId}")]
    public async Task<IActionResult> DeleteAlert(string alertId)
    {
        var result = await service.DeleteAlert(alertId, Session());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("analytics")]
    public async Task<IActionResult> Track(CareerAnalyticsRequest request)
    {
        var result = await service.Track(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("visitor-preferences")]
    public async Task<IActionResult> VisitorPreferences(VisitorPreferenceRequest request)
    {
        var result = await service.UpdateVisitorPreference(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("visitor-preferences")]
    public async Task<IActionResult> VisitorPreferences([FromQuery] string visitorToken)
    {
        var result = await service.GetVisitorPreference(visitorToken);
        return StatusCode(result.StatusCode, result);
    }

    private string Session(string? fallback = null) =>
        Request.Headers.TryGetValue("X-Career-Session", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString() : fallback ?? string.Empty;
}
