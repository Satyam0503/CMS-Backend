using System.Globalization;
using System.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollReportsService
{
    Task<Result<PayrollReportSummaryDto>> GetSummary(string companyId, PayrollReportQueryDto query);
    Task<Result<List<PayrollRegisterRowDto>>> GetRegister(string companyId, PayrollReportQueryDto query);
    Task<Result<byte[]>> ExportRegister(string companyId, PayrollReportQueryDto query);
    Task<Result<List<PayrollStatutoryRegisterRowDto>>> GetStatutoryRegister(string companyId, PayrollReportQueryDto query);
    Task<Result<byte[]>> ExportStatutoryRegister(string companyId, PayrollReportQueryDto query);
    Task<Result<byte[]>> ExportEpfoEcrText(string companyId, PayrollReportQueryDto query);
    Task<Result<byte[]>> ExportEsicReturnCsv(string companyId, PayrollReportQueryDto query);
    Task<Result<List<Form16CertificateDto>>> GetForm16Certificates(string companyId, string financialYear, string? employeeId = null);
    Task<Result<byte[]>> ExportForm16SummaryCsv(string companyId, string financialYear);
}

public sealed class PayrollReportsService(
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> employees,
    IMongoDbRepository<EmpUser>? userRepo = null,
    IMongoDbRepository<EmployeeTaxDeclaration>? declarations = null) : IPayrollReportsService
{
    public async Task<Result<PayrollReportSummaryDto>> GetSummary(string companyId, PayrollReportQueryDto query)
    {
        var validation = Validate(query);
        if (!validation.Success) return new Result<PayrollReportSummaryDto>
        {
            Success = false, StatusCode = validation.StatusCode, Message = validation.Message
        };

        var selected = await LoadRuns(companyId, query);
        var statusTotals = selected
            .GroupBy(x => x.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PayrollReportStatusTotalDto
            {
                Status = group.Key,
                RunCount = group.Count(),
                EmployeeCount = group.Sum(x => x.EmployeeCount),
                BlockedCount = group.Sum(x => x.BlockedCount),
                SkippedCount = group.Sum(x => x.SkippedCount),
                GrossTotal = Round(group.Sum(x => x.GrossTotal)),
                DeductionTotal = Round(group.Sum(x => x.DeductionTotal)),
                NetTotal = Round(group.Sum(x => x.NetTotal))
            }).ToList();

        var summary = new PayrollReportSummaryDto
        {
            From = query.From?.Date,
            To = query.To?.Date,
            RunCount = selected.Count,
            EmployeeCount = selected.Sum(x => x.EmployeeCount),
            BlockedCount = selected.Sum(x => x.BlockedCount),
            SkippedCount = selected.Sum(x => x.SkippedCount),
            GrossTotal = Round(selected.Sum(x => x.GrossTotal)),
            DeductionTotal = Round(selected.Sum(x => x.DeductionTotal)),
            NetTotal = Round(selected.Sum(x => x.NetTotal)),
            StatusTotals = statusTotals
        };
        return new Result<PayrollReportSummaryDto> { Success = true, MethodResult = summary, TotalRecords = selected.Count };
    }

    public async Task<Result<List<PayrollRegisterRowDto>>> GetRegister(string companyId, PayrollReportQueryDto query)
    {
        var validation = Validate(query);
        if (!validation.Success) return new Result<List<PayrollRegisterRowDto>>
        {
            Success = false, StatusCode = validation.StatusCode, Message = validation.Message
        };

        var selectedRuns = await LoadRuns(companyId, query);
        var runIds = selectedRuns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedEmployees = (await employees.GetAll(x => x.CompanyId == companyId))
            .Where(x => runIds.Contains(x.PayrollRunId))
            .Where(x => string.IsNullOrWhiteSpace(query.EmployeeId) || string.Equals(x.EmployeeId, query.EmployeeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.LineStatus) || string.Equals(x.Status, query.LineStatus.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        var runById = selectedRuns.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var rows = selectedEmployees
            .Where(x => runById.ContainsKey(x.PayrollRunId))
            .Select(x =>
            {
                var run = runById[x.PayrollRunId];
                return new PayrollRegisterRowDto
                {
                    RunId = run.Id, PeriodStart = run.PeriodStart, PeriodEnd = run.PeriodEnd, PayDate = run.PayDate,
                    RunType = run.RunType, RunStatus = run.Status, EmployeeId = x.EmployeeId,
                    DepartmentId = x.DepartmentId, PayGroupId = x.PayGroupId, LineStatus = x.Status,
                    ErrorCode = x.ErrorCode, ErrorMessage = x.ErrorMessage,
                    GrossAmount = Round(x.GrossAmount), DeductionAmount = Round(x.DeductionAmount), NetAmount = Round(x.NetAmount)
                };
            })
            .OrderByDescending(x => x.PeriodStart)
            .ThenBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new Result<List<PayrollRegisterRowDto>> { Success = true, MethodResult = rows, TotalRecords = rows.Count };
    }

    public async Task<Result<List<PayrollStatutoryRegisterRowDto>>> GetStatutoryRegister(string companyId, PayrollReportQueryDto query)
    {
        var validation = Validate(query);
        if (!validation.Success) return new Result<List<PayrollStatutoryRegisterRowDto>> { Success = false, StatusCode = validation.StatusCode, Message = validation.Message };

        var selectedRuns = await LoadRuns(companyId, query);
        var runIds = selectedRuns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runById = selectedRuns.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var rows = (await employees.GetAll(x => x.CompanyId == companyId && runIds.Contains(x.PayrollRunId)))
            .Where(x => string.IsNullOrWhiteSpace(query.EmployeeId) || string.Equals(x.EmployeeId, query.EmployeeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.LineStatus) || string.Equals(x.Status, query.LineStatus.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => runById.ContainsKey(x.PayrollRunId))
            .Select(x =>
            {
                var run = runById[x.PayrollRunId];
                var statutory = x.StatutorySnapshot;
                return new PayrollStatutoryRegisterRowDto
                {
                    RunId = run.Id, PeriodStart = run.PeriodStart, PeriodEnd = run.PeriodEnd, PayDate = run.PayDate,
                    RunType = run.RunType, RunStatus = run.Status, EmployeeId = x.EmployeeId,
                    PolicyProfileId = statutory.PolicyProfileId, PolicyProfileVersion = statutory.PolicyProfileVersion,
                    EmployeeEpf = Round(statutory.EmployeeEpf), EmployerEpf = Round(statutory.EmployerEpf),
                    EmployeeEsic = Round(statutory.EmployeeEsic), EmployerEsic = Round(statutory.EmployerEsic),
                    IncomeTax = Round(statutory.IncomeTax), ProfessionalTax = Round(statutory.ProfessionalTax),
                    TotalEmployeeStatutory = Round(statutory.EmployeeEpf + statutory.EmployeeEsic + statutory.IncomeTax + statutory.ProfessionalTax)
                };
            })
            .OrderByDescending(x => x.PeriodStart)
            .ThenBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new Result<List<PayrollStatutoryRegisterRowDto>> { Success = true, MethodResult = rows, TotalRecords = rows.Count };
    }

    public async Task<Result<byte[]>> ExportStatutoryRegister(string companyId, PayrollReportQueryDto query)
    {
        var result = await GetStatutoryRegister(companyId, query);
        if (!result.Success) return new Result<byte[]> { Success = false, StatusCode = result.StatusCode, Message = result.Message };
        var builder = new StringBuilder();
        builder.AppendLine("RunId,PeriodStart,PeriodEnd,PayDate,RunType,RunStatus,EmployeeId,PolicyProfileId,PolicyProfileVersion,EmployeeEpf,EmployerEpf,EmployeeEsic,EmployerEsic,IncomeTax,ProfessionalTax,TotalEmployeeStatutory");
        foreach (var row in result.MethodResult)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Csv(row.RunId), Csv(row.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), Csv(row.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(row.PayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), Csv(row.RunType), Csv(row.RunStatus), Csv(row.EmployeeId),
                Csv(row.PolicyProfileId), Csv(row.PolicyProfileVersion.ToString(CultureInfo.InvariantCulture)), Csv(row.EmployeeEpf.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(row.EmployerEpf.ToString("0.00", CultureInfo.InvariantCulture)), Csv(row.EmployeeEsic.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(row.EmployerEsic.ToString("0.00", CultureInfo.InvariantCulture)), Csv(row.IncomeTax.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(row.ProfessionalTax.ToString("0.00", CultureInfo.InvariantCulture)), Csv(row.TotalEmployeeStatutory.ToString("0.00", CultureInfo.InvariantCulture))
            }));
        }
        return new Result<byte[]> { Success = true, MethodResult = Encoding.UTF8.GetBytes(builder.ToString()), TotalRecords = result.TotalRecords };
    }
    public async Task<Result<byte[]>> ExportRegister(string companyId, PayrollReportQueryDto query)
    {
        var result = await GetRegister(companyId, query);
        if (!result.Success)
            return new Result<byte[]> { Success = false, StatusCode = result.StatusCode, Message = result.Message };

        var builder = new StringBuilder();
        builder.AppendLine("RunId,PeriodStart,PeriodEnd,PayDate,RunType,RunStatus,EmployeeId,DepartmentId,PayGroupId,LineStatus,ErrorCode,ErrorMessage,GrossAmount,DeductionAmount,NetAmount");
        foreach (var row in result.MethodResult)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Csv(row.RunId), Csv(row.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(row.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(row.PayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), Csv(row.RunType), Csv(row.RunStatus),
                Csv(row.EmployeeId), Csv(row.DepartmentId), Csv(row.PayGroupId), Csv(row.LineStatus), Csv(row.ErrorCode),
                Csv(row.ErrorMessage), Csv(row.GrossAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(row.DeductionAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(row.NetAmount.ToString("0.00", CultureInfo.InvariantCulture))
            }));
        }

        return new Result<byte[]> { Success = true, MethodResult = Encoding.UTF8.GetBytes(builder.ToString()), TotalRecords = result.TotalRecords };
    }

    public async Task<Result<byte[]>> ExportEpfoEcrText(string companyId, PayrollReportQueryDto query)
    {
        var validation = Validate(query);
        if (!validation.Success) return new Result<byte[]> { Success = false, StatusCode = validation.StatusCode, Message = validation.Message };

        var selectedRuns = await LoadRuns(companyId, query);
        var runIds = selectedRuns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runEmployees = (await employees.GetAll(x => x.CompanyId == companyId && runIds.Contains(x.PayrollRunId)))
            .Where(x => string.IsNullOrWhiteSpace(query.EmployeeId) || string.Equals(x.EmployeeId, query.EmployeeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Status == "CALCULATED")
            .ToList();

        var missingEpfBase = runEmployees.FirstOrDefault(x =>
            ((x.StatutorySnapshot?.EmployeeEpf ?? 0m) > 0m ||
             (x.StatutorySnapshot?.EmployerEpf ?? 0m) > 0m ||
             (x.StatutorySnapshot?.EmployerEps ?? 0m) > 0m ||
             (x.StatutorySnapshot?.EmployerEdli ?? 0m) > 0m) &&
            (x.StatutorySnapshot?.EpfWages is null || x.StatutorySnapshot.EpsWages is null || x.StatutorySnapshot.EdliWages is null));
        if (missingEpfBase is not null)
            return new Result<byte[]> { Success = false, StatusCode = 422, Message = $"EPF ECR export is blocked because employee {missingEpfBase.EmployeeId} has contribution amounts without a frozen EPF/EPS/EDLI wage base. Reconcile the historical payroll data before filing." };

        var users = userRepo is null ? [] : (await userRepo.GetAll(x => x.CompanyId == companyId)).ToDictionary(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        var exportedRecords = 0;
        foreach (var member in runEmployees)
        {
            var statutory = member.StatutorySnapshot;
            var eeShare = Round(statutory?.EmployeeEpf ?? 0m);
            if (eeShare <= 0m && (statutory?.EmployerEpf ?? 0m) <= 0m &&
                (statutory?.EmployerEps ?? 0m) <= 0m && (statutory?.EmployerEdli ?? 0m) <= 0m)
                continue;

            users.TryGetValue(member.EmployeeId, out var user);
            var memberName = user is not null
                ? $"{user.FirstName} {user.LastName}".Trim().ToUpperInvariant()
                : member.EmployeeId.ToUpperInvariant();

            var uan = user?.CustomAttributeList?.FirstOrDefault(x => string.Equals(x.CustomAttributeId, "UAN", StringComparison.OrdinalIgnoreCase))?.CustomAttributeValueId
                      ?? member.EmployeeId;

            var grossWages = Round(member.GrossAmount);
            // New payroll runs persist the statutory wage bases and employer split used by
            // calculation. Historical rows without these fields were rejected above.
            var epfWages = Round(statutory?.EpfWages ?? 0m);
            var epsWages = Round(statutory?.EpsWages ?? 0m);
            var edliWages = Round(statutory?.EdliWages ?? 0m);
            var employerEpsRaw = statutory?.EmployerEps ?? 0m;
            var employerEpfRaw = statutory?.EmployerEpf ?? 0m;

            var eeShareInt = Math.Round(eeShare, 0, MidpointRounding.AwayFromZero);
            var employerEpsInt = Math.Round(employerEpsRaw, 0, MidpointRounding.AwayFromZero);
            var employerEpfInt = Math.Round(employerEpfRaw, 0, MidpointRounding.AwayFromZero);

            builder.AppendLine($"{uan}#~#{memberName}#~#{grossWages:F0}#~#{epfWages:F0}#~#{epsWages:F0}#~#{edliWages:F0}#~#{eeShareInt:F0}#~#{employerEpsInt:F0}#~#{employerEpfInt:F0}#~#0#~#0");
            exportedRecords++;
        }

        return new Result<byte[]>
        {
            Success = true,
            MethodResult = Encoding.UTF8.GetBytes(builder.ToString()),
            TotalRecords = exportedRecords
        };
    }

    public async Task<Result<byte[]>> ExportEsicReturnCsv(string companyId, PayrollReportQueryDto query)
    {
        var validation = Validate(query);
        if (!validation.Success) return new Result<byte[]> { Success = false, StatusCode = validation.StatusCode, Message = validation.Message };

        var selectedRuns = await LoadRuns(companyId, query);
        var runIds = selectedRuns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runEmployees = (await employees.GetAll(x => x.CompanyId == companyId && runIds.Contains(x.PayrollRunId)))
            .Where(x => string.IsNullOrWhiteSpace(query.EmployeeId) || string.Equals(x.EmployeeId, query.EmployeeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Status == "CALCULATED")
            .ToList();

        var missingEsicBase = runEmployees.FirstOrDefault(x =>
            ((x.StatutorySnapshot?.EmployeeEsic ?? 0m) > 0m || (x.StatutorySnapshot?.EmployerEsic ?? 0m) > 0m) &&
            x.StatutorySnapshot?.EsicWages is null);
        if (missingEsicBase is not null)
            return new Result<byte[]> { Success = false, StatusCode = 422, Message = $"ESI return export is blocked because employee {missingEsicBase.EmployeeId} has contribution amounts without a frozen ESI wage base. Reconcile the historical payroll data before filing." };

        var users = userRepo is null ? [] : (await userRepo.GetAll(x => x.CompanyId == companyId)).ToDictionary(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        builder.AppendLine("IPNumber,IPName,NoOfDaysWorked,TotalMonthlyWages,IPContribution,ReasonForZeroWorkingDays");

        foreach (var member in runEmployees)
        {
            var statutory = member.StatutorySnapshot;
            var eeShare = Round(statutory?.EmployeeEsic ?? 0m);
            var esicCeiling = member.InputSnapshot?.EsicWageCeiling is > 0m ? member.InputSnapshot.EsicWageCeiling : 21000m;
            if (eeShare <= 0m && member.GrossAmount > esicCeiling && statutory?.EsicCoverageEligible != true)
                continue;

            users.TryGetValue(member.EmployeeId, out var user);
            var memberName = user is not null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : member.EmployeeId;

            var esicIpNumber = user?.CustomAttributeList?.FirstOrDefault(x => string.Equals(x.CustomAttributeId, "ESIC", StringComparison.OrdinalIgnoreCase) || string.Equals(x.CustomAttributeId, "IPNumber", StringComparison.OrdinalIgnoreCase))?.CustomAttributeValueId
                               ?? member.EmployeeId;

            var grossWages = Round(statutory?.EsicWages ?? 0m);
            var daysWorked = 30;
            var reasonCode = daysWorked == 0 ? "LEAVE_WITHOUT_PAY" : "";

            builder.AppendLine($"{Csv(esicIpNumber)},{Csv(memberName)},{daysWorked},{grossWages.ToString("0.00", CultureInfo.InvariantCulture)},{eeShare.ToString("0.00", CultureInfo.InvariantCulture)},{Csv(reasonCode)}");
        }

        return new Result<byte[]>
        {
            Success = true,
            MethodResult = Encoding.UTF8.GetBytes(builder.ToString()),
            TotalRecords = runEmployees.Count
        };
    }

    public async Task<Result<List<Form16CertificateDto>>> GetForm16Certificates(string companyId, string financialYear, string? employeeId = null)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(financialYear))
            return new Result<List<Form16CertificateDto>> { Success = false, StatusCode = 400, Message = "CompanyId and FinancialYear are required." };

        var taxPeriod = PayrollTaxPeriod.TryResolveFinancialYear(financialYear.Trim());
        if (taxPeriod is null)
            return new Result<List<Form16CertificateDto>> { Success = false, StatusCode = 400, Message = "FinancialYear must use the YYYY-YY format." };

        var fyStart = taxPeriod.StartDate.Date;
        var fyEnd = taxPeriod.EndDateExclusive.Date;

        var allRuns = (await runs.GetAll(x => x.CompanyId == companyId))
            .Where(x => x.PeriodStart.Date >= fyStart && x.PeriodStart.Date < fyEnd)
            .Where(x => string.Equals(x.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var runIds = allRuns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var employeeLines = (await employees.GetAll(x => x.CompanyId == companyId && runIds.Contains(x.PayrollRunId)))
            .Where(x => string.IsNullOrWhiteSpace(employeeId) || string.Equals(x.EmployeeId, employeeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(x.Status, "BLOCKED", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.Status, "SKIPPED", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var userMap = userRepo is not null
            ? (await userRepo.GetAll(x => x.CompanyId == companyId))
                .Where(u => !string.IsNullOrWhiteSpace(u.EmployeeId))
                .GroupBy(u => u.EmployeeId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, EmpUser>(StringComparer.OrdinalIgnoreCase);

        var declarationMap = declarations is not null
            ? (await declarations.GetAll(x => x.CompanyId == companyId && x.FinancialYear == financialYear.Trim() && x.Status == "APPROVED"))
                .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Version).First(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, EmployeeTaxDeclaration>(StringComparer.OrdinalIgnoreCase);

        var groupedByEmp = employeeLines.GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase);
        var certificates = new List<Form16CertificateDto>();

        var parts = financialYear.Split('-');
        int startYear = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var assessmentYear = $"{startYear + 1}-{String.Format(CultureInfo.InvariantCulture, "{0:D2}", (startYear + 2) % 100)}";

        foreach (var empGroup in groupedByEmp)
        {
            var empId = empGroup.Key;
            userMap.TryGetValue(empId, out var user);
            declarationMap.TryGetValue(empId, out var declaration);

            var grossSalary = Round(empGroup.Sum(x => x.GrossAmount));
            var ptDeducted = Round(empGroup.Sum(x => x.StatutorySnapshot?.ProfessionalTax ?? 0m));
            var tdsDeducted = Round(empGroup.Sum(x => x.StatutorySnapshot?.IncomeTax ?? 0m));

            var regime = declaration?.Regime ?? "NEW_115BAC";
            var isNewRegime = regime == "NEW_115BAC";

            var stdDeduction = isNewRegime
                ? (taxPeriod.TaxRuleSet?.NewRegimeStandardDeduction ?? 75000m)
                : (taxPeriod.TaxRuleSet?.OldRegimeStandardDeduction ?? 50000m);
            stdDeduction = Math.Min(grossSalary, stdDeduction);

            var hraExemption = isNewRegime ? 0m : Round(declaration?.ApprovedHraExemption ?? 0m);
            var totalSalaryAfterSec10 = Math.Max(0m, grossSalary - hraExemption);

            var totalSec16 = Round(stdDeduction + ptDeducted);
            var incomeFromSalaries = Math.Max(0m, totalSalaryAfterSec10 - totalSec16);

            var otherIncome = Round((declaration?.OtherIncomeSavingsInterest ?? 0m) + (declaration?.OtherIncomeOther ?? 0m));
            var previousEmployerSalary = Round(declaration?.PreviousEmployerGrossSalary ?? 0m);
            var grossTotalIncome = incomeFromSalaries + otherIncome + previousEmployerSalary;

            var sec80C = isNewRegime ? 0m : Math.Min(150000m, Round(declaration?.ApprovedSection80C ?? 0m));
            var sec80Ccd1B = isNewRegime ? 0m : Math.Min(50000m, Round(declaration?.ApprovedSection80Ccd1B ?? 0m));
            var sec80D = isNewRegime ? 0m : Math.Min(100000m, Round(declaration?.ApprovedSection80D ?? 0m));
            var sec24Loss = isNewRegime ? 0m : Math.Min(200000m, Round(declaration?.ApprovedSection24HomeLoanInterest ?? 0m));
            var sec80E = isNewRegime ? 0m : Round(declaration?.ApprovedSection80E ?? 0m);
            var otherDeductions = isNewRegime ? 0m : Round(declaration?.ApprovedOtherDeductions ?? 0m);

            var totalChapterVIA = Round(sec80C + sec80Ccd1B + sec80D + sec24Loss + sec80E + otherDeductions);
            var totalTaxableIncome = Math.Max(0m, grossTotalIncome - totalChapterVIA);

            decimal totalTaxPayable = 0m;
            decimal rebate87A = 0m;
            decimal cess = 0m;
            decimal taxOnIncome = 0m;

            if (taxPeriod.TaxRuleSet is not null)
            {
                totalTaxPayable = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(
                    grossSalary,
                    isNewRegime ? "AUTO_NEW_REGIME" : "AUTO_OLD_REGIME",
                    previousEmployerSalary,
                    otherIncome,
                    sec80C,
                    sec80Ccd1B,
                    sec80D,
                    sec24Loss,
                    hraExemption,
                    sec80E + otherDeductions,
                    taxPeriod.TaxRuleSet);

                cess = Round(totalTaxPayable * 4m / 104m);
                taxOnIncome = totalTaxPayable - cess;
                if (totalTaxPayable == 0m && grossTotalIncome > 0m)
                {
                    rebate87A = taxOnIncome;
                }
            }

            var periodMin = empGroup.Min(x => allRuns.First(r => r.Id == x.PayrollRunId).PeriodStart);
            var periodMax = empGroup.Max(x => allRuns.First(r => r.Id == x.PayrollRunId).PeriodEnd);

            certificates.Add(new Form16CertificateDto
            {
                EmployeeId = empId,
                EmployeeName = string.Join(" ", new[] { user?.FirstName, user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Pan = user?.PanNumber ?? string.Empty,
                FinancialYear = financialYear.Trim(),
                AssessmentYear = assessmentYear,
                Regime = regime,
                PeriodFrom = periodMin,
                PeriodTo = periodMax,
                GrossSalarySection17_1 = grossSalary,
                TotalGrossSalary = grossSalary,
                ExemptionHraSection10_13A = hraExemption,
                TotalExemptionsSection10 = hraExemption,
                TotalSalaryAfterSection10 = totalSalaryAfterSec10,
                StandardDeductionSection16_ia = stdDeduction,
                ProfessionalTaxSection16_iii = ptDeducted,
                TotalDeductionsSection16 = totalSec16,
                IncomeChargeableUnderSalaries = incomeFromSalaries,
                OtherIncomeReported = otherIncome,
                GrossTotalIncome = grossTotalIncome,
                Section80C = sec80C,
                Section80Ccd1B = sec80Ccd1B,
                Section80D = sec80D,
                Section24HomeLoanInterest = sec24Loss,
                Section80E = sec80E,
                OtherDeductionsChapterVIA = otherDeductions,
                TotalChapterVIADeductions = totalChapterVIA,
                TotalTaxableIncome = totalTaxableIncome,
                TaxOnTotalIncome = taxOnIncome,
                RebateSection87A = rebate87A,
                HealthAndEducationCess = cess,
                TotalTaxPayable = totalTaxPayable,
                NetTaxPayable = totalTaxPayable,
                TotalTdsDeducted = tdsDeducted,
                TotalTdsDeposited = tdsDeducted
            });
        }

        return new Result<List<Form16CertificateDto>>
        {
            Success = true,
            MethodResult = certificates.OrderBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase).ToList(),
            TotalRecords = certificates.Count
        };
    }

    public async Task<Result<byte[]>> ExportForm16SummaryCsv(string companyId, string financialYear)
    {
        var certsResult = await GetForm16Certificates(companyId, financialYear);
        if (!certsResult.Success)
            return new Result<byte[]> { Success = false, StatusCode = certsResult.StatusCode, Message = certsResult.Message };

        var sb = new StringBuilder();
        sb.AppendLine("Employee ID,Employee Name,PAN,Financial Year,Assessment Year,Tax Regime,Gross Salary (17(1)),Exemptions (Sec 10),Standard Deduction (Sec 16(ia)),Professional Tax (Sec 16(iii)),Income from Salaries,Other Income,Gross Total Income,Section 80C,Section 80CCD(1B),Section 80D,Section 24,Other Chapter VI-A,Total Chapter VI-A Deductions,Total Taxable Income,Tax on Income,Rebate 87A,Cess (4%),Total Tax Payable,TDS Deducted,TDS Deposited");

        foreach (var c in certsResult.MethodResult ?? [])
        {
            sb.AppendLine(string.Join(",",
                Csv(c.EmployeeId),
                Csv(c.EmployeeName),
                Csv(c.Pan),
                Csv(c.FinancialYear),
                Csv(c.AssessmentYear),
                Csv(c.Regime),
                c.GrossSalarySection17_1.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalExemptionsSection10.ToString("F2", CultureInfo.InvariantCulture),
                c.StandardDeductionSection16_ia.ToString("F2", CultureInfo.InvariantCulture),
                c.ProfessionalTaxSection16_iii.ToString("F2", CultureInfo.InvariantCulture),
                c.IncomeChargeableUnderSalaries.ToString("F2", CultureInfo.InvariantCulture),
                c.OtherIncomeReported.ToString("F2", CultureInfo.InvariantCulture),
                c.GrossTotalIncome.ToString("F2", CultureInfo.InvariantCulture),
                c.Section80C.ToString("F2", CultureInfo.InvariantCulture),
                c.Section80Ccd1B.ToString("F2", CultureInfo.InvariantCulture),
                c.Section80D.ToString("F2", CultureInfo.InvariantCulture),
                c.Section24HomeLoanInterest.ToString("F2", CultureInfo.InvariantCulture),
                c.OtherDeductionsChapterVIA.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalChapterVIADeductions.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalTaxableIncome.ToString("F2", CultureInfo.InvariantCulture),
                c.TaxOnTotalIncome.ToString("F2", CultureInfo.InvariantCulture),
                c.RebateSection87A.ToString("F2", CultureInfo.InvariantCulture),
                c.HealthAndEducationCess.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalTaxPayable.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalTdsDeducted.ToString("F2", CultureInfo.InvariantCulture),
                c.TotalTdsDeposited.ToString("F2", CultureInfo.InvariantCulture)
            ));
        }

        return new Result<byte[]>
        {
            Success = true,
            MethodResult = Encoding.UTF8.GetBytes(sb.ToString())
        };
    }

    private async Task<List<PayrollRun>> LoadRuns(string companyId, PayrollReportQueryDto query)
    {
        var rows = (await runs.GetAll(x => x.CompanyId == companyId)).AsEnumerable();
        if (query.From.HasValue) rows = rows.Where(x => x.PeriodStart.Date >= query.From.Value.Date);
        if (query.To.HasValue) rows = rows.Where(x => x.PeriodStart.Date <= query.To.Value.Date);
        if (!string.IsNullOrWhiteSpace(query.Status)) rows = rows.Where(x => string.Equals(x.Status, query.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.RunType)) rows = rows.Where(x => string.Equals(x.RunType, query.RunType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.PayGroupId)) rows = rows.Where(x => string.Equals(x.PayGroupId, query.PayGroupId.Trim(), StringComparison.OrdinalIgnoreCase));
        return rows.OrderByDescending(x => x.PeriodStart).ThenByDescending(x => x.CreatedAtUtc).ToList();
    }

    private static Result Validate(PayrollReportQueryDto query)
    {
        if (query.From.HasValue && query.To.HasValue && query.From.Value.Date > query.To.Value.Date)
            return new Result { Success = false, StatusCode = 400, Message = "Report start date cannot be after the end date." };
        return new Result { Success = true };
    }

    private static decimal Round(decimal value) => PayrollCalculationRules.RoundMoney(value);

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{escaped}\"" : escaped;
    }
}
