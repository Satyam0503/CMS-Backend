using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicantsController : BaseApiController
    {
        readonly IApplicantsService _applicantsService;
        readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicantsController(IApplicantsService applicantsService, IHttpContextAccessor httpContextAccessor)
        {
            _applicantsService = applicantsService;
            _httpContextAccessor = httpContextAccessor;
        }
        [HttpGet]
        [Route("GetApplicantList")]
        //[CustomAuthorize(Module = "Applicant", Role = ["View"])]
        public async Task<Result<ApplicantViewModel>> GetApplicantList()
        {
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>();
            List<ApplicantViewModel> data = await _applicantsService.GetApplicantsList(companyId);
            result.Success = data.Any();
            result.MethodResults = data;
            return result;
        }
        [HttpGet]
        [Route("ApplicantById")]
        //[CustomAuthorize(Module = "Applicant", Role = ["View"])]
        public async Task<Result<ApplicantViewModel>> ApplicantById(string id)
        {
            Result<ApplicantViewModel> result = await _applicantsService.ApplicantById(id);
            return result;
        }
        [HttpPost]
        [Route("AddApplicant")]
        //[CustomAuthorize(Module = "Applicant", Role = ["Create"])]
        public async Task<Result> AddApplicant([FromBody] ApplicantRegisterModel applicantAddModel)
        {

            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            if (string.IsNullOrEmpty(applicantAddModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            string? ApplicantId = await _applicantsService.GetApplicantsExistingId(applicantAddModel.Email, companyId);
            if (string.IsNullOrEmpty(ApplicantId))
            {
                result = await _applicantsService.RegisterApplicants(applicantAddModel, companyId);
            }
            else
            {
                result.Success = false;
                result.Message = "Applicant Already Exists";
            }
            return result;
        }

        [HttpPost]
        [Route("EditApplicant")]
        //[CustomAuthorize(Module = "Applicant", Role = ["Edit"])]
        public async Task<Result> EditApplicants([FromBody] ApplicantRegisterModel model)
        {
            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            result = await _applicantsService.UpdateApplicants(model, companyId);
            return result;
        }

    }
}

