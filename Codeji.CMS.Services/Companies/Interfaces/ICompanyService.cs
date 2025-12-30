using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.Policy;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Repository.Entities.Company;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Interface
{
    public interface ICompanyService
    {
        Task<Result> Register(CompanyRequestModel companyModel);
        Task<List<Company>> GetAllCompanyList();
        Task<bool> IsActiveCompanyExist(string companyId);
        Task<Result<Company>> GetCompanyDetails(string companyId);

        Task<Result<Company>> UpdateCompanyDetails(UpdateCompanyInfoRequestModel model, string companyId);

        Task<string> UpdateCompanyLogo(IFormFile companyLogo, string companyId);

        Task<string> GetCompanyExistingLogo(string companyId);

        Task<bool> AddCompanyLogo(string fileName, string companyId);

        // services related to company policies
        Task<Result> AddPolicy(CreatePolicyRequestModel model, string companyId);
        Task<Result<PolicyResponseModel>> UpdatePolicy(UpdatePolicyRequestModel model, string companyId);
        Task<Result<PolicyResponseModel>> GetAllPolicies(string userId, string companyId);

        Task<Result<PolicyVersionResponseModel>> AddPolicyVersion(PolicyVersionRequestModel model);
        Task<Result<PolicyVersionResponseModel>> EditPolicyVersion(PolicyVersionUpdateModel model);
        Task<Result<PolicyVersionResponseModel>> GetAllPolicyVersion(string policyId);
    }
}
