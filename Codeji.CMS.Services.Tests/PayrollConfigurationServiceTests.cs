using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollConfigurationServiceTests
{
    [Fact]
    public async Task SavePayGroup_RejectsUnsupportedSchedule()
    {
        var groups = Repository(Array.Empty<PayrollPayGroup>());
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SavePayGroup("company-1", "actor", new PayrollPayGroupDto
        {
            Name = "Quarterly", Schedule = "QUARTERLY", EffectiveFrom = new DateTime(2026, 10, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("monthly payroll schedules", result.Message, StringComparison.OrdinalIgnoreCase);
        groups.Verify(x => x.AddOne(It.IsAny<PayrollPayGroup>()), Times.Never);
    }

    [Fact]
    public async Task SavePayGroup_RejectsAssignmentOnlyPolicyProfile()
    {
        var groups = Repository(Array.Empty<PayrollPayGroup>());
        var policies = Repository(new[] { new PayrollPolicyProfile { Id = "policy-assignment", CompanyId = "company-1", Name = "Employee only", Scope = "ASSIGNMENT_ONLY", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, policies.Object);

        var result = await service.SavePayGroup("company-1", "actor", new PayrollPayGroupDto
        {
            Name = "Monthly", Schedule = "MONTHLY", PolicyProfileId = "policy-assignment", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        groups.Verify(x => x.AddOne(It.IsAny<PayrollPayGroup>()), Times.Never);
    }

    [Fact]
    public async Task SavePayGroup_RejectsPolicyProfileThatDoesNotCoverGroupWindow()
    {
        var groups = Repository(Array.Empty<PayrollPayGroup>());
        var policies = Repository(new[] { new PayrollPolicyProfile { Id = "policy-expiring", CompanyId = "company-1", Name = "Ends this year", Scope = "COMPANY_DEFAULT", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 12, 31), IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, policies.Object);

        var result = await service.SavePayGroup("company-1", "actor", new PayrollPayGroupDto
        {
            Name = "Monthly", Schedule = "MONTHLY", PolicyProfileId = "policy-expiring", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("entire pay group date range", result.Message, StringComparison.OrdinalIgnoreCase);
        groups.Verify(x => x.AddOne(It.IsAny<PayrollPayGroup>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsPayGroupFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RecordsCreatorForMakerCheckerApproval()
    {
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(
            Repository(Array.Empty<PayrollPayGroup>()).Object,
            assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object,
            Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "maker-1", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.True(result.Success);
        assignments.Verify(x => x.AddOne(It.Is<EmployeePayrollAssignment>(assignment => assignment.CreatedBy == "maker-1" && !assignment.IsApproved)), Times.Once);
    }

    [Fact]
    public async Task ApproveAssignment_RejectsSelfApproval()
    {
        var assignment = new EmployeePayrollAssignment
        {
            Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1",
            EffectiveFrom = new DateTime(2026, 9, 1), CreatedBy = "maker-1", IsApproved = false
        };
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        assignments.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(assignment);
        var service = new PayrollConfigurationService(
            Repository(Array.Empty<PayrollPayGroup>()).Object,
            assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object,
            Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.ApproveAssignment("company-1", "MAKER-1", "employee-1", new DateTime(2026, 9, 1));

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        assignments.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAssignment_IncrementsAssignmentVersion()
    {
        var assignment = new EmployeePayrollAssignment
        {
            Id = "assignment-v4", CompanyId = "company-1", EmployeeId = "employee-1", Version = 4,
            EffectiveFrom = new DateTime(2026, 9, 1), CreatedBy = "maker-1", IsApproved = false
        };
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        assignments.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(assignment);
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(
            Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.ApproveAssignment("company-1", "approver-1", "employee-1", new DateTime(2026, 9, 1));

        Assert.True(result.Success);
        Assert.True(assignment.IsApproved);
        Assert.Equal(5, assignment.Version);
        assignments.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(),
            It.Is<EmployeePayrollAssignment>(saved => saved.Version == 5 && saved.ApprovedBy == "approver-1")), Times.Once);
    }

    [Fact]
    public async Task SaveAssignment_RejectsOverlappingEffectivePeriod()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(new[] { new EmployeePayrollAssignment { Id = "existing", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsPolicyProfileFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var policies = Repository(new[] { new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, policies.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", PolicyProfileId = "policy-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsPayGroupThatDoesNotCoverTheFullAssignmentWindow()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Ends early", EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 12, 31), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("entire assignment date range", result.Message, StringComparison.OrdinalIgnoreCase);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsTemplateRestrictedToDifferentPayGroup()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var templates = Repository(new[] { new SalaryTemplate { Id = "template-1", CompanyId = "company-1", Name = "Engineering", PayGroupId = "group-2", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, templates.Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", SalaryTemplateId = "template-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("restricted to a different pay group", result.Message, StringComparison.OrdinalIgnoreCase);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_DerivesPayGroupFromSelectedTemplate()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var templates = Repository(new[] { new SalaryTemplate { Id = "template-1", CompanyId = "company-1", Name = "Engineering", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, templates.Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", SalaryTemplateId = "template-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.True(result.Success);
        assignments.Verify(x => x.AddOne(It.Is<EmployeePayrollAssignment>(assignment => assignment.PayGroupId == "group-1" && assignment.SalaryTemplateId == "template-1")), Times.Once);
    }

    [Fact]
    public async Task SaveAssignment_RejectsNonMonthlyLegacyPayGroup()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-weekly", CompanyId = "company-1", Name = "Legacy weekly", Schedule = "WEEKLY", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-weekly", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("monthly pay groups", result.Message, StringComparison.OrdinalIgnoreCase);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }
    [Fact]
    public async Task SaveAssignment_RejectsEmployeeFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var employees = Repository(new[] { new EmpUser { UserId = "user-1", EmployeeId = "employee-1", CompanyId = "company-2", FirstName = "Other", LastName = null, Email = "other@example.com" } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, employees.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsUnapprovedPolicyProfile()
    {
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var profiles = Repository(new[] { new PayrollPolicyProfile { Id = "policy-draft", CompanyId = "company-1", Name = "Draft", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "DRAFT", IsActive = true } });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, profiles.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PolicyProfileId = "policy-draft", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("must be approved", result.Message, StringComparison.OrdinalIgnoreCase);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsPolicyProfileOutsideAssignmentWindow()
    {
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var profiles = Repository(new[] { new PayrollPolicyProfile { Id = "policy-approved", CompanyId = "company-1", Name = "Approved", EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 12, 31), ApprovalStatus = "APPROVED", IsActive = true } });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, profiles.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PolicyProfileId = "policy-approved", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("policy profile must be effective", result.Message, StringComparison.OrdinalIgnoreCase);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }
    [Fact]
    public async Task PreviewBulkAssignments_IsCompanyScopedAndFlagsOverlaps()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true } });
        var assignments = Repository(new[] { new EmployeePayrollAssignment { Id = "existing", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true } });
        var employees = Repository(new[]
        {
            new EmpUser { EmployeeId = "employee-1", CompanyId = "company-1", FirstName = "Ready?", LastName = "No", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
            new EmpUser { EmployeeId = "employee-2", CompanyId = "company-1", FirstName = "Ready", LastName = "Yes", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
            new EmpUser { EmployeeId = "employee-3", CompanyId = "company-2", FirstName = "Other", LastName = "Tenant", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
        });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, employees.Object);

        var result = await service.PreviewBulkAssignments("company-1", new PayrollBulkAssignmentRequestDto
        {
            PayGroupId = "group-1", Department = "Engineering", JobRole = "Developer", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.True(result.Success);
        Assert.Equal(new[] { "employee-1", "employee-2" }, result.MethodResults.Select(x => x.EmployeeId));
        Assert.False(result.MethodResults.Single(x => x.EmployeeId == "employee-1").CanSubmit);
        Assert.True(result.MethodResults.Single(x => x.EmployeeId == "employee-2").CanSubmit);
    }

    [Fact]
    public async Task PreviewBulkAssignments_RejectsPayGroupFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-2", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, Repository(Array.Empty<EmpUser>()).Object);

        var result = await service.PreviewBulkAssignments("company-1", new PayrollBulkAssignmentRequestDto
        {
            PayGroupId = "group-2", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task PreviewBulkAssignments_RejectsPayGroupThatDoesNotCoverRequestedWindow()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Ends 2026", EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 12, 31), IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, Repository(Array.Empty<EmpUser>()).Object);

        var result = await service.PreviewBulkAssignments("company-1", new PayrollBulkAssignmentRequestDto
        {
            PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 11, 1), EffectiveTo = new DateTime(2027, 1, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("entire assignment date range", result.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task SavePayGroup_ReportsConflictWhenVersionChangesAfterInitialRead()
    {
        var existing = new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1), Version = 3, IsActive = true };
        var groups = Repository(new[] { existing });
        groups.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollPayGroup>>(), It.IsAny<PayrollPayGroup>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = false, StatusCode = 400, Message = "No matching pay group version." });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SavePayGroup("company-1", "actor", new PayrollPayGroupDto
        {
            Id = existing.Id, Version = existing.Version, Name = "Monthly revised", Schedule = "MONTHLY",
            EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("changed while you were editing", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveAssignment_ReportsConflictWhenVersionChangesAfterInitialRead()
    {
        var assignment = new EmployeePayrollAssignment
        {
            Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1",
            EffectiveFrom = new DateTime(2026, 9, 1), CreatedBy = "maker", Version = 2, IsApproved = false
        };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = false, StatusCode = 400, Message = "No matching pending assignment version." });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.ApproveAssignment("company-1", "reviewer", "employee-1", assignment.EffectiveFrom);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("changed while it was being approved", result.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ApproveAssignment_RejectsPreviouslyRejectedOrCancelledRows()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1), IsRejected = true };
        var assignments = Repository(new[] { assignment });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.ApproveAssignment("company-1", "admin", "employee-1", assignment.EffectiveFrom);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        assignments.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }
    [Fact]
    public async Task RejectAssignment_RequiresReasonAndPersistsAdminAuditFields()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1), CreatedBy = "maker", Version = 2 };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var missingReason = await service.RejectAssignment("company-1", "admin", "employee-1", assignment.EffectiveFrom, " ");
        Assert.False(missingReason.Success);
        Assert.Equal(400, missingReason.StatusCode);
        assignments.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()), Times.Never);

        var rejected = await service.RejectAssignment("company-1", "admin", "employee-1", assignment.EffectiveFrom, " Incorrect salary template ");
        Assert.True(rejected.Success);
        Assert.True(assignment.IsRejected);
        Assert.Equal("admin", assignment.RejectedBy);
        Assert.Equal("Incorrect salary template", assignment.RejectionReason);
        Assert.NotNull(assignment.RejectedAtUtc);
        Assert.Equal(3, assignment.Version);
    }

    [Fact]
    public async Task DeletePayGroup_RejectsDeletionWhenAssignedToEmployees()
    {
        var group = new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly" };
        var groups = Repository(new[] { group });
        var assignments = Repository(new[] { new EmployeePayrollAssignment { Id = "assign-1", CompanyId = "company-1", EmployeeId = "emp-1", PayGroupId = "group-1" } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.DeletePayGroup("company-1", "admin", "group-1");
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("assigned to employees", result.Message);
    }

    [Fact]
    public async Task DeletePayGroup_RejectsDeletionWhenLinkedToSalaryTemplates()
    {
        var group = new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly" };
        var groups = Repository(new[] { group });
        var templates = Repository(new[] { new SalaryTemplate { Id = "tpl-1", CompanyId = "company-1", Name = "Standard", PayGroupId = "group-1" } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, templates.Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.DeletePayGroup("company-1", "admin", "group-1");
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("salary templates are linked to it", result.Message);
    }

    [Fact]
    public async Task DeletePayGroup_DeletesWhenUnused()
    {
        var group = new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly" };
        var groups = Repository(new[] { group });
        groups.Setup(x => x.Delete(It.IsAny<FilterDefinition<PayrollPayGroup>>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.DeletePayGroup("company-1", "admin", "group-1");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task EndAssignment_RejectsEndDateBeforeStartDate()
    {
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);
        var result = await service.EndAssignment("company-1", "admin", "emp-1", new DateTime(2026, 10, 1), new DateTime(2026, 9, 30));
        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task EndAssignment_UpdatesEffectiveToOnAssignment()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assign-1", CompanyId = "company-1", EmployeeId = "emp-1", EffectiveFrom = new DateTime(2026, 1, 1), Version = 1 };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.EndAssignment("company-1", "admin", "emp-1", new DateTime(2026, 1, 1), new DateTime(2026, 10, 31));
        Assert.True(result.Success);
        Assert.Equal(new DateTime(2026, 10, 31), assignment.EffectiveTo);
        Assert.Equal(2, assignment.Version);
    }

    [Fact]
    public async Task EndAssignment_ReturnsConflict_WhenAssignmentChangedConcurrently()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assign-1", CompanyId = "company-1", EmployeeId = "emp-1", EffectiveFrom = new DateTime(2026, 1, 1), Version = 1 };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = false, StatusCode = 400 });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.EndAssignment("company-1", "admin", "emp-1", new DateTime(2026, 1, 1), new DateTime(2026, 10, 31));
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("changed while being updated", result.Message);
    }

    [Fact]
    public async Task DeleteAssignment_RejectsDeletingApprovedAssignment()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assign-1", CompanyId = "company-1", EmployeeId = "emp-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true };
        var assignments = Repository(new[] { assignment });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.DeleteAssignment("company-1", "admin", "emp-1", new DateTime(2026, 1, 1));
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("approved payroll assignment cannot be deleted", result.Message);
        assignments.Verify(x => x.Delete(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAssignment_DeletesAssignment()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assign-1", CompanyId = "company-1", EmployeeId = "emp-1", EffectiveFrom = new DateTime(2026, 1, 1) };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Delete(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.DeleteAssignment("company-1", "admin", "emp-1", new DateTime(2026, 1, 1));
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CancelAssignment_PersistsAdminWithdrawalAudit()
    {
        var assignment = new EmployeePayrollAssignment { Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1), CreatedBy = "maker", Version = 1 };
        var assignments = Repository(new[] { assignment });
        assignments.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeePayrollAssignment>>(), It.IsAny<EmployeePayrollAssignment>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new PayrollConfigurationService(Repository(Array.Empty<PayrollPayGroup>()).Object, assignments.Object,
            Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var cancelled = await service.CancelAssignment("company-1", "admin-user", "employee-1", assignment.EffectiveFrom);
        Assert.True(cancelled.Success);
        Assert.True(assignment.IsCancelled);
        Assert.Equal("admin-user", assignment.CancelledBy);
        Assert.NotNull(assignment.CancelledAtUtc);
        Assert.Equal(2, assignment.Version);
    }
    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(IEnumerable<TEntity> items)
    {
        var source = items.ToArray();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(source.Any(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(source.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(source.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}
