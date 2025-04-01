using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel);
        Task<Result> UpdateApplicants(ApplicantAddEditModel model);
        Task<bool> IsEmailExist(string email);
        Task<Result> GetApplicantsExistingId(string email);
        Task<string> GetApplicantExistingResume(string email);
        Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters, int pageNo, int records);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);
        Task<Result> AddAppicantResume(string fileName, string email, string filePath);
        Task<List<ApplicantLogResponseModel>> GetAllComment(string applicantId);
        Task<Result> AddComment(string userId, CommentRequestModel model);
        Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel model);
    }
}

