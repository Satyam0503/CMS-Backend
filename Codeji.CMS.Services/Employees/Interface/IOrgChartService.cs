using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;

namespace Codeji.CMS.Services.Employees.Interface;

public interface IOrgChartService
{
    Task<Result<OrgChartResponseDto>> GetOrganizationChartAsync(string companyId, string? departmentId = null, string? search = null);
    Task<Result<List<OrgChartNodeDto>>> GetReportingChainAsync(string companyId, string userId);
}
