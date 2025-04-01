using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities.Recruitments;
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
        [HttpPost]
        [Route("GetApplicantList")]
        //[CustomAuthorize(Module = "Applicant", Role = ["View"])]
        public async Task<Result<ApplicantViewModel>> GetApplicantList(ApplicantResultFilters filters, [FromQuery] int pageNo, [FromQuery] int records)
        {
            Result<ApplicantViewModel> data = await _applicantsService.GetApplicantsList(filters, pageNo, records);
            return data;
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
        public async Task<Result> AddApplicant([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {

            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            if (string.IsNullOrEmpty(applicantRegisterModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            var isExist = await _applicantsService.IsEmailExist(applicantRegisterModel.Email);
            if (isExist == false)

            {
                result = await _applicantsService.RegisterApplicants(applicantRegisterModel);
                result.Success = true;
                result.Message = "Application has been submitted successfully";
            }

            else
            {
                result.Success = false;
                result.Message = "Application has already been submitted. Please reapply after the waiting period";
            }
            return result;
        }

        [HttpPost]
        [Route("EditApplicant")]
        //[CustomAuthorize(Module = "Applicant", Role = ["Edit"])]
        public async Task<Result> EditApplicants([FromBody] ApplicantAddEditModel model)
        {
            Result result = new Result();
            string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            result = await _applicantsService.UpdateApplicants(model);
            return result;
        }

        [HttpPost]
        [Route("UploadResume")]
        [FilesExtensions([".pdf"])]
        [AllowAnonymous]
        //[CustomAuthorize(Module = "Applicant", Role = ["Edit"])]
        public async Task<Result> UploadResume([FromForm] ResumeApplicantModel model, string email)
        {
            Result result = new Result();
            //string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads/Resume/");
            string fileExtension = Path.GetExtension(model.File.FileName);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder + fileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(fileStream);
            }
            ;
            string? resume = await _applicantsService.GetApplicantExistingResume(email);
            if (string.IsNullOrEmpty(resume))
            {
                result = await _applicantsService.AddAppicantResume(fileName, email, filePath);
            }
            else
            {
                string oldPath = Path.Combine(uploadFolder, resume);
                FileInfo fileInfo = new(oldPath);
                fileInfo.Delete();
                result = await _applicantsService.AddAppicantResume(fileName, email, filePath);
            }

            return result;
        }
        [Route("AddComment")]
        [HttpPost]
        public async Task<Result> AddComment(CommentRequestModel model)
        {
            string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
            await _applicantsService.AddComment(userId, model);
            Result result = new Result()
            {
                Success = true,
                Message = "Comment Added Successfully",
                StatusCode = StatusCodes.Status200OK,
            };
            return result;
        }

        [Route("GetAllComment")]
        [HttpGet]
        public async Task<Result<ApplicantLogResponseModel>> GetAllComment([FromQuery] string applicantId)
        {
            var list = await _applicantsService.GetAllComment(applicantId);
            Result<ApplicantLogResponseModel> result = new()
            {
                Success = true,
                Message = "All Comments",
                StatusCode = StatusCodes.Status200OK,
                MethodResults = [.. list]
            };
            return result;
        }
        [HttpPost]
        [Route("GetProcessLogData")]
        public async Task<Result<ApplicantLogResponseModel>> GetProcessLogData(ApplicantLogFilterModel model)
        {
            var data = await _applicantsService.GetProcessLogData(model);
            return data;
        }


    }
}
