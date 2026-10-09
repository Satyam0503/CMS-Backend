using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryImportServiceTests
{
    [Fact]
    public async Task Submit_RejectsAggregateDeductionBeforeSavingAnySalary()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object);
        var row = Row(2, "e1");
        row.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = " total_deductions ", ComponentName = "Total", Kind = "DEDUCTION", Value = 500 });
        var preview = await service.Preview("company-1", new[] { row });
        Assert.False(preview[0].IsValid);
        Assert.Contains(preview[0].Errors, error => error.Contains("aggregate"));
        var submitted = await service.Submit("company-1", "actor", new[] { row });
        Assert.False(submitted.Success);
        salaryService.Verify(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()), Times.Never);
    }

    [Fact]
    public async Task Preview_RejectsUnknownTenantEmployeeAndDuplicateRows()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var result = await service.Preview("company-1", new[] { Row(2, "e1"), Row(3, "e1"), Row(4, "e2") });
        Assert.True(result[0].IsValid);
        Assert.Contains("appears more than once", result[1].Errors[0]);
        Assert.Contains("does not belong", result[2].Errors[0]);
    }

    [Fact]
    public async Task Preview_RejectsInvalidDatesNegativeValuesAndEmptyComponents()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var row = Row(2, "e1"); row.EffectiveFrom = new DateTime(2026, 10, 2); row.EffectiveTo = new DateTime(2026, 10, 1); row.AnnualCtc = -1; row.Components = [];
        var result = await service.Preview("company-1", new[] { row });
        Assert.False(result[0].IsValid);
        Assert.Contains(result[0].Errors, x => x.Contains("EffectiveTo"));
        Assert.Contains(result[0].Errors, x => x.Contains("AnnualCtc"));
        Assert.Contains(result[0].Errors, x => x.Contains("component"));
    }

    [Fact]
    public async Task Preview_RejectsUnsupportedPaymentFrequency()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var row = Row(2, "e1");
        row.PaymentFrequency = "YEARLY";

        var result = await service.Preview("company-1", new[] { row });

        Assert.False(result[0].IsValid);
        Assert.Contains(result[0].Errors, x => x.Contains("monthly payroll", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("MONTH")]
    public async Task Submit_NormalizesMissingOrAbbreviatedMonthlyFrequency(string frequency)
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        salaryService.Setup(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result<EmployeeSalaryStructureDto> { Success = true });
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object);
        var row = Row(2, "e1");
        row.PaymentFrequency = frequency;
        row.AnnualCtcSource = "SPREADSHEET";

        var result = await service.Submit("company-1", "actor", new[] { row });

        Assert.True(result.Success);
        salaryService.Verify(x => x.SaveDraft("company-1", "actor", It.Is<EmployeeSalaryStructureDto>(draft => draft.PaymentFrequency == "MONTHLY" && draft.AnnualCtcSource == "SPREADSHEET")), Times.Once);
    }

    [Fact]
    public async Task Preview_RejectsDuplicateComponentCodesIgnoringCaseAndWhitespace()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var row = Row(2, "e1");
        row.Components.Add(new EmployeeSalaryComponentValueDto { ComponentCode = " basic ", ComponentName = "Second basic", Kind = "EARNING", CalculationType = "FIXED", Value = 5000m });

        var result = await service.Preview("company-1", new[] { row });

        Assert.False(result[0].IsValid);
        Assert.Contains(result[0].Errors, x => x.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_RejectsFormulaWithMissingBaseBeforeImportSubmission()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var row = Row(2, "e1");
        row.Components.Add(new EmployeeSalaryComponentValueDto
        {
            ComponentCode = "HRA", ComponentName = "House Rent Allowance", Kind = "EARNING",
            CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "MISSING", Value = 40m
        });

        var result = await service.Preview("company-1", new[] { row });

        Assert.False(result[0].IsValid);
        Assert.Contains(result[0].Errors, x => x.Contains("references missing component", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_RejectsOverlapWithApprovedSalary()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(new[] { new EmployeeSalaryStructure { CompanyId = "company-1", EmployeeId = "e1", ApprovalStatus = "APPROVED", EffectiveFrom = new DateTime(2026, 1, 1) } });
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, Mock.Of<IEmployeeSalaryStructureService>());
        var result = await service.Preview("company-1", new[] { Row(2, "e1") });
        Assert.False(result[0].IsValid);
        Assert.Contains(result[0].Errors, x => x.Contains("overlap"));
    }

    [Fact]
    public async Task Submit_SameBatchSkipsExistingDraftForIdempotency()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(new[] { new EmployeeSalaryStructure { CompanyId = "company-1", EmployeeId = "e1", Source = "IMPORT:batch-1", ApprovalStatus = "DRAFT", EffectiveFrom = new DateTime(2026, 10, 1) } });
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object);

        var result = await service.Submit("company-1", "actor", new[] { Row(2, "e1") }, "batch-1");

        Assert.True(result.Success);
        salaryService.Verify(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()), Times.Never);
    }
    [Fact]
    public async Task Submit_PersistsBatchLifecycleAndMarksCompletion()
    {
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var batches = new Mock<IMongoDbRepository<EmployeeSalaryImportBatch>>();
        batches.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeSalaryImportBatch, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((EmployeeSalaryImportBatch?)null);
        batches.Setup(x => x.AddOne(It.IsAny<EmployeeSalaryImportBatch>()))
            .Callback<EmployeeSalaryImportBatch>(x => x.Id = "batch-doc")
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        batches.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryImportBatch>>(), It.IsAny<EmployeeSalaryImportBatch>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        salaryService.Setup(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result<EmployeeSalaryStructureDto> { Success = true });
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object, batches.Object);

        var result = await service.Submit("company-1", "actor", new[] { Row(2, "e1") }, "batch-lifecycle");

        Assert.True(result.Success);
        batches.Verify(x => x.AddOne(It.Is<EmployeeSalaryImportBatch>(b => b.CompanyId == "company-1" && b.BatchId == "batch-lifecycle")), Times.Once);
        batches.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeSalaryImportBatch>>(), It.Is<EmployeeSalaryImportBatch>(b => b.Status == "COMPLETED" && b.ProcessedRows == 1)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Submit_CompletedBatchRetryReturnsBeforeValidatingChangedLiveSalaryState()
    {
        var row = Row(2, "e1");
        var fingerprint = EmployeeSalaryImportService.CreateRequestFingerprint(new[] { row });
        var employees = Repository(Array.Empty<EmpUser>());
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var batches = Repository(new[]
        {
            new EmployeeSalaryImportBatch
            {
                CompanyId = "company-1", BatchId = "batch-1", Status = "COMPLETED", RequestFingerprint = fingerprint,
                TotalRows = 1, ProcessedRows = 1
            }
        });
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object, batches.Object);

        var result = await service.Submit("company-1", "actor", new[] { row }, "batch-1");

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        employees.Verify(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        salaryService.Verify(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()), Times.Never);
    }

    [Fact]
    public async Task Submit_RejectsReuseOfBatchIdWithDifferentSalaryPayload()
    {
        var originalRow = Row(2, "e1");
        var fingerprint = EmployeeSalaryImportService.CreateRequestFingerprint(new[] { originalRow });
        var employees = Repository(new[] { new EmpUser { CompanyId = "company-1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.com" } });
        var salaries = Repository(Array.Empty<EmployeeSalaryStructure>());
        var batches = Repository(new[]
        {
            new EmployeeSalaryImportBatch
            {
                CompanyId = "company-1", BatchId = "batch-1", Status = "PARTIAL_FAILURE", RequestFingerprint = fingerprint,
                TotalRows = 1
            }
        });
        var salaryService = new Mock<IEmployeeSalaryStructureService>();
        var service = new EmployeeSalaryImportService(employees.Object, salaries.Object, salaryService.Object, batches.Object);
        var changedRow = Row(2, "e1");
        changedRow.Components[0].Value = 90000m;

        var result = await service.Submit("company-1", "actor", new[] { changedRow }, "batch-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("different salary payload", result.Message, StringComparison.OrdinalIgnoreCase);
        employees.Verify(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        salaryService.Verify(x => x.SaveDraft(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmployeeSalaryStructureDto>()), Times.Never);
    }

    private static EmployeeSalaryImportRowDto Row(int number, string employeeId) => new() { RowNumber = number, EmployeeId = employeeId, EffectiveFrom = new DateTime(2026, 10, 1), Components = [new EmployeeSalaryComponentValueDto { ComponentCode = "BASIC", ComponentName = "Basic", Value = 10000m }] };
    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(IEnumerable<TEntity> items)
    {
        var source = items.ToArray();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>())).Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(source.Any(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(source.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(source.FirstOrDefault(predicate.Compile())));
        return repository;
    }
}
