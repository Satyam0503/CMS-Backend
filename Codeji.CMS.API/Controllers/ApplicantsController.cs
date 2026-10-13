using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicantsController : BaseApiController
    {
        readonly IApplicantsService _applicantsService;
        readonly IRecruitmentHandoffService _recruitmentHandoffService;
        readonly IRecruitmentInterviewService _recruitmentInterviewService;
        readonly IRecruitmentOfferService _recruitmentOfferService;
        readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicantsController(IApplicantsService applicantsService, IRecruitmentHandoffService recruitmentHandoffService, IRecruitmentInterviewService recruitmentInterviewService, IRecruitmentOfferService recruitmentOfferService, IHttpContextAccessor httpContextAccessor)
        {
            _applicantsService = applicantsService;
            _recruitmentHandoffService = recruitmentHandoffService;
            _recruitmentInterviewService = recruitmentInterviewService;
            _recruitmentOfferService = recruitmentOfferService;
            _httpContextAccessor = httpContextAccessor;
        }
        [HttpPost]
        [Route("GetApplicantList")]
        [ModulePermission(AppModule.Applications, Permission.View)]
        public async Task<Result<ApplicantViewModel>> GetApplicantList(ApplicantResultFilters? filters)
        {
            Result<ApplicantViewModel> data = await _applicantsService.GetApplicantsList(filters);
            return data;
        }
        [HttpGet]
        [Route("ApplicantById")]
        [ModulePermission(AppModule.Applications, Permission.View)]
        public async Task<Result<ApplicantViewModel>> ApplicantById(string id)
        {
            Result<ApplicantViewModel> result = await _applicantsService.ApplicantById(id);
            return result;
        }
        [HttpGet]
        [Route("Workflow/{id}")]
        [ModulePermission(AppModule.Applications, Permission.View)]
        public Task<Result<RecruitmentWorkflowViewModel>> Workflow(string id) => _applicantsService.GetWorkflow(id);

        [HttpPost]
        [Route("AddApplicant")]
        [ModulePermission(AppModule.Applications, Permission.Create)]
        public async Task<Result> AddApplicant([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {
            Result result = new Result();
            if (!ModelState.IsValid) return result;
            result = await _applicantsService.RegisterApplicants(applicantRegisterModel);
            return result;
        }

        [HttpPost]
        [Route("EditApplicant")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result> EditApplicants([FromBody] ApplicantAddEditModel model)
        {
            Result result = new();
            if (!ModelState.IsValid) return result;
            result = await _applicantsService.UpdateApplicants(model, CurrentContext.UserId(_httpContextAccessor));
            return result;
        }

        [HttpPost]
        [Route("UploadResume")]
        [AllowAnonymous]
        [EnableRateLimiting("career-sensitive")]
        [Obsolete("Use the application-bound public upload flow, or UploadResume/{applicantId} for authenticated recruiters.")]
        public Task<Result> UploadResume([FromForm] ResumeApplicantModel model, string email) =>
            Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status410Gone, Message = "Email-based resume uploads are retired. Use the application-bound upload flow." });

        [HttpPost]
        [Route("UploadResume/{applicantId}")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result> UploadResumeForApplicant(string applicantId, [FromForm] ResumeApplicantModel model)
        {
            if (model?.File is null) return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Resume is required." };
            return await _applicantsService.UploadResumeForApplicant(applicantId, model.File, CurrentContext.UserId(_httpContextAccessor));
        }
        [HttpPost]
        [Route("HandoffToPreboarding")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result<string>> HandoffToPreboarding([FromBody] CandidateHandoffRequest request)
        {
            if (!ModelState.IsValid)
                return new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Invalid handoff request." };
            return await _recruitmentHandoffService.HandoffToPreboarding(request, CurrentContext.UserId(_httpContextAccessor));
        }

        [HttpGet]
        [Route("InterviewRounds/{applicantId}/{vacancyId}")]
        [ModulePermission(AppModule.Applications, Permission.View)]
        public Task<Result<List<RecruitmentInterview>>> GetInterviewRounds(string applicantId, string vacancyId)
            => _recruitmentInterviewService.GetRounds(applicantId, vacancyId);

        [HttpPost]
        [Route("UpsertInterview")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result<string>> UpsertInterview([FromBody] RecruitmentInterviewCommand command)
            => await _recruitmentInterviewService.Upsert(command, CurrentContext.UserId(_httpContextAccessor));

        [HttpPost]
        [Route("UpsertOffer")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result<string>> UpsertOffer([FromBody] RecruitmentOfferCommand command)
            => await _recruitmentOfferService.Upsert(command, CurrentContext.UserId(_httpContextAccessor));

        [Route("AddComment")]
        [HttpPost]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result> AddComment(CommentRequestModel model)
        {
            string userId = CurrentContext.UserId(_httpContextAccessor);
            await _applicantsService.AddComment(userId, model);
            Result result = new Result()
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
            };
            return result;
        }

        [Route("GetAllComment/{applicantId}")]
        [HttpGet]
        [ModulePermission(AppModule.Applications, Permission.View)]
        public async Task<Result<ApplicantLogResponseModel>> GetAllComment(string applicantId, [FromQuery] int pageNo, [FromQuery] int pageSize)
        {
            var data = await _applicantsService.GetAllComment(applicantId, pageNo, pageSize);
            return data;
        }

        [HttpPost]
        [Route("GetProcessLogData")]
        [ModulePermission(AppModule.ProcessLog, Permission.View)]
        public async Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel model)
        {
            var data = await _applicantsService.GetProcessLogData(model);
            return data;
        }
    }
}




