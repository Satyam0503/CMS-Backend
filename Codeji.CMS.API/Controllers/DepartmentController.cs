using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentController : BaseApiController
    {
        readonly ICompanyService _companyService;
        public DepartmentController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpPost]
        [Route("AddDeparments")]
        public async Task<Result> AddDepartments(DepartmentRequestModel model)
        {
            Result data = await _companyService.AddDepartment(model);
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
            Result<Department> data = await _companyService.GetDepartmentList();
            return new Result
            {
                Success = data.Success,
                Message = data.Message,
                StatusCode = data.StatusCode
            };
        }
    }
}
