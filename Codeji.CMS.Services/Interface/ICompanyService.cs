using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;

namespace Codeji.CMS.Services.Interface
{
    public interface ICompanyService
    {
        Task<Result> Register(CompanyRequestModel companyModel);
    }
}
