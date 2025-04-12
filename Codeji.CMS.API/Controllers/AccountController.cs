using System.Net;
using AutoMapper;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.ApplyNow;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Codeji.CMS.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class AccountController : BaseApiController
    {
        private readonly IAntiforgery _antiforgery;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICompanyService _companyService;
        private readonly IApplicantsService _applicantsServices;
        private readonly IMapper _mapper;
        private readonly IEmployeeService _employeeService;
        private readonly HttpClient _httpClient;
        private readonly IPriorityTaskQueue _priorityTaskQueue;
        private readonly IMiddlewareService _middlewareService;
        public AccountController(IEmployeeService userService,
            IConfiguration configuration,
            IAntiforgery antiforgery,
            IHttpContextAccessor httpContextAccessor,
            IApplicantsService applicantsServices,
            ICompanyService companyService,
            IMapper mapper,
            HttpClient httpClient,
            IPriorityTaskQueue priorityTaskQueue,
            IMiddlewareService middlewareService)
        {
            _antiforgery = antiforgery;
            _httpContextAccessor = httpContextAccessor;
            _employeeService = userService;
            _applicantsServices = applicantsServices;
            _mapper = mapper;
            _companyService = companyService;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
        }
        /// This block contains pure anonymous API
        [HttpGet]
        [Route("account/antiforgerytoken/{appKey}")]
        [AllowAnonymous]
        public Result<AntiForgeryResponse> GetAntiForgeryToken(string appKey)
        {
            Result<AntiForgeryResponse> result = new Result<AntiForgeryResponse>();
            AntiForgeryResponse model = new AntiForgeryResponse();
            if (appKey == "uiploutssh-817181871" && _httpContextAccessor.HttpContext != null)
            {
                AntiforgeryTokenSet token = _antiforgery.GetAndStoreTokens(_httpContextAccessor.HttpContext);
                model.RequestToken = token.RequestToken;
                model.CookieToken = token.CookieToken;
                result.MethodResult = model;
                result.Success = true;
                result.StatusCode = StatusCodes.Status200OK;
            }
            else
            {
                result.Success = false;
                result.StatusCode = StatusCodes.Status400BadRequest;
            }
            return result;
        }
        [HttpGet]
        [Route("app/checkAppVersion")]
        [AllowAnonymous]
        public Result CheckAppVersion(string appVersion)
        {
            return new Result()
            {
                Success = appVersion == ConfigManager.AppSettings.AppVersion,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "App request is old"
            };
        }


        //Registering Company
        [HttpPost]
        [Route("account/register")]
        public async Task<Result> register([FromBody] CompanyRequestModel companyModel)
        {
            //Validating Company Registration
            bool isEmailExist = await _employeeService.IsEmailExist(companyModel.Email);
            if (isEmailExist)
            {
                return new Result()
                {
                    Message = "Company Already Exist",
                    StatusCode = StatusCodes.Status406NotAcceptable
                };
            }
            Result result = await _companyService.Register(companyModel);
            return new Result()
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
            };
        }


        [HttpPost]
        [Route("account/login")]
        [AllowAnonymous]
        public async Task<Result> login([FromBody] LoginModel model)
        {
            Result result = new Result();
            bool isEmailExist = await _employeeService.IsEmailExist(model.Email);
            if (!isEmailExist)
            {
                result.Message = "Incorrect Credentials";
                result.Success = false;
                result.StatusCode = 400;
                return result;

            }
            if (!ModelState.IsValid)
                return result;

            string token = await _employeeService.GetVerificationToken(model.Email, model.Password);
            if (string.IsNullOrEmpty(token))
            {
                result.Message = "Email or Password not matched.";
                result.Success = false;
                result.StatusCode = 404;
                return result;
            }
            if (token == "false")
            {
                result.Message = "Please Verify Email First";
                result.Success = false;
                result.StatusCode = 404;
                return result;
            }
            if (token == "No Access")
            {
                result.Message = "MESSAGE.APPLICATION.ACCESS_DENIED";
                result.Success = false;
                result.StatusCode = 401;
                return result;

            }
            result.Message = token;
            result.Success = true;
            return result;
        }

        [HttpPost]
        [Route("account/getSignedUserDetails")]
        [ModulePermission("Dashboard", "View")]
        public async Task<Result<LoginUserViewModel>> getUserByToken()
        {
            Result<LoginUserViewModel> result = new Result<LoginUserViewModel>();
            string userId = CurrentContext.UserId(_httpContextAccessor);
            string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
            LoginUserViewModel user = await _employeeService.GetSignedUserDetails(userId, roleId);
            if (user != null)
            {
                result.MethodResult = user;

                return result;
            }
            result.Success = true;
            return result;
        }

        [Route("CreateNewPassword")]
        [HttpPost]
        public async Task<Result> CreateNewPassword(CreateNewPasswordRequest model)
        {
            Result result = new Result();
            bool user = await _employeeService.CreateNewPassword(model.NewPassword, model.StatusNumber);
            if (!user)
            {
                result.Success = false;
                result.Message = "Password Not Created";
                return result;
            }
            result.Success = user;
            result.Message = "Password Created Successfully";
            return result;

        }
        //[Route("applicant/getOpenings")]
        //[HttpPost]
        //public async Task<Result<JobVacancyModel>> getOpenings(ApplyNowVacancyModel model)
        //{
        //    Result<JobVacancyModel> data = new Result<JobVacancyModel>();
        //    if (string.IsNullOrEmpty(model.companyId))
        //    {
        //        data.Success = false;
        //        data.StatusCode = StatusCodes.Status401Unauthorized;
        //        return data;
        //    }

        //    data = await _jobVacancyService.GetAllVacancy(model.companyId, 0, 0);

        //    return data;
        //}
        [HttpPost]
        [Route("applicant/applyJob")]
        [AllowAnonymous]
        public async Task<Result> RegisterApplicants([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {

            Result result = new Result();
            if (string.IsNullOrEmpty(applicantRegisterModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            Result ApplicantId = await _applicantsServices.GetApplicantsExistingId(applicantRegisterModel.Email);
            if (ApplicantId.Success)
            {
                applicantRegisterModel.ActivityType = string.IsNullOrEmpty(ApplicantId.Message)
                ? Utility.Enums.EnumsHelper.ActivityType.New :
                Utility.Enums.EnumsHelper.ActivityType.ReApply;
                result = await _applicantsServices.RegisterApplicants(applicantRegisterModel);
                result.Success = true;
                result.Message = "Your application has been submitted successfully";
            }
            else
            {
                result.Success = false;
                result.Message = "You have already applied. Please re-apply after the waiting period.";
            }
            return result;
        }

        [HttpGet]
        [Route("VerificationCaptch")]
        [AllowAnonymous]
        public async Task<bool> GetreCaptchaResponse(string userResponse)
        {
            string? reCaptchaSecretKey = ConfigManager.ReCaptcha.SecretKey;
            if (reCaptchaSecretKey != null && userResponse != null)
            {
                FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"secret", reCaptchaSecretKey },
                    {"response", userResponse }
                });
                HttpResponseMessage response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                if (response.IsSuccessStatusCode)
                {
                    reCaptchaResponse? result = await response.Content.ReadFromJsonAsync<reCaptchaResponse>();
                    return result.Success;
                }
            }
            return false;
        }
        [HttpGet]
        [AllowAnonymous]
        [Route("SendEmail")]
        public async Task<bool> SendEmail()
        {
            string a = AppModule.Applicants;
            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                _middlewareService.EmailSendAndSave(new Repository.Entities.EmpEmailLogs()
                {
                    UserTo = "",
                    Subject = "Test",
                    Body = "Test",
                    EmailLogType = Utility.Enums.EnumsHelper.MailType.ApplyNowMailToHR,
                    Email = "jay@codeji.in",
                    UserFrom = "    ",


                });
            }, priority: 1);


            return false;
        }
        public class reCaptchaResponse
        {
            public bool Success { get; set; }
            public string[] ErrorCodes { get; set; }
        }
    }
}


