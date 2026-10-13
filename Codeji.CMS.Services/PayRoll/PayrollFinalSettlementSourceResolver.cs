using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

/// <summary>
/// Resolves an immutable FINAL_SETTLEMENT payroll line for F&amp;F when the legacy
/// EmpPayRoll month row is not available. This resolver only reads and never
/// changes payroll runs or lines.
/// </summary>
public sealed class PayrollFinalSettlementSourceResolver(
    IMongoDbRepository<PayrollRun> payrollRunsRepository,
    IMongoDbRepository<PayrollRunEmployee> payrollRunEmployeesRepository)
{
    private static readonly string[] EligibleRunStatuses = ["CALCULATED", "PROCESSED", "PAID", "CLOSED"];
    private static readonly string[] EligibleLineStatuses = ["CALCULATED", "PROCESSED", "PAID", "CLOSED"];

    public async Task<PayrollFinalSettlementSource?> Resolve(string companyId, string employeeId, DateTime payrollMonth)
    {
        var monthStart = new DateTime(payrollMonth.Year, payrollMonth.Month, 1);
        var runs = await payrollRunsRepository.GetAll(x =>
            x.CompanyId == companyId &&
            x.RunType == "FINAL_SETTLEMENT" &&
            x.PeriodStart == monthStart &&
            EligibleRunStatuses.Contains(x.Status));

        foreach (var run in runs.OrderByDescending(x => StatusRank(x.Status)).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Version))
        {
            var lines = await payrollRunEmployeesRepository.GetAll(x =>
                x.CompanyId == companyId &&
                x.PayrollRunId == run.Id &&
                x.EmployeeId == employeeId &&
                EligibleLineStatuses.Contains(x.Status));
            var line = lines.OrderByDescending(x => StatusRank(x.Status)).ThenByDescending(x => x.Id).FirstOrDefault();
            if (line is null) continue;

            var alreadyProcessed = IsAlreadyProcessed(run.Status) || IsAlreadyProcessed(line.Status);
            return new PayrollFinalSettlementSource(run.Id, line.Id, run.PeriodStart, line.GrossAmount, line.DeductionAmount, alreadyProcessed);
        }

        return null;
    }

    private static int StatusRank(string? status) => status?.ToUpperInvariant() switch
    {
        "PAID" => 4,
        "CLOSED" => 3,
        "PROCESSED" => 2,
        "CALCULATED" => 1,
        _ => 0
    };

    private static bool IsAlreadyProcessed(string? status) => status?.ToUpperInvariant() is "PROCESSED" or "PAID" or "CLOSED";
}

public sealed record PayrollFinalSettlementSource(
    string PayrollRunId,
    string PayrollRunEmployeeId,
    DateTime PayrollMonth,
    decimal GrossAmount,
    decimal DeductionAmount,
    bool AlreadyProcessed);