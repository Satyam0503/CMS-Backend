using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Authorize, Route("api/payroll/configuration")]
public sealed class PayrollConfigurationController(
    IPayrollConfigurationService service,
    IPayrollTemplateService templates,
    IEmployeeSalaryStructureService salaries,
    IEmployeeSalaryImportService salaryImports,
    IHttpContextAccessor context) : ControllerBase
{
    [HttpGet("components"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<PayrollSalaryComponentDto>> GetComponents() =>
        templates.GetComponents(CurrentContext.CompanyId(context));

    [HttpPost("components"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SaveComponent(PayrollSalaryComponentDto dto) =>
        templates.SaveComponent(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpPost("components/seed-standard"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SeedStandardComponents() =>
        templates.SeedStandardComponents(CurrentContext.CompanyId(context), CurrentContext.UserId(context));

    [HttpGet("pay-groups"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<PayrollPayGroupDto>> GetPayGroups() =>
        service.GetPayGroups(CurrentContext.CompanyId(context));

    [HttpPost("pay-groups"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SavePayGroup(PayrollPayGroupDto dto) =>
        service.SavePayGroup(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpGet("assignments"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<EmployeePayrollAssignmentDto>> GetAssignments() =>
        service.GetAssignments(CurrentContext.CompanyId(context));

    [HttpPost("assignments"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SaveAssignment(EmployeePayrollAssignmentDto dto) =>
        service.SaveAssignment(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpPost("assignments/approve"), Authorize(Policy = "AdminOnly"), ModulePermission(AppModule.PayrollSettings, Permission.Approve)]
    public Task<Codeji.CMS.Domain.Models.Result> ApproveAssignment([FromQuery] string employeeId, [FromQuery] DateTime effectiveFrom) =>
        service.ApproveAssignment(CurrentContext.CompanyId(context), CurrentContext.UserId(context), employeeId, effectiveFrom);

    [HttpPost("assignments/reject"), Authorize(Policy = "AdminOnly"), ModulePermission(AppModule.PayrollSettings, Permission.Approve)]
    public Task<Codeji.CMS.Domain.Models.Result> RejectAssignment(PayrollAssignmentRejectionDto dto) =>
        service.RejectAssignment(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto.EmployeeId, dto.EffectiveFrom, dto.Reason);

    [HttpPost("assignments/cancel"), Authorize(Policy = "AdminOnly"), ModulePermission(AppModule.PayrollSettings, Permission.Approve)]
    public Task<Codeji.CMS.Domain.Models.Result> CancelAssignment([FromQuery] string employeeId, [FromQuery] DateTime effectiveFrom) =>
        service.CancelAssignment(CurrentContext.CompanyId(context), CurrentContext.UserId(context), employeeId, effectiveFrom);
    [HttpPost("assignments/bulk/preview"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result<PayrollBulkAssignmentPreviewRowDto>> PreviewBulkAssignments(PayrollBulkAssignmentRequestDto dto) =>
        service.PreviewBulkAssignments(CurrentContext.CompanyId(context), dto);

    [HttpPost("assignments/bulk"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result<PayrollBulkAssignmentResultDto>> SubmitBulkAssignments(PayrollBulkAssignmentRequestDto dto) =>
        service.SubmitBulkAssignments(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpGet("policies"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<PayrollPolicyProfileDto>> GetPolicies() =>
        templates.GetPolicies(CurrentContext.CompanyId(context));

    [HttpGet("salary-templates"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<SalaryTemplateDto>> GetTemplates() =>
        templates.GetTemplates(CurrentContext.CompanyId(context));

    [HttpPost("policies"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SavePolicy(PayrollPolicyProfileDto dto) =>
        templates.SavePolicy(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpDelete("policies/{policyId}"), ModulePermission(AppModule.PayrollSettings, Permission.Delete)]
    public Task<Codeji.CMS.Domain.Models.Result> DeletePolicyDraft(string policyId, [FromQuery] int version) =>
        templates.DeletePolicyDraft(CurrentContext.CompanyId(context), policyId, version);

    [HttpPost("policies/submit"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SubmitPolicy([FromQuery] string policyId, [FromQuery] int version) =>
        templates.SubmitPolicy(CurrentContext.CompanyId(context), CurrentContext.UserId(context), policyId, version);

    [HttpPost("policies/approve"), Authorize(Policy = "AdminOnly"), ModulePermission(AppModule.PayrollSettings, Permission.Approve)]
    public Task<Codeji.CMS.Domain.Models.Result> ApprovePolicy([FromQuery] string policyId, [FromQuery] int version) =>
        templates.ApprovePolicy(CurrentContext.CompanyId(context), CurrentContext.UserId(context), policyId, version, isCompanyAdministrator: true);

    [HttpPost("salary-templates"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SaveTemplate(SalaryTemplateDto dto) =>
        templates.SaveTemplate(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);
    [HttpPost("salary-templates/retire"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> RetireTemplate([FromQuery] string templateId, [FromQuery] int version) =>
        templates.RetireTemplate(CurrentContext.CompanyId(context), CurrentContext.UserId(context), templateId, version);
    [HttpPost("employee-salaries/import/pdf/preview"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public async Task<EmployeeSalaryPdfImportParseResult> PreviewEmployeeSalaryPdf([FromForm] IFormFile pdf, [FromForm] string? employeeId, [FromForm] DateTime? effectiveFrom)
    {
        if (pdf is null || pdf.Length == 0)
        {
            return new EmployeeSalaryPdfImportParseResult { Errors = new List<string> { "A PDF file is required." } };
        }

        if (pdf.Length > 10 * 1024 * 1024)
        {
            return new EmployeeSalaryPdfImportParseResult { Errors = new List<string> { "PDF files must be 10 MB or smaller." } };
        }

        if (!string.Equals(Path.GetExtension(pdf.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new EmployeeSalaryPdfImportParseResult { Errors = new List<string> { "Only .pdf files are supported." } };
        }

        await using var stream = pdf.OpenReadStream();
        return EmployeeSalaryPdfImportParser.Parse(stream, employeeIdFallback: employeeId, effectiveFromFallback: effectiveFrom);
    }

    [HttpPost("employee-salaries/import/preview"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<List<EmployeeSalaryImportPreviewRowDto>> PreviewEmployeeSalaryImport(List<EmployeeSalaryImportRowDto> rows) =>
        salaryImports.Preview(CurrentContext.CompanyId(context), rows);

    [HttpGet("employee-salaries/import/{batchId}"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<EmployeeSalaryImportBatchDto?> GetEmployeeSalaryImportBatch(string batchId) =>
        salaryImports.GetBatch(CurrentContext.CompanyId(context), batchId);

    [HttpPost("employee-salaries/import"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SubmitEmployeeSalaryImport(List<EmployeeSalaryImportRowDto> rows, [FromQuery] string? batchId = null) =>
        salaryImports.Submit(CurrentContext.CompanyId(context), CurrentContext.UserId(context), rows, batchId);

    [HttpGet("employee-salaries/{employeeId}"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<EmployeeSalaryStructureDto>> GetEmployeeSalaryHistory(string employeeId) =>
        salaries.GetHistory(CurrentContext.CompanyId(context), employeeId);

    [HttpGet("employee-salaries/pending"), ModulePermission(AppModule.PayrollSettings, Permission.View)]
    public Task<List<EmployeeSalaryStructureDto>> GetPendingEmployeeSalaries() =>
        salaries.GetPending(CurrentContext.CompanyId(context));

    [HttpPost("employee-salaries"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result<EmployeeSalaryStructureDto>> SaveEmployeeSalary(EmployeeSalaryStructureDto dto) =>
        salaries.SaveDraft(CurrentContext.CompanyId(context), CurrentContext.UserId(context), dto);

    [HttpPost("employee-salaries/submit"), ModulePermission(AppModule.PayrollSettings, Permission.Edit)]
    public Task<Codeji.CMS.Domain.Models.Result> SubmitEmployeeSalary([FromQuery] string id, [FromQuery] int version) =>
        salaries.Submit(CurrentContext.CompanyId(context), CurrentContext.UserId(context), id, version);

    [HttpPost("employee-salaries/approve"), Authorize(Policy = "AdminOnly"), ModulePermission(AppModule.PayrollSettings, Permission.Approve)]
    public Task<Codeji.CMS.Domain.Models.Result> ApproveEmployeeSalary([FromQuery] string id, [FromQuery] int version) =>
        salaries.Approve(CurrentContext.CompanyId(context), CurrentContext.UserId(context), id, version);
}

