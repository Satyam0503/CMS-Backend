using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees;

public class OrgChartService : IOrgChartService
{
    private readonly IMongoDbRepository<EmpUser> _empUserRepository;
    private readonly IMongoDbRepository<Department> _departmentRepository;
    private readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;

    public OrgChartService(
        IMongoDbRepository<EmpUser> empUserRepository,
        IMongoDbRepository<Department> departmentRepository,
        IMongoDbRepository<JobTitles> jobTitlesRepository)
    {
        _empUserRepository = empUserRepository;
        _departmentRepository = departmentRepository;
        _jobTitlesRepository = jobTitlesRepository;
    }

    public async Task<Result<OrgChartResponseDto>> GetOrganizationChartAsync(
        string companyId,
        string? departmentId = null,
        string? search = null)
    {
        var result = new Result<OrgChartResponseDto>();

        if (string.IsNullOrWhiteSpace(companyId))
        {
            result.Success = false;
            result.Message = "Company ID is required.";
            return result;
        }

        var normalizedCompanyId = companyId.Trim();

        // 1. Fetch active company users
        var userEnumerable = await _empUserRepository.GetAll(u =>
            u.CompanyId == normalizedCompanyId &&
            u.Status &&
            !u.IsDeleted);
        var users = userEnumerable?.ToList() ?? new List<EmpUser>();

        if (users.Count == 0)
        {
            result.Success = true;
            result.Data = new OrgChartResponseDto
            {
                Roots = new List<OrgChartNodeDto>(),
                TotalEmployeesCount = 0,
                TotalDepartmentsCount = 0,
                Departments = new List<OrgChartDepartmentSummaryDto>()
            };
            result.MethodResult = result.Data;
            return result;
        }

        // 2. Fetch lookup dictionaries for Departments and Job Titles
        var deptEnumerable = await _departmentRepository.GetAll(d =>
            d.CompanyId == normalizedCompanyId &&
            !d.IsDeleted);
        var departments = deptEnumerable?.ToList() ?? new List<Department>();
        var deptDict = departments
            .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentId))
            .ToDictionary(
                d => d.DepartmentId,
                d => ResolveTitle(d.Titles, d.DepartmentId),
                StringComparer.OrdinalIgnoreCase);

        var jobTitleEnumerable = await _jobTitlesRepository.GetAll(j =>
            j.CompanyId == normalizedCompanyId &&
            !j.IsDeleted);
        var jobTitles = jobTitleEnumerable?.ToList() ?? new List<JobTitles>();
        var jobTitleDict = jobTitles
            .Where(j => !string.IsNullOrWhiteSpace(j.JobTitleId))
            .ToDictionary(
                j => j.JobTitleId,
                j => ResolveTitle(j.Titles, "Team Member"),
                StringComparer.OrdinalIgnoreCase);

        // 3. Create user DTO map
        var nodeMap = new Dictionary<string, OrgChartNodeDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users)
        {
            var firstName = u.FirstName?.Trim() ?? string.Empty;
            var lastName = u.LastName?.Trim();
            var fullName = string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}".Trim();

            var deptName = !string.IsNullOrWhiteSpace(u.Department) && deptDict.TryGetValue(u.Department, out var dName)
                ? dName
                : (string.IsNullOrWhiteSpace(u.Department) ? "Unassigned" : u.Department);

            var titleName = !string.IsNullOrWhiteSpace(u.JobRole) && jobTitleDict.TryGetValue(u.JobRole, out var jTitle)
                ? jTitle
                : (string.IsNullOrWhiteSpace(u.JobRole) ? "Team Member" : u.JobRole);

            string? resolvedProfileUrl = null;
            if (!string.IsNullOrWhiteSpace(u.ProfileUrl))
            {
                resolvedProfileUrl = u.ProfileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? u.ProfileUrl
                    : Common.GetEmployeeImageUrl(u.ProfileUrl);
            }

            var node = new OrgChartNodeDto
            {
                UserId = u.UserId,
                EmployeeId = u.EmployeeId ?? string.Empty,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName,
                Email = u.Email ?? string.Empty,
                ProfileUrl = resolvedProfileUrl,
                JobRoleId = u.JobRole ?? string.Empty,
                JobRoleTitle = titleName,
                DepartmentId = u.Department ?? string.Empty,
                DepartmentName = deptName,
                ReportingManagerUserId = !string.IsNullOrWhiteSpace(u.ReportingManager) ? u.ReportingManager.Trim() : null,
                DirectReports = new List<OrgChartNodeDto>()
            };

            nodeMap[u.UserId] = node;
        }

        // Resolve manager names & sanitize self-reporting
        foreach (var node in nodeMap.Values)
        {
            if (string.Equals(node.ReportingManagerUserId, node.UserId, StringComparison.OrdinalIgnoreCase))
            {
                // Self-reporting is invalid in HRMS - treat as root
                node.ReportingManagerUserId = null;
                node.ReportingManagerName = null;
            }
            else if (!string.IsNullOrWhiteSpace(node.ReportingManagerUserId) &&
                nodeMap.TryGetValue(node.ReportingManagerUserId, out var manager))
            {
                node.ReportingManagerName = manager.FullName;
            }
            else
            {
                // Manager not found or outside company
                node.ReportingManagerUserId = null;
                node.ReportingManagerName = null;
            }
        }

        // 4. Build Parent-Child Hierarchy with Cycle Protection
        var roots = new List<OrgChartNodeDto>();
        var visitedInHierarchy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodeMap.Values)
        {
            if (string.IsNullOrWhiteSpace(node.ReportingManagerUserId) ||
                !nodeMap.ContainsKey(node.ReportingManagerUserId))
            {
                // Top-level root (No manager)
                roots.Add(node);
            }
            else
            {
                var parent = nodeMap[node.ReportingManagerUserId];
                // Prevent duplicate addition and cycles
                if (!parent.DirectReports.Any(c => string.Equals(c.UserId, node.UserId, StringComparison.OrdinalIgnoreCase)))
                {
                    parent.DirectReports.Add(node);
                }
            }
        }

        // Cycle breaker: If circular references caused roots to be empty despite users existing, pick first user as root
        if (roots.Count == 0 && nodeMap.Count > 0)
        {
            var fallback = nodeMap.Values.First();
            fallback.ReportingManagerUserId = null;
            fallback.ReportingManagerName = null;
            roots.Add(fallback);
        }

        // 5. Calculate DirectReportsCount & TotalTeamCount recursively
        foreach (var root in roots)
        {
            CalculateTeamMetrics(root, visitedInHierarchy);
        }

        // 6. Compute Department Summaries
        var deptSummaries = users
            .GroupBy(u => string.IsNullOrWhiteSpace(u.Department) ? "NO_DEPARTMENT" : u.Department)
            .Select(g =>
            {
                var dId = g.Key;
                var isNoDept = string.Equals(dId, "NO_DEPARTMENT", StringComparison.OrdinalIgnoreCase);
                var dName = isNoDept ? "No Department" : (deptDict.TryGetValue(dId, out var dn) ? dn : dId);
                return new OrgChartDepartmentSummaryDto
                {
                    DepartmentId = dId,
                    DepartmentName = dName,
                    EmployeeCount = g.Count()
                };
            })
            .OrderBy(d => string.Equals(d.DepartmentId, "NO_DEPARTMENT", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenByDescending(d => d.EmployeeCount)
            .ToList();

        // 7. Apply optional Department filter or Search filter if requested
        var filteredRoots = roots;
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            filteredRoots = FilterTreeByDepartment(roots, departmentId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredRoots = FilterTreeBySearch(filteredRoots, search.Trim());
        }

        result.Success = true;
        result.Data = new OrgChartResponseDto
        {
            Roots = filteredRoots,
            TotalEmployeesCount = users.Count,
            TotalDepartmentsCount = departments.Count,
            Departments = deptSummaries
        };
        result.MethodResult = result.Data;

        return result;
    }

    private static int CalculateTeamMetrics(OrgChartNodeDto node, HashSet<string> visited)
    {
        if (visited.Contains(node.UserId))
        {
            node.DirectReportsCount = 0;
            node.TotalTeamCount = 0;
            return 0;
        }

        visited.Add(node.UserId);

        // Remove any cyclic children before iteration
        node.DirectReports = node.DirectReports
            .Where(c => !visited.Contains(c.UserId))
            .ToList();

        node.DirectReportsCount = node.DirectReports.Count;

        int teamCount = 0;
        foreach (var child in node.DirectReports)
        {
            teamCount += 1 + CalculateTeamMetrics(child, visited);
        }

        node.TotalTeamCount = teamCount;
        return teamCount;
    }

    private static List<OrgChartNodeDto> FilterTreeByDepartment(List<OrgChartNodeDto> nodes, string departmentId)
    {
        var result = new List<OrgChartNodeDto>();
        foreach (var node in nodes)
        {
            var filteredChildren = FilterTreeByDepartment(node.DirectReports, departmentId);
            bool isMatch = string.Equals(node.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(node.DepartmentName, departmentId, StringComparison.OrdinalIgnoreCase);

            if (isMatch || filteredChildren.Count > 0)
            {
                var clone = CloneNode(node);
                clone.DirectReports = filteredChildren;
                clone.DirectReportsCount = filteredChildren.Count;
                result.Add(clone);
            }
        }
        return result;
    }

    private static List<OrgChartNodeDto> FilterTreeBySearch(List<OrgChartNodeDto> nodes, string query)
    {
        var result = new List<OrgChartNodeDto>();
        foreach (var node in nodes)
        {
            var filteredChildren = FilterTreeBySearch(node.DirectReports, query);
            bool isMatch = node.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           node.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           node.JobRoleTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           node.EmployeeId.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (isMatch || filteredChildren.Count > 0)
            {
                var clone = CloneNode(node);
                clone.DirectReports = filteredChildren;
                clone.DirectReportsCount = filteredChildren.Count;
                result.Add(clone);
            }
        }
        return result;
    }

    private static OrgChartNodeDto CloneNode(OrgChartNodeDto source)
    {
        return new OrgChartNodeDto
        {
            UserId = source.UserId,
            EmployeeId = source.EmployeeId,
            FirstName = source.FirstName,
            LastName = source.LastName,
            FullName = source.FullName,
            Email = source.Email,
            ProfileUrl = source.ProfileUrl,
            JobRoleId = source.JobRoleId,
            JobRoleTitle = source.JobRoleTitle,
            DepartmentId = source.DepartmentId,
            DepartmentName = source.DepartmentName,
            ReportingManagerUserId = source.ReportingManagerUserId,
            ReportingManagerName = source.ReportingManagerName,
            DirectReportsCount = source.DirectReportsCount,
            TotalTeamCount = source.TotalTeamCount,
            DirectReports = new List<OrgChartNodeDto>()
        };
    }

    private static string ResolveTitle(List<MultilingualModel>? titles, string fallback)
    {
        if (titles == null || titles.Count == 0) return fallback;
        var en = titles.FirstOrDefault(t => string.Equals(t.Language, "en", StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrWhiteSpace(en?.Label) ? en.Label : (titles[0].Label ?? fallback);
    }

    public async Task<Result<List<OrgChartNodeDto>>> GetReportingChainAsync(string companyId, string userId)
    {
        var result = new Result<List<OrgChartNodeDto>> { Success = true, MethodResult = new List<OrgChartNodeDto>() };

        if (string.IsNullOrWhiteSpace(companyId))
        {
            result.Success = false;
            result.Message = "Company ID is required.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            result.Success = false;
            result.Message = "User ID is required.";
            return result;
        }

        var normalizedCompanyId = companyId.Trim();
        var normalizedUserId = userId.Trim();

        var usersEnumerable = await _empUserRepository.GetAll(u =>
            u.CompanyId == normalizedCompanyId &&
            u.Status &&
            !u.IsDeleted);
        var users = usersEnumerable?.ToList() ?? new List<EmpUser>();

        var userMap = users.ToDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);

        if (!userMap.TryGetValue(normalizedUserId, out var currentUser))
        {
            result.Success = false;
            result.Message = "Target employee not found in current company.";
            return result;
        }

        var deptEnumerable = await _departmentRepository.GetAll(d =>
            d.CompanyId == normalizedCompanyId &&
            !d.IsDeleted);
        var departments = deptEnumerable?.ToList() ?? new List<Department>();
        var deptDict = departments
            .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentId))
            .ToDictionary(
                d => d.DepartmentId,
                d => ResolveTitle(d.Titles, d.DepartmentId),
                StringComparer.OrdinalIgnoreCase);

        var jobTitleEnumerable = await _jobTitlesRepository.GetAll(j =>
            j.CompanyId == normalizedCompanyId &&
            !j.IsDeleted);
        var jobTitles = jobTitleEnumerable?.ToList() ?? new List<JobTitles>();
        var jobTitleDict = jobTitles
            .Where(j => !string.IsNullOrWhiteSpace(j.JobTitleId))
            .ToDictionary(
                j => j.JobTitleId,
                j => ResolveTitle(j.Titles, "Team Member"),
                StringComparer.OrdinalIgnoreCase);

        var chain = new List<OrgChartNodeDto>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalizedUserId };

        var currentManagerId = currentUser.ReportingManager?.Trim();
        while (!string.IsNullOrWhiteSpace(currentManagerId) && userMap.TryGetValue(currentManagerId, out var managerUser))
        {
            if (visited.Contains(managerUser.UserId))
            {
                break;
            }

            visited.Add(managerUser.UserId);

            var managerNode = new OrgChartNodeDto
            {
                UserId = managerUser.UserId,
                EmployeeId = managerUser.EmployeeId ?? string.Empty,
                FirstName = managerUser.FirstName ?? string.Empty,
                LastName = managerUser.LastName,
                FullName = $"{managerUser.FirstName} {managerUser.LastName}".Trim(),
                Email = managerUser.Email ?? string.Empty,
                ProfileUrl = managerUser.ProfileUrl,
                JobRoleId = managerUser.JobRole ?? string.Empty,
                JobRoleTitle = !string.IsNullOrWhiteSpace(managerUser.JobRole) && jobTitleDict.TryGetValue(managerUser.JobRole, out var jTitle) ? jTitle : "Team Member",
                DepartmentId = managerUser.Department ?? string.Empty,
                DepartmentName = !string.IsNullOrWhiteSpace(managerUser.Department) && deptDict.TryGetValue(managerUser.Department, out var dName) ? dName : "Unassigned",
                ReportingManagerUserId = !string.IsNullOrWhiteSpace(managerUser.ReportingManager) ? managerUser.ReportingManager.Trim() : null,
                DirectReports = new List<OrgChartNodeDto>()
            };

            chain.Add(managerNode);
            currentManagerId = managerUser.ReportingManager?.Trim();
        }

        result.Data = chain;
        result.MethodResult = chain;
        return result;
    }
}
