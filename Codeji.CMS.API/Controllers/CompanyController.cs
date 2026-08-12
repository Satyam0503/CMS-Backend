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
    [Authorize]
    public class CompanyController : BaseApiController
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

        [HttpGet]
        [Route("career")]
        [AllowAnonymous]
        public Task<Result<PublicCareerCompanyDto>> GetPublicCareerCompany() =>
            _companyService.GetPublicCareerCompany();

        [HttpGet]
        [Route("career/{publicCompanyCode}")]
        [AllowAnonymous]
        public Task<Result<PublicCareerCompanyDto>> GetPublicCareerCompany(string publicCompanyCode) =>
            _companyService.GetPublicCareerCompany(publicCompanyCode);

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

        [HttpDelete]
        [Route("DeletePolicy/{policyId}")]
        [ModulePermission(AppModule.Policy, Permission.Delete)]
        public async Task<Result> DeletePolicy(string policyId)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            return await _companyService.DeletePolicy(policyId, companyId);
        }

        [HttpPost]
        [Route("CreatePolicyVersion")]
        [ModulePermission(AppModule.Policy, Permission.Create)]
        public async Task<Result<PolicyVersionResponseModel>> AddPolicyVersion([FromForm] PolicyVersionRequestModel model)
        {
            return await _companyService.AddPolicyVersion(model, CurrentContext.CompanyId(_httpContextAccessor));
        }

        [HttpPost]
        [Route("EditPolicyVersion")]
        [ModulePermission(AppModule.Policy, Permission.Edit)]
        public async Task<Result<PolicyVersionResponseModel>> EditPolicyVersion([FromForm] PolicyVersionUpdateModel model)
        {
            return await _companyService.EditPolicyVersion(model, CurrentContext.CompanyId(_httpContextAccessor));
        }

        [HttpPost]
        [Route("SetCurrentPolicyVersion")]
        [ModulePermission(AppModule.Policy, Permission.Edit)]
        public Task<Result<PolicyVersionResponseModel>> SetCurrentPolicyVersion([FromBody] SetCurrentPolicyVersionRequestModel model)
        {
            return _companyService.SetCurrentPolicyVersion(model, CurrentContext.CompanyId(_httpContextAccessor));
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

        [HttpDelete]
        [Route("DeletePolicyVersion/{policyVersionId}")]
        [ModulePermission(AppModule.Policy, Permission.Delete)]
        public async Task<Result> DeletePolicyVersion(string policyVersionId)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            return await _companyService.DeletePolicyVersion(policyVersionId, companyId);
        }

        [HttpGet]
        [Route("GetPolicyDocument/{policyVersionId}")]
        [ModulePermission(AppModule.Policy, Permission.View)]
        public async Task<IActionResult> GetPolicyDocument(string policyVersionId)
        {
            string userId = CurrentContext.UserId(_httpContextAccessor);
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);

            var result = await _companyService.GetPolicyDocument(policyVersionId, userId, companyId);

            if (!result.Success || result.MethodResult == null)
                return NotFound(result.Message);

            var document = result.MethodResult;

            return File(
                document.FileContent,
                document.ContentType,
                document.FileName
            );
        }
    }
}
