using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollRunCalculationService
{
    Task<Result> SyncCalculatedRows(string companyId, string runId, string actorId);
}

public sealed class PayrollRunCalculationService(
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> runEmployees,
    IMongoDbRepository<EmpPayRoll> payrolls) : IPayrollRunCalculationService
{
    public async Task<Result> SyncCalculatedRows(string companyId, string runId, string actorId)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == runId);
        if (run is null) return Fail(404, "Payroll run was not found.");
        if (run.Status != "DRAFT") return Fail(409, "Only a draft payroll run can be synchronized.");

        var members = (await runEmployees.GetAll(x => x.CompanyId == companyId && x.PayrollRunId == runId)).ToList();
        if (members.Count == 0) return Fail(409, "Payroll run has no employees.");

        var month = new DateTime(run.PeriodStart.Year, run.PeriodStart.Month, 1);
        var rows = (await payrolls.GetAll(x => x.CompanyId == companyId && x.PayMonth == month))
            .ToDictionary(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase);
        var missing = 0;
        decimal gross = 0;
        decimal deductions = 0;
        decimal net = 0;

        foreach (var member in members)
        {
            if (!rows.TryGetValue(member.EmployeeId, out var row))
            {
                Block(member, "PAYROLL_ROW_MISSING", "Calculated employee payroll row was not found.");
                missing++;
                await SaveLine(companyId, member);
                continue;
            }

            PayrollComponentCalculationOutcome? componentResult = null;
            if (member.InputSnapshot.Components.Count > 0)
            {
                componentResult = PayrollComponentCalculationEngine.Calculate(member.InputSnapshot.Components);
                if (!componentResult.Success)
                {
                    Block(member, componentResult.ErrorCode ?? "PAYROLL_COMPONENT_FORMULA_INVALID",
                        componentResult.ErrorMessage ?? "Payroll component formula is invalid.");
                    missing++;
                    await SaveLine(companyId, member);
                    continue;
                }
            }

            if (componentResult is not null)
            {
                member.Status = "CALCULATED";
                member.ErrorCode = null;
                member.ErrorMessage = null;
                member.GrossAmount = componentResult.Gross;
                member.DeductionAmount = componentResult.Deductions;
                member.NetAmount = componentResult.Net;
            }
            else
            {
                var earning = row.BasicPay + row.Bonus + row.Allowance.HRA + row.Allowance.LTA + row.Allowance.OtherAllowance;
                var deduction = row.Deduction.LossOfPay + row.Deduction.IncomeTax + row.Deduction.HealthInsurance + row.Deduction.EPF + row.Deduction.ESIC;
                member.Status = "CALCULATED";
                member.ErrorCode = null;
                member.ErrorMessage = null;
                member.GrossAmount = PayrollCalculationRules.RoundMoney(Math.Max(0m, earning));
                member.DeductionAmount = PayrollCalculationRules.RoundMoney(Math.Max(0m, deduction));
                member.NetAmount = PayrollCalculationRules.RoundMoney(Math.Max(0m, member.GrossAmount - member.DeductionAmount));
            }

            member.StatutorySnapshot = BuildStatutorySnapshot(member, componentResult, row);
            gross += member.GrossAmount;
            deductions += member.DeductionAmount;
            net += member.NetAmount;
            await SaveLine(companyId, member);
        }

        run.EmployeeCount = members.Count;
        run.ReadyCount = members.Count(x => x.Status == "CALCULATED");
        run.BlockedCount = missing;
        run.SkippedCount = members.Count(x => x.Status == "SKIPPED");
        run.GrossTotal = PayrollCalculationRules.RoundMoney(gross);
        run.DeductionTotal = PayrollCalculationRules.RoundMoney(deductions);
        run.NetTotal = PayrollCalculationRules.RoundMoney(net);

        if (missing > 0)
        {
            run.Status = "BLOCKED";
            await runs.Update(TenantRunFilter(companyId, runId), run);
            return Fail(409, $"{missing} employees are blocked by missing payroll data or invalid component formulas.");
        }

        run.Status = "CALCULATED";
        run.Version++;
        run.AuditHistory.Add(new PayrollRunAuditEvent
        {
            Action = "RunCalculated",
            ActorId = actorId,
            OccurredAtUtc = DateTime.UtcNow,
            Notes = "Calculated from employee payroll rows and immutable component snapshots."
        });
        var saved = await runs.Update(
            TenantRunFilter(companyId, runId) & Builders<PayrollRun>.Filter.Eq(x => x.Status, "DRAFT"),
            run);
        return saved.Success
            ? new Result { Success = true, ResourceId = runId, Message = "Payroll run calculated from employee payroll rows." }
            : Fail(409, "Payroll run changed by another request; reload and retry.");
    }

    private static PayrollStatutorySnapshot BuildStatutorySnapshot(PayrollRunEmployee member, PayrollComponentCalculationOutcome? componentResult, EmpPayRoll legacyRow)
    {
        var snapshot = new PayrollStatutorySnapshot
        {
            PolicyProfileId = member.InputSnapshot.PolicyProfileId,
            PolicyProfileVersion = member.InputSnapshot.PolicyProfileVersion
        };
        if (componentResult is null)
        {
            var contractualBasic = Math.Max(0m, legacyRow.BasicPay);
            var contractualGross = Math.Max(0m, legacyRow.BasicPay + legacyRow.Bonus + legacyRow.Allowance.HRA + legacyRow.Allowance.LTA + legacyRow.Allowance.OtherAllowance);
            var earnedGross = Math.Max(0m, contractualGross - legacyRow.Deduction.LossOfPay);
            var earnedBasic = Math.Max(0m, contractualBasic - Math.Min(contractualBasic, legacyRow.Deduction.LossOfPay));
            snapshot.EmployeeEpf = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.EPF));
            snapshot.EmployerEpf = PayrollCalculationRules.CalculateMonthlyEpf(legacyRow.CalculationSnapshot?.EpfMode, earnedBasic, legacyRow.CalculationSnapshot?.EpfWageCeiling ?? 15000m);
            snapshot.EmployeeEsic = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.ESIC));
            snapshot.EmployerEsic = PayrollCalculationRules.CalculateEmployerMonthlyEsic(legacyRow.CalculationSnapshot?.EsicMode, contractualGross, earnedGross, legacyRow.CalculationSnapshot?.EsicWageCeiling ?? 21000m);
            snapshot.IncomeTax = PayrollCalculationRules.RoundMoney(Math.Max(0m, legacyRow.Deduction.IncomeTax));
            return snapshot;
        }

        snapshot.EmployeeEpf = StatutoryComponent(componentResult.Values, "EPF", "PF", "PROVIDENT_FUND");
        snapshot.EmployerEpf = StatutoryComponent(componentResult.Values, "EMPLOYER_EPF", "ER_PF", "ERPF");
        snapshot.EmployeeEsic = StatutoryComponent(componentResult.Values, "ESIC");
        snapshot.EmployerEsic = StatutoryComponent(componentResult.Values, "EMPLOYER_ESIC", "ER_ESIC", "ERESIC");
        snapshot.IncomeTax = StatutoryComponent(componentResult.Values, "TDS", "INCOME_TAX", "INCOMETAX");
        snapshot.ProfessionalTax = StatutoryComponent(componentResult.Values, "PT", "PROFESSIONAL_TAX");
        return snapshot;    }

    private static decimal StatutoryComponent(IReadOnlyDictionary<string, decimal> values, params string[] codes)
    {
        foreach (var code in codes)
            if (values.TryGetValue(code, out var amount)) return PayrollCalculationRules.RoundMoney(Math.Max(0m, amount));
        return 0m;
    }

    private async Task SaveLine(string companyId, PayrollRunEmployee member) =>
        await runEmployees.Update(
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.Id, member.Id),
            member);

    private static void Block(PayrollRunEmployee member, string code, string message)
    {
        member.Status = "BLOCKED";
        member.ErrorCode = code;
        member.ErrorMessage = message;
        member.GrossAmount = 0;
        member.DeductionAmount = 0;
        member.NetAmount = 0;
    }

    private static FilterDefinition<PayrollRun> TenantRunFilter(string companyId, string runId) =>
        Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId) &
        Builders<PayrollRun>.Filter.Eq(x => x.Id, runId);

    private static Result Fail(int code, string message) => new() { Success = false, StatusCode = code, Message = message };
}