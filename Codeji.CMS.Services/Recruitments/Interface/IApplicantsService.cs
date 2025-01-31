using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ResumeApplicantModel applicantRegisterModel, string companyId, string filepath);
        Task<Result> UpdateApplicants(ApplicantRegisterModel model, string companyId);
        Task<bool> IsEmailExist(string email);
        Task<string> GetApplicantsExistingId(string email, string companyId);
        Task<List<ApplicantViewModel>> GetApplicantsList(string companyId);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);
        Task<Result> AddAppicantResume(string filePath, string companyId, string email);


    }
}

