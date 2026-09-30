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
        var service = new EmployeeSalaryStructureService(salaries.Object, employees.Object);

        var result = await service.Approve("company-1", "approver", "new", 1);

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2026, 9, 30), prior.EffectiveTo);
        Assert.Equal("APPROVED", next.ApprovalStatus);
        salaries.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryStructure>>(), prior), Times.Once);
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

