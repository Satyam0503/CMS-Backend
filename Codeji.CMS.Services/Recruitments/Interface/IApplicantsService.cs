using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ApplicantRegisterModel applicantRegisterModel, string companyId);
        Task<Result> UpdateApplicants(ApplicantRegisterModel applicantRegisterModel, string ApplicantId);
        Task<string> GetApplicantsEXistingId(string email, string companyId);
        Task<List<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);


    }
}

