using System.Linq.Expressions;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class OrgChartServiceTests
{
    private readonly Mock<IMongoDbRepository<EmpUser>> _userRepoMock = new();
    private readonly Mock<IMongoDbRepository<Department>> _deptRepoMock = new();
    private readonly Mock<IMongoDbRepository<JobTitles>> _jobTitleRepoMock = new();

    private OrgChartService CreateService()
    {
        return new OrgChartService(_userRepoMock.Object, _deptRepoMock.Object, _jobTitleRepoMock.Object);
    }

    [Fact]
    public async Task GetOrganizationChartAsync_WhenCompanyIdEmpty_ReturnsFailure()
    {
        var service = CreateService();
        var result = await service.GetOrganizationChartAsync("");

        Assert.False(result.Success);
        Assert.Equal("Company ID is required.", result.Message);
    }

    [Fact]
    public async Task GetOrganizationChartAsync_WhenNoEmployees_ReturnsEmptyRoots()
    {
        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>());

        var service = CreateService();
        var result = await service.GetOrganizationChartAsync("company-123");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Roots);
        Assert.Equal(0, result.Data.TotalEmployeesCount);
    }

    [Fact]
    public async Task GetOrganizationChartAsync_ConstructsMultiLevelHierarchyCorrectly()
    {
        var companyId = "c1";

        var ceo = new EmpUser
        {
            UserId = "u-ceo",
            CompanyId = companyId,
            FirstName = "Alice",
            LastName = "CEO",
            Email = "alice@company.com",
            Department = "d-exec",
            JobRole = "j-ceo",
            ReportingManager = null,
            Status = true,
            IsDeleted = false
        };

        var vp = new EmpUser
        {
            UserId = "u-vp",
            CompanyId = companyId,
            FirstName = "Bob",
            LastName = "VP",
            Email = "bob@company.com",
            Department = "d-eng",
            JobRole = "j-vp",
            ReportingManager = "u-ceo",
            Status = true,
            IsDeleted = false
        };

        var dev = new EmpUser
        {
            UserId = "u-dev",
            CompanyId = companyId,
            FirstName = "Charlie",
            LastName = "Dev",
            Email = "charlie@company.com",
            Department = "d-eng",
            JobRole = "j-dev",
            ReportingManager = "u-vp",
            Status = true,
            IsDeleted = false
        };

        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { ceo, vp, dev });

        var depts = new List<Department>
        {
            new Department { DepartmentId = "d-exec", Titles = new List<MultilingualModel> { new() { Language = "en", Label = "Executive" } } },
            new Department { DepartmentId = "d-eng", Titles = new List<MultilingualModel> { new() { Language = "en", Label = "Engineering" } } }
        };
        _deptRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(depts);

        var titles = new List<JobTitles>
        {
            new JobTitles { JobTitleId = "j-ceo", Titles = new List<MultilingualModel> { new() { Language = "en", Label = "Chief Executive Officer" } } },
            new JobTitles { JobTitleId = "j-vp", Titles = new List<MultilingualModel> { new() { Language = "en", Label = "VP Engineering" } } },
            new JobTitles { JobTitleId = "j-dev", Titles = new List<MultilingualModel> { new() { Language = "en", Label = "Software Engineer" } } }
        };
        _jobTitleRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(titles);

        var service = CreateService();
        var result = await service.GetOrganizationChartAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Roots);

        var rootNode = result.Data.Roots[0];
        Assert.Equal("u-ceo", rootNode.UserId);
        Assert.Equal("Alice CEO", rootNode.FullName);
        Assert.Equal("Executive", rootNode.DepartmentName);
        Assert.Equal("Chief Executive Officer", rootNode.JobRoleTitle);
        Assert.Equal(1, rootNode.DirectReportsCount);
        Assert.Equal(2, rootNode.TotalTeamCount); // vp + dev

        var vpNode = rootNode.DirectReports[0];
        Assert.Equal("u-vp", vpNode.UserId);
        Assert.Equal("Alice CEO", vpNode.ReportingManagerName);
        Assert.Equal(1, vpNode.DirectReportsCount);
        Assert.Equal(1, vpNode.TotalTeamCount); // dev

        var devNode = vpNode.DirectReports[0];
        Assert.Equal("u-dev", devNode.UserId);
        Assert.Equal("Bob VP", devNode.ReportingManagerName);
        Assert.Equal(0, devNode.DirectReportsCount);
        Assert.Equal(0, devNode.TotalTeamCount);
    }

    [Fact]
    public async Task GetOrganizationChartAsync_WhenCycleExists_BreaksCycleSafely()
    {
        var companyId = "c1";

        // Cycle: UserA reports to UserB, UserB reports to UserA
        var userA = new EmpUser
        {
            UserId = "u-a",
            CompanyId = companyId,
            FirstName = "User",
            LastName = "A",
            Email = "a@test.com",
            ReportingManager = "u-b",
            Status = true,
            IsDeleted = false
        };

        var userB = new EmpUser
        {
            UserId = "u-b",
            CompanyId = companyId,
            FirstName = "User",
            LastName = "B",
            Email = "b@test.com",
            ReportingManager = "u-a",
            Status = true,
            IsDeleted = false
        };

        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { userA, userB });

        _deptRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Department>());

        _jobTitleRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<JobTitles>());

        var service = CreateService();
        var result = await service.GetOrganizationChartAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        // Fallback kicks in, preventing empty tree or infinite recursion
        Assert.NotEmpty(result.Data.Roots);
    }

    [Fact]
    public async Task GetOrganizationChartAsync_FiltersBySearchCorrectly()
    {
        var companyId = "c1";

        var ceo = new EmpUser
        {
            UserId = "u-ceo",
            CompanyId = companyId,
            FirstName = "Alice",
            LastName = "CEO",
            Email = "alice@company.com",
            Status = true,
            IsDeleted = false
        };

        var dev = new EmpUser
        {
            UserId = "u-dev",
            CompanyId = companyId,
            FirstName = "Charlie",
            LastName = "Specialist",
            Email = "charlie@company.com",
            ReportingManager = "u-ceo",
            Status = true,
            IsDeleted = false
        };

        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { ceo, dev });

        _deptRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Department>());

        _jobTitleRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<JobTitles>());

        var service = CreateService();
        var result = await service.GetOrganizationChartAsync(companyId, search: "Specialist");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Roots);
        Assert.Equal("u-ceo", result.Data.Roots[0].UserId);
        Assert.Single(result.Data.Roots[0].DirectReports);
        Assert.Equal("u-dev", result.Data.Roots[0].DirectReports[0].UserId);
    }

    [Fact]
    public async Task GetReportingChainAsync_WhenCompanyOrUserIdEmpty_ReturnsFailure()
    {
        var service = CreateService();
        var emptyCompany = await service.GetReportingChainAsync("", "u-1");
        var emptyUser = await service.GetReportingChainAsync("c-1", "");

        Assert.False(emptyCompany.Success);
        Assert.False(emptyUser.Success);
    }

    [Fact]
    public async Task GetReportingChainAsync_WhenTargetUserNotFound_ReturnsFailure()
    {
        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>());

        var service = CreateService();
        var result = await service.GetReportingChainAsync("c-1", "u-nonexistent");

        Assert.False(result.Success);
        Assert.Equal("Target employee not found in current company.", result.Message);
    }

    [Fact]
    public async Task GetReportingChainAsync_TraversesUpwardHierarchyCorrectly()
    {
        var companyId = "c-100";
        var ceo = new EmpUser { UserId = "u-ceo", CompanyId = companyId, FirstName = "Alice", LastName = "Boss", Email = "alice@company.com", ReportingManager = "", Status = true, IsDeleted = false };
        var vp = new EmpUser { UserId = "u-vp", CompanyId = companyId, FirstName = "Bob", LastName = "VP", Email = "bob@company.com", ReportingManager = "u-ceo", Status = true, IsDeleted = false };
        var mgr = new EmpUser { UserId = "u-mgr", CompanyId = companyId, FirstName = "Charlie", LastName = "Manager", Email = "charlie@company.com", ReportingManager = "u-vp", Status = true, IsDeleted = false };
        var dev = new EmpUser { UserId = "u-dev", CompanyId = companyId, FirstName = "David", LastName = "Dev", Email = "david@company.com", ReportingManager = "u-mgr", Status = true, IsDeleted = false };

        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { ceo, vp, mgr, dev });

        _deptRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Department>());

        _jobTitleRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<JobTitles>());

        var service = CreateService();
        var result = await service.GetReportingChainAsync(companyId, "u-dev");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(3, result.MethodResult.Count);
        Assert.Equal("u-mgr", result.MethodResult[0].UserId);
        Assert.Equal("u-vp", result.MethodResult[1].UserId);
        Assert.Equal("u-ceo", result.MethodResult[2].UserId);
    }

    [Fact]
    public async Task GetReportingChainAsync_ProtectsAgainstReportingCycles()
    {
        var companyId = "c-100";
        var userA = new EmpUser { UserId = "u-a", CompanyId = companyId, FirstName = "User", LastName = "A", Email = "a@company.com", ReportingManager = "u-b", Status = true, IsDeleted = false };
        var userB = new EmpUser { UserId = "u-b", CompanyId = companyId, FirstName = "User", LastName = "B", Email = "b@company.com", ReportingManager = "u-a", Status = true, IsDeleted = false };

        _userRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { userA, userB });

        _deptRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Department>());

        _jobTitleRepoMock
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<JobTitles>());

        var service = CreateService();
        var result = await service.GetReportingChainAsync(companyId, "u-a");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Single(result.MethodResult);
        Assert.Equal("u-b", result.MethodResult[0].UserId);
    }
}
