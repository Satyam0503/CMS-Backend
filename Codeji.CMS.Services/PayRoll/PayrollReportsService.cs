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
}

public sealed class PayrollReportsService(
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> employees) : IPayrollReportsService
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
