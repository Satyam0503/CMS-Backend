using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Repository.Entities.Company;

namespace Codeji.CMS.Services.Interface
{
    public interface ICompanyService
    {
        Task<Result> Register(CompanyRequestModel companyModel);
        Task<List<Company>> GetAllCompanyList();
    }
}
