using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyMasterController : BaseApiController
{
    readonly ICompanyMasterService _companyMasterService;
    public CompanyMasterController(ICompanyMasterService companyService)
    {
        _companyMasterService = companyService;
    }

    [HttpPost]
    [Route("AddEditDepartment")]
    public async Task<Result> AddEditDepartment(DepartmentRequestModel model)
    {
        Result data = await _companyMasterService.AddEditDepartment(model);
        Result result = new()
        {
            Success = data.Success,
            StatusCode = data.StatusCode,
            Message = data.Message
        };
        return result;
    }
    [HttpGet]
    [Route("GetDepartmentList")]
    public async Task<Result<DepartmentViewModel>> GetDepartmentList()
    {
        Result<DepartmentViewModel> data = await _companyMasterService.GetDepartmentList();
        return data;
    }
}
