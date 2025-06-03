using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : BaseApiController
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly IEmployeeService _employeeService;
        private readonly IDashboardService _dashboardService;

        public DashboardController(IEmployeeService userService,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IDashboardService dashboardService
            )
        {
            _httpContextAccessor = httpContextAccessor;
            _employeeService = userService;
            _mapper = mapper;
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [Route("GetAllGenderDetails")]
        public async Task<Result<GenderDetailsResponseModel>> GetAllGenderDetails()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);

            var data = await _dashboardService.GetAllGenderDetails(companyId);

            return new Result<GenderDetailsResponseModel>()
            {
                MethodResults = data
            };
        }

        [HttpGet]
        [Route("GetAllDepartmentDetails")]
        public async Task<Result<AllDepartmentDetailsResponseModel>> GetAllDepartmentDetails()
        {

            string companyId = CurrentContext.CompanyId(_httpContextAccessor);

            var data = await _dashboardService.GetAllDepartmentsDetails(companyId);

            return new Result<AllDepartmentDetailsResponseModel>()
            {
                Message = "Department details fetched successfully",
                MethodResults = data
            };

        }
    }
}
