
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.DTO.Company
{
    public class UpdateCompanyInfoRequestModel
    {
        public string? CompanyName { get; set; }
        public string? DefaultLanguage { get; set; }
        public List<string> ApplicationLanguage { get; set; }
        public IFormFile? CompanyLogo { get; set; }
    }
}