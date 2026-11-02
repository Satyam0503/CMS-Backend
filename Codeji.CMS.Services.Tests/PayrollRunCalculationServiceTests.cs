using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunCalculationServiceTests
{
    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenAnyOverlappingSalaryVersionHasInvalidFormula()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 60000 }],
                SalaryStructures =
                [
                    new() { SalaryStructureId = "salary-1", Version = 1, EffectiveFrom = new DateTime(2026, 9, 1), EffectiveTo = new DateTime(2026, 9, 14), Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 60000 }] },
                    new() { SalaryStructureId = "salary-2", Version = 1, EffectiveFrom = new DateTime(2026, 9, 15), Components = [new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "MISSING", Value = 40 }] }
                ]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 60000, PaidDays = 30
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.False(result.Success);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal("PAYROLL_COMPONENT_BASE_MISSING", member.ErrorCode);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(0m, member.NetAmount);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksFullMonthWhenCanonicalSalaryEarningsDifferFromGeneratedRow()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                Components =
                [
                    new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000.005m, SortOrder = 10 },
                    new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 50, SortOrder = 20 },
                    new() { Code = "PF", Name = "PF", Kind = "DEDUCTION", CalculationType = "PERCENT_OF_GROSS", Value = 10, SortOrder = 30 }
                ],
                SalaryStructures =
                [
                    new()
                    {
                        SalaryStructureId = "salary-1", Version = 1,
                        EffectiveFrom = new DateTime(2026, 9, 1),
                        Components =
                        [
                            new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000.005m, SortOrder = 10 },
                            new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 50, SortOrder = 20 },
                            new() { Code = "PF", Name = "PF", Kind = "DEDUCTION", CalculationType = "PERCENT_OF_GROSS", Value = 10, IsStatutory = true, SortOrder = 30 }
                        ]
                    }
                ]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "legacy-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 2000m, Bonus = 0m, PaidDays = 30,
            Allowance = new Allowance { HRA = 400m, LTA = 0m, OtherAllowance = 500m },
            Deduction = new Deduction { LossOfPay = 0m, IncomeTax = 50m, HealthInsurance = 20m, EPF = 30m, ESIC = 5m },
            DeductionLines =
            [
                new() { Code = "SALARY_LOAN_REPAYMENT", Description = "Loan repayment", Amount = 40m }
            ],
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                EligibleFrom = new DateTime(2026, 9, 1), EligibleTo = new DateTime(2026, 9, 30),
                SalaryRevisions = [new() { SalaryStructureId = "salary-1", Version = 1 }],
                SalaryStructures =
                [
                    new()
                    {
                        SalaryStructureId = "salary-1", Version = 1, EffectiveFrom = new DateTime(2026, 9, 1),
                        Components = member.InputSnapshot.SalaryStructures[0].Components
                    }
                ]
            }
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var payrolls = Repository(row);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, payrolls.Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.False(result.Success);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal("PAYROLL_CANONICAL_COMPONENT_TOTAL_MISMATCH", member.ErrorCode);
        Assert.Contains("Frozen recurring salary components", member.ErrorMessage);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal(0m, run.GrossTotal);
        Assert.Equal("BLOCKED", run.Status);
        lines.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), member), Times.Once);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    [Fact]
    public async Task SyncCalculatedRows_AcceptsFullMonthWhenCanonicalSalaryEarningsMatchGeneratedRow()
    {
        var run = new PayrollRun
        {
            Id = "run-match", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var components = new List<PayrollComponentSnapshot>
        {
            new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000.005m, SortOrder = 10 },
            new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 50, SortOrder = 20 },
            new() { Code = "PF", Name = "PF", Kind = "DEDUCTION", CalculationType = "PERCENT_OF_GROSS", Value = 10, IsStatutory = true, SortOrder = 30 }
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-match", CompanyId = run.CompanyId, PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                SalaryStructures =
                [
                    new() { SalaryStructureId = "salary-1", Version = 1, EffectiveFrom = run.PeriodStart, Components = components }
                ]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "row-match", CompanyId = run.CompanyId, UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = run.PeriodStart, PaidDate = run.PayDate, BasicPay = 1000.01m, PaidDays = 30,
            Allowance = new Allowance { HRA = 500.01m },
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                EligibleFrom = run.PeriodStart, EligibleTo = run.PeriodEnd,
                SalaryRevisions = [new() { SalaryStructureId = "salary-1", Version = 1 }],
                SalaryStructures =
                [
                    new() { SalaryStructureId = "salary-1", Version = 1, EffectiveFrom = run.PeriodStart, Components = components }
                ]
            }
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows(run.CompanyId, run.Id, "actor-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal("CALCULATED", member.Status);
        Assert.Equal(1500.02m, member.GrossAmount);
        Assert.Equal("CALCULATED", run.Status);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenSourcePayrollRowContainsNegativeDeductionLine()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var member = new PayrollRunEmployee { Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1" };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30,
            DeductionLines = [new() { Code = "SALARY_RECOVERY", Description = "Recovery", Amount = -50m }]
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PAYROLL_SOURCE_ROW_INVALID", member.ErrorCode);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal("BLOCKED", run.Status);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenItemizedEarningsDoNotMatchAllowanceTotal()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var member = new PayrollRunEmployee { Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1" };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30,
            Allowance = new Allowance { OtherAllowance = 500m },
            EarningLines = [new() { Code = "MEAL", Description = "Meal", Amount = 250m }]
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PAYROLL_EARNING_LINES_MISMATCH", member.ErrorCode);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal("BLOCKED", run.Status);
    }
    [Fact]
    public async Task SyncCalculatedRows_BlocksEmployeeWhenActualDeductionsExceedGross()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var member = new PayrollRunEmployee { Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1" };
        var row = new EmpPayRoll { Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId, PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30, Deduction = new Deduction { IncomeTax = 600m, EPF = 600m } };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);
        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");
        Assert.False(result.Success);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal("PAYROLL_NET_NEGATIVE", member.ErrorCode);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal(0m, member.DeductionAmount);
        Assert.Equal(0m, member.NetAmount);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(0m, run.GrossTotal);
        Assert.Equal(0m, run.NetTotal);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenPayrollRowUsesDifferentSalaryRevisionThanFrozenRun()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000m }],
                SalaryStructures = [new() { SalaryStructureId = "salary-approved-v1", Version = 1, EffectiveFrom = new DateTime(2026, 9, 1) }]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 2000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                SalaryRevisions = [new() { SalaryStructureId = "salary-approved-v2", Version = 2 }]
            }
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_SALARY_SNAPSHOT_MISMATCH", member.ErrorCode);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(0m, run.GrossTotal);
    }

    [Theory]
    [InlineData("MATCH")]
    [InlineData("ASSIGNMENT_ID")]
    [InlineData("ASSIGNMENT_VERSION")]
    [InlineData("PAY_GROUP_ID")]
    [InlineData("PAY_GROUP_VERSION")]
    [InlineData("TEMPLATE_ID")]
    [InlineData("TEMPLATE_VERSION")]
    public async Task SyncCalculatedRows_ValidatesFrozenAssignmentGroupAndTemplate(string mismatch)
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                AssignmentId = "assignment-1", AssignmentVersion = 4,
                PayGroupId = "group-1", PayGroupVersion = 3,
                SalaryTemplateId = "template-1", SalaryTemplateVersion = 2
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                PayrollAssignmentId = "assignment-1", PayrollAssignmentVersion = 4,
                PayGroupId = "group-1", PayGroupVersion = 3,
                SalaryTemplateId = "template-1", SalaryTemplateVersion = 2
            }
        };
        switch (mismatch)
        {
            case "ASSIGNMENT_ID": row.CalculationSnapshot.PayrollAssignmentId = "assignment-other"; break;
            case "ASSIGNMENT_VERSION": row.CalculationSnapshot.PayrollAssignmentVersion++; break;
            case "PAY_GROUP_ID": row.CalculationSnapshot.PayGroupId = "group-other"; break;
            case "PAY_GROUP_VERSION": row.CalculationSnapshot.PayGroupVersion++; break;
            case "TEMPLATE_ID": row.CalculationSnapshot.SalaryTemplateId = "template-other"; break;
            case "TEMPLATE_VERSION": row.CalculationSnapshot.SalaryTemplateVersion++; break;
        }
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        if (mismatch == "MATCH")
        {
            Assert.True(result.Success);
            Assert.Equal("CALCULATED", member.Status);
        }
        else
        {
            Assert.False(result.Success);
            Assert.Equal("PAYROLL_ASSIGNMENT_SNAPSHOT_MISMATCH", member.ErrorCode);
            Assert.Equal("BLOCKED", member.Status);
        }
    }

    [Theory]
    [InlineData("policy-v2", 4)]
    [InlineData("policy-v1", 5)]
    public async Task SyncCalculatedRows_BlocksWhenPayrollRowUsesDifferentPolicyProfileOrVersion(string calculatedProfileId, int calculatedVersion)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                PolicyProfileId = "policy-v1", PolicyProfileVersion = 4,
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000m }]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                ResolvedPolicyProfileId = calculatedProfileId,
                ResolvedPolicyVersion = calculatedVersion
            }
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PAYROLL_POLICY_SNAPSHOT_MISMATCH", member.ErrorCode);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal(0m, member.GrossAmount);
        Assert.Equal("BLOCKED", run.Status);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenSalaryComponentValuesDifferDespiteMatchingRevision()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var expectedSalary = new PayrollSalaryStructureSnapshot
        {
            SalaryStructureId = "salary-v1", Version = 1, EffectiveFrom = new DateTime(2026, 9, 1),
            Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 50000m }]
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot { SalaryStructures = [expectedSalary] }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 50000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                SalaryRevisions = [new() { SalaryStructureId = "salary-v1", Version = 1 }],
                SalaryStructures = [new() { SalaryStructureId = "salary-v1", Version = 1, EffectiveFrom = new DateTime(2026, 9, 1), Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 52000m }] }]
            }
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_SALARY_COMPONENT_SNAPSHOT_MISMATCH", member.ErrorCode);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(0m, member.GrossAmount);
    }

    [Fact]
    public async Task SyncCalculatedRows_BlocksWhenFrozenStatutoryRateDiffersFromCalculatedRow()
    {
        var run = new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT" };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                PolicyProfileId = "policy-v1", PolicyProfileVersion = 2,
                EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m,
                EpfEmployeeRatePercent = 10m, EpfEmployerRatePercent = 12m,
                EsicMode = "STATUTORY", EsicWageCeiling = 21000m,
                EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
                TdsMode = "AUTO_NEW_REGIME"
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 50000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                ResolvedPolicyProfileId = "policy-v1", ResolvedPolicyVersion = 2,
                EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m,
                EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
                EsicMode = "STATUTORY", EsicWageCeiling = 21000m,
                EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
                TdsMode = "AUTO_NEW_REGIME"
            }
        };
        var service = new PayrollRunCalculationService(Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_POLICY_SETTINGS_SNAPSHOT_MISMATCH", member.ErrorCode);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(0m, member.GrossAmount);
    }

    [Fact]
    public async Task SyncCalculatedRows_AcceptsPayrollRowWhenPolicyProfileAndVersionMatchFrozenRun()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id, EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                PolicyProfileId = "policy-v3", PolicyProfileVersion = 7,
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000m }]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "payroll-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1), BasicPay = 1000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                ResolvedPolicyProfileId = "policy-v3",
                ResolvedPolicyVersion = 7
            }
        };
        var service = new PayrollRunCalculationService(
            Repository(run).Object, Repository(member).Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", run.Id, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("CALCULATED", member.Status);
        Assert.Equal(1000m, member.GrossAmount);
        Assert.Equal("policy-v3", member.StatutorySnapshot.PolicyProfileId);
        Assert.Equal(7, member.StatutorySnapshot.PolicyProfileVersion);
    }

    [Fact]
    public async Task SyncCalculatedRows_RetriesBlockedRunUsingItsFrozenSnapshotAfterPayrollRowAppears()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "BLOCKED", Version = 4
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            Status = "BLOCKED", ErrorCode = "PAYROLL_ROW_MISSING", ErrorMessage = "Payroll row was absent.",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                SalaryStructureId = "salary-approved-v3", SalaryStructureVersion = 3,
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000m }]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "legacy-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 1000m, PaidDays = 30
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository(row).Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "retry-actor");

        Assert.True(result.Success);
        Assert.Equal("CALCULATED", run.Status);
        Assert.Equal(5, run.Version);
        Assert.Equal("CALCULATED", member.Status);
        Assert.Equal("salary-approved-v3", member.InputSnapshot.SalaryStructureId);
        Assert.Equal(1000m, run.GrossTotal);
        Assert.Equal(1000m, run.NetTotal);
        Assert.Contains(run.AuditHistory, entry => entry.Action == "RunCalculated" && entry.Notes!.Contains("preserved"));
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    [Fact]
    public async Task SyncCalculatedRows_ContinuesToBlockAndAuditsWhenRetrySourceRowIsStillMissing()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "BLOCKED", Version = 2
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "BLOCKED",
            InputSnapshot = new PayrollEmployeeInputSnapshot { Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000m }] }
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository<EmpPayRoll>().Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "retry-actor");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("BLOCKED", run.Status);
        Assert.Equal(3, run.Version);
        Assert.Equal("BLOCKED", member.Status);
        Assert.Equal("PAYROLL_EMPLOYEE_SALARY_MISSING", member.ErrorCode);
        Assert.Contains("pay group or salary template alone is not payable", member.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(run.AuditHistory, entry => entry.Action == "RunCalculationBlocked" && entry.Notes!.Contains("Retry"));
    }

    [Theory]
    [InlineData("CALCULATED")]
    [InlineData("APPROVED")]
    [InlineData("PROCESSED")]
    public async Task SyncCalculatedRows_RejectsRetryAfterRunLeavesDraftOrBlocked(string status)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = status
        };
        var member = new PayrollRunEmployee { Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1" };
        var runs = Repository(run);
        var lines = Repository(member);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, Repository<EmpPayRoll>().Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        lines.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), It.IsAny<PayrollRunEmployee>()), Times.Never);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task SyncCalculatedRows_FreezesDayLevelAttendanceAndLopSnapshot()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };

        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                RequireAttendanceLock = true,
                SalaryStructureId = "salary-1",
                Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 30000m, SortOrder = 1 }]
            }
        };

        var attendanceSummary = new MonthlyAttendanceSummary
        {
            Id = "summary-101",
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "employee-1",
            PayrollMonth = new DateTime(2026, 9, 1),
            Version = 2,
            IsApproved = true,
            IsLocked = true,
            ApprovedBy = "hr-manager",
            ExpectedWorkingDays = 22,
            EligibleWorkingDays = 22,
            PresentDays = 18m,
            WfhDays = 2m,
            PaidLeaveDays = 1m,
            UnpaidDays = 1m,
            HalfDays = 0m,
            AbsentDays = 1m,
            PaidDays = 21m,
            LhdCount = 1,
            EdCount = 0,
            CombinedLhdEdCount = 1
        };

        var row = new EmpPayRoll
        {
            Id = "legacy-row-1",
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1),
            PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 29000m,
            PaidDays = 29f,
            Allowance = new Allowance { OtherAllowance = 0m },
            Deduction = new Deduction { LossOfPay = 1000m, LossOfPayDays = 1f },
            CalculationSnapshot = new PayrollCalculationSnapshot
            {
                AttendanceSummaryId = "summary-101",
                AttendanceSummaryVersion = 2,
                Divisor = 30m,
                DivisorPolicy = "CALENDAR_DAYS",
                LossOfPay = 1000m,
                LossOfPayDays = 1m
            }
        };

        var runs = Repository(run);
        var lines = Repository(member);
        var summaries = Repository(attendanceSummary);
        var payrolls = Repository(row);

        var service = new PayrollRunCalculationService(runs.Object, lines.Object, payrolls.Object, summaries.Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.True(result.Success);
        Assert.Equal("CALCULATED", member.Status);
        Assert.NotNull(member.AttendanceSnapshot);
        Assert.Equal("summary-101", member.AttendanceSnapshot.SummaryId);
        Assert.Equal(2, member.AttendanceSnapshot.SummaryVersion);
        Assert.True(member.AttendanceSnapshot.IsApproved);
        Assert.True(member.AttendanceSnapshot.IsLocked);
        Assert.Equal("hr-manager", member.AttendanceSnapshot.ApprovedBy);
        Assert.Equal(22, member.AttendanceSnapshot.ExpectedWorkingDays);
        Assert.Equal(18m, member.AttendanceSnapshot.PresentDays);
        Assert.Equal(2m, member.AttendanceSnapshot.WfhDays);
        Assert.Equal(1m, member.AttendanceSnapshot.LossOfPayDays);
        Assert.Equal(1000m, member.AttendanceSnapshot.LossOfPayAmount);
        Assert.Equal(30m, member.AttendanceSnapshot.Divisor);
        Assert.Equal("CALENDAR_DAYS", member.AttendanceSnapshot.DivisorPolicy);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult(items.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}
