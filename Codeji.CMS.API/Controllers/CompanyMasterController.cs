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
    public async Task<Result> AddEditDepartment(DepartmentDTO model)
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
    public async Task<Result<DepartmentDTO>> GetDepartmentList()
    {
        Result<DepartmentDTO> data = await _companyMasterService.GetDepartmentList();
        return data;
    }

    [HttpDelete]
    [Route("DeleteDepartment/{departmentId}")]
    public async Task<Result> DeleteDepartment(string departmentId)
    {
        var success = await _companyMasterService.DeleteDepartment(departmentId);
        if (!success)
        {
            return new Result()
            {
                Message = "Department Not Found",
                Success = false,
                StatusCode= 200,
            };
        }
        return new Result()
        {
            Message = "Department Deleted Successfully",
            StatusCode = 200,
            Success = true,
        };
    }
}
