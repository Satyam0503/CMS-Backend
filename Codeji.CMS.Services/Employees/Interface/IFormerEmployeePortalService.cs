using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Domain.Models;

namespace Codeji.CMS.Services.Employees.Interface;

public interface IFormerEmployeePortalService
{
    Task<Result<FormerEmployeePortalInviteDto>> CreateInvite(CreateFormerEmployeePortalInviteRequest request);
    Task<Result> RevokeAccess(RevokeFormerEmployeePortalAccessRequest request);
    Task<Result<FormerEmployeePortalSessionDto>> RedeemInvite(FormerEmployeePortalTokenRequest request);
    Task<Result<FormerEmployeePortalCaseDto>> GetCase(string sessionToken);
    Task<Result> CreateRequest(string sessionToken, FormerEmployeePortalRequest request);
    Task<Result<EmployeeExitEvidenceDto>> GetDocument(string sessionToken, string evidenceId);
}
