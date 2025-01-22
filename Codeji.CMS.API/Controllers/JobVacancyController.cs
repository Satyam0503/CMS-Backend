using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize]
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
    [HttpGet]
    public async Task<Result<JobVacancyModel>> GetAllVacancy()
    {
        List<JobVacancyModel> data = await _jobVacancyService.GetAllVacancy();
        Result<JobVacancyModel> result = new Result<JobVacancyModel>()
        {
            Success = true,
            MethodResults = data.ToList()
        };

        return result;
    }
}
