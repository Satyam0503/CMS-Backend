using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities.Recruitments;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel);
        Task<Result> ApplyNowService(ApplicantAddEditModel model);
        Task<Result> UpdateApplicants(ApplicantAddEditModel model);
        Task<Result<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters? filters);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);
        Task<Result<ApplicantLogResponseModel>> GetAllComment(string applicantId, int pageNo, int pageSize);
        Task<Result> AddComment(string userId, CommentRequestModel model);
        Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel model);

        // resume upload service 
        Task<Result> UploadResume(IFormFile resume, string email);
    }
}

