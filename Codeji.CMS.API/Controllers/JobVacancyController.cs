using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.ApplyNow;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]

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
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        return await _jobVacancyService.AddJobVacancy(jobVacancy, companyId);
    }

    [Route("EditJobVacancy")]
    [HttpPost]
    public async Task<Result<JobVacancyModel>> EditJobVacancy(JobVacancyModel jobVacancy, string jobId)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        return await _jobVacancyService.EditJobVacancy(jobVacancy, jobId, companyId);
    }
    [Route("GetAllVacancy")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<Result<JobVacancyModel>> GetAllVacancy(ApplyNowVacancyModel model, [FromQuery] int pageNo, [FromQuery] int records)
    {
        if (string.IsNullOrEmpty(model.companyId))
        {
            model.companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        }
        Result<JobVacancyModel> data = await _jobVacancyService.GetAllVacancy(model.companyId, pageNo, records);

        return data;
    }

    [Route("GetVacancyById")]
    [HttpPost]
    [Authorize]
    public async Task<string> GetVacancyById(string vacancyId)
    {
        string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        string data = await _jobVacancyService.GetVacancyById(companyId, vacancyId);
        return data;
    }
}
