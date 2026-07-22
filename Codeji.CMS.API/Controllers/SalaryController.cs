using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Services.Interfaces;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;


namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]

    public class SalaryController : ControllerBase
    {
        private readonly ISalaryService _salaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SalaryController(ISalaryService salaryService, IHttpContextAccessor httpContextAccessor)
        {
            _salaryService = salaryService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Route("UpdateSalary")]   
        [ModulePermission(AppModule.Employees, [Permission.Create, Permission.Edit])]

        public async Task<IActionResult> CreateSalary([FromBody] CreateSalaryDto dto)
        {
            var salary = await _salaryService.CreateSalaryAsync(CurrentContext.CompanyId(_httpContextAccessor), dto);
            return Ok(new { success = true, data = SalaryResponseDto.MapFromModel(salary) });
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveSalary(Guid userId)
        {
            var salary = await _salaryService.GetActiveSalaryAsync(CurrentContext.CompanyId(_httpContextAccessor), userId);
            if (salary == null) return NotFound(new { success = false, message = "No active salary found" });
            return Ok(new { success = true, data = salary });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetSalaryHistory(Guid userId)
        {
            var salaries = await _salaryService.GetSalaryHistoryAsync(CurrentContext.CompanyId(_httpContextAccessor), userId);
            return Ok(new { success = true, data = salaries });
        }

        [HttpPost]
        [Route("GetCompanySalaries")]
        [ModulePermission(AppModule.PayrollSettings, Permission.View)]
        public async Task<ActionResult<Result<CompanySalaryResponseDto>>> GetCompanySalaries([FromBody] CompanySalaryFilterDto filter)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var salaries = await _salaryService.GetCompanySalariesAsync(companyId, filter?.EmployeeName);
            var result = new Result<CompanySalaryResponseDto>
            {
                Success = true,
                MethodResults = salaries,
                TotalRecords = salaries.Count
            };
            return Ok(result);
        }
    }
}
