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
        public async Task<Result> AddApplicant([FromForm] ResumeApplicantModel model)
        {

            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            if (string.IsNullOrEmpty(model.Applicant.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };

            bool isEmailExist = await _applicantsService.IsEmailExist(model.Applicant.Email);

            if (isEmailExist)
                return new Result() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Email Already Exist" };

            //Logic For Resume Adding
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\Resume");
            string fileExtension = Path.GetExtension(model.File.FileName);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string uniqueFileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder, uniqueFileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(fileStream);
            };

            string? ApplicantId = await _applicantsService.GetApplicantsExistingId(model.Applicant.Email, companyId);
            if (string.IsNullOrEmpty(ApplicantId))
            {
                result = await _applicantsService.RegisterApplicants(model, companyId, filePath);
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

        [HttpPost]
        [Route("UploadResume")]
        [AllowAnonymous]
        //[CustomAuthorize(Module = "Applicant", Role = ["Edit"])]
        public async Task<Result> UploadResume([FromForm] ResumeApplicantModel model)
        {
            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\Resume");
            string fileExtension = Path.GetExtension(model.File.FileName);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string uniqueFileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder, uniqueFileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(fileStream);
            };
            string? ApplicantId = await _applicantsService.GetApplicantsExistingId(model.Applicant.Email, companyId);
            if (string.IsNullOrEmpty(ApplicantId))
            {
                result.Success = false;
                result.Message = "Please Check The Email";
                result.StatusCode = 400;
            }
            else
            {
                result = await _applicantsService.AddAppicantResume(filePath, companyId, model.Applicant.Email);

            }

            return result;
        }
    }
}

