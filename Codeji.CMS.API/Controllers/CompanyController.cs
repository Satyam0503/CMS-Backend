using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpGet]
        [Route("GetCompanyDetails")]
        public async Task<Result<Company>> GetCompanyDetails()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.GetCompanyDetails(companyId);
            return result;
        }

        [HttpPost]
        [Route("UpdateCompanyDetails")]

        public async Task<Result<Company>> UpdateCompanyDetails([FromForm] UpdateCompanyInfoRequestModel model)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.UpdateCompanyDetails(model, companyId);
            return result;
        }

    }
}
