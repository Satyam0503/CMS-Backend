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
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
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
        private readonly IAccountServices _accountService;
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
            IMiddlewareService middlewareService,
            IAccountServices accountServices
            )
        {
            _antiforgery = antiforgery;
            _httpContextAccessor = httpContextAccessor;
            _employeeService = userService;
            _applicantsServices = applicantsServices;
            _mapper = mapper;
            _companyService = companyService;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
            _accountService = accountServices;
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
        public async Task<Result> Register([FromBody] CompanyRequestModel companyModel)
        {
            //Validating Company Registration
            bool isEmailExist = await _employeeService.IsEmailExist(companyModel.Email);
            if (isEmailExist)
            {
                return new Result()
                {
                    Success = false,
                    StatusCode = CustomStatusCode.CompanyAlreadyExist,
                };
            }
            return await _companyService.Register(companyModel);
        }


        [HttpPost]
        [Route("account/login")]
        [AllowAnonymous]
        public async Task<Result<TokenResponseDto>> Login([FromBody] LoginModel model)
        {
            Result<TokenResponseDto> result = new();
            if (!ModelState.IsValid)
            {
                result.Success = false;
                return result;
            }
            result = await _accountService.VerifyAndGenerateToken(model);
            return result;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("account/refresh-token")]
        public async Task<Result<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            Result<TokenResponseDto> result = new();
            if (!ModelState.IsValid)
            {
                result.Success = false;
                return result;
            }
            return await _accountService.RefreshToken(model);
        }

        [HttpPost]
        [Route("account/logout")]
        [Authorize]
        public async Task<Result> LogOut([FromBody] RefreshTokenRequestDto model)
        {
            if (string.IsNullOrEmpty(model.RefreshToken))
            {
                return new Result();
            }
            string currentUser = CurrentContext.UserId(_httpContextAccessor);
            var result = await _accountService.LogOut(model.RefreshToken, currentUser);
            return result;
        }

        [HttpPost]
        [Route("account/getSignedUserDetails")]
        public async Task<Result<LoginUserViewModel>> GetUserByToken()
        {
            Result<LoginUserViewModel> result = new();
            string userId = CurrentContext.UserId(_httpContextAccessor);
            string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            LoginUserViewModel user = await _employeeService.GetSignedUserDetails(userId, roleId, companyId);
            if (user != null)
            {
                result.MethodResult = user;

                return result;
            }
            result.Success = true;
            return result;
        }

        [AllowAnonymous]
        [Route("CreateNewPassword")]
        [HttpPost]
        public async Task<Result> CreateNewPassword(CreateNewPasswordRequest model)
        {
            Result result = await _accountService.CreateNewPassword(model);
            return result;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("account/ResetPassword")]
        public async Task<Result> ResetPassword([FromBody] string email)
        {
            Result result = new();
            bool exist = await _employeeService.IsEmpExistAndActive(email);
            if (!exist)
            {
                result.StatusCode = CustomStatusCode.InvalidCredential;
                return result;
            }
            return await _accountService.GenerateTokenAndSendEmail(email);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("applicant/applyJob")]
        public async Task<Result> RegisterApplicants([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {
            Result result = new Result();
            if (!ModelState.IsValid)
            {
                return result;
            }
            Result ApplicantId = await _applicantsServices.GetApplicantsExistingId(applicantRegisterModel.Email);
            if (ApplicantId.Success)
            {
                applicantRegisterModel.ActivityType = string.IsNullOrEmpty(ApplicantId.Message)
                ? EnumsHelper.ActivityType.New :
                EnumsHelper.ActivityType.ReApply;
                applicantRegisterModel.Status = EnumsHelper.ActivityStatus.Active;
                result = await _applicantsServices.RegisterApplicants(applicantRegisterModel);
                if (result.Success)
                {
                    result.Message = "Your application has been submitted successfully";
                }
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
        public class reCaptchaResponse
        {
            public bool Success { get; set; }
            public string[] ErrorCodes { get; set; }
        }
    }
}


