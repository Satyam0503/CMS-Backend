using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Codeji.CMS.DTO.Company.Policy;
using System.Security.Policy;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<Company>> GetAllCompanyList()
        {
            Result<Company> result = new Result<Company>();
            List<Company> data = await _companyService.GetAllCompanyList();
            result.MethodResults = data;
            return result;

        }

        [HttpGet]
        [Route("GetCompanyDetails")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<Company>> GetCompanyDetails()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.GetCompanyDetails(companyId);
            return result;
        }

        [HttpPost]
        [Route("UpdateCompanyDetails")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<Result<Company>> UpdateCompanyDetails([FromForm] UpdateCompanyInfoRequestModel model)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.UpdateCompanyDetails(model, companyId);
            return result;
        }

        // services related to company policies
        [HttpPost]
        [Route("AddPolicy")]
        [ModulePermission(AppModule.Policy, Permission.Create)]
        public async Task<Result> AddPolicy([FromBody] CreatePolicyRequestModel model)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.AddPolicy(model, companyId);
            return result;
        }

        [HttpPost]
        [Route("UpdatePolicy")]
        [ModulePermission(AppModule.Policy, Permission.Edit)]
        public async Task<Result> UpdatePolicy([FromBody] UpdatePolicyRequestModel model)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var result = await _companyService.UpdatePolicy(model, companyId);
            return result;
        }

        [HttpGet]
        [Route("GetAllPolicies")]
        [ModulePermission(AppModule.Policy, Permission.View)]
        public async Task<Result<PolicyResponseModel>> GetAllPolicies()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            string userId = CurrentContext.UserId(_httpContextAccessor);
            var result = await _companyService.GetAllPolicies(userId, companyId);
            return result;
        }

        [HttpPost]
        [Route("CreatePolicyVersion")]
        [ModulePermission(AppModule.Policy, Permission.Create)]
        public async Task<Result<PolicyVersionResponseModel>> AddPolicyVersion([FromForm] PolicyVersionRequestModel model)
        {
            return await _companyService.AddPolicyVersion(model);
        }

        [HttpPost]
        [Route("EditPolicyVersion")]
        [ModulePermission(AppModule.Policy, Permission.Edit)]
        public async Task<Result<PolicyVersionResponseModel>> EditPolicyVersion([FromForm] PolicyVersionUpdateModel model)
        {
            return await _companyService.EditPolicyVersion(model);
        }

        [HttpGet]
        [Route("GetAllPolicyVersion/{policyId}")]
        [ModulePermission(AppModule.Policy, Permission.View)]
        public async Task<Result<PolicyVersionResponseModel>> GetAllPolicyVersion(string policyId)
        {
            string userId = CurrentContext.UserId(_httpContextAccessor);
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            return await _companyService.GetAllPolicyVersion(policyId, userId, companyId);
        }
    }
}
