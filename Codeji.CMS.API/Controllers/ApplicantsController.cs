using AutoMapper;
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
        readonly IMapper _mapper;
        readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicantsController(IApplicantsService applicantsService,
            IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _applicantsService = applicantsService;
            _mapper = mapper;
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
        [Route("AddEditApplicant")]
        //[CustomAuthorize(Module = "Applicant", Role = ["Create"])]
        public async Task<Result> AddEditApplicant([FromBody] ApplicantRegisterModel applicantAddModel)
        {

            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            if (string.IsNullOrEmpty(applicantAddModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            string? ApplicantId = await _applicantsService.GetApplicantsExistingId(applicantAddModel.Email, companyId);
            if (string.IsNullOrEmpty(ApplicantId))
                result = await _applicantsService.RegisterApplicants(applicantAddModel, companyId);
            else
                result = await _applicantsService.UpdateApplicants(applicantAddModel, ApplicantId);
            return result;
        }
        //[HttpPost]
        //[Route("EditApplicant")]
        ////[CustomAuthorize(Module = "Applicant", Role = ["Edit"])]
        //public async Task<Result> EditApplicants([FromBody] ApplicantRegisterModel applicantEditModel)
        //{
        //    Result result = new Result();
        //    string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
        //    if (string.IsNullOrEmpty(applicantEditModel.Email))
        //        return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
        //    string? ApplicantId = await _applicantsService.GetApplicantsEXistingId(applicantEditModel.Email, companyId);
        //    if (string.IsNullOrEmpty(ApplicantId))
        //        result = await _applicantsService.RegisterApplicants(applicantEditModel, companyId);
        //    else
        //        result = await _applicantsService.UpdateApplicants(applicantEditModel, ApplicantId);
        //    return result;
        //}

    }
}

