using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunWorkflowServiceTests
{
    [Theory]
    [InlineData("DRAFT")]
    [InlineData("BLOCKED")]
    public async Task Cancel_RetainsAuditReasonAndUsesOptimisticRunVersion(string startingStatus)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = startingStatus, Version = 4
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        runs.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()))
            .ReturnsAsync(new Result { Success = true });
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "  Formula cannot be repaired  ");

        Assert.True(result.Success);
        Assert.Equal("CANCELLED", run.Status);
        Assert.False(run.IsActive);
        Assert.Equal(5, run.Version);
        var audit = Assert.Single(run.AuditHistory);
        Assert.Equal("RunCancelled", audit.Action);
        Assert.Equal("payroll-admin", audit.ActorId);
        Assert.Equal("Formula cannot be repaired", audit.Notes);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    [Theory]
    [InlineData("CALCULATED")]
    [InlineData("APPROVED")]
    [InlineData("PROCESSED")]
    public async Task Cancel_RejectsRunOnceItHasBeenCalculated(string status)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = status, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "Do not cancel this run");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(status, run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_RequiresAUsableReason()
    {
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "   ");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        runs.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_DoesNotRevealAnotherCompanyRun()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5), Status = "DRAFT"
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollRun, bool>> predicate, bool _, bool _) =>
                Task.FromResult(predicate.Compile()(run) ? run : null));
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-2", "run-1", "payroll-admin", "Bad source setup");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Validate_RejectsDraftWithoutCalculatedPayrollRows()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "DRAFT", EmployeeCount = 1, ReadyCount = 1, Version = 1
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new PayrollRunEmployee { CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "READY" } });

        var result = await new PayrollRunWorkflowService(runs.Object, employees.Object).Validate("company-1", "run-1", "validator");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("DRAFT", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsRunWhenStoredTotalsDoNotReconcileWithEmployeeRows()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "CALCULATED", CreatedBy = "maker", ReviewedBy = "reviewer",
            ReviewedAtUtc = DateTime.UtcNow, EmployeeCount = 1, ReadyCount = 1,
            GrossTotal = 1000m, DeductionTotal = 100m, NetTotal = 900m, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[]
            {
                new PayrollRunEmployee
                {
                    CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
                    Status = "CALCULATED", GrossAmount = 900m, DeductionAmount = 100m, NetAmount = 800m
                }
            });

        var result = await new PayrollRunWorkflowService(runs.Object, employees.Object).Approve("company-1", "run-1", "approver");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("CALCULATED", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsRunCreatorEvenAfterReview()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "CALCULATED", CreatedBy = "maker", ReviewedBy = "reviewer",
            ReviewedAtUtc = DateTime.UtcNow, EmployeeCount = 1, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<PayrollRunEmployee, bool>> _, bool _, bool _) => new[]
            {
                new PayrollRunEmployee { CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "CALCULATED" }
            });
        var service = new PayrollRunWorkflowService(runs.Object, employees.Object);

        var result = await service.Approve("company-1", "run-1", "maker");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("CALCULATED", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Approve_FailsWhenSkippedRowLacksExclusionReason()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 5), Status = "CALCULATED", EmployeeCount = 2, ReadyCount = 1, SkippedCount = 1,
            GrossTotal = 1000m, NetTotal = 1000m, ReviewedBy = "reviewer", ReviewedAtUtc = DateTime.UtcNow, CreatedBy = "creator"
        };
        var empList = new[]
        {
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 1000m, NetAmount = 1000m },
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-2", Status = "SKIPPED", ExclusionReason = null }
        };
        var service = new PayrollRunWorkflowService(CreateRepo(run).Object, CreateRepo(empList).Object);

        var result = await service.Approve("c", "run-1", "approver");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("without an explicit exclusion reason", result.Message);
    }

    [Fact]
    public async Task Approve_SucceedsWhenSkippedRowHasExclusionReason()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 5), Status = "CALCULATED", EmployeeCount = 2, ReadyCount = 1, SkippedCount = 1,
            GrossTotal = 1000m, NetTotal = 1000m, ReviewedBy = "reviewer", ReviewedAtUtc = DateTime.UtcNow, CreatedBy = "creator"
        };
        var empList = new[]
        {
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 1000m, NetAmount = 1000m },
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-2", Status = "SKIPPED", ExclusionReason = "Salary revision pending board approval" }
        };
        var runs = CreateRepo(run);
        var service = new PayrollRunWorkflowService(runs.Object, CreateRepo(empList).Object);

        var result = await service.Approve("c", "run-1", "approver");

        Assert.True(result.Success);
        Assert.Equal("APPROVED", run.Status);
        Assert.Contains(run.AuditHistory, a => a.Action == "RunAPPROVED" && a.Notes!.Contains("1 authorized excluded employee(s)"));
    }

    [Fact]
    public async Task ExcludeEmployee_SetsSkippedAndExclusionReason_AndAudits()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 5), Status = "BLOCKED", EmployeeCount = 2, ReadyCount = 1, BlockedCount = 1, SkippedCount = 0
        };
        var emp1 = new PayrollRunEmployee { Id = "l1", CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 1000m, NetAmount = 1000m };
        var emp2 = new PayrollRunEmployee { Id = "l2", CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-2", Status = "BLOCKED", ErrorCode = "NO_SALARY", ErrorMessage = "Missing salary" };
        var runs = CreateRepo(run);
        var emps = CreateRepo(emp1, emp2);
        var service = new PayrollRunWorkflowService(runs.Object, emps.Object);

        var result = await service.ExcludeEmployee("c", "run-1", "emp-2", "hr-admin", "On sabbatical leave without pay");

        Assert.True(result.Success);
        Assert.Equal("SKIPPED", emp2.Status);
        Assert.Equal("On sabbatical leave without pay", emp2.ExclusionReason);
        Assert.Null(emp2.ErrorCode);
        Assert.Equal(1, run.SkippedCount);
        Assert.Equal(0, run.BlockedCount);
        Assert.Equal("DRAFT", run.Status);
        Assert.Contains(run.AuditHistory, a => a.Action == "EmployeeExcluded" && a.Notes!.Contains("emp-2"));
    }

    [Fact]
    public async Task IncludeEmployee_RevertsSkippedToReady_AndAudits()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 5), Status = "DRAFT", EmployeeCount = 2, ReadyCount = 1, SkippedCount = 1
        };
        var emp1 = new PayrollRunEmployee { Id = "l1", CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "CALCULATED" };
        var emp2 = new PayrollRunEmployee { Id = "l2", CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-2", Status = "SKIPPED", ExclusionReason = "Sabbatical" };
        var runs = CreateRepo(run);
        var emps = CreateRepo(emp1, emp2);
        var service = new PayrollRunWorkflowService(runs.Object, emps.Object);

        var result = await service.IncludeEmployee("c", "run-1", "emp-2", "hr-admin");

        Assert.True(result.Success);
        Assert.Equal("READY", emp2.Status);
        Assert.Null(emp2.ExclusionReason);
        Assert.Equal(0, run.SkippedCount);
        Assert.Contains(run.AuditHistory, a => a.Action == "EmployeeIncluded" && a.Notes!.Contains("emp-2"));
    }

    [Fact]
    public async Task ExcludeEmployee_RejectsShortReason()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "c", PeriodStart = DateTime.Today, PeriodEnd = DateTime.Today, PayDate = DateTime.Today, Status = "DRAFT" };
        var emp = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "READY" };
        var service = new PayrollRunWorkflowService(CreateRepo(run).Object, CreateRepo(emp).Object);

        var result = await service.ExcludeEmployee("c", "run-1", "emp-1", "hr-admin", "no");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ExcludeEmployee_RejectsCalculatedOrApprovedRun()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "c", PeriodStart = DateTime.Today, PeriodEnd = DateTime.Today, PayDate = DateTime.Today, Status = "CALCULATED" };
        var emp = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "run-1", EmployeeId = "emp-1", Status = "CALCULATED" };
        var service = new PayrollRunWorkflowService(CreateRepo(run).Object, CreateRepo(emp).Object);

        var result = await service.ExcludeEmployee("c", "run-1", "emp-1", "hr-admin", "Valid exclusion reason");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("draft or blocked", result.Message);
    }

[Fact]
    public async Task Reopen_ReopensClosedRunToPaidWithAuditReason()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "c", PeriodStart = DateTime.Today, PeriodEnd = DateTime.Today,
            PayDate = DateTime.Today, Status = "CLOSED", Version = 5
        };
        var runs = CreateRepo(run);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Reopen("c", "run-1", "admin-1", "Reopening to reconcile payment variance");

        Assert.True(result.Success);
        Assert.Equal("PAID", run.Status);
        Assert.Equal(6, run.Version);
        var audit = Assert.Single(run.AuditHistory, a => a.Action == "RunReopened");
        Assert.Equal("admin-1", audit.ActorId);
        Assert.Contains("Reopening to reconcile payment variance", audit.Notes);
    }

    [Fact]
    public async Task Reopen_ReopensApprovedRunToDraft()
    {
        var run = new PayrollRun
        {
            Id = "run-2", CompanyId = "c", PeriodStart = DateTime.Today, PeriodEnd = DateTime.Today,
            PayDate = DateTime.Today, Status = "APPROVED", Version = 2
        };
        var runs = CreateRepo(run);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Reopen("c", "run-2", "admin-1", "Need to recalculate due to late attendance sync");

        Assert.True(result.Success);
        Assert.Equal("DRAFT", run.Status);
        Assert.Equal(3, run.Version);
    }

    [Theory]
    [InlineData("CALCULATED")]
    [InlineData("PROCESSED")]
    public async Task Reopen_RejectsReopeningProcessedOrCalculatedRun(string status)
    {
        var run = new PayrollRun
        {
            Id = "run-3", CompanyId = "c", PeriodStart = DateTime.Today, PeriodEnd = DateTime.Today,
            PayDate = DateTime.Today, Status = status, Version = 2
        };
        var runs = CreateRepo(run);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Reopen("c", "run-3", "admin-1", "Invalid attempt");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task CreateCorrectionRun_CreatesDraftCorrectionRun_ForPaidRun()
    {
        var original = new PayrollRun
        {
            Id = "run-paid", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "PAID", RunType = "REGULAR"
        };
        var emp1 = new PayrollRunEmployee { Id = "e1", CompanyId = "c", PayrollRunId = "run-paid", EmployeeId = "emp-1", Status = "CALCULATED" };
        var emp2 = new PayrollRunEmployee { Id = "e2", CompanyId = "c", PayrollRunId = "run-paid", EmployeeId = "emp-2", Status = "SKIPPED", ExclusionReason = "On sabbatical" };
        var adjustment = new PayrollAdjustment
        {
            Id = "adj-1", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1",
            TargetPeriod = original.PeriodStart, Status = "APPROVED", Amount = 500m, Reason = "Approved arrears",
            ComponentKind = "EARNING", IsTaxable = false
        };
        var runs = CreateRepo(original);
        var employees = CreateRepo(emp1, emp2);
        var adjustmentRepository = CreateRepo(adjustment);
        PayrollRun? addedRun = null;
        var addedMembers = new List<PayrollRunEmployee>();
        runs.Setup(x => x.AddOne(It.IsAny<PayrollRun>())).Callback<PayrollRun>(r => { r.Id = "correction-created"; addedRun = r; }).ReturnsAsync(new Result { Success = true });
        employees.Setup(x => x.AddOne(It.IsAny<PayrollRunEmployee>())).Callback<PayrollRunEmployee>(m => addedMembers.Add(m)).ReturnsAsync(new Result { Success = true });
        var service = new PayrollRunWorkflowService(runs.Object, employees.Object, adjustments: adjustmentRepository.Object);

        var result = await service.CreateCorrectionRun("c", "run-paid", "admin-1", "Retroactive incentive adjustment");

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(addedRun);
        Assert.Equal("CORRECTION", addedRun!.RunType);
        Assert.Equal("run-paid", addedRun.ParentRunId);
        Assert.Equal("DRAFT", addedRun.Status);
        Assert.Equal(1, addedRun.EmployeeCount);
        Assert.Equal(1, addedRun.ReadyCount);
        Assert.Equal(0, addedRun.SkippedCount);
        Assert.Single(addedMembers);
        Assert.Contains(addedMembers, m => m.EmployeeId == "emp-1" && m.Status == "READY");
        Assert.Equal("correction-created", adjustment.CorrectionPayrollRunId);
    }

    [Fact]
    public async Task CreateCorrectionRun_FreezesOnlyApprovedUnassignedParentAdjustments()
    {
        var original = new PayrollRun
        {
            Id = "run-paid", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5), Status = "PAID"
        };
        var member = new PayrollRunEmployee { Id = "e1", CompanyId = "c", PayrollRunId = "run-paid", EmployeeId = "emp-1", Status = "CALCULATED" };
        var eligible = new PayrollAdjustment
        {
            Id = "adj-1", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1", TargetPeriod = original.PeriodStart,
            Reason = "Approved reimbursement correction",
            Status = "APPROVED", ComponentCode = "REIMBURSEMENT", ComponentName = "Reimbursement", ComponentKind = "EARNING",
            Amount = 500m, IsTaxable = false, IsStatutory = false, IsEmployerContribution = false,
            IncludeInEsiWage = true, ComponentVersion = 2
        };
        var otherEmployee = new PayrollAdjustment { Id = "adj-2", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-2", TargetPeriod = new DateTime(2026, 10, 1), Status = "APPROVED", Reason = "other month" };
        var consumed = new PayrollAdjustment { Id = "adj-3", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1", TargetPeriod = original.PeriodStart, Status = "APPROVED", CorrectionPayrollRunId = "old-correction", Reason = "already linked" };
        var wrongStatus = new PayrollAdjustment { Id = "adj-4", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1", TargetPeriod = original.PeriodStart, Status = "DRAFT", Reason = "not approved" };
        var runs = CreateRepo(original);
        var employees = CreateRepo(member);
        var adjustmentRepository = CreateRepo(eligible, otherEmployee, consumed, wrongStatus);
        PayrollRun? addedRun = null;
        var addedMembers = new List<PayrollRunEmployee>();
        runs.Setup(x => x.AddOne(It.IsAny<PayrollRun>())).Callback<PayrollRun>(r => { r.Id = "run-correction"; addedRun = r; }).ReturnsAsync(new Result { Success = true });
        employees.Setup(x => x.AddOne(It.IsAny<PayrollRunEmployee>())).Callback<PayrollRunEmployee>(m => addedMembers.Add(m)).ReturnsAsync(new Result { Success = true });
        var service = new PayrollRunWorkflowService(runs.Object, employees.Object, adjustments: adjustmentRepository.Object);

        var result = await service.CreateCorrectionRun("c", "run-paid", "admin", "Correct prior payroll");

        Assert.True(result.Success, result.Message);
        var correctionEmployee = Assert.Single(addedMembers);
        var frozenAdjustment = Assert.Single(correctionEmployee.InputSnapshot.Adjustments);
        Assert.Equal("adj-1", frozenAdjustment.Id);
        Assert.Equal("REIMBURSEMENT", frozenAdjustment.ComponentCode);
        Assert.Equal(500m, frozenAdjustment.Amount);
        Assert.True(frozenAdjustment.IncludeInEsiWage);
        Assert.Equal("run-correction", eligible.CorrectionPayrollRunId);
    }

    [Fact]
    public async Task CreateCorrectionRun_WhenEmployeeSnapshotWriteThrows_CompensatesReservationsAndCancelsDraft()
    {
        var original = new PayrollRun
        {
            Id = "run-paid", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5), Status = "PAID"
        };
        var member = new PayrollRunEmployee
        {
            Id = "employee-line", CompanyId = "c", PayrollRunId = "run-paid", EmployeeId = "emp-1", Status = "CALCULATED"
        };
        var adjustment = new PayrollAdjustment
        {
            Id = "adj-1", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1",
            TargetPeriod = original.PeriodStart, Status = "APPROVED", Amount = 500m, Reason = "Approved arrears",
            ComponentKind = "EARNING", IsTaxable = false, IsStatutory = false, IsEmployerContribution = false
        };
        var runs = CreateRepo(original);
        runs.Setup(x => x.AddOne(It.IsAny<PayrollRun>()))
            .Callback<PayrollRun>(run => run.Id = "correction-created")
            .ReturnsAsync(new Result { Success = true });
        var employees = CreateRepo(member);
        employees.Setup(x => x.AddOne(It.IsAny<PayrollRunEmployee>()))
            .ThrowsAsync(new InvalidOperationException("simulated persistence failure"));
        var adjustmentRepository = CreateRepo(adjustment);
        var service = new PayrollRunWorkflowService(runs.Object, employees.Object, adjustments: adjustmentRepository.Object);

        var result = await service.CreateCorrectionRun("c", "run-paid", "admin", "Correct prior payroll");

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Contains("draft was cancelled", result.Message);
        Assert.Equal("CANCELLED", runs.Invocations
            .Where(call => call.Method.Name == nameof(IMongoDbRepository<PayrollRun>.AddOne))
            .Select(call => call.Arguments[0]).Cast<PayrollRun>().Single().Status);
        Assert.Null(adjustment.CorrectionPayrollRunId);
        Assert.Contains(adjustment.AuditHistory, entry => entry.Action == "CorrectionRunReservationReleased");
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Once);
        adjustmentRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollAdjustment>>(), adjustment), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateCorrectionRun_RejectsWhenThereAreNoApprovedAdjustments()
    {
        var original = new PayrollRun
        {
            Id = "run-paid", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5), Status = "PAID"
        };
        var runs = CreateRepo(original);
        var service = new PayrollRunWorkflowService(runs.Object, CreateRepo<PayrollRunEmployee>().Object,
            adjustments: CreateRepo<PayrollAdjustment>().Object);

        var result = await service.CreateCorrectionRun("c", "run-paid", "admin", "Correction without requests");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("No approved", result.Message);
        runs.Verify(x => x.AddOne(It.IsAny<PayrollRun>()), Times.Never);
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("CALCULATED")]
    [InlineData("APPROVED")]
    public async Task Cancel_CorrectionRunReleasesApprovedAdjustmentReservation(string status)
    {
        var correction = new PayrollRun
        {
            Id = "correction-1", CompanyId = "c", ParentRunId = "run-paid", RunType = "CORRECTION",
            Status = status, IsActive = true, PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 10)
        };
        var adjustment = new PayrollAdjustment
        {
            Id = "adj-1", CompanyId = "c", OriginalPayrollRunId = "run-paid", EmployeeId = "emp-1",
            CorrectionPayrollRunId = "correction-1", Status = "APPROVED", Amount = 500m, Reason = "approved arrears"
        };
        var adjustmentRepository = CreateRepo(adjustment);
        var service = new PayrollRunWorkflowService(CreateRepo(correction).Object, CreateRepo<PayrollRunEmployee>().Object,
            adjustments: adjustmentRepository.Object);

        var result = await service.Cancel("c", correction.Id, "admin", "No longer needed");

        Assert.True(result.Success, result.Message);
        Assert.Equal("CANCELLED", correction.Status);
        Assert.False(correction.IsActive);
        Assert.Null(adjustment.CorrectionPayrollRunId);
        Assert.Contains(adjustment.AuditHistory, e => e.Action == "CorrectionRunReservationReleased");
    }

    [Fact]
    public async Task Process_SynchronizesApprovedAdjustmentStatusAfterCorrectionRunIsFinalized()
    {
        var run = new PayrollRun
        {
            Id = "correction-1", CompanyId = "c", ParentRunId = "original-1", RunType = "CORRECTION",
            Status = "PROCESSED", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 10), LegacyPayrollRowsSynchronizedAtUtc = DateTime.UtcNow
        };
        var adjustment = new PayrollAdjustment
        {
            Id = "adj-1", CompanyId = "c", OriginalPayrollRunId = "original-1", EmployeeId = "emp-1",
            Status = "APPROVED", Amount = 500m, Reason = "approved correction", CorrectionPayrollRunId = "correction-1"
        };
        var line = new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = run.Id, EmployeeId = "emp-1", Status = "CALCULATED",
            AppliedAdjustmentIds = ["adj-1"]
        };
        var adjustmentRepository = CreateRepo(adjustment);
        var service = new PayrollRunWorkflowService(CreateRepo(run).Object, CreateRepo(line).Object,
            adjustments: adjustmentRepository.Object);

        var result = await service.Process("c", run.Id, "processor");

        Assert.True(result.Success, result.Message);
        Assert.Equal("PROCESSED", adjustment.Status);
        Assert.Equal(run.Id, adjustment.CorrectionPayrollRunId);
        Assert.Equal("processor", adjustment.ProcessedBy);
        Assert.Contains(adjustment.AuditHistory, item => item.Action == "Processed" && item.Notes == $"Included in correction payroll run {run.Id}");
        adjustmentRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollAdjustment>>(), adjustment), Times.Once);
    }

    [Fact]
    public async Task CreateCorrectionRun_RejectsWhenActiveCorrectionAlreadyExists()
    {
        var original = new PayrollRun
        {
            Id = "run-paid", CompanyId = "c", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "PAID", RunType = "REGULAR"
        };
        var activeCorrection = new PayrollRun
        {
            Id = "run-correction-1", CompanyId = "c", ParentRunId = "run-paid",
            Status = "DRAFT", IsActive = true, PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 10)
        };
        var runs = CreateRepo(original, activeCorrection);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.CreateCorrectionRun("c", "run-paid", "admin-1", "Another correction");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("already exists", result.Message);
    }

    private static Mock<IMongoDbRepository<TEntity>> CreateRepo<TEntity>(params TEntity[] items) where TEntity : class
    {
        var m = new Mock<IMongoDbRepository<TEntity>>();
        m.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool _) => items.FirstOrDefault(p.Compile()));
        m.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool _) => items.Where(p.Compile()).ToArray());
        m.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return m;
    }
}
