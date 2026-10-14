using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Security.Claims;

namespace Codeji.CMS.Services.Tests;

public class EmployeeServiceBulkImportTests
{
    [Fact]
    public async Task BulkImportEmployees_ValidBatch_CreatesAllRows()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP001", FirstName = " John ", LastName = " Doe ", Email = "john.doe@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP002", FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(2, result.MethodResult.Total);
        Assert.Equal(2, result.MethodResult.CreatedCount);
        Assert.Equal(0, result.MethodResult.FailedCount);
        Assert.All(result.MethodResult.Results, r => Assert.Equal("Created", r.Status));
    }

    [Fact]
    public async Task BulkImportEmployees_MixedValidAndInvalidRows_ReturnsPartialSuccess()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP010", FirstName = "Alice", LastName = "Jones", Email = "alice.jones@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP011", FirstName = "Bob", LastName = "Brown", Email = "invalid-email", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP012", FirstName = " ", LastName = "Taylor", Email = "taylor@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(3, result.MethodResult.Total);
        Assert.Equal(1, result.MethodResult.CreatedCount);
        Assert.Equal(2, result.MethodResult.FailedCount);
        Assert.Contains(result.MethodResult.Results, r => r.RowNumber == 2 && r.Status == "Failed" && r.Message.Contains("Invalid email"));
        Assert.Contains(result.MethodResult.Results, r => r.RowNumber == 3 && r.Status == "Failed" && r.Message.Contains("Required fields"));
    }

    [Fact]
    public async Task BulkImportEmployees_DuplicateEmpIdOrEmailInRequest_FailsDuplicateRows()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP020", FirstName = "A", LastName = "One", Email = "a.one@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP020", FirstName = "B", LastName = "Two", Email = "b.two@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP021", FirstName = "C", LastName = "Three", Email = "a.one@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(1, result.MethodResult.CreatedCount);
        Assert.Equal(2, result.MethodResult.FailedCount);
        Assert.Contains(result.MethodResult.Results, r => r.RowNumber == 2 && r.Message.Contains("Duplicate employee ID in request"));
        Assert.Contains(result.MethodResult.Results, r => r.RowNumber == 3 && r.Message.Contains("Duplicate email in request"));
    }

    [Fact]
    public async Task SubmitOwnResignation_AnotherEmployeesIdentity_IsRejectedBeforeAnyWrite()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "employee-1");

        var result = await fixture.Service.SubmitOwnResignation(new InitiateEmployeeExitRequest
        {
            EmployeeUserId = "another-employee",
            ExitType = "Resigned",
            LastWorkingDate = DateTime.UtcNow.Date.AddDays(30)
        });

        Assert.False(result.Success);
        Assert.Equal("SELF_EXIT_ONLY", result.Message);
        fixture.EmployeeRepository.Verify(x => x.AddOne(It.IsAny<EmpUser>()), Times.Never);
    }

    [Fact]
    public async Task InitiateEmployeeExit_SelfTarget_IsRejectedBeforeAnyWrite()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "employee-1");

        var result = await fixture.Service.InitiateEmployeeExit(new InitiateEmployeeExitRequest
        {
            EmployeeUserId = "employee-1",
            ExitType = "Terminated",
            LastWorkingDate = DateTime.UtcNow.Date.AddDays(1)
        });

        Assert.False(result.Success);
        Assert.Equal("SELF_EXIT_NOT_ALLOWED", result.Message);
        fixture.EmployeeRepository.Verify(x => x.AddOne(It.IsAny<EmpUser>()), Times.Never);
    }

    [Fact]
    public async Task SubmitOwnResignation_TerminationType_IsRejectedBeforeAnyWrite()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "employee-1");

        var result = await fixture.Service.SubmitOwnResignation(new InitiateEmployeeExitRequest
        {
            EmployeeUserId = "employee-1",
            ExitType = "Terminated",
            LastWorkingDate = DateTime.UtcNow.Date.AddDays(30)
        });

        Assert.False(result.Success);
        Assert.Equal("SELF_SUBMISSION_MUST_BE_RESIGNATION", result.Message);
        fixture.EmployeeRepository.Verify(x => x.AddOne(It.IsAny<EmpUser>()), Times.Never);
    }

    [Fact]
    public async Task SubmitOwnResignation_WhenPolicyDisablesSelfResignation_IsRejectedBeforeAnyWrite()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "employee-1");
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = "company-1", UserId = "employee-1", FirstName = "Test", LastName = "Employee", Email = "test@example.com", Status = true, EmploymentStatus = "Active",
                DateOfJoining = DateTime.UtcNow.Date.AddYears(-1).ToString("yyyy-MM-dd"), Lifecycle = new EmployeeLifecycleState()
            });
        fixture.EmployeeExitRepository.Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        fixture.EmployeeExitPolicyRepository.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExitPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([new EmployeeExitPolicy { CompanyId = "company-1", IsActive = true, AllowEmployeeSelfResignation = false, EnableOffboarding = true, Version = 1 }]);

        var result = await fixture.Service.SubmitOwnResignation(new InitiateEmployeeExitRequest
        {
            EmployeeUserId = "employee-1", ExitType = "Resigned", LastWorkingDate = DateTime.UtcNow.Date.AddDays(30)
        });

        Assert.False(result.Success);
        Assert.Equal("SELF_RESIGNATION_DISABLED_BY_POLICY", result.Message);
        fixture.EmployeeExitRepository.Verify(x => x.AddOne(It.IsAny<EmployeeExit>()), Times.Never);
    }

    [Fact]
    public async Task SubmitOwnResignation_WhenPolicyAllowsSelfResignation_CreatesResignationCase()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "employee-1");
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = "company-1", UserId = "employee-1", FirstName = "Test", LastName = "Employee", Email = "test@example.com", Status = true, EmploymentStatus = "Active",
                DateOfJoining = DateTime.UtcNow.Date.AddYears(-1).ToString("yyyy-MM-dd"), Lifecycle = new EmployeeLifecycleState()
            });
        fixture.EmployeeExitRepository.Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        fixture.EmployeeExitRepository.Setup(x => x.AddOne(It.IsAny<EmployeeExit>())).ReturnsAsync(new Result { Success = true });
        fixture.EmployeeExitPolicyRepository.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExitPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([new EmployeeExitPolicy { CompanyId = "company-1", IsActive = true, AllowEmployeeSelfResignation = true, EnableOffboarding = true, Version = 1 }]);
        fixture.EmployeeRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<EmpUser>())).ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.SubmitOwnResignation(new InitiateEmployeeExitRequest
        {
            EmployeeUserId = "employee-1", ExitType = "Resigned", LastWorkingDate = DateTime.UtcNow.Date.AddDays(30)
        });

        Assert.True(result.Success);
        fixture.EmployeeExitRepository.Verify(x => x.AddOne(It.Is<EmployeeExit>(exit => exit.ExitType == "Resigned")), Times.Once);
    }

    [Fact]
    public async Task ReviewEmployeeExit_CombinedManagerAndHr_ApprovesBothStages()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "combined-reviewer");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "combined-reviewer", "company-1", null)).ReturnsAsync(true);
        var exit = new EmployeeExit
        {
            CompanyId = "company-1", EmployeeExitId = "exit-1", EmployeeUserId = "employee-1",
            ReportingManagerUserId = "combined-reviewer", Status = "PendingManagerApproval", IsActive = true, Version = 3,
            PolicySnapshot = new EmployeeExitPolicySnapshot { RequireManagerReview = true, RequireHrReview = true }
        };
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(exit);
        fixture.EmployeeExitRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), exit)).ReturnsAsync(new Result { Success = true });
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser { CompanyId = "company-1", UserId = "employee-1", FirstName = "Test", LastName = "Employee", Email = "test@example.com", Status = true, EmploymentStatus = "Active", Lifecycle = new EmployeeLifecycleState() });
        fixture.EmployeeRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<EmpUser>()))
            .ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.ReviewEmployeeExit(new ReviewEmployeeExitRequest
        {
            EmployeeExitId = "exit-1", Version = 3, Action = "Approve", Notes = "Approved by combined reviewer."
        });

        Assert.True(result.Success);
        Assert.Equal("NoticePeriod", exit.Status);
        Assert.Equal("combined-reviewer", exit.ApprovedByUserId);
        Assert.NotNull(exit.ManagerReviewedAtUtc);
        Assert.NotNull(exit.HrReviewedAtUtc);
        Assert.Contains(exit.AuditHistory, item => item.Action == "CombinedManagerHrApproval");
    }

    [Fact]
    public async Task CompleteEmployeeExit_RequiresVerifiedResignationEvidence_WhenPolicySnapshotsIt()
    {
        var fixture = CreateFixture(dbDuplicateExists: false); SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(new EmployeeExit { CompanyId = "company-1", EmployeeExitId = "exit-1", EmployeeUserId = "employee-1", Status = "NoticePeriod", IsActive = true, LastWorkingDate = DateTime.UtcNow.Date.AddDays(-1), PolicySnapshot = new EmployeeExitPolicySnapshot { RequireResignationEvidence = true } });

        var result = await fixture.Service.CompleteEmployeeExit(new CompleteEmployeeExitRequest { EmployeeUserId = "employee-1" });

        Assert.False(result.Success); Assert.Equal("RESIGNATION_EVIDENCE_PENDING", result.Message);
    }

    [Fact]
    public async Task CompleteEmployeeExit_RequiresAcknowledgement_WhenPolicySnapshotsIt()
    {
        var fixture = CreateFixture(dbDuplicateExists: false); SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(new EmployeeExit { CompanyId = "company-1", EmployeeExitId = "exit-1", EmployeeUserId = "employee-1", Status = "NoticePeriod", IsActive = true, LastWorkingDate = DateTime.UtcNow.Date.AddDays(-1), PolicySnapshot = new EmployeeExitPolicySnapshot { RequireEmployeeAcknowledgement = true }, PostExitAccess = new EmployeeExitPostExitAccess { AcknowledgementStatus = "Pending" } });

        var result = await fixture.Service.CompleteEmployeeExit(new CompleteEmployeeExitRequest { EmployeeUserId = "employee-1" });

        Assert.False(result.Success); Assert.Equal("EMPLOYEE_ACKNOWLEDGEMENT_PENDING", result.Message);
    }

    [Fact]
    public async Task CompleteEmployeeExit_UsesCompatibilityPath_WhenMongoTransactionsAreUnavailable()
    {
        var fixture = CreateFixture(dbDuplicateExists: false); SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);
        var exit = new EmployeeExit
        {
            CompanyId = "company-1", EmployeeExitId = "exit-1", EmployeeUserId = "employee-1", ExitType = "Resigned",
            Status = "NoticePeriod", IsActive = true, Version = 5, LastWorkingDate = DateTime.UtcNow.Date.AddDays(-1),
            PostExitAccess = new EmployeeExitPostExitAccess { PersonalEmail = "former.employee@example.com", PersonalEmailVerified = true }
        };
        var employee = new EmpUser { CompanyId = "company-1", UserId = "employee-1", EmployeeId = "EMP001", FirstName = "Former", LastName = "Employee", Email = "former.employee@example.com", Status = true };
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(exit);
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>())).ReturnsAsync(employee);
        fixture.EmployeeExitRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), exit)).ReturnsAsync(new Result { Success = true });
        fixture.EmployeeRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), employee)).ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.CompleteEmployeeExit(new CompleteEmployeeExitRequest { EmployeeExitId = "exit-1", Version = 5 });

        Assert.True(result.Success);
        Assert.False(exit.IsActive);
        Assert.Equal("Resigned", employee.EmploymentStatus);
        Assert.False(employee.Status);
        fixture.EmployeeExitRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), exit), Times.Once);
        fixture.EmployeeRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), employee), Times.Once);
    }

    [Fact]
    public async Task ResolveFormerEmployeeRequest_UpdatesTenantCaseAndWritesAuditEvent()
    {
        var fixture = CreateFixture(dbDuplicateExists: false); SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);
        var exit = new EmployeeExit
        {
            CompanyId = "company-1", EmployeeExitId = "exit-1", Version = 4,
            FormerEmployeeRequests = [new EmployeeExitFormerEmployeeRequest { RequestId = "request-1", Type = "RelievingLetter", Details = "Please issue it." }]
        };
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(exit);
        fixture.EmployeeExitRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), It.IsAny<EmployeeExit>())).ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.ResolveEmployeeExitFormerEmployeeRequest(new ResolveEmployeeExitFormerEmployeeRequest { EmployeeExitId = "exit-1", RequestId = "request-1", Version = 4, Status = "Issued", Notes = "Document published to the portal." });

        Assert.True(result.Success); Assert.Equal("Issued", exit.FormerEmployeeRequests[0].Status); Assert.Equal("hr-user", exit.FormerEmployeeRequests[0].ResolvedByUserId);
        Assert.Contains(exit.AuditHistory, x => x.Action == "FormerEmployeeRequestIssued");
        fixture.EmployeeExitRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), exit), Times.Once);
    }

    private static void SetExitContext(EmployeeServiceFixture fixture, string userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("user_id", userId),
            new Claim("company_id", "company-1")
        ], "test"));
        fixture.HttpContextAccessor.SetupGet(x => x.HttpContext).Returns(context);
    }

    [Fact]
    public async Task BulkImportEmployees_DuplicateEmpIdOrEmailInDb_FailsRow()
    {
        var fixture = CreateFixture(dbDuplicateExists: true);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP030", FirstName = "David", LastName = "Miller", Email = "david.miller@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal(0, result.MethodResult.CreatedCount);
        Assert.Equal(1, result.MethodResult.FailedCount);
        Assert.Equal("Failed", result.MethodResult.Results[0].Status);
        Assert.Equal("Duplicate email.", result.MethodResult.Results[0].Message);
    }

    [Fact]
    public async Task BulkImportEmployees_EmptyPayload_ReturnsFailedResult()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto { Employees = [] };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.False(result.Success);
        Assert.Contains("must contain 1 to", result.Message);
    }

    [Fact]
    public async Task BulkImportEmployees_InvalidEmail_ReturnsFailedRow()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP040", FirstName = "Invalid", LastName = "Email", Email = "invalid", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(0, result.MethodResult.CreatedCount);
        Assert.Equal(1, result.MethodResult.FailedCount);
        Assert.Contains("Invalid email format", result.MethodResult.Results[0].Message);
    }

    [Fact]
    public async Task BulkImportEmployees_NewRoleOrDepartment_ReturnsFailedRow()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP050", FirstName = "N", LastName = "Role", Email = "n.role@example.com", Role = "UnknownRole", Department = "Engineering", JobRole = "Developer", DateOfJoining = "2026-10-01" },
                new BulkImportEmployeeItemDto { EmpId = "EMP051", FirstName = "N", LastName = "Department", Email = "n.department@example.com", Role = "Employee", Department = "UnknownDepartment", JobRole = "Developer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(0, result.MethodResult.CreatedCount);
        Assert.Equal(2, result.MethodResult.FailedCount);
        Assert.Contains(result.MethodResult.Results, r => r.Message.Contains("Role not found"));
        Assert.Contains(result.MethodResult.Results, r => r.Message.Contains("Department not found"));
    }

    [Fact]
    public async Task BulkImportEmployees_NewJobRole_CreatesJobRoleAndEmployee()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP060", FirstName = "New", LastName = "JobRole", Email = "new.jobrole@example.com", Role = "Employee", Department = "Engineering", JobRole = "AI Engineer", DateOfJoining = "2026-10-01" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(1, result.MethodResult.CreatedCount);
        fixture.JobTitlesRepository.Verify(r => r.AddOne(It.Is<JobTitles>(j => j.Titles.Any(t => t.Label == "AI Engineer"))), Times.Once);
    }

    [Fact]
    public async Task BulkImportEmployees_MissingDateOfJoining_FailsRow()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP070", FirstName = "Missing", LastName = "DOJ", Email = "missing.doj@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(0, result.MethodResult.CreatedCount);
        Assert.Equal(1, result.MethodResult.FailedCount);
        Assert.Contains("Required fields", result.MethodResult.Results[0].Message);
    }

    [Fact]
    public async Task BulkImportEmployees_InvalidDateOfJoining_FailsRow()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto { EmpId = "EMP071", FirstName = "Invalid", LastName = "DOJ", Email = "invalid.doj@example.com", Role = "Employee", Department = "Engineering", JobRole = "Developer", DateOfJoining = "not-a-valid-date" }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(0, result.MethodResult.CreatedCount);
        Assert.Equal(1, result.MethodResult.FailedCount);
        Assert.Contains("Invalid Date of Joining format", result.MethodResult.Results[0].Message);
    }

    [Theory]
    [InlineData(false, "DRAFT", "PENDING", 1175, 100, 65)]
    [InlineData(true, "DRAFT", "PENDING", 0, 0, 0)]
    [InlineData(false, "PROCESSED", "PENDING", 0, 0, 0)]
    [InlineData(false, "DRAFT", "PAID", 0, 0, 0)]
    public async Task CalculateEmployeeFinalSettlement_DoesNotDoubleCountPublishedPayroll(bool isProcessed, string workflowStatus, string paymentStatus, decimal expectedSalary, decimal expectedBonus, decimal expectedDeductions)
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);

        var exit = new EmployeeExit
        {
            CompanyId = "company-1",
            EmployeeExitId = "exit-1",
            EmployeeUserId = "employee-1",
            Status = "NoticePeriod",
            IsActive = true,
            Version = 5,
            LastWorkingDate = DateTime.UtcNow.Date.AddDays(-1),
            AuditHistory = []
        };
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(exit);
        fixture.EmployeeExitRepository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeExit>>(), It.IsAny<EmployeeExit>())).ReturnsAsync(new Result { Success = true });
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>())).ReturnsAsync(new EmpUser
        {
            CompanyId = "company-1", UserId = "employee-1", EmployeeId = "emp-1",
            FirstName = "Former", LastName = "Employee", Email = "former.employee@example.com"
        });

        var payrollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var draftPayroll = new EmpPayRoll
        {
            Id = "payroll-1",
            CompanyId = "company-1",
            // Older generated payrolls can retain an obsolete user link. F&F must still
            // find the tenant-scoped record through the stable employee identifier.
            UserId = "stale-user-link",
            EmployeeId = "emp-1",
            PayMonth = new DateTime(2026, 9, 1),
            PaidDate = new DateTime(2026, 9, 20),
            BasicPay = 1000m,
            Bonus = 100m,
            PaidDays = 30f,
            IsProcessed = isProcessed,
            WorkflowStatus = workflowStatus,
            PaymentStatus = paymentStatus,
            CreatedAt = new DateTime(2026, 9, 18, 9, 0, 0, DateTimeKind.Utc),
            Allowance = new Allowance { HRA = 100m, LTA = 50m, OtherAllowance = 25m },
            Deduction = new Deduction { IncomeTax = 10m, HealthInsurance = 20m, EPF = 30m, ESIC = 5m, LossOfPay = 0m }
        };
        payrollRepo.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([draftPayroll]);

        EmployeeFinalSettlement? persistedSettlement = null;
        var settlementRepo = new Mock<IMongoDbRepository<EmployeeFinalSettlement>>();
        settlementRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeFinalSettlement, bool>>>(), It.IsAny<bool>())).ReturnsAsync((EmployeeFinalSettlement?)null);
        var mongoClient = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.SetupGet(x => x.IsInTransaction).Returns(true);
        session.Setup(x => x.WithTransactionAsync(It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(), It.IsAny<TransactionOptions>(), It.IsAny<CancellationToken>())).Returns((Func<IClientSessionHandle, CancellationToken, Task<bool>> callback, TransactionOptions _, CancellationToken token) => callback(session.Object, token));
        mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(session.Object);
        var settlementCollection = new Mock<IMongoCollection<EmployeeFinalSettlement>>();
        settlementRepo.Setup(x => x.GetCollection()).Returns(settlementCollection.Object);
        settlementCollection.Setup(x => x.InsertOneAsync(session.Object, It.IsAny<EmployeeFinalSettlement>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IClientSessionHandle, EmployeeFinalSettlement, InsertOneOptions, CancellationToken>((_, document, _, _) => persistedSettlement = document)
            .Returns(Task.CompletedTask);
        settlementCollection.Setup(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmployeeFinalSettlement>>(), It.IsAny<EmployeeFinalSettlement>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        var payrollCollection = new Mock<IMongoCollection<EmpPayRoll>>();
        payrollRepo.Setup(x => x.GetCollection()).Returns(payrollCollection.Object);
        payrollRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(draftPayroll);
        payrollCollection.Setup(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        var exitCollection = new Mock<IMongoCollection<EmployeeExit>>();

        fixture.EmployeeExitRepository.Setup(x => x.GetCollection()).Returns(exitCollection.Object);
        exitCollection.Setup(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmployeeExit>>(), It.IsAny<EmployeeExit>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        var service = new EmployeeService(
            fixture.EducationRepository.Object,
            fixture.Mapper.Object,
            fixture.CertificationRepository.Object,
            fixture.SummaryRepository.Object,
            fixture.EmployeeRepository.Object,
            fixture.EmployeeIdSequenceRepository.Object,
            fixture.RolesRepository.Object,
            fixture.RoleService.Object,
            fixture.EmployeeSkillsRepository.Object,
            fixture.CompanyRepository.Object,
            fixture.MailTemplateRepository.Object,
            fixture.PriorityTaskQueue.Object,
            fixture.MiddlewareService.Object,
            fixture.HttpContextAccessor.Object,
            fixture.DepartmentRepository.Object,
            fixture.SkillsRepository.Object,
            fixture.WorkHistoryRepository.Object,
            fixture.UserNotificationRepository.Object,
            fixture.NotificationsRepository.Object,
            fixture.JobTitlesRepository.Object,
            fixture.CustomAttributeRepository.Object,
            fixture.CustomAttributeValueRepository.Object,
            fixture.NotificationService.Object,
            fixture.NotificationPreferenceRepository.Object,
            fixture.UserSecurityTokenRepository.Object,
            fixture.OfficeScheduleRepository.Object,
            fixture.EmployeeScheduleAssignmentRepository.Object,
            fixture.Logger.Object,
            employeeExitRepository: fixture.EmployeeExitRepository.Object,
            employeeFinalSettlementRepository: settlementRepo.Object,
            employeePayrollRepository: payrollRepo.Object,
            mongoClient: mongoClient.Object);

        var result = await service.CalculateEmployeeFinalSettlement(new CalculateEmployeeFinalSettlementRequest { EmployeeExitId = "exit-1", Version = 5 });

        Assert.True(result.Success);
        settlementCollection.Verify(x => x.InsertOneAsync(session.Object, It.Is<EmployeeFinalSettlement>(s => s.Status == "Calculated" && s.SourcePayrollId == "payroll-1" && s.SalaryPayable == expectedSalary && s.BonusesAndIncentives == expectedBonus && s.Deductions == expectedDeductions), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(x => x.WithTransactionAsync(It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(), It.IsAny<TransactionOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        if (expectedSalary > 0)
        {
            Assert.NotNull(persistedSettlement);
            persistedSettlement!.Status = "Approved";
            persistedSettlement.Version++;
            exit.Version++;
            settlementRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeFinalSettlement, bool>>>(), It.IsAny<bool>())).ReturnsAsync(persistedSettlement);
            var mismatchedPayment = await service.UpdateEmployeeFinalSettlementPayment(new UpdateEmployeeFinalSettlementPaymentRequest { EmployeeExitId = "exit-1", Status = "Paid", PaidAmount = persistedSettlement.NetPayable + 1 });
            Assert.False(mismatchedPayment.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, mismatchedPayment.StatusCode);
            var paymentResult = await service.UpdateEmployeeFinalSettlementPayment(new UpdateEmployeeFinalSettlementPaymentRequest
            {
                EmployeeExitId = "exit-1", Status = "Paid", PaidAmount = persistedSettlement.NetPayable, PaymentReference = "FNF-001"
            });
            Assert.True(paymentResult.Success);
            Assert.True(draftPayroll.IsProcessed);
            Assert.Equal("PAID", draftPayroll.PaymentStatus);
            Assert.Equal("PAID", draftPayroll.WorkflowStatus);
            payrollCollection.Verify(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task CalculateEmployeeFinalSettlement_WhenExitVersionConflicts_DoesNotCommitTransaction()
    {
        var fixture = CreateFixture(dbDuplicateExists: false);
        SetExitContext(fixture, "hr-user");
        fixture.RoleService.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), "hr-user", "company-1", null)).ReturnsAsync(true);
        var exit = new EmployeeExit
        {
            CompanyId = "company-1", EmployeeExitId = "exit-1", EmployeeUserId = "employee-1",
            Status = "NoticePeriod", IsActive = true, Version = 5,
            LastWorkingDate = DateTime.UtcNow.Date.AddDays(-1), AuditHistory = []
        };
        fixture.EmployeeExitRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>())).ReturnsAsync(exit);
        fixture.EmployeeRepository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>())).ReturnsAsync(new EmpUser
        {
            CompanyId = "company-1", UserId = "employee-1", EmployeeId = "emp-1", FirstName = "Former", LastName = "Employee", Email = "former.employee@example.com"
        });
        var payrollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        payrollRepo.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([
            new EmpPayRoll { Id = "payroll-1", CompanyId = "company-1", EmployeeId = "emp-1", UserId = "employee-1", PayMonth = new DateTime(exit.LastWorkingDate.Year, exit.LastWorkingDate.Month, 1), PaidDate = DateTime.UtcNow, BasicPay = 0m, PaidDays = 0f, Allowance = new Allowance(), Deduction = new Deduction(), DeductionLines = [] }
        ]);
        var settlementRepo = new Mock<IMongoDbRepository<EmployeeFinalSettlement>>();
        settlementRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmployeeFinalSettlement, bool>>>(), It.IsAny<bool>())).ReturnsAsync((EmployeeFinalSettlement?)null);
        var session = new Mock<IClientSessionHandle>();
        session.Setup(x => x.WithTransactionAsync(It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(), It.IsAny<TransactionOptions>(), It.IsAny<CancellationToken>()))
            .Returns((Func<IClientSessionHandle, CancellationToken, Task<bool>> callback, TransactionOptions _, CancellationToken token) => callback(session.Object, token));
        var mongoClient = new Mock<IMongoClient>();
        mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(session.Object);
        var settlementCollection = new Mock<IMongoCollection<EmployeeFinalSettlement>>();
        settlementRepo.Setup(x => x.GetCollection()).Returns(settlementCollection.Object);
        settlementCollection.Setup(x => x.InsertOneAsync(session.Object, It.IsAny<EmployeeFinalSettlement>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var exitCollection = new Mock<IMongoCollection<EmployeeExit>>();
        fixture.EmployeeExitRepository.Setup(x => x.GetCollection()).Returns(exitCollection.Object);
        exitCollection.Setup(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmployeeExit>>(), It.IsAny<EmployeeExit>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));

        var service = new EmployeeService(
            fixture.EducationRepository.Object, fixture.Mapper.Object, fixture.CertificationRepository.Object, fixture.SummaryRepository.Object,
            fixture.EmployeeRepository.Object, fixture.EmployeeIdSequenceRepository.Object, fixture.RolesRepository.Object, fixture.RoleService.Object,
            fixture.EmployeeSkillsRepository.Object, fixture.CompanyRepository.Object, fixture.MailTemplateRepository.Object, fixture.PriorityTaskQueue.Object,
            fixture.MiddlewareService.Object, fixture.HttpContextAccessor.Object, fixture.DepartmentRepository.Object, fixture.SkillsRepository.Object,
            fixture.WorkHistoryRepository.Object, fixture.UserNotificationRepository.Object, fixture.NotificationsRepository.Object, fixture.JobTitlesRepository.Object,
            fixture.CustomAttributeRepository.Object, fixture.CustomAttributeValueRepository.Object, fixture.NotificationService.Object,
            fixture.NotificationPreferenceRepository.Object, fixture.UserSecurityTokenRepository.Object, fixture.OfficeScheduleRepository.Object,
            fixture.EmployeeScheduleAssignmentRepository.Object, fixture.Logger.Object,
            employeeExitRepository: fixture.EmployeeExitRepository.Object, employeeFinalSettlementRepository: settlementRepo.Object,
            employeePayrollRepository: payrollRepo.Object, mongoClient: mongoClient.Object);

        var result = await service.CalculateEmployeeFinalSettlement(new CalculateEmployeeFinalSettlementRequest { EmployeeExitId = "exit-1", Version = 5 });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal("FINAL_SETTLEMENT_CHANGED_RELOAD_AND_RETRY", result.Message);
        settlementCollection.Verify(x => x.InsertOneAsync(session.Object, It.IsAny<EmployeeFinalSettlement>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        exitCollection.Verify(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmployeeExit>>(), It.IsAny<EmployeeExit>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    private static EmployeeServiceFixture CreateFixture(bool dbDuplicateExists)
    {
        var fixture = new EmployeeServiceFixture();
        ConfigManager.AppSettings = new AppSettings { AppUrl = "http://localhost/", APIUrl = "http://localhost/api", AppVersion = "test", IsForDebug = true };

        fixture.EmployeeRepository
            .Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(dbDuplicateExists);
        fixture.EmployeeRepository
            .Setup(x => x.AddOne(It.IsAny<EmpUser>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.NotificationPreferenceRepository
            .Setup(x => x.AddOne(It.IsAny<NotificationPreference>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.MiddlewareService
            .Setup(x => x.GetDefaultNotificationPreferences())
            .Returns([]);
        fixture.MiddlewareService
            .Setup(x => x.GetUserById(It.IsAny<string>()))
            .ReturnsAsync(new UserModel { UserId = "admin-user-id", CompanyId = "company-1", FirstName = "Admin", LastName = "User" });

        fixture.RolesRepository
            .Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<Roles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new Roles { RolesId = "role-1", CompanyId = "company-1", Titles = "Employee", IsDeleted = false, UserRoles = [] }
            ]);
        fixture.RolesRepository
            .Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<Roles, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        fixture.DepartmentRepository
            .Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new Department
                {
                    DepartmentId = "dept-1",
                    CompanyId = "company-1",
                    IsActive = true,
                    Titles = [new MultilingualModel { Language = "en", Label = "Engineering" }]
                }
            ]);
        fixture.DepartmentRepository
            .Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        fixture.JobTitlesRepository
            .Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new JobTitles
                {
                    JobTitleId = "job-1",
                    CompanyId = "company-1",
                    DepartmentId = "dept-1",
                    IsActive = true,
                    Titles = [new MultilingualModel { Language = "en", Label = "Developer" }]
                }
            ]);
        fixture.JobTitlesRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobTitles
            {
                JobTitleId = "job-1",
                CompanyId = "company-1",
                DepartmentId = "dept-1",
                IsActive = true,
                Titles = [new MultilingualModel { Language = "en", Label = "Developer" }]
            });

        fixture.JobTitlesRepository
            .Setup(x => x.AddOne(It.IsAny<JobTitles>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Codeji", DefaultLanguage = "en", ApplicationLanguage = [], Status = true, AutoGenerateEmployeeId = false });

        fixture.MailTemplateRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<MailTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new MailTemplate { subject = "Welcome", body = "Hello {{EmployeeName}} {{PasswordCreationLink}}" });

        fixture.UserSecurityTokenRepository
            .Setup(x => x.AddOne(It.IsAny<UserSecurityToken>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.Service = new EmployeeService(
            fixture.EducationRepository.Object,
            fixture.Mapper.Object,
            fixture.CertificationRepository.Object,
            fixture.SummaryRepository.Object,
            fixture.EmployeeRepository.Object,
            fixture.EmployeeIdSequenceRepository.Object,
            fixture.RolesRepository.Object,
            fixture.RoleService.Object,
            fixture.EmployeeSkillsRepository.Object,
            fixture.CompanyRepository.Object,
            fixture.MailTemplateRepository.Object,
            fixture.PriorityTaskQueue.Object,
            fixture.MiddlewareService.Object,
            fixture.HttpContextAccessor.Object,
            fixture.DepartmentRepository.Object,
            fixture.SkillsRepository.Object,
            fixture.WorkHistoryRepository.Object,
            fixture.UserNotificationRepository.Object,
            fixture.NotificationsRepository.Object,
            fixture.JobTitlesRepository.Object,
            fixture.CustomAttributeRepository.Object,
            fixture.CustomAttributeValueRepository.Object,
            fixture.NotificationService.Object,
            fixture.NotificationPreferenceRepository.Object,
            fixture.UserSecurityTokenRepository.Object,
            fixture.OfficeScheduleRepository.Object,
            fixture.EmployeeScheduleAssignmentRepository.Object,
            fixture.Logger.Object,
            employeeExitRepository: fixture.EmployeeExitRepository.Object,
            employeeExitPolicyRepository: fixture.EmployeeExitPolicyRepository.Object);

        return fixture;
    }

    private sealed class EmployeeServiceFixture
    {
        public EmployeeService Service { get; set; } = null!;
        public Mock<IMongoDbRepository<EmpEducationDetails>> EducationRepository { get; } = new();
        public Mock<IMapper> Mapper { get; } = new();
        public Mock<IMongoDbRepository<EmpCertificationDetails>> CertificationRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpSummary>> SummaryRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpUser>> EmployeeRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeIdSequence>> EmployeeIdSequenceRepository { get; } = new();
        public Mock<IMongoDbRepository<Roles>> RolesRepository { get; } = new();
        public Mock<IRoleService> RoleService { get; } = new();
        public Mock<IMongoDbRepository<EmpSkills>> EmployeeSkillsRepository { get; } = new();
        public Mock<IMongoDbRepository<Company>> CompanyRepository { get; } = new();
        public Mock<IMongoDbRepository<MailTemplate>> MailTemplateRepository { get; } = new();
        public Mock<IPriorityTaskQueue> PriorityTaskQueue { get; } = new();
        public Mock<IMiddlewareService> MiddlewareService { get; } = new();
        public Mock<IHttpContextAccessor> HttpContextAccessor { get; } = new();
        public Mock<IMongoDbRepository<Department>> DepartmentRepository { get; } = new();
        public Mock<IMongoDbRepository<Skills>> SkillsRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpWorkHistory>> WorkHistoryRepository { get; } = new();
        public Mock<IMongoDbRepository<UserNotifications>> UserNotificationRepository { get; } = new();
        public Mock<IMongoDbRepository<Notifications>> NotificationsRepository { get; } = new();
        public Mock<IMongoDbRepository<JobTitles>> JobTitlesRepository { get; } = new();
        public Mock<IMongoDbRepository<CustomAttribute>> CustomAttributeRepository { get; } = new();
        public Mock<IMongoDbRepository<CustomAttributeValue>> CustomAttributeValueRepository { get; } = new();
        public Mock<INotificationService> NotificationService { get; } = new();
        public Mock<IMongoDbRepository<NotificationPreference>> NotificationPreferenceRepository { get; } = new();
        public Mock<IMongoDbRepository<UserSecurityToken>> UserSecurityTokenRepository { get; } = new();
        public Mock<IMongoDbRepository<CompanyOfficeSchedule>> OfficeScheduleRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeScheduleAssignment>> EmployeeScheduleAssignmentRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeExit>> EmployeeExitRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeExitPolicy>> EmployeeExitPolicyRepository { get; } = new();
        public Mock<ILogger<EmployeeService>> Logger { get; } = new();
    }
}


