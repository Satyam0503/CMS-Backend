using Codeji.CMS.Domain.Models;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        readonly IHttpContextAccessor _httpContextAccessor;
        public CompanyController(ICompanyService companyService, IHttpContextAccessor httpContextAccessor)
        {
            _companyService = companyService;
            _httpContextAccessor = httpContextAccessor;
        }
        [HttpGet]
        [Route("GetAllCompanyList")]

        public async Task<Result<Company>> GetAllCompanyList()
        {
            Result<Company> result = new Result<Company>();
            List<Company> data = await _companyService.GetAllCompanyList();
            result.MethodResults = data;
            return result;

        }

    }
}
