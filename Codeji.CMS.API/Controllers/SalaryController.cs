using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Services.Interfaces;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.API.App_Start;
using Microsoft.AspNetCore.Authorization;


namespace Codeji.CMS.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]

    public class SalaryController : ControllerBase
    {
        private readonly ISalaryService _salaryService;

        public SalaryController(ISalaryService salaryService)
        {
            _salaryService = salaryService;
        }

        [HttpPost]
        [Route("UpdateSalary")]   
        [ModulePermission(AppModule.Employees, [Permission.Create, Permission.Edit])]

        public async Task<IActionResult> CreateSalary([FromBody] CreateSalaryDto dto)
        {
            var salary = await _salaryService.CreateSalaryAsync(dto);
            return Ok(new { success = true, data = SalaryResponseDto.MapFromModel(salary) });
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveSalary(Guid userId)
        {
            var salary = await _salaryService.GetActiveSalaryAsync(userId);
            if (salary == null) return NotFound(new { success = false, message = "No active salary found" });
            return Ok(new { success = true, data = salary });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetSalaryHistory(Guid userId)
        {
            var salaries = await _salaryService.GetSalaryHistoryAsync(userId);
            return Ok(new { success = true, data = salaries });
        }
    }
}
