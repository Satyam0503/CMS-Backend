using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetJobs([FromQuery] PublicJobSearchRequest request)
    {
        var result = await _publicCareerService.GetMasterJobs(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{publicJobId}")]
    public async Task<IActionResult> GetJobByPublicId(string publicJobId)
    {
        var result = await _publicCareerService.GetJobByPublicId(publicJobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/by-slug/{jobSlug}")]
    public async Task<IActionResult> GetJobBySlug(string jobSlug)
    {
        var result = await _publicCareerService.GetJobBySlug(jobSlug);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("companies/{companyCode}/jobs")]
    public async Task<IActionResult> GetCompanyJobs(string companyCode, [FromQuery] PublicJobSearchRequest request)
    {
        var result = await _publicCareerService.GetCompanyJobs(companyCode, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("job-locations")]
    public async Task<IActionResult> GetLocations([FromQuery] string? companyCode, [FromQuery] string? search)
    {
        var result = await _publicCareerService.GetLocations(companyCode, search);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("jobs/{publicJobId}/applications")]
    public async Task<IActionResult> Apply(string publicJobId, [FromBody] PublicJobApplicationRequest request)
    {
        var result = await _publicCareerService.Apply(publicJobId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("applications/{applicationReference}/resume")]
    public async Task<IActionResult> UploadResume(string applicationReference, [FromForm] string token, [FromForm] IFormFile resume)
    {
        var result = await _publicCareerService.UploadResume(applicationReference, token, resume);
        return StatusCode(result.StatusCode, result);
    }
}
