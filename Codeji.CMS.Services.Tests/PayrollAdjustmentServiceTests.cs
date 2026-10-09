using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollAdjustmentServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsOnlyTenantAdjustmentsNewestFirst()
    {
        var records = new[]
        {
            Adjustment("old", "tenant-a", new DateTime(2026, 9, 1)),
            Adjustment("new", "tenant-a", new DateTime(2026, 9, 2)),
            Adjustment("foreign", "tenant-b", new DateTime(2026, 9, 3))
        };
        var service = new PayrollAdjustmentService(Repo(records).Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.GetAll("tenant-a");

        Assert.True(result.Success);
        Assert.Equal(new[] { "new", "old" }, result.MethodResult!.Select(x => x.Id));
    }

    [Fact]
    public async Task Create_RejectsDraftOriginalRun()
    {
        var run = Run("r", "c", "DRAFT");
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object, Repo(Component("c")).Object);

        var result = await service.Create("c", "a", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "r", EmployeeId = "e", ComponentCode = "BONUS", TargetPeriod = new DateTime(2026, 9, 1), Amount = 1, Reason = "late input"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Create_RequiresEmployeeInOriginalRun()
    {
        var run = Run("r", "c", "PAID");
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object, Repo(Component("c")).Object);

        var result = await service.Create("c", "a", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "r", EmployeeId = "e", ComponentCode = "BONUS", TargetPeriod = new DateTime(2026, 9, 1), Amount = 1, Reason = "late input", Type = "ARREARS"
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetComponents_ReturnsOnlyTenantComponentEffectiveOnRequestedDate()
    {
        var definitions = Repo(
            Component("company-1", effectiveFrom: new DateTime(2026, 1, 1)),
            Component("company-1", name: "Bonus v2", version: 2, effectiveFrom: new DateTime(2026, 10, 1)),
            Component("company-2", code: "FOREIGN", name: "Foreign", effectiveFrom: new DateTime(2026, 1, 1)));
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo<PayrollRun>().Object,
            Repo<PayrollRunEmployee>().Object, definitions.Object);

        var result = await service.GetComponents("company-1", new DateTime(2026, 10, 1));

        Assert.True(result.Success);
        var component = Assert.Single(result.MethodResult!);
        Assert.Equal("Bonus v2", component.Name);
        Assert.Equal(2, component.Version);
    }

    [Fact]
    public async Task Create_SnapshotsEffectiveComponentMetadataAndTargetMonth()
    {
        var run = Run("r", "c", "PAID");
        var employee = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "e" };
        var adjustmentRepository = Repo<PayrollAdjustment>();
        PayrollAdjustment? saved = null;
        adjustmentRepository.Setup(x => x.AddOne(It.IsAny<PayrollAdjustment>()))
            .Callback<PayrollAdjustment>(value => saved = value).ReturnsAsync(new Result { Success = true });
        var service = new PayrollAdjustmentService(adjustmentRepository.Object, Repo(run).Object,
            Repo(employee).Object, Repo(Component("c", name: "Bonus v3", taxable: true, version: 3,
                effectiveFrom: new DateTime(2026, 9, 1))).Object);

        var result = await service.Create("c", "actor", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "r", EmployeeId = "e", ComponentCode = "BONUS",
            TargetPeriod = new DateTime(2026, 10, 1), Type = "ARREARS", Amount = 125m, Reason = "approved arrears"
        });

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("BONUS", saved!.ComponentCode);
        Assert.Equal("Bonus v3", saved.ComponentName);
        Assert.Equal("EARNING", saved.ComponentKind);
        Assert.True(saved.IsTaxable);
        Assert.Equal(3, saved.ComponentVersion);
        Assert.Equal(new DateTime(2026, 10, 1), saved.TargetPeriod);
    }

    [Fact]
    public async Task Create_RejectsComponentNotEffectiveForTargetMonth()
    {
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo<PayrollRun>().Object,
            Repo<PayrollRunEmployee>().Object, Repo(Component("c", effectiveFrom: new DateTime(2026, 10, 1))).Object);

        var result = await service.Create("c", "actor", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "run", EmployeeId = "employee", ComponentCode = "BONUS",
            TargetPeriod = new DateTime(2026, 9, 1), Amount = 100m, Reason = "correction"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Approve_RejectsSelfApproval()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", CreatedBy = "creator-1", Status = "DRAFT", Amount = 100,
            Reason = "Backdated earning"
        };
        var adjustmentRepository = Repo(adjustment);
        var service = new PayrollAdjustmentService(adjustmentRepository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Approve("company-1", "CREATOR-1", "adjustment-1");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        adjustmentRepository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()), Times.Never);
    }

    [Fact]
    public async Task Approve_AllowsDifferentApproverAndAuditsTheApproval()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", CreatedBy = "creator-1", Status = "DRAFT", Amount = 100,
            Reason = "Backdated earning"
        };
        var adjustmentRepository = Repo(adjustment);
        var service = new PayrollAdjustmentService(adjustmentRepository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Approve("company-1", "approver-1", "adjustment-1");

        Assert.True(result.Success);
        Assert.Equal("APPROVED", adjustment.Status);
        Assert.Equal("approver-1", adjustment.ApprovedBy);
        Assert.Equal("Approved", Assert.Single(adjustment.AuditHistory).Action);
    }

    [Fact]
    public async Task Approve_ReturnsConflictWhenDraftWasChangedConcurrently()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", CreatedBy = "creator-1", Status = "DRAFT", Amount = 100,
            Reason = "Backdated earning"
        };
        var repository = Repo(adjustment);
        repository.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 400 });
        var service = new PayrollAdjustmentService(repository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Approve("company-1", "approver-2", "adjustment-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("changed before approval", result.Message);
    }

    [Fact]
    public async Task Reject_RequiresReasonAndAuditsAdminDecision()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", Status = "DRAFT", Amount = 100, Reason = "Correction request"
        };
        var repository = Repo(adjustment);
        var service = new PayrollAdjustmentService(repository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var invalid = await service.Reject("company-1", "admin-1", "adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "no" });
        Assert.False(invalid.Success);
        Assert.Equal(400, invalid.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()), Times.Never);

        var result = await service.Reject("company-1", "admin-1", "adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "Duplicate request" });

        Assert.True(result.Success);
        Assert.Equal("REJECTED", adjustment.Status);
        Assert.Equal("admin-1", adjustment.RejectedBy);
        Assert.Equal("Duplicate request", adjustment.RejectionReason);
        Assert.Equal("Rejected", Assert.Single(adjustment.AuditHistory).Action);
        Assert.Equal("Duplicate request", adjustment.AuditHistory[0].Notes);
    }

    [Fact]
    public async Task Reject_RejectsNonDraftAdjustment()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", Status = "APPROVED", Amount = 100, Reason = "Correction request"
        };
        var repository = Repo(adjustment);
        var service = new PayrollAdjustmentService(repository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reject("company-1", "admin-1", "adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "No longer needed" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()), Times.Never);
    }

    [Fact]
    public async Task Reject_ReturnsConflictWhenDraftWasChangedConcurrently()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", Status = "DRAFT", Amount = 100, Reason = "Correction request"
        };
        var repository = Repo(adjustment);
        repository.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 400 });
        var service = new PayrollAdjustmentService(repository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reject("company-1", "admin-1", "adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "Duplicate request" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("changed before rejection", result.Message);
    }

    [Fact]
    public async Task Reject_DoesNotFindAnAdjustmentOwnedByAnotherTenant()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "adjustment-1", CompanyId = "company-1", OriginalPayrollRunId = "run-1",
            EmployeeId = "employee-1", Status = "DRAFT", Amount = 100, Reason = "Correction request"
        };
        var repository = Repo(adjustment);
        var service = new PayrollAdjustmentService(repository.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reject("company-2", "admin-2", "adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "Duplicate request" });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()), Times.Never);
    }

    [Fact]
    public async Task Process_RequiresCalculatedCorrectionEmployee()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "a", CompanyId = "c", OriginalPayrollRunId = "original", EmployeeId = "e",
            Status = "APPROVED", Amount = 10, Reason = "correction"
        };
        var correction = Run("correction", "c", "CALCULATED"); correction.RunType = "CORRECTION";
        var blocked = new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = "correction", EmployeeId = "e", Status = "BLOCKED"
        };
        var service = new PayrollAdjustmentService(Repo(adjustment).Object, Repo(correction).Object, Repo(blocked).Object);

        var result = await service.Process("c", "actor", "a", new PayrollAdjustmentActionDto { CorrectionPayrollRunId = "correction" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Process_FailsClosedWithoutApplyingTheAmountToCorrectionPayroll()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "a", CompanyId = "c", OriginalPayrollRunId = "original", EmployeeId = "e",
            Status = "APPROVED", Amount = 1250m, Reason = "approved arrears"
        };
        var correction = Run("correction", "c", "CALCULATED"); correction.RunType = "CORRECTION";
        var row = new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = "correction", EmployeeId = "e", Status = "CALCULATED", NetAmount = 5000m
        };
        var adjustmentRepository = Repo(adjustment);
        var service = new PayrollAdjustmentService(adjustmentRepository.Object, Repo(correction).Object, Repo(row).Object);

        var result = await service.Process("c", "actor", "a", new PayrollAdjustmentActionDto { CorrectionPayrollRunId = "correction" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("gross-to-net", result.Message);
        Assert.Equal("APPROVED", adjustment.Status);
        Assert.Null(adjustment.CorrectionPayrollRunId);
        adjustmentRepository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollAdjustment>>(), It.IsAny<PayrollAdjustment>()), Times.Never);
    }

    private static PayrollRun Run(string id, string companyId, string status) => new()
    {
        Id = id, CompanyId = companyId, PeriodStart = DateTime.Today,
        PeriodEnd = DateTime.Today, PayDate = DateTime.Today, Status = status
    };

    private static PayrollAdjustment Adjustment(string id, string companyId, DateTime createdAtUtc) => new()
    {
        Id = id, CompanyId = companyId, OriginalPayrollRunId = "run", EmployeeId = "employee",
        Amount = 10m, Reason = "test", CreatedAtUtc = createdAtUtc
    };

    private static PayrollSalaryComponentDefinition Component(string companyId, string code = "BONUS", string name = "Bonus",
        bool taxable = false, int version = 1, DateTime? effectiveFrom = null) => new()
    {
        CompanyId = companyId, Code = code, Name = name, Kind = "EARNING", IsTaxable = taxable,
        Version = version, EffectiveFrom = effectiveFrom ?? new DateTime(2026, 1, 1), IsActive = true
    };

    private static Mock<IMongoDbRepository<TEntity>> Repo<TEntity>(params TEntity[] items)
    {
        var m = new Mock<IMongoDbRepository<TEntity>>();
        m.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.FirstOrDefault(p.Compile()));
        m.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.Where(p.Compile()).ToArray());
        m.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _) => items.Any(p.Compile()));
        m.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Result { Success = true });
        m.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return m;
    }
}

