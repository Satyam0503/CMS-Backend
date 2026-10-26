using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Authorize]
[Route("api/EmployeeLifecycleCases")]
public sealed class EmployeeLifecycleCaseController(IEmployeeLifecycleCaseService service) : ControllerBase
{
    [HttpGet]
    [ModulePermission(AppModule.Offboarding, [Permission.View, Permission.CaseManage])]
    public async Task<IActionResult> Get([FromQuery] string? employeeUserId = null, [FromQuery] string? status = null, [FromQuery] bool overdueOnly = false) => Ok(await service.GetCasesAsync(employeeUserId, status, overdueOnly));

    [HttpGet("summary")]
    [ModulePermission(AppModule.Offboarding, [Permission.View, Permission.CaseManage])]
    public async Task<IActionResult> Summary() => Ok(await service.GetSummaryAsync());

    [HttpPost]
    [ModulePermission(AppModule.Offboarding, [Permission.Edit, Permission.CaseManage])]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeLifecycleCaseRequest request)
    {
        var result = await service.CreateFromApprovedLeaveAsync(request);
        return result.Success ? Ok(result.Case) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPatch]
    [ModulePermission(AppModule.Offboarding, [Permission.Edit, Permission.CaseManage])]
    public async Task<IActionResult> Update([FromBody] UpdateEmployeeLifecycleCaseRequest request)
    {
        var result = await service.UpdateAsync(request);
        return result.Success ? Ok(result.Case) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPost("{caseId}/evidence")]
    [ModulePermission(AppModule.Offboarding, [Permission.Edit, Permission.CaseManage, Permission.EvidenceManage])]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadEvidence(string caseId, [FromForm] IFormFile file)
    {
        var result = await service.UploadEvidenceAsync(caseId, file);
        return result.Success ? Ok(result.Case) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPatch("evidence/verification")]
    [ModulePermission(AppModule.Offboarding, [Permission.Edit, Permission.CaseManage, Permission.EvidenceManage])]
    public async Task<IActionResult> VerifyEvidence([FromBody] VerifyEmployeeLifecycleCaseEvidenceRequest request)
    {
        var result = await service.VerifyEvidenceAsync(request);
        return result.Success ? Ok(result.Case) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpGet("{caseId}/evidence/{evidenceId}")]
    [ModulePermission(AppModule.Offboarding, [Permission.View, Permission.CaseManage, Permission.Download])]
    public async Task<IActionResult> GetEvidence(string caseId, string evidenceId)
    {
        var result = await service.GetEvidenceAsync(caseId, evidenceId);
        return result.Success
            ? File(result.Content!, result.Evidence!.ContentType, result.Evidence.FileName)
            : NotFound(new { success = false, message = result.Error });
    }

    [HttpPost("{caseId}/reconcile-attendance")]
    [ModulePermission(AppModule.Offboarding, [Permission.Edit, Permission.CaseManage])]
    public async Task<IActionResult> ReconcileAttendance(string caseId)
    {
        var result = await service.ReconcileAttendanceAsync(caseId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
