using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Repository.Entities.Employees;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Employees.Interface;

public interface IEmployeeLifecycleCaseService
{
    Task<IReadOnlyList<EmployeeLifecycleCase>> GetCasesAsync(string? employeeUserId = null, string? status = null, bool overdueOnly = false);
    Task<EmployeeLifecycleCaseSummary> GetSummaryAsync();
    Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CreateFromApprovedLeaveAsync(CreateEmployeeLifecycleCaseRequest request);
    Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CreateFromApprovedLeaveSystemAsync(CreateEmployeeLifecycleCaseRequest request, string actorUserId);
    Task<(bool Success, string? Error)> CancelFromLeaveRequestSystemAsync(string leaveRequestId, string actorUserId, string reason);
    Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> UpdateAsync(UpdateEmployeeLifecycleCaseRequest request);
    Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> UploadEvidenceAsync(string caseId, IFormFile file);
    Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> VerifyEvidenceAsync(VerifyEmployeeLifecycleCaseEvidenceRequest request);
    Task<(bool Success, string? Error, EmployeeLifecycleCaseEvidence? Evidence, byte[]? Content)> GetEvidenceAsync(string caseId, string evidenceId);
}
