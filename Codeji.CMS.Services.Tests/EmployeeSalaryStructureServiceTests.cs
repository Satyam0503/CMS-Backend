using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryStructureServiceTests
{
    [Fact]
    public async Task SaveDraft_PreservesAnExistingDraftsRetiredTemplateLink()
    {
        var salaries = Repository(new[] { new EmployeeSalaryStructure { Id = "draft", CompanyId = "company-1", EmployeeId = "e1", SalaryTemplateId = "retired", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "DRAFT", Version = 2 } });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var templates = Repository(new[] { new SalaryTemplate { Id = "retired", CompanyId = "company-1", Name = "Retired", EffectiveFrom = new DateTime(2026, 1, 1), IsRetired = true } });
        var draft = Draft("e1", new DateTime(2026, 10, 1)); draft.Id = "draft"; draft.Version = 2; draft.SalaryTemplateId = "retired";
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, salaryTemplates: templates.Object);
        var result = await service.SaveDraft("company-1", "actor", draft);
        Assert.True(result.Success);
        salaries.Verify(repository => repository.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.Is<EmployeeSalaryStructure>(value => value.SalaryTemplateId == "retired" && value.Version == 3)), Times.Once);
    }

    [Fact]
    public async Task SaveDraft_RejectsRetiredTemplateForNewSalary()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var templates = Repository(new[] { new SalaryTemplate { Id = "retired", CompanyId = "company-1", Name = "Retired", EffectiveFrom = new DateTime(2026, 1, 1), IsRetired = true } });
        var draft = Draft("e1", new DateTime(2026, 10, 1)); draft.SalaryTemplateId = "retired";
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, salaryTemplates: templates.Object);
        var result = await service.SaveDraft("company-1", "actor", draft);
        Assert.False(result.Success); Assert.Equal(409, result.StatusCode);
        salaries.Verify(repository => repository.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsAggregateDeductionComponentBeforeWriting()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);
        var draft = Draft("e1", new DateTime(2026, 10, 1));
        draft.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = "TOTAL_DEDUCTIONS", ComponentName = "Total", Kind = "DEDUCTION", Value = 500 });
        var result = await service.SaveDraft("company-1", "actor", draft);
        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("itemized", result.Message);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsEmployeeFromAnotherTenant()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-2", FirstName = "Other", LastName = "Tenant", Email = "other@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", Draft("e1", new DateTime(2026, 10, 1)));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsUnsupportedPaymentFrequency()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);
        var draft = Draft("e1", new DateTime(2026, 10, 1));
        draft.PaymentFrequency = "QUARTERLY";

        var result = await service.SaveDraft("company-1", "actor", draft);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("monthly payroll", result.Message, StringComparison.OrdinalIgnoreCase);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsPayGroupFromAnotherTenant()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var payGroups = Repository(new[] { new PayrollPayGroup { Id = "pg-other", CompanyId = "company-2", Name = "Other tenant", EffectiveFrom = new DateTime(2026, 1, 1) } });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.PayGroupId = "pg-other";
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, payGroups: payGroups.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsSalaryTemplateFromAnotherTenant()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var templates = Repository(new[] { new SalaryTemplate { Id = "template-other", CompanyId = "company-2", Name = "Other tenant", EffectiveFrom = new DateTime(2026, 1, 1) } });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.SalaryTemplateId = "template-other";
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, salaryTemplates: templates.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsPayGroupOutsideEffectiveWindow()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var payGroups = Repository(new[] { new PayrollPayGroup { Id = "pg-1", CompanyId = "company-1", Name = "Current", EffectiveFrom = new DateTime(2027, 1, 1) } });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.PayGroupId = "pg-1";
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, payGroups: payGroups.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task GetPending_ReturnsOnlyThisCompanyDraftsAndSubmittedRevisions()
    {
        var salaries = Repository(new[]
        {
            new EmployeeSalaryStructure { Id = "draft", CompanyId = "company-1", EmployeeId = "e1", ApprovalStatus = "DRAFT", EffectiveFrom = new DateTime(2026, 10, 1) },
            new EmployeeSalaryStructure { Id = "submitted", CompanyId = "company-1", EmployeeId = "e2", ApprovalStatus = "SUBMITTED", EffectiveFrom = new DateTime(2026, 10, 2) },
            new EmployeeSalaryStructure { Id = "approved", CompanyId = "company-1", EmployeeId = "e3", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 10, 3) },
            new EmployeeSalaryStructure { Id = "other-tenant", CompanyId = "company-2", EmployeeId = "e4", ApprovalStatus = "DRAFT", EffectiveFrom = new DateTime(2026, 10, 4) }
        });
        var employees = Repository(Array.Empty<EmpUser>());
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.GetPending("company-1");

        Assert.Equal(new[] { "draft", "submitted" }, result.Select(x => x.Id));
    }

    [Fact]
    public async Task SaveDraft_LeavesPriorSalaryOpenUntilNewRevisionIsApproved()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" };
        var salaries = Repository(new[] { prior });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", Draft("e1", new DateTime(2026, 10, 1)));

        Assert.True(result.Success);
        Assert.Null(prior.EffectiveTo);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), prior), Times.Never);
        salaries.Verify(x => x.AddOne(It.Is<EmployeeSalaryStructure>(s => s.EffectiveFrom == new DateTime(2026, 10, 1) && s.ApprovalStatus == "DRAFT")), Times.Once);
    }

    [Fact]
    public async Task Approve_ClosesPriorOpenSalaryAtTheNewRevisionBoundary()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" };
        var next = new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "e1", CreatedBy = "maker", SubmittedBy = "submitter", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { prior, next });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, mongoClient: SalaryTransaction(salaries, 1, 1).Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2026, 9, 30), prior.EffectiveTo);
        Assert.Equal(2, prior.Version);
        Assert.Equal("APPROVED", next.ApprovalStatus);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), prior), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsOverlapWithAlreadyApprovedFutureRevisionWithoutMutatingEitherRow()
    {
        var future = new EmployeeSalaryStructure
        {
            Id = "future", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 11, 1),
            ApprovalStatus = "APPROVED", Status = "ACTIVE", Version = 2
        };
        var next = new EmployeeSalaryStructure
        {
            Id = "new", CompanyId = "company-1", EmployeeId = "e1", CreatedBy = "maker", SubmittedBy = "submitter",
            EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1
        };
        var salaries = Repository(new[] { future, next });
        var service = new EmployeeSalaryStructureService(salaries.Object,
            Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } }).Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("overlaps approved salary revision 'future'", result.Message);
        Assert.Equal("SUBMITTED", next.ApprovalStatus);
        Assert.Equal("APPROVED", future.ApprovalStatus);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsMoreThanOnePriorOpenRevisionInsteadOfLeavingAnOverlappingTimeline()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" };
        var secondPrior = new EmployeeSalaryStructure { Id = "older-open", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2025, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" };
        var next = new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "e1", CreatedBy = "maker", SubmittedBy = "submitter", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { prior, secondPrior, next });
        var service = new EmployeeSalaryStructureService(salaries.Object,
            Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } }).Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("More than one open approved salary revision", result.Message);
        Assert.Null(prior.EffectiveTo);
        Assert.Null(secondPrior.EffectiveTo);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task Approve_AbortsTransactionWhenNewApprovalConflicts()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE", Version = 4 };
        var next = new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { prior, next });
        var client = SalaryTransaction(salaries, 1, 0);
        var service = new EmployeeSalaryStructureService(salaries.Object,
            Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } }).Object, mongoClient: client.Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Null(prior.EffectiveTo);
        Assert.Equal(4, prior.Version);
        Assert.Equal("SUBMITTED", next.ApprovalStatus);
        Assert.Equal(1, next.Version);
        Assert.Null(next.ApprovedBy);
        Assert.Null(next.ApprovedAtUtc);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task Approve_FailsClosedWithoutTransactionClient()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE", Version = 4 };
        var next = new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { prior, next });
        var service = new EmployeeSalaryStructureService(salaries.Object,
            Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } }).Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.Null(prior.EffectiveTo);
        Assert.Equal(4, prior.Version);
    }

    [Fact]
    public async Task Approve_DoesNotApproveNewRevisionWhenPriorVersionWasChanged()
    {
        var prior = new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE", Version = 4 };
        var next = new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { prior, next });
        var client = SalaryTransaction(salaries, 0);
        var service = new EmployeeSalaryStructureService(salaries.Object,
            Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } }).Object, mongoClient: client.Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("SUBMITTED", next.ApprovalStatus);
        Assert.Null(prior.EffectiveTo);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsOverlapWithSubmittedSalary()
    {
        var submitted = new EmployeeSalaryStructure { Id = "submitted", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 9, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW" };
        var salaries = Repository(new[] { submitted });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", Draft("e1", new DateTime(2026, 10, 1)));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task SubmitAndApprove_RequireExpectedLifecycle()
    {
        var row = new EmployeeSalaryStructure { Id = "salary-1", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "DRAFT", Status = "DRAFT", Version = 1 };
        var salaries = Repository(new[] { row });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var submitted = await service.Submit("company-1", "submitter", "salary-1", 1);
        Assert.True(submitted.Success);
        Assert.Equal("SUBMITTED", row.ApprovalStatus);
        var approved = await service.Approve("company-1", "approver", "salary-1", 1);

        Assert.True(approved.Success);
        Assert.Equal("APPROVED", row.ApprovalStatus);
        Assert.Equal("ACTIVE", row.Status);
        Assert.Equal("approver", row.ApprovedBy);
        Assert.Equal(2, row.Version);
    }

    [Fact]
    public async Task Approve_RejectsSalaryCreatorApprovingTheirOwnStructure()
    {
        var row = new EmployeeSalaryStructure { Id = "salary-1", CompanyId = "company-1", EmployeeId = "e1", CreatedBy = "maker", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "SUBMITTED", Status = "PENDING_REVIEW", Version = 1 };
        var salaries = Repository(new[] { row });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.Approve("company-1", "maker", "salary-1", 1);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("SUBMITTED", row.ApprovalStatus);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsMutationOfAnApprovedSalaryRecord()
    {
        var approved = new EmployeeSalaryStructure { Id = "approved", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE", Version = 2 };
        var salaries = Repository(new[] { approved });
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var dto = Draft("e1", new DateTime(2026, 1, 1)); dto.Id = "approved"; dto.Version = 2;
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsReassigningAnExistingSalaryToAnotherEmployee()
    {
        var original = new EmployeeSalaryStructure { Id = "salary-1", CompanyId = "company-1", EmployeeId = "e1", EffectiveFrom = new DateTime(2026, 9, 1), ApprovalStatus = "DRAFT", Status = "DRAFT", Version = 1 };
        var salaries = Repository(new[] { original });
        var employees = Repository(new[]
        {
            new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e1@example.com" },
            new EmpUser { EmployeeId = "e2", CompanyId = "company-1", FirstName = "Employee", LastName = "Two", Email = "e2@example.com" }
        });
        var dto = Draft("e2", new DateTime(2026, 9, 1)); dto.Id = "salary-1"; dto.Version = 1;
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RejectsInvalidComponentFormulaBeforePersistence()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.Components =
        [
            new EmployeeSalaryComponentValueDto
            {
                ComponentCode = "HRA", ComponentName = "HRA", Kind = "EARNING",
                CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "MISSING", Value = 40m
            }
        ];
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_RecalculatesMonthlyGrossFromFormulaComponentsInsteadOfTrustingClientTotal()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.AnnualCtc = 1800000m;
        dto.AnnualCtcSource = "OFFER_PDF";
        dto.MonthlyGross = 999999m;
        dto.Components.Add(new EmployeeSalaryComponentValueDto
        {
            ComponentCode = "HRA", ComponentName = "Housing", Kind = "EARNING",
            CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 40m,
            IsRecurring = true
        });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.True(result.Success);
        Assert.Equal(14000m, result.MethodResult!.MonthlyGross);
        Assert.Equal("OFFER_PDF", result.MethodResult.AnnualCtcSource);
        salaries.Verify(x => x.AddOne(It.Is<EmployeeSalaryStructure>(salary => salary.MonthlyGross == 14000m && salary.AnnualCtc == 1800000m && salary.AnnualCtcSource == "OFFER_PDF")), Times.Once);
    }

    [Fact]
    public async Task SaveDraft_RejectsCompanyDisabledSalaryComponent()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var definitions = Repository(new[]
        {
            new PayrollSalaryComponentDefinition { CompanyId = "company-1", Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 },
            new PayrollSalaryComponentDefinition { CompanyId = "company-1", Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 10, 1), IsActive = false, Version = 2 }
        });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = "BONUS", ComponentName = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, Value = 1000m });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, definitions.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("not enabled", result.Message);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_DoesNotUseAnotherCompanyComponentWhenTenantHasCatalog()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var definitions = Repository(new[]
        {
            new PayrollSalaryComponentDefinition { CompanyId = "company-1", Code = "MEAL", Name = "Meal", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 },
            new PayrollSalaryComponentDefinition { CompanyId = "company-2", Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true, Version = 1 }
        });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = "BONUS", ComponentName = "Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, Value = 1000m });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, definitions.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("not enabled", result.Message);
        salaries.Verify(x => x.AddOne(It.IsAny<EmployeeSalaryStructure>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_UsesCompanyDefinitionForEligibleComponentMetadata()
    {
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var employees = Repository(new[] { new EmpUser { EmployeeId = "e1", CompanyId = "company-1", FirstName = "Employee", LastName = "One", Email = "e@example.com" } });
        var definitions = Repository(new[]
        {
            new PayrollSalaryComponentDefinition { CompanyId = "company-1", Code = "BONUS", Name = "Approved bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = false, EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 12, 31), IsActive = true, Version = 2 }
        });
        var dto = Draft("e1", new DateTime(2026, 10, 1));
        dto.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = " bonus ", ComponentName = "Untrusted imported label", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = false, Value = 1000m });
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object, definitions.Object);

        var result = await service.SaveDraft("company-1", "actor", dto);

        Assert.True(result.Success);
        var savedBonus = Assert.Single(result.MethodResult!.Components, component => component.ComponentCode == "BONUS");
        Assert.Equal("Approved bonus", savedBonus.ComponentName);
        Assert.False(savedBonus.IsTaxable);
    }

    private static Mock<IMongoClient> SalaryTransaction(Mock<IMongoDbRepository<EmployeeSalaryStructure>> repository, params long[] matches)
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(x => x.WithTransactionAsync(It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(), It.IsAny<TransactionOptions>(), It.IsAny<CancellationToken>()))
            .Returns((Func<IClientSessionHandle, CancellationToken, Task<bool>> callback, TransactionOptions _, CancellationToken token) => callback(session.Object, token));
        client.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(session.Object);
        var collection = new Mock<IMongoCollection<EmployeeSalaryStructure>>();
        repository.Setup(x => x.GetCollection()).Returns(collection.Object);
        var sequence = collection.SetupSequence(x => x.ReplaceOneAsync(session.Object, It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), It.IsAny<EmployeeSalaryStructure>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()));
        foreach (var count in matches)
            sequence.ReturnsAsync(new ReplaceOneResult.Acknowledged(count, count, null));
        return client;
    }

    private static EmployeeSalaryStructureDto Draft(string employeeId, DateTime from) => new() { EmployeeId = employeeId, EffectiveFrom = from, Currency = "INR", PaymentFrequency = "MONTHLY", Components = [new EmployeeSalaryComponentValueDto { ComponentCode = "BASIC", ComponentName = "Basic", Value = 10000m }] };

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
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true, StatusCode = 201 });
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}

