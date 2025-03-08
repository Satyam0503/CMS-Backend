using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel, string companyId);
        Task<Result> UpdateApplicants(ApplicantAddEditModel model, string companyId);
        Task<bool> IsEmailExist(string email);
        Task<string> GetApplicantsExistingId(string email, string companyId);
        Task<string> GetApplicantExistingResume(string email);
        Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters, string companyId, int pageNo, int records);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);
        Task<Result> AddAppicantResume(string fileName, string email, string filePath);


    }
}

