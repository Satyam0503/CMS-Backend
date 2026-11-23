using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicCareerController : ControllerBase
{
    private readonly IPublicCareerService _publicCareerService;

    public PublicCareerController(IPublicCareerService publicCareerService)
    {
        _publicCareerService = publicCareerService;
    }

    [HttpGet("jobs")]
    [EnableRateLimiting("career-search")]
    public async Task<IActionResult> GetJobs([FromQuery] PublicJobSearchRequest request)
    {
        var result = await _publicCareerService.GetMasterJobs(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{publicJobId}")]
    [EnableRateLimiting("career-search")]
    public async Task<IActionResult> GetJobByPublicId(string publicJobId)
    {
        var result = await _publicCareerService.GetJobByPublicId(publicJobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/by-slug/{jobSlug}")]
    [EnableRateLimiting("career-search")]
    public async Task<IActionResult> GetJobBySlug(string jobSlug)
    {
        var result = await _publicCareerService.GetJobBySlug(jobSlug);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("companies/{companyCode}/jobs")]
    [EnableRateLimiting("career-search")]
    public async Task<IActionResult> GetCompanyJobs(string companyCode, [FromQuery] PublicJobSearchRequest request)
    {
        var result = await _publicCareerService.GetCompanyJobs(companyCode, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("job-locations")]
    [EnableRateLimiting("career-search")]
    public async Task<IActionResult> GetLocations([FromQuery] string? companyCode, [FromQuery] string? search)
    {
        var result = await _publicCareerService.GetLocations(companyCode, search);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("jobs/{publicJobId}/applications")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> Apply(string publicJobId, [FromBody] PublicJobApplicationRequest request)
    {
        var result = await _publicCareerService.Apply(publicJobId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("applications/status")]
    [EnableRateLimiting("career-sensitive")]
    public async Task<IActionResult> GetApplicationStatus([FromBody] PublicApplicationStatusRequest request)
    {
        var result = await _publicCareerService.GetApplicationStatus(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("applications/{applicationReference}/resume")]
    [EnableRateLimiting("career-sensitive")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024, ValueCountLimit = 8, MultipartHeadersCountLimit = 16, MultipartHeadersLengthLimit = 8192)]
    public async Task<IActionResult> UploadResume(string applicationReference, [FromForm] string token, [FromForm] IFormFile resume)
    {
        var result = await _publicCareerService.UploadResume(applicationReference, token, resume);
        return StatusCode(result.StatusCode, result);
    }
}
