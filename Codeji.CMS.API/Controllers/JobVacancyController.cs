using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.ApplyNow;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]


public class JobVacancyController : BaseApiController
{
    private readonly IJobVacancy _jobVacancyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JobVacancyController(IJobVacancy jobVacancyService, IHttpContextAccessor httpContextAccessor)
    {
        _jobVacancyService = jobVacancyService;
        _httpContextAccessor = httpContextAccessor;

    }

    [Route("AddJobVacancy")]
    [HttpPost]

    public async Task<Result<JobVacancyModel>> AddJobVacancy(JobVacancyModel jobVacancy)
    {
        return await _jobVacancyService.AddJobVacancy(jobVacancy);
    }

    [Route("EditJobVacancy")]
    [HttpPost]
    public async Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId)
    {
        return await _jobVacancyService.EditJobVacancy(jobVacancy, jobId);
    }
    [Route("GetAllVacancy")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<Result<JobVacancyModel>> GetAllVacancy([FromQuery] int pageNo, [FromQuery] int records)
    {
        Result<JobVacancyModel> data = await _jobVacancyService.GetAllVacancy(pageNo, records);

        return data;
    }

    [Route("GetVacancyById")]
    [HttpPost]

    public async Task<string> GetVacancyById(string vacancyId)
    {
        var data = await _jobVacancyService.GetVacancyById(vacancyId);
        return data?.Title ?? string.Empty;
    }

    [Route("DeleteJobVacancy")]
    [HttpDelete]

    public async Task<Result> DeleteJobVacancy([FromQuery] string vacancyId)
    {
        Result data = await _jobVacancyService.DeleteJobVacancy(vacancyId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                Message = "Job Not Deleted ",
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            Message = "Job Deleted Successfully",
            StatusCode = 200
        };
    }
}
