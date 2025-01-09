using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codeji.CMS.DTO.RequestModels.Company;

namespace Codeji.CMS.Services.Interface
{
    public interface ICompanyService
    {
        Task<CompanyRequestModel> Register(CompanyRequestModel companyModel);
    }
}
