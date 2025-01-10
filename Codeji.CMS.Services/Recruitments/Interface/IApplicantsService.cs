using System;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface
{
    public interface IApplicantsService
    {
        Task<Result> RegisterApplicants(ApplicantAddEditModel applicantRegisterModel);
        Task<Result> UpdateApplicants(ApplicantAddEditModel applicantRegisterModel);
        Task<string> GetApplicantsEXistingId(string email, string companyId = "");
        Task<List<ApplicantViewModel>> GetApplicantsList(ApplicantResultFilters filters);
        Task<Result<ApplicantViewModel>> ApplicantById(string applicantId);


    }
}

