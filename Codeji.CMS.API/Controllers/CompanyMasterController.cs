using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
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
  [Route("AddDeparments")]
  public async Task<Result> AddDepartments(DepartmentRequestModel model)
  {
    Result data = await _companyMasterService.AddDepartment(model);
    Result result = new Result()
    {
      Success = data.Success,
      StatusCode = data.StatusCode,
      Message = data.Message
    };
    return result;
  }
  [HttpGet]
  [Route("GetDepartmentList")]
  public async Task<Result> GetDepartmentList()
  {
    Result<DepartmentViewModel> data = await _companyMasterService.GetDepartmentList();
    return new Result
    {
      Success = data.Success,
      Message = data.Message,
      StatusCode = data.StatusCode
    };
  }
}
