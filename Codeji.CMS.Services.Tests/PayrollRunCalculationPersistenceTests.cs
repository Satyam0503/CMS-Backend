using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunCalculationPersistenceTests
{
    [Theory]
    [InlineData("CALCULATED")]
    [InlineData("MISSING")]
    [InlineData("DUPLICATE")]
    [InlineData("NEGATIVE_NET")]
    [InlineData("POLICY_MISMATCH")]
    [InlineData("INVALID_FORMULA")]
    [InlineData("SALARY_REVISION_MISMATCH")]
    [InlineData("SALARY_COMPONENT_MISMATCH")]
    [InlineData("ASSIGNMENT_MISMATCH")]
    [InlineData("POLICY_SETTINGS_MISMATCH")]
    public async Task Calculation_DoesNotPublishRunWhenEmployeeResultCannotBeSaved(string path)
    {
        var run = Run();
        var member = Member("employee-1");
        var row = Row(member.EmployeeId);
        ConfigurePath(path, member, row);
        var runs = Repository(run);
        var members = Repository(member);
        FilterDefinition<PayrollRunEmployee>? writeFilter = null;
        members.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), It.IsAny<PayrollRunEmployee>()))
            .Callback<FilterDefinition<PayrollRunEmployee>, PayrollRunEmployee>((filter, _) => writeFilter = filter)
            .ReturnsAsync(new Result { Success = false, StatusCode = 409 });
        EmpPayRoll[] rows = path == "MISSING" ? [] : path == "DUPLICATE" ? [row, Row(row.EmployeeId, "duplicate")] : [row];
        var service = new PayrollRunCalculationService(runs.Object, members.Object, Repository(rows).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PAYROLL_CALCULATION_WRITE_FAILED", result.ErrorCode);
        Assert.Contains(member.EmployeeId, result.Message);
        Assert.Equal("DRAFT", run.Status);
        Assert.Equal(3, run.Version);
        Assert.Equal(0m, run.NetTotal);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
        Assert.NotNull(writeFilter);
        var rendered = writeFilter.Render(new RenderArgs<PayrollRunEmployee>(
            BsonSerializer.SerializerRegistry.GetSerializer<PayrollRunEmployee>(), BsonSerializer.SerializerRegistry));
        Assert.Equal(run.CompanyId, rendered["CompanyId"].AsString);
        Assert.Equal(run.Id, rendered["PayrollRunId"].AsString);
        Assert.Equal(member.Id, rendered["_id"].AsString);
    }

    [Fact]
    public async Task Calculation_BlocksAmbiguousPayrollRowsAndExcludesThemFromTotals()
    {
        var run = Run();
        var ambiguous = Member("employee-1");
        var valid = Member("employee-2");
        var foreignRow = Row("employee-2", "foreign");
        foreignRow.CompanyId = "company-2";
        foreignRow.BasicPay = 999999m;
        var historicalRow = Row("employee-2", "historical");
        historicalRow.PayMonth = run.PeriodStart.AddMonths(-1);
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(ambiguous, valid).Object,
            Repository(Row("employee-1"), Row("EMPLOYEE-1", "duplicate"), Row("employee-2"), foreignRow, historicalRow).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal("PAYROLL_ROW_DUPLICATE", ambiguous.ErrorCode);
        Assert.Equal("BLOCKED", ambiguous.Status);
        Assert.Equal(0m, ambiguous.NetAmount);
        Assert.Equal("CALCULATED", valid.Status);
        Assert.Equal(1, run.ReadyCount);
        Assert.Equal(1, run.BlockedCount);
        Assert.Equal(1000m, run.GrossTotal);
        Assert.Equal(1000m, run.NetTotal);
    }

    [Fact]
    public async Task Calculation_PreservesSkippedEmployeeAndDoesNotPayTheirAvailablePayrollRow()
    {
        var run = Run();
        var skipped = Member("employee-skipped");
        skipped.Status = "SKIPPED";
        skipped.ExclusionReason = "Employee excluded from this run.";
        var valid = Member("employee-1");
        var members = Repository(skipped, valid);
        var service = new PayrollRunCalculationService(Repository(run).Object, members.Object,
            Repository(Row(skipped.EmployeeId), Row(valid.EmployeeId)).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.True(result.Success);
        Assert.Equal("SKIPPED", skipped.Status);
        Assert.Equal("Employee excluded from this run.", skipped.ExclusionReason);
        Assert.Equal(0m, skipped.NetAmount);
        Assert.Equal("CALCULATED", run.Status);
        Assert.Equal(1, run.ReadyCount);
        Assert.Equal(1, run.SkippedCount);
        Assert.Equal(1000m, run.NetTotal);
        members.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), skipped), Times.Never);
    }

    [Fact]
    public async Task Calculation_ClearsStaleStatutoryAmountsWhenRetryBlocksEmployee()
    {
        var run = Run();
        run.Status = "BLOCKED";
        var member = Member("employee-1");
        member.StatutorySnapshot = new PayrollStatutorySnapshot { IncomeTax = 500m, EmployeeEpf = 100m, EmployerEpf = 100m };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository<EmpPayRoll>().Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_EMPLOYEE_SALARY_MISSING", member.ErrorCode);
        Assert.Contains("pay group or salary template alone is not payable", member.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0m, member.StatutorySnapshot.IncomeTax);
        Assert.Equal(0m, member.StatutorySnapshot.EmployeeEpf);
        Assert.Equal(0m, member.StatutorySnapshot.EmployerEpf);
        Assert.Equal(0m, run.NetTotal);
    }

    [Fact]
    public async Task Calculation_FreezesConfiguredEmployerStatutorySplitAndWageBases()
    {
        var run = Run();
        var member = Member("employee-1");
        member.InputSnapshot.EpfMode = "CAPPED_CEILING";
        member.InputSnapshot.EpfWageCeiling = 25000m;
        member.InputSnapshot.EpfEmployeeRatePercent = 12m;
        member.InputSnapshot.EpfEmployerRatePercent = 10m;
        member.InputSnapshot.EsicMode = "STATUTORY";
        member.InputSnapshot.EsicWageCeiling = 21000m;
        member.InputSnapshot.EsicEmployeeRatePercent = 0.75m;
        member.InputSnapshot.EsicEmployerRatePercent = 3.25m;
        member.InputSnapshot.TdsMode = "AUTO_NEW_REGIME";
        var row = Row(member.EmployeeId);
        row.BasicPay = 12000m;
        row.Deduction.LossOfPay = 500m;
        row.Deduction.LossOfPayDays = 1f;
        row.Deduction.EPF = 1392m;
        row.Deduction.ESIC = 87m;
        row.CalculationSnapshot = new PayrollCalculationSnapshot
        {
            Divisor = 30m, LossOfPayDays = 1m,
            EpfMode = "CAPPED_CEILING", EpfStatutoryRuleSetId = PayrollCalculationRules.IndiaEpfRulesFrom2026_09_17, EpfWageCeiling = 25000m,
            EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 10m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m,
            EsicWages = 22000m, EsicCoverageEligible = true,
            EsicContributionPeriodStart = new DateTime(2026, 4, 1), EsicContributionPeriodEnd = new DateTime(2026, 9, 30),
            EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "AUTO_NEW_REGIME"
        };
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.True(result.Success);
        Assert.Equal(194m, member.StatutorySnapshot.EmployerEpf);
        Assert.Equal(966m, member.StatutorySnapshot.EmployerEps);
        Assert.Equal(58m, member.StatutorySnapshot.EmployerEdli);
        Assert.Equal(11600m, member.StatutorySnapshot.EpfWages);
        Assert.Equal(11600m, member.StatutorySnapshot.EpsWages);
        Assert.Equal(11600m, member.StatutorySnapshot.EdliWages);
        Assert.Equal(22000m, member.StatutorySnapshot.EsicWages);
        Assert.True(member.StatutorySnapshot.EsicCoverageEligible);
        Assert.Equal(715m, member.StatutorySnapshot.EmployerEsic);
    }

    [Fact]
    public async Task Calculation_UsesFrozenSeptember2026EpfRuleForEmployerLiability()
    {
        var run = Run();
        var member = Member("employee-1");
        member.InputSnapshot.EpfMode = "CAPPED_CEILING";
        member.InputSnapshot.EpfWageCeiling = 25000m;
        member.InputSnapshot.EpfEmployeeRatePercent = 12m;
        member.InputSnapshot.EpfEmployerRatePercent = 12m;
        member.InputSnapshot.EsicMode = "STATUTORY";
        member.InputSnapshot.EsicWageCeiling = 21000m;
        member.InputSnapshot.EsicEmployeeRatePercent = 0.75m;
        member.InputSnapshot.EsicEmployerRatePercent = 3.25m;
        member.InputSnapshot.TdsMode = "MANUAL_ONLY";
        var row = Row(member.EmployeeId);
        row.BasicPay = 30000m;
        row.Deduction.EPF = 3000m;
        row.CalculationSnapshot = new PayrollCalculationSnapshot
        {
            PayrollMonth = new DateTime(2026, 9, 1), Divisor = 30m,
            EpfMode = "CAPPED_CEILING", EpfStatutoryRuleSetId = PayrollCalculationRules.IndiaEpfRulesFrom2026_09_17,
            EpfWageCeiling = 25000m, EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m,
            EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "MANUAL_ONLY"
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.True(result.Success, result.Message);
        Assert.Equal(3000m, member.StatutorySnapshot.EmployeeEpf);
        Assert.Equal(2083m, member.StatutorySnapshot.EmployerEps);
        Assert.Equal(917m, member.StatutorySnapshot.EmployerEpf);
        Assert.Equal(125m, member.StatutorySnapshot.EmployerEdli);
        Assert.Equal(25000m, member.StatutorySnapshot.EpfWages);
        Assert.Equal(25000m, member.StatutorySnapshot.EpsWages);
        Assert.Equal(25000m, member.StatutorySnapshot.EdliWages);
    }

    [Theory]
    [InlineData(false, 1, 1, false, "PAYROLL_ATTENDANCE_NOT_LOCKED")]
    [InlineData(true, 2, 1, false, "PAYROLL_ATTENDANCE_SNAPSHOT_MISMATCH")]
    [InlineData(true, 1, 1, true, null)]
    public async Task Calculation_EnforcesEmployeeAttendanceLockAndSourceVersion(
        bool isLocked, int currentSummaryVersion, int payrollSummaryVersion, bool expectedSuccess, string? expectedError)
    {
        var run = Run();
        run.ConfigurationSnapshot.RequireAttendanceLock = false;
        var member = Member("employee-1");
        member.InputSnapshot.RequireAttendanceLock = true;
        var row = Row(member.EmployeeId);
        row.CalculationSnapshot = new PayrollCalculationSnapshot { AttendanceSummaryVersion = payrollSummaryVersion };
        var summary = new MonthlyAttendanceSummary
        {
            Id = "attendance-1", CompanyId = run.CompanyId, EmployeeId = member.EmployeeId,
            UserId = "user-1", PayrollMonth = run.PeriodStart, IsApproved = true,
            IsLocked = isLocked, Version = currentSummaryVersion
        };
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository(row).Object, Repository(summary).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal("CALCULATED", member.Status);
            Assert.Equal(1000m, member.NetAmount);
        }
        else
        {
            Assert.Equal("BLOCKED", member.Status);
            Assert.Equal(expectedError, member.ErrorCode);
            Assert.Equal(0m, member.GrossAmount);
            Assert.Equal(0m, member.NetAmount);
            Assert.Equal("BLOCKED", run.Status);
        }
    }

    [Fact]
    public async Task Calculation_FailsClosedWhenAttendanceVerificationIsUnavailable()
    {
        var run = Run();
        var member = Member("employee-1");
        member.InputSnapshot.RequireAttendanceLock = true;
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository<EmpPayRoll>().Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.Contains("verification is unavailable", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DRAFT", run.Status);
        Assert.Equal(3, run.Version);
        Assert.Equal(0m, run.NetTotal);
        Assert.Equal("READY", member.Status);
    }

    [Fact]
    public async Task Calculation_BlocksDuplicateApprovedLockedAttendanceSummaries()
    {
        var run = Run();
        var member = Member("employee-1");
        member.InputSnapshot.RequireAttendanceLock = true;
        var summaries = new[]
        {
            AttendanceSummary(run, member, "attendance-1"),
            AttendanceSummary(run, member, "attendance-2")
        };
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository<EmpPayRoll>().Object,
            Repository(summaries).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal("PAYROLL_ATTENDANCE_SUMMARY_AMBIGUOUS", member.ErrorCode);
        Assert.Contains("Multiple approved and locked attendance summaries", member.ErrorMessage);
        Assert.Equal(0m, member.NetAmount);
    }

    [Fact]
    public async Task Calculation_UsesCompanyAttendanceRequirementForLegacyEmployeeSnapshot()
    {
        var run = Run();
        run.ConfigurationSnapshot.RequireAttendanceLock = true;
        var member = Member("employee-1");
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository(Row(member.EmployeeId)).Object,
            Repository<MonthlyAttendanceSummary>().Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_ATTENDANCE_NOT_LOCKED", member.ErrorCode);
        Assert.Equal("BLOCKED", run.Status);
    }

    [Fact]
    public async Task Calculation_AllowsEmployeeSnapshotToDisableCompanyAttendanceRequirement()
    {
        var run = Run();
        run.ConfigurationSnapshot.RequireAttendanceLock = true;
        var member = Member("employee-1");
        member.InputSnapshot.RequireAttendanceLock = false;
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository(Row(member.EmployeeId)).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "calculator");

        Assert.True(result.Success);
        Assert.Equal("CALCULATED", member.Status);
        Assert.Equal(1000m, member.NetAmount);
    }

    private static MonthlyAttendanceSummary AttendanceSummary(PayrollRun run, PayrollRunEmployee member, string id) => new()
    {
        Id = id, CompanyId = run.CompanyId, EmployeeId = member.EmployeeId,
        UserId = "user-1", PayrollMonth = run.PeriodStart, IsApproved = true,
        IsLocked = true, Version = 1
    };

    private static void ConfigurePath(string path, PayrollRunEmployee member, EmpPayRoll row)
    {
        switch (path)
        {
            case "NEGATIVE_NET": row.Deduction.IncomeTax = 2000m; break;
            case "POLICY_MISMATCH": member.InputSnapshot.PolicyProfileId = "approved-policy"; break;
            case "INVALID_FORMULA":
                member.InputSnapshot.Components = [new() { Code = "HRA", Name = "HRA", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "MISSING", Value = 40m }];
                break;
            case "SALARY_REVISION_MISMATCH":
                member.InputSnapshot.SalaryStructures = [new() { SalaryStructureId = "salary-1", Version = 1 }];
                break;
            case "SALARY_COMPONENT_MISMATCH":
                member.InputSnapshot.SalaryStructures = [new() { SalaryStructureId = "salary-1", Version = 1, Components = [new() { Code = "BASIC", Name = "Basic", Value = 1000m }] }];
                row.CalculationSnapshot = new PayrollCalculationSnapshot
                {
                    SalaryRevisions = [new() { SalaryStructureId = "salary-1", Version = 1 }],
                    SalaryStructures = [new() { SalaryStructureId = "salary-1", Version = 1, Components = [new() { Code = "BASIC", Name = "Basic", Value = 2000m }] }]
                };
                break;
            case "ASSIGNMENT_MISMATCH": member.InputSnapshot.AssignmentId = "assignment-1"; break;
            case "POLICY_SETTINGS_MISMATCH": member.InputSnapshot.EpfMode = "EXEMPT"; break;
        }
    }

    private static PayrollRun Run() => new()
    {
        Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
        PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT", Version = 3
    };

    private static PayrollRunEmployee Member(string employeeId) => new()
    {
        Id = $"line-{employeeId}", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = employeeId
    };

    private static EmpPayRoll Row(string employeeId, string id = "payroll-1") => new()
    {
        Id = id, CompanyId = "company-1", UserId = "user-1", EmployeeId = employeeId,
        PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30
    };

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(items.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return repository;
    }
}
