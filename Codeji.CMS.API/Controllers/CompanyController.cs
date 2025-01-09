using System.Drawing;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }
        [HttpPost]
        [Route("Register")]
        public Result Register(CompanyRequestModel companyModel)
        {
            var result = _companyService.Register(companyModel);
            return new Result()
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,                
            };
        }
    }
}
