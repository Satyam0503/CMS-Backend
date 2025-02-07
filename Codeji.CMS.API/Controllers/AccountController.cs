using System.Net;
using AutoMapper;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
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
        public AccountController(IEmployeeService userService,
            IAntiforgery antiforgery,
            IHttpContextAccessor httpContextAccessor,
            IApplicantsService applicantsServices,
            ICompanyService companyService,
            IMapper mapper)
        {
            _antiforgery = antiforgery;
            _httpContextAccessor = httpContextAccessor;
            _employeeService = userService;
            _applicantsServices = applicantsServices;
            _mapper = mapper;
            _companyService = companyService;
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
                Success = appVersion == ConfigManager.App_Version,
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
                return result;
            }
            result.Message = token;
            result.Success = true;
            return result;
        }

        [HttpPost]
        [Route("account/getSignedUserDetails")]
        [Authorize]
        public async Task<Result<LoginUserViewModel>> getUserByToken()
        {
            Result<LoginUserViewModel> result = new Result<LoginUserViewModel>();
            string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
            string roleId = CurrentContext.CurrentUserRoleId(_httpContextAccessor);
            LoginUserViewModel user = await _employeeService.GetSignedUserDetails(userId, roleId);
            if (user != null)
            {
                result.MethodResult = user;

                return result;
            }
            result.Success = true;
            return result;
        }

        [HttpPost]
        [Route("applicant/applyJob")]
        [AllowAnonymous]
        public async Task<Result> RegisterApplicants([FromBody] ApplicantAddEditModel applicantRegisterModel)
        {

            Result result = new Result();
            if (string.IsNullOrEmpty(applicantRegisterModel.Email))
                return new Result() { Success = false, StatusCode = StatusCodes.Status500InternalServerError };
            string? ApplicantId = await _applicantsServices.GetApplicantsExistingId(applicantRegisterModel.Email, applicantRegisterModel.CompanyId);
            if (string.IsNullOrEmpty(ApplicantId))
            {
                result = await _applicantsServices.RegisterApplicants(applicantRegisterModel, applicantRegisterModel.CompanyId);
                result.Success = true;
                result.Message = "Your application has been submitted successfully";
            }

            else
            {
                result.Success = false;
                result.Message = "You have already applied. Please reapply after the waiting period.";
            }
            return result;
        }
    }
}

