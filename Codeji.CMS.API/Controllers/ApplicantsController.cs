using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
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
        [HttpPost]
        [Route("AddApplicant")]
        [ModulePermission(AppModule.Applications, Permission.Create)]
        public async Task<Result> AddApplicant([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {
            Result result = new Result();
            if (!ModelState.IsValid)
            {
                return result;
            }
            result = await _applicantsService.RegisterApplicants(applicantRegisterModel);
            return result;
        }

        [HttpPost]
        [Route("EditApplicant")]
        [ModulePermission(AppModule.Applications, Permission.Edit)]
        public async Task<Result> EditApplicants([FromBody] ApplicantAddEditModel model)
        {
            var result = await _applicantsService.UpdateApplicants(model);
            return result;
        }

        [HttpPost]
        [Route("UploadResume")]
        //[FilesExtensions([".pdf"])]
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
