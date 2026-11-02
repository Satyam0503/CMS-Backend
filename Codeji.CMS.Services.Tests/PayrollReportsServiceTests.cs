using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollReportsServiceTests
{
    [Fact]
    public async Task Summary_IsTenantScopedAndUsesRunTotals()
    {
        var runs = Repository(
            new PayrollRun
            {
                Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
                PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1),
                Status = "CALCULATED", EmployeeCount = 2, BlockedCount = 0, SkippedCount = 0,
                GrossTotal = 3000.005m, DeductionTotal = 300.005m, NetTotal = 2700m
            },
            new PayrollRun
            {
                Id = "run-other", CompanyId = "company-2", PeriodStart = new DateTime(2026, 9, 1),
                PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1),
                Status = "CALCULATED", EmployeeCount = 99, GrossTotal = 999999m
            });
        var employees = Repository<PayrollRunEmployee>();
        var service = new PayrollReportsService(runs.Object, employees.Object);

        var result = await service.GetSummary("company-1", new PayrollReportQueryDto
        {
            From = new DateTime(2026, 9, 1), To = new DateTime(2026, 9, 30), Status = "calculated"
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.MethodResult.RunCount);
        Assert.Equal(2, result.MethodResult.EmployeeCount);
        Assert.Equal(3000.01m, result.MethodResult.GrossTotal);
        Assert.Equal(300.01m, result.MethodResult.DeductionTotal);
        Assert.Equal(2700m, result.MethodResult.NetTotal);
        Assert.Single(result.MethodResult.StatusTotals);
    }

    [Fact]
    public async Task Register_FiltersLinesByEmployeeAndStatusAndExportsCsv()
    {
        var runs = Repository(new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1),
            Status = "CALCULATED", RunType = "REGULAR"
        });
        var employees = Repository(
            new PayrollRunEmployee
            {
                Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
                Status = "CALCULATED", GrossAmount = 1000, DeductionAmount = 100, NetAmount = 900,
                ErrorMessage = "none"
            },
            new PayrollRunEmployee
            {
                Id = "line-2", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-2",
                Status = "BLOCKED", ErrorCode = "FORMULA", ErrorMessage = "Invalid, formula"
            });
        var service = new PayrollReportsService(runs.Object, employees.Object);

        var query = new PayrollReportQueryDto { EmployeeId = "employee-1", LineStatus = "CALCULATED" };
        var register = await service.GetRegister("company-1", query);
        var csv = await service.ExportRegister("company-1", query);

        Assert.True(register.Success);
        Assert.Single(register.MethodResult);
        Assert.Equal(900m, register.MethodResult[0].NetAmount);
        Assert.True(csv.Success);
        var content = System.Text.Encoding.UTF8.GetString(csv.MethodResult);
        Assert.Contains("RunId,PeriodStart", content);
        Assert.Contains("employee-1", content);
        Assert.DoesNotContain("employee-2", content);
    }

    [Fact]
    public async Task StatutoryRegister_IsTenantScopedAndExportsCapturedContributions()
    {
        var runs = Repository(
            new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), RunType = "REGULAR", Status = "PAID" },
            new PayrollRun { Id = "other-run", CompanyId = "company-2", PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), RunType = "REGULAR", Status = "PAID" });
        var employees = Repository(
            new PayrollRunEmployee { Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "CALCULATED", StatutorySnapshot = new PayrollStatutorySnapshot { PolicyProfileId = "policy-1", PolicyProfileVersion = 3, EmployeeEpf = 1800, EmployerEpf = 1800, EmployeeEsic = 75, EmployerEsic = 325, IncomeTax = 1000, ProfessionalTax = 200 } },
            new PayrollRunEmployee { Id = "line-other", CompanyId = "company-2", PayrollRunId = "other-run", EmployeeId = "employee-1", Status = "CALCULATED", StatutorySnapshot = new PayrollStatutorySnapshot { EmployeeEpf = 99999 } });
        var service = new PayrollReportsService(runs.Object, employees.Object);

        var result = await service.GetStatutoryRegister("company-1", new PayrollReportQueryDto());
        var csv = await service.ExportStatutoryRegister("company-1", new PayrollReportQueryDto());

        Assert.True(result.Success);
        var row = Assert.Single(result.MethodResult);
        Assert.Equal("policy-1", row.PolicyProfileId);
        Assert.Equal(3, row.PolicyProfileVersion);
        Assert.Equal(3075m, row.TotalEmployeeStatutory);
        Assert.Equal(325m, row.EmployerEsic);
        Assert.True(csv.Success);
        Assert.Contains("ProfessionalTax", System.Text.Encoding.UTF8.GetString(csv.MethodResult));
    }
    [Fact]
    public async Task RejectsReversedDateRange()
    {
        var service = new PayrollReportsService(Repository<PayrollRun>().Object, Repository<PayrollRunEmployee>().Object);

        var result = await service.GetSummary("company-1", new PayrollReportQueryDto
        {
            From = new DateTime(2026, 10, 1), To = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ExportEpfoEcrText_GeneratesValidEcrFormatWithCustomUanAndCalculatedSplit()
    {
        var runs = Repository(new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1),
            Status = "CALCULATED", RunType = "REGULAR"
        });

        var employees = Repository(
            new PayrollRunEmployee
            {
                Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "EMP001",
                Status = "CALCULATED", GrossAmount = 25000m,
                InputSnapshot = new PayrollEmployeeInputSnapshot { EpfWageCeiling = 15000m },
                StatutorySnapshot = new PayrollStatutorySnapshot
                {
                    PolicyProfileId = "epf-policy",
                    EmployeeEpf = 1800m,
                    EmployerEpf = 1800m
                }
            });

        var users = Repository(
            new EmpUser
            {
                CompanyId = "company-1", UserId = "user-1", EmployeeId = "EMP001",
                FirstName = "Rajesh", LastName = "Kumar", Email = "rajesh@example.com",
                CustomAttributeList = [new() { CustomAttributeId = "UAN", CustomAttributeValueId = "100987654321" }]
            });

        var service = new PayrollReportsService(runs.Object, employees.Object, users.Object);

        var result = await service.ExportEpfoEcrText("company-1", new PayrollReportQueryDto());

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRecords);
        var text = System.Text.Encoding.UTF8.GetString(result.MethodResult).Trim();
        Assert.Equal("100987654321#~#RAJESH KUMAR#~#25000#~#15000#~#15000#~#15000#~#1800#~#1250#~#550#~#0#~#0", text);
    }

    [Fact]
    public async Task ExportEsicReturnCsv_GeneratesValidCsvWithIpNumberAndContributions()
    {
        var runs = Repository(new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1),
            Status = "CALCULATED", RunType = "REGULAR"
        });

        var employees = Repository(
            new PayrollRunEmployee
            {
                Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "EMP002",
                Status = "CALCULATED", GrossAmount = 18000m,
                InputSnapshot = new PayrollEmployeeInputSnapshot { EsicWageCeiling = 21000m },
                StatutorySnapshot = new PayrollStatutorySnapshot
                {
                    EmployeeEsic = 135m,
                    EmployerEsic = 585m
                }
            });

        var users = Repository(
            new EmpUser
            {
                CompanyId = "company-1", UserId = "user-2", EmployeeId = "EMP002",
                FirstName = "Anita", LastName = "Sharma", Email = "anita@example.com",
                CustomAttributeList = [new() { CustomAttributeId = "IPNumber", CustomAttributeValueId = "31000554433" }]
            });

        var service = new PayrollReportsService(runs.Object, employees.Object, users.Object);

        var result = await service.ExportEsicReturnCsv("company-1", new PayrollReportQueryDto());

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRecords);
        var csv = System.Text.Encoding.UTF8.GetString(result.MethodResult);

        Assert.Contains("IPNumber,IPName,NoOfDaysWorked,TotalMonthlyWages,IPContribution,ReasonForZeroWorkingDays", csv);
        Assert.Contains("31000554433,Anita Sharma,30,18000.00,135.00", csv);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        return repository;
    }
}
