using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments;
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
        IApplicantsService _applicantsService;
        IMapper _mapper;
        public ApplicantsController(IApplicantsService applicantsService,
            IMapper mapper)
        {
            _applicantsService = applicantsService;
            _mapper = mapper;
        }
        [HttpPost]
        [Route("GetApplicantList")]
        [CustomAuthorizeAttribute]
        public async Task<Result<ApplicantViewModel>> GetApplicantList(ApplicantResultFilters filters)
        {
            Result<ApplicantViewModel> result = new Result<ApplicantViewModel>();
           var data= await _applicantsService.GetApplicantsList(filters);
            result.Success = data.Any();
            result.MethodResults = data;
            return result;
        }
        [HttpGet]
        [Route("ApplicantById")]
        [CustomAuthorizeAttribute]
        public async Task<Result<ApplicantViewModel>> ApplicantById(string id)
        {
            Result<ApplicantViewModel> result = await _applicantsService.ApplicantById(id);
            return result;
        }
        [HttpPost]
        [Route("AddApplicant")]
        public async Task<Result> AppApplicants([FromBody] ApplicantAddEditModel applicantAddModel)
        {

            Result result = new Result();
            if (string.IsNullOrEmpty(applicantAddModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            string? ApplicantId = await _applicantsService.GetApplicantsEXistingId(applicantAddModel.Email);
            if (string.IsNullOrEmpty(ApplicantId))
                result = await _applicantsService.RegisterApplicants(applicantAddModel);
            else
                result = await _applicantsService.UpdateApplicants(applicantAddModel);
            return result;
        }
        [HttpPost]
        [Route("EditApplicant")]
        public async Task<Result> EditApplicants([FromBody] ApplicantAddEditModel applicantEditModel)
        {
            Result result = new Result();
            if (string.IsNullOrEmpty(applicantEditModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            string? ApplicantId = await _applicantsService.GetApplicantsEXistingId(applicantEditModel.Email);
            if (string.IsNullOrEmpty(ApplicantId))
                result = await _applicantsService.RegisterApplicants(applicantEditModel);
            else
                result = await _applicantsService.UpdateApplicants(applicantEditModel);
            return result;
        }

    }
}

